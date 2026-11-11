namespace ClientRequest
{
    public enum RequestCommand
    {
        Join, Leave, Message, Joined, ChatFull, ServerClosed
    }

    public class ClientCommand
    {
        public RequestCommand Command { get; set; }
        public string Name { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }

        public ClientCommand()
        { }

        public ClientCommand(RequestCommand command)
        {
            Command = command;
            Time = DateTime.Now;
        }

        public ClientCommand(string name, string message, RequestCommand command = RequestCommand.Message)
        {
            Name = name;
            Message = message;
            Command = command;
            Time = DateTime.Now;
        }

        public override string ToString()
        {
            return $"[{Time.ToShortTimeString()}] {Name} : {Message}";
        }
    }
}
