using UnityEngine;

namespace BounceBlast.UI
{
    /// <summary>
    /// Hiệu ứng xuất hiện của một thành phần UI khi trang chứa nó được mở (do <see cref="PageTransition"/> điều khiển):
    /// mờ → rõ, nảy từ <see cref="fromScale"/> lên đúng cỡ, trượt từ vị trí lệch <see cref="fromOffset"/> về đúng chỗ.
    /// Mọi giá trị tính TỪ cỡ / vị trí trong prefab (ghi ở Awake) – đứng yên luôn đúng như prefab, chỉnh UI không bị đè.
    /// Cần CanvasGroup để làm mờ (tự có khi gắn component trong Editor).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [DisallowMultipleComponent]
    public class UiAppear : MonoBehaviour
    {
        [Tooltip("Giây chạy hiệu ứng")] [SerializeField] float duration = 0.35f;
        [Tooltip("Trễ thêm riêng cho thành phần này (cộng vào độ trễ so le của PageTransition)")] [SerializeField] float extraDelay;
        [Tooltip("Bắt đầu lệch bao nhiêu so với vị trí prefab (đơn vị canvas). VD (0,-60) = từ dưới trượt lên")] [SerializeField] Vector2 fromOffset = new Vector2(0f, -60f);
        [Tooltip("Bắt đầu từ cỡ bao nhiêu lần cỡ prefab")] [SerializeField] float fromScale = 0.85f;
        [Tooltip("Có làm mờ → rõ không")] [SerializeField] bool fade = true;
        [Tooltip("Nảy quá đà một chút khi về cỡ thật (EaseOutBack)")] [SerializeField] bool overshoot = true;
        [Tooltip("Đổi chiều X của fromOffset theo hướng chuyển trang: sang trang bên phải thì vào từ phải, sang trang bên trái thì vào từ trái")] [SerializeField] bool followDirection;

        RectTransform _rt;
        CanvasGroup _group;
        Vector2 _basePos;
        Vector3 _baseScale;
        float _baseAlpha = 1f;
        bool _saved, _running;
        float _t, _delay;
        Vector2 _offset;   // fromOffset đã đổi chiều theo hướng chuyển trang

        void Awake() => Save();

        /// <summary>Cỡ trong prefab (không phải cỡ đang thu nhỏ giữa hiệu ứng).</summary>
        public Vector3 BaseScale { get { Save(); return _baseScale; } }

        void Save()
        {
            if (_saved) return;
            _rt = transform as RectTransform;
            _group = GetComponent<CanvasGroup>();
            if (_rt != null) _basePos = _rt.anchoredPosition;
            _baseScale = transform.localScale;
            if (_group != null) _baseAlpha = _group.alpha;
            _saved = true;
        }

        /// <summary>Về trạng thái bắt đầu (ẩn) – gọi khi trang vừa lộ ra, trước khi chạy.
        /// <paramref name="dir"/>: +1 = trang mới nằm bên phải trang cũ, −1 = bên trái, 0 = không rõ.</summary>
        public void Prepare(int dir = 0)
        {
            Save();
            SetDir(dir);
            _running = false;
            Apply(0f);
        }

        /// <summary>Chạy hiệu ứng sau <paramref name="delay"/> giây.</summary>
        public void Play(float delay, int dir = 0)
        {
            Save();
            SetDir(dir);
            _delay = delay + extraDelay;
            _t = 0f;
            _running = true;
            Apply(0f);
        }

        /// <summary>Về đúng như prefab ngay (dừng hiệu ứng).</summary>
        public void Finish()
        {
            if (!_saved) return;
            _running = false;
            Apply(1f);
        }

        void OnDisable() => Finish();

        void SetDir(int dir)
        {
            _offset = fromOffset;
            if (followDirection && dir != 0) _offset.x = Mathf.Abs(fromOffset.x) * Mathf.Sign(dir);
        }

        void Update()
        {
            if (!_running) return;
            float dt = Time.unscaledDeltaTime;
            if (_delay > 0f) { _delay -= dt; return; }
            _t += dt / Mathf.Max(0.01f, duration);
            if (_t >= 1f) { Finish(); return; }
            Apply(_t);
        }

        void Apply(float k)
        {
            float move = 1f - (1f - k) * (1f - k) * (1f - k);                    // ease-out cubic
            float pop = overshoot ? EaseOutBack(k) : move;
            if (_rt != null) _rt.anchoredPosition = _basePos + _offset * (1f - move);
            transform.localScale = _baseScale * Mathf.LerpUnclamped(fromScale, 1f, pop);
            if (_group != null && fade) _group.alpha = _baseAlpha * Mathf.Clamp01(k * 2.5f);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }
    }
}
