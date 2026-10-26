using UnityEngine;
using UnityEngine.EventSystems;

namespace BounceBlast.UI
{
    /// <summary>Hiệu ứng rê chuột cho nút</summary>
    [DisallowMultipleComponent]
    public class UiHoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Tooltip("Nhấc lên bao nhiêu đơn vị canvas khi rê chuột vào")] [SerializeField] float lift = 8f;
        [Tooltip("Tốc độ nhấc / hạ (cao = nhanh)")] [SerializeField] float speed = 18f;
        [Tooltip("Vệt sáng lướt qua một lượt khi rê chuột vào – để trống = không có")] [SerializeField] UiShineSweep shine;

        RectTransform _rt;
        Vector2 _basePos;
        float _cur, _target;
        bool _saved;

        void Awake() => Save();

        void Save()
        {
            if (_saved) return;
            _rt = transform as RectTransform;
            if (_rt != null) _basePos = _rt.anchoredPosition;
            _saved = true;
        }

        /// <summary>Xử lý sáng lên khi con trỏ vào</summary>
        public void OnPointerEnter(PointerEventData e)
        {
            Save();
            _target = 1f;
            if (shine != null) shine.SweepOnce();
        }

        /// <summary>Xử lý tắt sáng khi con trỏ rời</summary>
        public void OnPointerExit(PointerEventData e) => _target = 0f;

        void OnDisable()
        {
            _target = _cur = 0f;
            if (_saved && _rt != null) _rt.anchoredPosition = _basePos;
        }

        void Update()
        {
            if (_rt == null || Mathf.Approximately(_cur, _target)) return;
            _cur = Mathf.MoveTowards(_cur, _target, speed * Time.unscaledDeltaTime * Mathf.Max(0.1f, Mathf.Abs(_target - _cur)) + 0.001f);
            float e = _cur * _cur * (3f - 2f * _cur);
            _rt.anchoredPosition = _basePos + new Vector2(0f, lift * e);
        }
    }
}
