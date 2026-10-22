using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BounceBlast.UI
{
    /// <summary>
    /// Phản hồi chung cho MỌI nút UI: lún nhỏ khi nhấn, nảy bật lên khi thả/bấm, nở nhẹ khi rê chuột (chỉ có tác dụng trên máy tính).
    /// Gắn cạnh <see cref="Button"/> (menu <c>Tools ▸ Bounce Blast ▸ UI ▸ Thêm phản hồi cho mọi nút</c> gắn hàng loạt vào các prefab).
    /// Dùng thời gian không phụ thuộc timeScale nên popup (lúc game tạm dừng) vẫn có phản hồi.
    /// Không đổi logic bấm: chỉ chạm vào <c>localScale</c> của chính object này, và CHỈ khi đang lún / nảy –
    /// đứng yên thì không đặt scale (để <see cref="UiAppear"/> hay code trạng thái khác trên cùng object không bị đè).
    /// Cỡ gốc ghi MỘT lần (lấy từ UiAppear nếu có – lúc trang vừa lộ ra UiAppear đang thu nhỏ object).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public class UiButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [Header("Cỡ khi...")]
        [Tooltip("Đang nhấn giữ")] [SerializeField, Range(0.7f, 1f)] float pressScale = 0.9f;
        [Tooltip("Rê chuột lên (máy tính)")] [SerializeField, Range(1f, 1.2f)] float hoverScale = 1.05f;
        [Tooltip("Đỉnh cú nảy sau khi bấm")] [SerializeField, Range(1f, 1.3f)] float bounceScale = 1.12f;

        [Header("Chuyển động")]
        [Tooltip("Độ cứng lò xo (cao = đáp ứng nhanh)")] [SerializeField] float stiffness = 260f;
        [Tooltip("Giảm chấn (thấp = nảy nhiều)")] [SerializeField] float damping = 16f;

        Selectable _selectable;
        Vector3 _baseScale = Vector3.one;
        float _scale = 1f, _velocity;     // hệ số nhân lên scale gốc
        float _target = 1f;
        bool _down, _over;
        bool _resting = true;   // đứng yên ở cỡ gốc → không đặt scale

        void Awake()
        {
            _selectable = GetComponent<Selectable>();
            var appear = GetComponent<UiAppear>();
            _baseScale = appear != null ? appear.BaseScale : transform.localScale;   // prefab có nút scale ≠ 1 (VD 1.2): giữ làm gốc
        }

        void OnEnable()
        {
            _scale = _target = 1f; _velocity = 0f; _down = _over = false;
            _resting = true;
        }

        void OnDisable()
        {
            if (!_resting) transform.localScale = _baseScale;   // đang lún / nảy dở thì trả về cỡ gốc
            _resting = true;
        }

        bool Active => _selectable == null || (_selectable.IsInteractable() && isActiveAndEnabled);

        public void OnPointerDown(PointerEventData e) { if (!Active) return; _down = true; Refresh(); }
        public void OnPointerUp(PointerEventData e) { _down = false; Refresh(); }
        public void OnPointerEnter(PointerEventData e) { _over = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { _over = false; _down = false; Refresh(); }

        /// <summary>Bấm xong: nảy bật lên một cái (vận tốc ban đầu hướng lên) rồi lò xo kéo về cỡ nghỉ.</summary>
        public void OnPointerClick(PointerEventData e)
        {
            if (!Active) return;
            _velocity = (bounceScale - 1f) * 14f;
        }

        void Refresh() => _target = _down ? pressScale : (_over ? hoverScale : 1f);

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (dt <= 0f) return;
            // lò xo giảm chấn: a = k·(target − x) − c·v
            float a = stiffness * (_target - _scale) - damping * _velocity;
            _velocity += a * dt;
            _scale += _velocity * dt;
            if (Mathf.Abs(_scale - _target) < 0.0005f && Mathf.Abs(_velocity) < 0.005f) { _scale = _target; _velocity = 0f; }
            bool rest = _scale == 1f && _velocity == 0f && _target == 1f;
            if (rest && _resting) return;            // đứng yên: không đụng scale
            transform.localScale = _baseScale * _scale;
            _resting = rest;
        }
    }
}
