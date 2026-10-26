using System;
using System.Collections;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>Quản lý LEVEL: nạp / dựng lại bàn chơi và khay đạn cho một level, cân camera, áp theme + nhạc theo độ khó…</summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Level đầu tiên")]
        [Range(1, 550)] public int startLevel = 1;
        [Tooltip("Bật: chơi tiếp level đã lưu (PlayerData). Tắt: luôn bắt đầu từ startLevel (tiện test). " +
                 "Vào từ MenuGame thì luôn chơi tiếp level đã lưu.")]
        public bool continueFromSave = false;

        [Header("Debug (read only)")]
        public int currentLevel;

        /// <summary>Sắp dọn level cũ (dừng coroutine, đóng popup…)</summary>
        public event Action OnLevelLoading;
        /// <summary>Level mới đã dựng xong (bàn, khay, theme, nhạc, UI)</summary>
        public event Action<LevelModel> OnLevelLoaded;

        public LevelModel Level { get; private set; }
        public int CurrentLevel => currentLevel;

        GameConfig _config;
        ColorLibrary _colors;
        GameUI _ui;
        BoardController _board;
        AmmoGridController _grid;
        ConveyorSystem _conveyor;
        BallManager _balls;
        ThemeApplier _theme;
        CameraFitter _fitter;

        /// <summary>Xử lý khởi tạo quản lý level</summary>
        public void Init(GameConfig config, ColorLibrary colors, GameUI ui, BoardController board, AmmoGridController grid,
                         ConveyorSystem conveyor, BallManager balls, ThemeApplier theme, CameraFitter fitter)
        {
            _config = config; _colors = colors; _ui = ui; _board = board; _grid = grid;
            _conveyor = conveyor; _balls = balls; _theme = theme; _fitter = fitter;
        }

        IEnumerator Start()
        {
            yield return null;
#if UNITY_EDITOR
            string playtest = UnityEditor.SessionState.GetString(PlaytestKey, "");
            if (playtest.Length > 0)
            {
                UnityEditor.SessionState.EraseString(PlaytestKey);
                _playtest = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelData>(playtest);
                if (_playtest != null) { LoadLevel(1); yield break; }
            }
#endif
            LoadLevel(continueFromSave || SceneFlow.FromMenu ? PlayerData.Level : startLevel);
        }

        /// <summary>Khoá SessionState mà Level Editor ghi đường dẫn LevelData cần chơi thử</summary>
        public const string PlaytestKey = "BB.Playtest.LevelPath";
        LevelData _playtest;

        /// <summary>Xử lý nạp và dựng một level</summary>
        public void LoadLevel(int id)
        {
            OnLevelLoading?.Invoke();
            id = _playtest != null ? 1 : Mathf.Clamp(id, 1, _config.MaxLevel);

            _ui.ClosePopup();
            _balls.ClearAll();
            _conveyor.ClearAll();
            _board.ClearAll();
            _grid.ClearAll();
            foreach (AmmoBox stray in FindObjectsOfType<AmmoBox>()) Destroy(stray.gameObject);

            Level = _playtest != null ? LevelParser.Build(_playtest.ToTokens(id)) : LevelParser.Load(_config, id);
            if (Level == null) return;
            currentLevel = id;

            _fitter.SetLevel(Level);
            _fitter.Fit();
            _grid.AreaBottomOverride = _fitter.AmmoAreaBottom();
            _board.Build(Level, _config, _colors);
            _grid.Build(Level, _config, _colors);

            _theme.Apply(_config.ThemeFor(Level.Difficulty));
            AudioManager.PlayMusic(Level.Difficulty == LevelDifficulty.SuperHard ? _config.musicSuperHard
                : Level.Difficulty == LevelDifficulty.Hard ? _config.musicHard : _config.musicNormal);
            _ui.SetLevel(id, Level.Difficulty);

            Debug.Log($"[BounceBlast] Level {id} ({Level.Difficulty}) – board {Level.BoardCols}x{Level.BoardRows} x{Level.Multiplier}, " +
                      $"capacity {Level.Capacity}, tổng bi {Level.TotalBalls}, khối {_board.DestructibleCount}");
            OnLevelLoaded?.Invoke(Level);
            SceneFlow.MarkLevelReady();
        }

        /// <summary>Xử lý chơi lại level hiện tại</summary>
        public void Restart() => LoadLevel(currentLevel);

        /// <summary>Sang level kế (tối đa level cuối)</summary>
        public void LoadNext() => LoadLevel(Mathf.Min(currentLevel + 1, _config.MaxLevel));

        [ContextMenu("Debug/Next level")] void DebugNext() => LoadLevel(currentLevel + 1);
        [ContextMenu("Debug/Prev level")] void DebugPrev() => LoadLevel(currentLevel - 1);
    }
}
