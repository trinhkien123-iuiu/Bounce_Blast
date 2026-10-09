using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Một máng (trái hoặc phải). Mô phỏng dòng chảy hạt chất lỏng (Fluid Stream Simulation):
    /// - Không sinh theo trung tuyến: sinh thành luồng hạt đa làn (multi-lane packed stream) với độ lệch ngang,
    ///   jitter hữu cơ và dao động sóng nước (wave undulation).
    /// - Quán tính góc cua: khi gặp khúc rẽ (lên góc trên và rẽ vào cửa phun), lực ly tâm ép các viên bi
    ///   đập vào thành ngoài rồi trôi bo theo mép cong.
    /// - Hội tụ ở cửa phun: cuối đường máng hẹp dần, dồn bi về mũi nhọn (triangle divider) và tách luồng tự nhiên.
    /// </summary>
    public class ConveyorLane : MonoBehaviour
    {
        struct LaneBall
        {
            public Ball ball;
            public float s;                     // Quãng đường dọc theo trục máng [0..length]
            public float baseLateral;           // Độ lệch làn cơ sở [-0.22 .. +0.22]
            public float organicLateralJitter;  // Jitter lệch ngang hữu cơ
            public float organicLongJitter;     // Jitter dọc hữu cơ
            public float wavePhase;             // Pha sóng nước
            public float waveFreq;              // Tần số sóng
            public float waveLateralAmp;        // Biên độ sóng ngang
            public float inertiaCoeff;          // Hệ số quán tính riêng của từng hạt
            public float swirlSeed;             // Seed xoáy nước ngẫu nhiên
            public float distTravelled;         // Quãng đường lăn thực tế
            public float currentLateral;        // Toạ độ lệch ngang tức thời
        }

        Vector3[] _pts;
        float[] _cum;
        float _length;
        GameConfig _config;
        BallManager _balls;
        readonly List<LaneBall> _onLane = new List<LaneBall>();
        readonly Queue<AmmoBox> _waiting = new Queue<AmmoBox>();
        AmmoBox _docked;
        int _dockedTotal;
        int _dockedRemaining;
        float _sinceEmit;
        int _emitSeq;

        float _lastSide = 1f;

        // Mũi tên chạy trên mặt máng: nằm chìm dưới đáy máng (sau bi), sortingOrder thấp.
        const float ArrowSpacing = 1.25f, ArrowSpeed = 1.5f, ArrowZ = -1.05f;
        readonly List<SpriteRenderer> _arrows = new List<SpriteRenderer>();
        float _arrowPhase;

        public Vector3 DockPosition { get; private set; }
        public Quaternion DockRotation { get; set; } = Quaternion.identity;
        public CrusherFx Crusher { get; set; }
        public float DockWidth { get; set; }

        public Vector3 WaitPosition(int index)
        {
            float step = (DockWidth > 0f ? DockWidth : 0.8f) * (AmmoBox.DockHeightRatio + 0.15f);
            Vector3 dockUp = DockRotation * Vector3.up;
            return DockPosition + dockUp * (step * (index + 1));
        }

        public int PendingBalls
        {
            get
            {
                int n = _onLane.Count + _dockedRemaining;
                foreach (AmmoBox b in _waiting) n += b.Data.Balls;
                return n;
            }
        }

        public bool IsBusy => _docked != null || _waiting.Count > 0 || _onLane.Count > 0;

        public void Init(GameConfig config, BallManager balls, Vector3[] path, Vector3 dock)
        {
            _config = config;
            _balls = balls;
            _pts = path;
            DockPosition = dock;
            _cum = new float[path.Length];
            for (int i = 1; i < path.Length; i++) _cum[i] = _cum[i - 1] + Vector3.Distance(path[i - 1], path[i]);
            _length = _cum[path.Length - 1];
        }

        public void Receive(AmmoBox box)
        {
            box.DockRotation = DockRotation;
            if (_docked == null && _waiting.Count == 0) Dock(box);
            else
            {
                _waiting.Enqueue(box);
                box.MoveTo(WaitPosition(_waiting.Count - 1), 0.15f);
                box.ShowDocked(1f, box.Data.Balls);
            }
        }

        void Dock(AmmoBox box)
        {
            _docked = box;
            _dockedTotal = _dockedRemaining = box.Data.Balls;
            _sinceEmit = 999f;
            box.MoveTo(DockPosition, 0.12f);
            box.ShowDocked(1f, _dockedRemaining);
            if (Crusher != null) Crusher.OnBoxEnter(_balls != null ? _balls.ColorOf(box.Data.ColorCode) : Color.white);
        }

        public void CreateArrows(Sprite sprite, Color color, Vector3 scale, int sortingOrder)
        {
            if (sprite == null || _length <= 0f) return;
            int n = Mathf.FloorToInt(_length / ArrowSpacing);
            for (int i = 0; i < n; i++)
            {
                var go = new GameObject("Arrow");
                go.transform.SetParent(transform, false);
                go.transform.localScale = scale;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                _arrowColor = color;
                sr.sortingOrder = 2; // Mũi tên nằm dưới bi (bi có sortingOrder = 100)
                _arrows.Add(sr);
            }
            UpdateArrows(0f);
        }

        Color _arrowColor = Color.white;

        public void SetArrowColor(Color color)
        {
            _arrowColor = color;
            UpdateArrows(0f);
        }

        float ArrowAlpha(float s)
        {
            float a = Mathf.Clamp01(s / 0.8f);
            float corner = _cum != null && _cum.Length > 1 ? _cum[1] : _length;
            if (s > corner)
                a *= Mathf.Lerp(1f, 0.2f, Mathf.Clamp01((s - corner) / Mathf.Max(0.01f, _length - corner)));
            return a;
        }

        void UpdateArrows(float dt)
        {
            if (_arrows.Count == 0) return;
            _arrowPhase = Mathf.Repeat(_arrowPhase + ArrowSpeed * dt, ArrowSpacing);
            for (int i = 0; i < _arrows.Count; i++)
            {
                float s = _arrowPhase + i * ArrowSpacing;
                Transform t = _arrows[i].transform;
                bool show = s > 0.2f && s < _length - 0.6f;
                _arrows[i].enabled = show;
                if (!show) continue;
                Color c = _arrowColor;
                c.a *= ArrowAlpha(s);
                _arrows[i].color = c;
                Vector3 p = EvalCenter(s);
                Vector3 d = EvalTangent(s);
                t.position = new Vector3(p.x, p.y, ArrowZ);
                if (d.sqrMagnitude > 1e-6f) t.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg - 90f);
            }
        }

        public Vector3 EvalCenter(float s)
        {
            if (_pts == null || _pts.Length == 0) return Vector3.zero;
            s = Mathf.Clamp(s, 0f, _length);
            for (int i = 1; i < _pts.Length; i++)
            {
                if (s <= _cum[i])
                {
                    float seg = _cum[i] - _cum[i - 1];
                    float t = seg > 0f ? (s - _cum[i - 1]) / seg : 0f;
                    return Vector3.Lerp(_pts[i - 1], _pts[i], t);
                }
            }
            return _pts[_pts.Length - 1];
        }

        public Vector3 EvalTangent(float s)
        {
            const float ds = 0.05f;
            Vector3 pPrev = EvalCenter(Mathf.Max(0f, s - ds));
            Vector3 pNext = EvalCenter(Mathf.Min(_length, s + ds));
            Vector3 diff = pNext - pPrev;
            return diff.sqrMagnitude > 1e-6f ? diff.normalized : Vector3.up;
        }

        /// <summary>
        /// Tính toạ độ 3D theo mô hình dòng chảy chất lỏng tự nhiên (Fluid Stream Dynamics):
        /// - Giữ nguyên độ dày/bề ngang luồng chảy (Multi-strand fluid thickness across the bend)
        /// - Lực ly tâm chỉ dạt nhẹ cả luồng, KHÔNG ép chập mọi hạt thành 1 hàng đơn điệu
        /// - Mỗi hạt có quán tính, dao động và nhiễu loạn xoáy nước riêng (per-particle turbulence)
        /// - Hội tụ thu hẹp mượt mà khi vào phễu cửa phun đỉnh
        /// </summary>
        void EvalFluidBall(ref LaneBall lb, float time, out Vector3 worldPos, out Vector3 forwardDir)
        {
            // 1) Co giãn dọc hữu cơ theo mật độ dòng nước
            float longWave = Mathf.Sin(time * 3.2f + lb.wavePhase + lb.s * 2.8f) * 0.025f;
            float s = Mathf.Clamp(lb.s + lb.organicLongJitter + longWave, 0f, _length);
            Vector3 center = EvalCenter(s);
            forwardDir = EvalTangent(s);

            Vector3 lateralUnit = new Vector3(-forwardDir.y, forwardDir.x, 0f);

            // 2) Tính độ cong & lực ly tâm
            const float cds = 0.12f;
            Vector3 tPrev = EvalTangent(Mathf.Max(0f, s - cds));
            Vector3 tNext = EvalTangent(Mathf.Min(_length, s + cds));
            Vector3 curvatureVec = (tNext - tPrev) / (2f * cds);
            float centrifugalForce = Vector3.Dot(-curvatureVec, lateralUnit);

            // 3) Dao động dòng nước tự nhiên (Continuous fluid turbulence & wave sloshing):
            float slosh1 = Mathf.Sin(time * lb.waveFreq + lb.wavePhase + s * 2.8f) * lb.waveLateralAmp;
            float slosh2 = Mathf.Cos(time * (lb.waveFreq * 0.7f) + lb.swirlSeed + s * 1.6f) * (lb.waveLateralAmp * 0.6f);
            float turbulence = (Mathf.PerlinNoise(s * 1.4f + lb.swirlSeed, time * 1.6f) - 0.5f) * 0.055f;

            // 4) Độ dạt quán tính tự nhiên khi qua khúc cua (KHÔNG ép bẻ góc cứng nhắc):
            // Lực ly tâm dịch chuyển mềm mại cả luồng hạt theo hệ số quán tính riêng của từng hạt
            float centrifugalDrift = centrifugalForce * (0.05f * lb.inertiaCoeff);

            float lat = lb.baseLateral + lb.organicLateralJitter + slosh1 + slosh2 + turbulence + centrifugalDrift;

            // Giới hạn trong lòng rãnh máng
            const float wallLimit = 0.24f;
            lat = Mathf.Clamp(lat, -wallLimit, wallLimit);

            // 5) Thu hẹp luồng khi tới vòi phun ở cuối (Funneling into outlet nozzle)
            float distToEnd = _length - s;
            if (distToEnd < 1.0f)
            {
                float taper01 = Mathf.Clamp01(distToEnd / 1.0f);
                float taperScale = Mathf.Lerp(0.08f, 1.0f, taper01 * taper01);
                lat *= taperScale;
            }

            // Độ nhấp nhô 3D bồng bềnh
            float zBob = Mathf.Sin(time * 4.2f + lb.wavePhase + s * 3.5f) * 0.025f;

            lb.currentLateral = lat;
            worldPos = center + lateralUnit * lat + new Vector3(0f, 0f, -0.55f + zBob);
        }

        void EmitBall()
        {
            Ball b = _balls.Get(_docked.Data.ColorCode);
            b.EnterLane();

            // Sinh ngẫu nhiên đa làn hữu cơ (True Multi-Strand Fluid Stream):
            // Phân bổ tự nhiên thành luồng dày 3-4 làn đan xen trên bề rộng máng [-0.20 .. +0.20]
            _lastSide = -_lastSide;
            float strandCenter = _lastSide * Random.Range(0.06f, 0.17f);
            float randomSpread = Random.Range(-0.045f, 0.045f);
            float baseLat = Mathf.Clamp(strandCenter + randomSpread, -0.20f, 0.20f);

            float latJitter = Random.Range(-0.02f, 0.02f);
            float longJitter = Random.Range(-0.03f, 0.03f);
            float wavePhase = Random.Range(0f, Mathf.PI * 2f);
            float waveFreq = Random.Range(2.2f, 3.8f);
            float waveAmp = Random.Range(0.025f, 0.045f);
            float inertia = Random.Range(0.7f, 1.3f);
            float swirl = Random.Range(0f, 100f);

            var lb = new LaneBall
            {
                ball = b,
                s = 0f,
                baseLateral = baseLat,
                organicLateralJitter = latJitter,
                organicLongJitter = longJitter,
                wavePhase = wavePhase,
                waveFreq = waveFreq,
                waveLateralAmp = waveAmp,
                inertiaCoeff = inertia,
                swirlSeed = swirl,
                distTravelled = 0f,
                currentLateral = baseLat
            };
            _onLane.Add(lb);

            EvalFluidBall(ref lb, Time.time, out Vector3 initPos, out Vector3 initDir);
            b.SetLaneMotion(initPos, initDir, 0f);
            _dockedRemaining--;
            if (Crusher != null) Crusher.Pulse(_balls != null ? _balls.ColorOf(_docked.Data.ColorCode) : Color.white);
            _docked.ShowDocked(_dockedTotal > 0 ? (float)_dockedRemaining / _dockedTotal : 0f, _dockedRemaining);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            UpdateArrows(dt);

            // 1) Hộp đang đậu nhả bi tự nhiên, có phương sai nhịp nhàng (organic cadence)
            if (_docked != null)
            {
                _sinceEmit += dt;
                float baseInterval = _config.laneBallSpacing / _config.laneSpeed;
                float interval = baseInterval * (0.92f + Mathf.PerlinNoise(_emitSeq * 0.35f, Time.time * 2f) * 0.16f);
                while (_dockedRemaining > 0 && _sinceEmit >= interval)
                {
                    _sinceEmit -= interval;
                    if (_sinceEmit > interval) _sinceEmit = interval;
                    _emitSeq++;
                    EmitBall();
                }
                if (_dockedRemaining <= 0)
                {
                    _docked.Finish();
                    _docked = null;
                    if (_waiting.Count > 0)
                    {
                        Dock(_waiting.Dequeue());
                        int i = 0;
                        foreach (AmmoBox w in _waiting) w.MoveTo(WaitPosition(i++), 0.15f);
                    }
                }
            }

            // 2) Cập nhật dòng bi chạy dọc máng theo động lực học chất lỏng
            float step = _config.laneSpeed * dt;
            float time = Time.time;
            for (int i = _onLane.Count - 1; i >= 0; i--)
            {
                LaneBall lb = _onLane[i];
                lb.s += step;
                lb.distTravelled += step;
                if (lb.s >= _length)
                {
                    _onLane.RemoveAt(i);
                    _balls.ReleaseIntoArena(lb.ball, lb.currentLateral);
                    continue;
                }
                EvalFluidBall(ref lb, time, out Vector3 pos, out Vector3 dir);
                _onLane[i] = lb;
                lb.ball.SetLaneMotion(pos, dir, lb.distTravelled);
            }
        }

        public void ClearAll()
        {
            _onLane.Clear();
            _waiting.Clear();
            if (_docked != null) _docked.Finish();
            _docked = null;
            _dockedRemaining = 0;
        }
    }
}

