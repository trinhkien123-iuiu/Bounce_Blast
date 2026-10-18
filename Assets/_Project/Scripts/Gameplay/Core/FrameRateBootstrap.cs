using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Thiết lập hiệu năng ngay khi app khởi động (trước mọi scene), không cần đặt vào scene:
    /// - tắt VSync (VSync bật làm Unity bỏ qua <c>targetFrameRate</c>) và đặt 60 fps (mobile mặc định chỉ 30);
    /// - giới hạn bước thời gian tối đa: máy chậm không chạy bù hàng loạt bước vật lý (vòng xoáy giật);
    /// - trên mobile: chặn độ phân giải ở <see cref="MaxWidth"/> px và gắn <see cref="AdaptiveResolution"/> (tự hạ nếu vẫn tụt fps).
    /// </summary>
    public static class FrameRateBootstrap
    {
        public const int TargetFps = 60;
        public const int MaxWidth = 1080;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFps;
            Time.maximumDeltaTime = 0.05f;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;   // không tắt màn hình khi đang chơi/test

            if (!Application.isMobilePlatform) return;
            if (Screen.width > MaxWidth)
            {
                float k = (float)MaxWidth / Screen.width;
                Screen.SetResolution(MaxWidth, Mathf.RoundToInt(Screen.height * k), true);
            }
            var go = new GameObject("AdaptiveResolution");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<AdaptiveResolution>();
        }
    }
}
