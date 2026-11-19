using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Một slot (máy xay / vỏ sò) trong hàng chờ. KHÔNG gắn màu: ammo màu nào cũng vào được, miễn slot đang trống.
    /// Hộp bay tới rồi tan vào vỏ sò; vỏ sò tô màu ammo + hiện số cua còn lại. Đặt sẵn trong Hierarchy, vị trí/đèn chỉnh tay.</summary>
    public class GrinderSlot : MonoBehaviour
    {
        [Header("Chỉnh tay trong Inspector")]
        [Tooltip("Điểm hộp ammo bay tới / cua chui ra. Trống = dùng chính transform của slot")] public Transform dockPoint;
        [Tooltip("Bề ngang hộp khi bay tới (đv thế giới)")] public float dockWidth = 1.0f;
        [Tooltip("Đèn tín hiệu (Renderer, tuỳ chọn): xanh = trống, đỏ = đang chiếm, vàng = đang chờ")] public Renderer lamp;
        [Tooltip("Quầng sáng quanh bóng đèn (SpriteRenderer, tuỳ chọn) – cùng màu đèn")] public SpriteRenderer lampGlow;
        [Range(0f, 1f)] public float lampGlowAlpha = 0.55f;
        public Color lampFree = new Color(0.25f, 0.9f, 0.3f);
        public Color lampBusy = new Color(1f, 0.2f, 0.15f);
        [Tooltip("Đèn khi ammo đang ngồi chờ vì chưa có khối cùng màu để ăn")] public Color lampWaiting = new Color(1f, 0.75f, 0.1f);
        [Tooltip("Máy xay (mesh Crusher gốc) – rung, quay lưỡi, bắn vụn mỗi khi nhả cua")] public CrusherFx crusher;
        [Tooltip("Bật: hộp ammo đứng trong lòng máy xay như Bounce Blast (thanh thạch thấp dần + số). Tắt: hộp tan vào vỏ sò, vỏ sò tô màu + số")] public bool dockBoxVisual = true;
        [Header("Nảy khi hộp rơi vào")]
        [Tooltip("Độ bẹp xuống khi hộp đáp vào (0 = tắt)")] [Range(0f, 0.6f)] public float landSquash = 0.28f;
        [Tooltip("Thời gian nảy (giây)")] public float landBounceTime = 0.45f;
        [Tooltip("Hiệu ứng bụi khi hộp đáp vào (trống = dùng ammoLaunchVfx của GameConfig)")] public GameObject landVfx;
        public float landVfxSize = 0.8f;

        [Tooltip("Vỏ sò của slot: sprite trống / bận; khi bận được tô màu ammo")] public SpriteRenderer shell;
        public Sprite shellFree, shellBusy;
        [Tooltip("Chữ số cua còn lại trên vỏ sò")] public TMP_Text countLabel;
        [Tooltip("Biểu tượng đồng hồ cát, bật khi ammo đang chờ")] public GameObject waitingIcon;

        [Header("Debug (read only)")]
        public int remaining;
        public string colorCode;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        MaterialPropertyBlock _mpb;
        AmmoBox _flying;
        bool _occupied;
        Color _tint = Color.white;

        public bool Landed { get; private set; }
        public bool IsFree => !_occupied;
        public bool IsWaiting { get; private set; }
        public string ColorCode => colorCode;
        public int Total { get; private set; }
        /// <summary>Thứ tự bấm (nhỏ = vào trước) — hai ammo cùng màu thì ammo vào trước nhả trước</summary>
        public int Seq { get; private set; }
        static int _seqCounter;

        public Vector3 DockPosition => dockPoint != null ? dockPoint.position : transform.position;
        public Quaternion DockRotation => dockPoint != null ? dockPoint.rotation : transform.rotation;

        Coroutine _bounce;
        Vector3 _crusherBaseScale;
        bool _baseSaved;
        GameConfig _config;

        /// <summary>GrinderQueue gán cấu hình</summary>
        public void SetConfig(GameConfig config) => _config = config;

        void Awake() => ApplyVisual();

        /// <summary>Máy bẹp xuống (rộng ra) rồi bật lên quá đà và dao động tắt dần – kiểu squash & stretch</summary>
        System.Collections.IEnumerator LandBounce()
        {
            Transform t = crusher != null ? crusher.transform : null;
            if (t == null) yield break;
            if (!_baseSaved) { _crusherBaseScale = t.localScale; _baseSaved = true; }
            float dur = Mathf.Max(0.1f, landBounceTime);
            for (float e = 0f; e < dur; e += Time.deltaTime)
            {
                float k = e / dur;
                float wave = Mathf.Cos(k * Mathf.PI * 3f) * (1f - k) * (1f - k);   // -1..1 tắt dần: bẹp → giãn → bẹp nhẹ
                float sy = 1f - landSquash * wave, sx = 1f + landSquash * 0.6f * wave;
                t.localScale = new Vector3(_crusherBaseScale.x * sx, _crusherBaseScale.y * sy, _crusherBaseScale.z * sx);
                _bounceK = sy; PoseBar();   // thanh thạch bẹp/giãn cùng nhịp với máy
                yield return null;
            }
            t.localScale = _crusherBaseScale;
            _bounceK = 1f; PoseBar();
            _bounce = null;
        }

        float _bounceK = 1f;

        /// <summary>Đặt chiều cao thanh thạch trong máy = phần cua còn lại × hệ số nảy</summary>
        void PoseBar()
        {
            if (!dockBoxVisual || _flying == null || !Landed) return;
            float fill = Mathf.Max(0.12f, remaining / (float)Mathf.Max(1, Total));
            _flying.Flight.ShowDocked(Mathf.Clamp(fill * _bounceK, 0.05f, 1.3f), remaining);
        }

        /// <summary>Ống bay đã yên vị hẳn: chỉ chỉnh lại tư thế thanh (mọi hiệu ứng chạm đã chạy lúc đầu ống chạm máy)</summary>
        public void OnFlightDone() => PoseBar();

        /// <summary>Hộp vừa được bấm: slot bị chiếm ngay (đồng bộ) để hai lần bấm liên tiếp không chung slot</summary>
        public void Assign(AmmoBox box, Color tint)
        {
            _occupied = true;
            _flying = box;
            colorCode = box.Data.ColorCode;
            Total = remaining = box.Data.Balls;
            _tint = tint;
            Seq = ++_seqCounter;
            Landed = false;
            IsWaiting = false;
            ApplyVisual();
        }

        /// <summary>Đầu ống chạm miệng máy (gọi lúc ~80% chuyến bay): máy nảy, bụi, tiếng, hiện số – tất cả cùng một khoảnh khắc</summary>
        public void OnLanded()
        {
            if (!_occupied) return;
            Landed = true;
            if (crusher != null) crusher.OnBoxEnter(_tint);
            if (landSquash > 0f) { if (_bounce != null) StopCoroutine(_bounce); _bounce = StartCoroutine(LandBounce()); }
            GameObject vfx = landVfx != null ? landVfx : (_config != null ? _config.ammoLaunchVfx : null);
            if (vfx != null) Vfx.Spawn(vfx, DockPosition + new Vector3(0f, 0f, -0.3f), Color.Lerp(_tint, Color.white, 0.4f), landVfxSize);
            AudioManager.Play(Sfx.TunnelOut);
            if (_flying != null)
            {
                if (dockBoxVisual) PoseBar();
                else { _flying.Finish(); _flying = null; }
            }
            ApplyVisual();
        }

        /// <summary>Slot sinh ra 1 con cua: số còn lại giảm 1. Hết số thì nhả slot. Trả về true nếu slot vừa trống</summary>
        public bool SpendCharge()
        {
            if (!_occupied || !Landed || remaining <= 0) return false;
            remaining--;
            if (crusher != null) crusher.Pulse(_tint);
            if (remaining > 0)
            {
                PoseBar();
                ApplyVisual();
                return false;
            }
            AmmoBox done = _flying;
            _flying = null;
            Release();   // nhả máy TRƯỚC – lỗi ở hiệu ứng hộp không được làm máy kẹt
            if (done != null) { try { done.Finish(); } catch (System.Exception e) { Debug.LogException(e); if (done != null) Destroy(done.gameObject); } }
            return true;
        }

        /// <summary>Báo ammo đang chờ (chưa có khối cùng màu có đường ăn)</summary>
        public void SetWaiting(bool waiting)
        {
            if (!_occupied || IsWaiting == waiting) return;
            IsWaiting = waiting;
            ApplyVisual();
        }

        void Release()
        {
            _occupied = false;
            Landed = false;
            IsWaiting = false;
            remaining = 0;
            colorCode = null;
            ApplyVisual();
        }

        /// <summary>Dọn slot khi đổi level</summary>
        public void ResetSlot()
        {
            if (_flying != null) Destroy(_flying.gameObject);
            _flying = null;
            Release();
        }

        void ApplyVisual()
        {
            bool showAmmo = _occupied && Landed;
            bool shellLabel = showAmmo && !dockBoxVisual;
            if (shell != null)
            {
                Sprite sp = _occupied ? shellBusy : shellFree;
                if (sp != null) shell.sprite = sp;
                shell.color = shellLabel ? _tint : Color.white;
            }
            if (countLabel != null)
            {
                countLabel.gameObject.SetActive(showAmmo && remaining > 0);
                if (showAmmo) countLabel.SetText("{0}", remaining);
            }
            if (waitingIcon != null && waitingIcon.activeSelf != (showAmmo && IsWaiting)) waitingIcon.SetActive(showAmmo && IsWaiting);
            SetLamp(!_occupied ? lampFree : IsWaiting ? lampWaiting : lampBusy);
        }

        void SetLamp(Color c)
        {
            if (lampGlow != null) lampGlow.color = new Color(c.r, c.g, c.b, lampGlowAlpha);
            if (lamp == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            lamp.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorId, c);
            _mpb.SetColor(BaseColorId, c);
            lamp.SetPropertyBlock(_mpb);
        }
    }
}
