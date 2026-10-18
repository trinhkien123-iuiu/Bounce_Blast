using System.Collections.Generic;
using BounceBlast.Gameplay;
using UnityEditor;
using UnityEngine;
using Cell = BounceBlast.Gameplay.LevelData.BoardCell;
using CellType = BounceBlast.Gameplay.LevelData.CellType;

namespace BounceBlast.EditorTools
{
    /// <summary>Tab "Bàn gạch" của Level Editor: công cụ vẽ + lưới + thao tác trên vùng chọn (gộp khối, hòm gỗ, cột nhà).</summary>
    public class BoardPainter
    {
        public enum Tool { Brush, Fill, Erase, Pick, Select }

        public Tool tool = Tool.Brush;
        public string color = "r";
        public int hp = 1;
        public int groupHp = 30, crateHp = 50, columnHp = 25;
        public readonly GridView grid = new GridView();
        public string hoverInfo = "";

        readonly HashSet<Vector2Int> _selection = new HashSet<Vector2Int>();
        Vector2Int _dragStart;
        bool _dragging;

        static readonly Color EmptyColor = new Color(0.17f, 0.17f, 0.2f);
        static readonly Color CrateColor = new Color(0.72f, 0.48f, 0.25f);
        static readonly Color ColumnColor = new Color(0.93f, 0.87f, 0.74f);

        // ------------------------------------------------------------------ Thanh công cụ
        public void DrawToolbar(LevelData d, ColorLibrary colors)
        {
            EditorGUILayout.BeginHorizontal();
            tool = (Tool)GUILayout.Toolbar((int)tool, new[] { "Cọ (B)", "Tô vùng (G)", "Tẩy (E)", "Hút màu (I)", "Chọn (S)" }, GUILayout.Height(22));
            GUILayout.Space(8);
            GUILayout.Label("Máu", GUILayout.Width(30));
            hp = Mathf.Max(1, EditorGUILayout.IntField(hp, GUILayout.Width(40)));
            GUILayout.Label("Zoom", GUILayout.Width(36));
            grid.cellSize = GUILayout.HorizontalSlider(grid.cellSize, 10f, 40f, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            ColorPalette(colors, ref color);

            if (tool == Tool.Select)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(_selection.Count > 0 ? $"Đã chọn {_selection.Count} ô" : "Kéo chuột để chọn vùng", GUILayout.Width(120));
                GUI.enabled = _selection.Count > 0;
                groupHp = Mathf.Max(1, EditorGUILayout.IntField(groupHp, GUILayout.Width(40)));
                if (GUILayout.Button("Gộp khối")) Apply(d, "Gộp khối", MakeGroup);
                if (GUILayout.Button("Tách khối")) Apply(d, "Tách khối", c => { foreach (Vector2Int p in _selection) { Cell x = d.Cell(p.x, p.y); if (x.type == CellType.Cube) x.group = ""; } });
                crateHp = Mathf.Max(1, EditorGUILayout.IntField(crateHp, GUILayout.Width(40)));
                if (GUILayout.Button("Thêm hòm")) Apply(d, "Thêm hòm gỗ", MakeCrate);
                if (GUILayout.Button("Bỏ hòm")) Apply(d, "Bỏ hòm gỗ", c => { foreach (Vector2Int p in _selection) d.Cell(p.x, p.y).crate = ""; });
                columnHp = Mathf.Max(1, EditorGUILayout.IntField(columnHp, GUILayout.Width(40)));
                if (GUILayout.Button("Cột nhà")) Apply(d, "Thêm cột nhà", MakeColumn);
                if (GUILayout.Button("Tô màu")) Apply(d, "Tô màu vùng", c => { foreach (Vector2Int p in _selection) PaintCube(d.Cell(p.x, p.y)); });
                if (GUILayout.Button("Xoá")) Apply(d, "Xoá vùng", c => { foreach (Vector2Int p in _selection) Clear(d.Cell(p.x, p.y)); });
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("Ô số cạnh nút = máu cho khối gộp / hòm gỗ / cột nhà (theo ô thiết kế; game nhân Multiplier²).", MessageType.None);
            }
        }

        /// <summary>Bảng màu lấy từ ColorLibrary (dùng chung cho bàn và khay).</summary>
        public static void ColorPalette(ColorLibrary colors, ref string selected)
        {
            if (colors == null) { EditorGUILayout.HelpBox("Không tìm thấy ColorLibrary", MessageType.Warning); return; }
            EditorGUILayout.BeginHorizontal();
            int perRow = 0;
            foreach (ColorLibrary.Entry e in colors.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.code)) continue;
                Rect r = GUILayoutUtility.GetRect(30, 20, GUILayout.Width(30), GUILayout.Height(20));
                EditorGUI.DrawRect(r, e.color);
                GridView.Label(r, e.code, GridView.TextOn(e.color), 9);
                if (e.code == selected) GridView.Outline(r, Color.yellow, 2f);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition)) { selected = e.code; Event.current.Use(); }
                if (++perRow % 18 == 0) { EditorGUILayout.EndHorizontal(); EditorGUILayout.BeginHorizontal(); }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------ Lưới
        public void DrawGrid(Rect area, LevelData d, ColorLibrary colors, System.Action changed)
        {
            d.EnsureSize();
            grid.Begin(area, d.boardCols, d.boardRows);
            Event e = Event.current;

            if (e.type == EventType.Repaint)
            {
                for (int r = 0; r < d.boardRows; r++)
                    for (int c = 0; c < d.boardCols; c++)
                        DrawCell(d, colors, c, r);
                foreach (Vector2Int p in _selection) GridView.Outline(grid.CellRect(p.x, p.y), Color.yellow, 2f);
            }

            bool inside = grid.CellAt(e.mousePosition, out int hc, out int hr);
            hoverInfo = inside ? $"Ô ({hc},{hr})  mã: {LevelData.CellToken(d.Cell(hc, hr))}" : "";

            if (inside && e.button == 0 && (e.type == EventType.MouseDown || (e.type == EventType.MouseDrag && _dragging)))
            {
                if (e.type == EventType.MouseDown)
                {
                    _dragging = true;
                    _dragStart = new Vector2Int(hc, hr);
                    if (tool != Tool.Select && tool != Tool.Pick) Undo.RecordObject(d, "Vẽ bàn gạch");
                }
                UseTool(d, hc, hr, e.type == EventType.MouseDown);
                changed?.Invoke();
                e.Use();
            }
            if (e.type == EventType.MouseUp) _dragging = false;
            grid.End();
        }

        void DrawCell(LevelData d, ColorLibrary colors, int c, int r)
        {
            Cell x = d.Cell(c, r);
            Rect rect = grid.CellRect(c, r);
            Rect inner = new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2);
            switch (x.type)
            {
                case CellType.Empty: EditorGUI.DrawRect(inner, EmptyColor); break;
                case CellType.Column:
                    EditorGUI.DrawRect(inner, ColumnColor);
                    GridView.Label(inner, "C" + x.hp, Color.black, 8);
                    break;
                case CellType.Blocker:
                    EditorGUI.DrawRect(inner, Color.black);
                    GridView.Label(inner, "✕", Color.gray, 10);
                    break;
                case CellType.Cube:
                    Color col = colors != null ? colors.ColorOf(x.color) : Color.magenta;
                    EditorGUI.DrawRect(inner, col);
                    if (!string.IsNullOrEmpty(x.group))
                    {
                        // viền khối gộp ở cạnh giáp ô không cùng khối
                        Color edge = Color.Lerp(col, Color.black, 0.55f);
                        if (!SameGroup(d, x, c, r - 1)) EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2), edge);
                        if (!SameGroup(d, x, c, r + 1)) EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), edge);
                        if (!SameGroup(d, x, c - 1, r)) EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), edge);
                        if (!SameGroup(d, x, c + 1, r)) EditorGUI.DrawRect(new Rect(rect.xMax - 2, rect.y, 2, rect.height), edge);
                    }
                    else if (x.hp > 1) GridView.Label(inner, x.hp.ToString(), GridView.TextOn(col), 8);
                    if (!string.IsNullOrEmpty(x.crate))
                    {
                        Color cc = CrateColor; cc.a = 0.55f;
                        EditorGUI.DrawRect(new Rect(inner.x, inner.y, inner.width, inner.height * 0.35f), cc);
                    }
                    break;
            }
            // nhãn máu khối gộp: ở ô trên-trái của khối
            if (x.type == CellType.Cube && !string.IsNullOrEmpty(x.group) && !SameGroup(d, x, c, r - 1) && !SameGroup(d, x, c - 1, r))
                GridView.Label(inner, x.hp.ToString(), GridView.TextOn(colors != null ? colors.ColorOf(x.color) : Color.white), 8);
        }

        static bool SameGroup(LevelData d, Cell x, int c, int r)
        {
            if (c < 0 || r < 0 || c >= d.boardCols || r >= d.boardRows) return false;
            Cell y = d.Cell(c, r);
            return y.type == CellType.Cube && y.group == x.group && y.crate == x.crate;
        }

        // ------------------------------------------------------------------ Công cụ
        void UseTool(LevelData d, int c, int r, bool down)
        {
            Cell x = d.Cell(c, r);
            switch (tool)
            {
                case Tool.Brush: PaintCube(x); break;
                case Tool.Erase: Clear(x); break;
                case Tool.Fill: if (down) Fill(d, c, r); break;
                case Tool.Pick:
                    if (x.type == CellType.Cube) { color = x.color; hp = x.hp; tool = Tool.Brush; }
                    break;
                case Tool.Select:
                    _selection.Clear();
                    int x0 = Mathf.Min(_dragStart.x, c), x1 = Mathf.Max(_dragStart.x, c), y0 = Mathf.Min(_dragStart.y, r), y1 = Mathf.Max(_dragStart.y, r);
                    for (int yy = y0; yy <= y1; yy++) for (int xx = x0; xx <= x1; xx++) _selection.Add(new Vector2Int(xx, yy));
                    break;
            }
            EditorUtility.SetDirty(d);
        }

        void PaintCube(Cell x)
        {
            if (x.type != CellType.Cube) { x.type = CellType.Cube; x.group = ""; }
            x.color = color;
            if (string.IsNullOrEmpty(x.group)) x.hp = hp;
            x.raw = "";
        }

        static void Clear(Cell x)
        {
            x.type = CellType.Empty; x.color = ""; x.hp = 1; x.group = ""; x.crate = ""; x.overlay = ""; x.raw = "";
        }

        void Fill(LevelData d, int c, int r)
        {
            string key = LevelData.CellToken(d.Cell(c, r));
            var stack = new Stack<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            stack.Push(new Vector2Int(c, r));
            while (stack.Count > 0)
            {
                Vector2Int p = stack.Pop();
                if (p.x < 0 || p.y < 0 || p.x >= d.boardCols || p.y >= d.boardRows || !seen.Add(p)) continue;
                if (LevelData.CellToken(d.Cell(p.x, p.y)) != key) continue;
                PaintCube(d.Cell(p.x, p.y));
                stack.Push(p + Vector2Int.up); stack.Push(p + Vector2Int.down); stack.Push(p + Vector2Int.left); stack.Push(p + Vector2Int.right);
            }
        }

        void Apply(LevelData d, string undoName, System.Action<LevelData> op)
        {
            Undo.RecordObject(d, undoName);
            op(d);
            EditorUtility.SetDirty(d);
        }

        string NewId(LevelData d, System.Func<Cell, string> idOf)
        {
            int max = 0;
            foreach (Cell x in d.board) if (int.TryParse(idOf(x), out int v)) max = Mathf.Max(max, v);
            return (max + 1).ToString();
        }

        void MakeGroup(LevelData d)
        {
            string id = NewId(d, x => x.type == CellType.Cube ? x.group : "");
            string groupColor = null;
            foreach (Vector2Int p in _selection)
            {
                Cell x = d.Cell(p.x, p.y);
                if (x.type != CellType.Cube) continue;
                groupColor ??= x.color;   // khối gộp dùng một màu: lấy màu ô đầu tiên
                x.color = groupColor; x.group = id; x.hp = groupHp;
            }
        }

        void MakeCrate(LevelData d)
        {
            string id = NewId(d, x => x.crate);
            foreach (Vector2Int p in _selection)
            {
                Cell x = d.Cell(p.x, p.y);
                if (x.type != CellType.Cube) continue;   // hòm chỉ che gạch (game dựng hòm từ các ô gạch có /cr)
                x.crate = id; x.crateHp = crateHp;
            }
        }

        void MakeColumn(LevelData d)
        {
            string id = NewId(d, x => x.type == CellType.Column ? x.group : "");
            foreach (Vector2Int p in _selection)
            {
                Cell x = d.Cell(p.x, p.y);
                Clear(x);
                x.type = CellType.Column; x.group = id; x.hp = columnHp;
            }
        }

        public void ClearSelection() => _selection.Clear();
    }
}
