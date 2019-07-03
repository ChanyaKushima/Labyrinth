using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace WinForms1
{
    public partial class Form1 : Form
    {
        private enum Direction
        {
            None,
            Up,
            Down,
            Right,
            Left,
        }

        static readonly IReadOnlyCollection<Direction> EveryDirections = 
                new[] { Direction.Down, Direction.Left, Direction.Right, Direction.Up };

        const int PieceSize = 30;
        const int MapWidth = 33;
        const int MapHeight = 33;

        const int GoalX = MapWidth - 2;
        const int GoalY = MapHeight - 2;

        private Piece[,] Map=new Piece[MapWidth,MapHeight];

        private Random rand = new Random();

        private int m_x = 1;
        private int m_y = 1;

        bool GameClear = false;

        public Form1()
        {
            InitializeComponent();
            ClientSize = new Size(MapWidth * PieceSize, MapHeight * PieceSize);
            DoubleBuffered = true;
            Dig(MapWidth, MapHeight,Map);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            Brush wallBrush = Brushes.Black;
            Brush roadBrush = Brushes.White;
            Brush myBrush = Brushes.SkyBlue;
            Brush goalBrush = Brushes.Gold;
            const int mySize = 20;
            const int goalSize = 20;
            const float myMargin = (PieceSize - mySize) / 2.0f;
            const float goalMargin = (PieceSize - goalSize) / 2.0f;


            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    Brush brush = Map[x, y].IsWall ? wallBrush : roadBrush;
                    g.FillRectangle(brush, x * PieceSize, y * PieceSize, PieceSize, PieceSize);
                }
            }

            g.FillEllipse(myBrush, m_x * PieceSize + myMargin, m_y * PieceSize + myMargin, mySize, mySize);
            g.FillEllipse(goalBrush, GoalX * PieceSize + goalMargin, GoalY * PieceSize + goalMargin, goalSize, goalSize);

            if (GameClear)
            {
                var fontSize = Math.Min(ClientSize.Width / 5, ClientSize.Height / 2);
                var format = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var center = new PointF(ClientSize.Width / 2.0f, ClientSize.Height / 2.0f);
                var font = new Font(FontFamily.GenericMonospace, fontSize);
                var backScreen = new SolidBrush(Color.FromArgb(50, Color.Black));

                g.FillRectangle(backScreen, new Rectangle(new Point(), ClientSize));
                g.DrawString("Goal!", font, Brushes.Red, center, format);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            Keys key = e.KeyCode;

            if (GameClear)
            {
                if (key == Keys.Enter)
                {
                    GameLoad();
                }
            }
            else
            {
                if (key == Keys.Up)
                {
                    MoveMe(0, -1);
                }
                else if (key == Keys.Down)
                {
                    MoveMe(0, 1);
                }
                else if (key == Keys.Right)
                {
                    MoveMe(1, 0);

                }
                else if (key == Keys.Left)
                {
                    MoveMe(-1, 0);
                }

                if ((m_x, m_y) == (GoalX, GoalY))
                {
                    GameClear = true;
                }
            }

            Refresh();
        }

        private bool MoveMe(int dx, int dy)
        {
            try
            {
                if (Map[m_x + dx, m_y + dy].IsRoad)
                {
                    m_x += dx;
                    m_y += dy;
                }
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private static void Dig(int width, int heigth, Piece[,] map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }
            if (width % 2 == 0 || heigth % 2 == 0 || width < 5 || heigth < 5)
            {
                throw new ArgumentException("引数は5以上の奇数を入力してください");
            }
            if (map.GetLength(0) < width || map.GetLength(1) < heigth)
            {
                throw new ArgumentException("Mapが生成されていません");
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < heigth; y++)
                {
                    map[x, y] = new Piece();
                }
            }

            List<Direction> movableDirections = new List<Direction>(4);
            Random rand = new Random();

            const int startX = 1;
            const int startY = 1;

            int nowX = startX;
            int nowY = startY;

            List<(int X, int Y)> digablePoints = new List<(int X, int Y)>();

            int count = 0;

            map[nowX, nowY].PieceParameter = PieceParameter.Road;

            while (true)
            {
                while (true)
                {
                    foreach (var d in EveryDirections)
                    {
                        if (CanDig2Piece(nowX, nowY, map, d))
                        {
                            movableDirections.Add(d);
                        }
                    }

                    count = movableDirections.Count;

                    if (count == 0)
                    {
                        break;
                    }

                    Dig2Piece(ref nowX, ref nowY, map, movableDirections[rand.Next(count)]);

                    movableDirections.Clear();
                }

                for (int x = 1; x < width; x += 2)
                {
                    for (int y = 1; y < heigth; y += 2)
                    {
                        if (CanDigSomeDirection(x, y, map))
                        {
                            digablePoints.Add((x, y));
                        }
                    }
                }

                count = digablePoints.Count;

                if (count == 0)
                {
                    break;
                }

                (nowX, nowY) = digablePoints[rand.Next(count)];

                digablePoints.Clear();
            }
        }

        private static void Dig2Piece(ref int nowX, ref int nowY,Piece[,] map, Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    map[nowX, nowY - 1].PieceParameter = PieceParameter.Road;
                    map[nowX, nowY - 2].PieceParameter = PieceParameter.Road;
                    nowY -= 2;
                    break;
                case Direction.Down:
                    map[nowX, nowY + 1].PieceParameter = PieceParameter.Road;
                    map[nowX, nowY + 2].PieceParameter = PieceParameter.Road;
                    nowY += 2;
                    break;
                case Direction.Right:
                    map[nowX + 1, nowY].PieceParameter = PieceParameter.Road;
                    map[nowX + 2, nowY].PieceParameter = PieceParameter.Road;
                    nowX += 2;
                    break;
                case Direction.Left:
                    map[nowX - 1, nowY].PieceParameter = PieceParameter.Road;
                    map[nowX - 2, nowY].PieceParameter = PieceParameter.Road;
                    nowX -= 2;
                    break;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanDig2Piece(int nowX, int nowY,Piece[,] map, Direction direction)
        {
            try
            {
                switch (direction)
                {
                    case Direction.Up:
                        return map[nowX, nowY - 0].IsRoad &&
                               map[nowX, nowY - 1].IsWall &&
                               map[nowX, nowY - 2].IsWall &&
                               map[nowX, nowY - 3].IsWall;
                    case Direction.Down:
                        return map[nowX, nowY + 0].IsRoad &&
                               map[nowX, nowY + 1].IsWall &&
                               map[nowX, nowY + 2].IsWall &&
                               map[nowX, nowY + 3].IsWall;
                    case Direction.Right:
                        return map[nowX + 0, nowY].IsRoad &&
                               map[nowX + 1, nowY].IsWall &&
                               map[nowX + 2, nowY].IsWall &&
                               map[nowX + 3, nowY].IsWall;
                    case Direction.Left:
                        return map[nowX - 0, nowY].IsRoad &&
                               map[nowX - 1, nowY].IsWall &&
                               map[nowX - 2, nowY].IsWall &&
                               map[nowX - 3, nowY].IsWall;
                }
            }
            catch (Exception) { }

            return false;
        }
        private static bool CanDigSomeDirection(int nowX, int nowY, Piece[,] map)
        {
            return CanDig2Piece(nowX, nowY, map, Direction.Up) || CanDig2Piece(nowX, nowY, map, Direction.Down) ||
                CanDig2Piece(nowX, nowY, map, Direction.Left) || CanDig2Piece(nowX, nowY, map, Direction.Right);
        }

        private void GameLoad()
        {
            Dig(MapWidth, MapHeight, Map);
            (m_x, m_y) = (1, 1);
            GameClear = false;
        }
    }

    [DebuggerDisplay("IsRoad = {_isRoad}")]
    public struct Piece
    {
        bool _isRoad;

        public bool IsRoad => _isRoad;
        public bool IsWall => !_isRoad;

        public PieceParameter PieceParameter
        {
            get
            {
                if (_isRoad)
                {
                    return PieceParameter.Road;
                }
                return PieceParameter.Wall;
            }
            set
            {
                _isRoad = value == PieceParameter.Road;
            }
        }

        public Piece(PieceParameter pieceParam)
        {
            _isRoad = pieceParam == PieceParameter.Road;
        }
    }

    public enum PieceParameter
    {
        Wall,
        Road,
    }
}
