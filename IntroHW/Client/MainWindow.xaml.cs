using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace Client;

public partial class MainWindow : Window
{
    UdpClient client = new UdpClient();
    ObservableCollection<MessageInfo> messages = new ObservableCollection<MessageInfo>();
    bool isListening = false;

    public MainWindow()
    {
        InitializeComponent();
        dialogList.ItemsSource = messages;
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(msgTxtBox.Text))
        {
            MessageBox.Show("Enter message!");
            return;
        }

        try
        {
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ipTxtBox.Text), int.Parse(portTxtBox.Text));
            byte[] data = Encoding.Unicode.GetBytes(msgTxtBox.Text);
            await client.SendAsync(data, data.Length, serverEndPoint);

            messages.Insert(0, new MessageInfo("Я: " + msgTxtBox.Text));
            msgTxtBox.Text = "";

            if (!isListening)
                Listen();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void Listen()
    {
        isListening = true;
        while (isListening)
        {
            try
            {
                var result = await client.ReceiveAsync();
                string message = Encoding.Unicode.GetString(result.Buffer);
                messages.Insert(0, new MessageInfo("Говорун: " + message));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Server is not responding\n" + ex.Message);
                isListening = false;
            }
        }
    }
}

public class MessageInfo
{
    public string Text { get; set; }
    public string Time { get; set; }

    public MessageInfo(string text)
    {
        Text = text;
        Time = DateTime.Now.ToShortTimeString();
    }
}
