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
    /// Bộ điều phối UI màn chơi – KHÔNG chứa chi tiết bên trong từng panel. Mỗi panel là một prefab đặt sẵn trong Hierarchy
    /// (Systems/UI/...) và có script View riêng giữ tham chiếu bằng Inspector (xem Docs/kien-truc-ui-dung.md):
    ///   OverlayView (booster + viền cảnh báo + thông báo), HudView, CapacityBarView,
    ///   FailPopupView, SuccessPopupView, SettingsPopupView, OutOfCapacityPopupView.
    /// Các ô [SerializeField] dưới đây kéo thả sẵn trong scene; ô nào để trống thì GameUI tự dựng từ prefab trong UiSkin
    /// lúc chạy (scene trống vẫn chạy được). Việc duy nhất code còn làm với Canvas là gán camera UI (không serialize được).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public event Action<BoosterType> OnBoosterPressed;
        public event Action OnSettingsPressed;

        static readonly Vector2 RefResolution = new Vector2(1242f, 2688f); // CanvasScaler của game gốc

        [Header("Các View đặt sẵn trong scene (để trống = tự dựng từ prefab lúc chạy)")]
        [SerializeField] OverlayView _overlayView;
        [SerializeField] HudView _hudView;
        [SerializeField] CapacityBarView _capacity;
        [SerializeField] FailPopupView _fail;
        [SerializeField] SuccessPopupView _success;
        [SerializeField] SettingsPopupView _settings;
        [SerializeField] OutOfCapacityPopupView _outOfCapacity;

        UiSkin _skin;
        Camera _cam;
        GameConfig _config;
        LevelDifficulty _difficulty;

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
        /// Lấy một View: dùng bản đã kéo sẵn vào ô [SerializeField] (assigned); chưa kéo thì tìm bản đặt sẵn dưới Systems/UI
        /// (kể cả đang tắt); vẫn không có mới dựng từ prefab để scene trống vẫn chạy được. Kiểm tra tham chiếu bằng
        /// <paramref name="validate"/> ngay để thiếu ô nào báo rõ ô đó.
        /// </summary>
        T GetView<T>(T assigned, GameObject prefab, string name, Func<T, string> validate) where T : Component
        {
            T v = assigned != null ? assigned : GetComponentInChildren<T>(true);
            if (v == null && prefab != null)
            {
                GameObject go = Instantiate(prefab, transform);
                go.name = name;
                v = go.GetComponent<T>();
            }
            if (v == null) { Debug.LogError(name + ": không có trong scene và prefab thiếu script " + typeof(T).Name); return null; }
            string missing = validate != null ? validate(v) : null;
            if (missing != null) Debug.LogError(name + " chưa nối đủ tham chiếu " + typeof(T).Name + ": " + missing);
            return v;
        }

        /// <summary>Phiên bản cho popup (View kế thừa PopupViewBase): dựng từ prefab thì để TẮT sẵn.</summary>
        T GetPopupView<T>(T assigned, GameObject prefab, string name) where T : PopupViewBase
        {
            bool existed = assigned != null || GetComponentInChildren<T>(true) != null;
            T v = GetView(assigned, prefab, name, x => x.Validate());
            if (v != null && !existed) v.gameObject.SetActive(false);
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
        /// Lớp UI phủ lên bàn chơi = prefab OverlayCanvas với script <see cref="OverlayView"/> (booster + viền cảnh báo + thông báo).
        /// GameUI chỉ gán camera cho Canvas rồi nối sự kiện bấm booster.
        /// </summary>
        void BuildOverlay()
        {
            _overlayView = GetView(_overlayView, _skin.overlayCanvasPrefab, "OverlayCanvas", v => v.Validate());
            if (_overlayView == null) return;
            _overlayView.gameObject.SetActive(true);
            UseCamera(_overlayView.Canvas, 5);
            SetupScaler(_overlayView.Canvas.GetComponent<CanvasScaler>());
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
            _hudView = GetView(_hudView, _skin.hudPrefab, "HUD (GameCanvas)", v => v.Validate());
            if (_hudView == null) return;
            _hudView.gameObject.SetActive(true);
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
            _capacity = GetView(_capacity, _skin.capacityBarPrefab, "CapacityBar", v => v.Validate());
            if (_capacity == null) return;
            _capacity.gameObject.SetActive(true);
            _capacity.Bind(_worldCam, 4, _skin.font, _skin.fontOutlineMaterial);
        }

        public void SetCapacity(float fill, bool started, bool infinite)
        {
            if (_capacity != null) _capacity.SetFill(fill, started, infinite);
            if (_boosterBarView != null) _boosterBarView.SetWarn(!infinite && started && fill >= 0.9f);
            if (_overlayView != null) _overlayView.SetDanger(fill);
        }

        public void Toast(string message, float seconds = 1.6f)
        {
            if (_overlayView != null) _overlayView.ShowToast(message, seconds);
        }

        //Booster
        /// <summary>
        /// Thanh booster là prefab có script quản lý riêng (<see cref="BoosterBarView"/> gắn trên gốc prefab):
        /// nó giữ tham chiếu tới từng nút/badge/mũi tên bằng Inspector và lo phần hiển thị.
        /// GameUI chỉ nối sự kiện bấm và ra lệnh (đổi số lượt, đổi tông dải nền, bật mũi tên).
        /// </summary>
        void BindBoosters()
        {
            _boosterBarView = _overlayView != null ? _overlayView.Booster : null;
            if (_boosterBarView == null) { Debug.LogError("OverlayCanvas thiếu BoosterBar (ô booster của OverlayView chưa nối)"); return; }
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
        public void ShowFail(int level, LevelDifficulty diff, string reason, Action onRetry)
        {
            _difficulty = diff;
            ClosePopup();
            if (!_failWired)
            {
                _fail = GetPopupView(_fail, _skin.failPrefab, "FailPopup");
                if (_fail != null) { _fail.OnRetry += () => { ClosePopup(); _failRetry?.Invoke(); }; _failWired = true; }
            }
            if (_fail == null) return;
            _failRetry = onRetry;
            PrepareCanvas(_fail);
            _fail.Show(level, diff);
            _activeView = _fail;

            Time.timeScale = 0;
        }

        // Mỗi popup chỉ được nối sự kiện MỘT lần – không dựa vào "ô còn trống" vì View đặt sẵn trong scene đã được kéo vào ô.
        bool _failWired, _outWired, _settingsWired, _successWired;

        Action _failRetry;

        //Hết capacity
        Action _outPlayOn, _outGiveUp;

        public void ShowOutOfCapacity(LevelDifficulty diff, Action onAddCapacity, Action onGiveUp)
        {
            _difficulty = diff;
            ClosePopup();
            if (!_outWired)
            {
                _outOfCapacity = GetPopupView(_outOfCapacity, _skin.outOfCapacityPrefab, "OutOfCapacityPopup");
                if (_outOfCapacity != null)
                {
                    _outWired = true;
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
        Action _settingsRestart, _settingsClose, _settingsHome;

        public void ShowSettings(LevelDifficulty diff, Action onRestart, Action onClose, Action onHome)
        {
            _difficulty = diff;
            ClosePopup();
            if (!_settingsWired)
            {
                _settings = GetPopupView(_settings, _skin.settingsPrefab, "Settings");
                if (_settings != null)
                {
                    _settingsWired = true;
                    _settings.OnClose += () => { ClosePopup(); _settingsClose?.Invoke(); };
                    _settings.OnRestart += () => { ClosePopup(); _settingsRestart?.Invoke(); };
                    _settings.OnHome += () => { ClosePopup(); _settingsHome?.Invoke(); };
                    // View chỉ báo "người chơi vừa bật/tắt"; ghi vào âm thanh/PlayerPrefs là việc của GameUI
                    _settings.OnSoundChanged += v => AudioManager.SoundOn = v;
                    _settings.OnMusicChanged += v => AudioManager.MusicOn = v;
                    _settings.OnVibrationChanged += v => PlayerPrefs.SetInt("bb_vibration", v ? 1 : 0);
                }
            }
            if (_settings == null) return;
            _settingsRestart = onRestart;
            _settingsClose = onClose;
            _settingsHome = onHome;
            PrepareCanvas(_settings);
            _settings.Show(diff, AudioManager.SoundOn, AudioManager.MusicOn, PlayerPrefs.GetInt("bb_vibration", 1) == 1);
            _activeView = _settings;

            Time.timeScale = 0;
        }

        //Thắng
        /// <summary>Màn đen → chữ B-O-U-N-C-E nảy lần lượt → logo BOUNCE BLAST + pháo hoa → bảng Level / PERFECT / Rewards.</summary>
        Action _winContinue;

        /// <summary>
        /// Chạy chuỗi màn thắng. Toàn bộ hiệu ứng (logo BOUNCE BLAST, pháo, bảng thắng, xu rơi…) do
        /// <see cref="SuccessPopupView"/> lo; GameUI chỉ chuẩn bị Canvas, chờ chuỗi chạy xong và chuyển tiếp nút Continue.
        /// </summary>
        public IEnumerator PlayWinSequence(int level, LevelDifficulty diff, int coins, Action onContinue)
        {
            _difficulty = diff;
            ClosePopup();
            if (!_successWired)
            {
                _success = GetPopupView(_success, _skin.successPrefab, "SuccessPopup");
                if (_success != null) { _success.OnContinue += () => { ClosePopup(); _winContinue?.Invoke(); }; _successWired = true; }
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
