using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Một con cua: chui ra khỏi máy xay → chạy theo đường tới block cùng màu → quay mặt vào block, cạp vài nhát → block vỡ → cua tan biến.
    /// Hình dáng + Animator nằm trong prefab (chỉnh tay); code chỉ diễn hoạt.</summary>
    public class Crab : MonoBehaviour
    {
        [Tooltip("Các Renderer tô màu theo màu ammo (thân, chân)")] public Renderer[] tinted;
        [Tooltip("Phần hình để nhún / lao tới khi cạp (con của prefab)")] public Transform visual;
        [Tooltip("Càng (tuỳ chọn – cua hình khối tạm). Model Crab Stash để trống")] public Transform clawLeft, clawRight;
        [Tooltip("Animator đi bộ (Crab_Rig_Walk_Side). Trống = tự tìm trong con")] public Animator animator;

        [Header("Hướng")]
        [Tooltip("Bật: mặt trước (+Y) hướng theo đường chạy (cua hình khối). Tắt: cua bò NGANG như cua thật – trục X theo đường chạy, mặt luôn hướng lên")]
        public bool faceHeading = false;
        [Tooltip("Tốc độ xoay người (độ/giây)")] public float turnSpeed = 900f;

        [Header("Cạp")]
        [Tooltip("Số nhát cạp trong một lần cạp")] public int chompBites = 3;
        [Tooltip("Lao tới block bao xa mỗi nhát (theo cỡ cua)")] public float chompLunge = 0.25f;
        [Tooltip("Animator chạy nhanh gấp mấy lần khi cạp (chân quẫy)")] public float chompAnimSpeed = 2.2f;

        [Header("Biến mất: ôm mảnh block, nhảy lên rồi lặn xuống cát")]
        [Tooltip("Độ cao cú nhảy (theo cỡ cua)")] public float hopHeight = 0.9f;
        [Tooltip("Phần thời gian dành cho cú nhảy (còn lại là lặn)")] [Range(0.1f, 0.9f)] public float hopPortion = 0.45f;
        [Tooltip("Mảnh block cua ôm (cỡ theo ô)")] public float chunkSize = 0.45f;

        enum S { Hatch, Walk, Chomp, Poof }
        Transform _chunk;
        MeshRenderer _chunkRenderer;

        CrabManager _mgr;
        GameConfig _cfg;
        BoardBlock _target;
        string _color;
        List<Vector2> _path;
        int _seg;
        float _t;
        S _s;
        float _walkPhase;
        float _angle;
        int _bitesDone;
        Renderer[] _allRenderers;

        public string ColorCode => _color;
        public BoardBlock Target => _target;
        public bool IsDone { get; private set; }

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            _allRenderers = GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>Bắt đầu một đời cua mới (đã lấy từ pool)</summary>
        public void Begin(CrabManager mgr, GameConfig cfg, string color, Material mat, BoardBlock target, List<Vector2> path, Vector3 start)
        {
            _mgr = mgr; _cfg = cfg; _color = color;
            foreach (Renderer r in tinted) if (r != null) r.sharedMaterial = mat;
            IsDone = false;
            SetRoute(target, path);
            transform.position = start;
            transform.localScale = Vector3.zero;
            _angle = 0f;
            transform.rotation = Quaternion.identity;
            _s = S.Hatch; _t = 0f; _walkPhase = Random.value * 6.28f; _bitesDone = 0;
            gameObject.SetActive(true);
            if (animator != null) { animator.speed = 1f; animator.Play(0, 0, Random.value); }
        }

        void SetRoute(BoardBlock target, List<Vector2> path)
        {
            _target = target;
            _path = path;
            _seg = 0;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            float full = _cfg.crabScale;
            switch (_s)
            {
                case S.Hatch:
                    float k = Mathf.Clamp01(_t / Mathf.Max(0.01f, _cfg.crabHatchTime));
                    transform.localScale = Vector3.one * full * (k < 0.7f ? Mathf.Lerp(0f, 1.15f, k / 0.7f) : Mathf.Lerp(1.15f, 1f, (k - 0.7f) / 0.3f));
                    if (_t >= _cfg.crabHatchTime) { _s = S.Walk; _t = 0f; transform.localScale = Vector3.one * full; }
                    break;
                case S.Walk:
                    if (_target == null || _target.IsDestroyed) { _mgr.Retarget(this); if (_s == S.Poof) break; }
                    Walk(dt);
                    break;
                case S.Chomp:
                    Chomp(dt);
                    break;
                case S.Poof:
                    Poof(full);
                    break;
            }
        }

        /// <summary>Mục tiêu cũ mất: nhận đường mới (hoặc null = biến mất)</summary>
        public void Reroute(BoardBlock target, List<Vector2> path)
        {
            if (target == null) { BeginPoof(); _target = null; return; }
            SetRoute(target, path);
        }

        void BeginPoof()
        {
            _s = S.Poof; _t = 0f; _burrowed = false;
            if (animator != null) animator.speed = 1.6f;
            if (visual != null) visual.localRotation = Quaternion.identity;
        }

        bool _burrowed;

        /// <summary>Nhảy lên (ôm mảnh block nếu có) → rơi xuống → lặn vào cát: bẹp dần, xoay nhẹ, bụi cát tung lên</summary>
        void Poof(float full)
        {
            float dur = Mathf.Max(0.05f, _cfg.crabPoofTime + 0.25f);
            float p = Mathf.Clamp01(_t / dur);
            float hp = Mathf.Max(0.1f, hopPortion);
            if (p < hp)
            {
                float k = p / hp;
                float h = Mathf.Sin(k * Mathf.PI) * hopHeight;
                if (visual != null) visual.localPosition = new Vector3(0f, h, 0f);
                float squash = k < 0.15f ? 1f - (0.15f - k) * 1.5f : 1f;
                transform.localScale = new Vector3(full / squash, full * squash, full);
            }
            else
            {
                if (!_burrowed) { _burrowed = true; _mgr.OnPoof(this); }
                float k = (p - hp) / (1f - hp);
                if (visual != null) visual.localPosition = new Vector3(0f, -k * 0.35f, 0f);
                transform.localScale = new Vector3(full * (1f + k * 0.3f) * (1f - k), full * (1f - k) * (1f - k), full);
                transform.rotation = Quaternion.Euler(0f, 0f, _angle + k * 120f);
            }
            if (p >= 1f) { IsDone = true; _mgr.Recycle(this); }
        }

        /// <summary>Cạp xong: ôm một mảnh block cùng màu trên càng</summary>
        public void CarryChunk(Material mat, float cell)
        {
            if (_chunk == null)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Chunk";
                Destroy(go.GetComponent<Collider>());
                _chunk = go.transform;
                _chunk.SetParent(visual != null ? visual : transform, false);
                _chunkRenderer = go.GetComponent<MeshRenderer>();
            }
            if (mat != null) _chunkRenderer.sharedMaterial = mat;
            float s = cell * chunkSize / Mathf.Max(0.01f, transform.lossyScale.x);
            _chunk.localScale = Vector3.one * s;
            _chunk.localPosition = new Vector3(0f, 0.55f, -0.3f);
            _chunk.localRotation = Quaternion.Euler(15f, 20f, 10f);
            _chunk.gameObject.SetActive(true);
        }

        void TurnTo(float target, float dt) => _angle = Mathf.MoveTowardsAngle(_angle, target, turnSpeed * dt);

        void Walk(float dt)
        {
            if (_path == null || _path.Count == 0) { EnterChomp(); return; }
            Vector3 pos = transform.position;
            float step = _cfg.crabSpeed * dt;
            Vector2 dir = Vector2.zero;
            while (step > 0f && _seg < _path.Count)
            {
                Vector2 goal = _path[_seg];
                Vector2 to = goal - (Vector2)pos;
                float d = to.magnitude;
                if (d > 0.0001f) dir = to / d;
                if (d <= step) { pos = new Vector3(goal.x, goal.y, pos.z); step -= d; _seg++; }
                else { pos += (Vector3)(to / d * step); step = 0f; }
            }
            transform.position = pos;
            if (dir != Vector2.zero)
            {
                float a = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (faceHeading) a -= 90f;
                else { if (a > 90f) a -= 180f; else if (a < -90f) a += 180f; } // bò ngang: trục X theo đường, lưng không lộn ngược
                TurnTo(a, dt);
            }
            transform.rotation = Quaternion.Euler(0f, 0f, _angle);
            _walkPhase += dt * 18f;
            if (visual != null)
            {
                visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(_walkPhase)) * 0.06f, 0f);
                visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(_walkPhase) * 5f);
            }
            if (_seg >= _path.Count) EnterChomp();
        }

        void EnterChomp()
        {
            _s = S.Chomp; _t = 0f; _bitesDone = 0;
            if (animator != null) animator.speed = chompAnimSpeed;
            if (visual != null) visual.localRotation = Quaternion.identity;
        }

        float FaceBlockAngle()
        {
            if (_target == null) return _angle;
            Vector2 to = (Vector2)_target.transform.position - (Vector2)transform.position;
            return to.sqrMagnitude > 0.0001f ? Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg - 90f : _angle;
        }

        void Chomp(float dt)
        {
            TurnTo(FaceBlockAngle(), dt);
            transform.rotation = Quaternion.Euler(0f, 0f, _angle);
            float dur = Mathf.Max(0.05f, _cfg.crabChompTime), bites = Mathf.Max(1, chompBites);
            float u = Mathf.Clamp01(_t / dur) * bites;               // 0..bites
            float phase = u - Mathf.Floor(u);                       // trong 1 nhát
            float lunge = Mathf.Sin(phase * Mathf.PI);              // lao tới rồi lùi
            if (visual != null) visual.localPosition = new Vector3(0f, lunge * chompLunge, 0f);
            float claw = Mathf.Sin(phase * Mathf.PI * 2f) * 25f;
            if (clawLeft != null) clawLeft.localRotation = Quaternion.Euler(0f, 0f, claw);
            if (clawRight != null) clawRight.localRotation = Quaternion.Euler(0f, 0f, -claw);
            int reached = Mathf.Min((int)bites, Mathf.FloorToInt(u + 0.5f)); // mỗi nhát "chạm" ở giữa cú lao
            while (_bitesDone < reached && _bitesDone < bites - 1) { _bitesDone++; _mgr.Nibble(this); }
            if (_t < dur) return;
            _mgr.Bite(this);
            BeginPoof();
        }

        public void ResetState()
        {
            _target = null; _path = null; IsDone = true;
            if (visual != null) { visual.localPosition = Vector3.zero; visual.localRotation = Quaternion.identity; }
            if (clawLeft != null) clawLeft.localRotation = Quaternion.identity;
            if (clawRight != null) clawRight.localRotation = Quaternion.identity;
            if (animator != null) animator.speed = 1f;
            if (_chunk != null) _chunk.gameObject.SetActive(false);
        }
    }
}
