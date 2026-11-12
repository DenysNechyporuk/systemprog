using CommandClasses;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace Server
{
    public class PlayerInfo
    {
        public TcpClient TcpClient { get; set; }
        public StreamReader Reader { get; set; }
        public string Nickname { get; set; }
        public bool IsX { get; set; }
        public char Symbol => IsX ? 'X' : 'O';
        public char OpponentSymbol => IsX ? 'O' : 'X';

        static Random rnd = new Random();

        private void SendCommand(ServerCommand command)
        {
            lock (this)
            {
                StreamWriter writer = new StreamWriter(TcpClient.GetStream());
                writer.WriteLine(JsonConvert.SerializeObject(command));
                writer.Flush();
            }
        }

        public void SendCloseCommand()
        {
            SendCommand(new ServerCommand(CommandType.CLOSE));
        }
        public void SendWaitCommand()
        {
            SendCommand(new ServerCommand(CommandType.WAIT));
        }
        public void SendStartCommand(string opponentName, bool vsComputer)
        {
            ServerCommand command = new ServerCommand(CommandType.START)
            {
                IsX = this.IsX,
                OpponentName = opponentName,
                VsComputer = vsComputer
            };

            SendCommand(command);
        }
        public void SendMoveCommand(CellCoord moveCoord)
        {
            ServerCommand command = new ServerCommand(CommandType.MOVE)
            {
                MoveCoord = moveCoord
            };

            SendCommand(command);
        }
        public void SendResultCommand(CommandType result)
        {
            SendCommand(new ServerCommand(result));
        }
        public void SendOfferDrawCommand()
        {
            SendCommand(new ServerCommand(CommandType.OFFER_DRAW));
        }
        public void SendDrawAnswerCommand(bool answer)
        {
            SendCommand(new ServerCommand(CommandType.DRAW_ANSWER) { Answer = answer });
        }

        public ClientCommand ReceiveCommand()
        {
            string? resp = Reader.ReadLine();
            if (resp == null)
                return new ClientCommand(CommandType.EXIT, Nickname);
            return JsonConvert.DeserializeObject<ClientCommand>(resp)!;
        }

        private void SendResults(Game game, PlayerInfo? opponent, char winner)
        {
            game.IsOver = true;
            Console.ForegroundColor = ConsoleColor.Yellow;

            if (winner == 'D')
            {
                Console.WriteLine($"Draw in game {Nickname}");
                SendResultCommand(CommandType.DRAW);
                opponent?.SendResultCommand(CommandType.DRAW);
            }
            else if (winner == Symbol)
            {
                Console.WriteLine($"{Nickname} won");
                SendResultCommand(CommandType.WIN);
                opponent?.SendResultCommand(CommandType.LOSE);
            }
            else
            {
                Console.WriteLine($"{Nickname} lost");
                SendResultCommand(CommandType.LOSE);
                opponent?.SendResultCommand(CommandType.WIN);
            }
        }

        public void StartSession(PlayerInfo? opponent, Game game)
        {
            bool isExit = false;
            while (!isExit)
            {
                try
                {
                    ClientCommand command = ReceiveCommand();

                    switch (command.Type)
                    {
                        case CommandType.MOVE:
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine($"Move on {command.MoveCoord} from {command.Nickname}");
                            lock (game)
                            {
                                if (game.IsOver || !game.SetMove(command.MoveCoord, Symbol))
                                    break;

                                opponent?.SendMoveCommand(command.MoveCoord);

                                char winner = game.CheckWinner();
                                if (winner != ' ')
                                {
                                    SendResults(game, opponent, winner);
                                    break;
                                }

                                if (opponent == null)
                                {
                                    Thread.Sleep(500);
                                    CellCoord coord = game.ComputerMove(OpponentSymbol);
                                    SendMoveCommand(coord);
                                    winner = game.CheckWinner();
                                    if (winner != ' ')
                                        SendResults(game, opponent, winner);
                                }
                            }
                            break;
                        case CommandType.OFFER_DRAW:
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"{command.Nickname} offers a draw");
                            if (opponent == null)
                            {
                                if (rnd.Next(2) == 0)
                                    SendResults(game, null, 'D');
                                else
                                    SendDrawAnswerCommand(false);
                            }
                            else
                            {
                                opponent.SendOfferDrawCommand();
                            }
                            break;
                        case CommandType.DRAW_ANSWER:
                            if (opponent == null)
                                break;
                            if (command.Answer)
                                SendResults(game, opponent, 'D');
                            else
                                opponent.SendDrawAnswerCommand(false);
                            break;
                        case CommandType.SURRENDER:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"{command.Nickname} surrendered");
                            SendResults(game, opponent, OpponentSymbol);
                            break;
                        case CommandType.CLOSE:
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            Console.WriteLine($"Close command from {command.Nickname}");
                            isExit = true;
                            break;
                        case CommandType.EXIT:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Exit command from {Nickname}");
                            if (!game.IsOver)
                            {
                                game.IsOver = true;
                                opponent?.SendCloseCommand();
                            }
                            isExit = true;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    isExit = true;
                }
            }

            TcpClient.Close();
        }
    }
}
