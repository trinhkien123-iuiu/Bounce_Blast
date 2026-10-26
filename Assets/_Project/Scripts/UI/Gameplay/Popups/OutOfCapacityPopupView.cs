using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của popup HẾT SỨC CHỨA ("OUT OF CAPACITY") – gắn trên gốc OutOfCapacityPopup.prefab.
    /// Prefab này bọc (lồng) panel gốc OutOfCapacityPanel trong một Canvas riêng, nên Canvas không còn phải dựng bằng code.
    /// GameUI chỉ gọi <see cref="Show"/> và nghe 2 sự kiện.
    ///
    /// Chuyển cảnh đo từ video gốc (lose_coche.mp4, 30 fps, f540 = bắt đầu tối nền):
    ///  - nền tối dần ~0,3 s (ease-out);
    ///  - 0,30 s: nút Play On trượt vào từ mép TRÁI, Give Up từ mép PHẢI (~0,4 s);
    ///  - 0,37 s: tiếng fail 2 (khớp âm thanh gốc, dài ~1,9 s);
    ///  - 0,47 s: chữ OUT OF CAPACITY rơi từ trên xuống (~0,23 s) rồi đứng yên.
    /// </summary>
    [DisallowMultipleComponent]
    public class OutOfCapacityPopupView : PopupViewBase
    {
        [Header("Riêng popup Hết sức chứa")]
        [Tooltip("Nút \"Add Capacity / Play On\" – tăng sức chứa rồi chơi tiếp")] [SerializeField] Button playOnButton;
        [Tooltip("Nút \"Give Up\" – bỏ cuộc (thua)")] [SerializeField] Button giveUpButton;

        [Header("Chuyển cảnh (đo từ video gốc)")]
        [Tooltip("Nền tối (Panel/Dim) – alpha trong prefab là độ tối cuối")] [SerializeField] Image dim;
        [Tooltip("Cả cụm Play On (nút + giá) – trượt vào từ trái")] [SerializeField] RectTransform playOnGroup;
        [Tooltip("Nút Give Up – trượt vào từ phải")] [SerializeField] RectTransform giveUpGroup;
        [Tooltip("Chữ OUT OF CAPACITY – rơi từ trên xuống")] [SerializeField] RectTransform title;
        [SerializeField] float dimTime = 0.3f;
        [SerializeField] float buttonsDelay = 0.30f;
        [SerializeField] float buttonsTime = 0.4f;
        [SerializeField] float soundDelay = 0.37f;
        [SerializeField] float titleDelay = 0.47f;
        [SerializeField] float titleTime = 0.23f;

        /// <summary>Chọn Play On: tăng sức chứa và cho hộp bay tiếp.</summary>
        public event Action OnPlayOn;
        /// <summary>Chọn Give Up: thua.</summary>
        public event Action OnGiveUp;

        // vị trí / alpha trong prefab (hiệu ứng chỉ dời quanh các giá trị này)
        readonly Dictionary<RectTransform, Vector2> _rest = new Dictionary<RectTransform, Vector2>();
        float _dimAlpha = -1f;
        Coroutine _intro;

        protected override bool UsesPopIn => false;

        void Awake()
        {
            if (playOnButton != null) playOnButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnPlayOn?.Invoke(); });
            if (giveUpButton != null) giveUpButton.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnGiveUp?.Invoke(); });
        }

        public void Show(LevelDifficulty difficulty)
        {
            OpenBase(difficulty);
            if (_intro != null) StopCoroutine(_intro);
            _intro = StartCoroutine(Intro());
        }

        Vector2 Rest(RectTransform rt)
        {
            Vector2 v;
            if (!_rest.TryGetValue(rt, out v)) _rest[rt] = v = rt.anchoredPosition;
            return v;
        }

        static float EaseOutQuad(float k) => 1f - (1f - k) * (1f - k);
        static float EaseOutCubic(float k) => 1f - Mathf.Pow(1f - k, 3f);

        IEnumerator Intro()
        {
            var canvasRect = Canvas.GetComponent<RectTransform>();
            float w = canvasRect.rect.width, h = canvasRect.rect.height;
            if (dim != null && _dimAlpha < 0f) _dimAlpha = dim.color.a;
            Vector2 playRest = playOnGroup != null ? Rest(playOnGroup) : Vector2.zero;
            Vector2 giveRest = giveUpGroup != null ? Rest(giveUpGroup) : Vector2.zero;
            Vector2 titleRest = title != null ? Rest(title) : Vector2.zero;

            // trạng thái đầu: nền trong suốt, nút ngoài màn hình, chữ ở trên cao
            SetDim(0f);
            if (playOnGroup != null) playOnGroup.anchoredPosition = playRest - new Vector2(w, 0f);
            if (giveUpGroup != null) giveUpGroup.anchoredPosition = giveRest + new Vector2(w, 0f);
            if (title != null) { title.anchoredPosition = titleRest + new Vector2(0f, h * 0.35f); title.gameObject.SetActive(false); }

            bool soundDone = false;
            float t0 = Time.unscaledTime, total = Mathf.Max(titleDelay + titleTime, buttonsDelay + buttonsTime) + 0.05f;
            while (true)
            {
                float t = Time.unscaledTime - t0;
                SetDim(EaseOutQuad(Mathf.Clamp01(t / dimTime)));
                float kb = Mathf.Clamp01((t - buttonsDelay) / buttonsTime);
                if (playOnGroup != null) playOnGroup.anchoredPosition = playRest - new Vector2(w * (1f - EaseOutCubic(kb)), 0f);
                if (giveUpGroup != null) giveUpGroup.anchoredPosition = giveRest + new Vector2(w * (1f - EaseOutCubic(kb)), 0f);
                if (!soundDone && t >= soundDelay) { soundDone = true; AudioManager.Play(Sfx.OutOfCapacity); }
                if (title != null)
                {
                    float kt = Mathf.Clamp01((t - titleDelay) / titleTime);
                    if (t >= titleDelay && !title.gameObject.activeSelf) title.gameObject.SetActive(true);
                    // gần như đều tay rồi dừng hẳn (đo từ frame: ~41 px/khung)
                    float e = 0.75f * kt + 0.25f * EaseOutQuad(kt);
                    title.anchoredPosition = titleRest + new Vector2(0f, h * 0.35f * (1f - e));
                }
                if (t >= total) break;
                yield return null;
            }
            if (playOnGroup != null) playOnGroup.anchoredPosition = playRest;
            if (giveUpGroup != null) giveUpGroup.anchoredPosition = giveRest;
            if (title != null) { title.anchoredPosition = titleRest; title.gameObject.SetActive(true); }
            SetDim(1f);
            _intro = null;
        }

        void SetDim(float k)
        {
            if (dim == null) return;
            Color c = dim.color; c.a = _dimAlpha * k; dim.color = c;
        }

        protected override void CollectMissing(List<string> missing)
        {
            if (playOnButton == null) missing.Add("playOnButton");
            if (giveUpButton == null) missing.Add("giveUpButton");
        }
    }
}
