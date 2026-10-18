using System;
using System.Collections;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    public enum GameState { Loading, Playing, Won, Lost }

    /// <summary>
    /// Bộ điều phối của màn chơi (composition root): dựng / nối các manager với nhau lúc Awake, giữ TRẠNG THÁI game
    /// (Loading → Playing → Won / Lost) và quyết định thắng / thua. Việc cụ thể do manager riêng lo:
    ///   LevelManager (nạp level) · InputManager (bấm) · LaunchManager (bắn hộp + sức chứa) · BoosterManager (booster)
    ///   · DebugManager (test) · AudioManager (âm thanh) · CameraFitter (camera) · GameUI (giao diện).
    /// Các manager nói chuyện bằng sự kiện (Observer), xem Docs/kien-truc-manager.md.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Asset")]
        public GameConfig config;
        public ColorLibrary colors;
        public UiSkin skin;

        [Header("Scene")]
        public Transform boardRoot;
        public Camera gameCamera;
        [Tooltip("Đặt sẵn trong Hierarchy để thấy & sửa UI trong Editor (UI/GameUI, bên dưới là các canvas…). " +
                 "Bỏ trống thì GameManager tự tạo lúc chạy từ các prefab trong UiSkin.")]
        public GameUI ui;
        [Tooltip("Camera vẽ UI (đặt sẵn trong Hierarchy, đứng xa bàn chơi ở x −1000). Bỏ trống thì tự tạo lúc chạy. " +
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
            Application.targetFrameRate = 60;
            if (gameCamera == null) gameCamera = Camera.main;

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
            _ui = ui != null ? ui : CreateChild<GameUI>("UI");
            if (vfxRoot == null)
            {
                var vfx = new GameObject("VFX");
                vfx.transform.SetParent(transform, false);
                vfxRoot = vfx.transform;
            }
            Vfx.Root = vfxRoot;

            // Một hướng nhìn: camera thẳng → là phẳng khung bàn 3D gốc (dựng cho camera nghiêng 20°) trước khi đọc vị trí máy xay/máng
            ViewSpace.FlattenStaticScene(boardRoot);
            CreateCubeLight(gameCamera != null ? gameCamera.transform.parent : null);
            balls.Init(config, colors, board);
            conveyor.Init(config, balls);
            StartCoroutine(balls.Prewarm(config.ballPrewarm));   // dựng sẵn bi + nạp shader: hộp đầu tiên lên máy xay không bị khựng
            theme.Init(boardRoot, gameCamera);
            theme.SetConveyor(conveyor);
            if (config.normalTheme != null) theme.SetReferenceGround(config.normalTheme.groundColor);
            Transform tri = boardRoot != null ? boardRoot.Find("Environment Base (1)/Triangle") : null;
            Transform triCol = tri != null ? tri.Find("Model/Triangle") : null;
            if (triCol != null) triCol.gameObject.AddComponent<TriangleFx>().Init(tri);
            _ui.Build(skin, config, uiCamera != null ? uiCamera : CreateUiCamera(), gameCamera);
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
            boosterManager.Init(_ui, board, ammoGrid, launchManager, levelManager);
            launchManager.Init(_ui, board, ammoGrid, conveyor, balls, boosterManager, levelManager);
            inputManager.Init(gameCamera, _ui, board, ammoGrid, boosterManager, launchManager);
            debugManager.Init(_ui, board, launchManager);

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
            PlayerData.Level = Mathf.Min(levelManager.CurrentLevel + 1, config.maxLevel);
            PlayerData.Coins += WinCoins;
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

        /// <summary>Chỉ cho công cụ cheat (Scripts/Debug/CheatRegistry): ép thua ngay để thử màn thua.</summary>
        public void CheatLose() => Lose("Cheat");

        void Lose(string reason)
        {
            if (state != GameState.Playing) return;
            SetState(GameState.Lost);
            AudioManager.Play(Sfx.Lose);
            OnLose?.Invoke(reason);
            _ui.ShowFail(levelManager.CurrentLevel, CurrentDifficulty, reason, levelManager.Restart);
        }

        /// <summary>
        /// Gạch/khối bàn quay thêm -20° để nhìn thẳng camera (camera gốc nghiêng 20°) → đèn chiếu gạch phải xoay bù +20°
        /// thì ánh sáng trên mặt gạch mới y hệt game gốc.
        /// DỰ PHÒNG: nếu scene chưa có đèn riêng cho layer Cube thì tạo từ đèn chung (xoay +20° cho khớp gạch ngả −20°).
        /// Scene hiện tại đã có sẵn đèn "(Cube, view-aligned)" nên hàm này không làm gì. Đèn riêng của thạch cũng đã nung sẵn trong scene.
        /// </summary>
        void CreateCubeLight(Transform root)
        {
            int cube = LayerMask.NameToLayer("Cube");
            if (root == null || cube < 0) return;
            foreach (Light src in root.GetComponentsInChildren<Light>(true))
            {
                if (src.type != LightType.Directional || (src.cullingMask & (1 << cube)) == 0 || src.cullingMask == 1 << cube) continue;
                var go = new GameObject(src.name + " (Cube, view-aligned)");
                go.transform.SetParent(src.transform.parent, false);
                go.transform.rotation = Quaternion.Euler(ViewSpace.LegacyPitch, 0f, 0f) * src.transform.rotation; // gạch đứng ngả -20° + đèn +20° (đo Lv44: S 197/199)
                var l = go.AddComponent<Light>();
                l.type = LightType.Directional;
                l.color = src.color;
                l.intensity = src.intensity;
                l.shadows = src.shadows;
                l.renderMode = src.renderMode;
                l.cullingMask = 1 << cube;
                src.cullingMask &= ~(1 << cube);
                break;
            }
        }

        /// <summary>
        /// Camera riêng cho canvas UI (Screen Space - Camera): chiếu ortho chuẩn. Camera game dùng ma trận chiếu ép dọc
        /// (giữ khung hình gốc) – nếu UI vẽ bằng nó thì cũng bị kéo dọc/lệch. Canvas Screen Space - Camera chỉ được vẽ
        /// bởi camera gán cho nó, nên camera game không cần đổi culling mask (thanh capacity world-space vẫn do camera game vẽ).
        /// </summary>
        Camera CreateUiCamera()
        {
            var go = new GameObject("UI Camera");
            go.transform.SetParent(gameCamera != null ? gameCamera.transform.parent : null, false);
            go.transform.position = new Vector3(-1000f, 0f, -10f); // xa bàn chơi: canvas đặt trước camera này không đè vật thể
            var c = go.AddComponent<Camera>();
            c.orthographic = true;
            c.orthographicSize = 10f;
            c.clearFlags = CameraClearFlags.Depth;
            c.cullingMask = ~0; // đứng xa bàn chơi (x -1000): frustum chỉ chứa các canvas gắn vào nó
            c.depth = (gameCamera != null ? gameCamera.depth : 0f) + 1f;
            c.nearClipPlane = 0.01f;
            c.farClipPlane = 50f;
            c.allowHDR = false;
            c.allowMSAA = false;
            return c;
        }
    }
}
