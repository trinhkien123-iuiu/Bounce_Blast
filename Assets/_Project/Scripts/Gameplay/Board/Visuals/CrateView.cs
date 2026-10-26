using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hiển thị HÒM GỖ (/cr-hp-id) bằng asset gốc</summary>
    public class CrateView : MonoBehaviour
    {
        Transform _fracture;
        readonly List<Transform> _cells = new List<Transform>();

        /// <summary>Dựng hình hòm dưới parent</summary>
        public static CrateView Build(Transform parent, BoardItemData item, float cell, GameObject modular, GameObject combined, int layer)
        {
            var root = new GameObject("Visual").transform;
            root.SetParent(parent, false);
            var view = root.gameObject.AddComponent<CrateView>();
            if (modular == null) return view;

            Transform model = modular.transform.Find("Model");
            if (model == null) model = modular.transform;
            Dictionary<string, Transform> pieces = ModularPrimitiveBuilder.Pieces(model);
            HashSet<Vector2Int> cells = ModularPrimitiveBuilder.Cells(item);
            RectInt r = item.Cells;
            foreach (Vector2Int c in cells)
            {
                string key = ModularPrimitiveBuilder.NeighbourKey(cells, c, false);
                if (!pieces.TryGetValue(key, out Transform piece) && !pieces.TryGetValue("", out piece)) continue;
                view._cells.Add(ModularPrimitiveBuilder.Place(root, piece, c, r, cell, model.localScale, layer));
            }
            view._fracture = model.Find("Exploder/CrateFracture");
            view._cellSize = cell;
            if (combined != null) view.AddPlanks(combined, new Vector2(r.width, r.height) * cell);
            return view;
        }

        const float PlankWidthCells = 0.65f;
        const float PlankDepthCells = 0.12f;
        const float PlankMarginCells = 0.3f;
        float _cellSize;

        /// <summary>Ván chéo từ góc dưới-trái lên góc trên-phải</summary>
        void AddPlanks(GameObject combined, Vector2 size)
        {
            Transform mid = combined.transform.Find("Model/Planes/PlaneMid/PlaneMid");
            if (mid == null) return;
            float cell = _cellSize;
            var anchor = new GameObject("Planks").transform;
            anchor.SetParent(transform, false);
            GameObject bar = Instantiate(mid.gameObject, anchor, false);
            ModelUtil.StripPhysics(bar);
            bar.transform.localPosition = Vector3.zero;
            bar.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            bar.transform.localScale = Vector3.one;
            Renderer r = bar.GetComponentInChildren<Renderer>();
            if (r == null) return;
            Bounds b = r.bounds;
            bar.transform.localPosition = -(b.center - anchor.position);
            float m = PlankMarginCells * cell;
            Vector2 d = new Vector2(size.x - 2f * m, size.y - 2f * m);
            float len = d.magnitude;
            anchor.localScale = new Vector3(PlankWidthCells * cell / Mathf.Max(b.size.x, 1e-3f),
                                            len / Mathf.Max(b.size.y, 1e-3f),
                                            PlankDepthCells * cell / Mathf.Max(b.size.z, 1e-3f));
            float ang = Mathf.Atan2(size.y, size.x) * Mathf.Rad2Deg - 90f;
            anchor.localRotation = Quaternion.Euler(0f, 0f, ang);
            anchor.localPosition = new Vector3(0f, 0f, FrontZ());
            foreach (Transform t in anchor.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
        }

        /// <summary>z cục bộ của mặt trước thân hòm</summary>
        float FrontZ()
        {
            float minZ = 0f; bool has = false;
            foreach (Transform c in _cells)
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>())
                {
                    float z = transform.InverseTransformPoint(r.bounds.min).z;
                    if (!has || z < minZ) { minZ = z; has = true; }
                }
            return has ? minZ - 0.01f : -0.5f;
        }

        /// <summary>Hòm vỡ: ẩn thân + ván, mỗi ô văng một bộ mảnh CrateFracture gốc</summary>
        public void Break()
        {
            Vector3 center = transform.position;
            foreach (Transform c in _cells)
            {
                if (_fracture != null)
                {
                    GameObject f = Instantiate(_fracture.gameObject, c, false);
                    f.transform.localPosition = _fracture.localPosition;
                    f.transform.localRotation = _fracture.localRotation;
                    f.transform.localScale = _fracture.localScale;
                    foreach (Transform t in f.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = gameObject.layer;
                    DebrisFx.FlingCells(f.transform, center);
                }
                foreach (Renderer r in c.GetComponentsInChildren<Renderer>()) r.enabled = false;
            }
            Transform planks = transform.Find("Planks");
            if (planks != null) planks.gameObject.SetActive(false);
        }
    }
}
