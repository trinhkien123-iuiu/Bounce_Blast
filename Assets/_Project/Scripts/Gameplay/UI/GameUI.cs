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

        HudView _hudView;

        CapacityBarView _capacity;

        RectTransform _overlay; // canvas của mình: booster, toast, vignette
        Image _vignette;
        float _vignetteTarget;
        TextMeshProUGUI _toast;
        float _toastTime;
        BoosterBarView _boosterBarView;

        PopupViewBase _activeView;   // popup đang mở (mọi popup đều có View riêng)
        public bool IsPopupOpen => _activeView != null;

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
            HideScenePopups();
        }

        /// <summary>
        /// Popup đặt sẵn trong scene PHẢI tắt khi vào game, bất kể scene đang lưu trạng thái nào. Prefab gốc có CanvasGroup
        /// alpha 0 nhưng vẫn chặn raycast – một popup lỡ bật sẽ trong suốt mà nuốt mọi cú bấm vào khay đạn.
        /// </summary>
        void HideScenePopups()
        {
            foreach (PopupViewBase v in GetComponentsInChildren<PopupViewBase>(true)) v.gameObject.SetActive(false);
        }

        /// <summary>
        /// Lấy View của một popup: ưu tiên bản ĐẶT SẴN trong Hierarchy (kể cả đang tắt); chưa đặt thì dựng từ prefab
        /// để scene trống vẫn chạy được. Kiểm tra tham chiếu ngay để thiếu ô nào báo rõ ô đó.
        /// </summary>
        T GetPopupView<T>(GameObject prefab, string name) where T : PopupViewBase
        {
            T v = GetComponentInChildren<T>(true);
            if (v == null && prefab != null)
            {
                GameObject go = Instantiate(prefab, transform);
                go.name = name;
                go.SetActive(false);
                v = go.GetComponent<T>();
            }
            if (v == null) { Debug.LogError(name + ": không có trong scene và prefab thiếu script " + typeof(T).Name); return null; }
            string missing = v.Validate();
            if (missing != null) Debug.LogError(name + " chưa nối đủ tham chiếu " + typeof(T).Name + ": " + missing);
            return v;
        }

        /// <summary>Mở một popup đã có View: gán camera/thứ tự vẽ cho Canvas rồi nhờ View tự hiện.</summary>
        void PrepareCanvas(PopupViewBase v)
        {
            if (v.Canvas == null) return;
            UseCamera(v.Canvas, 30);
            CanvasScaler cs = v.Canvas.GetComponent<CanvasScaler>();
            if (cs != null) SetupScaler(cs);
        }

        /// <summary>
        /// Lớp UI phủ lên bàn chơi = prefab (UiSkin.overlayCanvasPrefab → Assets/_Project/UI/Prefabs/OverlayCanvas.prefab):
        /// Canvas + CanvasScaler + GraphicRaycaster, bên trong có BoosterBar, Vignette và Toast.
        /// Code chỉ nối camera (không serialize được) rồi tìm con theo tên; bố cục/hình ảnh sửa trong prefab.
        /// </summary>
        void BuildOverlay()
        {
            GameObject go = Spawn(_skin.overlayCanvasPrefab, "OverlayCanvas");
            if (go == null) { Debug.LogError("Không có OverlayCanvas trong scene và UiSkin.overlayCanvasPrefab cũng chưa gán"); return; }
            UseCamera(go.GetComponent<Canvas>(), 5);
            SetupScaler(go.GetComponent<CanvasScaler>());
            _overlay = (RectTransform)go.transform;

            Transform vig = _overlay.Find("Vignette");
            Transform toast = _overlay.Find("Toast");
            _vignette = vig != null ? vig.GetComponent<Image>() : null;
            _toast = toast != null ? toast.GetComponent<TextMeshProUGUI>() : null;
            if (_vignette == null || _toast == null) { Debug.LogError("OverlayCanvas.prefab thiếu node \"Vignette\" hoặc \"Toast\""); return; }
            _toast.alpha = 0f;

            BindBoosters();
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

        /// <summary>
        /// Lấy một node UI: nếu nó đã được ĐẶT SẴN trong scene (con của UI, đúng tên) thì dùng luôn – nhờ vậy mở Hierarchy
        /// là thấy và sửa được ngay. Chưa đặt thì mới tạo từ prefab lúc chạy, để scene trống vẫn chạy được.
        /// </summary>
        GameObject Spawn(GameObject prefab, string name)
        {
            Transform placed = transform.Find(name);
            GameObject go;
            if (placed != null) go = placed.gameObject;
            else
            {
                if (prefab == null) return null;
                go = Instantiate(prefab, transform);
                go.name = name;
            }
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
        /// <summary>Tâm dọc hàng HUD (nút Settings, pixel màn hình) – mép trên khung máng đặt ngang đây như game gốc. -1 nếu chưa dựng.</summary>
        public float HudCenterScreenY
        {
            get
            {
                if (_hudView == null || _hudView.SettingsRect == null) return -1f;
                Canvas.ForceUpdateCanvases();
                var c = new Vector3[4];
                _hudView.SettingsRect.GetWorldCorners(c);
                return RectTransformUtility.WorldToScreenPoint(_cam, (c[0] + c[2]) * 0.5f).y;
            }
        }

        /// <summary>Mép trên hàng nút booster (pixel màn hình) – khay đạn phải nằm trên mốc này. -1 nếu chưa dựng.</summary>
        public float BoosterTopScreenY
        {
            get
            {
                if (_boosterBarView == null || _boosterBarView.ButtonsRow == null) return -1f;
                Canvas.ForceUpdateCanvases();
                var c = new Vector3[4];
                _boosterBarView.ButtonsRow.GetWorldCorners(c);
                return RectTransformUtility.WorldToScreenPoint(_cam, c[1]).y;
            }
        }

        /// <summary>
        /// HUD là prefab có script quản lý riêng (<see cref="HudView"/>): giữ badge Level, nút Settings và kiểu dáng theo độ khó.
        /// GameUI chỉ gán camera, nghe sự kiện bấm Settings và ra lệnh <see cref="SetLevel"/>.
        /// </summary>
        void BuildHud()
        {
            GameObject hud = Spawn(_skin.hudPrefab, "HUD (GameCanvas)");
            if (hud == null) return;
            _hudView = hud.GetComponent<HudView>();
            if (_hudView == null) { Debug.LogError("GameCanvas.prefab thiếu script HudView"); return; }
            string missing = _hudView.Validate();
            if (missing != null) Debug.LogError("GameCanvas.prefab chưa nối đủ tham chiếu HudView: " + missing);
            UseCamera(_hudView.Canvas, 10);
            _hudView.OnSettingsPressed += () => OnSettingsPressed?.Invoke();
        }

        public void SetLevel(int level, LevelDifficulty difficulty)
        {
            _difficulty = difficulty;
            if (_hudView != null) _hudView.Apply(level, difficulty);
            // dải booster dưới cùng đổi tông theo độ khó (màu đo từ ảnh gốc: HardBG đỏ / SuperHardBG navy tối)
            if (_boosterBarView != null)
                _boosterBarView.SetStripColor(difficulty == LevelDifficulty.SuperHard ? new Color32(38, 40, 71, 255)
                                            : difficulty == LevelDifficulty.Hard ? new Color32(190, 42, 44, 255)
                                            : new Color32(71, 46, 226, 255));
        }

        //Capacity
        /// <summary>
        /// Thanh sức chứa là prefab có script quản lý riêng (<see cref="CapacityBarView"/>).
        /// Tư thế đã nung trong prefab (Docs 5.9); ở đây chỉ đưa cho nó camera + font, rồi mỗi frame bảo nó vẽ %.
        /// </summary>
        void BuildCapacityBar()
        {
            GameObject cap = Spawn(_skin.capacityBarPrefab, "CapacityBar");
            if (cap == null) return;
            _capacity = cap.GetComponent<CapacityBarView>();
            if (_capacity == null) { Debug.LogError("CapacityBar.prefab thiếu script CapacityBarView"); return; }
            string missing = _capacity.Validate();
            if (missing != null) Debug.LogError("CapacityBar.prefab chưa nối đủ tham chiếu: " + missing);
            _capacity.Bind(_worldCam, 4, _skin.font, _skin.fontOutlineMaterial);
        }

        public void SetCapacity(float fill, bool started, bool infinite)
        {
            if (_capacity != null) _capacity.SetFill(fill, started, infinite);
            if (_boosterBarView != null) _boosterBarView.SetWarn(!infinite && started && fill >= 0.9f);
            _vignetteTarget = fill >= 0.75f ? Mathf.InverseLerp(0.75f, 1f, fill) : 0f;
        }

        void LateUpdate()
        {
            float breathe = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f);
            Color vc = _vignette.color;
            vc.a = Mathf.MoveTowards(vc.a, _vignetteTarget * 0.55f * breathe, Time.unscaledDeltaTime * 2f);
            _vignette.color = vc;

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
        /// <summary>
        /// Thanh booster là prefab có script quản lý riêng (<see cref="BoosterBarView"/> gắn trên gốc prefab):
        /// nó giữ tham chiếu tới từng nút/badge/mũi tên bằng Inspector và lo phần hiển thị.
        /// GameUI chỉ nối sự kiện bấm và ra lệnh (đổi số lượt, đổi tông dải nền, bật mũi tên).
        /// </summary>
        void BindBoosters()
        {
            _boosterBarView = _overlay.GetComponentInChildren<BoosterBarView>(true);
            if (_boosterBarView == null) { Debug.LogError("OverlayCanvas.prefab thiếu BoosterBar (script BoosterBarView)"); return; }
            string missing = _boosterBarView.Validate();
            if (missing != null) Debug.LogError("BoosterBar.prefab chưa nối đủ tham chiếu: " + missing);
            _boosterBarView.OnPressed += t => OnBoosterPressed?.Invoke(t);
        }


        public void SetBooster(BoosterType type, int count, bool armed)
        {
            if (_boosterBarView != null) _boosterBarView.SetBooster(type, count, armed);
        }

        //Popup chung
        /// <summary>Đóng popup đang mở: chỉ TẮT (không huỷ) vì popup đặt sẵn trong Hierarchy và dùng lại.</summary>
        public void ClosePopup()
        {
            if (_activeView != null) { _activeView.Hide(); _activeView = null; }
            Time.timeScale = 1;
        }

        //Thua
        FailPopupView _fail;

        public void ShowFail(int level, LevelDifficulty diff, string reason, Action onRetry)
        {
            _difficulty = diff;
            ClosePopup();
            if (_fail == null)
            {
                _fail = GetPopupView<FailPopupView>(_skin.failPrefab, "FailPopup");
                if (_fail != null) _fail.OnRetry += () => { ClosePopup(); _failRetry?.Invoke(); };
            }
            if (_fail == null) return;
            _failRetry = onRetry;
            PrepareCanvas(_fail);
            _fail.Show(level, diff);
            _activeView = _fail;

            Time.timeScale = 0;
        }

        Action _failRetry;

        //Hết capacity
        OutOfCapacityPopupView _outOfCapacity;
        Action _outPlayOn, _outGiveUp;

        public void ShowOutOfCapacity(LevelDifficulty diff, Action onAddCapacity, Action onGiveUp)
        {
            _difficulty = diff;
            ClosePopup();
            if (_outOfCapacity == null)
            {
                _outOfCapacity = GetPopupView<OutOfCapacityPopupView>(_skin.outOfCapacityPrefab, "OutOfCapacityPopup");
                if (_outOfCapacity != null)
                {
                    _outOfCapacity.OnPlayOn += () => { ClosePopup(); _outPlayOn?.Invoke(); };
                    _outOfCapacity.OnGiveUp += () => { ClosePopup(); _outGiveUp?.Invoke(); };
                }
            }
            if (_outOfCapacity == null) return;
            _outPlayOn = onAddCapacity;
            _outGiveUp = onGiveUp;
            PrepareCanvas(_outOfCapacity);
            _outOfCapacity.Show(diff);
            _activeView = _outOfCapacity;

            Time.timeScale = 0;
        }

        //Cài đặt
        SettingsPopupView _settings;
        Action _settingsRestart, _settingsClose;

        public void ShowSettings(LevelDifficulty diff, Action onRestart, Action onClose)
        {
            _difficulty = diff;
            ClosePopup();
            if (_settings == null)
            {
                _settings = GetPopupView<SettingsPopupView>(_skin.settingsPrefab, "Settings");
                if (_settings != null)
                {
                    _settings.OnClose += () => { ClosePopup(); _settingsClose?.Invoke(); };
                    _settings.OnRestart += () => { ClosePopup(); _settingsRestart?.Invoke(); };
                    // View chỉ báo "người chơi vừa bật/tắt"; ghi vào âm thanh/PlayerPrefs là việc của GameUI
                    _settings.OnSoundChanged += v => AudioManager.SoundOn = v;
                    _settings.OnMusicChanged += v => AudioManager.MusicOn = v;
                    _settings.OnVibrationChanged += v => PlayerPrefs.SetInt("bb_vibration", v ? 1 : 0);
                }
            }
            if (_settings == null) return;
            _settingsRestart = onRestart;
            _settingsClose = onClose;
            PrepareCanvas(_settings);
            _settings.Show(diff, AudioManager.SoundOn, AudioManager.MusicOn, PlayerPrefs.GetInt("bb_vibration", 1) == 1);
            _activeView = _settings;

            Time.timeScale = 0;
        }

        //Thắng
        /// <summary>Màn đen → chữ B-O-U-N-C-E nảy lần lượt → logo BOUNCE BLAST + pháo hoa → bảng Level / PERFECT / Rewards.</summary>
        SuccessPopupView _success;
        Action _winContinue;

        /// <summary>
        /// Chạy chuỗi màn thắng. Toàn bộ hiệu ứng (logo BOUNCE BLAST, pháo, bảng thắng, xu rơi…) do
        /// <see cref="SuccessPopupView"/> lo; GameUI chỉ chuẩn bị Canvas, chờ chuỗi chạy xong và chuyển tiếp nút Continue.
        /// </summary>
        public IEnumerator PlayWinSequence(int level, LevelDifficulty diff, int coins, Action onContinue)
        {
            _difficulty = diff;
            ClosePopup();
            if (_success == null)
            {
                _success = GetPopupView<SuccessPopupView>(_skin.successPrefab, "SuccessPopup");
                if (_success != null) _success.OnContinue += () => { ClosePopup(); _winContinue?.Invoke(); };
            }
            if (_success == null) { onContinue?.Invoke(); yield break; }
            _winContinue = onContinue;
            PrepareCanvas(_success);
            _activeView = _success;
            yield return _success.Play(level, diff, coins, _config != null ? _config.winFireworksTime : 2.4f);
        }

        /// <summary>
        /// Hiệu ứng bảng thắng như video gốc: xu nổ tia sáng + sao (Coin/ExploteParticle, Fx_Star), quầng sáng AreaShine,
        /// chữ PERFECT! bật lên. Các ParticleSystem gốc nằm trong canvas nên phải vẽ trên canvas (sortingOrder) và
        /// bật lại scale (animator gốc đã mất).
        /// </summary>

        //Helper
    }
}
