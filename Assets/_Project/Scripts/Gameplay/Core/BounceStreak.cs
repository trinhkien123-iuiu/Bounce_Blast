using System;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Sự kiện "Bounce Streak" (thanh huy chương trên trang Home của bản gốc)</summary>
    public static class BounceStreak
    {
        /// <summary>Số trận thắng liên tiếp cần cho từng mốc</summary>
        static readonly int[] Targets = { 1, 3, 5, 10 };
        /// <summary>Phút tim vô hạn nhận ở từng mốc</summary>
        static readonly int[] RewardMinutes = { 10, 30, 60, 120 };

        /// <summary>Mỗi đợt sự kiện kéo dài bao lâu</summary>
        const int EventDays = 4;

        static int Step => Mathf.Min(PlayerData.StreakStep, Targets.Length - 1);

        /// <summary>Thời gian còn lại của đợt</summary>
        public static TimeSpan TimeLeft
        {
            get
            {
                DateTime now = DateTime.UtcNow;
                if (PlayerData.StreakEventEndsAt <= now)
                {
                    PlayerData.StreakEventEndsAt = now.AddDays(EventDays);
                    PlayerData.WinStreak = 0;
                    PlayerData.StreakStep = 0;
                }
                return PlayerData.StreakEventEndsAt - now;
            }
        }

        public static int Wins => PlayerData.WinStreak;
        public static int Target => Targets[Step];
        public static int Reward => RewardMinutes[Step];
        public static bool CanClaim => Wins >= Target;

        /// <summary>Xử lý ghi nhận một trận thắng cho chuỗi</summary>
        public static void RecordWin() => PlayerData.WinStreak = Mathf.Min(PlayerData.WinStreak + 1, Targets[Targets.Length - 1]);

        /// <summary>Thua → về mốc đầu</summary>
        public static void RecordLose()
        {
            PlayerData.WinStreak = 0;
            PlayerData.StreakStep = 0;
        }

        /// <summary>Nhận thưởng mốc hiện tại (nếu đủ) rồi sang mốc kế</summary>
        public static int Claim()
        {
            if (!CanClaim) return 0;
            int minutes = Reward;
            PlayerData.AddInfiniteHearts(minutes);
            if (PlayerData.StreakStep < Targets.Length - 1) PlayerData.StreakStep++;
            else PlayerData.WinStreak = 0;
            return minutes;
        }
    }
}
