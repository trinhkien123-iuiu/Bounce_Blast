using UnityEditor;
using UnityEngine;

namespace BounceBlast.EditorTools
{
    /// <summary>Vẽ một lưới ô có zoom/cuộn và trả về ô dưới chuột</summary>
    public class GridView
    {
        public float cellSize = 22f;
        Vector2 _scroll;
        Rect _gridRect;
        int _cols, _rows;

        /// <summary>Bắt đầu vùng cuộn; trả về Rect của lưới</summary>
        public Rect Begin(Rect area, int cols, int rows)
        {
            _cols = cols; _rows = rows;
            var content = new Rect(0, 0, cols * cellSize + 20f, rows * cellSize + 20f);
            _scroll = GUI.BeginScrollView(area, _scroll, content);
            _gridRect = new Rect(10f, 10f, cols * cellSize, rows * cellSize);
            return _gridRect;
        }

        /// <summary>Xử lý kết thúc vùng cuộn</summary>
        public void End() => GUI.EndScrollView();

        /// <summary>Dùng cho lấy hình chữ nhật của ô</summary>
        public Rect CellRect(int c, int r) => new Rect(_gridRect.x + c * cellSize, _gridRect.y + r * cellSize, cellSize, cellSize);

        /// <summary>Ô dưới chuột (toạ độ trong vùng cuộn)</summary>
        public bool CellAt(Vector2 mouse, out int c, out int r)
        {
            c = Mathf.FloorToInt((mouse.x - _gridRect.x) / cellSize);
            r = Mathf.FloorToInt((mouse.y - _gridRect.y) / cellSize);
            return c >= 0 && r >= 0 && c < _cols && r < _rows;
        }

        /// <summary>Xử lý vẽ viền</summary>
        public static void Outline(Rect r, Color c, float w = 1f)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - w, r.width, w), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, w, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y, w, r.height), c);
        }

        static GUIStyle _label;
        /// <summary>Xử lý vẽ chữ trong ô</summary>
        public static void Label(Rect r, string text, Color color, int size = 9)
        {
            if (_label == null) _label = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, clipping = TextClipping.Overflow };
            _label.fontSize = size;
            _label.normal.textColor = color;
            GUI.Label(r, text, _label);
        }

        /// <summary>Chữ đen hoặc trắng tuỳ độ sáng nền</summary>
        public static Color TextOn(Color bg) => bg.r * 0.3f + bg.g * 0.59f + bg.b * 0.11f > 0.55f ? Color.black : Color.white;
    }
}
