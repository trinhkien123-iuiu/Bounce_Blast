using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Làm thân thạch của hộp đạn LẮC như thạch thật khi nhấn giữ</summary>
    public class JellyDeformer : MonoBehaviour
    {
        /// <summary>Dùng cho lò xo một chiều của hiệu ứng thạch</summary>
        sealed class Spring
        {
            public float x, v;
            /// <summary>Xử lý tính một bước vật lý lò xo</summary>
            public void Step(float dt, float target, float omega, float zeta)
            {
                float a = -omega * omega * (x - target) - 2f * zeta * omega * v;
                v += a * dt;
                x += v * dt;
            }
            /// <summary>Kiểm tra lò xo đã đứng yên</summary>
            public bool Settled(float eps) => Mathf.Abs(x) < eps && Mathf.Abs(v) < eps * 20f;
        }

        GameConfig _cfg;
        MeshFilter _mf;
        Mesh _shared, _inst;
        Vector3[] _base, _cur;
        Bounds _baseBounds;
        float _minY, _len, _width;
        bool _capAtMax;
        bool _usingInstance;

        readonly Spring _stretch = new Spring(), _swayX = new Spring(), _swayZ = new Spring();
        bool _held;
        float _blend;
        float _t;

        /// <summary>Xử lý khởi tạo thông số thạch từ config</summary>
        public bool Init(GameConfig cfg, bool capAtMax)
        {
            _cfg = cfg;
            _mf = GetComponent<MeshFilter>();
            if (_mf == null || _mf.sharedMesh == null || !_mf.sharedMesh.isReadable) return false;
            _shared = _mf.sharedMesh;
            _base = _shared.vertices;
            _cur = new Vector3[_base.Length];
            _baseBounds = _shared.bounds;
            _minY = _baseBounds.min.y;
            _len = Mathf.Max(1e-4f, _baseBounds.size.y);
            _width = Mathf.Max(1e-4f, Mathf.Max(_baseBounds.size.x, _baseBounds.size.z));
            _capAtMax = capAtMax;
            enabled = false;
            return true;
        }

        /// <summary>Bắt đầu / thôi nhấn giữ. Mỗi lần đổi có một cú hích</summary>
        public void SetHeld(bool held)
        {
            if (_shared == null || _held == held) return;
            _held = held;
            if (held) _t = 0f;
            _stretch.v += (held ? -_cfg.jellyPressKick : _cfg.jellyReleaseKick);
            _swayZ.v += (held ? 0.3f : -0.5f) * _cfg.jellyReleaseKick * 0.2f;
            enabled = true;
        }

        /// <summary>Hích nhẹ một cái (rung khi bị từ chối, lộ màu…)</summary>
        public void Kick(float strength)
        {
            if (_shared == null) return;
            _stretch.v += strength * _cfg.jellyReleaseKick;
            _swayX.v += strength * 0.5f * _cfg.jellyReleaseKick;
            enabled = true;
        }

        /// <summary>Tắt ngay, trả mesh gốc (hộp bay đi / lơ lửng…)</summary>
        public void ResetNow()
        {
            _held = false; _blend = 0f;
            _stretch.x = _stretch.v = _swayX.x = _swayX.v = _swayZ.x = _swayZ.v = 0f;
            Restore();
            enabled = false;
        }

        void Restore()
        {
            if (_usingInstance && _mf != null) { _mf.sharedMesh = _shared; _usingInstance = false; }
        }

        void OnDestroy()
        {
            if (_inst != null) Destroy(_inst);
        }

        void LateUpdate()
        {
            if (_shared == null || _cfg == null) { enabled = false; return; }
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _blend = Mathf.MoveTowards(_blend, _held ? 1f : 0f, dt / Mathf.Max(0.01f, _held ? _cfg.heldBlendIn : _cfg.heldBlendOut));
            if (_held) _t += dt;

            float w = 2f * Mathf.PI * _cfg.jellyFrequency, z = _cfg.jellyDamping;
            float fd = 2f * Mathf.PI * _cfg.heldDriveFrequency;
            float dS = Mathf.Sin(fd * _t) * _cfg.heldStretch * _blend;
            float dX = Mathf.Sin(fd * _t * 0.93f + 0.6f) * _cfg.heldSway * _blend;
            float dZ = Mathf.Sin(fd * _t * 1.27f + 1.7f) * _cfg.heldSway * 0.7f * _blend;
            int steps = Mathf.CeilToInt(dt / (1f / 120f));
            float h = dt / Mathf.Max(1, steps);
            for (int i = 0; i < steps; i++)
            {
                _stretch.Step(h, dS, w, z);
                _swayX.Step(h, dX, w * 0.9f, z);
                _swayZ.Step(h, dZ, w * 0.9f, z);
            }

            if (!_held && _blend <= 0f && _stretch.Settled(0.002f) && _swayX.Settled(0.002f) && _swayZ.Settled(0.002f))
            {
                _stretch.x = _stretch.v = _swayX.x = _swayX.v = _swayZ.x = _swayZ.v = 0f;
                Restore();
                enabled = false;
                return;
            }
            _stretch.x = Mathf.Clamp(_stretch.x, -MaxStretch, MaxStretch);
            _swayX.x = Mathf.Clamp(_swayX.x, -MaxSway, MaxSway);
            _swayZ.x = Mathf.Clamp(_swayZ.x, -MaxSway, MaxSway);
            Apply();
        }

        const float MaxStretch = 0.05f;
        const float MaxSway = 0.05f;

        void Apply()
        {
            if (!_usingInstance)
            {
                if (_inst == null) { _inst = Instantiate(_shared); _inst.name = _shared.name + " (jelly)"; _inst.MarkDynamic(); }
                _mf.sharedMesh = _inst;
                _usingInstance = true;
            }
            float st = _stretch.x, sx = _swayX.x * _width, sz = _swayZ.x * _width;
            float t = Time.unscaledTime;
            for (int i = 0; i < _base.Length; i++)
            {
                Vector3 p = _base[i];
                float hgt = Mathf.Clamp01((p.y - _minY) / _len);
                if (_capAtMax == false) hgt = 1f - hgt;
                float tip = hgt * hgt;
                float mid = Mathf.Sin(hgt * Mathf.PI);
                float anchorY = _capAtMax ? _minY : _minY + _len;
                p.y = anchorY + (p.y - anchorY) * (1f + st);
                float wave = Mathf.Sin(2f * Mathf.PI * (hgt * 0.9f - t * _cfg.heldDriveFrequency * 0.5f)) * _cfg.heldStretch * 0.5f * _blend * mid;
                float radial = 1f - st * (0.35f + 0.65f * mid) + wave;
                p.x = p.x * radial + sx * tip;
                p.z = p.z * radial + sz * tip;
                _cur[i] = p;
            }
            _inst.SetVertices(_cur, 0, _cur.Length, UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds | UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices);
            _inst.bounds = _baseBounds;
        }
    }
}
