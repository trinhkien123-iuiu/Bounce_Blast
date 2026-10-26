using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Thiết lập hiệu năng ngay khi app khởi động</summary>
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
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

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
