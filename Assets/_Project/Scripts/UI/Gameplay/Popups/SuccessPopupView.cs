using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>Script quản lý của popup THẮNG</summary>
    [DisallowMultipleComponent]
    public class SuccessPopupView : PopupViewBase
    {
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
        [Tooltip("Animator gốc trên Logo (Banner.controller → clip \"Banner Animation\"): chữ bật lần lượt, BLAST nhảy lên rồi đập xuống")]
        [SerializeField] Animator logoAnimator;
        [Tooltip("Tên state trong Banner.controller")] [SerializeField] string logoState = "Banner Animation";
        [Tooltip("Giây (theo clip) phát tiếng bật của từng chữ b o u n c e")] [SerializeField] float[] letterSoundTimes = { 0f, 0.1f, 0.217f, 0.317f, 0.417f, 0.517f };
        [Tooltip("Giây (theo clip) BLAST đập xuống – phát tiếng pháo")] [SerializeField] float blastSlamTime = 1.233f;

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

        [Header("Thanh \"New Booster\" (gốc: BlockerBar + chữ New Feature) – tiến độ tới booster kế tiếp")]
        [SerializeField] GameObject featureBar;
        [SerializeField] Image featureFill;
        [SerializeField] TMP_Text featurePercent;
        [SerializeField] TMP_Text featureLabel;
        [SerializeField] Image featureIcon;

        [Header("Ô tiền trên đầu bảng thắng – xu bay từ đống xu vào đây")]
        [SerializeField] GameObject coinCounter;
        [SerializeField] TMP_Text coinCounterAmount;
        [Tooltip("Icon xu trong ô tiền (đích của xu bay)")] [SerializeField] RectTransform coinCounterIcon;
        [Tooltip("Mẫu xu bay (tắt sẵn)")] [SerializeField] RectTransform flyCoinTemplate;
        [Tooltip("Nơi xu bắt đầu bay (đống xu)")] [SerializeField] Transform coinSource;
        [SerializeField, Range(4, 30)] int flyCoinCount = 12;
        [SerializeField] float flyCoinTime = 0.6f;
        [SerializeField] float flyCoinStagger = 0.06f;

        int _prize;

        float _featureFrom, _featureTo;
        bool _featureOn;

        /// <summary>Đặt dữ liệu thanh New Booster trước khi Play</summary>
        public void SetFeature(Sprite icon, float from, float to)
        {
            _featureOn = icon != null;
            _featureFrom = from; _featureTo = to;
            if (featureBar != null) featureBar.SetActive(_featureOn);
            if (!_featureOn) return;
            if (featureIcon != null) featureIcon.sprite = icon;
            ShowFeature(from);
        }

        void ShowFeature(float k)
        {
            if (featureFill != null) featureFill.fillAmount = k;
            if (featurePercent != null) featurePercent.text = Mathf.RoundToInt(k * 100f) + "%";
        }

        IEnumerator FillFeature()
        {
            if (!_featureOn) yield break;
            yield return new WaitForSecondsRealtime(0.25f);
            float t0 = Time.unscaledTime;
            while (true)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - t0) / 0.8f);
                ShowFeature(Mathf.Lerp(_featureFrom, _featureTo, 1f - (1f - k) * (1f - k)));
                if (k >= 1f) break;
                yield return null;
            }
            if (featureIcon != null)
            {
                Transform ic = featureIcon.transform; Vector3 b = BaseScale(ic);
                for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
                {
                    ic.localScale = b * (1f + 0.35f * Mathf.Sin(t / 0.35f * Mathf.PI));
                    yield return null;
                }
                ic.localScale = b;
            }
        }

        /// <summary>Người chơi bấm Continue hoặc x2</summary>
        public event Action OnContinue;

        Vector3 _coinPos, _coinScale = Vector3.one;
        readonly Dictionary<Transform, Vector3> _baseScale = new Dictionary<Transform, Vector3>();
        Vector3 BaseScale(Transform t) { Vector3 v; if (!_baseScale.TryGetValue(t, out v)) _baseScale[t] = v = t.localScale; return v; }

        protected override bool UsesPopIn => false;

        void Awake()
        {
            if (coinIcon != null) { _coinPos = coinIcon.localPosition; _coinScale = coinIcon.localScale; }
            if (container != null) BaseScale(container);
            if (perfect != null) BaseScale(perfect);
            if (featureIcon != null) BaseScale(featureIcon.transform);
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (x2Button != null) x2Button.onClick.AddListener(Continue);
        }

        void Update()
        {
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

        /// <summary>Chạy cả chuỗi màn thắng. GameManager chờ coroutine này xong</summary>
        public IEnumerator Play(int level, LevelDifficulty difficulty, int coins, float fireworksHold)
        {
            OpenBase(difficulty);
            if (container != null) container.gameObject.SetActive(false);
            if (coinCounter != null) coinCounter.SetActive(false);   // màn logo / pháo hoa chưa hiện ô tiền
            if (dim != null) dim.gameObject.SetActive(true);

            if (logoArea != null && logo != null)
            {
                logoArea.gameObject.SetActive(true);
                logo.gameObject.SetActive(true);
                PlayLogoEffects();
                yield return AnimateLogo();
            }
            yield return new WaitForSecondsRealtime(fireworksHold * 0.25f);
            AudioManager.Play(Sfx.Confetti);
            yield return new WaitForSecondsRealtime(fireworksHold * 0.35f);
            AudioManager.Play(Sfx.Confetti);
            yield return new WaitForSecondsRealtime(fireworksHold * 0.4f);
            if (logoArea != null) logoArea.gameObject.SetActive(false);
            if (dim != null) dim.gameObject.SetActive(false);

            if (container == null) yield break;
            container.gameObject.SetActive(true);
            if (levelText != null) levelText.text = "Level " + level;
            if (prizeText != null) prizeText.text = coins.ToString();
            _prize = coins;
            if (coinCounter != null) { coinCounter.SetActive(false); }
            CanvasGroup cg = GetComponent<CanvasGroup>();
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.35f)
            {
                if (container == null) yield break;
                container.localScale = BaseScale(container) * EaseOutBack(k);
                if (cg != null) cg.alpha = 1f;
                yield return null;
            }
            if (container != null) container.localScale = BaseScale(container);
            if (container != null) yield return PanelEffects();
        }

        /// <summary>Hiệu ứng bảng thắng như video gốc</summary>
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
            Time.timeScale = 0;

            float coinStart = Time.unscaledTime;
            while (true)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - coinStart) / 0.38f);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                if (coinIcon != null)
                {
                    coinIcon.localScale = _coinScale * Mathf.Lerp(coinDropScale, 1f, e);
                    coinIcon.localPosition = _coinPos + new Vector3(0f, Mathf.Lerp(coinDropHeight, 0f, e), 0f);
                }
                if (k >= 1f) break;
                yield return null;
            }
            if (coinIcon != null) { coinIcon.localScale = _coinScale; coinIcon.localPosition = _coinPos; }

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
                if (perfect != null) perfect.localScale = BaseScale(perfect) * EaseOutBack(Mathf.Clamp01(t / 0.4f));
                yield return null;
            }
            foreach (Graphic g in flashes) if (g != null) g.gameObject.SetActive(false);
            if (coinRays != null) coinRays.rectTransform.localEulerAngles = raysRot;
            if (perfect != null) perfect.localScale = BaseScale(perfect);

            if (sparkles != null) sparkles.Play();
            foreach (UiShineSweep s in shines) if (s != null) s.Restart();
            StartCoroutine(FillFeature());
            StartCoroutine(CollectCoins());

        }

        /// <summary>Xử lý xu bay từ đống xu vào ô tiền: mỗi xu tới thì ô tiền nảy, số tăng dần, có tiếng xu</summary>
        IEnumerator CollectCoins()
        {
            if (coinCounter == null || coinCounterAmount == null || coinCounterIcon == null || flyCoinTemplate == null || coinSource == null || _prize <= 0) yield break;
            int total = PlayerData.Coins, start = Mathf.Max(0, total - _prize);
            coinCounterAmount.text = start.ToString();
            coinCounter.SetActive(true);
            var group = coinCounter.GetComponent<CanvasGroup>();
            if (group == null) group = coinCounter.AddComponent<CanvasGroup>();
            Vector3 baseScale = coinCounter.transform.localScale;
            for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / 0.3f;
                coinCounter.transform.localScale = baseScale * EaseOutBack(t / 0.3f);
                yield return null;
            }
            group.alpha = 1f; coinCounter.transform.localScale = baseScale;
            yield return new WaitForSecondsRealtime(0.15f);

            int n = flyCoinCount, arrived = 0, shown = start;
            float perCoin = (float)_prize / n;
            for (int i = 0; i < n; i++)
            {
                StartCoroutine(FlyOneCoin(i, n, () =>
                {
                    arrived++;
                    shown = start + Mathf.RoundToInt(perCoin * arrived);
                    if (arrived == n) shown = total;
                    coinCounterAmount.text = shown.ToString();
                    AudioManager.Play(Sfx.CoinCollect);
                    StartCoroutine(PopCounter(baseScale));
                }));
                yield return new WaitForSecondsRealtime(flyCoinStagger);
            }
            while (arrived < n) yield return null;
            coinCounterAmount.text = total.ToString();
        }

        IEnumerator FlyOneCoin(int index, int total, Action onArrive)
        {
            RectTransform coin = Instantiate(flyCoinTemplate, flyCoinTemplate.parent);
            coin.gameObject.SetActive(true);
            Vector3 from = coinSource.position + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.12f);
            Vector3 to = coinCounterIcon.position;
            Vector3 mid = (from + to) * 0.5f + new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(0.2f, 0.6f), 0f);
            Vector3 s0 = flyCoinTemplate.localScale * 1.1f, s1 = flyCoinTemplate.localScale * 0.6f;
            for (float t = 0f; t < flyCoinTime; t += Time.unscaledDeltaTime)
            {
                float k = t / flyCoinTime, e = k * k * (3f - 2f * k);
                Vector3 p = (1f - e) * (1f - e) * from + 2f * (1f - e) * e * mid + e * e * to;
                coin.position = p;
                coin.localScale = Vector3.Lerp(s0, s1, e);
                coin.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(k * 6f + index) * 25f);
                yield return null;
            }
            Destroy(coin.gameObject);
            onArrive();
        }

        IEnumerator PopCounter(Vector3 baseScale)
        {
            for (float t = 0f; t < 0.14f; t += Time.unscaledDeltaTime)
            {
                coinCounterIcon.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(t / 0.14f * Mathf.PI));
                yield return null;
            }
            coinCounterIcon.localScale = Vector3.one;
        }

        static void SetAlpha(Graphic g, float a) { Color c = g.color; c.a = a; g.color = c; }

        static float Pulse(float t, float start, float dur)
        {
            float k = (t - start) / dur;
            if (k < 0f || k > 1f) return 0f;
            return k < 0.3f ? k / 0.3f : 1f - (k - 0.3f) / 0.7f;
        }

        /// <summary>Pháo giấy + pháo hoa của màn thắng</summary>
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

        /// <summary>Chạy clip gốc "Banner Animation"</summary>
        IEnumerator AnimateLogo()
        {
            if (logoAnimator == null) yield break;
            logoAnimator.enabled = true;
            logoAnimator.Play(logoState, 0, 0f);
            logoAnimator.Update(0f);
            AnimatorClipInfo[] clips = logoAnimator.GetCurrentAnimatorClipInfo(0);
            float clipLength = clips.Length > 0 ? clips[0].clip.length : 0f;
            int next = 0;
            bool slammed = false;
            for (float t = 0f; t < clipLength; t = logoAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime * clipLength)
            {
                while (next < letterSoundTimes.Length && t >= letterSoundTimes[next]) { AudioManager.Play(Sfx.QmarkReveal); next++; }
                if (!slammed && t >= blastSlamTime) { AudioManager.Play(Sfx.Confetti); slammed = true; }
                yield return null;
            }
            if (!slammed) AudioManager.Play(Sfx.Confetti);
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
            if (logoAnimator == null) missing.Add("logoAnimator");
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
