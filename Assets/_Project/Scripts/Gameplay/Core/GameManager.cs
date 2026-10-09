using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BounceBlast.Gameplay
{
    public enum GameState { Loading, Playing, Won, Lost }

    /// <summary>
    /// Điều phối màn chơi: nạp level → bấm hộp đạn → hộp bay lên máng → bi chạy máng → phun vào bàn
    /// → trúng khối cùng màu → khối vỡ/rơi → sạch bàn = THẮNG. Vượt sức chứa / hết nước đi = THUA.
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

        [Header("Level")]
        [Range(1, 550)] public int startLevel = 1;
        [Tooltip("Bật: chơi tiếp level đã lưu (PlayerPrefs). Tắt: luôn bắt đầu từ startLevel (tiện test).")]
        public bool continueFromSave = false;

        [Header("Debug")]
        [Tooltip("Tự bấm hộp đạn khi còn capacity – để test chơi hết màn")]
        public bool autoPlay;

        [Header("Debug (read only)")]
        public GameState state;
        public int currentLevel;
        public int capacityUsed;
        public float capacityMax;
        public int ballsInSystem;
        public int blocksLeft;

        BoardController _board;
        AmmoGridController _grid;
        ConveyorSystem _conveyor;
        BallManager _balls;
        GameUI _ui;
        ThemeApplier _theme;
        readonly CapacitySystem _capacity = new CapacitySystem();
        readonly BoosterSystem _boosters = new BoosterSystem();
        LevelModel _level;
        bool _started;
        float _idleTimer;
        float _deadlockTimer;

        const string SaveKey = "bb_level";

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            if (gameCamera == null) gameCamera = Camera.main;

            AudioManager.Init(gameObject, config.sounds);
            DynamicGI.UpdateEnvironment(); // cập nhật ambient SH (Trilight như scene gốc) cho shader toon

            _board = CreateChild<BoardController>("Board");
            _board.gameObject.layer = LayerMask.NameToLayer("Cube") >= 0 ? LayerMask.NameToLayer("Cube") : 0;
            _grid = CreateChild<AmmoGridController>("AmmoGrid");
            _conveyor = CreateChild<ConveyorSystem>("Conveyor");
            _balls = CreateChild<BallManager>("Balls");
            _theme = CreateChild<ThemeApplier>("Theme");
            _ui = CreateChild<GameUI>("UI");
            var vfx = new GameObject("VFX");
            vfx.transform.SetParent(transform, false);
            Vfx.Root = vfx.transform;

            // Một hướng nhìn: camera thẳng → là phẳng khung bàn 3D gốc (dựng cho camera nghiêng 20°) trước khi đọc vị trí máy xay/máng
            ViewSpace.FlattenStaticScene(boardRoot);
            CreateCubeLight(gameCamera != null ? gameCamera.transform.parent : null);
            _balls.Init(config, colors, _board);
            _conveyor.Init(config, _balls);
            _theme.Init(boardRoot, gameCamera);
            _theme.SetConveyor(_conveyor);
            Transform tri = boardRoot != null ? boardRoot.Find("Environment Base (1)/Triangle") : null;
            Transform triCol = tri != null ? tri.Find("Model/Triangle") : null;
            if (triCol != null) triCol.gameObject.AddComponent<TriangleFx>().Init(tri);
            _ui.Build(skin, config, CreateUiCamera(), gameCamera);
            CreateArenaWalls();

            _board.OnBoardCleared += HandleWin;
            _balls.OnBallConsumed += _ => _capacity.Remove(1);
            _grid.OnBoxLaunched += box => _conveyor.Send(box);
            _ui.OnBoosterPressed += HandleBooster;
            _ui.OnSettingsPressed += () => _ui.ShowSettings(_level != null ? _level.Difficulty : LevelDifficulty.Normal, Restart, null);
            _boosters.OnChanged += RefreshBoosterUi;
        }

        void Start()
        {
            int level = continueFromSave ? PlayerPrefs.GetInt(SaveKey, startLevel) : startLevel;
            LoadLevel(level);
        }

        T CreateChild<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        // ------------------------------------------------------------------ Nạp level
        public void LoadLevel(int id)
        {
            StopAllCoroutines();
            id = Mathf.Clamp(id, 1, config.maxLevel);
            state = GameState.Loading;

            _ui.ClosePopup();
            _balls.ClearAll();
            _conveyor.ClearAll();
            _board.ClearAll();
            _grid.ClearAll();
            foreach (AmmoBox stray in FindObjectsOfType<AmmoBox>()) Destroy(stray.gameObject);

            _level = LevelParser.Load(config.levelSet, id);
            if (_level == null) return;
            currentLevel = id;

            FitCamera();
            _grid.AreaBottomOverride = AmmoAreaBottom();
            _board.Build(_level, config, colors);
            _grid.Build(_level, config, colors);
            _overflowPending = false;
            _capacity.Reset(_level.Capacity);
            _boosters.ResetArmed();
            _started = false;

            _theme.Apply(config.ThemeFor(_level.Difficulty));
            AudioManager.PlayMusic(_level.Difficulty == LevelDifficulty.SuperHard ? config.musicSuperHard
                : _level.Difficulty == LevelDifficulty.Hard ? config.musicHard : config.musicNormal);
            _ui.SetLevel(id, _level.Difficulty);
            RefreshBoosterUi();

            Debug.Log($"[BounceBlast] Level {id} ({_level.Difficulty}) – board {_level.BoardCols}x{_level.BoardRows} x{_level.Multiplier}, " +
                      $"capacity {_level.Capacity}, tổng bi {_level.TotalBalls}, khối {_board.DestructibleCount}");
            state = GameState.Playing;
        }

        public void Restart() => LoadLevel(currentLevel);

        float _fitAspect;
        Rect _fitSafe;

        /// <summary>
        /// Gạch/khối bàn quay thêm -20° để nhìn thẳng camera (camera gốc nghiêng 20°) → đèn chiếu gạch phải xoay bù +20°
        /// thì ánh sáng trên mặt gạch mới y hệt game gốc. Đèn chung "Cubes and others" còn chiếu khung bàn tĩnh (chỉ là phẳng,
        /// không xoay) nên giữ nguyên; tách layer Cube sang một đèn riêng đã xoay.
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
            // Đèn riêng của thạch (chỉ layer Ammunation): thiết lập theo thông số inspector (14.7°, -27.7°, 37.83°)
            int ammo = LayerMask.NameToLayer("Ammunation");
            if (ammo >= 0)
                foreach (Light l in root.GetComponentsInChildren<Light>(true))
                    if (l.type == LightType.Directional && l.cullingMask == 1 << ammo)
                    {
                        l.transform.position = new Vector3(6.49f, 14.88f, -32.09f);
                        l.transform.rotation = Quaternion.Euler(14.7f, -27.7f, 37.83f);
                        l.intensity = 0.585f;
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

        const float FrameTop = 8.43f;      // mép trên NGOÀI của khung máng (đo trên ảnh chụp: viền sáng đầu tiên)
        const float AmmoBoxShift = -0.218f; // ViewSpace.LegacyShift(-0.6): mặt hộp đạn z = -0.6
        const float BoosterGap = 0.2f;      // khe giữa đáy khay và nút booster (đv thế giới)

        /// <summary>
        /// Mép dưới vùng khay đạn = mép trên hàng nút booster THẬT trên màn hình (+ khe) → không ô nào bị booster đè.
        /// </summary>
        float AmmoAreaBottom()
        {
            float px = _ui != null ? _ui.BoosterTopScreenY : -1f;
            if (gameCamera == null || px <= 0f) return float.NaN;
            Vector3 w = gameCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, px, 24.2f)); // mặt hộp z = -0.6
            return w.y - AmmoBoxShift + BoosterGap;
        }

        const float AmmoTargetCell = 1.45f; // ô khay mong muốn (gốc Lv44 ≈ 1.45)

        void FitCamera()
        {
            if (gameCamera == null) return;
            // Camera ortho NHÌN THẲNG (một hướng nhìn cho cả game – xem ViewSpace). Camera gốc nghiêng 20° nên chiều dọc
            // của thế giới bị ép cos20 ≈ 0.94 trên màn hình; giữ đúng tỉ lệ đó bằng ma trận chiếu ortho ép dọc.
            //
            // Bố cục (như game gốc): mép trên khung máng NGANG TÂM hàng HUD (Lv, Settings đè lên góc khung), khung phủ ~90% bề
            // ngang (nửa bề ngang 5.98). Khay đạn nằm giữa thanh Capacity và hàng booster; nếu màn hình thấp (tai thỏ, máy
            // 19:9…) không đủ chỗ cho ô khay ≥ AmmoTargetCell thì THU NHỎ cả cảnh (đỉnh khung vẫn ở tâm HUD) vừa đủ để khay
            // đạt cỡ đó – tối đa MaxZoomOut; quá nữa thì ô khay mới co lại. Không ô nào bị booster đè.
            const float BaseHalfW = 5.98f, MaxZoomOut = 1.3f, MinHalfHeight = 11.6f;
            float aspect = Mathf.Max(0.3f, gameCamera.aspect);
            float W = Mathf.Max(1, Screen.width), H = Mathf.Max(1, Screen.height);
            float hudY = _ui != null ? _ui.HudCenterScreenY : -1f;
            if (hudY <= 0f) hudY = Screen.safeArea.yMax;
            float boosterY = _ui != null ? _ui.BoosterTopScreenY : -1f;

            float halfW = BaseHalfW;
            if (_level != null && boosterY > 0f && hudY > boosterY)
            {
                Vector2Int ext = AmmoGridController.VisibleExtent(_level);
                float target = Mathf.Min(config.ammoCellMax, AmmoTargetCell, config.ammoAreaWidth / ext.x);
                // chiều cao khay cần (đv thế giới, theo góc nhìn cũ như ammoAreaTop) – khoảng HUD→booster trên màn hình phải chứa
                // FrameTop → ammoAreaTop (cố định theo thế giới) + khay + khe
                float need = (FrameTop - config.ammoAreaTop) + ext.y * target - AmmoBoxShift + BoosterGap;
                float k = need / (hudY - boosterY);                                 // đv thế giới / pixel dọc cần có
                float halfWNeed = k * ViewSpace.LegacyCos * W * 0.5f;               // k = 2·halfW / (cos20 · W)
                halfW = Mathf.Clamp(halfWNeed, BaseHalfW, BaseHalfW * MaxZoomOut);
            }
            float size = Mathf.Max(halfW / aspect, MinHalfHeight);   // nửa chiều cao màn hình (đv màn hình); máy bè: kẹp dọc
            float halfH = size / ViewSpace.LegacyCos;                 // nửa vùng nhìn trên z = 0 (đv thế giới)
            halfW = size * aspect;
            float centerY = FrameTop - (Mathf.Clamp01(hudY / H) - 0.5f) * 2f * halfH;
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = size;
            gameCamera.transform.SetPositionAndRotation(new Vector3(5.308f, centerY, -24.8f), Quaternion.identity);
            gameCamera.nearClipPlane = 0.1f;
            gameCamera.farClipPlane = 100f;
            gameCamera.projectionMatrix = Matrix4x4.Ortho(-halfW, halfW, -halfH, halfH, gameCamera.nearClipPlane, gameCamera.farClipPlane);
            _fitAspect = gameCamera.aspect;
            _fitSafe = Screen.safeArea;
        }

        /// <summary>Tường vật lý của lòng bàn (theo khung collider gốc, có lỗ phun ở đỉnh được bịt bằng mép y = 6.92).</summary>
        void CreateArenaWalls()
        {
            // (Collider máng gốc "SMALL/BIG CONVEYOR COLLIDERS", "Orbit_Collider" đã xoá khỏi scene:
            //  máng chạy kinematic nên chỉ cần tường lòng bàn dưới đây.)
            var go = new GameObject("ArenaWalls");
            go.transform.SetParent(transform, false);
            var mat = new PhysicsMaterial2D("ArenaBounce") { bounciness = 1f, friction = 0f };
            var edge = go.AddComponent<EdgeCollider2D>();
            edge.sharedMaterial = mat;
            Rect a = config.arena;
            // Đo lại theo mép trong NHÌN THẤY của khung (render 2×): tường dọc x = arena.xMin/xMax (1.07 / 9.53),
            // trần y ≈ 7.18, góc trên bo ~0.25; khe cửa phun x 4.74..5.86 được bịt bởi mép y ≈ 6.92.
            const float top = 7.18f, rc = 0.25f;
            edge.points = new[]
            {
                new Vector2(4.95f, 6.92f), new Vector2(4.74f, top), new Vector2(a.xMin + rc, top), new Vector2(a.xMin, top - rc),
                new Vector2(a.xMin, a.yMin), new Vector2(a.xMax, a.yMin), new Vector2(a.xMax, top - rc), new Vector2(a.xMax - rc, top),
                new Vector2(5.86f, top), new Vector2(5.65f, 6.93f), new Vector2(4.95f, 6.92f)
            };
            edge.edgeRadius = 0.02f;
        }

        // ------------------------------------------------------------------ Vòng lặp
        void Update()
        {
            // ma trận chiếu tự đặt không tự cập nhật khi đổi tỉ lệ màn hình (xoay máy / đổi device trong Simulator)
            if (gameCamera != null && (!Mathf.Approximately(gameCamera.aspect, _fitAspect) || Screen.safeArea != _fitSafe)) FitCamera();
            _ui.SetCapacity(_capacity.Fill, _started, _capacity.Infinite);
            capacityUsed = _capacity.Used;
            capacityMax = _capacity.Max;
            ballsInSystem = _capacity.Used; // bi đã rời khay mà chưa tiêu hao (máng + bàn + hộp đang nhả)
            blocksLeft = _board.DestructibleCount;

            if (state != GameState.Playing) return;
            HandleInput();
            if (autoPlay) AutoPlayStep();
            CheckDeadlock();
        }

        void HandleInput()
        {
            if (_ui.IsPopupOpen) return;
            // chuột hoặc touch (Device Simulator / điện thoại)
            bool down = Input.GetMouseButtonDown(0);
            Vector2 screen = Input.mousePosition;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                down = true;
                screen = Input.GetTouch(0).position;
            }
            if (!down || IsOverUiButton(screen)) return;

            // Camera nghiêng 20° → phải chiếu tia xuống đúng mặt phẳng của vật (không dùng ScreenToWorldPoint)
            Ray ray = gameCamera.ScreenPointToRay(screen);
            Vector2 world = PointOnPlane(ray, -0.6f);

            if (_boosters.Armed == BoosterType.Hammer)
            {
                BoardBlock block = _board.BlockAt(PointOnPlane(ray, 0f));
                if (block != null && block.IsDestructible)
                {
                    _boosters.Consume(BoosterType.Hammer);
                    AudioManager.Play(Sfx.Hammer);
                    _board.DestroyBlock(block);
                }
                return;
            }

            AmmoBox box = _grid.BoxAtWorld(world);
            if (box == null) return;

            bool magic = _boosters.Armed == BoosterType.MagicHand && !box.IsClickable;
            if (!box.IsClickable && !magic) { box.PlayDenied(); return; }
            TryLaunch(box, magic);
        }

        static Vector2 PointOnPlane(Ray ray, float z)
        {
            float t = Mathf.Abs(ray.direction.z) > 1e-5f ? (z - ray.origin.z) / ray.direction.z : 0f;
            Vector3 p = ray.origin + ray.direction * t;
            return new Vector2(p.x, p.y);
        }

        /// <summary>Chỉ chặn bấm xuống game khi con trỏ nằm trên một nút UI thật (không chặn bởi ảnh trang trí).</summary>
        static readonly System.Collections.Generic.List<RaycastResult> UiHits = new System.Collections.Generic.List<RaycastResult>();
        static bool IsOverUiButton(Vector2 screen)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screen };
            UiHits.Clear();
            EventSystem.current.RaycastAll(data, UiHits);
            foreach (RaycastResult r in UiHits)
                if (r.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return true;
            return false;
        }

        bool _overflowPending;

        void TryLaunch(AmmoBox box, bool forced)
        {
            if (_overflowPending) return;
            // Video gốc (lose_coche): hộp làm vượt sức chứa → thanh nhảy kín 100%, hộp bật lên lơ lửng, ~0.6s sau mới hiện Out Of Capacity.
            // Bi của hộp CHƯA được cộng; chỉ khi chọn Play On (tăng sức chứa) hộp mới bay tiếp.
            if (!_capacity.CanAdd(box.Data.Balls))
            {
                _overflowPending = true;
                _started = true;
                _capacity.ForceFull = true;
                box.Hover();
                AudioManager.Play(Sfx.Error);
                StartCoroutine(OutOfCapacityRoutine(box, forced));
                return;
            }
            LaunchNow(box, forced);
        }

        void LaunchNow(AmmoBox box, bool forced)
        {
            if (forced)
            {
                _boosters.Consume(BoosterType.MagicHand);
                AudioManager.Play(Sfx.Magic);
            }
            _started = true;
            _capacity.Add(box.Data.Balls);
            AudioManager.Play(Sfx.TapAmmo);
            AudioManager.Play(Sfx.BoxLaunch);
            _grid.Launch(box, forced);
            _deadlockTimer = 0f;
        }

        IEnumerator OutOfCapacityRoutine(AmmoBox box, bool forced)
        {
            yield return new WaitForSeconds(0.6f);
            if (state != GameState.Playing || box == null) { _overflowPending = false; _capacity.ForceFull = false; yield break; }
            _ui.ShowOutOfCapacity(_level.Difficulty,
                onAddCapacity: () =>
                {
                    _capacity.Increase(0.25f);
                    AudioManager.Play(Sfx.Magic);
                    _capacity.ForceFull = false;
                    _overflowPending = false;
                    LaunchNow(box, forced);
                },
                onGiveUp: () => { _capacity.ForceFull = false; _overflowPending = false; Lose("Out of capacity"); });
        }

        /// <summary>
        /// Thua khi không còn gì làm được: không hộp nào bấm được, máng trống,
        /// và không bi nào trên bàn còn mục tiêu cùng màu (gốc: Capacity.IsInDeadlock).
        /// </summary>
        void CheckDeadlock()
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer < 0.5f) return;
            _idleTimer = 0f;

            bool canAct = _grid.AnyClickable() || _conveyor.IsBusy || _balls.AnyBallHasTarget();
            if (canAct) { _deadlockTimer = 0f; return; }
            _deadlockTimer += 0.5f;
            if (_deadlockTimer >= 2.5f) Lose("No more moves");
        }

        // ------------------------------------------------------------------ Booster
        void HandleBooster(BoosterType type)
        {
            if (state != GameState.Playing) return;
            if (_boosters.Count(type) <= 0)
            {
                _boosters.Add(type, 1); // bản clone: bấm "+" để nhận thêm miễn phí (game gốc mua bằng coin)
                _ui.Toast("+1 " + type);
                return;
            }

            switch (type)
            {
                case BoosterType.Hammer:
                case BoosterType.MagicHand:
                    _boosters.Toggle(type);
                    if (_boosters.Armed == BoosterType.Hammer) _ui.Toast("Tap a block to smash it!");
                    else if (_boosters.Armed == BoosterType.MagicHand) _ui.Toast("Tap any ammo box!");
                    break;
                case BoosterType.Shuffle:
                    _boosters.Consume(type);
                    _grid.ShuffleColors();
                    AudioManager.Play(Sfx.Magic);
                    break;
                case BoosterType.Capacity:
                    _boosters.Consume(type);
                    _capacity.Increase(0.25f);
                    AudioManager.Play(Sfx.Magic);
                    _ui.Toast("Capacity +25%");
                    break;
            }
        }

        void RefreshBoosterUi()
        {
            foreach (BoosterType t in System.Enum.GetValues(typeof(BoosterType)))
                _ui.SetBooster(t, _boosters.Count(t), _boosters.Armed == t);
        }

        // ------------------------------------------------------------------ Kết thúc
        void HandleWin()
        {
            if (state != GameState.Playing) return;
            state = GameState.Won;
            AudioManager.Play(Sfx.Win);
            AudioManager.Play(Sfx.Confetti);
            int next = Mathf.Min(currentLevel + 1, config.maxLevel);
            PlayerPrefs.SetInt(SaveKey, next);
            StartCoroutine(WinRoutine(next));
        }

        IEnumerator WinRoutine(int next)
        {
            yield return new WaitForSeconds(0.35f);
            Vfx.Spawn(config.confettiVfx, new Vector3(config.arena.center.x, 2f, -3f), Color.white, 2.5f);
            _conveyor.ClearAll();
            _balls.ClearAll();
            yield return _ui.PlayWinSequence(currentLevel, _level.Difficulty, 50, () => LoadLevel(next));
        }

        void Lose(string reason)
        {
            if (state != GameState.Playing) return;
            state = GameState.Lost;
            AudioManager.Play(Sfx.Lose);
            _ui.ShowFail(currentLevel, _level.Difficulty, reason, Restart);
        }

        float _autoTimer;
        void AutoPlayStep()
        {
            _autoTimer -= Time.deltaTime;
            if (_autoTimer > 0f || _ui.IsPopupOpen) return;
            _autoTimer = 0.6f;
            foreach (AmmoBox b in FindObjectsOfType<AmmoBox>())
            {
                if (!b.IsClickable || b.IsLaunched || !_capacity.CanAdd(b.Data.Balls)) continue;
                TryLaunch(b, false);
                return;
            }
        }

        // Cheat/debug trong Inspector (chuột phải component)
        [ContextMenu("Debug/Win now")] void DebugWin() { foreach (BoardBlock b in new System.Collections.Generic.List<BoardBlock>(_board.Blocks)) _board.DestroyBlock(b); }
        [ContextMenu("Debug/Next level")] void DebugNext() => LoadLevel(currentLevel + 1);
        [ContextMenu("Debug/Prev level")] void DebugPrev() => LoadLevel(currentLevel - 1);
    }
}
