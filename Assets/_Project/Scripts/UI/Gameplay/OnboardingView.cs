using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của prefab OnboardingCanvas</summary>
    [DisallowMultipleComponent]
    public class OnboardingView : MonoBehaviour
    {
        [SerializeField] Canvas canvas;
        [Tooltip("Nền tối phủ toàn màn (RawImage + material Spotlight)")] [SerializeField] RawImage dim;
        [Tooltip("Bàn tay chỉ – pivot đặt ở đầu ngón trỏ")] [SerializeField] RectTransform hand;
        [SerializeField] TMP_Text message;
        [Tooltip("Dòng \"Tap To Continue\" nhấp nháy ở dưới")] [SerializeField] TMP_Text continueHint;
        [Tooltip("Độ tối của nền (0..1)")] [SerializeField, Range(0f, 1f)] float dimAlpha = 0.72f;
        [Tooltip("Độ mờ viền lỗ sáng (pixel màn hình)")] [SerializeField] float softPixels = 28f;
        [Tooltip("Biên độ tay gõ (đơn vị canvas)")] [SerializeField] float tapAmplitude = 46f;

        Material _mat;
        RectTransform _root;
        Vector2 _center, _half;
        float _round, _fade;
        bool _spot, _handOn;
        Vector2 _handTarget;

        static readonly int CenterId = Shader.PropertyToID("_Center");
        static readonly int HalfId = Shader.PropertyToID("_Half");
        static readonly int RoundId = Shader.PropertyToID("_Round");
        static readonly int SoftId = Shader.PropertyToID("_Soft");
        static readonly int ScreenId = Shader.PropertyToID("_ScreenSize");
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public bool IsShown => gameObject.activeSelf;

        void Awake()
        {
            _root = canvas != null ? canvas.GetComponent<RectTransform>() : (RectTransform)transform;
            if (dim != null) { _mat = new Material(dim.material); dim.material = _mat; }
        }

        /// <summary>Bật lớp hướng dẫn. radius/half theo pixel màn hình</summary>
        public void Show(string text, Vector2 screenPos, Vector2 halfSizePx, float roundPx, bool spot, bool showHand, bool showContinue, float textOffsetY)
        {
            gameObject.SetActive(true);
            _spot = spot; _handOn = showHand;
            _center = screenPos; _half = halfSizePx; _round = roundPx;
            if (dim != null) dim.gameObject.SetActive(spot);
            if (hand != null) hand.gameObject.SetActive(showHand);
            if (continueHint != null) continueHint.gameObject.SetActive(showContinue);
            if (message != null)
            {
                message.text = text;
                message.gameObject.SetActive(!string.IsNullOrEmpty(text));
                message.rectTransform.anchoredPosition = ToCanvas(new Vector2(Screen.width * 0.5f, screenPos.y + textOffsetY));
            }
            _handTarget = ToCanvas(screenPos);
            StartCoroutine(FadeIn());
        }

        /// <summary>Dời lỗ sáng / bàn tay tới vị trí mới</summary>
        public void Move(Vector2 screenPos)
        {
            _center = screenPos;
            _handTarget = ToCanvas(screenPos);
        }

        /// <summary>Xử lý tắt lớp hướng dẫn</summary>
        public void Hide()
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
        }

        IEnumerator FadeIn()
        {
            for (_fade = 0f; _fade < 1f; _fade += Time.unscaledDeltaTime / 0.25f) yield return null;
            _fade = 1f;
        }

        /// <summary>Đổi toạ độ màn hình → toạ độ trong canvas</summary>
        Vector2 ToCanvas(Vector2 screen)
        {
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, cam, out local);
            return local;
        }

        void Update()
        {
            if (_spot && _mat != null)
            {
                _mat.SetVector(ScreenId, new Vector4(Screen.width, Screen.height, 0f, 0f));
                _mat.SetVector(CenterId, new Vector4(_center.x, _center.y, 0f, 0f));
                _mat.SetVector(HalfId, new Vector4(_half.x, _half.y, 0f, 0f));
                _mat.SetFloat(RoundId, _round);
                _mat.SetFloat(SoftId, softPixels);
                _mat.SetColor(ColorId, new Color(0f, 0f, 0f, dimAlpha * _fade));
            }
            if (_handOn && hand != null)
            {
                float k = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.4f));
                hand.anchoredPosition = _handTarget + new Vector2(tapAmplitude * 0.5f, -tapAmplitude * 0.4f) * k;
                hand.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.05f, k);
            }
            if (continueHint != null && continueHint.gameObject.activeSelf)
            {
                Color c = continueHint.color; c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.4f)); continueHint.color = c;
            }
        }

        /// <summary>Kiểm tra các ô tham chiếu còn trống</summary>
        public string Validate()
        {
            var miss = new System.Collections.Generic.List<string>();
            if (canvas == null) miss.Add("canvas");
            if (dim == null) miss.Add("dim");
            if (hand == null) miss.Add("hand");
            if (message == null) miss.Add("message");
            if (continueHint == null) miss.Add("continueHint");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
