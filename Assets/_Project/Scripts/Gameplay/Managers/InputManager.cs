using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Quản lý ĐẦU VÀO: đọc chuột / touch, đổi vị trí chạm thành "bấm vào hộp đạn" hoặc "bấm vào khối" (khi Hammer lên cò),
    /// rồi ra lệnh cho <see cref="LaunchManager"/> / <see cref="BoosterManager"/>. Không chứa luật chơi.
    /// Nhấn giữ một hộp bấm được thì hộp lắc như thạch (<see cref="AmmoBox.SetHeld"/>); thả tay trên đúng hộp đó thì bắn,
    /// kéo ngón ra khỏi hộp rồi thả thì huỷ (không bắn).
    /// Nhấn giữ chỗ trống trong khu khay đạn (không trúng hộp) = tăng tốc chơi (Time.timeScale → <see cref="holdSpeed"/>) + viền trắng và chữ báo (GameUI.SetSpeedUp); chỉ khi trên bàn đang có bi, thả tay hoặc hết bi là thôi.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        [Tooltip("Bật: nhấn giữ hộp thì lắc, THẢ TAY trên hộp mới bắn. Tắt: bắn ngay lúc chạm xuống như trước (không có cú lắc khi giữ).")]
        [SerializeField] bool fireOnRelease = true;
        [Tooltip("Giữ lâu hơn số giây này thì hộp mới núng nính (kéo ngón ra khỏi hộp vẫn lắc tiếp); nhả sớm hơn = coi là một cú CLICK bình thường (không lắc). Chỉ có tác dụng khi bật fireOnRelease.")]
        [SerializeField, Min(0f)] float holdToWobble = 0.5f;

        [Header("Tăng tốc – nhấn giữ chỗ trống trong khu khay đạn")]
        [Tooltip("Tốc độ chơi khi đang giữ (Time.timeScale). 1 = tắt tính năng. Chỉ tăng tốc khi trên bàn đang có bi")] [SerializeField, Min(1f)] float holdSpeed = 1.5f;
        [Tooltip("Giây để tăng từ 1 lên holdSpeed (mượt, không giật)")] [SerializeField, Min(0f)] float speedRamp = 0.15f;

        Camera _camera;
        GameUI _ui;
        BoardController _board;
        AmmoGridController _grid;
        BoosterManager _boosters;
        LaunchManager _launch;
        BallManager _balls;
        bool _playing;
        AmmoBox _heldBox;
        bool _heldMagic;
        float _pressTime;
        bool _wobbling;
        bool _tapSoundPlayed;
        bool _speeding;

        public void Init(Camera camera, GameUI ui, BoardController board, AmmoGridController grid, BoosterManager boosters, LaunchManager launch, BallManager balls)
        {
            _camera = camera; _ui = ui; _board = board; _grid = grid; _boosters = boosters; _launch = launch; _balls = balls;
        }

        public void SetPlaying(bool playing)
        {
            _playing = playing;
            if (!playing) { CancelHold(); StopSpeed(); }
        }

        void Update()
        {
            if (!_playing) return;
            if (_ui.IsPopupOpen) { CancelHold(); StopSpeed(); return; }
            HandleInput();
        }

        void OnDisable() => StopSpeed();

        /// <summary>Đang giữ chỗ trống trong khu khay: tăng dần Time.timeScale lên holdSpeed; thả tay thì về 1.</summary>
        void UpdateSpeed(bool up)
        {
            // thả tay, hoặc bàn hết bi (không còn gì để tăng tốc) → thôi; muốn tăng tốc lại phải giữ lần mới
            if (up || !HasBalls) { StopSpeed(); return; }
            float step = speedRamp > 0f ? (holdSpeed - 1f) * Time.unscaledDeltaTime / speedRamp : holdSpeed;
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, holdSpeed, step);
        }

        bool HasBalls => _balls != null && _balls.ActiveCount > 0;

        /// <summary>Tắt tăng tốc. Chỉ trả timeScale về 1 khi chính mình đã tăng (popup đặt 0 thì để nguyên).</summary>
        void StopSpeed()
        {
            if (!_speeding) return;
            _speeding = false;
            if (Time.timeScale > 1f) Time.timeScale = 1f;
            if (_ui != null) _ui.SetSpeedUp(false, holdSpeed);
        }

        /// <summary>Đọc con trỏ: vừa chạm xuống / đang giữ / vừa thả.</summary>
        static void ReadPointer(out bool down, out bool up, out Vector2 screen)
        {
            down = Input.GetMouseButtonDown(0);
            up = Input.GetMouseButtonUp(0);
            screen = Input.mousePosition;
            if (Input.touchCount > 0)
            {
                Touch t = Input.GetTouch(0);
                screen = t.position;
                if (t.phase == TouchPhase.Began) down = true;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) up = true;
            }
        }

        void HandleInput()
        {
            ReadPointer(out bool down, out bool up, out Vector2 screen);

            if (_speeding) { UpdateSpeed(up); return; }

            if (_heldBox != null)
            {
                // hộp đã bay đi / bị huỷ trong lúc giữ
                if (_heldBox.IsLaunched) { _heldBox = null; _wobbling = false; _tapSoundPlayed = false; }
                else if (up)
                {
                    AmmoBox box = _heldBox;
                    bool magic = _heldMagic;
                    bool stillOver = BoxUnder(screen) == box;
                    bool tapSound = _tapSoundPlayed;
                    CancelHold();
                    if (stillOver) _launch.TryLaunch(box, magic, tapSound);   // click nhanh hay giữ lâu, thả trên hộp đều bắn
                    return;
                }
                else
                {
                    // còn đang giữ: quá ngưỡng thời gian thì núng nính (click nhanh không lắc). KÉO ngón ra khỏi hộp vẫn lắc tiếp,
                    // để người chơi dời tay (tay che hộp) mà vẫn thấy hộp lắc; thả ngoài hộp = huỷ, thả trên hộp = bắn.
                    bool shouldWobble = Time.unscaledTime - _pressTime >= holdToWobble;
                    if (shouldWobble != _wobbling) { _wobbling = shouldWobble; _heldBox.SetHeld(shouldWobble); }
                }
            }

            if (!down || IsOverUiButton(screen)) return;

            // Camera nghiêng 20° → phải chiếu tia xuống đúng mặt phẳng của vật (không dùng ScreenToWorldPoint)
            Ray ray = _camera.ScreenPointToRay(screen);

            if (_boosters.Armed == BoosterType.Hammer)
            {
                _boosters.UseHammer(_board.BlockAt(PointOnPlane(ray, 0f)));
                return;
            }

            Vector2 onAmmo = PointOnPlane(ray, -0.6f);
            AmmoBox hit = _grid.BoxAtWorld(onAmmo);
            if (hit == null)
            {
                // chạm chỗ trống trong khu khay đạn → giữ để tăng tốc
                if (holdSpeed > 1f && HasBalls && _grid.IsInAmmoArea(onAmmo)) { _speeding = true; _ui.SetSpeedUp(true, holdSpeed); UpdateSpeed(false); }
                return;
            }

            bool magicHand = _boosters.Armed == BoosterType.MagicHand && !hit.IsClickable;
            if (!hit.IsClickable && !magicHand) { hit.PlayDenied(); return; }

            if (!fireOnRelease) { _launch.TryLaunch(hit, magicHand); return; }
            CancelHold();
            _heldBox = hit;
            _heldMagic = magicHand;
            _pressTime = Time.unscaledTime;
            _wobbling = false;
            // Phản hồi TỨC THÌ lúc chạm (bù độ trễ vì bắn lúc thả): hộp lún xuống + tiếng chạm
            hit.SetPressed(true);
            AudioManager.Play(Sfx.TapAmmo);
            _tapSoundPlayed = true;   // chưa lắc: đợi giữ đủ holdToWobble giây
        }

        AmmoBox BoxUnder(Vector2 screen) => _grid.BoxAtWorld(PointOnPlane(_camera.ScreenPointToRay(screen), -0.6f));

        void CancelHold()
        {
            if (_heldBox != null) { _heldBox.SetHeld(false); _heldBox.SetPressed(false); }
            _heldBox = null;
            _wobbling = false;
            _tapSoundPlayed = false;
        }

        static Vector2 PointOnPlane(Ray ray, float z)
        {
            float t = Mathf.Abs(ray.direction.z) > 1e-5f ? (z - ray.origin.z) / ray.direction.z : 0f;
            Vector3 p = ray.origin + ray.direction * t;
            return new Vector2(p.x, p.y);
        }

        /// <summary>Chỉ chặn bấm xuống game khi con trỏ nằm trên một nút UI thật (không chặn bởi ảnh trang trí).</summary>
        static readonly List<RaycastResult> UiHits = new List<RaycastResult>();
        static bool IsOverUiButton(Vector2 screen)
        {
            if (EventSystem.current == null) return false;
            var data = new PointerEventData(EventSystem.current) { position = screen };
            UiHits.Clear();
            EventSystem.current.RaycastAll(data, UiHits);
            foreach (RaycastResult r in UiHits)
                if (r.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>() != null) return true;
            return false;
        }
    }
}
