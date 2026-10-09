using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    public enum LevelDifficulty { Normal, Hard, SuperHard }

    /// <summary>Loại vật thể trên bàn chơi (lưới cube).</summary>
    public enum BoardItemKind
    {
        Cube,          // cube nhỏ 1 ô, có màu
        CombinedCube,  // khối lớn nhiều ô dùng chung máu (màu-hp-groupId)
        Crate,         // hòm gỗ: bi màu nào cũng trừ máu, vỡ thì lộ vật bên trong
        Wall,          // vật cản tĩnh, không phá được (vật cản chưa hỗ trợ)
        Column         // cột nhà "/wl-hp-id": bi màu nào cũng trừ máu (không tiêu hao bi), hết máu thì vỡ vụn
    }

    /// <summary>Một vật thể trên bàn, toạ độ theo lưới "mịn" (BoardCols x BoardRows, hàng 0 = đáy).</summary>
    public class BoardItemData
    {
        public BoardItemKind Kind;
        public string ColorCode;            // null với Crate/Wall
        public int Hp;
        public RectInt Cells;               // x = cột trái, y = hàng đáy, w/h = số ô
        /// <summary>Khối gộp hình bất kỳ (chữ L…): các ô chiếm, tính từ Cells.min. null = phủ kín Cells.</summary>
        public List<Vector2Int> Mask;
        public List<BoardItemData> Contents = new List<BoardItemData>(); // vật nằm trong Crate
    }

    public enum AmmoCellKind { Empty, Wall, Box, Tunnel }

    public class AmmoBoxData
    {
        public string ColorCode;
        public int Balls;
        public int IceHits;     // > 0: bị đóng băng, cần N lượt bấm hộp khác để vỡ
        public bool Hidden;     // hộp "?" – lộ màu khi thông đường ra
        /// <summary>Cửa kính "/door-N" (BlockerType.AmmoCover): 0 = không có, 1 = bắt đầu ĐÓNG, 2 = bắt đầu MỞ.
        /// Gốc BlockerCover.OnAmmoUsed: mỗi lần bắn 1 hộp, mọi cửa đảo trạng thái.</summary>
        public int Door;
        public bool DoorClosed;
    }

    public class AmmoTunnelData
    {
        public char Id;
        public Vector2Int Direction;                 // hướng nhả hộp (trong toạ độ lưới đạn, y tăng xuống dưới)
        public List<AmmoBoxData> Queue = new List<AmmoBoxData>();
    }

    public class AmmoCellData
    {
        public AmmoCellKind Kind;
        /// <summary>Ô "/awl": khối tường tối nổi trong khay (khác ô rỗng = ngoài khay, không vẽ gì).</summary>
        public bool SolidWall;
        public AmmoBoxData Box;
        public AmmoTunnelData Tunnel;
    }

    /// <summary>Dữ liệu 1 level sau khi parse CSV.</summary>
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

        // Lưới hộp đạn: [cột, hàng], hàng 0 = hàng trên cùng (sát lối ra)
        public int AmmoCols;
        public int AmmoRows;
        public AmmoCellData[,] AmmoCells;

        public int TotalBalls;
        public readonly Dictionary<string, int> BallsByColor = new Dictionary<string, int>();
        public readonly Dictionary<string, int> HpByColor = new Dictionary<string, int>();
    }
}
