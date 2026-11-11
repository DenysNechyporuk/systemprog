using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    static int port = 8080;
    static Dictionary<string, string> answers = new Dictionary<string, string>();
    static List<string> defaultAnswers = new List<string>()
    {
        "Не зовсім зрозумів тебе",
        "Цікаво, розкажи ще",
        "Хм, а ти як думаєш?",
        "Можливо",
        "Я про це не знаю, але звучить класно"
    };
    static Random rnd = new Random();

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        LoadAnswers();

        UdpClient server = new UdpClient(port);
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        Console.WriteLine($"Server started on port {port}!!! Waiting for messages");

        while (true)
        {
            try
            {
                byte[] data = server.Receive(ref remoteEndPoint);
                string message = Encoding.Unicode.GetString(data);
                Console.WriteLine($"{DateTime.Now.ToShortTimeString()} {remoteEndPoint} : {message}");

                string answer = GetAnswer(message);
                data = Encoding.Unicode.GetBytes(answer);
                server.Send(data, data.Length, remoteEndPoint);
                Console.WriteLine($"\tanswer : {answer}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    static void LoadAnswers()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "answers.txt");
        if (!File.Exists(path))
        {
            Console.WriteLine("answers.txt not found");
            return;
        }

        foreach (string line in File.ReadAllLines(path))
        {
            string[] parts = line.Split('|');
            if (parts.Length == 2)
                answers[parts[0].ToLower()] = parts[1];
        }
        Console.WriteLine($"Loaded {answers.Count} answers");
    }

    static string GetAnswer(string message)
    {
        string text = message.ToLower();
        foreach (var item in answers)
        {
            if (text.Contains(item.Key))
                return item.Value;
        }
        return defaultAnswers[rnd.Next(defaultAnswers.Count)];
    }
}
