using ClientRequest;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Windows;

namespace ChatUI;

public partial class MainWindow : Window
{
    private string remoteIP = "127.0.0.1";
    private int port = 4040;
    TcpClient client = null;
    StreamReader sr = null;
    StreamWriter sw = null;
    string name = "";
    bool isConnected = false;
    ObservableCollection<string> messages;

    public MainWindow()
    {
        InitializeComponent();
        messages = new ObservableCollection<string>();
        this.DataContext = messages;
    }

    private async void JoinButton_Click(object sender, RoutedEventArgs e)
    {
        if (isConnected)
            return;

        if (string.IsNullOrWhiteSpace(nameTextBox.Text))
        {
            MessageBox.Show("Enter your name!");
            return;
        }

        try
        {
            client = new TcpClient();
            await client.ConnectAsync(IPAddress.Parse(remoteIP), port);
            NetworkStream ns = client.GetStream();
            sr = new StreamReader(ns);
            sw = new StreamWriter(ns);

            name = nameTextBox.Text.Trim();
            await SendRequest(new ClientCommand(name, "", RequestCommand.Join));

            ClientCommand answer = await GetResponse();
            if (answer.Command == RequestCommand.ChatFull)
            {
                MessageBox.Show(answer.Message);
                client.Close();
                return;
            }

            isConnected = true;
            SetButtons();
            messages.Add(answer.ToString());
            ServerListener();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void ServerListener()
    {
        while (isConnected)
        {
            try
            {
                ClientCommand command = await GetResponse();
                if (command.Command == RequestCommand.Message)
                {
                    messages.Add(command.ToString());
                }
                else if (command.Command == RequestCommand.ServerClosed)
                {
                    messages.Add(command.ToString());
                    Disconnect();
                }
            }
            catch (Exception)
            {
                if (isConnected)
                {
                    messages.Add("Connection with server was lost");
                    Disconnect();
                }
            }
        }
    }

    private async Task<ClientCommand> GetResponse()
    {
        string? resp = await sr.ReadLineAsync();
        if (resp == null)
            throw new Exception("Server disconnected");
        return JsonSerializer.Deserialize<ClientCommand>(resp)!;
    }

    private async Task SendRequest(ClientCommand command)
    {
        string json = JsonSerializer.Serialize(command);
        await sw.WriteLineAsync(json);
        await sw.FlushAsync();
    }

    private async void LeaveButton_Click(object sender, RoutedEventArgs e)
    {
        await Leave();
    }

    private async Task Leave()
    {
        if (!isConnected)
            return;

        try
        {
            await SendRequest(new ClientCommand(name, "", RequestCommand.Leave));
        }
        catch (Exception)
        { }

        messages.Add("You left the chat");
        Disconnect();
    }

    private void Disconnect()
    {
        isConnected = false;
        client?.Close();
        SetButtons();
    }

    private void SetButtons()
    {
        joinBtn.IsEnabled = !isConnected;
        nameTextBox.IsEnabled = !isConnected;
        leaveBtn.IsEnabled = isConnected;
        sendBtn.IsEnabled = isConnected;
    }

    private async void SendButtonClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(msgTextBox.Text))
        {
            MessageBox.Show("Enter message!");
            return;
        }

        try
        {
            await SendRequest(new ClientCommand(name, msgTextBox.Text));
            msgTextBox.Text = "";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void Window_Closed(object sender, EventArgs e)
    {
        await Leave();
    }
}
