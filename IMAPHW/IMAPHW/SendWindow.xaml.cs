using MailKit.Net.Smtp;
using Microsoft.Win32;
using MimeKit;
using System.Collections.ObjectModel;
using System.Windows;

namespace IMAPHW;

public partial class SendWindow : Window
{
    MainWindow main;
    ObservableCollection<string> files = new ObservableCollection<string>();

    public SendWindow(MainWindow main, string to, string subject, string body)
    {
        InitializeComponent();
        this.main = main;
        toTextBox.Text = to;
        subjectTextBox.Text = subject;
        bodyTextBox.Text = body;
        filesListBox.ItemsSource = files;
    }

    private void AddFileButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new OpenFileDialog();
        dialog.Multiselect = true;
        if (dialog.ShowDialog() == true)
        {
            foreach (string file in dialog.FileNames)
                files.Add(file);
        }
    }

    private void RemoveFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (filesListBox.SelectedItem != null)
            files.Remove((string)filesListBox.SelectedItem);
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(toTextBox.Text))
        {
            MessageBox.Show("Enter receiver email!");
            return;
        }

        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(main.Username, main.Username));
        message.To.Add(new MailboxAddress("", toTextBox.Text.Trim()));
        message.Subject = subjectTextBox.Text;

        var builder = new BodyBuilder();
        builder.TextBody = bodyTextBox.Text;
        foreach (string file in files)
            builder.Attachments.Add(file);
        message.Body = builder.ToMessageBody();

        sendBtn.IsEnabled = false;
        try
        {
            using (var client = new SmtpClient())
            {
                var options = main.SmtpPort == 465 ? MailKit.Security.SecureSocketOptions.SslOnConnect : MailKit.Security.SecureSocketOptions.StartTls;
                await client.ConnectAsync(main.SmtpServer, main.SmtpPort, options);
                await client.AuthenticateAsync(main.Username, main.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }

            MessageBox.Show("Message was send!");
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            sendBtn.IsEnabled = true;
        }
    }
}
