using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Một máng (trái hoặc phải)</summary>
    public class ConveyorLane : MonoBehaviour
    {
        /// <summary>Dùng cho lưu trạng thái một bi trên máng</summary>
        struct LaneBall
        {
            public Ball ball;
            public float s;
            public float baseLateral;
            public float organicLateralJitter;
            public float organicLongJitter;
            public float wavePhase;
            public float waveFreq;
            public float waveLateralAmp;
            public float inertiaCoeff;
            public float swirlSeed;
            public float distTravelled;
            public float currentLateral;
        }

        Vector3[] _pts;
        float[] _cum;
        float _length;
        GameConfig _config;
        BallManager _balls;
        readonly List<LaneBall> _onLane = new List<LaneBall>();
        /// <summary>ĐƯỜNG ỐNG hộp của máng, theo thứ tự bấm</summary>
        readonly List<AmmoBox> _pipe = new List<AmmoBox>();
        readonly HashSet<AmmoBox> _ready = new HashSet<AmmoBox>();
        bool _preloaded;
        AmmoBox _docked;
        int _dockedTotal;
        int _dockedRemaining;
        float _sinceEmit;
        int _emitSeq;

        float _lastSide = 1f;

        /// <summary>Còn chừng này bi thì kéo hộp kế xuống máy xay</summary>
        const int PreloadBalls = 4;

        LaneArrows _arrows;

        public Vector3 DockPosition { get; private set; }
        public Quaternion DockRotation { get; set; } = Quaternion.identity;
        public CrusherFx Crusher { get; set; }
        public float DockWidth { get; set; }

        /// <summary>Dùng cho vị trí chờ của bi thứ n</summary>
        public Vector3 WaitPosition(int index)
        {
            float ratio = _config != null ? _config.dockHeightRatio : AmmoBoxFlight.DefaultDockHeightRatio;
            float step = (DockWidth > 0f ? DockWidth : 0.8f) * (ratio + 0.15f);
            Vector3 dockUp = DockRotation * Vector3.up;
            return DockPosition + dockUp * (step * (index + 1));
        }

        /// <summary>Chỗ của hộp thứ slot trong đường ống</summary>
        Vector3 SlotPosition(int slot) => slot <= 0 ? DockPosition : WaitPosition(slot - 1);

        public int PendingBalls
        {
            get
            {
                int n = _onLane.Count + _dockedRemaining;
                foreach (AmmoBox b in _pipe) if (b != null && b != _docked) n += b.Data.Balls;
                return n;
            }
        }

        public bool IsBusy => _pipe.Count > 0 || _onLane.Count > 0;

        /// <summary>Xử lý khởi tạo đường máng</summary>
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

        /// <summary>Hộp vừa được bấm: xếp vào cuối đường ống, trả về chỗ nó phải bay tới</summary>
        public Vector3 Reserve(AmmoBox box)
        {
            box.Flight.DockRotation = DockRotation;
            _pipe.Add(box);
            return SlotPosition(_pipe.Count - 1);
        }

        /// <summary>Đầu ống đã tới chỗ của mình</summary>
        public void HeadArrived(AmmoBox box) { if (box != null) _ready.Add(box); }

        /// <summary>Ống đã yên vị hẳn. Hộp đang chờ thì hiện số bi còn lại</summary>
        public void Arrived(AmmoBox box)
        {
            if (box == null) return;
            _ready.Add(box);
            if (box != _docked) box.Flight.ShowDocked(1f, box.Data.Balls);
        }

        float BaseInterval => _config.laneBallSpacing / _config.laneSpeed;

        /// <summary>Hộp vào máy xay. keepCadence</summary>
        void Dock(AmmoBox box, bool keepCadence)
        {
            _docked = box;
            _preloaded = false;
            _dockedTotal = _dockedRemaining = box.Data.Balls;
            if (!keepCadence) _sinceEmit = BaseInterval;
            box.Flight.MoveTo(DockPosition, 0.12f);
            box.Flight.ShowDocked(1f, _dockedRemaining);
            if (Crusher != null) Crusher.OnBoxEnter(_balls != null ? _balls.ColorOf(box.Data.ColorCode) : Color.white);
        }

        /// <summary>Hộp xay xong: bỏ khỏi đường ống, dồn hàng chờ xuống một chỗ</summary>
        void Retire()
        {
            AmmoBox done = _docked;
            _docked = null;
            _pipe.Remove(done);
            _ready.Remove(done);
            done.Finish();
            for (int i = 0; i < _pipe.Count; i++) if (_pipe[i] != null) _pipe[i].Flight.MoveTo(SlotPosition(i), 0.15f);
        }

        /// <summary>Chiều dài đường máng (đv thế giới)</summary>
        public float Length => _length;
        /// <summary>Quãng đường tới góc cua đầu tiên của máng</summary>
        public float CornerDistance => _cum != null && _cum.Length > 1 ? _cum[1] : _length;

        /// <summary>Dựng các mũi tên chạy dọc máng</summary>
        public void CreateArrows(Sprite sprite, Color color, Vector3 scale, int sortingOrder)
        {
            _arrows = new LaneArrows(this, _config);
            _arrows.Create(sprite, color, scale, sortingOrder);
        }

        /// <summary>Xử lý đổi màu mũi tên máng</summary>
        public void SetArrowColor(Color color) => _arrows?.SetColor(color);

        /// <summary>Dùng cho điểm giữa máng tại quãng đường s</summary>
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

        /// <summary>Dùng cho hướng máng tại quãng đường s</summary>
        public Vector3 EvalTangent(float s)
        {
            const float ds = 0.05f;
            Vector3 pPrev = EvalCenter(Mathf.Max(0f, s - ds));
            Vector3 pNext = EvalCenter(Mathf.Min(_length, s + ds));
            Vector3 diff = pNext - pPrev;
            return diff.sqrMagnitude > 1e-6f ? diff.normalized : Vector3.up;
        }

        /// <summary>Tính toạ độ 3D theo mô hình dòng chảy chất lỏng tự nhiên</summary>
        void EvalFluidBall(ref LaneBall lb, float time, out Vector3 worldPos, out Vector3 forwardDir)
        {
            float longWave = Mathf.Sin(time * 3.2f + lb.wavePhase + lb.s * 2.8f) * 0.025f;
            float s = Mathf.Clamp(lb.s + lb.organicLongJitter + longWave, 0f, _length);
            Vector3 center = EvalCenter(s);
            forwardDir = EvalTangent(s);

            Vector3 lateralUnit = new Vector3(-forwardDir.y, forwardDir.x, 0f);

            const float cds = 0.12f;
            Vector3 tPrev = EvalTangent(Mathf.Max(0f, s - cds));
            Vector3 tNext = EvalTangent(Mathf.Min(_length, s + cds));
            Vector3 curvatureVec = (tNext - tPrev) / (2f * cds);
            float centrifugalForce = Vector3.Dot(-curvatureVec, lateralUnit);

            float slosh1 = Mathf.Sin(time * lb.waveFreq + lb.wavePhase + s * 2.8f) * lb.waveLateralAmp;
            float slosh2 = Mathf.Cos(time * (lb.waveFreq * 0.7f) + lb.swirlSeed + s * 1.6f) * (lb.waveLateralAmp * 0.6f);
            float turbulence = (Mathf.PerlinNoise(s * 1.4f + lb.swirlSeed, time * 1.6f) - 0.5f) * 0.055f;

            float centrifugalDrift = centrifugalForce * (0.05f * lb.inertiaCoeff);

            float lat = lb.baseLateral + lb.organicLateralJitter + slosh1 + slosh2 + turbulence + centrifugalDrift;

            const float wallLimit = 0.24f;
            lat = Mathf.Clamp(lat, -wallLimit, wallLimit);

            float distToEnd = _length - s;
            if (distToEnd < 1.0f)
            {
                float taper01 = Mathf.Clamp01(distToEnd / 1.0f);
                float taperScale = Mathf.Lerp(0.08f, 1.0f, taper01 * taper01);
                lat *= taperScale;
            }

            float zBob = Mathf.Sin(time * 4.2f + lb.wavePhase + s * 3.5f) * 0.025f;

            lb.currentLateral = lat;
            worldPos = center + lateralUnit * lat + new Vector3(0f, 0f, -0.55f + zBob);
        }

        void EmitBall()
        {
            Ball b = _balls.Get(_docked.Data.ColorCode);
            b.EnterLane();

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
            _docked.Flight.ShowDocked(_dockedTotal > 0 ? (float)_dockedRemaining / _dockedTotal : 0f, _dockedRemaining);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            _arrows?.Tick(dt);

            if (_docked == null && _pipe.Count > 0 && _pipe[0] != null && _ready.Contains(_pipe[0]))
                Dock(_pipe[0], false);

            if (_docked != null)
            {
                _sinceEmit += dt;
                float interval = BaseInterval * (0.92f + Mathf.PerlinNoise(_emitSeq * 0.35f, Time.time * 2f) * 0.16f);
                while (_dockedRemaining > 0 && _sinceEmit >= interval)
                {
                    _sinceEmit -= interval;
                    if (_sinceEmit > interval) _sinceEmit = interval;
                    _emitSeq++;
                    EmitBall();
                }

                if (!_preloaded && _dockedRemaining > 0 && _dockedRemaining <= PreloadBalls && _pipe.Count > 1 && _pipe[1] != null)
                {
                    _preloaded = true;
                    _pipe[1].Flight.MoveTo(DockPosition, Mathf.Max(0.08f, _dockedRemaining * BaseInterval));
                }

                if (_dockedRemaining <= 0)
                {
                    Retire();
                    if (_pipe.Count > 0 && _pipe[0] != null && _ready.Contains(_pipe[0])) Dock(_pipe[0], true);
                }
            }

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

        /// <summary>Xử lý xoá toàn bộ bi trên máng</summary>
        public void ClearAll()
        {
            _onLane.Clear();
            foreach (AmmoBox b in _pipe) if (b != null) b.Finish();
            _pipe.Clear();
            _ready.Clear();
            _docked = null;
            _dockedRemaining = 0;
            _preloaded = false;
        }
    }
}
