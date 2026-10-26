using System;
using System.Collections;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    public enum GameState { Loading, Playing, Won, Lost }

    /// <summary>
    /// Bộ điều phối của màn chơi (composition root): dựng / nối các manager với nhau lúc Awake, giữ TRẠNG THÁI game
    /// (Loading → Playing → Won / Lost) và quyết định thắng / thua. Việc cụ thể do manager riêng lo:
    ///   LevelManager (nạp level) · InputManager (bấm) · LaunchManager (bắn hộp + sức chứa) · BoosterManager (booster)
    ///   · DebugManager (test) · AudioManager (âm thanh) · CameraFitter (camera) · GameUI (giao diện).
    /// Các manager nói chuyện bằng sự kiện (Observer), xem ARCHITECTURE.md (mục 4b).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Asset")]
        public GameConfig config;
        public ColorLibrary colors;
        [Tooltip("Chỉ dùng cho tool Editor (Tools/Bounce Blast/Setup) – lúc chạy không đọc")] public UiSkin skin;

        [Header("Scene")]
        public Transform boardRoot;
        public Camera gameCamera;
        [Tooltip("UI đặt sẵn trong Hierarchy (UI/GameUI, bên dưới là các canvas…) – bắt buộc, lúc chạy không tự dựng UI.")]
        public GameUI ui;
        [Tooltip("Camera vẽ UI (đặt sẵn trong Hierarchy, đứng xa bàn chơi ở x −1000) – bắt buộc. " +
                 "Các Canvas UI đã trỏ sẵn tới camera này nên Edit mode nhìn Game view giống hệt lúc Play.")]
        public Camera uiCamera;

        [Header("Bàn chơi (đặt sẵn trong Hierarchy; để trống = tự tạo lúc chạy)")]
        [Tooltip("Bàn chơi: gạch, khối gộp, cột, hòm…")] public BoardController board;
        [Tooltip("Khay hộp đạn")] public AmmoGridController ammoGrid;
        [Tooltip("Máng + máy xay")] public ConveyorSystem conveyor;
        [Tooltip("Bi")] public BallManager balls;
        [Tooltip("Áp theme theo độ khó")] public ThemeApplier theme;
        [Tooltip("Cha của các hiệu ứng hạt (vỡ gạch, nổ…)")] public Transform vfxRoot;
        [Tooltip("Tường vật lý của lòng bàn (điểm đã nung sẵn, xem Gizmo khi chọn)")] public ArenaWalls arenaWalls;

        [Header("Các manager (đặt sẵn dưới Managers; để trống = tự tạo lúc chạy)")]
        public LevelManager levelManager;
        public InputManager inputManager;
        public LaunchManager launchManager;
        public BoosterManager boosterManager;
        public DebugManager debugManager;
        [Tooltip("Hướng dẫn level đầu + popup New Feature (bỏ trống = tự tạo)")] public OnboardingManager onboardingManager;
        [Tooltip("Prefab OnboardingCanvas đặt sẵn trong UI/ (bỏ trống = không có hướng dẫn tay chỉ)")] public OnboardingView onboardingView;
        [Tooltip("Cân camera theo HUD & hàng booster (số chỉnh ở GameConfig → Bố cục màn hình)")] public CameraFitter cameraFitter;
        [Tooltip("Âm thanh – bỏ trống thì tự gắn vào object này lúc chạy")] public AudioManager audioManager;

        [Header("Debug (read only)")]
        public GameState state;

        /// <summary>Trạng thái game vừa đổi.</summary>
        public event Action<GameState> OnStateChanged;
        /// <summary>Người chơi thắng level hiện tại.</summary>
        public event Action OnWin;
        /// <summary>Người chơi thua, kèm lý do.</summary>
        public event Action<string> OnLose;

        GameUI _ui;
        const int WinCoins = 50; // xu thưởng mỗi lần thắng

        void Awake()
        {
            Instance = this;
            if (gameCamera == null) gameCamera = Camera.main;

            PlayerData.UseLevelSet(config.ActiveSetName);   // tiến độ riêng từng bộ level
            AmmoBoxTuning.Config = config; // tư thế hộp đạn đọc từ GameConfig (chỉnh được ngay cả khi đang Play)
            AudioManager.Init(audioManager != null ? audioManager.gameObject : gameObject, config.sounds);
            DynamicGI.UpdateEnvironment(); // cập nhật ambient SH (Trilight như scene gốc) cho shader toon

            // ---- hệ thống bàn chơi
            board = Get(board, "Board");
            board.gameObject.layer = LayerMask.NameToLayer("Cube") >= 0 ? LayerMask.NameToLayer("Cube") : 0;
            ammoGrid = Get(ammoGrid, "AmmoGrid");
            conveyor = Get(conveyor, "Conveyor");
            balls = Get(balls, "Balls");
            theme = Get(theme, "Theme");
            _ui = ui;
            if (_ui == null) { Debug.LogError("GameManager: ô UI trống – đặt UI/GameUI vào scene và kéo vào ô"); enabled = false; return; }
            if (vfxRoot == null)
            {
                var vfx = new GameObject("VFX");
                vfx.transform.SetParent(transform, false);
                vfxRoot = vfx.transform;
            }
            Vfx.Root = vfxRoot;

            // Một hướng nhìn: camera thẳng → là phẳng khung bàn 3D gốc (dựng cho camera nghiêng 20°) trước khi đọc vị trí máy xay/máng
            ViewSpace.FlattenStaticScene(boardRoot);
            SceneRig.EnsureCubeLight(gameCamera != null ? gameCamera.transform.parent : null);
            balls.Init(config, colors, board);
            conveyor.Init(config, balls);
            StartCoroutine(balls.Prewarm(config.ballPrewarm));   // dựng sẵn bi + nạp shader: hộp đầu tiên lên máy xay không bị khựng
            theme.Init(boardRoot, gameCamera);
            theme.SetConveyor(conveyor);
            if (config.normalTheme != null) theme.SetReferenceGround(config.normalTheme.groundColor);
            Transform tri = boardRoot != null ? boardRoot.Find("Environment Base (1)/Triangle") : null;
            Transform triCol = tri != null ? tri.Find("Model/Triangle") : null;
            if (triCol != null) triCol.gameObject.AddComponent<TriangleFx>().Init(tri);
            if (uiCamera == null) Debug.LogError("GameManager: ô UI Camera trống – các Canvas trỏ tới camera này");
            _ui.Build(config, uiCamera, gameCamera);
            cameraFitter = Get(cameraFitter, "CameraFitter");
            cameraFitter.Init(config, _ui, gameCamera);
            arenaWalls = Get(arenaWalls, "ArenaWalls"); // RequireComponent tự thêm EdgeCollider2D
            arenaWalls.EnsureBuilt(config);

            // ---- các manager
            levelManager = Get(levelManager, "LevelManager");
            launchManager = Get(launchManager, "LaunchManager");
            boosterManager = Get(boosterManager, "BoosterManager");
            inputManager = Get(inputManager, "InputManager");
            debugManager = Get(debugManager, "DebugManager");
            levelManager.Init(config, colors, _ui, board, ammoGrid, conveyor, balls, theme, cameraFitter);
            boosterManager.SetColors(colors);
            boosterManager.Init(config, gameCamera, _ui, board, ammoGrid, launchManager, levelManager);
            launchManager.Init(_ui, board, ammoGrid, conveyor, balls, boosterManager, levelManager);
            onboardingManager = Get(onboardingManager, "OnboardingManager");
            inputManager.Init(gameCamera, _ui, board, ammoGrid, boosterManager, launchManager, balls, onboardingManager);
            debugManager.Init(_ui, board, launchManager);

            onboardingManager.Init(config, _ui, levelManager, boosterManager, ammoGrid, balls, gameCamera, onboardingView);

            // ---- Observer: ai báo gì thì ai nghe
            levelManager.OnLevelLoading += () => { StopAllCoroutines(); SetState(GameState.Loading); };
            levelManager.OnLevelLoaded += _ => SetState(GameState.Playing);
            board.OnBoardCleared += HandleWin;
            launchManager.OnGameOver += Lose;
            _ui.OnSettingsPressed += () => _ui.ShowSettings(CurrentDifficulty, levelManager.Restart, null, SceneFlow.GoToMenu);
        }

        LevelDifficulty CurrentDifficulty => levelManager.Level != null ? levelManager.Level.Difficulty : LevelDifficulty.Normal;

        /// <summary>Lấy hệ thống đã đặt sẵn; chưa có thì tạo một object con mới (scene trống vẫn chạy được).</summary>
        T Get<T>(T assigned, string name) where T : Component => assigned != null ? assigned : CreateChild<T>(name);

        T CreateChild<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        void SetState(GameState s)
        {
            state = s;
            bool playing = s == GameState.Playing;
            launchManager.SetPlaying(playing);
            boosterManager.SetPlaying(playing);
            inputManager.SetPlaying(playing);
            debugManager.SetPlaying(playing);
            OnStateChanged?.Invoke(s);
        }

        // ------------------------------------------------------------------ Kết thúc
        void HandleWin()
        {
            if (state != GameState.Playing) return;
            SetState(GameState.Won);
            AudioManager.Play(Sfx.Win);
            AudioManager.Play(Sfx.Confetti);
            PlayerData.Level = Mathf.Min(levelManager.CurrentLevel + 1, config.MaxLevel);
            PlayerData.Coins += WinCoins;
            BounceStreak.RecordWin();
            OnWin?.Invoke();
            StartCoroutine(WinRoutine());
        }

        IEnumerator WinRoutine()
        {
            yield return new WaitForSeconds(0.35f);
            Vfx.Spawn(config.confettiVfx, new Vector3(config.arena.center.x, 2f, -3f), Color.white, 2.5f);
            conveyor.ClearAll();
            balls.ClearAll();
            yield return _ui.PlayWinSequence(levelManager.CurrentLevel, CurrentDifficulty, WinCoins, levelManager.LoadNext);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS
        /// <summary>Chỉ cho công cụ cheat (Scripts/Debug/CheatRegistry): ép thua ngay để thử màn thua.</summary>
        public void CheatLose() => Lose("Cheat");
#endif

        void Lose(string reason)
        {
            if (state != GameState.Playing) return;
            SetState(GameState.Lost);
            AudioManager.Play(Sfx.Lose);
            BounceStreak.RecordLose();
            OnLose?.Invoke(reason);
            _ui.ShowFail(levelManager.CurrentLevel, CurrentDifficulty, reason, levelManager.Restart);
        }
    }
}
