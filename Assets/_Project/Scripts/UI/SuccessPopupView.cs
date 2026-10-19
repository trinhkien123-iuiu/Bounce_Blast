using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của popup THẮNG – gắn trên gốc SuccessCanvas.prefab. Nó SỞ HỮU toàn bộ chuỗi màn thắng:
    ///   1) màn logo "BOUNCE BLAST" (chữ bật lên lần lượt) + pháo giấy + pháo hoa;
    ///   2) bảng thắng nảy lên, đống xu rơi xuống, chớp sáng + tia, chữ PERFECT!, sao lấp lánh, vệt sáng quét nút.
    /// Mọi node cần thiết giữ bằng [SerializeField] (không Find theo tên). GameUI chỉ gọi <see cref="Play"/> và nghe <see cref="OnContinue"/>.
    ///
    /// Mọi hình tĩnh (cỡ chữ logo, chỗ đứng đống xu, quầng sáng, chớp sáng, lớp sao, vệt sáng nút, pháo hoa, mưa pháo)
    /// nằm SẴN trong prefab – sửa trong Hierarchy; code chỉ bật/tắt theo trình tự và diễn hoạt theo thời gian.
    /// Popup DÙNG LẠI qua nhiều màn thắng: trạng thái đổi trong lúc chạy luôn tính từ giá trị prefab ghi lúc Awake, không cộng dồn.
    /// </summary>
    [DisallowMultipleComponent]
    public class SuccessPopupView : PopupViewBase
    {
        /// <summary>Một chữ của logo: nền + chữ chính (b, o, u, n, c, e).</summary>
        [Serializable]
        public class LogoLetter
        {
            public Transform background;
            public Transform glyph;
        }

        [Header("Bảng thắng")]
        [Tooltip("Bảng thắng (Container v2) – ẩn trong màn logo, nảy lên sau đó")] [SerializeField] Transform container;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text prizeText;
        [SerializeField] Button continueButton;
        [SerializeField] Button x2Button;
        [Tooltip("Nền đen che bàn chơi trong màn logo (Dim)")] [SerializeField] Image dim;

        [Header("Màn logo BOUNCE BLAST")]
        [SerializeField] Transform logoArea;
        [SerializeField] Transform logo;
        [Tooltip("Node chứa các chữ (Logo/Animation)")] [SerializeField] Transform logoAnimation;
        [SerializeField] Transform logoBadge;
        [SerializeField] Transform logoBlast;
        [Tooltip("Theo thứ tự b, o, u, n, c, e")] [SerializeField] LogoLetter[] logoLetters = new LogoLetter[0];

        [Header("Pháo")]
        [Tooltip("Cha của các pháo nổ (LogoArea/Confetti)")] [SerializeField] GameObject confettiRoot;
        [Tooltip("Các pháo nổ (UiConfetti, mode Burst) – nổ lần lượt")] [SerializeField] UiConfetti[] confettiBursts = new UiConfetti[0];
        [Tooltip("Giây trễ của pháo nổ đầu tiên (lúc chữ BLAST bật lên)")] [SerializeField] float burstDelay = 0.95f;
        [Tooltip("Giây giữa hai pháo nổ")] [SerializeField] float burstStep = 0.15f;
        [Tooltip("Mưa pháo giấy phủ toàn màn (UiConfetti, mode Rain)")] [SerializeField] UiConfetti confettiRain;
        [SerializeField] float rainDelay = 1f;
        [Tooltip("Tắt mưa sau bao giây (tắt chứ không huỷ – popup dùng lại)")] [SerializeField] float rainHideAfter = 6f;
        [Tooltip("Pháo hoa SF_Basic (ParticleSystem gốc)")] [SerializeField] ParticleSystem fireworks;

        [Header("Hiệu ứng bảng thắng")]
        [Tooltip("Quầng sáng sau xu (ảnh thường, alpha blend)")] [SerializeField] Graphic areaGlow;
        [Tooltip("Độ trong suốt của quầng sáng (nhịp thở ±15%)")] [SerializeField, Range(0.1f, 1f)] float areaGlowAlpha = 0.5f;
        [Tooltip("Đống xu (Coin/coinIcon) – vị trí/cỡ trong prefab là chỗ dừng cuối")] [SerializeField] Transform coinIcon;
        [Tooltip("Đống xu bắt đầu rơi từ cao bao nhiêu (đơn vị canvas)")] [SerializeField] float coinDropHeight = 290f;
        [Tooltip("Lúc bắt đầu rơi đống xu to gấp mấy lần")] [SerializeField] float coinDropScale = 1.9f;
        [Tooltip("Khối chữ PERFECT! (tên gốc OutOfCapsity)")] [SerializeField] Transform perfect;
        [Tooltip("Các hệ hạt nổ cần bật + chạy khi bảng hiện (Coin/ExploteParticle, Coin/Fx_Star, ExploteParticle (1))")]
        [SerializeField] Transform[] burstEffects = new Transform[0];
        [Tooltip("Chớp sáng tròn sau xu (lưu tắt)")] [SerializeField] Graphic coinFlash;
        [Tooltip("Lõi chớp sáng trắng phủ bục (lưu tắt)")] [SerializeField] Graphic coinFlashCore;
        [Tooltip("Tia sáng toả sau xu (lưu tắt)")] [SerializeField] Graphic coinRays;
        [Tooltip("Sao lấp lánh quanh xu (Coin/SparkleLayer)")] [SerializeField] UiSparkles sparkles;
        [Tooltip("Vệt sáng quét qua nút Continue / x2")] [SerializeField] UiShineSweep[] shines = new UiShineSweep[0];

        /// <summary>Người chơi bấm Continue hoặc x2.</summary>
        public event Action OnContinue;

        Vector3 _coinPos, _coinScale = Vector3.one;   // chỗ dừng cuối của đống xu = giá trị trong prefab

        protected override bool UsesPopIn => false;

        void Awake()
        {
            if (coinIcon != null) { _coinPos = coinIcon.localPosition; _coinScale = coinIcon.localScale; }
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (x2Button != null) x2Button.onClick.AddListener(Continue);
        }

        void Update()
        {
            // quầng sáng thở nhẹ (thời gian tuyệt đối, không phụ thuộc fps/timeScale)
            if (areaGlow != null && areaGlow.gameObject.activeInHierarchy)
            {
                Color c = areaGlow.color;
                c.a = areaGlowAlpha * (1f + 0.15f * Mathf.Sin(Time.unscaledTime * 2.2f));
                areaGlow.color = c;
            }
        }

        void Continue()
        {
            AudioManager.Play(Sfx.Button);
            OnContinue?.Invoke();
        }

        /// <summary>
        /// Chạy cả chuỗi màn thắng. GameManager chờ coroutine này xong (yield return) rồi mới sang màn kế.
        /// </summary>
        /// <param name="fireworksHold">giây giữ màn logo + pháo hoa sau khi chữ BLAST bật xong (GameConfig.winFireworksTime)</param>
        public IEnumerator Play(int level, LevelDifficulty difficulty, int coins, float fireworksHold)
        {
            OpenBase(difficulty);
            if (container != null) container.gameObject.SetActive(false);
            if (dim != null) dim.gameObject.SetActive(true);

            // ---- 1) màn logo ----
            if (logoArea != null && logo != null)
            {
                logoArea.gameObject.SetActive(true);
                logo.gameObject.SetActive(true);
                PlayLogoEffects();
                yield return AnimateLogo();
            }
            // giữ màn logo + pháo hoa thêm một lúc cho kịp ngắm; rải thêm 2 tiếng pháo nổ trong lúc pháo hoa bắn
            yield return new WaitForSecondsRealtime(fireworksHold * 0.25f);
            AudioManager.Play(Sfx.Confetti);
            yield return new WaitForSecondsRealtime(fireworksHold * 0.35f);
            AudioManager.Play(Sfx.Confetti);
            yield return new WaitForSecondsRealtime(fireworksHold * 0.4f);
            if (logoArea != null) logoArea.gameObject.SetActive(false);
            if (dim != null) dim.gameObject.SetActive(false);

            // ---- 2) bảng thắng ----
            if (container == null) yield break;
            container.gameObject.SetActive(true);
            if (levelText != null) levelText.text = "Level " + level;
            if (prizeText != null) prizeText.text = coins.ToString();
            CanvasGroup cg = GetComponent<CanvasGroup>();
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.35f)
            {
                if (container == null) yield break;
                container.localScale = Vector3.one * EaseOutBack(k);
                if (cg != null) cg.alpha = 1f;
                yield return null;
            }
            if (container != null) container.localScale = Vector3.one;
            if (container != null) yield return PanelEffects();
        }

        /// <summary>
        /// Hiệu ứng bảng thắng như video gốc: xu nổ tia sáng + sao, quầng sáng, chữ PERFECT! bật lên.
        /// Hình (vị trí, cỡ, ảnh, thứ tự vẽ của hạt) nằm sẵn trong prefab; ở đây chỉ bật/tắt và diễn hoạt theo thời gian.
        /// </summary>
        IEnumerator PanelEffects()
        {
            foreach (Transform t in burstEffects)
            {
                if (t == null) continue;
                t.gameObject.SetActive(true);
                ParticleSystem ps = t.GetComponent<ParticleSystem>();
                if (ps != null) { ps.Clear(true); ps.Play(true); }
            }
            if (perfect != null) perfect.localScale = Vector3.zero;

            // (video coin_panel) 1) đống xu rơi xuống: to & cao hơn rồi thu về đúng chỗ trên bục.
            // Tính theo THỜI GIAN TUYỆT ĐỐI: khung hình khựng chỉ làm hiện chậm một nhịp chứ không làm đống xu nhảy từng bước.
            float coinStart = Time.unscaledTime;
            while (true)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - coinStart) / 0.38f);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);   // ease-out cubic: mềm hơn khi chạm đáy
                if (coinIcon != null)
                {
                    coinIcon.localScale = _coinScale * Mathf.Lerp(coinDropScale, 1f, e);
                    coinIcon.localPosition = _coinPos + new Vector3(0f, Mathf.Lerp(coinDropHeight, 0f, e), 0f);
                }
                if (k >= 1f) break;
                yield return null;
            }
            if (coinIcon != null) { coinIcon.localScale = _coinScale; coinIcon.localPosition = _coinPos; }

            // 2) chớp sáng trắng tròn + tia toả sau xu, 2 nhịp (~0.5s/nhịp)
            Graphic[] flashes = { coinFlash, coinFlashCore, coinRays };
            foreach (Graphic g in flashes) if (g != null) { SetAlpha(g, 0f); g.gameObject.SetActive(true); }
            Vector3 raysRot = coinRays != null ? coinRays.rectTransform.localEulerAngles : Vector3.zero;
            AudioManager.Play(Sfx.QmarkReveal);
            float flashStart = Time.unscaledTime;
            for (float t = 0f; t < 1.1f; t = Time.unscaledTime - flashStart)
            {
                float pulse = Mathf.Max(Pulse(t, 0f, 0.5f), Pulse(t, 0.5f, 0.55f));
                if (coinFlash != null) SetAlpha(coinFlash, pulse);
                if (coinFlashCore != null) SetAlpha(coinFlashCore, pulse);
                if (coinRays != null)
                {
                    SetAlpha(coinRays, pulse * 0.8f);
                    coinRays.rectTransform.localEulerAngles = raysRot + new Vector3(0f, 0f, -t * 40f);
                }
                if (perfect != null) perfect.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(t / 0.4f));
                yield return null;
            }
            foreach (Graphic g in flashes) if (g != null) g.gameObject.SetActive(false);
            if (coinRays != null) coinRays.rectTransform.localEulerAngles = raysRot;
            if (perfect != null) perfect.localScale = Vector3.one;

            // 3) sao lấp lánh liên tục quanh xu  4) vệt sáng quét qua nút Continue / x2
            if (sparkles != null) sparkles.Play();
            foreach (UiShineSweep s in shines) if (s != null) s.Restart();
        }

        static void SetAlpha(Graphic g, float a) { Color c = g.color; c.a = a; g.color = c; }

        static float Pulse(float t, float start, float dur)
        {
            float k = (t - start) / dur;
            if (k < 0f || k > 1f) return 0f;
            return k < 0.3f ? k / 0.3f : 1f - (k - 0.3f) / 0.7f;
        }

        /// <summary>Pháo giấy + pháo hoa của màn thắng (thông số nằm trên từng UiConfetti / ParticleSystem trong prefab).</summary>
        void PlayLogoEffects()
        {
            if (confettiRoot != null) confettiRoot.SetActive(true);
            float delay = burstDelay;
            foreach (UiConfetti c in confettiBursts)
            {
                if (c == null) continue;
                c.Play(delay);
                delay += burstStep;
            }
            if (confettiRain != null)
            {
                confettiRain.Play(rainDelay);
                StartCoroutine(HideAfter(confettiRain.gameObject, rainHideAfter));
            }
            if (fireworks != null)
            {
                fireworks.gameObject.SetActive(true);
                fireworks.Clear(true);
                fireworks.Play(true);
            }
        }

        static IEnumerator HideAfter(GameObject go, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (go != null) go.SetActive(false);
        }

        IEnumerator AnimateLogo()
        {
            Transform anim = logoAnimation != null ? logoAnimation : logo;
            var all = new List<Transform>();
            foreach (Transform t in anim) { all.Add(t); t.localScale = Vector3.zero; }
            if (logoBadge != null) yield return Pop(logoBadge, 0.35f);
            foreach (LogoLetter l in logoLetters)
            {
                if (l == null) continue;
                if (l.background != null) StartCoroutine(Pop(l.background, 0.18f));
                if (l.glyph != null) StartCoroutine(Pop(l.glyph, 0.18f));
                AudioManager.Play(Sfx.QmarkReveal);
                yield return new WaitForSecondsRealtime(0.12f);
            }
            if (logoBlast != null) yield return Pop(logoBlast, 0.3f);
            AudioManager.Play(Sfx.Confetti);
            foreach (Transform t in all) if (t.localScale == Vector3.zero) t.localScale = Vector3.one;
        }

        static IEnumerator Pop(Transform t, float dur)
        {
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / dur)
            {
                if (t == null) yield break;
                t.localScale = Vector3.one * EaseOutBack(k);
                yield return null;
            }
            if (t != null) t.localScale = Vector3.one;
        }

        protected override void CollectMissing(List<string> missing)
        {
            if (container == null) missing.Add("container");
            if (levelText == null) missing.Add("levelText");
            if (prizeText == null) missing.Add("prizeText");
            if (continueButton == null) missing.Add("continueButton");
            if (x2Button == null) missing.Add("x2Button");
            if (dim == null) missing.Add("dim");
            if (logoArea == null) missing.Add("logoArea");
            if (logo == null) missing.Add("logo");
            if (logoBadge == null) missing.Add("logoBadge");
            if (logoBlast == null) missing.Add("logoBlast");
            if (logoLetters == null || logoLetters.Length != 6) missing.Add("logoLetters (cần đúng 6: b o u n c e)");
            if (coinIcon == null) missing.Add("coinIcon");
            if (perfect == null) missing.Add("perfect");
            if (areaGlow == null) missing.Add("areaGlow");
            if (confettiBursts.Length == 0) missing.Add("confettiBursts");
            if (confettiRain == null) missing.Add("confettiRain");
            if (fireworks == null) missing.Add("fireworks");
            if (coinFlash == null) missing.Add("coinFlash");
            if (coinFlashCore == null) missing.Add("coinFlashCore");
            if (coinRays == null) missing.Add("coinRays");
            if (sparkles == null) missing.Add("sparkles");
        }
    }
}
