using System.Net;
using System.Net.Sockets;
using System.Text;

public class ChatServer
{
    const int port = 4040;
    const string JOIN_CMD = "$<Join>";
    const string LEAVE_CMD = "$<Leave>";

    Dictionary<IPEndPoint, string> members;
    UdpClient udpClient;
    IPEndPoint clientEndPoint = null;

    public ChatServer()
    {
        udpClient = new UdpClient(port);
        members = new Dictionary<IPEndPoint, string>();
    }

    public void Start()
    {
        Console.WriteLine("Chat server started!!!");
        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref clientEndPoint);
                string message = Encoding.Unicode.GetString(data);

                if (message.StartsWith(JOIN_CMD))
                {
                    AddMember(clientEndPoint, message.Substring(JOIN_CMD.Length));
                }
                else if (message == LEAVE_CMD)
                {
                    RemoveMember(clientEndPoint);
                }
                else
                {
                    if (!members.ContainsKey(clientEndPoint))
                        continue;
                    if (string.IsNullOrWhiteSpace(message))
                        continue;

                    string nickname = members[clientEndPoint];
                    Console.WriteLine($"Got message {message,-20} from {nickname} {clientEndPoint} at {DateTime.Now.ToShortTimeString()}");
                    SendToAll($"{nickname} : {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    private void AddMember(IPEndPoint clientPoint, string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname))
            nickname = "Anonymous";

        members[clientPoint] = nickname;
        Console.WriteLine($"Member {nickname} was added");
        SendToAll($"{nickname} joined the chat");
    }

    private void RemoveMember(IPEndPoint clientPoint)
    {
        if (!members.ContainsKey(clientPoint))
            return;

        string nickname = members[clientPoint];
        members.Remove(clientPoint);
        Console.WriteLine($"Member {nickname} left");
        SendToAll($"{nickname} left the chat");
    }

    private void SendToAll(string message)
    {
        byte[] data = Encoding.Unicode.GetBytes(message);
        foreach (var member in members.Keys)
        {
            udpClient.SendAsync(data, data.Length, member);
        }
    }
}

internal class Program
{
    private static void Main(string[] args)
    {
        ChatServer chat = new ChatServer();
        chat.Start();
    }
}
