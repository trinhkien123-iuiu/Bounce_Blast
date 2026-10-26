using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Lưới chiếm chỗ của bàn chơi</summary>
    public class BoardGrid
    {
        readonly BoardBlock[,] _cells;
        readonly int _rows;

        public Vector2 Origin { get; }
        public float Cell { get; }
        public int Version { get; private set; }

        public BoardGrid(int cols, int rows, Vector2 origin, float cell)
        {
            _cells = new BoardBlock[cols, Mathf.Max(rows, 64)];
            _rows = rows;
            Origin = origin;
            Cell = cell;
        }

        /// <summary>Dùng cho đổi ô lưới sang toạ độ thế giới</summary>
        public Vector2 CellToWorld(RectInt r) =>
            Origin + new Vector2((r.x + r.width * 0.5f) * Cell, (r.y + r.height * 0.5f) * Cell);

        /// <summary>Kiểm tra vùng ô nằm trong lưới</summary>
        public bool Contains(RectInt r) =>
            r.xMin >= 0 && r.xMax <= _cells.GetLength(0) && r.yMin >= 0 && r.yMax <= _cells.GetLength(1);

        /// <summary>Khối đang chiếm ô chứa điểm thế giới này</summary>
        public BoardBlock BlockAtWorld(Vector2 p)
        {
            int x = Mathf.FloorToInt((p.x - Origin.x) / Cell), y = Mathf.FloorToInt((p.y - Origin.y) / Cell);
            if (x < 0 || y < 0 || x >= _cells.GetLength(0) || y >= _cells.GetLength(1)) return null;
            BoardBlock b = _cells[x, y];
            return b == null || b.IsDestroyed ? null : b;
        }

        /// <summary>Đặt (on = true) hoặc gỡ khối khỏi các ô nó chiếm</summary>
        public void Occupy(BoardBlock b, bool on)
        {
            Version++;
            foreach (Vector2Int c in b.OccupiedCells())
                _cells[c.x, c.y] = on ? b : (_cells[c.x, c.y] == b ? null : _cells[c.x, c.y]);
        }

        /// <summary>Khối có thể rơi xuống một hàng</summary>
        public bool CanFall(BoardBlock b)
        {
            if (b.IsStatic) return false;
            foreach (Vector2Int c in b.OccupiedCells())
            {
                if (c.y == 0) return false;
                BoardBlock below = _cells[c.x, c.y - 1];
                if (below != null && below != b) return false;
            }
            return true;
        }

        /// <summary>Khối có ít nhất một ô lộ ra</summary>
        public bool IsExposed(BoardBlock b)
        {
            int cols = _cells.GetLength(0);
            RectInt r = b.Cells;
            if (b.Mask == null)
            {
                for (int x = r.xMin; x < r.xMax; x++)
                    for (int y = r.yMin; y < r.yMax; y++)
                        if (CellExposed(x, y, cols, b)) return true;
                return false;
            }
            for (int i = 0; i < b.Mask.Count; i++)
                if (CellExposed(r.x + b.Mask[i].x, r.y + b.Mask[i].y, cols, b)) return true;
            return false;
        }

        bool CellExposed(int x, int y, int cols, BoardBlock b) =>
            y + 1 >= _rows || Free(x, y + 1, b) || x == 0 || Free(x - 1, y, b) || x + 1 >= cols || Free(x + 1, y, b);

        bool Free(int x, int y, BoardBlock self) => _cells[x, y] == null || _cells[x, y] == self;
    }
}
