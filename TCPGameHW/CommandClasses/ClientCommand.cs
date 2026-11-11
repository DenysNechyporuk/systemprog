namespace CommandClasses
{
    [Serializable]
    public enum CommandType
    {
        WAIT,
        START,
        MOVE,
        CLOSE,
        EXIT,
        WIN,
        LOSE,
        DRAW,
        OFFER_DRAW,
        DRAW_ANSWER,
        SURRENDER
    }

    [Serializable]
    public struct CellCoord
    {
        public int RowIndex { get; set; }
        public int ColumnIndex { get; set; }

        public override string ToString()
        {
            return $"{RowIndex}:{ColumnIndex}";
        }
    }

    [Serializable]
    public class ClientCommand
    {
        public CommandType Type { get; set; }
        public string Nickname { get; set; }
        public CellCoord MoveCoord { get; set; }
        public bool VsComputer { get; set; }
        public bool Answer { get; set; }

        public ClientCommand(CommandType type, string nick)
        {
            this.Type = type;
            this.Nickname = nick;
        }
    }

    [Serializable]
    public class ServerCommand
    {
        public CommandType Type { get; set; }
        public bool IsX { get; set; }
        public string OpponentName { get; set; }
        public CellCoord MoveCoord { get; set; }
        public bool VsComputer { get; set; }
        public bool Answer { get; set; }

        public ServerCommand(CommandType type)
        {
            this.Type = type;
        }
    }
}
