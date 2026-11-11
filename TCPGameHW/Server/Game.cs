using CommandClasses;

namespace Server
{
    public class Game
    {
        public char[,] Board { get; set; } = new char[3, 3];
        public bool IsOver { get; set; } = false;
        static Random rnd = new Random();

        public bool SetMove(CellCoord coord, char symbol)
        {
            if (Board[coord.RowIndex, coord.ColumnIndex] != '\0')
                return false;
            Board[coord.RowIndex, coord.ColumnIndex] = symbol;
            return true;
        }

        public CellCoord ComputerMove(char symbol)
        {
            List<CellCoord> free = new List<CellCoord>();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (Board[i, j] == '\0')
                        free.Add(new CellCoord() { RowIndex = i, ColumnIndex = j });
                }
            }

            CellCoord coord = free[rnd.Next(free.Count)];
            SetMove(coord, symbol);
            return coord;
        }

        public char CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                if (Board[i, 0] != '\0' && Board[i, 0] == Board[i, 1] && Board[i, 1] == Board[i, 2])
                    return Board[i, 0];
                if (Board[0, i] != '\0' && Board[0, i] == Board[1, i] && Board[1, i] == Board[2, i])
                    return Board[0, i];
            }

            if (Board[1, 1] != '\0' && Board[0, 0] == Board[1, 1] && Board[1, 1] == Board[2, 2])
                return Board[1, 1];
            if (Board[1, 1] != '\0' && Board[0, 2] == Board[1, 1] && Board[1, 1] == Board[2, 0])
                return Board[1, 1];

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (Board[i, j] == '\0')
                        return ' ';
                }
            }
            return 'D';
        }
    }
}
