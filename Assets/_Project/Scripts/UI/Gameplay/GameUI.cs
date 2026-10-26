using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Bộ điều phối UI màn chơi – KHÔNG chứa chi tiết bên trong từng panel. Mỗi panel là một prefab đặt sẵn trong Hierarchy
    /// (Systems/UI/...) và có script View riêng giữ tham chiếu bằng Inspector (xem Docs/kien-truc/ui.md):
    ///   OverlayView (booster + viền cảnh báo + thông báo), HudView, CapacityBarView,
    ///   FailPopupView, SuccessPopupView, SettingsPopupView, OutOfCapacityPopupView.
    /// Các ô [SerializeField] dưới đây kéo thả sẵn trong scene. GameUI KHÔNG dựng UI lúc chạy và KHÔNG đặt Canvas
    /// (chế độ vẽ, camera, thứ tự, CanvasScaler đều lưu sẵn trong scene/prefab – xem Docs/kien-truc/ui.md mục "Quy tắc").
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public event Action<BoosterType> OnBoosterPressed;
        public event Action OnSettingsPressed;

        [Header("Các View đặt sẵn trong scene (UI/...)")]
        [SerializeField] OverlayView _overlayView;
        [SerializeField] HudView _hudView;
        [SerializeField] CapacityBarView _capacity;
        [SerializeField] FailPopupView _fail;
        [SerializeField] SuccessPopupView _success;
        [SerializeField] SettingsPopupView _settings;
        [SerializeField] OutOfCapacityPopupView _outOfCapacity;
        [SerializeField] BoosterBuyPopupView _boosterBuy;
        [SerializeField] BoosterTutorialPopupView _boosterTutorial;
        [Tooltip("Viền trắng khi nhấn giữ tăng tốc (prefab SpeedUpCanvas)")] [SerializeField] SpeedUpView _speedUp;

        Camera _cam;
        GameConfig _config;
        LevelDifficulty _difficulty;

        BoosterBarView _boosterBarView;

        PopupViewBase _activeView;   // popup đang mở (mọi popup đều có View riêng)
        public bool IsPopupOpen => _activeView != null;

        Camera _worldCam;

        /// <param name="uiCam">camera vẽ các canvas Screen Space (chiếu chuẩn)</param>
        /// <param name="worldCam">camera game – cho thanh capacity đặt trong thế giới</param>
        public void Build(GameConfig config, Camera uiCam, Camera worldCam)
        {
            _config = config;
            _cam = uiCam;
            _worldCam = worldCam;

            if (FindObjectOfType<EventSystem>() == null) Debug.LogError("Scene thiếu EventSystem (đặt sẵn dưới UI/) – nút bấm sẽ không chạy");

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
        /// Lấy một View đặt sẵn trong Hierarchy: ô [SerializeField] đã kéo, không có thì tìm dưới UI/ (kể cả đang tắt).
        /// KHÔNG dựng từ prefab lúc chạy – thiếu thì báo lỗi rõ để đặt vào scene. Kiểm tham chiếu ngay để thiếu ô nào báo ô đó.
        /// </summary>
        T GetView<T>(T assigned, string name, Func<T, string> validate) where T : Component
        {
            T v = assigned != null ? assigned : GetComponentInChildren<T>(true);
            if (v == null) { Debug.LogError(name + ": chưa đặt prefab vào Hierarchy (UI/...) – " + typeof(T).Name); return null; }
            string missing = validate != null ? validate(v) : null;
            if (missing != null) Debug.LogError(name + " chưa nối đủ tham chiếu " + typeof(T).Name + ": " + missing);
            return v;
        }

        T GetPopupView<T>(T assigned, string name) where T : PopupViewBase => GetView(assigned, name, x => x.Validate());

        /// <summary>
        /// Lớp UI phủ lên bàn chơi = prefab OverlayCanvas với script <see cref="OverlayView"/> (booster + viền cảnh báo + thông báo).
        /// GameUI chỉ nối sự kiện bấm booster.
        /// </summary>
        void BuildOverlay()
        {
            _overlayView = GetView(_overlayView, "OverlayCanvas", v => v.Validate());
            if (_overlayView == null) return;
            _overlayView.gameObject.SetActive(true);
            BindBoosters();
            _speedUp = GetView(_speedUp, "SpeedUpCanvas", v => v.Validate());
        }

        /// <summary>Hiện / ẩn viền trắng tăng tốc (InputManager gọi khi bắt đầu / thôi nhấn giữ).</summary>
        public void SetSpeedUp(bool on, float speed = 1.5f)
        {
            if (_speedUp != null) _speedUp.SetOn(on, speed);
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
        /// GameUI chỉ nghe sự kiện bấm Settings và ra lệnh <see cref="SetLevel"/>.
        /// </summary>
        void BuildHud()
        {
            _hudView = GetView(_hudView, "HUD (GameCanvas)", v => v.Validate());
            if (_hudView == null) return;
            _hudView.gameObject.SetActive(true);
            _hudView.OnSettingsPressed += () => OnSettingsPressed?.Invoke();
        }

        public void SetLevel(int level, LevelDifficulty difficulty)
        {
            _difficulty = difficulty;
            if (_hudView != null) _hudView.Apply(level, difficulty);
            // nút + dải booster đổi tông theo độ khó (hình/màu từng độ khó nằm trong BoosterBar.prefab)
            if (_boosterBarView != null) _boosterBarView.ApplyDifficulty(difficulty);
        }

        //Capacity
        /// <summary>
        /// Thanh sức chứa là prefab có script quản lý riêng (<see cref="CapacityBarView"/>).
        /// Tư thế, camera, font đã lưu sẵn trong prefab/scene; ở đây chỉ mỗi frame bảo nó vẽ %.
        /// </summary>
        void BuildCapacityBar()
        {
            _capacity = GetView(_capacity, "CapacityBar", v => v.Validate());
            if (_capacity == null) return;
            _capacity.gameObject.SetActive(true);
        }

        /// <summary>Thanh Capacity (hiệu ứng booster +25% bám vào đây).</summary>
        public Transform CapacityBarTransform => _capacity != null ? _capacity.transform : null;

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


        public void SetBoosterLocked(BoosterType type, bool locked) { if (_boosterBarView != null) _boosterBarView.SetLocked(type, locked); }
        public void ShowBoosterLockHint(BoosterType type, int unlockLevel) { if (_boosterBarView != null) _boosterBarView.ShowUnlockHint(type, unlockLevel); }
        public Vector2 BoosterButtonScreenPos(BoosterType type) => _boosterBarView != null ? _boosterBarView.ButtonScreenPos(type) : Vector2.zero;
        public Coroutine PlayBoosterUnlock(BoosterType type, Action onPop, Action onDone) => _boosterBarView != null ? StartCoroutine(_boosterBarView.PlayUnlock(type, onPop, onDone)) : null;

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
                _fail = GetPopupView(_fail, "FailPopup");
                if (_fail != null) { _fail.OnRetry += () => { ClosePopup(); _failRetry?.Invoke(); }; _failWired = true; }
            }
            if (_fail == null) return;
            _failRetry = onRetry;
            _fail.Show(level, diff);
            _activeView = _fail;

            Time.timeScale = 0;
        }

        // Mỗi popup chỉ được nối sự kiện MỘT lần – không dựa vào "ô còn trống" vì View đặt sẵn trong scene đã được kéo vào ô.
        bool _failWired, _outWired, _settingsWired, _successWired, _buyWired;

        Action _failRetry;

        //Hết capacity
        Action _outPlayOn, _outGiveUp;

        public void ShowOutOfCapacity(LevelDifficulty diff, Action onAddCapacity, Action onGiveUp)
        {
            _difficulty = diff;
            ClosePopup();
            if (!_outWired)
            {
                _outOfCapacity = GetPopupView(_outOfCapacity, "OutOfCapacityPopup");
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
            _outOfCapacity.Show(diff);
            _activeView = _outOfCapacity;

            Time.timeScale = 0;
        }

        //Mua booster (hết lượt)
        Action<BoosterType, int> _buyAction;

        /// <summary>Mở popup mua booster khi nút booster đã hết lượt. Bấm mua → <paramref name="onBuy"/> (bản clone: miễn phí).</summary>
        public void ShowBoosterBuy(BoosterType type, LevelDifficulty diff, Action<BoosterType, int> onBuy)
        {
            _difficulty = diff;
            ClosePopup();
            if (!_buyWired)
            {
                _boosterBuy = GetPopupView(_boosterBuy, "BoosterBuyPopup");
                if (_boosterBuy != null)
                {
                    _buyWired = true;
                    _boosterBuy.OnClose += ClosePopup;
                    _boosterBuy.OnBuy += (t, n) => { ClosePopup(); _buyAction?.Invoke(t, n); };
                }
            }
            if (_boosterBuy == null) return;
            _buyAction = onBuy;
            _boosterBuy.Show(type, diff);
            _activeView = _boosterBuy;

            Time.timeScale = 0;
        }

        bool _tutorialWired;
        Action<BoosterType> _tutorialUse;
        Action _tutorialClosed;

        /// <summary>Popup hướng dẫn booster vừa mở khoá. Bấm Use → <paramref name="onUse"/> (dùng booster ngay); bấm Close → chỉ đóng.</summary>
        public void ShowBoosterTutorial(BoosterType type, LevelDifficulty diff, Action<BoosterType> onUse, Action onClosed = null, int freeCount = 0)
        {
            _difficulty = diff;
            ClosePopup();
            if (!_tutorialWired)
            {
                _boosterTutorial = GetPopupView(_boosterTutorial, "BoosterTutorialPopup");
                if (_boosterTutorial != null)
                {
                    _tutorialWired = true;
                    _boosterTutorial.OnClose += () => { ClosePopup(); _tutorialClosed?.Invoke(); };
                    _boosterTutorial.OnUse += t => { ClosePopup(); _tutorialUse?.Invoke(t); _tutorialClosed?.Invoke(); };
                }
            }
            if (_boosterTutorial == null) return;
            _tutorialUse = onUse; _tutorialClosed = onClosed;
            _boosterTutorial.Show(type, diff, freeCount);
            _activeView = _boosterTutorial;

            Time.timeScale = 0;
        }

        /// <summary>Popup "New Feature" của cơ chế mới (dùng chung khung với popup hướng dẫn booster). Đóng (OK / X) → <paramref name="onClosed"/>.</summary>
        public void ShowFeatureIntro(string title, string description, Sprite icon, LevelDifficulty diff, Action onClosed)
        {
            ShowBoosterTutorial(BoosterType.MagicHand, diff, null, onClosed);   // nối sự kiện + dựng popup (nội dung ghi đè ngay sau đó)
            if (_boosterTutorial != null) _boosterTutorial.ShowFeature(title, description, icon, diff);
        }

        //Cài đặt
        Action _settingsRestart, _settingsClose, _settingsHome;

        public void ShowSettings(LevelDifficulty diff, Action onRestart, Action onClose, Action onHome)
        {
            _difficulty = diff;
            ClosePopup();
            if (!_settingsWired)
            {
                _settings = GetPopupView(_settings, "Settings");
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
                _success = GetPopupView(_success, "SuccessPopup");
                if (_success != null) { _success.OnContinue += () => { ClosePopup(); _winContinue?.Invoke(); }; _successWired = true; }
            }
            if (_success == null) { onContinue?.Invoke(); yield break; }
            _winContinue = onContinue;
            BoosterType nextType; float from, to;
            if (_config != null && _config.NextBoosterUnlock(level, out nextType, out from, out to)) _success.SetFeature(_config.BoosterIcon(nextType), from, to);
            else _success.SetFeature(null, 0f, 0f);
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
