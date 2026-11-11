using System.Net;
using System.Net.Sockets;
using Server;
using CommandClasses;
using Newtonsoft.Json;

internal class Program
{
    static Random rnd = new Random();

    static void Main(string[] args)
    {
        const int port = 8080;
        IPAddress address = IPAddress.Parse("127.0.0.1");

        TcpListener server = new TcpListener(address, port);

        bool hasPlayer = false;
        PlayerInfo player1 = null;

        server.Start();
        Console.WriteLine("Tic Tac Toe server started");

        while (true)
        {
            try
            {
                TcpClient client = server.AcceptTcpClient();

                StreamReader sr = new StreamReader(client.GetStream());
                string resp = sr.ReadLine()!;
                var command = JsonConvert.DeserializeObject<ClientCommand>(resp)!;

                switch (command.Type)
                {
                    case CommandType.START:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Request to start a new game from {command.Nickname} (vs computer : {command.VsComputer})");

                        PlayerInfo player = new PlayerInfo()
                        {
                            Nickname = command.Nickname,
                            TcpClient = client,
                            Reader = sr
                        };

                        if (command.VsComputer)
                        {
                            player.IsX = true;
                            Game game = new Game();
                            player.SendStartCommand("Computer", true);
                            Task.Run(() => player.StartSession(null, game));
                        }
                        else if (!hasPlayer)
                        {
                            player.IsX = Convert.ToBoolean(rnd.Next(2));
                            player1 = player;
                            player1.SendWaitCommand();
                            hasPlayer = true;
                        }
                        else
                        {
                            player.IsX = !player1.IsX;
                            PlayerInfo first = player1;
                            Game game = new Game();

                            first.SendStartCommand(player.Nickname, false);
                            player.SendStartCommand(first.Nickname, false);

                            Task.Run(() => first.StartSession(player, game));
                            Task.Run(() => player.StartSession(first, game));

                            hasPlayer = false;
                        }
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine($"Unknown command from {command.Nickname}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
