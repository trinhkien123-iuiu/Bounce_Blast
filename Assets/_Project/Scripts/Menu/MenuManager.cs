using UnityEngine;
using BounceBlast.Gameplay;
using BounceBlast.UI;

namespace BounceBlast.Menu
{
    /// <summary>
    /// Điều phối scene MenuGame (màn hình chính): hiển thị level kế tiếp / xu / tim, nghe sự kiện của <see cref="MainMenuView"/>
    /// rồi chuyển sang Gameplay. Mọi tham chiếu kéo sẵn trong Hierarchy (Systems/MenuManager).
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        [Header("Asset")]
        [Tooltip("Dùng chung với Gameplay: danh sách level (đọc độ khó), âm thanh, nhạc")] [SerializeField] GameConfig config;

        [Tooltip("Âm thanh – bỏ trống thì tự gắn vào object này lúc chạy")] [SerializeField] AudioManager audioManager;

        [Header("Scene")]
        [Tooltip("Màn hình chính (prefab MainMenuCanvas đặt sẵn dưới Systems/UI)")] [SerializeField] MainMenuView view;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Time.timeScale = 1f;
            AudioManager.Init(audioManager != null ? audioManager.gameObject : gameObject, config.sounds);

            string missing = view != null ? view.Validate() : "view";
            if (missing != null) { Debug.LogError("MenuManager: MainMenuView chưa nối đủ: " + missing); return; }

            view.OnPlay += SceneFlow.GoToGameplay;
            view.OnComingSoon += title => view.ShowToast(string.IsNullOrEmpty(title) ? "Coming soon!" : title + " - coming soon!");
            view.OnSoundChanged += v => AudioManager.SoundOn = v;
            view.OnMusicChanged += v => AudioManager.MusicOn = v;
            view.OnVibrationChanged += v => PlayerPrefs.SetInt("bb_vibration", v ? 1 : 0);
        }

        void Start()
        {
            if (view == null) return;
            int level = Mathf.Min(PlayerData.Level, config.maxLevel);
            LevelModel model = LevelParser.Load(config.levels, level);
            view.SetLevel(level, model != null ? model.Difficulty : LevelDifficulty.Normal);
            view.SetCoins(PlayerData.Coins);
            view.SetHearts(PlayerData.MaxHearts);
            view.SetSettings(AudioManager.SoundOn, AudioManager.MusicOn, PlayerPrefs.GetInt("bb_vibration", 1) == 1);
            AudioManager.PlayMusic(config.musicMenu);
        }
    }
}
