using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;

using System.Windows.Input;
using static System.Windows.Input.Keyboard;

namespace WinForms1
{

    using System.Windows.Forms;
    public partial class Form1 : Form
    {
        private enum Direction
        {
            Up,
            Down,
            Right,
            Left,
        }

        static readonly IReadOnlyList<Direction> EveryDirections =
                new [] { Direction.Down, Direction.Left, Direction.Right, Direction.Up };
        static readonly StringFormat FormatCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        static readonly Brush BackScreen = new SolidBrush(Color.FromArgb(50, Color.Black));
        static readonly PointF CenterPosition = new PointF(MapDisplayWidth / 2.0f, MapDisplayHeight / 2.0f);
        static readonly string DefaultTitle = "Labyrinth";
        static readonly Size MapDisplaySize = new Size(MapDisplayWidth, MapDisplayHeight);
        static readonly Rectangle MapDisplayRect = new Rectangle(default, MapDisplaySize);

        const int PieceSize = 5;
        const int MapWidth = 361;
        const int MapHeight = 201;
        const int MapDisplayWidth = MapWidth * PieceSize;
        const int MapDisplayHeight = MapHeight * PieceSize;

        const int GoalX = MapWidth - 2;
        const int GoalY = MapHeight - 2;
        Brush wallBrush = Brushes.DeepSkyBlue;
        Brush roadBrush = Brushes.AliceBlue;
        Brush myBrush = Brushes.SkyBlue;
        Brush goalPointBrush = Brushes.Gold;

        private Image MapImage;
        private Piece[,] Map = new Piece[MapWidth, MapHeight];

        private Random rand = new Random();
        private Timer timer = new Timer()
        {
            Enabled = false,
            Interval = 50,
        };

        private int m_x = 1;
        private int m_y = 1;

        bool GameClear = false;

        private static List<(int X, int Y)> GeneralPurposeList = new List<(int X, int Y)>();

        public Form1()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(MapDisplayWidth, MapDisplayHeight);
            DoubleBuffered = true;
            timer.Tick += MoveMeByKey_OnTick;

            DigLabyrinth(Map, true);
            MapImage = GetMapImage(Map, roadBrush, wallBrush);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            const int mySize = ((int)(PieceSize * 0.8) > 1) ? (int)(PieceSize * 0.8) : 1;
            const int goalSize = mySize;
            const float myMargin = (PieceSize - mySize) / 2.0f;
            const float goalMargin = (PieceSize - goalSize) / 2.0f;
            Graphics g = e.Graphics;

            DateTime start = DateTime.Now;

            g.DrawImage(MapImage, new Point());

            g.FillEllipse(myBrush, m_x * PieceSize + myMargin, m_y * PieceSize + myMargin, mySize, mySize);
            g.FillEllipse(goalPointBrush, GoalX * PieceSize + goalMargin, GoalY * PieceSize + goalMargin, goalSize, goalSize);

            if (GameClear)
            {
                var fontSize = Math.Min(ClientSize.Width / 4, ClientSize.Height / 2);
                var font = new Font(FontFamily.GenericSerif, fontSize);

                g.FillRectangle(BackScreen, MapDisplayRect);
                g.DrawString("Goal!", font, Brushes.Red, CenterPosition, FormatCenter);
            }
            DateTime end = DateTime.Now;

            Text = DefaultTitle + " - " + (end - start).TotalMilliseconds.ToString();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            Keys key = e.KeyCode;

            if (GameClear && key == Keys.Enter)
            {
                GameLoad();
                Refresh();
            }

            if (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right)
            {
                timer.Start();
            }
        }


        private void MoveMeByKey_OnTick(object sender, EventArgs e)
        {
            if (!GameClear)
            {
                if (IsKeyDown(Key.Up))
                {
                    MoveMe(0, -1);
                }
                else if (IsKeyDown(Key.Down))
                {
                    MoveMe(0, 1);
                }
                else if (IsKeyDown(Key.Right))
                {
                    MoveMe(1, 0);
                }
                else if (IsKeyDown(Key.Left))
                {
                    MoveMe(-1, 0);
                }
                else
                {
                    timer.Stop();
                    return;
                }
            }
            if (m_x == GoalX && m_y == GoalY)
            {
                GameClear = true;
                timer.Stop();
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

        private static void DigLabyrinth(Piece[,] map, bool mapInited)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            ThrowHelper(width, height, map);

            if (!mapInited)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        map[x, y] = default;
                    }
                }
            }

            Random rand = new Random();

            const int startX = 1;
            const int startY = 1;

            int nowX = startX;
            int nowY = startY;

            List<(int X, int Y)> digablePoints = null;

            int count = 0;

            map[nowX, nowY].State = PieceState.Road;

            while (true)
            {
                Dig1Line(map, ref nowX, ref nowY);

                var preDigablePoints = digablePoints ?? new List<(int X, int Y)>();
                var tmpResult = preDigablePoints.
                    Where((point) => CanDigSomeDirection(point.X, point.Y, map));

                digablePoints = tmpResult.ToList();

                if (digablePoints.Count == 0)
                {
                    digablePoints = GetDigablePoints(width, height, map);
                }

                count = digablePoints.Count;

                if (count == 0)
                {
                    break;
                }

                (nowX, nowY) = digablePoints[rand.Next(count)];
            }
        }

        private static Image GetMapImage(Piece[,] map, Brush roadBrush, Brush wallBrush)
        {
            Image mapImage = new Bitmap(MapDisplayWidth, MapDisplayHeight);
            var g = Graphics.FromImage(mapImage);

            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    Brush brush = map[x, y].IsRoad ? roadBrush : wallBrush;
                    g.FillRectangle(brush, x * PieceSize, y * PieceSize, PieceSize, PieceSize);
                }
            }

            return mapImage;
        }

        private static void ThrowHelper(int width, int heigth, Piece[,] map)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }
            if (width % 2 == 0 || heigth % 2 == 0 || width < 5 || heigth < 5)
            {
                throw new ArgumentException("引数は5以上の奇数を入力してください");
            }
        }

        private static List<(int X, int Y)> GetDigablePoints(int width, int heigth, Piece[,] map)
        {
            GeneralPurposeList.Clear();
            var digablePoints = GeneralPurposeList;

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

            return digablePoints;
        }

        private static void Dig1Line(Piece[,] map, ref int nowX, ref int nowY)
        {
            List<Direction> movableDirections = new List<Direction>(4);
            Random rand = new Random();

            while (true)
            {
                foreach (var d in EveryDirections)
                {
                    if (CanDig2Piece(nowX, nowY, map, d))
                    {
                        movableDirections.Add(d);
                    }
                }

                int count = movableDirections.Count;

                if (count == 0)
                {
                    break;
                }

                Dig2Piece(ref nowX, ref nowY, map, movableDirections[rand.Next(count)]);

                if (nowX == GoalX && nowY == GoalY)
                {
                    break;
                }

                movableDirections.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Dig2Piece(ref int nowX, ref int nowY, Piece[,] map, Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    map[nowX, nowY - 1].State = PieceState.Road;
                    map[nowX, nowY - 2].State = PieceState.Road;
                    nowY -= 2;
                    break;
                case Direction.Down:
                    map[nowX, nowY + 1].State = PieceState.Road;
                    map[nowX, nowY + 2].State = PieceState.Road;
                    nowY += 2;
                    break;
                case Direction.Right:
                    map[nowX + 1, nowY].State = PieceState.Road;
                    map[nowX + 2, nowY].State = PieceState.Road;
                    nowX += 2;
                    break;
                case Direction.Left:
                    map[nowX - 1, nowY].State = PieceState.Road;
                    map[nowX - 2, nowY].State = PieceState.Road;
                    nowX -= 2;
                    break;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanDig2Piece(int nowX, int nowY, Piece[,] map, Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return nowY > 2 && map[nowX, nowY - 0].IsRoad && map[nowX, nowY - 2].IsWall;
                case Direction.Left:
                    return nowX > 2 && map[nowX - 0, nowY].IsRoad && map[nowX - 2, nowY].IsWall;
                case Direction.Down:
                    return nowY < map.GetLength(1) - 3 && map[nowX, nowY + 0].IsRoad && map[nowX, nowY + 2].IsWall;
                case Direction.Right:
                    return nowX < map.GetLength(0) - 3 && map[nowX + 0, nowY].IsRoad && map[nowX + 2, nowY].IsWall;
                default:
                    return false;
            }
        }
        private static bool CanDigSomeDirection(int nowX, int nowY, Piece[,] map)
        {
            return CanDig2Piece(nowX, nowY, map, Direction.Up) || CanDig2Piece(nowX, nowY, map, Direction.Down) ||
                CanDig2Piece(nowX, nowY, map, Direction.Left) || CanDig2Piece(nowX, nowY, map, Direction.Right);
        }



        private void GameLoad()
        {
            DigLabyrinth(Map, false);
            MapImage=GetMapImage(Map, roadBrush, wallBrush);
            (m_x, m_y) = (1, 1);
            GameClear = false;
        }
    }

    [DebuggerDisplay("State = {State}")]
    public struct Piece
    {
        bool _isRoad;

        public bool IsRoad => _isRoad;
        public bool IsWall => !_isRoad;

        public PieceState State
        {
            get
            {
                if (_isRoad)
                {
                    return PieceState.Road;
                }
                return PieceState.Wall;
            }
            set
            {
                _isRoad = value == PieceState.Road;
            }
        }

        public Piece(PieceState pieceParam)
        {
            _isRoad = pieceParam == PieceState.Road;
        }
    }

    public enum PieceState
    {
        Wall,
        Road,
    }
}
