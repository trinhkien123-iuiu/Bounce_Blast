using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Cổng vào của công cụ cheat (gắn trên gốc prefab CheatPanel, đặt sẵn trong scene Gameplay).
    /// - Chỉ hoạt động ở bản DEV (Editor / Development Build) hoặc khi bật <see cref="allowInReleaseBuild"/>; bản phát hành tự huỷ object.
    /// - Mở panel: chạm 5 lần liên tiếp vào góc trên-trái màn hình, hoặc chạm 3 ngón cùng lúc (Editor: phím F1).
    /// Thiết kế đầy đủ: Docs/design/cheat-tool.md.
    /// </summary>
    public class CheatHost : MonoBehaviour
    {
        [SerializeField] CheatPanelView panel;
        [SerializeField] CheatPerfOverlay overlay;
        [Tooltip("Cho phép cả ở bản Release (chỉ bật khi muốn thử cheat trên bản không Development)")] [SerializeField] bool allowInReleaseBuild;
        [Tooltip("Vùng chạm 5 lần: phần ngang từ mép trái và phần dọc từ mép trên (0..1 của màn hình)")]
        [SerializeField, Range(0.1f, 0.6f)] float cornerWidth = 0.35f;
        [SerializeField, Range(0.04f, 0.3f)] float cornerHeight = 0.1f;
        [SerializeField] int tapsToOpen = 5;
        [SerializeField] float tapWindow = 3f;

        int _taps;
        float _firstTapTime, _lastToggle;
        bool _ready;

        void Awake()
        {
            if (!Debug.isDebugBuild && !Application.isEditor && !allowInReleaseBuild) { Destroy(gameObject); return; }
        }

        void Start()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || panel == null) { enabled = false; return; }
            panel.SetItems(CheatRegistry.Build(gm, overlay));
            if (gm.levelManager != null) gm.levelManager.OnLevelLoaded += _ => CheatRegistry.ReapplyPersistent(gm);
            _ready = true;
        }

        void Update()
        {
            if (!_ready) return;
            if (Input.GetKeyDown(KeyCode.F1)) Toggle();

            if (Input.touchCount >= 3 && Input.GetTouch(Input.touchCount - 1).phase == TouchPhase.Began) { Toggle(); return; }

            bool down = Input.GetMouseButtonDown(0);
            Vector2 pos = Input.mousePosition;
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) { down = true; pos = Input.GetTouch(0).position; }
            if (!down || panel.IsOpen) return;

            bool inCorner = pos.x < Screen.width * cornerWidth && pos.y > Screen.height * (1f - cornerHeight);
            float now = Time.unscaledTime;
            if (!inCorner || now - _firstTapTime > tapWindow) { _taps = 0; }
            if (!inCorner) return;
            if (_taps == 0) _firstTapTime = now;
            if (++_taps >= tapsToOpen) { _taps = 0; Toggle(); }
        }

        void Toggle()
        {
            if (Time.unscaledTime - _lastToggle < 0.5f) return;   // chống kích hoạt kép (3 ngón + chạm góc)
            _lastToggle = Time.unscaledTime;
            panel.Toggle();
        }
    }
}
