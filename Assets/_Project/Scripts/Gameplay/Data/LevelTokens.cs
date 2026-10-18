using System.Collections.Generic;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Một level ở dạng "lưới mã" trung gian (đúng các chuỗi mã trong CSV gốc, vd "r-1", "y-30-2/cr-50-1", "p-10/ice-2", "/tu-AL").
    /// Cả CSV gốc lẫn <see cref="LevelData"/> (level tự làm) đều đổi về dạng này rồi dùng CHUNG <see cref="LevelParser.Build"/>
    /// để dựng <see cref="LevelModel"/> → luật đọc level chỉ có một chỗ, hai nguồn không thể lệch nhau.
    /// Định dạng mã: Docs/tham-khao/csv-format.md.
    /// </summary>
    public class LevelTokens
    {
        public int Id;
        public string HardLevel = "-";          // "-" / "Hard" / "SHard"
        public int Capacity = 300;
        public int BoardCols = 20;              // kích thước bàn THẬT (sau khi nhân Multiplier), như cột BoardSize
        public int BoardRows = 32;
        public bool IsFigurative;
        public int Multiplier = 1;

        /// <summary>Ô bàn theo ô thiết kế: [cột, dòng], dòng 0 = dòng TRÊN CÙNG (như thứ tự dòng CSV). Số dòng quyết định vị trí đáy bàn.</summary>
        public string[,] Board = new string[0, 0];
        /// <summary>Ô khay đạn: [cột, hàng], hàng 0 = hàng sát lối ra.</summary>
        public string[,] Ammo = new string[0, 0];
        /// <summary>Hàng chờ của từng tunnel (theo chữ cái id), mỗi phần tử là mã hộp "màu-số/...".</summary>
        public Dictionary<char, List<string>> Tunnels = new Dictionary<char, List<string>>();

        public int BoardDesignCols => Board.GetLength(0);
        public int BoardLines => Board.GetLength(1);
        public int AmmoCols => Ammo.GetLength(0);
        public int AmmoRows => Ammo.GetLength(1);
    }
}
