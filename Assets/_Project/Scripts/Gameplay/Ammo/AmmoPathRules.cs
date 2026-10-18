using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Luật thông đường của khay (gốc: GridAmmo): một ô "thông" khi ở hàng sát lối ra (hàng 0) hoặc kề một ô trống
    /// có đường đi qua các ô trống tới lối ra (BFS). Chỉ đọc lưới loại ô – không biết gì về hình hay hộp.
    /// </summary>
    public class AmmoPathRules
    {
        static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        bool[,] _reach;
        int _cols, _rows;

        /// <summary>Tính lại các ô trống thông ra lối ra (phía trên hàng 0).</summary>
        public void Compute(AmmoCellKind[,] kind)
        {
            _cols = kind.GetLength(0);
            _rows = kind.GetLength(1);
            if (_reach == null || _reach.GetLength(0) != _cols || _reach.GetLength(1) != _rows) _reach = new bool[_cols, _rows];
            else Array.Clear(_reach, 0, _reach.Length);
            var q = new Queue<Vector2Int>();
            for (int c = 0; c < _cols; c++)
                if (kind[c, 0] == AmmoCellKind.Empty) { _reach[c, 0] = true; q.Enqueue(new Vector2Int(c, 0)); }
            while (q.Count > 0)
            {
                Vector2Int p = q.Dequeue();
                foreach (Vector2Int d in Dirs)
                {
                    Vector2Int n = p + d;
                    if (!InGrid(n) || kind[n.x, n.y] != AmmoCellKind.Empty || _reach[n.x, n.y]) continue;
                    _reach[n.x, n.y] = true;
                    q.Enqueue(n);
                }
            }
        }

        /// <summary>Hộp ở ô này lấy ra được (sau <see cref="Compute"/>).</summary>
        public bool IsOpen(Vector2Int p)
        {
            if (p.y == 0) return true;
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int n = p + d;
                if (InGrid(n) && _reach[n.x, n.y]) return true;
            }
            return false;
        }

        bool InGrid(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < _cols && p.y < _rows;
    }
}
