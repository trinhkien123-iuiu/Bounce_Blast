using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BounceBlast.Gameplay
{
    public enum BoosterType { MagicHand, Hammer, Shuffle, Capacity }

    /// <summary>
    /// UI màn chơi. Dùng NGUYÊN prefab UI của game gốc (tách từ ManagerScene, xem Tools/UnityYaml):
    ///   GameCanvas (HUD Lv + Settings), CapacityBar (world canvas dưới bàn), FailCanvas, SettingsCanvas,
    ///   OutOfCapacityPanel, SuccessCanvas (Level / PERFECT / Rewards / Continue, logo BOUNCE BLAST).
    /// Script logic gốc của các prefab (KPFramework...) đã bị bỏ khi tách → class này tìm phần tử theo TÊN
    /// rồi gắn sự kiện, đổi chữ, bật đúng biến thể theo độ khó (NormalLevel / HardLevel / SuperHard).
    /// Thanh booster + toast + vignette dựng bằng code (booster gốc là object 3D gắn theo camera gốc).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public event Action<BoosterType> OnBoosterPressed;
        public event Action OnSettingsPressed;

        static readonly Vector2 RefResolution = new Vector2(1242f, 2688f); // CanvasScaler của game gốc

        UiSkin _skin;
        Camera _cam;
        GameConfig _config;
        LevelDifficulty _difficulty;

        GameObject _hud;
        TMP_Text _hudLevel;
        TMP_Text _hudHard;
        Image _hudBadge;
        Color _hudBadgeColor = Color.white;

        Transform _capacityRoot;
        Image _capacityFill;
        TMP_Text _capacityText;
        GameObject _capacityInfinite;

        RectTransform _overlay; // canvas của mình: booster, toast, vignette
        Image _vignette;
        float _vignetteTarget;
        TextMeshProUGUI _toast;
        float _toastTime;
        readonly Dictionary<BoosterType, (Image frame, TextMeshProUGUI count, Image badge)> _boosters =
            new Dictionary<BoosterType, (Image, TextMeshProUGUI, Image)>();

        GameObject _popup;
        public bool IsPopupOpen => _popup != null;

        Camera _worldCam;

        /// <param name="uiCam">camera vẽ các canvas Screen Space (chiếu chuẩn)</param>
        /// <param name="worldCam">camera game – cho thanh capacity đặt trong thế giới</param>
        public void Build(UiSkin skin, GameConfig config, Camera uiCam, Camera worldCam)
        {
            _skin = skin;
            _config = config;
            _cam = uiCam;
            _worldCam = worldCam;

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                es.transform.SetParent(transform, false);
            }

            BuildOverlay();
            BuildHud();
            BuildCapacityBar();
        }

        void BuildOverlay()
        {
            var go = new GameObject("OverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            UseCamera(go.GetComponent<Canvas>(), 5);
            SetupScaler(go.GetComponent<CanvasScaler>());
            _overlay = (RectTransform)go.transform;

            _vignette = Img(_overlay, "Vignette", _skin.vignette);
            Stretch(_vignette.rectTransform);
            _vignette.color = new Color(1f, 0.1f, 0.1f, 0f);
            _vignette.raycastTarget = false;

            BuildBoosters();

            _toast = Txt(_overlay, "Toast", "", 64);
            _toast.rectTransform.anchorMin = _toast.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _toast.rectTransform.anchoredPosition = new Vector2(0f, 200f);
            _toast.rectTransform.sizeDelta = new Vector2(1100f, 200f);
            _toast.alpha = 0f;
        }

        /// <summary>Canvas vẽ bằng camera game (Screen Space - Camera, sát camera): nhìn như Overlay,
        /// nhưng ảnh chụp/recorder theo camera vẫn thấy UI. Order lớn hơn = nằm trên.</summary>
        void UseCamera(Canvas c, int order)
        {
            if (c == null) return;
            if (_cam == null) { c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = order; return; }
            c.renderMode = RenderMode.ScreenSpaceCamera;
            c.worldCamera = _cam;
            c.planeDistance = 1.2f - order * 0.01f;
            c.sortingOrder = order;
        }

        static void SetupScaler(CanvasScaler s)
        {
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = RefResolution;
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 1f;
        }

        GameObject Spawn(GameObject prefab, string name)
        {
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, transform);
            go.name = name;
            go.SetActive(true);
            // CanvasGroup gốc để alpha = 0 (script gốc tự fade-in) → mở lại
            foreach (CanvasGroup cg in go.GetComponentsInChildren<CanvasGroup>(true)) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
            Canvas c = go.GetComponent<Canvas>();
            if (c != null)
            {
                UseCamera(c, 10);
                if (go.GetComponent<GraphicRaycaster>() == null) go.AddComponent<GraphicRaycaster>();
            }
            return go;
        }

        //HUD
        Image _hudSettings;
        Image _boosterStrip;
        RectTransform _boosterBar;

        /// <summary>Tâm dọc hàng HUD (nút Settings, pixel màn hình) – mép trên khung máng đặt ngang đây như game gốc. -1 nếu chưa dựng.</summary>
        public float HudCenterScreenY
        {
            get
            {
                if (_hudSettings == null) return -1f;
                Canvas.ForceUpdateCanvases();
                var c = new Vector3[4];
                _hudSettings.rectTransform.GetWorldCorners(c);
                return RectTransformUtility.WorldToScreenPoint(_cam, (c[0] + c[2]) * 0.5f).y;
            }
        }

        /// <summary>Mép trên hàng nút booster (pixel màn hình) – khay đạn phải nằm trên mốc này. -1 nếu chưa dựng.</summary>
        public float BoosterTopScreenY
        {
            get
            {
                if (_boosterBar == null) return -1f;
                Canvas.ForceUpdateCanvases();
                var c = new Vector3[4];
                _boosterBar.GetWorldCorners(c);
                return RectTransformUtility.WorldToScreenPoint(_cam, c[1]).y;
            }
        }
        Sprite _hudSettingsDefault;

        void BuildHud()
        {
            _hud = Spawn(_skin.hudPrefab, "HUD (GameCanvas)");
            if (_hud == null) return;
            UseCamera(_hud.GetComponent<Canvas>(), 10);
            Transform level = Find(_hud.transform, "CurrentLevel (1)");
            if (level != null)
            {
                _hudBadge = level.GetComponent<Image>();
                _hudBadgeColor = _hudBadge != null ? _hudBadge.color : Color.white;
                _hudLevel = Find(level, "LevelText")?.GetComponent<TMP_Text>();
                _hudHard = Find(level, "hardLevelTMP")?.GetComponent<TMP_Text>();
            }
            foreach (Image img in _hud.GetComponentsInChildren<Image>(true))
                if (img.name == "SettingsButton" && img.sprite != null) { _hudSettings = img; _hudSettingsDefault = img.sprite; }
            Button settings = FindButton(_hud.transform, "SettingsButton");
            if (settings != null)
            {
                settings.onClick.RemoveAllListeners();
                settings.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnSettingsPressed?.Invoke(); });
            }
        }

        public void SetLevel(int level, LevelDifficulty difficulty)
        {
            _difficulty = difficulty;
            if (_hudLevel != null) _hudLevel.text = "Lv " + level;
            if (_hudHard != null)
            {
                _hudHard.gameObject.SetActive(difficulty != LevelDifficulty.Normal);
                _hudHard.text = difficulty == LevelDifficulty.SuperHard ? "Super Hard" : "Hard";
            }
            LayoutLevelBadge(difficulty);
            // dải booster dưới cùng đổi tông theo độ khó (màu đo từ ảnh gốc: HardBG đỏ / SuperHardBG navy tối)
            if (_boosterStrip != null)
                _boosterStrip.color = difficulty == LevelDifficulty.SuperHard ? new Color32(38, 40, 71, 255)
                                    : difficulty == LevelDifficulty.Hard ? new Color32(190, 42, 44, 255)
                                    : new Color32(71, 46, 226, 255);
            if (_hudSettings != null)
            {
                Sprite gear = difficulty == LevelDifficulty.SuperHard ? _skin.settingsSuperHard
                            : difficulty == LevelDifficulty.Hard ? _skin.settingsHard : null;
                _hudSettings.sprite = gear != null ? gear : _hudSettingsDefault;
            }
            if (_hud != null) ApplyDifficultyVariants(_hud.transform, difficulty);
        }

        /// <summary>
        /// Badge level góc trái trên như game gốc: Normal = viên xanh "Level" (≈198×96), Hard / Super Hard = khung
        /// "Hard Level_0" / "Super Hard Level" có tab đầu lâu ở trên, tab chữ độ khó ở dưới (≈203×164).
        /// Kích thước/vị trí đo từ ảnh gốc 858×1908 (×1.448 ra đơn vị canvas 1242).
        /// </summary>
        void LayoutLevelBadge(LevelDifficulty difficulty)
        {
            if (_hudBadge == null) return;
            bool hard = difficulty != LevelDifficulty.Normal;
            Sprite sp = difficulty == LevelDifficulty.SuperHard ? _skin.levelBadgeSuperHard
                      : difficulty == LevelDifficulty.Hard ? _skin.levelBadgeHard : _skin.levelBadgeNormal;
            if (sp != null) _hudBadge.sprite = sp;
            _hudBadge.color = Color.white;
            _hudBadge.type = Image.Type.Simple;
            _hudBadge.preserveAspect = false; // gốc: viên badge dẹt hơn sprite một chút (≈2.24:1)

            RectTransform rt = _hudBadge.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = hard ? new Vector2(203f, 164f) : new Vector2(203f, 90f);
            rt.anchoredPosition = hard ? new Vector2(40f, -17f) : new Vector2(40f, -43f);

            if (_hudLevel != null)
            {
                RectTransform t = _hudLevel.rectTransform;
                // chữ chừa lề rộng như gốc (cao ≈ 36% viên badge)
                t.anchorMin = hard ? new Vector2(0.14f, 0.36f) : new Vector2(0.15f, 0.2f);
                t.anchorMax = hard ? new Vector2(0.86f, 0.72f) : new Vector2(0.85f, 0.8f);
                t.offsetMin = t.offsetMax = Vector2.zero;
                t.anchoredPosition = Vector2.zero;
                _hudLevel.margin = Vector4.zero;
                _hudLevel.enableWordWrapping = false;
                _hudLevel.overflowMode = TextOverflowModes.Overflow;
                _hudLevel.enableAutoSizing = true;
                _hudLevel.fontSizeMin = 10f;
                _hudLevel.fontSizeMax = 44f;
                _hudLevel.alignment = TextAlignmentOptions.Center;
            }
            if (_hudHard != null)
            {
                RectTransform t = _hudHard.rectTransform;
                t.anchorMin = new Vector2(0.22f, 0.05f);
                t.anchorMax = new Vector2(0.78f, 0.3f);
                t.offsetMin = t.offsetMax = Vector2.zero;
                t.anchoredPosition = Vector2.zero;
                _hudHard.margin = Vector4.zero;
                _hudHard.enableWordWrapping = false;
                _hudHard.overflowMode = TextOverflowModes.Overflow;
                _hudHard.enableAutoSizing = true;
                _hudHard.fontSizeMin = 8f;
                _hudHard.fontSizeMax = 30f;
                _hudHard.alignment = TextAlignmentOptions.Center;
            }
        }

        //Capacity
        void BuildCapacityBar()
        {
            GameObject cap = Spawn(_skin.capacityBarPrefab, "CapacityBar");
            if (cap == null) return;
            _capacityRoot = cap.transform;
            Rect a = _config.arena;
            // gốc: root (5.34, -12.10) scale 0.726 ngay dưới đáy bàn → đặt sát đáy lòng bàn của scene này
            _capacityRoot.position = new Vector3(a.center.x, a.yMin - 0.42f, -0.6f);
            _capacityRoot.rotation = _worldCam != null ? _worldCam.transform.rotation : Quaternion.identity;
            _capacityRoot.localScale = Vector3.one * 0.62f;
            Transform overlay = Find(_capacityRoot, "Canvas Base");
            if (overlay != null) overlay.gameObject.SetActive(false); // vignette của prefab: dùng vignette của mình
            Transform world = Find(_capacityRoot, "Canvas");
            if (world != null)
            {
                Canvas wc = world.GetComponent<Canvas>();
                wc.worldCamera = _worldCam;
                wc.sortingOrder = 4;
                world.localPosition = new Vector3(0f, 0f, 0f);
                // Canvas con trong prefab gốc nằm ngang (rot X 70 so với root) → nhìn qua camera nghiêng 20° bị dẹt
                // còn ~1/3 chiều cao. Ảnh gốc: thanh chỉ hơi dẹt (cao ≈ 0.8 so với nhìn thẳng) → thẳng mặt camera,
                // ép nhẹ trục y (ngả canvas thì đâm vào đế bàn).
                world.localRotation = Quaternion.identity;
                Vector3 ws = world.localScale;
                world.localScale = new Vector3(ws.x, ws.y * 0.8f, ws.z);
            }
            _capacityFill = Find(_capacityRoot, "FillBar")?.GetComponent<Image>();
            _capacityText = Find(_capacityRoot, "UI Text")?.GetComponent<TMP_Text>();
            if (_capacityText != null)
            {
                _capacityText.color = Color.white;
                if (_skin.fontOutlineMaterial != null) { _capacityText.font = _skin.font; _capacityText.fontSharedMaterial = _skin.fontOutlineMaterial; }
            }
            _capacityInfinite = Find(_capacityRoot, "endless icon (1)")?.gameObject;
            if (_capacityInfinite != null) _capacityInfinite.SetActive(false);
        }

        int _capacityKey = int.MinValue;

        public void SetCapacity(float fill, bool started, bool infinite)
        {
            float shown = Mathf.Clamp01(fill);
            if (_capacityFill != null)
            {
                _capacityFill.fillAmount = Mathf.MoveTowards(_capacityFill.fillAmount, started ? shown : 0f, Time.deltaTime * 1.5f);
                _capacityFill.color = CapacitySystem.FillColor(shown);
            }
            if (_capacityText != null)
            {
                // chỉ dựng chuỗi khi số % đổi (trước đây cấp phát chuỗi mới mỗi frame)
                int key = infinite ? -2 : !started ? -1 : Mathf.RoundToInt(fill * 100f);
                if (key != _capacityKey)
                {
                    _capacityKey = key;
                    if (key >= 0) _capacityText.SetText("{0}%", key);
                    else _capacityText.text = key == -2 ? "" : "CAPACITY";
                }
            }
            if (_capacityInfinite != null) _capacityInfinite.SetActive(infinite);
            _warnOn = !infinite && started && fill >= 0.9f;
            _vignetteTarget = fill >= 0.75f ? Mathf.InverseLerp(0.75f, 1f, fill) : 0f;
        }

        void LateUpdate()
        {
            float breathe = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);
            Color vc = _vignette.color;
            vc.a = Mathf.MoveTowards(vc.a, _vignetteTarget * 0.55f * breathe, Time.unscaledDeltaTime * 2f);
            _vignette.color = vc;

            for (int i = 0; i < _warnArrows.Count; i++)
            {
                if (_warnArrows[i].gameObject.activeSelf != _warnOn) _warnArrows[i].gameObject.SetActive(_warnOn);
                if (_warnOn) _warnArrows[i].anchoredPosition = new Vector2(0f, 8f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 1.6f)) * 26f);
            }

            if (_toastTime > 0f)
            {
                _toastTime -= Time.unscaledDeltaTime;
                _toast.alpha = Mathf.Clamp01(_toastTime * 2f);
            }
        }

        public void Toast(string message, float seconds = 1.6f)
        {
            _toast.text = message;
            _toastTime = seconds;
            _toast.alpha = 1f;
        }

        //Booster
        void BuildBoosters()
        {
            // dải nền tím + viền vàng ngang màn hình
            Image strip = Img(_overlay, "BoosterStrip", null);
            strip.rectTransform.anchorMin = new Vector2(0f, 0f);
            strip.rectTransform.anchorMax = new Vector2(1f, 0f);
            strip.rectTransform.pivot = new Vector2(0.5f, 0f);
            strip.rectTransform.sizeDelta = new Vector2(0f, 150f);
            strip.color = new Color(0.25f, 0.20f, 0.62f);
            _boosterStrip = strip;
            strip.raycastTarget = false;
            Image line = Img(strip.rectTransform, "GoldLine", null);
            line.rectTransform.anchorMin = new Vector2(0f, 1f);
            line.rectTransform.anchorMax = new Vector2(1f, 1f);
            line.rectTransform.sizeDelta = new Vector2(0f, 14f);
            line.color = new Color(1f, 0.66f, 0.12f);
            line.raycastTarget = false;

            var bar = new GameObject("Boosters", typeof(RectTransform)).GetComponent<RectTransform>();
            bar.SetParent(_overlay, false);
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 150f);
            bar.sizeDelta = new Vector2(1150f, 260f);
            _boosterBar = bar;

            var types = new[] { BoosterType.MagicHand, BoosterType.Hammer, BoosterType.Shuffle, BoosterType.Capacity };
            var icons = new[] { _skin.boosterMagicHand, _skin.boosterHammer, _skin.boosterShuffle, _skin.boosterCapacity };
            Sprite frameSprite = MakeRing(new Color(0.30f, 0.24f, 0.78f), new Color(1f, 0.62f, 0.08f), 0.11f);
            Sprite darkBadge = MakeRing(new Color(0.16f, 0.14f, 0.38f), new Color(0.16f, 0.14f, 0.38f), 0.01f, 64);
            for (int i = 0; i < types.Length; i++)
            {
                BoosterType type = types[i];
                Image frame = Img(bar, type.ToString(), frameSprite);
                frame.rectTransform.anchoredPosition = new Vector2((i - 1.5f) * 268f, 0f);
                frame.rectTransform.sizeDelta = new Vector2(232f, 232f);
                Button b = frame.gameObject.AddComponent<Button>();
                Image icon = Img(frame.rectTransform, "Icon", icons[i]);
                icon.rectTransform.sizeDelta = new Vector2(170f, 170f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                Image badge = Img(frame.rectTransform, "Badge", darkBadge);
                badge.rectTransform.anchorMin = badge.rectTransform.anchorMax = new Vector2(1f, 0f);
                badge.rectTransform.anchoredPosition = new Vector2(-28f, 28f);
                badge.rectTransform.sizeDelta = new Vector2(76f, 76f);
                TextMeshProUGUI count = Txt(badge.rectTransform, "Count", "0", 52);
                Stretch(count.rectTransform);
                b.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnBoosterPressed?.Invoke(type); });
                _boosters[type] = (frame, count, badge);
            }
            // Mũi tên vàng nhấp nhô chỉ vào Hammer và Capacity khi thanh sức chứa gần đầy (video lose_coche)
            Sprite tri = MakeTriangleSprite();
            foreach (BoosterType t in new[] { BoosterType.Hammer, BoosterType.Capacity })
            {
                Image a = Img(_boosters[t].frame.rectTransform, "WarnArrow", tri);
                a.rectTransform.anchorMin = a.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                a.rectTransform.pivot = new Vector2(0.5f, 0f);
                a.rectTransform.sizeDelta = new Vector2(110f, 90f);
                a.raycastTarget = false;
                a.gameObject.SetActive(false);
                _warnArrows.Add(a.rectTransform);
            }
        }

        readonly List<RectTransform> _warnArrows = new List<RectTransform>();
        bool _warnOn;

        static Sprite MakeTriangleSprite()
        {
            const int W = 64, H = 52;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            Color fill = new Color(1f, 0.82f, 0.1f), edge = new Color(0.85f, 0.4f, 0.05f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    // tam giác chỉ xuống: rộng ở trên (y lớn), nhọn ở dưới
                    float half = (W * 0.5f) * (y + 1f) / H;
                    float d = half - Mathf.Abs(x + 0.5f - W * 0.5f);
                    Color c = d < 0f ? Color.clear : (d < 4f || y > H - 5 ? edge : fill);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
        }

        public void SetBooster(BoosterType type, int count, bool armed)
        {
            if (!_boosters.TryGetValue(type, out var ui)) return;
            ui.count.text = count > 0 ? count.ToString() : "+";
            ui.badge.color = count > 0 ? Color.white : new Color(0.35f, 0.85f, 0.25f) * 3f;
            ui.frame.rectTransform.localScale = Vector3.one * (armed ? 1.12f : 1f);
            ui.frame.color = armed ? new Color(1f, 0.9f, 0.5f) : Color.white;
        }

        //Popup chung
        GameObject OpenPopup(GameObject prefab, string name, bool wrapInCanvas = false)
        {
            ClosePopup();
            GameObject go;
            if (wrapInCanvas)
            {
                var host = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                host.transform.SetParent(transform, false);
                UseCamera(host.GetComponent<Canvas>(), 30);
                SetupScaler(host.GetComponent<CanvasScaler>());
                GameObject inner = Spawn(prefab, prefab.name);
                inner.transform.SetParent(host.transform, false);
                var rt = (RectTransform)inner.transform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                go = host;
            }
            else go = Spawn(prefab, name);

            UseCamera(go.GetComponent<Canvas>(), 30);
            ApplyDifficultyVariants(go.transform, _difficulty);
            _popup = go;
            StartCoroutine(PopIn(go));
            return go;
        }

        public void ClosePopup()
        {
            if (_popup != null) Destroy(_popup);
            _popup = null;

            Time.timeScale = 1;
        }

        /// <summary>Fade nền + nảy khung (thay cho tween DOTween của bản gốc).</summary>
        IEnumerator PopIn(GameObject go)
        {
            CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            Transform content = Find(go.transform, "Content Top") ?? Find(go.transform, "Container v2") ?? Find(go.transform, "Content Top - Continue-1");
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.3f)
            {
                if (go == null) yield break;
                cg.alpha = Mathf.Clamp01(k * 2f);
                if (content != null) content.localScale = Vector3.one * EaseOutBack(k);
                yield return null;
            }
            if (go == null) yield break;
            cg.alpha = 1f;
            if (content != null) content.localScale = Vector3.one;
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// <summary>Bật đúng khung theo độ khó (thay KPHardLevelUIElement của bản gốc).</summary>
        static void ApplyDifficultyVariants(Transform root, LevelDifficulty d)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                switch (t.name)
                {
                    case "NormalLevel": t.gameObject.SetActive(d == LevelDifficulty.Normal); break;
                    case "HardLevel": t.gameObject.SetActive(d == LevelDifficulty.Hard); break;
                    case "SuperHard":
                    case "SuperHart": t.gameObject.SetActive(d == LevelDifficulty.SuperHard); break;
                    case "CurrencyBg Normal": t.gameObject.SetActive(d == LevelDifficulty.Normal); break;
                    case "CurrencyBg Hard": t.gameObject.SetActive(d == LevelDifficulty.Hard); break;
                    case "CurrencyBg Super Hard": t.gameObject.SetActive(d == LevelDifficulty.SuperHard); break;
                }
            }
        }

        void Wire(Transform root, string path, Action action)
        {
            Button b = Find(root, path)?.GetComponent<Button>();
            if (b == null) { Debug.LogWarning($"[GameUI] Không thấy nút '{path}' trong {root.name}"); return; }
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); action?.Invoke(); });
        }

        static void SetText(Transform root, string path, string text)
        {
            TMP_Text t = Find(root, path)?.GetComponent<TMP_Text>();
            if (t != null) t.text = text;
        }

        //Thua
        public void ShowFail(int level, LevelDifficulty diff, string reason, Action onRetry)
        {
            _difficulty = diff;
            GameObject go = OpenPopup(_skin.failPrefab, "FailPopup");
            Transform r = go.transform;
            SetText(r, "Text - Inner Content - Level Text (1)", "Level " + level);
            Wire(r, "BTN-Try Again", () => { ClosePopup(); onRetry?.Invoke(); });
            Wire(r, "Specific-Close Button Variant", () => { ClosePopup(); onRetry?.Invoke(); });

            Time.timeScale = 0;
        }

        //Hết capacity
        public void ShowOutOfCapacity(LevelDifficulty diff, Action onAddCapacity, Action onGiveUp)
        {
            _difficulty = diff;
            GameObject go = OpenPopup(_skin.outOfCapacityPrefab, "OutOfCapacity", true);
            Transform r = go.transform;
            Wire(r, "BTN-Play On Gold", () => { ClosePopup(); onAddCapacity?.Invoke(); });
            Wire(r, "BTN-Give Up", () => { ClosePopup(); onGiveUp?.Invoke(); });
            Transform ab = Find(r, "AB_EGP");
            if (ab != null) ab.gameObject.SetActive(false);

            Time.timeScale = 0;
        }

        //Cài đặt
        public void ShowSettings(LevelDifficulty diff, Action onRestart, Action onClose)
        {
            _difficulty = diff;
            GameObject go = OpenPopup(_skin.settingsPrefab, "Settings");
            Transform r = go.transform;
            foreach (string n in new[] { "Panel", "Window" }) { Transform t = Find(r, n); if (t != null) t.gameObject.SetActive(true); }
            Transform debug = Find(r, "Release Debugger Opener");
            if (debug != null) debug.gameObject.SetActive(false);

            Time.timeScale = 0;

            Wire(r, "Close Button", () => { ClosePopup(); onClose?.Invoke(); });
            Wire(r, "Bottom/Restart", () => { ClosePopup(); onRestart?.Invoke(); });
            Wire(r, "Bottom/Home", () => { ClosePopup(); onRestart?.Invoke(); });
            SetupToggle(r, "SoundArea", () => AudioManager.SoundOn, v => AudioManager.SoundOn = v);
            SetupToggle(r, "MusicArea", () => AudioManager.MusicOn, v => AudioManager.MusicOn = v);
            SetupToggle(r, "Vibration", () => PlayerPrefs.GetInt("bb_vibration", 1) == 1, v => PlayerPrefs.SetInt("bb_vibration", v ? 1 : 0));
            FixTrimmedIcons(r);
        }

        /// <summary>
        /// Icon 4 nút Settings: sprite trong bản build đã bị cắt sát (vd "Volume on" 363×308) nhưng khung Image vẫn
        /// vuông 173×173 → bị kéo méo. Sprite lớn (cắt từ ảnh nguồn 512px) → khung = rect × 173/512 (đúng cỡ icon gốc);
        /// sprite nhỏ (không padding, vd Support_icon) → giữ tỉ lệ trong khung.
        /// </summary>
        static void FixTrimmedIcons(Transform root)
        {
            foreach (Image img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.name != "Icon" || img.sprite == null) continue;
                Vector2 size = img.sprite.rect.size;
                if (Mathf.Max(size.x, size.y) > 200f) img.rectTransform.sizeDelta = size * (173.35f / 512f);
                else img.preserveAspect = true;
            }
        }

        void SetupToggle(Transform root, string area, Func<bool> get, Action<bool> set)
        {
            Transform a = Find(root, area);
            if (a == null) return;
            Transform on = a.Find("Parent Active"), off = a.Find("Parent Passive");
            void Refresh()
            {
                bool v = get();
                if (on != null) on.gameObject.SetActive(v);
                if (off != null) off.gameObject.SetActive(!v);
            }
            Refresh();
            Button b = a.Find("SoundButton")?.GetComponent<Button>();
            if (b == null) return;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => { set(!get()); Refresh(); AudioManager.Play(Sfx.Button); });
        }

        //Thắng
        /// <summary>Màn đen → chữ B-O-U-N-C-E nảy lần lượt → logo BOUNCE BLAST + pháo hoa → bảng Level / PERFECT / Rewards.</summary>
        public IEnumerator PlayWinSequence(int level, LevelDifficulty diff, int coins, Action onContinue)
        {
            _difficulty = diff;
            GameObject go = Spawn(_skin.successPrefab, "SuccessPopup");
            if (go == null) { onContinue?.Invoke(); yield break; }
            _popup = go;
            UseCamera(go.GetComponent<Canvas>(), 30);
            ApplyDifficultyVariants(go.transform, diff);
            Transform r = go.transform;
            Transform container = Find(r, "Container v2");
            Transform logoArea = Find(r, "LogoArea");
            Transform logo = Find(r, "LogoArea/Logo");
            Transform liveOps = Find(r, "WinPanelLiveOpsManagement");
            if (liveOps != null) liveOps.gameObject.SetActive(false);
            Transform currency = Find(r, "CurrencyInstance (2)");
            if (currency != null) currency.gameObject.SetActive(false);

            //Logo
            if (container != null) container.gameObject.SetActive(false);
            Image dim = Img(r, "Dim", null);
            Stretch(dim.rectTransform);
            dim.transform.SetAsFirstSibling();
            dim.color = Color.black;
            if (logoArea != null && logo != null)
            {
                logoArea.gameObject.SetActive(true);
                logo.gameObject.SetActive(true);
                Animator anim = logo.GetComponent<Animator>();
                if (anim != null) anim.enabled = false; // tự diễn hoạt bằng code (clip gốc không đủ)
                PlayLogoEffects(logoArea, go.GetComponent<Canvas>());
                yield return AnimateLogo(Find(logo, "Animation") ?? logo);
            }
            // giữ màn logo + pháo hoa thêm một lúc cho kịp ngắm trước khi hiện bảng thắng (GameConfig.winFireworksTime)
            // thêm 2 tiếng pháo nổ rải trong lúc pháo hoa bắn (ngoài tiếng pháo khi chữ BLAST bật lên)
            float hold = _config != null ? _config.winFireworksTime : 2.4f;
            yield return new WaitForSecondsRealtime(hold * 0.25f);
            AudioManager.Play(Sfx.Confetti);
            yield return new WaitForSecondsRealtime(hold * 0.35f);
            AudioManager.Play(Sfx.Confetti);
            yield return new WaitForSecondsRealtime(hold * 0.4f);
            if (logoArea != null) logoArea.gameObject.SetActive(false);
            Destroy(dim.gameObject);

            //Bảng thắng
            if (container != null)
            {
                container.gameObject.SetActive(true);
                SetText(container, "LevelTMP", "Level " + level);
                SetText(container, "Prize TMP", coins.ToString());
                Wire(container, "Continue Button", () => { ClosePopup(); onContinue?.Invoke(); });
                Wire(container, "X2", () => { ClosePopup(); onContinue?.Invoke(); });
                CanvasGroup cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
                for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.35f)
                {
                    if (container == null) yield break;
                    container.localScale = Vector3.one * EaseOutBack(k);
                    cg.alpha = 1f;
                    yield return null;
                }
                if (container != null) container.localScale = Vector3.one;
                if (container != null) yield return PanelEffects(container, go.GetComponent<Canvas>());
            }
        }

        /// <summary>
        /// Hiệu ứng bảng thắng như video gốc: xu nổ tia sáng + sao (Coin/ExploteParticle, Fx_Star), quầng sáng AreaShine,
        /// chữ PERFECT! bật lên. Các ParticleSystem gốc nằm trong canvas nên phải vẽ trên canvas (sortingOrder) và
        /// bật lại scale (animator gốc đã mất).
        /// </summary>
        const float CoinLift = 45f; // dời đống xu lên để nằm trọn trong vòng tròn bục (đơn vị canvas)

        IEnumerator PanelEffects(Transform container, Canvas canvas)
        {
            FixTrimmedSprites(container);
            int order = canvas != null ? canvas.sortingOrder + 1 : 100;
            foreach (ParticleSystemRenderer r in container.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                if (canvas != null) r.sortingLayerID = canvas.sortingLayerID;
                r.sortingOrder = order;
            }
            Transform shine = Find(container, "AreaShine");
            // quầng sáng sau xu: plugin UIParticle gốc phóng particle theo UI → ~4.5 đơn vị world (≈ nửa bảng)
            if (shine != null) { shine.localScale = Vector3.one * 250f; ((RectTransform)shine).anchoredPosition = new Vector2(6f, -63f); }
            foreach (string n in new[] { "Coin/ExploteParticle", "Coin/Fx_Star", "ExploteParticle (1)" })
            {
                Transform t = Find(container, n);
                if (t == null) continue;
                t.gameObject.SetActive(true);
                ParticleSystem ps = t.GetComponent<ParticleSystem>();
                if (ps != null) { ps.Clear(true); ps.Play(true); }
            }
            Transform perfect = Find(container, "OutOfCapsity"); // khối chữ "PERFECT!" của prefab gốc (tên gốc)
            Transform coin = Find(container, "Coin/coinIcon");
            Transform coinRoot = Find(container, "Coin");
            if (perfect != null) perfect.localScale = Vector3.zero;

            // (video coin_panel) 1) đống xu rơi xuống: to & cao hơn rồi thu về đúng chỗ trên bục.
            // Tính theo THỜI GIAN TUYỆT ĐỐI (không cộng dồn deltaTime): khung hình bị khựng/không đều (Editor, canvas nặng)
            // chỉ làm hiện chậm một nhịp chứ không làm đống xu nhảy từng bước → hết giật.
            Vector3 coinPos = coin != null ? coin.localPosition + new Vector3(0f, CoinLift, 0f) : Vector3.zero;
            float coinStart = Time.unscaledTime;
            while (true)
            {
                float k = Mathf.Clamp01((Time.unscaledTime - coinStart) / 0.38f);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);   // ease-out cubic: mềm hơn khi chạm đáy
                if (coin != null)
                {
                    coin.localScale = Vector3.one * 0.85f * Mathf.Lerp(1.9f, 1f, e);
                    coin.localPosition = coinPos + new Vector3(0f, Mathf.Lerp(290f, 0f, e), 0f);
                }
                if (k >= 1f) break;
                yield return null;
            }
            if (coin != null) { coin.localScale = Vector3.one * 0.85f; coin.localPosition = coinPos; }

            // 2) chớp sáng trắng tròn + tia toả sau xu, 2 nhịp (~0.5s/nhịp)
            RawImage glow = null, rays = null, core = null;
            if (coinRoot != null && _skin.glowTexture != null)
            {
                glow = FxImage(coinRoot.parent, "CoinFlash", _skin.glowTexture, new Vector2(900f, 900f), coinRoot);
                core = FxImage(coinRoot.parent, "CoinFlashCore", _skin.glowTexture, new Vector2(620f, 560f), coinRoot); // lõi trắng chói phủ bục
                if (_skin.raysTexture != null) rays = FxImage(coinRoot.parent, "CoinRays", _skin.raysTexture, new Vector2(1000f, 1000f), coinRoot);
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
            if (coinRoot != null && _skin.sparkleTexture != null)
                UiSparkles.Play((RectTransform)coinRoot, _skin.sparkleTexture, new Vector2(520f, 380f));
            Transform cont = Find(container, "Continue Button");
            Transform x2 = Find(container, "X2");
            if (_skin.glowTexture != null)
            {
                if (cont != null) UiShineSweep.Attach((RectTransform)cont, _skin.glowTexture, 0f);
                if (x2 != null) UiShineSweep.Attach((RectTransform)x2, _skin.glowTexture, 0.25f);
            }
        }

        static float Pulse(float t, float start, float dur)
        {
            float k = (t - start) / dur;
            if (k < 0f || k > 1f) return 0f;
            return k < 0.3f ? k / 0.3f : 1f - (k - 0.3f) / 0.7f;
        }

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
        /// Pháo giấy + pháo hoa của màn thắng. Plugin UI-particle gốc đã bị strip (ConfettiBurst / UI Particle
        /// ConfettiFullscreen chỉ còn RectTransform) → dựng lại bằng UiConfetti theo thông số gốc.
        /// SF_Basic là ParticleSystem pháo hoa gốc (rocket + sub-emitter nổ) nhưng prefab để scale 0 và vẽ
        /// dưới nền đen của canvas → bật scale, đặt sortingOrder trên canvas.
        /// </summary>
        void PlayLogoEffects(Transform logoArea, Canvas canvas)
        {
            Texture sheet = _skin.confettiTexture;
            Transform confetti = Find(logoArea, "Confetti");
            if (confetti != null)
            {
                confetti.gameObject.SetActive(true);
                float delay = 0.95f; // nổ lúc chữ BLAST bật lên
                foreach (Transform t in confetti)
                {
                    if (!t.name.StartsWith("ConfettiBurst")) continue;
                    foreach (Transform child in t) child.gameObject.SetActive(false); // TrailParticle: plugin đã mất
                    UiConfetti.Play(t, UiConfetti.Mode.Burst, sheet, delay);
                    delay += 0.15f;
                }
            }
            Transform rain = Find(logoArea, "UI Particle ConfettiFullscreen");
            if (rain != null)
            {
                // phủ toàn màn hình (fitRect gốc): gắn thẳng vào canvas gốc, vẽ trên cùng
                var rt = (RectTransform)rain;
                if (canvas != null) rt.SetParent(canvas.transform, false);
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.localRotation = Quaternion.identity; // object gốc bị xoay ngược (phục vụ plugin cũ) → mưa rơi từ đáy
                rt.localScale = Vector3.one;
                rt.SetAsLastSibling();
                if (_popup != null) Destroy(rain.gameObject, 6f);
                UiConfetti.Play(rain, UiConfetti.Mode.Rain, sheet, 1f);
            }
            Transform fw = Find(logoArea, "SF_Basic");
            if (fw != null)
            {
                foreach (ParticleSystem p in fw.GetComponentsInChildren<ParticleSystem>(true))
                {
                    p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = p.main;
                    main.playOnAwake = false;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
                ParticleSystem rocket = fw.GetComponent<ParticleSystem>();
                if (rocket != null)
                {
                    rocket.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = rocket.main;
                    main.duration = 0.45f;          // ~2 tên lửa/giây
                    var shape = rocket.shape;
                    shape.radius = 3.5f;            // tản ra gần hết bề ngang
                }

                fw.gameObject.SetActive(true);
                // Tên lửa pháo hoa gốc: mô phỏng Local trong canvas (scale ≈ 0.01) thì chỉ bay được vài mm →
                // chuyển sang World, scale tuyệt đối = 1, đặt ở đáy màn hình, phóng liên tục như video gốc.
                fw.localScale = Vector3.one / Mathf.Max(fw.parent.lossyScale.x, 1e-4f); // lossyScale = 1
                var frt = fw as RectTransform;
                if (frt != null) frt.anchoredPosition = new Vector2(0f, -1150f);
                else fw.localPosition = new Vector3(0f, -1150f, 0f);

                int order = canvas != null ? canvas.sortingOrder + 1 : 100;
                foreach (ParticleSystemRenderer r in fw.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (canvas != null) r.sortingLayerID = canvas.sortingLayerID;
                    r.sortingOrder = order;
                }
                ParticleSystem ps = fw.GetComponent<ParticleSystem>();
                if (ps != null) { ps.Clear(true); ps.Play(true); }
            }
        }

        IEnumerator AnimateLogo(Transform anim)
        {
            string[] letters = { "b", "o", "u", "n", "c", "e" };
            var all = new List<Transform>();
            foreach (Transform t in anim) { all.Add(t); t.localScale = Vector3.zero; }
            // Sprite chữ trong bản build đã bị cắt sát (vd "blast" 465×158, "b" 133×157) nhưng khung Image vẫn là
            // ô vuông gốc (600×600, 250×250) → bị kéo méo. Đặt lại khung = kích thước sprite × 1.29
            // (1.29 = 600/465: "blast" phủ đúng bề ngang gốc), giữ nguyên tâm → chữ nền/chữ chính vẫn đúng tỉ lệ nhau.
            foreach (Image img in anim.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || img.name == "badge") continue;
                img.rectTransform.sizeDelta = img.sprite.rect.size * 1.29f;
            }
            Transform badge = anim.Find("badge");
            if (badge != null) yield return Pop(badge, 0.35f);
            foreach (string l in letters)
            {
                Transform bg = anim.Find(l + " bg"), fg = anim.Find(l);
                if (bg != null) StartCoroutine(Pop(bg, 0.18f));
                if (fg != null) StartCoroutine(Pop(fg, 0.18f));
                AudioManager.Play(Sfx.QmarkReveal);
                yield return new WaitForSecondsRealtime(0.12f);
            }
            Transform blast = anim.Find("blast");
            if (blast != null) yield return Pop(blast, 0.3f);
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

        //Helper
        static Transform Find(Transform root, string path)
        {
            if (root == null) return null;
            Transform direct = root.Find(path);
            if (direct != null) return direct;
            string last = path.Contains("/") ? path.Substring(path.LastIndexOf('/') + 1) : path;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != last) continue;
                if (!path.Contains("/")) return t;
                string full = t.name;
                for (Transform p = t.parent; p != null && p != root; p = p.parent) full = p.name + "/" + full;
                if (full.EndsWith(path)) return t;
            }
            return null;
        }

        static Button FindButton(Transform root, string name)
        {
            foreach (Button b in root.GetComponentsInChildren<Button>(true)) if (b.name == name) return b;
            return null;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        Image Img(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = sprite;
            return img;
        }

        TextMeshProUGUI Txt(Transform parent, string name, string text, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
            if (_skin.font != null) t.font = _skin.font;
            if (_skin.fontOutlineMaterial != null) t.fontSharedMaterial = _skin.fontOutlineMaterial;
            t.text = text;
            t.fontSize = size;
            t.alignment = TextAlignmentOptions.Center;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            return t;
        }

        static Sprite MakeRing(Color fill, Color ring, float ringWidth01, int size = 128)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    Color c = d > 1f - ringWidth01 ? ring : fill;
                    c.a *= Mathf.Clamp01((1f - d) * size * 0.5f);
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
