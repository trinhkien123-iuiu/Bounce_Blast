using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tunnel trong khay đạn (/tu-XD)</summary>
    public class AmmoTunnel : MonoBehaviour
    {
        public AmmoTunnelData Data { get; private set; }
        public Vector2Int Cell { get; private set; }
        public Vector2Int OutputCell => IsBasket ? PreferredOutput : Cell + Data.Direction;
        public int Remaining => Data.Queue.Count;
        /// <summary>Giỏ (/ba): không có hướng nhả, nhả vào ô kề vừa được trống sau khi nắp mở</summary>
        public bool IsBasket => Data != null && Data.Basket;
        public bool IsOpen { get; private set; }
        public Vector2Int PreferredOutput { get; private set; }

        Transform _cover;
        Quaternion _coverClosed = Quaternion.identity;
        float _coverOpen;

        TextMeshPro _label;
        Transform _visual;
        ParticleSystem _puff;
        float _punch;
        float _cell;

        /// <summary>Slot vật liệu của VÀNH miệng ống</summary>
        const int RimSlot = 0;

        Renderer _tube;
        Material _tubeMat;
        ColorLibrary _colors;

        /// <summary>Xử lý khởi tạo ống nhả hộp theo dữ liệu</summary>
        public void Init(AmmoTunnelData data, Vector2Int cell, float cellSize, GameConfig config, ColorLibrary colors = null)
        {
            _colors = colors;
            Data = data;
            Cell = cell;
            _cell = cellSize;
            if (data.Basket) { InitBasket(config); return; }

            GameObject v = Instantiate(config.tunnelPrefab, transform);
            v.name = "Visual";
            ModelUtil.StripPhysics(v);
            _visual = v.transform;
            Transform decoy = _visual.Find("Model/Decoy Ammo Start");
            if (decoy != null) decoy.gameObject.SetActive(false);
            _puff = v.GetComponentInChildren<ParticleSystem>(true);
            if (_puff != null) _puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            foreach (TextMeshPro t in v.GetComponentsInChildren<TextMeshPro>(true)) t.gameObject.SetActive(false);
            ModelUtil.RemoveStrayText(v.transform);
            Transform tube = _visual.Find("Model/Tunnel");
            if (tube != null) { _tube = tube.GetComponent<Renderer>(); _tubeMat = _tube.sharedMaterials.Length > RimSlot ? _tube.sharedMaterials[RimSlot] : null; }

            float angle = data.Direction.x < 0 ? config.tunnelAngleLeft : data.Direction.x > 0 ? config.tunnelAngleRight
                        : data.Direction.y < 0 ? config.tunnelAngleUp : config.tunnelAngleDown;
            _visual.localRotation = Quaternion.Euler(0f, 0f, angle) * Quaternion.Euler(config.tunnelTiltX, 0f, 0f);
            FitMaxExtent(_visual, cellSize * config.tunnelSize);
            AlignRimToHoleEdge(data.Direction, cellSize, config.tunnelRimOverhang);
            _config = config;
            _tubeNode = tube;
            _baseZ = transform.position.z - config.tunnelBackShift * cellSize;
            ApplyLive();

            var labelGo = new GameObject("Count");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -cellSize);
            _label = labelGo.AddComponent<TextMeshPro>();
            if (config.worldFont != null) _label.font = config.worldFont;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = cellSize * 6f;
            _label.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
            _label.sortingOrder = 6;
            CenterLabelOnBody(data.Direction, cellSize, config.tunnelLabelShift);
            Refresh();
        }

        /// <summary>Giỏ gốc (BlockerBasket): thân giỏ + nắp, số hộp còn lại ở giữa; nắp mở khi hộp kề bên được lấy đi</summary>
        void InitBasket(GameConfig config)
        {
            GameObject src = config.basketPrefab != null ? config.basketPrefab : config.tunnelPrefab;
            GameObject v = Instantiate(src, transform);
            v.name = "Visual";
            ModelUtil.StripPhysics(v);
            _visual = v.transform;
            foreach (TextMeshPro t in v.GetComponentsInChildren<TextMeshPro>(true)) t.gameObject.SetActive(false);
            foreach (Transform t in v.GetComponentsInChildren<Transform>(true))
            {
                // đoàn tàu mồi, tấm va chạm, bóng, chữ gốc (prefab rip đã mất script TextMeshPro) không dùng
                if (t.name.StartsWith("TrainEngine") || t.name == "Collider Jelly Board" || t.name == "Shadow" || t.name.StartsWith("3D Text"))
                    t.gameObject.SetActive(false);
            }
            _puff = v.GetComponentInChildren<ParticleSystem>(true);
            if (_puff != null) _puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (Transform t in v.GetComponentsInChildren<Transform>(true)) if (t.name == "Cover") { _cover = t; break; }
            if (_cover != null) _coverClosed = _cover.localRotation;
            _visual.localRotation = Quaternion.Euler(config.basketEuler);
            FitMaxExtent(_visual, _cell * config.basketSize);
            _config = config;
            TintBasket(v, config);

            var labelGo = new GameObject("Count");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -_cell * config.basketLabelDepth);
            _label = labelGo.AddComponent<TextMeshPro>();
            if (config.worldFont != null) _label.font = config.worldFont;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = _cell * config.basketLabelSize;
            _label.rectTransform.sizeDelta = new Vector2(_cell, _cell);
            _label.sortingOrder = 6;
            Refresh();
        }

        readonly System.Collections.Generic.List<TrainTube> _previews = new System.Collections.Generic.List<TrainTube>();

        /// <summary>DEBUG (basketDebugShowMinis): dựng sẵn các cục ammo nhỏ nằm trong giỏ để chỉnh vị trí/cỡ/nghiêng; tắt khi giỏ mở hoặc bỏ tick</summary>
        void UpdatePreviews()
        {
            bool want = _config.basketDebugShowMinis && !IsOpen && _vanish < 0f && Remaining > 0;
            if (!want)
            {
                if (_previews.Count > 0) ClearPreviews();
                return;
            }
            if (_previews.Count == Remaining) return;
            ClearPreviews();
            Vector3 center = transform.position - new Vector3(0f, 0f, _config.tunnelBackShift * _cell);
            for (int i = 0; i < Data.Queue.Count; i++)
            {
                Material mat = _colors != null ? _colors.AmmoMat(Data.Queue[i].ColorCode) : null;
                TrainTube p = TrainTube.Launch(_config, mat, center, center, _cell, 1f, 0f, 1e9f, null, center, i);
                if (p != null) _previews.Add(p);
            }
        }

        void ClearPreviews()
        {
            foreach (TrainTube p in _previews) if (p != null) Destroy(p.gameObject);
            _previews.Clear();
        }

        void OnDestroy() { ClearPreviews(); }

        Vector3 _fitLocalPos;
        bool _hasFit;
        Vector3 _appliedEuler; float _appliedSize = -1f; Color _appliedTint; float _appliedShadow = -1f;

        /// <summary>Đọc lại hướng/cỡ/màu giỏ từ GameConfig mỗi khi đổi (kéo Inspector lúc Play thấy ngay); thoát Play vẫn giữ vì là asset</summary>
        void ApplyBasketLive()
        {
            if (_config == null || _visual == null) return;
            if (_cover != null && _cover.gameObject.activeSelf == _config.basketHideCover) _cover.gameObject.SetActive(!_config.basketHideCover);
            UpdatePreviews();
            // số đếm nằm trên nắp trong game gốc: ẩn nắp (debug) thì ẩn luôn số để nhìn rõ ammo bên trong
            if (_label != null && !IsOpen && _vanish < 0f && _label.gameObject.activeSelf == _config.basketHideCover) _label.gameObject.SetActive(!_config.basketHideCover);
            if (_vanish >= 0f) return;
            // vị trí giỏ = vị trí sau khi vừa khít ô + basketOffset (đơn vị ô khay)
            if (_hasFit) _visual.localPosition = _fitLocalPos + _config.basketOffset * _cell;
            bool pose = _appliedEuler != _config.basketEuler || !Mathf.Approximately(_appliedSize, _config.basketSize);
            bool color = _appliedTint != _config.basketTint || !Mathf.Approximately(_appliedShadow, _config.basketShadowBrightness);
            if (!pose && !color) return;
            if (pose)
            {
                _visual.localRotation = Quaternion.Euler(_config.basketEuler);
                FitMaxExtent(_visual, _cell * _config.basketSize);
                _fitLocalPos = _visual.localPosition;
                _hasFit = true;
                _visual.localPosition = _fitLocalPos + _config.basketOffset * _cell;
                _appliedEuler = _config.basketEuler;
                _appliedSize = _config.basketSize;
            }
            if (color)
            {
                TintBasket(_visual.gameObject, _config);
                _appliedTint = _config.basketTint;
                _appliedShadow = _config.basketShadowBrightness;
            }
        }

        /// <summary>Material giỏ gốc để trắng (_BaseColor/_HColor = 1) nên lên nhạt: nhân tông màu và hạ vùng bóng, chỉnh trong GameConfig</summary>
        static void TintBasket(GameObject v, GameConfig config)
        {
            foreach (Renderer r in v.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Material[] mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].name.StartsWith("Basket") || mats[i].name.Contains("inside")) continue;
                    var m = new Material(mats[i]);
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", config.basketTint);
                    if (m.HasProperty("_HColor")) m.SetColor("_HColor", config.basketTint);
                    float s = config.basketShadowBrightness;
                    if (m.HasProperty("_SColor")) m.SetColor("_SColor", new Color(s * config.basketTint.r, s * config.basketTint.g, s * config.basketTint.b, 1f));
                    mats[i] = m;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        /// <summary>Một hộp kề bên vừa được lấy đi: nắp giỏ mở (nếu đang đóng) và giỏ sẽ nhả hộp kế vào ô vừa trống</summary>
        public void Open(Vector2Int vacatedCell)
        {
            if (!IsBasket) return;
            PreferredOutput = vacatedCell;
            if (IsOpen) return;
            IsOpen = true;
            if (_label != null) _label.gameObject.SetActive(false);   // số nằm trên nắp giỏ trong game gốc → nắp lật là hết thấy
            if (_puff != null) _puff.Play(true);
        }

        /// <summary>Hộp cuối đã ra khỏi giỏ: giỏ biến mất (KPDisappearObject của game gốc)</summary>
        public void Vanish(float afterSeconds = 0f)
        {
            _vanishDelay = afterSeconds;
            _vanishRequested = true;
            if (_label != null) _label.gameObject.SetActive(false);
        }

        float _vanish = -1f, _vanishDelay;
        bool _vanishRequested;

        /// <summary>Xử lý đặt số đếm vào giữa thân ống (theo hình ống đã đặt), có thể xê dịch dọc hướng nhả</summary>
        void CenterLabelOnBody(Vector2Int dir, float cell, float shift)
        {
            Bounds b = default; bool has = false;
            foreach (Renderer r in _visual.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is SpriteRenderer || r.GetComponent<TextMeshPro>() != null || !r.gameObject.activeInHierarchy) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return;
            Vector2 d = new Vector2(dir.x, -dir.y);
            Vector2 p = (Vector2)b.center + d * shift * cell;
            _label.transform.position = new Vector3(p.x, p.y, _label.transform.position.z);
        }

        /// <summary>Xử lý dời ống để vành miệng (đầu nhả hộp) trùng mép hố phía hướng nhả, thân ống nằm trong hố như game gốc</summary>
        void AlignRimToHoleEdge(Vector2Int dir, float cell, float overhang)
        {
            Bounds b = default; bool has = false;
            foreach (Renderer r in _visual.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is SpriteRenderer || r.GetComponent<TextMeshPro>() != null || !r.gameObject.activeInHierarchy) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return;
            Vector2 d = new Vector2(dir.x, -dir.y);
            float half = Mathf.Abs(d.x) * b.extents.x + Mathf.Abs(d.y) * b.extents.y;
            float center = Vector2.Dot((Vector2)(b.center - transform.position), d);
            float shift = cell * 0.5f + overhang * cell - half - center;
            _visual.position += (Vector3)(d * shift);
        }

        static void FitMaxExtent(Transform v, float size)
        {
            v.localScale = Vector3.one;
            v.localPosition = Vector3.zero;
            Bounds b = default; bool has = false;
            foreach (Renderer r in v.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is SpriteRenderer || r.GetComponent<TextMeshPro>() != null) continue;
                if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
            }
            if (!has) return;
            float s = size / Mathf.Max(b.size.x, b.size.y, 1e-4f);
            Vector3 off = v.position - b.center;
            v.localScale = Vector3.one * s;
            v.localPosition = off * s;
        }

        /// <summary>Xử lý lấy hộp kế tiếp ra khỏi ống</summary>
        public AmmoBoxData Pop()
        {
            if (Data.Queue.Count == 0) return null;
            AmmoBoxData next = Data.Queue[0];
            Data.Queue.RemoveAt(0);
            _punch = 1f;
            if (_puff != null) _puff.Play(true);
            AudioManager.Play(Sfx.TunnelOut);
            Refresh();
            return next;
        }

        void Refresh()
        {
            _label.SetText("{0}", Data.Queue.Count);
            if (_tube != null && _colors != null && _tube.sharedMaterials.Length > RimSlot)
            {
                Material[] mats = _tube.sharedMaterials;
                Material next = Data.Queue.Count > 0 ? _colors.AmmoMat(Data.Queue[0].ColorCode) : null;
                mats[RimSlot] = next != null ? next : _tubeMat;
                _tube.sharedMaterials = mats;
            }
        }

        GameConfig _config;
        Transform _tubeNode;
        float _baseZ;

        /// <summary>Xử lý đọc lại tư thế thân ống và độ lùi từ GameConfig mỗi khung hình (chỉnh lúc Play thấy ngay)</summary>
        void ApplyLive()
        {
            if (_config == null) return;
            Vector3 p = transform.position;
            p.z = _baseZ + _config.tunnelBackShift * _cell;
            transform.position = p;
            GameConfig.TunnelPose pose = _config.TunnelPoseOf(Data.Direction);
            if (pose.use && _tubeNode != null)
            {
                _tubeNode.localPosition = pose.position;
                _tubeNode.localEulerAngles = pose.euler;
                _tubeNode.localScale = pose.scale;
            }
        }

        void Update()
        {
            if (IsBasket)
            {
                ApplyBasketLive();
                if (_vanishRequested && _vanish < 0f)
                {
                    _vanishDelay -= Time.deltaTime;
                    if (_vanishDelay <= 0f) _vanish = 0f;
                }
                if (_vanish >= 0f)
                {
                    // sau khi nhả hết hộp: giỏ co nhỏ dần biến mất (nắp vẫn mở)
                    _vanish += Time.deltaTime / Mathf.Max(0.05f, _config.basketVanishSeconds);
                    transform.localScale = Vector3.one * Mathf.Lerp(1f, 0f, Mathf.Clamp01(_vanish));
                    if (_vanish >= 1f) Destroy(gameObject);
                }
                if (_cover != null)
                {
                    _coverOpen = Mathf.MoveTowards(_coverOpen, IsOpen ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, _config.basketCoverSeconds));
                    float e = _coverOpen * _coverOpen * (3f - 2f * _coverOpen);
                    _cover.localRotation = Quaternion.AngleAxis(_config.basketCoverOpenAngle * e, Vector3.forward) * _coverClosed;
                }
            }
            else ApplyLive();
            if (_punch <= 0f) return;
            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 4f);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(_punch * Mathf.PI) * 0.15f);
        }
    }
}
