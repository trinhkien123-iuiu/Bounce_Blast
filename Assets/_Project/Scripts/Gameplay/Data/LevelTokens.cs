using System.Collections.Generic;

namespace BounceBlast.Gameplay
{
    /// <summary>Một level ở dạng "lưới mã" trung gian</summary>
    public class LevelTokens
    {
        public int Id;
        public string HardLevel = "-";
        public int Capacity = 300;
        public int BoardCols = 20;
        public int BoardRows = 32;
        public bool IsFigurative;
        public int Multiplier = 1;

        /// <summary>Ô bàn theo ô thiết kế: [cột, dòng], dòng 0 = dòng TRÊN CÙNG</summary>
        public string[,] Board = new string[0, 0];
        /// <summary>Ô khay đạn: [cột, hàng], hàng 0 = hàng sát lối ra</summary>
        public string[,] Ammo = new string[0, 0];
        /// <summary>Hàng chờ của từng tunnel (theo chữ cái id), mỗi phần tử là mã hộp "màu-số/..."</summary>
        public Dictionary<char, List<string>> Tunnels = new Dictionary<char, List<string>>();
        /// <summary>Hàng đợi của tunnel trên bàn (/btu-X), mỗi phần tử "màu-số" (số × Multiplier² cube)</summary>
        public Dictionary<char, List<string>> BoardTunnels = new Dictionary<char, List<string>>();

        public int BoardDesignCols => Board.GetLength(0);
        public int BoardLines => Board.GetLength(1);
        public int AmmoCols => Ammo.GetLength(0);
        public int AmmoRows => Ammo.GetLength(1);
    }
}
