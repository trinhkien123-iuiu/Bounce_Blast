using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Dựng vật thể nhiều ô từ bộ MẢNH GỐC của prefab "ModularPrimitive…" (cột nhà, hòm gỗ…) – thay cho script
    /// ModularPrimitive/CombinedCubic gốc đã bị strip khi rip.
    ///
    /// Quy ước tên mảnh của game gốc: hậu tố là tập ô hàng xóm cùng khối, nối bằng '-'.
    /// VD hòm gỗ: <c>CrateCorner1-T-R</c> (góc dưới-trái: có ô trên & phải), <c>CrateMid-L-T-R-B</c> (ô giữa), <c>CrateSide2</c> (không hàng xóm).
    /// Cột nhà dùng thêm hàng xóm chéo (TL/TR/BL/BR). Mỗi ô chọn mảnh có tập hàng xóm khớp; mảnh giữ nguyên transform gốc.
    /// </summary>
    public static class ModularPrimitiveBuilder
    {
        static readonly HashSet<string> DirTokens = new HashSet<string> { "T", "B", "L", "R", "TL", "TR", "BL", "BR" };
        static readonly Dictionary<Transform, Dictionary<string, Transform>> Cache = new Dictionary<Transform, Dictionary<string, Transform>>();

        /// <summary>Khoá hàng xóm chuẩn hoá (sắp xếp) từ các token hướng trong tên mảnh; token không phải hướng (tên mảnh) bị bỏ.</summary>
        public static string Key(IEnumerable<string> tokens)
        {
            var l = new List<string>();
            foreach (string t in tokens) if (DirTokens.Contains(t)) l.Add(t);
            l.Sort(System.StringComparer.Ordinal);
            return string.Join("-", l);
        }

        /// <summary>Bảng khoá → mảnh của một container (VD prefab.transform hoặc "Model"). Chỉ lấy con có MeshRenderer.</summary>
        public static Dictionary<string, Transform> Pieces(Transform container)
        {
            if (Cache.TryGetValue(container, out var d) && d != null) return d;
            d = new Dictionary<string, Transform>();
            foreach (Transform t in container)
                if (t.GetComponent<MeshRenderer>() != null)
                {
                    string k = Key(t.name.Split('-'));
                    if (!d.ContainsKey(k)) d[k] = t;
                }
            Cache[container] = d;
            return d;
        }

        /// <summary>Tập hàng xóm của ô <paramref name="c"/> trong <paramref name="cells"/>.</summary>
        public static string NeighbourKey(HashSet<Vector2Int> cells, Vector2Int c, bool diagonals)
        {
            var n = new List<string>(8);
            if (cells.Contains(c + new Vector2Int(0, 1))) n.Add("T");
            if (cells.Contains(c + new Vector2Int(0, -1))) n.Add("B");
            if (cells.Contains(c + new Vector2Int(-1, 0))) n.Add("L");
            if (cells.Contains(c + new Vector2Int(1, 0))) n.Add("R");
            if (diagonals)
            {
                if (cells.Contains(c + new Vector2Int(-1, 1))) n.Add("TL");
                if (cells.Contains(c + new Vector2Int(1, 1))) n.Add("TR");
                if (cells.Contains(c + new Vector2Int(-1, -1))) n.Add("BL");
                if (cells.Contains(c + new Vector2Int(1, -1))) n.Add("BR");
            }
            return Key(n);
        }

        /// <summary>
        /// Đặt một bản sao mảnh vào ô <paramref name="c"/> (toạ độ ô trong khối kích thước <paramref name="size"/>), con của <paramref name="root"/>.
        /// <paramref name="containerScale"/> = scale của container gốc (VD "Model" của hòm gỗ là 1.06 → các ô chồng mép nhẹ như gốc).
        /// </summary>
        public static Transform Place(Transform root, Transform piece, Vector2Int c, RectInt size, float cell, Vector3 containerScale, int layer)
        {
            var cellT = new GameObject("Cell").transform;
            cellT.SetParent(root, false);
            cellT.localPosition = new Vector3((c.x + 0.5f - size.width * 0.5f) * cell, (c.y + 0.5f - size.height * 0.5f) * cell, 0f);
            cellT.localScale = Vector3.Scale(Vector3.one * cell, containerScale);
            GameObject inst = Object.Instantiate(piece.gameObject, cellT, false);
            inst.SetActive(true);
            inst.transform.localPosition = piece.localPosition;
            inst.transform.localRotation = piece.localRotation;
            inst.transform.localScale = piece.localScale;
            ModelUtil.StripPhysics(inst);
            foreach (Transform t in inst.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            return cellT;
        }

        /// <summary>Tập ô của khối (mask nếu có, không thì cả hình chữ nhật).</summary>
        public static HashSet<Vector2Int> Cells(BoardItemData item)
        {
            var cells = new HashSet<Vector2Int>();
            RectInt r = item.Cells;
            if (item.Mask != null) foreach (Vector2Int m in item.Mask) cells.Add(m);
            else for (int x = 0; x < r.width; x++) for (int y = 0; y < r.height; y++) cells.Add(new Vector2Int(x, y));
            return cells;
        }
    }
}
