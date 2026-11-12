using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System.Windows;
using System.Windows.Controls;

namespace IMAPHW;

public partial class MainWindow : Window
{
    ImapClient client = new ImapClient();
    List<MailItem> mails = new List<MailItem>();
    IMailFolder? currentFolder = null;
    bool isLoading = false;

    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string SmtpServer { get; set; } = "";
    public int SmtpPort { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(emailTextBox.Text) || string.IsNullOrWhiteSpace(passwordBox.Password))
        {
            MessageBox.Show("Enter email and password!");
            return;
        }

        string imapServer = "imap.gmail.com";
        SmtpServer = "smtp.gmail.com";
        SmtpPort = 587;
        if (serviceComboBox.SelectedIndex == 1)
        {
            imapServer = "imap.ukr.net";
            SmtpServer = "smtp.ukr.net";
            SmtpPort = 465;
        }
        else if (serviceComboBox.SelectedIndex == 2)
        {
            imapServer = "imap.mail.me.com";
            SmtpServer = "smtp.mail.me.com";
            SmtpPort = 587;
        }

        Username = emailTextBox.Text.Trim();
        Password = passwordBox.Password;
        loginBtn.IsEnabled = false;

        try
        {
            await client.ConnectAsync(imapServer, 993, MailKit.Security.SecureSocketOptions.SslOnConnect);
            await client.AuthenticateAsync(Username, Password);

            loginPanel.Visibility = Visibility.Collapsed;
            mainPanel.Visibility = Visibility.Visible;
            this.Title = "Mail Client - " + Username;

            await LoadFolders();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Login error : " + ex.Message);
            if (client.IsConnected)
                await client.DisconnectAsync(true);
        }
        loginBtn.IsEnabled = true;
    }

    private async Task LoadFolders()
    {
        var folders = await client.GetFoldersAsync(client.PersonalNamespaces[0]);
        var list = folders.Where(f => !f.Attributes.HasFlag(FolderAttributes.NoSelect)).ToList();
        foldersListBox.ItemsSource = list;
        moveComboBox.ItemsSource = list;
    }

    private async void FoldersListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (foldersListBox.SelectedItem == null)
            return;

        currentFolder = (IMailFolder)foldersListBox.SelectedItem;
        searchTextBox.Text = "";
        await LoadMessages();
    }

    private async Task LoadMessages()
    {
        if (currentFolder == null || isLoading)
            return;

        isLoading = true;
        try
        {
            statusText.Text = "Loading...";
            ClearDetails();

            if (!currentFolder.IsOpen)
                await currentFolder.OpenAsync(FolderAccess.ReadWrite);

            SearchQuery query = SearchQuery.All;
            if (!string.IsNullOrWhiteSpace(searchTextBox.Text))
                query = SearchQuery.SubjectContains(searchTextBox.Text).Or(SearchQuery.FromContains(searchTextBox.Text));
            if (unreadCheckBox.IsChecked == true)
                query = query.And(SearchQuery.NotSeen);

            var uids = await currentFolder.SearchAsync(query);
            mails.Clear();

            foreach (var uid in uids.Reverse().Take(30))
            {
                MimeMessage message = await currentFolder.GetMessageAsync(uid);
                mails.Add(new MailItem(uid, message));
            }

            ShowMessages();
            statusText.Text = $"Found : {uids.Count} (showing last {mails.Count})";
        }
        catch (Exception ex)
        {
            statusText.Text = "";
            MessageBox.Show(ex.Message);
        }
        isLoading = false;
    }

    private void ShowMessages()
    {
        List<MailItem> sorted = mails;
        switch (sortComboBox.SelectedIndex)
        {
            case 0:
                sorted = mails.OrderByDescending(m => m.Date).ToList();
                break;
            case 1:
                sorted = mails.OrderBy(m => m.Date).ToList();
                break;
            case 2:
                sorted = mails.OrderBy(m => m.Subject).ToList();
                break;
            case 3:
                sorted = mails.OrderBy(m => m.From).ToList();
                break;
        }
        messagesListView.ItemsSource = sorted;
    }

    private void MessagesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (messagesListView.SelectedItem == null)
            return;

        MailItem mail = (MailItem)messagesListView.SelectedItem;
        fromRun.Text = mail.From;
        toRun.Text = mail.To;
        dateRun.Text = mail.Date.ToString("dd.MM.yyyy HH:mm");
        subjectRun.Text = mail.Subject;
        bodyTextBox.Text = mail.Text;
        attachmentsListBox.ItemsSource = mail.Attachments;
    }

    private void ClearDetails()
    {
        fromRun.Text = "";
        toRun.Text = "";
        dateRun.Text = "";
        subjectRun.Text = "";
        bodyTextBox.Text = "";
        attachmentsListBox.ItemsSource = null;
    }

    private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (messagesListView != null)
            ShowMessages();
    }

    private async void UnreadCheckBox_Click(object sender, RoutedEventArgs e)
    {
        await LoadMessages();
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadMessages();
    }

    private void NewMessageButton_Click(object sender, RoutedEventArgs e)
    {
        SendWindow window = new SendWindow(this, "", "", "");
        window.Show();
    }

    private void ReplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (messagesListView.SelectedItem == null)
        {
            MessageBox.Show("Select a message!");
            return;
        }

        MailItem mail = (MailItem)messagesListView.SelectedItem;
        string subject = mail.Subject.StartsWith("Re:") ? mail.Subject : "Re: " + mail.Subject;
        string body = $"\n\n----- {mail.Date} {mail.From} wrote: -----\n{mail.Text}";
        SendWindow window = new SendWindow(this, mail.FromAddress, subject, body);
        window.Show();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (messagesListView.SelectedItem == null || currentFolder == null)
        {
            MessageBox.Show("Select a message!");
            return;
        }

        if (MessageBox.Show("Delete this message?", "Delete", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        try
        {
            MailItem mail = (MailItem)messagesListView.SelectedItem;
            await currentFolder.AddFlagsAsync(mail.Uid, MessageFlags.Deleted, true);
            await currentFolder.ExpungeAsync();
            mails.Remove(mail);
            ShowMessages();
            ClearDetails();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void MoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (messagesListView.SelectedItem == null || currentFolder == null || moveComboBox.SelectedItem == null)
        {
            MessageBox.Show("Select a message and folder!");
            return;
        }

        IMailFolder target = (IMailFolder)moveComboBox.SelectedItem;
        if (target.FullName == currentFolder.FullName)
            return;

        try
        {
            MailItem mail = (MailItem)messagesListView.SelectedItem;
            await currentFolder.MoveToAsync(mail.Uid, target);
            mails.Remove(mail);
            ShowMessages();
            ClearDetails();
            MessageBox.Show("Message was moved to " + target.FullName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void CreateFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(folderNameTextBox.Text))
        {
            MessageBox.Show("Enter folder name!");
            return;
        }

        try
        {
            var root = client.GetFolder(client.PersonalNamespaces[0]);
            await root.CreateAsync(folderNameTextBox.Text.Trim(), true);
            folderNameTextBox.Text = "";
            await LoadFolders();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void RenameFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (foldersListBox.SelectedItem == null || string.IsNullOrWhiteSpace(folderNameTextBox.Text))
        {
            MessageBox.Show("Select folder and enter new name!");
            return;
        }

        try
        {
            IMailFolder folder = (IMailFolder)foldersListBox.SelectedItem;
            if (folder.IsOpen)
                await folder.CloseAsync();
            await folder.RenameAsync(folder.ParentFolder, folderNameTextBox.Text.Trim());
            folderNameTextBox.Text = "";
            currentFolder = null;
            mails.Clear();
            ShowMessages();
            await LoadFolders();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void Window_Closed(object sender, EventArgs e)
    {
        if (client.IsConnected)
            await client.DisconnectAsync(true);
    }
}

public class MailItem
{
    public UniqueId Uid { get; set; }
    public string From { get; set; }
    public string FromAddress { get; set; }
    public string To { get; set; }
    public string Subject { get; set; }
    public DateTime Date { get; set; }
    public string Text { get; set; }
    public List<string> Attachments { get; set; } = new List<string>();

    public MailItem(UniqueId uid, MimeMessage message)
    {
        Uid = uid;
        From = message.From.ToString();
        FromAddress = message.From.Mailboxes.FirstOrDefault()?.Address ?? "";
        To = message.To.ToString();
        Subject = message.Subject ?? "(no subject)";
        Date = message.Date.LocalDateTime;
        Text = message.TextBody ?? message.HtmlBody ?? "";

        foreach (var item in message.Attachments)
        {
            string? name = item.ContentDisposition?.FileName ?? item.ContentType.Name;
            if (name != null)
                Attachments.Add(name);
        }
    }
}
