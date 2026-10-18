using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Cột nhà "/wl-hp-id": bi màu nào cũng trừ máu (không tiêu hao), KHÔNG tính vào điều kiện thắng (máu cột nằm ngoài ngân sách bi).
    /// Hình ghép từ mảnh gốc ModularPrimitiveColumn + huy hiệu số máu; collider chỉ bằng thân cột; vỡ thì văng mảnh + bụi kem.
    /// </summary>
    public class ColumnKind : BlockKind
    {
        static readonly Color ColumnDust = new Color(0.96f, 0.9f, 0.8f);
        GameObject _badgeSource;

        public override BoardItemKind Kind => BoardItemKind.Column;
        public override bool CountsForWin => false;
        public override bool HitByAnyColor => true;
        public override bool ShearMesh => false;

        public override BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts)
        {
            // collider chỉ bằng thân cột (mảnh, ~60% bề ngang ô) – bi không bật vào khoảng trống hai bên
            float cw = size.x * board.Config.columnWidthRatio;
            return new BlockView
            {
                visual = CreateVisual(board, parent, item),
                label = CreateBadge(board, parent, item, size),
                colliders = new List<Rect> { new Rect(-cw * 0.5f, -size.y * 0.5f, cw, size.y) },
            };
        }

        /// <summary>
        /// Cột dựng từ mảnh gốc của ModularPrimitiveColumn (<see cref="ModularPrimitiveBuilder"/>, hàng xóm 8 hướng):
        /// T = đế cột, B = đầu cột, T-B = thân; cột rộng 2 ô dùng nửa trái/phải như T-TR-R, L-TL-T…
        /// </summary>
        static Transform CreateVisual(BoardController board, Transform parent, BoardItemData item)
        {
            var root = new GameObject("Visual").transform;
            root.SetParent(parent, false);
            GameObject prefab = board.Config.columnPrefab;
            if (prefab == null) return root;
            Dictionary<string, Transform> pieces = ModularPrimitiveBuilder.Pieces(prefab.transform);
            HashSet<Vector2Int> cells = ModularPrimitiveBuilder.Cells(item);
            foreach (Vector2Int c in cells)
            {
                string key = ModularPrimitiveBuilder.NeighbourKey(cells, c, true);
                if (!pieces.TryGetValue(key, out Transform src))
                {
                    // ngoài các biến thể có sẵn: lấy theo hướng dọc (đế / thân / đầu cột)
                    bool up = cells.Contains(c + Vector2Int.up), down = cells.Contains(c + Vector2Int.down);
                    pieces.TryGetValue(up && down ? "B-T" : up ? "T" : down ? "B" : "B-T", out src);
                }
                if (src != null) ModularPrimitiveBuilder.Place(root, src, c, item.Cells, board.CellSize, Vector3.one, parent.gameObject.layer);
            }
            return root;
        }

        /// <summary>Huy hiệu số máu ở giữa thân cột: đĩa kem gốc (CombinedCubicColumn/…/Cylinder) + số.</summary>
        TextMeshPro CreateBadge(BoardController board, Transform parent, BoardItemData item, Vector2 size)
        {
            float d = Mathf.Min(size.x * 0.85f, size.y);  // video gốc: huy hiệu rộng hơn hẳn thân cột (~1.4 lần)
            TextMeshPro tmp = board.CreateLabel(parent, item, new Vector2(d, d));
            if (tmp == null) return null;
            tmp.transform.localPosition = new Vector3(0f, 0f, -board.CellSize * 1.1f);
            tmp.fontSize = Mathf.Clamp(d * 7.5f, 2f, 10f); // số to, lấp ~65% huy hiệu
            if (_badgeSource == null)
            {
                GameObject pf = board.Config.combinedColumnPrefab;
                Transform cyl = pf != null ? pf.transform.Find("Model/3D Text/Text (TMP)/Cylinder") : null;
                _badgeSource = cyl != null ? cyl.gameObject : null;
            }
            if (_badgeSource != null)
            {
                GameObject disc = Object.Instantiate(_badgeSource, tmp.transform, false);
                disc.name = "Badge";
                disc.transform.localPosition = new Vector3(0f, 0f, 0.03f);
                disc.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                disc.transform.localScale = new Vector3(d, 0.02f, d);   // mesh Cylinder: đường kính 1, cao 2
                BoardController.StripPhysics(disc);
            }
            return tmp;
        }

        /// <summary>Cột vỡ vụn (như video gốc): ẩn mảnh liền, bật các mảnh vỡ _cell gốc và cho văng/rơi, kèm bụi kem.</summary>
        public override void OnBroken(BoardController board, BoardBlock block)
        {
            Vector3 center = block.transform.position;
            foreach (MeshRenderer mr in block.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.gameObject.activeInHierarchy || mr.GetComponent<TextMeshPro>() != null || mr.name == "Badge") continue;
                if (mr.name.Contains("_cell")) continue;
                bool hasCells = false;
                foreach (Transform t in mr.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.Contains("_cell")) continue;
                    hasCells = true;
                    DebrisFx.Fling(t, center);
                }
                if (hasCells) mr.enabled = false; else DebrisFx.Fling(mr.transform, center);
            }
            float h = block.Cells.height * board.CellSize;
            for (int i = 0; i < 3; i++)
                Vfx.Spawn(board.Config.cubeBreakVfx, center + new Vector3(0f, (i - 1) * h * 0.3f, 0f), ColumnDust, block.Cells.width * board.CellSize);
            AudioManager.Play(Sfx.CrateBreak);
        }
    }
}
