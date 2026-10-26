using UnityEngine;
using BounceBlast.Gameplay;

namespace BounceBlast.Cheats
{
    /// <summary>Cổng vào của công cụ cheat</summary>
    public class CheatHost : MonoBehaviour
    {
        [SerializeField] CheatPanelView panel;
        [SerializeField] CheatPerfOverlay overlay;
        [Tooltip("Vùng chạm 5 lần: phần ngang từ mép trái và phần dọc từ mép trên (0..1 của màn hình)")]
        [SerializeField, Range(0.1f, 0.6f)] float cornerWidth = 0.35f;
        [SerializeField, Range(0.04f, 0.3f)] float cornerHeight = 0.1f;
        [SerializeField] int tapsToOpen = 5;
        [SerializeField] float tapWindow = 3f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS
        int _taps;
        float _firstTapTime, _lastToggle;
        bool _ready;

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
            if (Time.unscaledTime - _lastToggle < 0.5f) return;
            _lastToggle = Time.unscaledTime;
            panel.Toggle();
        }
#else
        void Awake() => Destroy(gameObject);
#endif
    }
}
