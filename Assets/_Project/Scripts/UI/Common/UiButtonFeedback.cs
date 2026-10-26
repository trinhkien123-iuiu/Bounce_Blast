using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BounceBlast.UI
{
    /// <summary>Phản hồi chung cho MỌI nút UI</summary>
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
        float _scale = 1f, _velocity;
        float _target = 1f;
        bool _down, _over;
        bool _resting = true;

        void Awake()
        {
            _selectable = GetComponent<Selectable>();
            var appear = GetComponent<UiAppear>();
            _baseScale = appear != null ? appear.BaseScale : transform.localScale;
        }

        void OnEnable()
        {
            _scale = _target = 1f; _velocity = 0f; _down = _over = false;
            _resting = true;
        }

        void OnDisable()
        {
            if (!_resting) transform.localScale = _baseScale;
            _resting = true;
        }

        bool Active => _selectable == null || (_selectable.IsInteractable() && isActiveAndEnabled);

        /// <summary>Xử lý nhấn xuống nút</summary>
        public void OnPointerDown(PointerEventData e) { if (!Active) return; _down = true; Refresh(); }
        /// <summary>Xử lý thả nút</summary>
        public void OnPointerUp(PointerEventData e) { _down = false; Refresh(); }
        /// <summary>Xử lý con trỏ vào nút</summary>
        public void OnPointerEnter(PointerEventData e) { _over = true; Refresh(); }
        /// <summary>Xử lý con trỏ rời nút</summary>
        public void OnPointerExit(PointerEventData e) { _over = false; _down = false; Refresh(); }

        /// <summary>Bấm xong: nảy bật lên một cái</summary>
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
            float a = stiffness * (_target - _scale) - damping * _velocity;
            _velocity += a * dt;
            _scale += _velocity * dt;
            if (Mathf.Abs(_scale - _target) < 0.0005f && Mathf.Abs(_velocity) < 0.005f) { _scale = _target; _velocity = 0f; }
            bool rest = _scale == 1f && _velocity == 0f && _target == 1f;
            if (rest && _resting) return;
            transform.localScale = _baseScale * _scale;
            _resting = rest;
        }
    }
}
