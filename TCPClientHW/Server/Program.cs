using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    const string localIp = "127.0.0.1";
    const int port = 5050;
    static Dictionary<string, string> regions = new Dictionary<string, string>();

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        LoadRegions();

        TcpListener server = new TcpListener(IPAddress.Parse(localIp), port);
        server.Start();
        Console.WriteLine($"Server started on {localIp}:{port}");

        while (true)
        {
            try
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine($"Client connected {client.Client.RemoteEndPoint}");
                Task.Run(() => ClientSession(client));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    static void LoadRegions()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "regions.txt");
        foreach (string line in File.ReadAllLines(path))
        {
            string[] parts = line.Split('|');
            if (parts.Length == 2)
                regions[parts[0].Trim().ToUpper()] = parts[1].Trim();
        }
        Console.WriteLine($"Loaded {regions.Count} regions from database");
    }

    static void ClientSession(TcpClient client)
    {
        try
        {
            NetworkStream ns = client.GetStream();
            StreamReader sr = new StreamReader(ns);
            StreamWriter sw = new StreamWriter(ns);

            while (true)
            {
                string? request = sr.ReadLine();
                if (request == null || request == "$<close>")
                    break;

                Console.WriteLine($"Request from {client.Client.RemoteEndPoint} : {request}");
                string answer = GetAnswer(request);

                sw.WriteLine(answer);
                sw.Flush();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        Console.WriteLine("Client disconnected");
        client.Close();
    }

    static string GetAnswer(string request)
    {
        if (request.StartsWith("CODE:"))
        {
            string code = request.Substring(5).Trim().ToUpper();
            if (regions.ContainsKey(code))
                return regions[code];
            return "Region not found";
        }

        if (request.StartsWith("REGION:"))
        {
            string name = request.Substring(7).Trim().ToLower();
            List<string> codes = new List<string>();
            foreach (var item in regions)
            {
                if (item.Value.ToLower().Contains(name))
                    codes.Add($"{item.Key} - {item.Value}");
            }

            if (codes.Count == 0)
                return "Code not found";
            return string.Join("; ", codes);
        }

        return "Unknown request";
    }
}
