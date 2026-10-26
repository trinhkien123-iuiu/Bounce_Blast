using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tunnel trong khay đạn (/tu-XD)</summary>
    public class AmmoTunnel : MonoBehaviour
    {
        public AmmoTunnelData Data { get; private set; }
        public Vector2Int Cell { get; private set; }
        public Vector2Int OutputCell => Cell + Data.Direction;
        public int Remaining => Data.Queue.Count;

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
            ApplyLive();
            if (_punch <= 0f) return;
            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 4f);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(_punch * Mathf.PI) * 0.15f);
        }
    }
}
