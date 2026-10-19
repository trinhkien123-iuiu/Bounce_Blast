using System;
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

        // ------------------------------------------------------------------ Shop / Bounce Streak
        const string InfiniteHeartsKey = "bb_inf_hearts_until";
        const string NoAdsKey = "bb_no_ads";
        const string WinStreakKey = "bb_win_streak";
        const string StreakStepKey = "bb_streak_step";
        const string FreeCoinKey = "bb_free_coin_at";

        /// <summary>Còn bao lâu tim vô hạn (0 nếu không có). Lưu mốc hết hạn theo giờ UTC.</summary>
        public static TimeSpan InfiniteHeartsLeft
        {
            get
            {
                TimeSpan left = ReadTime(InfiniteHeartsKey) - DateTime.UtcNow;
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        /// <summary>Cộng thêm phút tim vô hạn (nối tiếp nếu đang còn).</summary>
        public static void AddInfiniteHearts(int minutes)
        {
            if (minutes <= 0) return;
            DateTime from = DateTime.UtcNow + InfiniteHeartsLeft;
            WriteTime(InfiniteHeartsKey, from.AddMinutes(minutes));
        }

        /// <summary>Đã mua gói "No Ads" (bản dựng lại chưa có quảng cáo – chỉ lưu cờ).</summary>
        public static bool NoAds
        {
            get => PlayerPrefs.GetInt(NoAdsKey, 0) == 1;
            set => PlayerPrefs.SetInt(NoAdsKey, value ? 1 : 0);
        }

        /// <summary>Số trận thắng liên tiếp (Bounce Streak) – thua thì về 0.</summary>
        public static int WinStreak
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(WinStreakKey, 0));
            set => PlayerPrefs.SetInt(WinStreakKey, Mathf.Max(0, value));
        }

        /// <summary>Đang ở mốc thưởng thứ mấy của Bounce Streak.</summary>
        public static int StreakStep
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(StreakStepKey, 0));
            set => PlayerPrefs.SetInt(StreakStepKey, Mathf.Max(0, value));
        }

        /// <summary>Lúc nhận được xu miễn phí lần kế tiếp trong Shop.</summary>
        public static DateTime FreeCoinReadyAt
        {
            get => ReadTime(FreeCoinKey);
            set => WriteTime(FreeCoinKey, value);
        }

        // ------------------------------------------------------------------ Hồ sơ
        const string ProfileNameKey = "bb_profile_name";
        const string AvatarKey = "bb_profile_avatar";

        public static string ProfileName
        {
            get => PlayerPrefs.GetString(ProfileNameKey, "Player");
            set => PlayerPrefs.SetString(ProfileNameKey, string.IsNullOrEmpty(value) ? "Player" : value);
        }

        /// <summary>Chỉ số avatar đang dùng (0 = Profile-1).</summary>
        public static int Avatar
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(AvatarKey, 0));
            set => PlayerPrefs.SetInt(AvatarKey, Mathf.Max(0, value));
        }

        // ------------------------------------------------------------------ Sự kiện Bounce Streak
        const string StreakEventEndKey = "bb_streak_event_end";

        /// <summary>Lúc sự kiện Bounce Streak hiện tại kết thúc (UTC).</summary>
        public static DateTime StreakEventEndsAt
        {
            get => ReadTime(StreakEventEndKey);
            set => WriteTime(StreakEventEndKey, value);
        }

        static DateTime ReadTime(string key)
        {
            long ticks;
            return long.TryParse(PlayerPrefs.GetString(key, "0"), out ticks) && ticks > 0
                ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue;
        }

        static void WriteTime(string key, DateTime utc) => PlayerPrefs.SetString(key, utc.Ticks.ToString());
    }
}
