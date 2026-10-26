using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Dùng cho độ khó của level</summary>
    public enum LevelDifficulty { Normal, Hard, SuperHard }

    /// <summary>Loại vật thể trên bàn chơi</summary>
    public enum BoardItemKind
    {
        Cube,
        CombinedCube,
        Crate,
        Wall,
        Column,
        Stone
    }

    /// <summary>Một vật thể trên bàn, toạ độ theo lưới "mịn"</summary>
    public class BoardItemData
    {
        public BoardItemKind Kind;
        public string ColorCode;
        public int Hp;
        public RectInt Cells;
        /// <summary>Khối gộp hình bất kỳ (chữ L…)</summary>
        public List<Vector2Int> Mask;
        public List<BoardItemData> Contents = new List<BoardItemData>();
    }

    /// <summary>Dùng cho loại ô trong khay đạn</summary>
    public enum AmmoCellKind { Empty, Wall, Box, Tunnel }

    /// <summary>Dùng cho dữ liệu hộp đạn của level</summary>
    public class AmmoBoxData
    {
        public string ColorCode;
        public int Balls;
        public int IceHits;
        public bool Hidden;
        /// <summary>Cửa kính "/door-N" (BlockerType.AmmoCover)</summary>
        public int Door;
        public bool DoorClosed;
    }

    /// <summary>Dùng cho dữ liệu ống nhả hộp của level</summary>
    public class AmmoTunnelData
    {
        public char Id;
        public Vector2Int Direction;
        public List<AmmoBoxData> Queue = new List<AmmoBoxData>();
    }

    /// <summary>Dùng cho dữ liệu một ô khay của level</summary>
    public class AmmoCellData
    {
        public AmmoCellKind Kind;
        /// <summary>Ô "/awl": khối tường tối nổi trong khay</summary>
        public bool SolidWall;
        public AmmoBoxData Box;
        public AmmoTunnelData Tunnel;
    }

    /// <summary>Dữ liệu 1 level sau khi parse CSV</summary>
    public class LevelModel
    {
        public int Id;
        public LevelDifficulty Difficulty;
        public int Capacity;
        public int BoardCols = 20;
        public int BoardRows = 32;
        public int Multiplier = 1;
        public bool IsFigurative;

        public readonly List<BoardItemData> BoardItems = new List<BoardItemData>();

        public int AmmoCols;
        public int AmmoRows;
        public AmmoCellData[,] AmmoCells;

        public int TotalBalls;
        public readonly Dictionary<string, int> BallsByColor = new Dictionary<string, int>();
        public readonly Dictionary<string, int> HpByColor = new Dictionary<string, int>();
    }
}
