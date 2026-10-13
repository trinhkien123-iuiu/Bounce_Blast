using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Làm thân thạch của hộp đạn LẮC như thạch thật khi nhấn giữ: biến dạng từng đỉnh mesh theo vài lò xo có quán tính.
    ///  • Thân là cột dài, một đầu (đáy) neo trong hố, đầu kia (nắp) hướng về camera → đầu nắp đung đưa nhiều nhất, đáy đứng yên.
    ///  • Ba lò xo giảm chấn: nhún co giãn dọc thân, đung đưa ngang (X) và đung đưa theo chiều sâu (Z, lên màn hình thành dọc).
    ///    Lực lắc là sóng sin đẩy gốc của lò xo → thạch chạy theo trễ pha, vọt quá rồi dội lại (có "overlap"), không cứng như
    ///    nhân scale.
    ///  • Phình ngang theo cao độ (giữ thể tích) + sóng phình chạy dọc thân.
    ///  • Chỉ dùng khi đang lắc: lúc yên lại trả mesh gốc dùng chung (gom batch), mesh bản sao chỉ tồn tại cho hộp từng được giữ.
    /// Mesh gốc giữ nguyên bounds → các phép căn / dò hộp theo bounds (FitVisualUniform, TryGetVisibleBounds) không bị rung theo.
    /// Thông số: GameConfig → "Hộp đạn – thạch khi nhấn giữ".
    /// </summary>
    public class JellyDeformer : MonoBehaviour
    {
        sealed class Spring
        {
            public float x, v;
            public void Step(float dt, float target, float omega, float zeta)
            {
                float a = -omega * omega * (x - target) - 2f * zeta * omega * v;
                v += a * dt;
                x += v * dt;
            }
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
        float _blend;      // 0..1 độ mạnh của lực lắc (vào / ra mượt)
        float _t;

        /// <param name="capAtMax">true nếu nắp (đầu hướng camera) nằm ở y lớn nhất của mesh</param>
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
            enabled = false; // chỉ chạy khi có lắc
            return true;
        }

        /// <summary>Bắt đầu / thôi nhấn giữ. Mỗi lần đổi có một cú hích (nhún xuống lúc nhấn, bật lên lúc thả).</summary>
        public void SetHeld(bool held)
        {
            if (_shared == null || _held == held) return;
            _held = held;
            if (held) _t = 0f;
            _stretch.v += (held ? -_cfg.jellyPressKick : _cfg.jellyReleaseKick);
            _swayZ.v += (held ? 0.3f : -0.5f) * _cfg.jellyReleaseKick * 0.2f;
            enabled = true;
        }

        /// <summary>Hích nhẹ một cái (rung khi bị từ chối, lộ màu…).</summary>
        public void Kick(float strength)
        {
            if (_shared == null) return;
            _stretch.v += strength * _cfg.jellyReleaseKick;
            _swayX.v += strength * 0.5f * _cfg.jellyReleaseKick;
            enabled = true;
        }

        /// <summary>Tắt ngay, trả mesh gốc (hộp bay đi / lơ lửng…).</summary>
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
            // chặn cứng: dù cú hích mạnh cũng không lấn sang hộp bên cạnh
            _stretch.x = Mathf.Clamp(_stretch.x, -MaxStretch, MaxStretch);
            _swayX.x = Mathf.Clamp(_swayX.x, -MaxSway, MaxSway);
            _swayZ.x = Mathf.Clamp(_swayZ.x, -MaxSway, MaxSway);
            Apply();
        }

        const float MaxStretch = 0.05f;   // ±5% chiều dài
        const float MaxSway = 0.05f;      // ±5% bề ngang ở đầu nắp

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
                if (_capAtMax == false) hgt = 1f - hgt;            // 0 = đáy neo, 1 = nắp
                float tip = hgt * hgt;                              // đầu nắp đung đưa nhiều nhất
                float mid = Mathf.Sin(hgt * Mathf.PI);              // phình ở giữa thân
                // nhún dọc thân quanh đáy neo
                float anchorY = _capAtMax ? _minY : _minY + _len;
                p.y = anchorY + (p.y - anchorY) * (1f + st);
                // giữ thể tích: cao ↑ thì ngang ↓ (đậm ở giữa) + sóng phình chạy dọc thân khi đang lắc
                float wave = Mathf.Sin(2f * Mathf.PI * (hgt * 0.9f - t * _cfg.heldDriveFrequency * 0.5f)) * _cfg.heldStretch * 0.5f * _blend * mid;
                float radial = 1f - st * (0.35f + 0.65f * mid) + wave;
                p.x = p.x * radial + sx * tip;
                p.z = p.z * radial + sz * tip;
                _cur[i] = p;
            }
            _inst.SetVertices(_cur, 0, _cur.Length, UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds | UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices);
            _inst.bounds = _baseBounds; // giữ bounds gốc → FitVisualUniform / TryGetVisibleBounds không rung theo cú lắc
        }
    }
}
