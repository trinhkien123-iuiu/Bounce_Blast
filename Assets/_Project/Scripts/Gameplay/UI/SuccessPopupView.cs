using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Script quản lý của popup THẮNG – gắn trên gốc SuccessCanvas.prefab. Nó SỞ HỮU toàn bộ chuỗi màn thắng:
    ///   1) màn logo "BOUNCE BLAST" (chữ bật lên lần lượt) + pháo giấy + pháo hoa;
    ///   2) bảng thắng nảy lên, đống xu rơi xuống, chớp sáng + tia, chữ PERFECT!, sao lấp lánh, vệt sáng quét nút.
    /// Mọi node cần thiết giữ bằng [SerializeField] (không Find theo tên). GameUI chỉ gọi <see cref="Play"/> và nghe <see cref="OnContinue"/>.
    ///
    /// Popup này được đặt sẵn trong Hierarchy và DÙNG LẠI qua nhiều màn thắng, nên mọi trạng thái đổi trong lúc chạy
    /// (vị trí đống xu, mưa pháo…) phải được đặt về từ giá trị gốc ghi lại lúc Awake, không cộng dồn.
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
        [Tooltip("Các khối live-ops / tiền tệ của prefab gốc – luôn tắt")] [SerializeField] Transform[] alwaysHidden = new Transform[0];
        [Tooltip("Nền đen che bàn chơi trong màn logo (Dim)")] [SerializeField] Image dim;

        [Header("Màn logo BOUNCE BLAST")]
        [SerializeField] Transform logoArea;
        [SerializeField] Transform logo;
        [Tooltip("Node chứa các chữ (Logo/Animation)")] [SerializeField] Transform logoAnimation;
        [SerializeField] Transform logoBadge;
        [SerializeField] Transform logoBlast;
        [Tooltip("Theo thứ tự b, o, u, n, c, e")] [SerializeField] LogoLetter[] logoLetters = new LogoLetter[0];

        [Header("Pháo")]
        [Tooltip("Cha của các ConfettiBurst (LogoArea/Confetti)")] [SerializeField] Transform confettiBursts;
        [Tooltip("Mưa pháo giấy phủ toàn màn (UI Particle ConfettiFullscreen)")] [SerializeField] Transform confettiRain;
        [Tooltip("Pháo hoa SF_Basic")] [SerializeField] Transform fireworks;

        [Header("Hiệu ứng bảng thắng")]
        [Tooltip("Quầng sáng sau xu (AreaShine)")] [SerializeField] Transform areaShine;
        [Tooltip("Bục xu (Coin) – cha của icon xu")] [SerializeField] Transform coinRoot;
        [Tooltip("Đống xu (Coin/coinIcon)")] [SerializeField] Transform coinIcon;
        [Tooltip("Khối chữ PERFECT! (tên gốc OutOfCapsity)")] [SerializeField] Transform perfect;
        [Tooltip("Các hệ hạt nổ cần bật + chạy khi bảng hiện (Coin/ExploteParticle, Coin/Fx_Star, ExploteParticle (1))")]
        [SerializeField] Transform[] burstEffects = new Transform[0];

        [Header("Texture hiệu ứng (_Ripped)")]
        [Tooltip("Pháo giấy 3×3 gốc (confetti_0.png)")] [SerializeField] Texture2D confettiTexture;
        [Tooltip("Quầng sáng tròn mềm (fx_glow.png)")] [SerializeField] Texture2D glowTexture;
        [Tooltip("Tia sáng toả (glow7.png)")] [SerializeField] Texture2D raysTexture;
        [Tooltip("Sao lấp lánh 4 cánh (sparkle3.png)")] [SerializeField] Texture2D sparkleTexture;

        /// <summary>Người chơi bấm Continue hoặc x2.</summary>
        public event Action OnContinue;

        /// <summary>Dời đống xu lên để nằm trọn trong vòng tròn bục (đơn vị canvas).</summary>
        const float CoinLift = 45f;

        Vector3 _coinBasePos;          // vị trí gốc của đống xu trong prefab (chưa cộng CoinLift)
        bool _coinBaseSaved;

        protected override bool UsesPopIn => false;

        void Awake()
        {
            if (coinIcon != null) { _coinBasePos = coinIcon.localPosition; _coinBaseSaved = true; }
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (x2Button != null) x2Button.onClick.AddListener(Continue);
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
            foreach (Transform t in alwaysHidden) if (t != null) t.gameObject.SetActive(false);
            if (container != null) container.gameObject.SetActive(false);
            if (dim != null) { dim.gameObject.SetActive(true); dim.transform.SetAsFirstSibling(); }

            // ---- 1) màn logo ----
            if (logoArea != null && logo != null)
            {
                logoArea.gameObject.SetActive(true);
                logo.gameObject.SetActive(true);
                Animator anim = logo.GetComponent<Animator>();
                if (anim != null) anim.enabled = false; // tự diễn hoạt bằng code (clip gốc không đủ)
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
        /// Hiệu ứng bảng thắng như video gốc: xu nổ tia sáng + sao, quầng sáng AreaShine, chữ PERFECT! bật lên. Các ParticleSystem
        /// gốc nằm trong canvas nên phải vẽ trên canvas (sortingOrder) và bật lại scale (animator gốc đã mất).
        /// </summary>
        IEnumerator PanelEffects()
        {
            Canvas canvas = Canvas;
            FixTrimmedSprites(container);
            int order = canvas != null ? canvas.sortingOrder + 1 : 100;
            foreach (ParticleSystemRenderer r in container.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (canvas != null) r.sortingLayerID = canvas.sortingLayerID;
                r.sortingOrder = order;
            }
            // quầng sáng sau xu: plugin UIParticle gốc phóng particle theo UI → ~4.5 đơn vị world (≈ nửa bảng)
            if (areaShine != null) { areaShine.localScale = Vector3.one * 250f; ((RectTransform)areaShine).anchoredPosition = new Vector2(6f, -63f); }
            foreach (Transform t in burstEffects)
            {
                if (t == null) continue;
                t.gameObject.SetActive(true);
                ParticleSystem ps = t.GetComponent<ParticleSystem>();
                if (ps != null) { ps.Clear(true); ps.Play(true); }
            }
            if (perfect != null) perfect.localScale = Vector3.zero;

            // (video coin_panel) 1) đống xu rơi xuống: to & cao hơn rồi thu về đúng chỗ trên bục.
            // Tính theo THỜI GIAN TUYỆT ĐỐI (không cộng dồn deltaTime): khung hình khựng chỉ làm hiện chậm một nhịp chứ không làm
            // đống xu nhảy từng bước → hết giật. Vị trí lấy từ giá trị GỐC đã ghi (popup dùng lại nên không được cộng dồn CoinLift).
            Vector3 coinPos = _coinBaseSaved ? _coinBasePos + new Vector3(0f, CoinLift, 0f) : Vector3.zero;
            float coinStart = Time.unscaledTime;
            while (true)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - coinStart) / 0.38f);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);   // ease-out cubic: mềm hơn khi chạm đáy
                if (coinIcon != null)
                {
                    coinIcon.localScale = Vector3.one * 0.85f * Mathf.Lerp(1.9f, 1f, e);
                    coinIcon.localPosition = coinPos + new Vector3(0f, Mathf.Lerp(290f, 0f, e), 0f);
                }
                if (k >= 1f) break;
                yield return null;
            }
            if (coinIcon != null) { coinIcon.localScale = Vector3.one * 0.85f; coinIcon.localPosition = coinPos; }

            // 2) chớp sáng trắng tròn + tia toả sau xu, 2 nhịp (~0.5s/nhịp)
            RawImage glow = null, rays = null, core = null;
            if (coinRoot != null && glowTexture != null)
            {
                glow = FxImage(coinRoot.parent, "CoinFlash", glowTexture, new Vector2(900f, 900f), coinRoot);
                core = FxImage(coinRoot.parent, "CoinFlashCore", glowTexture, new Vector2(620f, 560f), coinRoot); // lõi trắng chói phủ bục
                if (raysTexture != null) rays = FxImage(coinRoot.parent, "CoinRays", raysTexture, new Vector2(1000f, 1000f), coinRoot);
            }
            AudioManager.Play(Sfx.QmarkReveal);
            float flashStart = Time.unscaledTime;
            for (float t = 0f; t < 1.1f; t = Time.unscaledTime - flashStart)
            {
                float pulse = Mathf.Max(Pulse(t, 0f, 0.5f), Pulse(t, 0.5f, 0.55f));
                if (glow != null) glow.color = new Color(1f, 1f, 1f, pulse);
                if (core != null) core.color = new Color(1f, 1f, 1f, pulse);
                if (rays != null)
                {
                    rays.color = new Color(1f, 1f, 1f, pulse * 0.8f);
                    rays.rectTransform.localEulerAngles = new Vector3(0f, 0f, -t * 40f);
                }
                if (perfect != null) perfect.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(t / 0.4f));
                yield return null;
            }
            if (glow != null) Destroy(glow.gameObject);
            if (core != null) Destroy(core.gameObject);
            if (rays != null) Destroy(rays.gameObject);
            if (perfect != null) perfect.localScale = Vector3.one;

            // 3) sao lấp lánh liên tục quanh xu  4) vệt sáng quét qua nút Continue / x2
            if (coinRoot != null && sparkleTexture != null)
                UiSparkles.Play((RectTransform)coinRoot, sparkleTexture, new Vector2(520f, 380f));
            if (glowTexture != null)
            {
                if (continueButton != null) UiShineSweep.Attach((RectTransform)continueButton.transform, glowTexture, 0f);
                if (x2Button != null) UiShineSweep.Attach((RectTransform)x2Button.transform, glowTexture, 0.25f);
            }
        }

        static float Pulse(float t, float start, float dur)
        {
            float k = (t - start) / dur;
            if (k < 0f || k > 1f) return 0f;
            return k < 0.3f ? k / 0.3f : 1f - (k - 0.3f) / 0.7f;
        }

        /// <summary>Ảnh hiệu ứng tạm (chớp sáng, tia) – tạo rồi huỷ trong chớp mắt nên dựng bằng code là hợp lý.</summary>
        static RawImage FxImage(Transform parent, string name, Texture tex, Vector2 size, Transform placeLike)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            if (placeLike is RectTransform pr) rt.anchoredPosition = pr.anchoredPosition;
            rt.SetSiblingIndex(placeLike.GetSiblingIndex()); // ngay sau bục, trước đống xu
            var img = go.AddComponent<RawImage>();
            img.texture = tex;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0f);
            return img;
        }

        /// <summary>Sprite trong atlas bản build đã bị cắt sát → Image Simple khung cố định bị kéo méo: giữ tỉ lệ sprite.</summary>
        static void FixTrimmedSprites(Transform root)
        {
            foreach (Image img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || img.type != Image.Type.Simple || img.preserveAspect || img is MirroredImage) continue;
                Rect r = img.rectTransform.rect;
                if (r.width <= 1f || r.height <= 1f) continue;
                float a = img.sprite.rect.width / img.sprite.rect.height, b = r.width / r.height;
                if (Mathf.Abs(a / b - 1f) > 0.08f) img.preserveAspect = true;
            }
        }

        /// <summary>
        /// Pháo giấy + pháo hoa của màn thắng. Plugin UI-particle gốc đã bị strip (ConfettiBurst / UI Particle ConfettiFullscreen
        /// chỉ còn RectTransform) → dựng lại bằng UiConfetti theo thông số gốc. SF_Basic là ParticleSystem pháo hoa gốc
        /// (rocket + sub-emitter nổ) nhưng prefab để scale 0 và vẽ dưới nền đen của canvas → bật scale, đặt sortingOrder trên canvas.
        /// </summary>
        void PlayLogoEffects()
        {
            Canvas canvas = Canvas;
            Texture sheet = confettiTexture;
            if (confettiBursts != null)
            {
                confettiBursts.gameObject.SetActive(true);
                float delay = 0.95f; // nổ lúc chữ BLAST bật lên
                foreach (Transform t in confettiBursts)
                {
                    if (!t.name.StartsWith("ConfettiBurst")) continue;
                    foreach (Transform child in t) child.gameObject.SetActive(false); // TrailParticle: plugin đã mất
                    UiConfetti.Play(t, UiConfetti.Mode.Burst, sheet, delay);
                    delay += 0.15f;
                }
            }
            if (confettiRain != null)
            {
                // phủ toàn màn hình (fitRect gốc): gắn thẳng vào canvas gốc, vẽ trên cùng
                var rt = (RectTransform)confettiRain;
                if (canvas != null) rt.SetParent(canvas.transform, false);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.localRotation = Quaternion.identity; // object gốc bị xoay ngược (phục vụ plugin cũ) → mưa rơi từ đáy
                rt.localScale = Vector3.one;
                rt.SetAsLastSibling();
                confettiRain.gameObject.SetActive(true);
                UiConfetti.Play(confettiRain, UiConfetti.Mode.Rain, sheet, 1f);
                StartCoroutine(HideAfter(confettiRain.gameObject, 6f)); // tắt (không huỷ): popup dùng lại cho màn thắng sau
            }
            if (fireworks != null)
            {
                foreach (ParticleSystem p in fireworks.GetComponentsInChildren<ParticleSystem>(true))
                {
                    p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = p.main;
                    main.playOnAwake = false;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
                ParticleSystem rocket = fireworks.GetComponent<ParticleSystem>();
                if (rocket != null)
                {
                    rocket.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = rocket.main;
                    main.duration = 0.45f;          // ~2 tên lửa/giây
                    var shape = rocket.shape;
                    shape.radius = 3.5f;            // tản ra gần hết bề ngang
                }

                fireworks.gameObject.SetActive(true);
                // Tên lửa pháo hoa gốc: mô phỏng Local trong canvas (scale ≈ 0.01) thì chỉ bay được vài mm →
                // chuyển sang World, scale tuyệt đối = 1, đặt ở đáy màn hình, phóng liên tục như video gốc.
                fireworks.localScale = Vector3.one / Mathf.Max(fireworks.parent.lossyScale.x, 1e-4f); // lossyScale = 1
                var frt = fireworks as RectTransform;
                if (frt != null) frt.anchoredPosition = new Vector2(0f, -1150f);
                else fireworks.localPosition = new Vector3(0f, -1150f, 0f);

                int order = canvas != null ? canvas.sortingOrder + 1 : 100;
                foreach (ParticleSystemRenderer r in fireworks.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (canvas != null) r.sortingLayerID = canvas.sortingLayerID;
                    r.sortingOrder = order;
                }
                ParticleSystem ps = fireworks.GetComponent<ParticleSystem>();
                if (ps != null) { ps.Clear(true); ps.Play(true); }
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
            // Sprite chữ trong bản build đã bị cắt sát (vd "blast" 465×158, "b" 133×157) nhưng khung Image vẫn là ô vuông gốc
            // (600×600, 250×250) → bị kéo méo. Đặt lại khung = kích thước sprite × 1.29 (1.29 = 600/465: "blast" phủ đúng bề ngang
            // gốc), giữ nguyên tâm → chữ nền/chữ chính vẫn đúng tỉ lệ nhau.
            foreach (Image img in anim.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || img.transform == logoBadge) continue;
                img.rectTransform.sizeDelta = img.sprite.rect.size * 1.29f;
            }
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
            if (coinRoot == null) missing.Add("coinRoot");
            if (coinIcon == null) missing.Add("coinIcon");
            if (perfect == null) missing.Add("perfect");
            if (areaShine == null) missing.Add("areaShine");
            if (glowTexture == null) missing.Add("glowTexture");
            if (sparkleTexture == null) missing.Add("sparkleTexture");
            if (confettiTexture == null) missing.Add("confettiTexture");
        }
    }
}
