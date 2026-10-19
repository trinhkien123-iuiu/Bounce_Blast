using System;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Sự kiện "Bounce Streak" (thanh huy chương trên trang Home của bản gốc): thắng liên tiếp đủ mốc thì nhận tim vô hạn,
    /// thua là mất chuỗi. Bản gốc lấy mốc/phần thưởng từ server LiveOps – ở đây cố định trong code.
    /// Dữ liệu lưu trong <see cref="PlayerData"/>; Gameplay báo thắng/thua, Menu hiển thị và nhận thưởng.
    /// </summary>
    public static class BounceStreak
    {
        /// <summary>Số trận thắng liên tiếp cần cho từng mốc (ảnh gốc mốc đầu là "0/1").</summary>
        static readonly int[] Targets = { 1, 3, 5, 10 };
        /// <summary>Phút tim vô hạn nhận ở từng mốc (ảnh gốc mốc đầu là "10m").</summary>
        static readonly int[] RewardMinutes = { 10, 30, 60, 120 };

        /// <summary>Mỗi đợt sự kiện kéo dài bao lâu (ảnh gốc còn "3d 18h").</summary>
        const int EventDays = 4;

        static int Step => Mathf.Min(PlayerData.StreakStep, Targets.Length - 1);

        /// <summary>Thời gian còn lại của đợt; hết đợt thì mở đợt mới và chuỗi về 0.</summary>
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

        public static void RecordWin() => PlayerData.WinStreak = Mathf.Min(PlayerData.WinStreak + 1, Targets[Targets.Length - 1]);

        /// <summary>Thua → về mốc đầu.</summary>
        public static void RecordLose()
        {
            PlayerData.WinStreak = 0;
            PlayerData.StreakStep = 0;
        }

        /// <summary>Nhận thưởng mốc hiện tại (nếu đủ) rồi sang mốc kế; mốc cuối thì lặp lại mốc cuối. Trả số phút đã nhận (0 nếu chưa đủ).</summary>
        public static int Claim()
        {
            if (!CanClaim) return 0;
            int minutes = Reward;
            PlayerData.AddInfiniteHearts(minutes);
            if (PlayerData.StreakStep < Targets.Length - 1) PlayerData.StreakStep++;
            else PlayerData.WinStreak = 0;   // đã ở mốc cuối: đếm lại cho lượt sau
            return minutes;
        }
    }
}
