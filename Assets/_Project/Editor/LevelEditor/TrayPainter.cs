using BounceBlast.Gameplay;
using UnityEditor;
using UnityEngine;
using Slot = BounceBlast.Gameplay.LevelData.Slot;
using SlotType = BounceBlast.Gameplay.LevelData.SlotType;
using Box = BounceBlast.Gameplay.LevelData.Box;

namespace BounceBlast.EditorTools
{
    /// <summary>Tab "Khay đạn" của Level Editor</summary>
    public class TrayPainter
    {
        /// <summary>Dùng cho công cụ vẽ khay đạn</summary>
        public enum Tool { Box, Empty, Wall, Off, Tunnel, Edit }

        public Tool tool = Tool.Box;
        public string color = "r";
        public int balls = 8;
        public int ice;
        public bool hidden;
        public int door;
        public string tunnelId = "A", tunnelDir = "D";
        public readonly GridView grid = new GridView { cellSize = 46f };
        public string hoverInfo = "";

        Vector2Int _selected = new Vector2Int(-1, -1);
        bool _dragging;

        static readonly Color OffColor = new Color(0.1f, 0.1f, 0.12f);
        static readonly Color WallColor = new Color(0.28f, 0.22f, 0.36f);
        static readonly Color EmptyColor = new Color(0.36f, 0.38f, 0.62f);
        static readonly Color OpenColor = new Color(0.3f, 1f, 0.45f);

        /// <summary>Xử lý vẽ thanh công cụ khay đạn</summary>
        public void DrawToolbar(LevelData d, ColorLibrary colors)
        {
            EditorGUILayout.BeginHorizontal();
            tool = (Tool)GUILayout.Toolbar((int)tool, new[] { "Hộp", "Ô trống", "Tường", "Ngoài khay", "Tunnel", "Sửa ô" }, GUILayout.Height(22));
            GUILayout.Label("Zoom", GUILayout.Width(36));
            grid.cellSize = GUILayout.HorizontalSlider(grid.cellSize, 24f, 70f, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            if (tool == Tool.Box)
            {
                BoardPainter.ColorPalette(colors, ref color);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Số bi", GUILayout.Width(36)); balls = Mathf.Max(1, EditorGUILayout.IntField(balls, GUILayout.Width(40)));
                GUILayout.Label($"(= {balls * Mul(d)} bi trong game)", GUILayout.Width(120));
                GUILayout.Label("Băng", GUILayout.Width(34)); ice = Mathf.Clamp(EditorGUILayout.IntField(ice, GUILayout.Width(30)), 0, 9);
                hidden = GUILayout.Toggle(hidden, "Hộp \"?\"", GUILayout.Width(70));
                GUILayout.Label("Cửa", GUILayout.Width(28)); door = EditorGUILayout.Popup(door, new[] { "Không", "Đóng", "Mở" }, GUILayout.Width(70));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
            else if (tool == Tool.Tunnel)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Id", GUILayout.Width(18)); tunnelId = EditorGUILayout.TextField(tunnelId, GUILayout.Width(30)).ToUpperInvariant();
                GUILayout.Label("Hướng nhả", GUILayout.Width(64));
                int di = System.Array.IndexOf(new[] { "U", "D", "L", "R" }, tunnelDir);
                di = GUILayout.Toolbar(Mathf.Max(0, di), new[] { "↑ lên", "↓ xuống", "← trái", "→ phải" });
                tunnelDir = new[] { "U", "D", "L", "R" }[di];
                EditorGUILayout.EndHorizontal();
            }
        }

        static int Mul(LevelData d) => Mathf.Max(1, d.multiplier) * Mathf.Max(1, d.multiplier);

        /// <summary>Xử lý vẽ lưới khay đạn</summary>
        public void DrawGrid(Rect area, LevelData d, ColorLibrary colors, System.Action changed)
        {
            d.EnsureSize();
            bool[,] open = OpenCells(d);
            grid.Begin(area, d.ammoCols, d.ammoRows);
            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                for (int r = 0; r < d.ammoRows; r++)
                    for (int c = 0; c < d.ammoCols; c++)
                        DrawSlot(d, colors, c, r, open[c, r]);
                if (_selected.x >= 0 && _selected.x < d.ammoCols && _selected.y < d.ammoRows) GridView.Outline(grid.CellRect(_selected.x, _selected.y), Color.yellow, 3f);
            }
            bool inside = grid.CellAt(e.mousePosition, out int hc, out int hr);
            hoverInfo = inside ? $"Ô ({hc},{hr})  mã: {LevelData.SlotToken(d.AmmoSlot(hc, hr))}   (hàng 0 = sát lối ra, trên cùng)" : "";
            if (inside && e.button == 0 && (e.type == EventType.MouseDown || (e.type == EventType.MouseDrag && _dragging)))
            {
                if (e.type == EventType.MouseDown) { _dragging = true; if (tool != Tool.Edit) Undo.RecordObject(d, "Xếp khay"); }
                UseTool(d, hc, hr);
                changed?.Invoke();
                e.Use();
            }
            if (e.type == EventType.MouseUp) _dragging = false;
            grid.End();
        }

        void DrawSlot(LevelData d, ColorLibrary colors, int c, int r, bool open)
        {
            Slot s = d.AmmoSlot(c, r);
            Rect rect = grid.CellRect(c, r);
            Rect inner = new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4);
            switch (s.type)
            {
                case SlotType.None: EditorGUI.DrawRect(inner, OffColor); break;
                case SlotType.Wall: EditorGUI.DrawRect(inner, WallColor); GridView.Label(inner, "tường", Color.gray, 8); break;
                case SlotType.Empty: EditorGUI.DrawRect(inner, EmptyColor); break;
                case SlotType.Tunnel:
                    EditorGUI.DrawRect(inner, new Color(0.25f, 0.3f, 0.4f));
                    string arrow = s.tunnelDir == "U" ? "↑" : s.tunnelDir == "L" ? "←" : s.tunnelDir == "R" ? "→" : "↓";
                    LevelData.Tunnel tu = d.tunnels.Find(t => t.id == s.tunnelId);
                    GridView.Label(inner, $"T{s.tunnelId}{arrow}\n{(tu != null ? tu.queue.Count : 0)} hộp", Color.white, 9);
                    break;
                case SlotType.Box:
                    Box b = s.box;
                    Color col = colors != null ? colors.ColorOf(b.color) : Color.magenta;
                    EditorGUI.DrawRect(inner, b.hidden ? Color.Lerp(col, Color.gray, 0.6f) : col);
                    string extra = (b.hidden ? "?" : "") + (b.ice > 0 ? $" ❄{b.ice}" : "") + (b.door == 1 ? " ▣" : b.door == 2 ? " □" : "");
                    GridView.Label(inner, $"{b.balls * Mul(d)}{(extra.Length > 0 ? "\n" + extra.Trim() : "")}", GridView.TextOn(col), 10);
                    if (b.ice > 0) GridView.Outline(inner, new Color(0.7f, 0.9f, 1f), 3f);
                    break;
            }
            if (open && s.type == SlotType.Box) GridView.Outline(rect, OpenColor, 2f);
        }

        void UseTool(LevelData d, int c, int r)
        {
            Slot s = d.AmmoSlot(c, r);
            switch (tool)
            {
                case Tool.Box:
                    s.type = SlotType.Box;
                    s.box = new Box { color = color, balls = balls, ice = ice, hidden = hidden, door = door };
                    break;
                case Tool.Empty: s.type = SlotType.Empty; break;
                case Tool.Wall: s.type = SlotType.Wall; s.raw = "/awl"; break;
                case Tool.Off: s.type = SlotType.None; break;
                case Tool.Tunnel:
                    s.type = SlotType.Tunnel; s.tunnelId = string.IsNullOrEmpty(tunnelId) ? "A" : tunnelId.Substring(0, 1); s.tunnelDir = tunnelDir;
                    if (!d.tunnels.Exists(t => t.id == s.tunnelId)) d.tunnels.Add(new LevelData.Tunnel { id = s.tunnelId });
                    break;
                case Tool.Edit: _selected = new Vector2Int(c, r); break;
            }
            EditorUtility.SetDirty(d);
        }

        /// <summary>Ô có hộp lấy ra được ngay lúc bắt đầu</summary>
        static bool[,] OpenCells(LevelData d)
        {
            var kind = new AmmoCellKind[d.ammoCols, d.ammoRows];
            for (int r = 0; r < d.ammoRows; r++)
                for (int c = 0; c < d.ammoCols; c++)
                {
                    SlotType t = d.AmmoSlot(c, r).type;
                    kind[c, r] = t == SlotType.Empty ? AmmoCellKind.Empty : t == SlotType.Box ? AmmoCellKind.Box : t == SlotType.Tunnel ? AmmoCellKind.Tunnel : AmmoCellKind.Wall;
                }
            var rules = new AmmoPathRules();
            rules.Compute(kind);
            var open = new bool[d.ammoCols, d.ammoRows];
            for (int r = 0; r < d.ammoRows; r++)
                for (int c = 0; c < d.ammoCols; c++)
                    open[c, r] = rules.IsOpen(new Vector2Int(c, r));
            return open;
        }

        /// <summary>Xử lý vẽ bảng thuộc tính ô đang chọn</summary>
        public void DrawInspector(LevelData d, ColorLibrary colors)
        {
            if (tool == Tool.Edit && _selected.x >= 0 && _selected.x < d.ammoCols && _selected.y < d.ammoRows)
            {
                Slot s = d.AmmoSlot(_selected.x, _selected.y);
                EditorGUILayout.LabelField($"Ô đang sửa ({_selected.x},{_selected.y})", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                var type = (SlotType)EditorGUILayout.EnumPopup("Loại ô", s.type);
                Box b = s.box ?? new Box();
                string bc = b.color; int bb = b.balls, bi = b.ice, bd = b.door; bool bh = b.hidden;
                if (type == SlotType.Box)
                {
                    bc = EditorGUILayout.TextField("Màu", b.color);
                    bb = Mathf.Max(1, EditorGUILayout.IntField($"Số bi (×{Mul(d)})", b.balls));
                    bi = Mathf.Clamp(EditorGUILayout.IntField("Băng", b.ice), 0, 9);
                    bh = EditorGUILayout.Toggle("Hộp \"?\"", b.hidden);
                    bd = EditorGUILayout.Popup("Cửa", b.door, new[] { "Không", "Đóng lúc đầu", "Mở lúc đầu" });
                }
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(d, "Sửa ô khay");
                    s.type = type;
                    s.box = new Box { color = bc, balls = bb, ice = bi, hidden = bh, door = bd };
                    EditorUtility.SetDirty(d);
                }
                EditorGUILayout.Space();
            }

            if (d.tunnels.Count == 0) return;
            EditorGUILayout.LabelField("Hàng chờ tunnel", EditorStyles.boldLabel);
            for (int i = 0; i < d.tunnels.Count; i++)
            {
                LevelData.Tunnel tu = d.tunnels[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Tunnel " + tu.id, GUILayout.Width(70));
                if (GUILayout.Button("+ hộp (màu & bi đang chọn)", GUILayout.Width(180)))
                {
                    Undo.RecordObject(d, "Thêm hộp vào tunnel");
                    tu.queue.Add(new Box { color = color, balls = balls });
                    EditorUtility.SetDirty(d);
                }
                if (GUILayout.Button("Xoá tunnel", GUILayout.Width(80))) { Undo.RecordObject(d, "Xoá tunnel"); d.tunnels.RemoveAt(i); EditorUtility.SetDirty(d); EditorGUILayout.EndHorizontal(); break; }
                EditorGUILayout.EndHorizontal();
                for (int k = 0; k < tu.queue.Count; k++)
                {
                    Box q = tu.queue[k];
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    Rect sw = GUILayoutUtility.GetRect(16, 16, GUILayout.Width(16));
                    EditorGUI.DrawRect(sw, colors != null ? colors.ColorOf(q.color) : Color.white);
                    EditorGUI.BeginChangeCheck();
                    string qc = EditorGUILayout.TextField(q.color, GUILayout.Width(36));
                    int qb = Mathf.Max(1, EditorGUILayout.IntField(q.balls, GUILayout.Width(40)));
                    bool del = GUILayout.Button("✕", GUILayout.Width(22));
                    if (EditorGUI.EndChangeCheck() || del)
                    {
                        Undo.RecordObject(d, "Sửa hàng chờ tunnel");
                        if (del) tu.queue.RemoveAt(k); else { q.color = qc; q.balls = qb; }
                        EditorUtility.SetDirty(d);
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}
