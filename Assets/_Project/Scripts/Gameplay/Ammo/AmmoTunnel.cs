using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Tunnel trong khay đạn (/tu-XD): chứa hàng đợi hộp (TunnelProps_v3), mỗi khi ô phía trước
    /// (theo hướng D) trống thì nhả hộp kế tiếp ra ô đó. Số trên tunnel = số hộp còn lại.
    /// (gốc: BlockerAmmoTunnel + TrainHandlerTunnelToGrid)
    /// </summary>
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

        /// <summary>Slot vật liệu của VÀNH miệng ống (mesh Tunnel gốc có 2 slot: thân thép xanh + vành). Thân giữ nguyên Tunnel_0, vành mang màu hộp kế tiếp.</summary>
        const int RimSlot = 0;

        Renderer _tube;
        Material _tubeMat;
        ColorLibrary _colors;

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

            // Mesh "Tunnel" gốc là ống hở 1 đầu; nghiêng -62° thì miệng ống hướng lên màn hình (= U, về phía lối ra).
            // Xoay quanh trục nhìn cho miệng ống quay đúng hướng nhả hộp (/tu-XR → phải, /tu-XL → trái…).
            // (Direction tính trong lưới đạn: y tăng xuống dưới.)
            float angle = data.Direction.x < 0 ? 90f : data.Direction.x > 0 ? -90f : data.Direction.y < 0 ? 0f : 180f;
            _visual.localRotation = Quaternion.Euler(0f, 0f, angle) * Quaternion.Euler(-82f, 0f, 0f);
            // Giữ đúng tỉ lệ mesh (FitVisual khối vuông làm ống nằm ngang bị bẹp) – cạnh lớn nhất trên màn hình ≈ 0.95 ô
            FitMaxExtent(_visual, cellSize * 0.95f);

            var labelGo = new GameObject("Count");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -cellSize);
            _label = labelGo.AddComponent<TextMeshPro>();
            if (config.worldFont != null) _label.font = config.worldFont;
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = cellSize * 4.2f;
            _label.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
            _label.sortingOrder = 6;
            Refresh();
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
            // Vành ống mang màu hộp sắp nhả (ảnh gốc: viền hồng/đỏ/xanh quanh số)
            if (_tube != null && _colors != null && _tube.sharedMaterials.Length > RimSlot)
            {
                Material[] mats = _tube.sharedMaterials;
                Material next = Data.Queue.Count > 0 ? _colors.AmmoMat(Data.Queue[0].ColorCode) : null;
                mats[RimSlot] = next != null ? next : _tubeMat;
                _tube.sharedMaterials = mats;
            }
        }

        void Update()
        {
            if (_punch <= 0f) return;
            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 4f);
            transform.localScale = Vector3.one * (1f + Mathf.Sin(_punch * Mathf.PI) * 0.15f);
        }
    }
}
