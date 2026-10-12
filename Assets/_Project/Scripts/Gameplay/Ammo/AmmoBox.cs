using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Hộp đạn (jelly) trong khay. Dùng prefab Ammunation gốc.
    /// Trạng thái: bấm được (to, sáng) / chưa thông đường (nhỏ, tối), đóng băng, hộp "?" bí ẩn,
    /// bay lên máng, đậu ở dock và co dần khi nhả bi.
    /// </summary>
    public class AmmoBox : MonoBehaviour
    {
        public AmmoBoxData Data { get; private set; }
        public Vector2Int Cell { get; set; }
        public bool IsClickable { get; private set; }
        public bool IsFrozen => Data != null && Data.IceHits > 0;
        public bool IsHidden => Data != null && Data.Hidden;
        public bool HasDoor => Data != null && Data.Door > 0;
        public bool IsDoorClosed => Data != null && Data.Door > 0 && Data.DoorClosed;
        public bool IsLaunched { get; private set; }

        GameConfig _config;
        ColorLibrary _colors;
        Transform _visual;

        /// <summary>
        /// Khung (x, y) của phần HÌNH THẬT đang hiện ra của hộp – gộp mọi renderer đang bật (thân jelly, nắp, hộp mù, cửa…).
        /// Dùng để dò hộp khi người chơi bấm: bấm vào chỗ NHÌN THẤY hộp. Không dùng gốc xoay của model vì gốc đó nằm lệch
        /// hẳn so với phần nhìn thấy (tilt −68°, cột jelly dài) và đổi theo mọi thông số tư thế trong GameConfig.
        /// </summary>
        public bool TryGetVisibleBounds(out Bounds bounds)
        {
            bounds = new Bounds(transform.position, Vector3.zero);
            bool has = false;
            foreach (Renderer r in GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                if (!has) { bounds = r.bounds; has = true; } else bounds.Encapsulate(r.bounds);
            }
            return has;
        }
        Renderer _jelly;
        GameObject _qmark;
        GameObject _ice;
        Transform _coverT;
        Vector3 _capLocal;          // tâm mặt nắp (đầu cột hướng về camera) trong toạ độ mesh của jelly
        bool _hasCap;
        Camera _cam;
        float _coverBlend, _coverBlendTarget;
        TextMeshPro _iceLabel;
        float _cell;
        float _scaleNow;
        float _scaleTarget;
        float _tint = 1f;
        float _tintTarget = 1f;
        float _wobble;
        float _sink;        // 0 = trồi lên (bấm được), 1 = lún trong lỗ khay
        float _sinkTarget;

        public static float SinkDepth => AmmoBoxTuning.SinkDepth; // chỉnh trong GameConfig.ammoSinkDepth
        MaterialPropertyBlock _mpb;
        Color _baseColor;
        Coroutine _move;

        // Tư thế hộp (nghiêng / chiều dài cột / dịch / cỡ) dùng chung cho TẤT CẢ hộp – chỉnh trong GameConfig (ammoActivePose/ammoInactivePose).
        static Quaternion GridTilt => Quaternion.Euler(AmmoBoxTuning.Active.tiltX, 0f, 0f);
        int _tuningVersion = -1;

        public void Init(AmmoBoxData data, Vector2Int cell, float cellSize, GameConfig config, ColorLibrary colors)
        {
            Data = data;
            Cell = cell;
            _cell = cellSize;
            _config = config;
            _colors = colors;
            _mpb = new MaterialPropertyBlock();

            GameObject v = Instantiate(config.ammoPrefab, transform);
            v.name = "Visual";
            BoardController.StripPhysics(v);
            foreach (TextMeshPro t in v.GetComponentsInChildren<TextMeshPro>(true)) t.gameObject.SetActive(false);
            BoardController.RemoveStrayText(v.transform);
            _visual = v.transform;
            _visual.localRotation = GridTilt;

            Transform jellyT = _visual.Find("Model/Cube/Jelly Cube/RiggedCube (8)");
            _jelly = jellyT != null ? jellyT.GetComponent<Renderer>() : v.GetComponentInChildren<Renderer>();
            Transform q = _visual.Find("Qmark Model");
            _qmark = q != null ? q.gameObject : null;

            // Game gốc chiếu jelly bằng đèn riêng (culling mask = layer Ammunation)
            int ammoLayer = LayerMask.NameToLayer("Ammunation");
            if (ammoLayer >= 0) foreach (Transform t in GetComponentsInChildren<Transform>(true)) t.gameObject.layer = ammoLayer;

            ComputeCapPoint();
            if (data.IceHits > 0) CreateIce();
            if (data.Door > 0) CreateCover();
            ApplyAppearance();

            _scaleNow = _scaleTarget = config.ammoInactiveScale;
            _sink = _sinkTarget = 1f;
            ApplyScale();
        }

        /// <summary>
        /// Cửa kính 2 cánh như cửa thang máy (BlockerCover gốc: blend shape 0 = đóng, 90 = mở – 2 nửa trượt sang 2 bên).
        /// Dựng lại thành 2 tấm phẳng khít đúng miệng hố (cùng bo góc), vật liệu "Cover" của prefab gốc AmmunationBlockerCover.
        /// </summary>
        void CreateCover()
        {
            var root = new GameObject("DoorCover");
            root.transform.SetParent(transform, false);
            _coverT = root.transform;
            var pf = Resources.Load<GameObject>("prefabs/AmmunationBlockerCover");
            Transform src = pf != null ? pf.transform.Find("Cover") : null;
            _coverMat = src != null ? src.GetComponent<Renderer>().sharedMaterial : null;
            _coverBlend = _coverBlendTarget = Data.DoorClosed ? 0f : 90f;
            _doorSink = Data.DoorClosed ? 1f : 0f;
        }

        Material _coverMat;
        float _doorSink;                 // 0..1, nội suy theo trạng thái cửa
        Transform _doorL, _doorR, _doorBack;

        /// <summary>Tấm lót: hình chữ nhật bo đủ 4 góc (DoorHalf chỉ bo 1 phía).</summary>
        static Mesh MirrorRound(Mesh src, float hw, float hh, float r)
        {
            var pts = new System.Collections.Generic.List<Vector2>();
            void C(Vector2 c0, float sx, float sy, float start)
            { Vector2 c = c0 + new Vector2(sx * r, sy * r); for (int i = 0; i <= 5; i++) { float a = (start + 18f * i) * Mathf.Deg2Rad; pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r); } }
            C(new Vector2(hw, -hh), -1f, 1f, 270f); C(new Vector2(hw, hh), -1f, -1f, 0f); C(new Vector2(-hw, hh), 1f, -1f, 90f); C(new Vector2(-hw, -hh), 1f, 1f, 180f);
            var v = new System.Collections.Generic.List<Vector3> { Vector3.zero }; var t = new System.Collections.Generic.List<int>();
            foreach (Vector2 p in pts) v.Add(p);
            for (int i = 0; i < pts.Count; i++) { int a = 1 + i, b = 1 + (i + 1) % pts.Count; t.Add(0); t.Add(b); t.Add(a); t.Add(0); t.Add(a); t.Add(b); }
            var m = new Mesh { name = "DoorBack" }; m.SetVertices(v);
            var n = new System.Collections.Generic.List<Vector3>(); for (int i = 0; i < v.Count; i++) n.Add(Vector3.back);
            m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds(); return m;
        }

        /// <summary>Mỗi lần bắn 1 hộp bất kỳ: cửa đảo trạng thái (gốc BlockerCover.OnAmmoUsed).</summary>
        public void ToggleDoor()
        {
            if (Data.Door <= 0) return;
            Data.DoorClosed = !Data.DoorClosed;
            _coverBlendTarget = Data.DoorClosed ? 0f : 90f;
            _wobble = 1f;
        }

        /// <summary>Đặt 2 cánh cửa phẳng trên miệng hố (toạ độ world của tâm, kích thước, bán kính bo góc).</summary>
        public void SetCoverPlacement(Vector3 worldCenter, Vector2 size, float radius)
        {
            if (_coverT == null) return;
            _coverT.SetPositionAndRotation(worldCenter, Quaternion.identity);
            _coverT.localScale = Vector3.one;
            float hw = size.x * 0.5f, hh = size.y * 0.5f, gap = size.x * 0.006f;
            // tấm lót sáng sau 2 cánh: khe giữa 2 cánh hiện vạch sáng mảnh như gốc (không lộ màu hộp)
            _doorBack = DoorHalf("DoorBack", new Vector3(0f, 0f, 0.003f), -hw, hw, -hh, hh, radius, true);
            _doorBack.GetComponent<MeshFilter>().sharedMesh = MirrorRound(_doorBack.GetComponent<MeshFilter>().sharedMesh, hw, hh, radius);
            if (_coverMat != null)
            {
                var light = new Material(_coverMat) { name = "DoorGap" };
                if (light.HasProperty("_BaseColor")) light.SetColor("_BaseColor", Color.Lerp(_coverMat.GetColor("_BaseColor"), Color.white, 0.6f));
                _doorBack.GetComponent<MeshRenderer>().sharedMaterial = light;
            }
            // pivot mỗi cánh ở mép ngoài → mở = co bề ngang về phía tường
            _doorL = DoorHalf("DoorL", new Vector3(-hw, 0f, 0f), 0f, hw - gap, -hh, hh, radius, true);
            _doorR = DoorHalf("DoorR", new Vector3(hw, 0f, 0f), -(hw - gap), 0f, -hh, hh, radius, false);
            ApplyCover();
        }

        Transform DoorHalf(string name, Vector3 pivot, float x0, float x1, float y0, float y1, float r, bool roundLeft)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_coverT, false);
            go.transform.localPosition = pivot;
            var pts = new System.Collections.Generic.List<Vector2>();
            void Corner(Vector2 c0, float rr, float sx, float sy, float start)
            {
                if (rr <= 0f) { pts.Add(c0); return; }
                Vector2 c = c0 + new Vector2(sx * rr, sy * rr);
                for (int i = 0; i <= 5; i++) { float a = (start + 18f * i) * Mathf.Deg2Rad; pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rr); }
            }
            float rl = roundLeft ? r : 0f, rr2 = roundLeft ? 0f : r;
            Corner(new Vector2(x1, y0), rr2, -1f, 1f, 270f);
            Corner(new Vector2(x1, y1), rr2, -1f, -1f, 0f);
            Corner(new Vector2(x0, y1), rl, 1f, -1f, 90f);
            Corner(new Vector2(x0, y0), rl, 1f, 1f, 180f);
            var v = new System.Collections.Generic.List<Vector3>(); var t = new System.Collections.Generic.List<int>();
            var uv = new System.Collections.Generic.List<Vector2>();
            Vector2 cen = Vector2.zero; foreach (Vector2 p in pts) cen += p; cen /= pts.Count;
            v.Add(cen); foreach (Vector2 p in pts) v.Add(p);
            for (int i = 0; i < v.Count; i++) uv.Add(Vector2.zero);
            for (int i = 0; i < pts.Count; i++) { int a = 1 + i, b = 1 + (i + 1) % pts.Count; t.Add(0); t.Add(b); t.Add(a); t.Add(0); t.Add(a); t.Add(b); }
            var m = new Mesh { name = "DoorHalf" };
            m.SetVertices(v); m.SetUVs(0, uv);
            var n = new System.Collections.Generic.List<Vector3>(); for (int i = 0; i < v.Count; i++) n.Add(Vector3.back);
            m.SetNormals(n); m.SetTriangles(t, 0); m.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _coverMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            int ammoLayer = LayerMask.NameToLayer("Ammunation");
            if (ammoLayer >= 0) go.layer = ammoLayer;
            return go.transform;
        }

        void ApplyCover()
        {
            if (_doorL == null) return;
            float open = Mathf.SmoothStep(0f, 1f, _coverBlend / 90f);
            float k = Mathf.Max(0.001f, 1f - open);
            _doorL.localScale = new Vector3(k, 1f, 1f);
            _doorR.localScale = new Vector3(k, 1f, 1f);
            _doorL.gameObject.SetActive(open < 0.999f);
            _doorR.gameObject.SetActive(open < 0.999f);
            if (_doorBack != null) _doorBack.gameObject.SetActive(open < 0.05f);
        }

        /// <summary>Tâm mặt nắp của cột jelly: trung bình các đỉnh ở đầu cột (trục dài nhất của mesh) quay về phía camera.</summary>
        void ComputeCapPoint()
        {
            _cam = Camera.main;
            _hasCap = CapOf(_jelly, out _capLocal);
            // hộp mù: mảnh "?" gốc (Qmark Model) là cột cùng cỡ jelly nhưng pivot khác → căn nắp của nó trùng nắp jelly
            _qRenderer = _qmark != null ? _qmark.GetComponentInChildren<Renderer>(true) : null;
            _hasQCap = CapOf(_qRenderer, out _qCapLocal);
        }

        bool CapOf(Renderer r, out Vector3 local)
        {
            local = default;
            if (r == null) return false;
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return false;
            Mesh m = mf.sharedMesh; Bounds b = m.bounds;
            int ax = b.extents.x >= b.extents.y && b.extents.x >= b.extents.z ? 0 : (b.extents.y >= b.extents.z ? 1 : 2);
            Vector3 axis = ax == 0 ? Vector3.right : (ax == 1 ? Vector3.up : Vector3.forward);
            Vector3 camFwd = _cam != null ? _cam.transform.forward : Vector3.forward;
            // đầu cột nào đang hướng về camera (khi hộp ở tư thế nghiêng hiện tại)?
            float sign = Vector3.Dot(r.transform.TransformDirection(axis), camFwd) < 0f ? 1f : -1f;
            float ext = b.extents[ax], thr = b.center[ax] + sign * ext * 0.8f;
            Vector3 sum = Vector3.zero; int n = 0;
            foreach (Vector3 v in m.vertices)
                if (sign > 0f ? v[ax] >= thr : v[ax] <= thr) { sum += v; n++; }
            if (n == 0) return false;
            local = sum / n;
            return true;
        }

        Renderer _qRenderer;
        Vector3 _qCapLocal;
        bool _hasQCap;

        /// <summary>Đưa tâm mặt nắp về ĐÚNG tâm hố trên màn hình (camera ortho: chỉ cần dời trong mặt phẳng vuông góc hướng nhìn).</summary>
        void CenterCapOnHole()
        {
            if (!_hasCap || _jelly == null) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            Vector3 view = _cam.transform.forward;
            Vector3 cap = _jelly.transform.TransformPoint(_capLocal);
            Vector3 d = transform.position - cap;           // transform.position = tâm ô
            d -= view * Vector3.Dot(d, view);
            _visual.position += d;
        }

        void CreateIce()
        {
            if (_jelly == null) return;
            _ice = Instantiate(_jelly.gameObject, _jelly.transform.parent);
            _ice.name = "Ice";
            _ice.transform.localScale = _jelly.transform.localScale * 1.16f;
            Renderer r = _ice.GetComponent<Renderer>();
            if (_config.iceMaterial != null) r.sharedMaterial = _config.iceMaterial;

            var labelGo = new GameObject("IceCount");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -_cell * 0.7f);
            _iceLabel = labelGo.AddComponent<TextMeshPro>();
            if (_config.worldFont != null) _iceLabel.font = _config.worldFont;
            _iceLabel.alignment = TextAlignmentOptions.Center;
            _iceLabel.fontSize = _cell * 3.2f;
            _iceLabel.rectTransform.sizeDelta = new Vector2(_cell, _cell);
            _iceLabel.sortingOrder = 6;
            UpdateIce();
        }

        void ApplyAppearance()
        {
            bool hidden = Data.Hidden;
            if (_jelly != null)
            {
                _jelly.gameObject.SetActive(!hidden);
                if (!hidden)
                {
                    _jelly.sharedMaterial = _colors.AmmoMat(Data.ColorCode);
                    _baseColor = _jelly.sharedMaterial != null && _jelly.sharedMaterial.HasProperty("_BaseColor")
                        ? _jelly.sharedMaterial.GetColor("_BaseColor") : _colors.ColorOf(Data.ColorCode);
                }
            }
            if (_qmark != null) _qmark.SetActive(hidden);
        }

        // ------------------------------------------------------------------ Trạng thái trong khay
        public void SetClickable(bool value, bool instant = false)
        {
            IsClickable = value;
            _scaleTarget = value ? _config.ammoActiveScale : _config.ammoInactiveScale;
            _tintTarget = value ? 1f : 0.92f; // Tươi sáng, phân biệt active qua độ nhô & scale
            _sinkTarget = value ? 0f : 1f;
            if (instant) { _scaleNow = _scaleTarget; _tint = _tintTarget; _sink = _sinkTarget; ApplyScale(); ApplyTint(); }
        }

        /// <summary>Lộ màu hộp "?" khi nó thông đường ra (gốc: Ammunation.InitializeQmark / IsHidden).</summary>
        public void Reveal()
        {
            if (!Data.Hidden) return;
            Data.Hidden = false;
            ApplyAppearance();
            _wobble = 1f;
            Vfx.Spawn(_config.ammoLaunchVfx, transform.position, Color.white, _cell * 0.6f);
            AudioManager.Play(Sfx.QmarkReveal);
        }

        /// <summary>Hộp kề (trên/dưới/trái/phải) vừa được bắn → băng nứt thêm 1 nấc.</summary>
        public void CrackIce()
        {
            if (Data.IceHits <= 0) return;
            Data.IceHits--;
            _wobble = 1f;
            UpdateIce();
            if (Data.IceHits <= 0)
            {
                Vfx.Spawn(_config.ammoLaunchVfx, transform.position, new Color(0.7f, 0.9f, 1f), _cell);
                AudioManager.Play(Sfx.IceBreak);
                if (_ice != null) Destroy(_ice);
                if (_iceLabel != null) Destroy(_iceLabel.gameObject);
            }
            else AudioManager.Play(Sfx.IceCrack);
        }

        void UpdateIce()
        {
            if (_iceLabel != null) _iceLabel.SetText("{0}", Data.IceHits);
        }

        public void PlayDenied()
        {
            _wobble = 1f;
            AudioManager.Play(Sfx.Error);
        }

        void Update()
        {
            if (Data == null || IsLaunched || _hover) return;

            bool scaleChanged = false;
            if (_scaleNow != _scaleTarget) { _scaleNow = Mathf.MoveTowards(_scaleNow, _scaleTarget, Time.deltaTime * 0.8f); scaleChanged = true; }
            if (_sink != _sinkTarget) { _sink = Mathf.MoveTowards(_sink, _sinkTarget, Time.deltaTime * 4f); scaleChanged = true; }
            float doorTarget = IsDoorClosed ? 1f : 0f;
            if (_doorSink != doorTarget) { _doorSink = Mathf.MoveTowards(_doorSink, doorTarget, Time.deltaTime * 4f); scaleChanged = true; }
            if (_wobble > 0f) { _wobble = Mathf.Max(0f, _wobble - Time.deltaTime * 3f); scaleChanged = true; }

            bool tintChanged = false;
            if (_tint != _tintTarget) { _tint = Mathf.MoveTowards(_tint, _tintTarget, Time.deltaTime * 3f); tintChanged = true; }
            if (_coverBlend != _coverBlendTarget) { _coverBlend = Mathf.MoveTowards(_coverBlend, _coverBlendTarget, Time.deltaTime * 360f); }

            if (_tuningVersion != AmmoBoxTuning.Version) { _tuningVersion = AmmoBoxTuning.Version; scaleChanged = true; }
            if (scaleChanged) ApplyScale();
            if (tintChanged) ApplyTint();
        }

        void ApplyScale()
        {
            if (_visual == null) return;
            float w = 1f + Mathf.Sin(_wobble * Mathf.PI * 3f) * 0.12f * _wobble;
            float kt = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_sink));
            GameConfig.AmmoPose pose = AmmoBoxTuning.Lerp(1f - kt);   // active ↔ chưa active theo độ lún
            float s = _cell * _scaleNow * pose.scale;
            _visual.localRotation = Quaternion.Euler(pose.tiltX, 0f, 0f);
            if (_jelly != null) _jelly.transform.localScale = new Vector3(1f, pose.length, 1f);
            if (_qRenderer != null) _qRenderer.transform.localScale = new Vector3(1f, pose.length, 1f); // hộp mù theo cùng chiều dài
            BoardController.FitVisualUniform(_visual, s * w);
            if (_sink > 0f)
            {
                float k = Mathf.SmoothStep(0f, 1f, _sink);
                // cửa đóng: hộp tụt sâu hẳn sau mặt khay như buồng thang máy, cửa 2 cánh khép trên miệng hố
                float dz = (SinkDepth + _doorSink * _config.doorExtraSink) * _cell * k;
                // xuống tới mặt khay thì bám theo độ lệch y của miệng hố, sâu hơn nữa thì tụt THẲNG theo trục giếng (không trôi sang giếng dưới)
                float dyz = Mathf.Min(dz, 0.8f * _cell * k); // bù 80% → hộp hơi thấp hơn tâm hố, gờ dưới của hố che mép dưới như ảnh gốc
                _visual.localPosition += new Vector3(0f, 0f, dz);
            }
            CenterCapOnHole();
            // Dịch tay (AmmoLayoutTuner): x phải+, y lên+ trong mặt phẳng màn hình; z ra xa camera+ (đổi thứ tự che giữa các hộp)
            if (_cam != null)
            {
                Vector3 o = pose.offset * _cell;
                _visual.position += _cam.transform.right * o.x + _cam.transform.up * o.y + _cam.transform.forward * o.z;
            }
            AlignQmarkToJelly();
            ApplyCover();
        }

        /// <summary>Hộp mù: dời mảnh "?" để nắp của nó trùng đúng nắp jelly (jelly đang ẩn nhưng transform vẫn đúng tư thế).</summary>
        void AlignQmarkToJelly()
        {
            if (_qmark == null || !_qmark.activeSelf || !_hasQCap || !_hasCap || _qRenderer == null || _jelly == null) return;
            Vector3 d = _jelly.transform.TransformPoint(_capLocal) - _qRenderer.transform.TransformPoint(_qCapLocal);
            _qmark.transform.position += d;
        }

        void ApplyTint()
        {
            if (_jelly == null || !_jelly.gameObject.activeSelf || !_jelly.sharedMaterial || !_jelly.sharedMaterial.HasProperty("_BaseColor")) return;
            _jelly.GetPropertyBlock(_mpb);
            Color c = _baseColor * _tint;
            c.a = _baseColor.a;
            _mpb.SetColor("_BaseColor", c);
            Material sm = _jelly.sharedMaterial;
            if (sm.HasProperty("_HColor")) { Color hc = sm.GetColor("_HColor") * Mathf.Lerp(0.9f, 1f, _tint); hc.a = 1f; _mpb.SetColor("_HColor", hc); }
            if (sm.HasProperty("_SColor")) { Color sc = sm.GetColor("_SColor") * Mathf.Lerp(0.85f, 1f, _tint); sc.a = 1f; _mpb.SetColor("_SColor", sc); }
            if (sm.HasProperty("_SpecularColor")) _mpb.SetColor("_SpecularColor", sm.GetColor("_SpecularColor") * Mathf.Lerp(0.6f, 1f, _tint));
            _jelly.SetPropertyBlock(_mpb);
        }

        // ------------------------------------------------------------------ Bay lên máng / đậu dock
        public void BeginLaunch()
        {
            IsLaunched = true;
            _hover = false;
            StopAllCoroutines(); _move = null;
            if (_coverT != null) Destroy(_coverT.gameObject);
            _tint = 1f;
            _sink = _sinkTarget = 0f;
            ApplyTint();
            if (_iceLabel != null) Destroy(_iceLabel.gameObject);
        }

        bool _hover;

        /// <summary>
        /// Video gốc (lose_coche): bấm hộp làm vượt sức chứa → hộp bật khỏi ô, kéo dài thành "thanh" chéo và LƠ LỬNG giữa không trung
        /// trong lúc hiện Out Of Capacity; chọn Play On thì hộp bay tiếp lên máng (Launch), Give Up thì thua.
        /// </summary>
        public void Hover()
        {
            if (_hover || IsLaunched) return;
            _hover = true;
            if (_coverT != null) Destroy(_coverT.gameObject);
            _tint = 1f; _sink = _sinkTarget = 0f;
            ApplyTint();
            StartCoroutine(HoverRoutine());
        }

        public void StopHover() { _hover = false; }

        /// <summary>
        /// Lơ lửng như video gốc (lose_coche f528–540): hộp thành ỐNG THẠCH dày đúng bề ngang hộp (cùng thân gắn xương với lúc bay,
        /// không kéo giãn mesh hộp → không bị bẹp/que mảnh), trồi khỏi hố rồi ngả chéo sang phải ~28°, dài ~3 bề ngang,
        /// đuôi rời hẳn miệng hố; bật nhẹ kiểu thạch rồi đứng yên chờ Out Of Capacity.
        /// </summary>
        IEnumerator HoverRoutine()
        {
            if (_jelly == null) yield break;
            _jelly.GetPropertyBlock(_mpb);
            if (_body != null) Destroy(_body.gameObject);
            AmmoFlightTube tube = _body = AmmoFlightTube.Create(_jelly, _mpb);
            _flying = true; // PoseDocked không đụng vào
            _visual.gameObject.SetActive(false);

            float sBox = _cell * _config.ammoActiveScale;
            // nổi ra trước mặt khay & thanh Capacity (camera ortho nhìn thẳng: đổi z không đổi chỗ trên màn hình)
            Vector3 start = transform.position; start.z = -2.5f;
            Vector3 exit = start + Vector3.up * (_cell * 0.45f);
            float hoverAngle = _config.hoverAngle;
            Vector3 dir = new Vector3(Mathf.Sin(hoverAngle * Mathf.Deg2Rad), Mathf.Cos(hoverAngle * Mathf.Deg2Rad), 0f);
            tube.SetPath(new[] { start, exit, exit + dir * (_cell * 5f) }, sBox * 1.3f);

            float h0 = sBox * 0.9f, len = sBox * _config.hoverLength;
            float tailEnd = _cell * 0.45f + _cell * 0.55f;        // đuôi qua khỏi khúc gập, lơ lửng trên miệng hố
            float t = 0f;
            float Dur = Mathf.Max(0.05f, _config.hoverTime);
            while (_hover)
            {
                // đầu vọt ra trước, đuôi theo sau; ease-out-back cho cú bật kiểu thạch
                float u = Mathf.Clamp01(t / Dur);
                float back = 1f + 2.2f * Mathf.Pow(u - 1f, 3f) + 1.2f * Mathf.Pow(u - 1f, 2f);
                float uh = Mathf.Clamp01(u * 1.25f);
                float head = Mathf.Lerp(h0, tailEnd + len, 1f - Mathf.Pow(1f - uh, 3f));
                float tail = Mathf.Lerp(0f, tailEnd, back);
                tail = Mathf.Clamp(tail, 0f, head - h0 * 0.8f);
                float stretch = (head - tail) / len;
                float w = sBox * Mathf.Clamp(Mathf.Pow(1f / Mathf.Max(0.3f, stretch), 0.25f), 0.9f, 1.1f);
                tube.Build(tail, head, w, GridTilt, Quaternion.identity);
                if (u >= 1f) break;
                yield return null;
                t += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            }
            _flying = false;
        }

        public void FlyTo(Vector3 target, float duration, Action done)
        {
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(FlyRoutine(target, duration, done));
        }

        /// <summary>
        /// Video gốc: hộp bay lên máng như một ỐNG THẠCH DẺO – đầu đi trước theo quỹ đạo (lên khỏi hố → chéo tới trên máng
        /// → cắm xuống chân máng), đuôi bám đúng đường đầu đã đi nên rời hố thì gập khuỷu, vào máng thì cong mượt.
        /// Cuối hành trình ống co về đúng thanh đậu ở máng (ShowDocked: dài 1.5, đáy dưới dock 0.3).
        /// </summary>
        IEnumerator FlyRoutine(Vector3 target, float duration, Action done)
        {
            // ống bay ở mặt phẳng z = -2 (trước bàn chơi); điểm đầu/cuối được dời theo trục up của camera nghiêng
            // để trên màn hình vẫn trùng đúng ô trong khay và tâm máy xay
            Vector3 start = transform.position;
            Vector3 startUp = GridTilt * Vector3.up;
            Vector3 dock = target;
            Vector3 dockUp = DockRotation * Vector3.up;
            float sBox = _cell * _config.ammoActiveScale;
            float dw = DockWidthResolved;
            float DockLen = dw * DockHeightRatio, DockBottom = dw * DockBottomRatio, DockR = dw * 0.5f;

            if (_jelly != null) _jelly.GetPropertyBlock(_mpb);
            if (_body != null) Destroy(_body.gameObject);
            AmmoFlightTube tube = _body = AmmoFlightTube.Create(_jelly, _mpb);
            _flying = true;
            tube.SetPath(new[]
            {
                start,
                start + startUp * (_cell * 0.9f),
                dock + dockUp * (DockLen + 1.2f), // vào máng cắm theo đúng trục nghiêng của máy xay
                dock - dockUp * DockBottom,
            }, Mathf.Max(sBox, dw) * 1.3f);
            _visual.gameObject.SetActive(false);
            transform.rotation = DockRotation;

            // Lò xo (stretch & squash): nén lấy đà → ĐẦU phóng đi trước trong khi đuôi còn kẹt trong hố (kéo giãn từ đầu),
            // đuôi là khối lượng nối lò xo đuổi theo (giãn dần tới cuối); đầu dừng ở máng thì đuôi lao tới quá đà → ống nén
            // ngắn & phình, bật lại rồi yên. Bán kính giữ thể tích: r ~ sqrt(dài nghỉ / dài hiện tại).
            float total = tube.Length, h0 = sBox * 0.9f;
            const float Anticip = 0.035f, TailHold = 0.05f, K = 300f, Damp = 0.45f;
            float c = 2f * Mathf.Sqrt(K) * Damp;
            float tail = 0f, tailV = 0f, time = 0f, flyEnd = Anticip + duration;
            while (true)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.05f);
                float head, rest;
                if (time < Anticip)
                {
                    float k = Mathf.Sin(time / Anticip * Mathf.PI);
                    head = h0 * (1f - 0.15f * k);   // nén nhẹ lấy đà
                    rest = h0;
                }
                else
                {
                    float u = Mathf.Clamp01((time - Anticip) / duration);
                    float e = 1f - Mathf.Pow(1f - u, 2.4f); // phóng nhanh ngay khi bấm (không khựng), chậm dần khi tới máng
                    head = Mathf.Lerp(h0, total, e);
                    rest = Mathf.Lerp(h0, DockLen, u);
                    // đuôi: lò xo hướng về (đầu - dài nghỉ), kẹt trong hố một nhịp đầu
                    if (time - Anticip > TailHold * duration)
                    {
                        float acc = K * ((head - rest) - tail) - c * tailV;
                        tailV += acc * dt;
                        tail += tailV * dt;
                    }
                    tail = Mathf.Clamp(tail, 0f, head - 0.25f * rest);
                }
                float len = head - tail;
                float r0 = Mathf.Lerp(sBox * 0.5f, DockR, Mathf.Clamp01((time - Anticip) / duration) * Mathf.Clamp01((time - Anticip) / duration));
                float r = r0 * Mathf.Clamp(Mathf.Pow(rest / Mathf.Max(0.05f, len), 0.25f), 0.85f, 1.12f); // bề ngang gần như giữ nguyên, chỉ phình/thon nhẹ khi nén/giãn
                tube.Build(tail, head, r * 2f, GridTilt, DockRotation);
                transform.position = tube.PointAt(tail + len * 0.5f);
                bool settled = time >= flyEnd && Mathf.Abs(tail - (total - DockLen)) < 0.03f && Mathf.Abs(tailV) < 0.4f;
                if (settled || time > flyEnd + 0.35f) break;
                yield return null;
                time += dt;
            }
            // giữ thân đã gắn xương làm thanh đậu ở máy xay
            transform.position = target;
            transform.rotation = DockRotation;
            _flying = false;
            _dockFraction = 1f;
            _move = null;
            done?.Invoke();
        }

        public void MoveTo(Vector3 target, float duration)
        {
            if (_move != null) StopCoroutine(_move);
            _move = StartCoroutine(MoveRoutine(target, duration));
        }

        IEnumerator MoveRoutine(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            transform.rotation = DockRotation;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.01f))
            {
                transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            transform.position = target;
            _move = null;
        }

        Vector3 ToPlane(Vector3 p, float z)
        {
            Vector3 up = _cam != null ? _cam.transform.up : Vector3.up;
            return new Vector3(p.x, p.y + (p.z - z) * up.z / Mathf.Max(0.1f, up.y), z);
        }

        AmmoFlightTube _body;
        bool _flying;
        float _dockFraction = 1f;

        /// <summary>Bề ngang hộp trong khay.</summary>
        public float BoxWidth => _cell * _config.ammoActiveScale;
        /// <summary>Bề ngang khi đậu = lòng khung máy xay (ConveyorSystem đo từ mesh khung gốc); 0 = dùng BoxWidth.</summary>
        public float DockWidth { get; set; }
        /// <summary>Góc xoay / nghiêng của máy xay tại dock.</summary>
        public Quaternion DockRotation { get; set; } = Quaternion.identity;
        float DockWidthResolved => DockWidth > 0f ? DockWidth : BoxWidth;
        /// <summary>Chiều cao thanh đậu / bề ngang lòng máy xay (GameConfig.dockHeightRatio).</summary>
        public float DockHeightRatio => _config != null ? _config.dockHeightRatio : DefaultDockHeightRatio;
        /// <summary>Đáy thanh đậu nằm dưới tâm 2 lưỡi cắt bao nhiêu lần bề ngang (GameConfig.dockBottomRatio).</summary>
        public float DockBottomRatio => _config != null ? _config.dockBottomRatio : 0.3f;
        /// <summary>Mặc định khi chưa có config (ConveyorLane tính chỗ xếp hàng trước khi Init).</summary>
        public const float DefaultDockHeightRatio = 2.6f;

        TextMeshPro _dockLabel;

        /// <summary>
        /// Hộp đậu trong lòng khung máy xay (tâm = tâm 2 lưỡi cắt, viền khung vẫn lộ 2 bên & dưới); đáy cố định ở
        /// dock − W/2, chiều cao giảm theo số bi còn lại → hộp lún dần vào máy xay (video gốc).
        /// </summary>
        public void ShowDocked(float fraction, int remainingCount = -1)
        {
            _dockFraction = Mathf.Clamp01(fraction);
            PoseDocked();
            UpdateDockLabel(remainingCount);
        }

        void UpdateDockLabel(int count)
        {
            if (count <= 0)
            {
                if (_dockLabel != null) _dockLabel.gameObject.SetActive(false);
                return;
            }
            if (_dockLabel == null)
            {
                var labelGo = new GameObject("DockCount");
                labelGo.transform.SetParent(transform, false);
                _dockLabel = labelGo.AddComponent<TextMeshPro>();
                if (_config != null && _config.worldFont != null) _dockLabel.font = _config.worldFont;
                _dockLabel.alignment = TextAlignmentOptions.Center;
                float w = DockWidthResolved;
                _dockLabel.fontSize = Mathf.Max(w * 3.5f, 3.2f);
                _dockLabel.color = Color.white;
                _dockLabel.fontStyle = FontStyles.Bold;
                _dockLabel.outlineColor = new Color32(0, 0, 0, 220);
                _dockLabel.outlineWidth = 0.22f;
                _dockLabel.sortingOrder = 105;
            }
            _dockLabel.gameObject.SetActive(true);
            _dockLabel.SetText("{0}", count);
            _dockLabel.transform.rotation = DockRotation;
            float dw = DockWidthResolved;
            Vector3 dockUp = DockRotation * Vector3.up;
            Vector3 dockFwd = DockRotation * Vector3.forward;
            _dockLabel.transform.position = transform.position + dockUp * (dw * 0.15f) - dockFwd * 0.08f;
        }

        void PoseDocked()
        {
            if (_body == null || _flying) return;
            float w = DockWidthResolved;
            float h = Mathf.Max(0.03f, w * DockHeightRatio * _dockFraction);
            Vector3 dockUp = DockRotation * Vector3.up;
            Vector3 bottom = transform.position - dockUp * (w * DockBottomRatio);
            _body.PoseStraight(bottom, h, w, DockRotation);
            if (_dockLabel != null && _dockLabel.gameObject.activeSelf)
            {
                _dockLabel.transform.rotation = DockRotation;
                Vector3 dockFwd = DockRotation * Vector3.forward;
                _dockLabel.transform.position = transform.position + dockUp * (w * 0.15f) - dockFwd * 0.08f;
            }
        }

        void LateUpdate()
        {
            if (IsLaunched && _body != null && !_flying) PoseDocked(); // bám theo khi xếp hàng chờ / MoveTo
        }

        void OnDestroy()
        {
            if (_body != null) Destroy(_body.gameObject);
        }

        public void Finish()
        {
            Vfx.Spawn(_config.ammoLaunchVfx, transform.position, _colors.ColorOf(Data.ColorCode), 0.6f);
            Destroy(gameObject);
        }
    }
}
