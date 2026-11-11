using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CommandClasses;
using Newtonsoft.Json;

namespace Client;

public partial class MainWindow : Window
{
    TcpClient client;
    StreamReader reader;

    public bool IsX { get; set; }
    public char Symbol => IsX ? 'X' : 'O';
    public char OpponentSymbol => IsX ? 'O' : 'X';
    public string Nickname { get; set; }
    public string OpponentNickname { get; set; }
    public bool IsPlaying { get; set; }
    private bool isMoving = false;
    public bool IsMoving
    {
        get { return isMoving; }
        set
        {
            isMoving = value;

            var brush = isMoving ? Brushes.White : Brushes.Gray;
            foreach (Border item in fieldGrid.Children.OfType<Border>())
            {
                item.Background = brush;
            }
            drawBtn.IsEnabled = isMoving;
            surrenderBtn.IsEnabled = isMoving;
            if (IsPlaying && OpponentNickname != null)
                statusLabel.Content = isMoving ? "Your move" : "Opponent's move";
        }
    }

    public MainWindow()
    {
        InitializeComponent();

        IsPlaying = false;
        IsMoving = false;
    }

    private Task SendCommand(ClientCommand command)
    {
        return Task.Run(() =>
        {
            StreamWriter writer = new StreamWriter(client.GetStream());
            writer.WriteLine(JsonConvert.SerializeObject(command));
            writer.Flush();
        });
    }

    private Task<ServerCommand> ReceiveCommand()
    {
        return Task.Run(() =>
        {
            string? resp = reader.ReadLine();
            if (resp == null)
                return new ServerCommand(CommandType.CLOSE);
            return JsonConvert.DeserializeObject<ServerCommand>(resp)!;
        });
    }

    private async void Listen()
    {
        try
        {
            bool isExit = false;
            while (!isExit)
            {
                ServerCommand command = await ReceiveCommand();

                switch (command.Type)
                {
                    case CommandType.WAIT:
                        opponentNameTxtBox.Content = "Waiting...";
                        statusLabel.Content = "Waiting for opponent...";
                        break;
                    case CommandType.START:
                        IsX = command.IsX;
                        symbolLabel.Content = Symbol;
                        OpponentNickname = command.OpponentName;
                        opponentNameTxtBox.Content = OpponentNickname;
                        modeLabel.Content = command.VsComputer ? "Human - Computer" : "Human - Human";
                        IsMoving = IsX;
                        break;
                    case CommandType.MOVE:
                        foreach (Border item in fieldGrid.Children.OfType<Border>())
                        {
                            if (item.Tag.Equals(command.MoveCoord))
                            {
                                ((TextBlock)item.Child).Text = OpponentSymbol.ToString();
                            }
                        }
                        IsMoving = true;
                        break;
                    case CommandType.OFFER_DRAW:
                        var answer = MessageBox.Show($"{OpponentNickname} offers a draw. Do you agree?", "Draw", MessageBoxButton.YesNo);
                        await SendCommand(new ClientCommand(CommandType.DRAW_ANSWER, Nickname) { Answer = answer == MessageBoxResult.Yes });
                        break;
                    case CommandType.DRAW_ANSWER:
                        MessageBox.Show($"{OpponentNickname} declined the draw");
                        break;
                    case CommandType.WIN:
                    case CommandType.LOSE:
                    case CommandType.DRAW:
                        IsMoving = false;
                        string result = command.Type == CommandType.WIN ? "You won!" : command.Type == CommandType.LOSE ? "You lost!" : "Draw!";
                        statusLabel.Content = result;
                        MessageBox.Show(result, "Game over");
                        CloseSession();
                        isExit = true;
                        break;
                    case CommandType.CLOSE:
                        MessageBox.Show("Opponent left the game");
                        CloseSession();
                        isExit = true;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!IsPlaying)
            {
                ClearField();

                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ipTxtBox.Text), int.Parse(portTxtBox.Text));
                client = new TcpClient();
                client.Connect(serverEndPoint);
                reader = new StreamReader(client.GetStream());
                Nickname = nameTxtBox.Text;

                await SendCommand(new ClientCommand(CommandType.START, Nickname) { VsComputer = computerRadio.IsChecked == true });
                IsPlaying = true;
                Listen();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private async void CloseSession()
    {
        try
        {
            await SendCommand(new ClientCommand(CommandType.CLOSE, Nickname));
        }
        catch (Exception)
        { }

        client.Close();
        client = null;

        IsPlaying = false;
        IsMoving = false;
        OpponentNickname = null;
        symbolLabel.Content = "-";
        opponentNameTxtBox.Content = "-";
        modeLabel.Content = "-";
    }

    private void ClearField()
    {
        foreach (Border item in fieldGrid.Children.OfType<Border>())
        {
            ((TextBlock)item.Child).Text = string.Empty;
        }
        statusLabel.Content = "";
    }

    async private void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsMoving)
        {
            TextBlock textBlock = (TextBlock)((Border)sender).Child;
            if (textBlock.Text != string.Empty)
                return;

            textBlock.Text = Symbol.ToString();
            IsMoving = false;

            ClientCommand command = new ClientCommand(CommandType.MOVE, Nickname)
            {
                MoveCoord = (CellCoord)((Border)sender).Tag
            };
            await SendCommand(command);
        }
    }

    private async void DrawButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsMoving)
            await SendCommand(new ClientCommand(CommandType.OFFER_DRAW, Nickname));
    }

    private async void SurrenderButton_Click(object sender, RoutedEventArgs e)
    {
        if (IsMoving && MessageBox.Show("Are you sure?", "Surrender", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            await SendCommand(new ClientCommand(CommandType.SURRENDER, Nickname));
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        if (IsPlaying)
            SendCommand(new ClientCommand(CommandType.EXIT, Nickname));
    }
}
