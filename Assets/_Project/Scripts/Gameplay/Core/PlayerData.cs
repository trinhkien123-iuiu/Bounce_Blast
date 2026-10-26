using System;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tiến độ người chơi lưu trên máy</summary>
    public static class PlayerData
    {
        const string BaseLevelKey = "bb_level";
        static string _levelKey = BaseLevelKey;

        /// <summary>Tiến độ level lưu riêng theo bộ level</summary>
        public static void UseLevelSet(string setName) =>
            _levelKey = string.IsNullOrEmpty(setName) || setName == "original" ? BaseLevelKey : BaseLevelKey + "_" + setName;
        const string CoinsKey = "bb_coins";

        /// <summary>Số tim tối đa</summary>
        public const int MaxHearts = 5;
        /// <summary>Số phút hồi một tim</summary>
        public const int HeartRegenMinutes = 30;
        /// <summary>Số xu của người chơi mới (video gốc: 600)</summary>
        public const int StartCoins = 600;
        /// <summary>Giá nút Play On khi hết sức chứa (khớp số hiện trên popup)</summary>
        public const int PlayOnCost = 900;

        const string HeartsKey = "bb_hearts";
        const string HeartNextKey = "bb_heart_next";

        /// <summary>Số tim hiện có (đã tính hồi theo thời gian)</summary>
        public static int Hearts
        {
            get
            {
                RefreshHearts();
                return PlayerPrefs.GetInt(HeartsKey, MaxHearts);
            }
        }

        /// <summary>Còn tim hoặc đang có tim vô hạn nên chơi được</summary>
        public static bool CanPlay => InfiniteHeartsLeft > TimeSpan.Zero || Hearts > 0;

        /// <summary>Còn bao lâu tới tim kế tiếp (0 nếu đã đầy)</summary>
        public static TimeSpan NextHeartIn
        {
            get
            {
                if (Hearts >= MaxHearts) return TimeSpan.Zero;
                TimeSpan left = ReadTime(HeartNextKey) - DateTime.UtcNow;
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        /// <summary>Xử lý hồi tim theo thời gian đã trôi</summary>
        static void RefreshHearts()
        {
            int h = PlayerPrefs.GetInt(HeartsKey, MaxHearts);
            if (h >= MaxHearts) return;
            DateTime next = ReadTime(HeartNextKey);
            DateTime now = DateTime.UtcNow;
            if (next == DateTime.MinValue) next = now.AddMinutes(HeartRegenMinutes);
            while (h < MaxHearts && now >= next) { h++; next = next.AddMinutes(HeartRegenMinutes); }
            PlayerPrefs.SetInt(HeartsKey, h);
            if (h < MaxHearts) WriteTime(HeartNextKey, next);
        }

        /// <summary>Xử lý trừ một tim khi thua (không trừ nếu đang tim vô hạn)</summary>
        public static void LoseHeart()
        {
            if (InfiniteHeartsLeft > TimeSpan.Zero) return;
            int h = Hearts;
            if (h <= 0) return;
            PlayerPrefs.SetInt(HeartsKey, h - 1);
            if (h >= MaxHearts) WriteTime(HeartNextKey, DateTime.UtcNow.AddMinutes(HeartRegenMinutes));
        }

        /// <summary>Xử lý hồi đầy tim</summary>
        public static void RefillHearts() => PlayerPrefs.SetInt(HeartsKey, MaxHearts);

        /// <summary>Thử trừ xu: đủ thì trừ và trả true, không đủ trả false</summary>
        public static bool TrySpendCoins(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            return true;
        }

        /// <summary>Level kế tiếp sẽ chơi (≥ 1)</summary>
        public static int Level
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(_levelKey, 1));
            set => PlayerPrefs.SetInt(_levelKey, Mathf.Max(1, value));
        }

        public static int Coins
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, StartCoins));
            set => PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, value));
        }

        const string InfiniteHeartsKey = "bb_inf_hearts_until";
        const string NoAdsKey = "bb_no_ads";
        const string WinStreakKey = "bb_win_streak";
        const string StreakStepKey = "bb_streak_step";
        const string FreeCoinKey = "bb_free_coin_at";

        /// <summary>Còn bao lâu tim vô hạn (0 nếu không có)</summary>
        public static TimeSpan InfiniteHeartsLeft
        {
            get
            {
                TimeSpan left = ReadTime(InfiniteHeartsKey) - DateTime.UtcNow;
                return left > TimeSpan.Zero ? left : TimeSpan.Zero;
            }
        }

        /// <summary>Cộng thêm phút tim vô hạn</summary>
        public static void AddInfiniteHearts(int minutes)
        {
            if (minutes <= 0) return;
            DateTime from = DateTime.UtcNow + InfiniteHeartsLeft;
            WriteTime(InfiniteHeartsKey, from.AddMinutes(minutes));
        }

        /// <summary>Đã mua gói "No Ads" (bản dựng lại chưa có quảng cáo</summary>
        public static bool NoAds
        {
            get => PlayerPrefs.GetInt(NoAdsKey, 0) == 1;
            set => PlayerPrefs.SetInt(NoAdsKey, value ? 1 : 0);
        }

        /// <summary>Số trận thắng liên tiếp (Bounce Streak)</summary>
        public static int WinStreak
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(WinStreakKey, 0));
            set => PlayerPrefs.SetInt(WinStreakKey, Mathf.Max(0, value));
        }

        /// <summary>Đang ở mốc thưởng thứ mấy của Bounce Streak</summary>
        public static int StreakStep
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(StreakStepKey, 0));
            set => PlayerPrefs.SetInt(StreakStepKey, Mathf.Max(0, value));
        }

        /// <summary>Lúc nhận được xu miễn phí lần kế tiếp trong Shop</summary>
        public static DateTime FreeCoinReadyAt
        {
            get => ReadTime(FreeCoinKey);
            set => WriteTime(FreeCoinKey, value);
        }

        const string ProfileNameKey = "bb_profile_name";
        const string AvatarKey = "bb_profile_avatar";

        public static string ProfileName
        {
            get => PlayerPrefs.GetString(ProfileNameKey, "Player");
            set => PlayerPrefs.SetString(ProfileNameKey, string.IsNullOrEmpty(value) ? "Player" : value);
        }

        /// <summary>Chỉ số avatar đang dùng (0 = Profile-1)</summary>
        public static int Avatar
        {
            get => Mathf.Max(0, PlayerPrefs.GetInt(AvatarKey, 0));
            set => PlayerPrefs.SetInt(AvatarKey, Mathf.Max(0, value));
        }

        const string StreakEventEndKey = "bb_streak_event_end";

        /// <summary>Lúc sự kiện Bounce Streak hiện tại kết thúc</summary>
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
