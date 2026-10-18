using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Tiến độ người chơi lưu trên máy (PlayerPrefs): level đang chơi và số xu. Dùng chung cho Menu và Gameplay
    /// để hai scene nói chuyện với nhau qua một chỗ duy nhất (không rải chuỗi khoá khắp nơi).
    /// Âm thanh / nhạc / booster có khoá riêng trong AudioManager / BoosterSystem.
    /// </summary>
    public static class PlayerData
    {
        const string BaseLevelKey = "bb_level";
        static string _levelKey = BaseLevelKey;

        /// <summary>Tiến độ level lưu riêng theo bộ level (bộ gốc giữ khoá cũ "bb_level" để không mất tiến độ đã có).</summary>
        public static void UseLevelSet(string setName) =>
            _levelKey = string.IsNullOrEmpty(setName) || setName == "original" ? BaseLevelKey : BaseLevelKey + "_" + setName;
        const string CoinsKey = "bb_coins";

        /// <summary>Số tim tối đa hiển thị ở Menu (game gốc có hệ thống tim – bản dựng lại chưa làm nên luôn đầy).</summary>
        public const int MaxHearts = 5;

        /// <summary>Level kế tiếp sẽ chơi (≥ 1).</summary>
        public static int Level
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(_levelKey, 1));
            set => PlayerPrefs.SetInt(_levelKey, Mathf.Max(1, value));
        }

        public static int Coins
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 0));
            set => PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, value));
        }
    }
}
