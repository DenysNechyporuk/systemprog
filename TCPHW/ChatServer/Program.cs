using ClientRequest;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

internal class Program
{
    private const string localIp = "127.0.0.1";
    private const int port = 4040;
    private const int maxMembers = 3;

    static TcpListener server;
    static Chat chat;

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(localIp), port);
        server = new TcpListener(localEndPoint);
        chat = new Chat(maxMembers);
        server.Start();

        Console.WriteLine($"Server started. Max members : {maxMembers}");
        Console.WriteLine("Type 'exit' to stop the server");

        Task.Run(() => AcceptClients());

        while (true)
        {
            string? text = Console.ReadLine();
            if (text == "exit")
                break;
        }

        chat.CloseAll();
        server.Stop();
        Console.WriteLine("Server stopped");
    }

    static void AcceptClients()
    {
        while (true)
        {
            try
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Got a TcpClient");
                StreamReader sr = new StreamReader(client.GetStream());
                ClientCommand req = Chat.GetClientCommand(sr);
                Console.WriteLine($"\tGot a {req.Command} request from {req.Name}");

                if (req.Command == RequestCommand.Join)
                {
                    chat.AddMember(client, sr, req.Name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                break;
            }
        }
    }
}

class Chat
{
    Dictionary<TcpClient, string> members = new Dictionary<TcpClient, string>();
    int maxMembers;

    public Chat(int maxMembers)
    {
        this.maxMembers = maxMembers;
    }

    public void AddMember(TcpClient client, StreamReader sr, string name)
    {
        lock (members)
        {
            if (members.Count >= maxMembers)
            {
                SendMessage(new ClientCommand("Server", $"Chat is full! Max members : {maxMembers}", RequestCommand.ChatFull), client);
                client.Close();
                Console.WriteLine($"\t{name} was rejected, chat is full");
                return;
            }

            members.Add(client, name);
        }

        SendMessage(new ClientCommand("Server", "You joined the chat", RequestCommand.Joined), client);
        SendToAll(new ClientCommand("Server", $"{name} joined the chat"));
        Task.Run(() => ListenerClient(client, sr, name));
    }

    public void ListenerClient(TcpClient client, StreamReader sr, string name)
    {
        bool isExit = false;
        while (!isExit)
        {
            try
            {
                ClientCommand command = GetClientCommand(sr);
                switch (command.Command)
                {
                    case RequestCommand.Leave:
                        RemoveMember(client);
                        SendToAll(new ClientCommand("Server", $"{name} left the chat"));
                        isExit = true;
                        break;
                    case RequestCommand.Message:
                        Console.WriteLine(command);
                        SendToAll(command);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception)
            {
                if (RemoveMember(client))
                    SendToAll(new ClientCommand("Server", $"{name} disconnected"));
                isExit = true;
            }
        }
    }

    private bool RemoveMember(TcpClient client)
    {
        lock (members)
        {
            if (!members.ContainsKey(client))
                return false;
            members.Remove(client);
        }
        client.Close();
        return true;
    }

    public void CloseAll()
    {
        List<TcpClient> list;
        lock (members)
        {
            list = members.Keys.ToList();
            members.Clear();
        }

        foreach (var member in list)
        {
            try
            {
                SendMessage(new ClientCommand("Server", "Server was stopped", RequestCommand.ServerClosed), member);
                member.Close();
            }
            catch (Exception)
            { }
        }
    }

    private void SendToAll(ClientCommand command)
    {
        List<TcpClient> list;
        lock (members)
        {
            list = members.Keys.ToList();
        }

        foreach (var member in list)
        {
            try
            {
                SendMessage(command, member);
            }
            catch (Exception)
            { }
        }
    }

    public static ClientCommand GetClientCommand(StreamReader sr)
    {
        string? line = sr.ReadLine();
        if (line == null)
            throw new Exception("Client disconnected");
        return JsonSerializer.Deserialize<ClientCommand>(line)!;
    }

    private void SendMessage(ClientCommand command, TcpClient reciever)
    {
        lock (reciever)
        {
            NetworkStream ns = reciever.GetStream();
            StreamWriter sw = new StreamWriter(ns);
            sw.WriteLine(JsonSerializer.Serialize(command));
            sw.Flush();
        }
    }
}
