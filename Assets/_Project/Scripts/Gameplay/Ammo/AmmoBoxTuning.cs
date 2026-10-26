using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tư thế hộp đạn trong khay</summary>
    public static class AmmoBoxTuning
    {
        /// <summary>Config đang dùng – GameManager gán lúc khởi động</summary>
        public static GameConfig Config;

        static readonly GameConfig.AmmoPose ActiveFallback =
            new GameConfig.AmmoPose { tiltX = -55f, length = 0.6f, offset = new Vector3(-0.01f, 0.69f, 0f), scale = 1.038f };
        static readonly GameConfig.AmmoPose InactiveFallback =
            new GameConfig.AmmoPose { tiltX = -78f, length = 0.4f, offset = new Vector3(0f, 0.07f, 0f), scale = 1.06f };

        public static GameConfig.AmmoPose Active => Config != null && Config.ammoActivePose != null ? Config.ammoActivePose : ActiveFallback;
        public static GameConfig.AmmoPose Inactive => Config != null && Config.ammoInactivePose != null ? Config.ammoInactivePose : InactiveFallback;

        /// <summary>Độ lún của hộp chưa active</summary>
        public static float SinkDepth => Config != null ? Config.ammoSinkDepth : 1.1f;

        /// <summary>Tăng mỗi lần chỉnh để AmmoBox biết phải dựng lại tư thế ngay</summary>
        public static int Version { get; private set; }

        /// <summary>GameConfig.OnValidate gọi khi có người kéo thanh trượt trong Inspector</summary>
        public static void Bump() => Version++;

        /// <summary>Dùng cho nội suy tư thế hộp giữa active và chưa active</summary>
        public static GameConfig.AmmoPose Lerp(float kActive01)
        {
            GameConfig.AmmoPose a = Active, b = Inactive;
            return new GameConfig.AmmoPose
            {
                tiltX = Mathf.Lerp(b.tiltX, a.tiltX, kActive01),
                length = Mathf.Lerp(b.length, a.length, kActive01),
                offset = Vector3.Lerp(b.offset, a.offset, kActive01),
                scale = Mathf.Lerp(b.scale, a.scale, kActive01),
            };
        }
    }
}
