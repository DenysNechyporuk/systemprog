using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace Messanger;

public partial class MainWindow : Window
{
    const string serverAddress = "127.0.0.1";
    const int serverPort = 4040;
    IPEndPoint serverPoint;
    UdpClient client = null;
    ObservableCollection<MessageInfo> messages;
    bool isJoined = false;

    public MainWindow()
    {
        InitializeComponent();
        serverPoint = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);
        messages = new ObservableCollection<MessageInfo>();
        this.DataContext = messages;
    }

    private void SendButtonClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(msgTextBox.Text))
        {
            MessageBox.Show("You didn't enter a message!");
            return;
        }

        SendMessage(msgTextBox.Text);
        msgTextBox.Text = "";
    }

    private void JoinButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(nickTextBox.Text))
        {
            MessageBox.Show("Enter your nickname!");
            return;
        }

        client = new UdpClient();
        isJoined = true;
        SendMessage("$<Join>" + nickTextBox.Text.Trim());

        joinBtn.IsEnabled = false;
        nickTextBox.IsEnabled = false;
        sendBtn.IsEnabled = true;
        leaveBtn.IsEnabled = true;

        Listener();
    }

    private void LeaveButton_Click(object sender, RoutedEventArgs e)
    {
        Leave();
    }

    private async void Leave()
    {
        if (!isJoined)
            return;

        isJoined = false;
        byte[] data = Encoding.Unicode.GetBytes("$<Leave>");
        await client.SendAsync(data, data.Length, serverPoint);
        client.Close();

        messages.Add(new MessageInfo("You left the chat"));
        joinBtn.IsEnabled = true;
        nickTextBox.IsEnabled = true;
        sendBtn.IsEnabled = false;
        leaveBtn.IsEnabled = false;
    }

    private async void SendMessage(string message)
    {
        try
        {
            byte[] data = Encoding.Unicode.GetBytes(message);
            await client.SendAsync(data, data.Length, serverPoint);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void Listener()
    {
        while (isJoined)
        {
            try
            {
                var data = await client.ReceiveAsync();
                string message = Encoding.Unicode.GetString(data.Buffer);
                messages.Add(new MessageInfo(message));
            }
            catch (Exception)
            {
                if (isJoined)
                    messages.Add(new MessageInfo("Server is not available"));
            }
        }
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        Leave();
    }
}

public class MessageInfo
{
    public string Text { get; set; }
    public DateTime Time { get; set; }

    public MessageInfo(string message)
    {
        Text = message;
        Time = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{Text,-20} {Time}";
    }
}
