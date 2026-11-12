using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;

namespace SMTPHW;

public partial class MainWindow : Window
{
    string server = "";
    int port = 587;
    string username = "";
    string password = "";
    ObservableCollection<string> files = new ObservableCollection<string>();

    public MainWindow()
    {
        InitializeComponent();
        filesListBox.ItemsSource = files;
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(emailTextBox.Text) || string.IsNullOrWhiteSpace(passwordBox.Password))
        {
            MessageBox.Show("Enter email and password!");
            return;
        }

        server = ((ComboBoxItem)serviceComboBox.SelectedItem).Content.ToString()!;
        username = emailTextBox.Text.Trim();
        password = passwordBox.Password;

        fromLabel.Content = username;
        loginPanel.Visibility = Visibility.Collapsed;
        sendPanel.Visibility = Visibility.Visible;
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        username = "";
        password = "";
        passwordBox.Password = "";
        sendPanel.Visibility = Visibility.Collapsed;
        loginPanel.Visibility = Visibility.Visible;
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

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(toTextBox.Text))
        {
            MessageBox.Show("Enter receiver email!");
            return;
        }

        try
        {
            MailMessage message = new MailMessage(username, toTextBox.Text.Trim(), subjectTextBox.Text, bodyTextBox.Text);
            if (importantCheckBox.IsChecked == true)
                message.Priority = MailPriority.High;

            foreach (string file in files)
                message.Attachments.Add(new Attachment(file));

            SmtpClient client = new SmtpClient(server, port);
            client.EnableSsl = true;
            client.Credentials = new NetworkCredential(username, password);
            client.SendCompleted += Client_SendCompleted;
            client.SendAsync(message, message);

            this.Title = "SMTP Mail Sender - sending...";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void Client_SendCompleted(object? sender, AsyncCompletedEventArgs e)
    {
        this.Title = "SMTP Mail Sender";
        var state = (MailMessage)e.UserState!;

        if (e.Error != null)
        {
            MessageBox.Show("Error : " + e.Error.Message);
        }
        else
        {
            MessageBox.Show($"Message was send! Subject : {state?.Subject}");
            toTextBox.Text = "";
            subjectTextBox.Text = "";
            bodyTextBox.Text = "";
            importantCheckBox.IsChecked = false;
            files.Clear();
        }

        state?.Dispose();
    }
}
