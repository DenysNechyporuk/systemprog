using System.Net;
using System.Net.Sockets;
using System.Text;

internal class Program
{
    static string address = "127.0.0.1";
    static int port = 5050;

    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        TcpClient client = new TcpClient();
        try
        {
            client.Connect(new IPEndPoint(IPAddress.Parse(address), port));
            Console.WriteLine("Connected to server!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Can't connect to server : " + ex.Message);
            return;
        }

        NetworkStream ns = client.GetStream();
        StreamReader sr = new StreamReader(ns);
        StreamWriter sw = new StreamWriter(ns);

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1 - Find region by code");
            Console.WriteLine("2 - Find code by region");
            Console.WriteLine("0 - Exit");
            Console.Write("Enter choice :: ");
            string? choice = Console.ReadLine();

            string request;
            if (choice == "1")
            {
                Console.Write("Enter code :: ");
                request = "CODE:" + Console.ReadLine();
            }
            else if (choice == "2")
            {
                Console.Write("Enter region :: ");
                request = "REGION:" + Console.ReadLine();
            }
            else if (choice == "0")
            {
                sw.WriteLine("$<close>");
                sw.Flush();
                break;
            }
            else
            {
                Console.WriteLine("Wrong choice");
                continue;
            }

            try
            {
                sw.WriteLine(request);
                sw.Flush();
                string? answer = sr.ReadLine();
                Console.WriteLine("Result : " + answer);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                break;
            }
        }

        client.Close();
    }
}
