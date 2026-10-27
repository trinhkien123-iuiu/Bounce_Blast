using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Luật của TUNNEL TRÊN BÀN (/btu-X) (video boardTunnel + code gốc BlockerBoardTunnel: managedColumns, RelocateForBoardTunnel, IsThereAnyCubeAbove, Break).
    /// Mỗi cột phía trên miệng tunnel (từ ô sát miệng tới đỉnh chồng cube lúc đầu) giữ cube dồn về phía TRÊN: gạch trên bị phá thì cube bên dưới đẩy lên,
    /// ở đáy cột tunnel nhả cube mới (màu theo hàng đợi, mỗi "màu-N" = N×Multiplier² cube). Cột còn nguyên thì đứng yên. Hết hàng đợi và hết cube thì tunnel biến mất</summary>
    public class BoardTunnels
    {
        class Tunnel
        {
            public BoardBlock block;
            public BoardTunnelView view;
            public RectInt mouth;
            public int[] top;
            public float[] ready;
            public readonly List<TunnelBatch> queue = new List<TunnelBatch>();
            public int remaining;
            /// <summary>Số về 0: thôi đẩy, cube đã được thả để vật lý bình thường</summary>
            public bool released;
        }

        readonly BoardController _board;
        readonly BoardGrid _grid;
        readonly GameConfig _config;
        readonly List<Tunnel> _tunnels = new List<Tunnel>();
        bool _initialised;

        public BoardTunnels(BoardController board, BoardGrid grid, GameConfig config)
        {
            _board = board;
            _grid = grid;
            _config = config;
        }

        public bool Any => _tunnels.Count > 0;

        /// <summary>Đăng ký một tunnel vừa dựng</summary>
        public void Register(BoardBlock block, BoardItemData item, BoardTunnelView view)
        {
            var t = new Tunnel { block = block, view = view, mouth = item.Cells };
            foreach (TunnelBatch b in item.Queue)
            {
                t.queue.Add(b);
                t.remaining += b.Cubes;
            }
            _tunnels.Add(t);
        }

        /// <summary>Sau khi dựng xong cả bàn: tính vùng quản lý của từng cột và ghim các cube trong đó</summary>
        public void Init()
        {
            _initialised = true;
            foreach (Tunnel t in _tunnels)
            {
                int w = t.mouth.width;
                t.top = new int[w];
                t.ready = new float[w];
                for (int i = 0; i < w; i++)
                {
                    int x = t.mouth.x + i;
                    int y = t.mouth.yMax;
                    while (IsZoneCube(_grid.BlockAt(x, y))) { Pin(_grid.BlockAt(x, y)); y++; }
                    t.top[i] = y - 1;
                }
            }
        }

        void Pin(BoardBlock b)
        {
            b.PinnedByTunnel = true;
            b.PinnedSpeed = _grid.Cell * _config.boardTunnelPushSpeed;
        }

        static bool IsZoneCube(BoardBlock b) =>
            b != null && !b.IsDestroyed && b.Kind == BoardItemKind.Cube && b.Cells.width == 1 && b.Cells.height == 1;

        /// <summary>Mỗi khung hình: dồn cube lên trên, nhả cube mới, hết việc thì phá tunnel</summary>
        public void Tick(float now)
        {
            if (!_initialised) return;
            for (int k = _tunnels.Count - 1; k >= 0; k--)
            {
                Tunnel t = _tunnels[k];
                if (t.block == null || t.block.IsDestroyed) { _tunnels.RemoveAt(k); continue; }
                if (t.released)
                {
                    // đã hết hàng đợi: tunnel chỉ còn là vật cản đứng yên, hết cube phía trên thì biến mất (IsThereAnyCubeAbove → Break)
                    if (!AnyCubeAbove(t)) { _tunnels.RemoveAt(k); _board.DestroyBlock(t.block); }
                    continue;
                }
                bool anyCube = false;
                for (int i = 0; i < t.mouth.width; i++)
                {
                    int x = t.mouth.x + i, yb = t.mouth.yMax, yt = t.top[i];
                    if (yt < yb) continue;
                    int n = 0;
                    BoardBlock lowest = null;
                    for (int y = yt; y >= yb; y--)
                    {
                        BoardBlock b = _grid.BlockAt(x, y);
                        if (!IsZoneCube(b)) continue;
                        int target = yt - n++;
                        anyCube = true;
                        if (b.Cells.y != target) Move(b, x, target);
                        lowest = b;
                    }
                    int free = yt - yb + 1 - n;
                    if (free > 0 && t.remaining > 0 && now >= t.ready[i] && BottomClear(lowest, x, yb))
                    {
                        Emit(t, x, yb);
                        t.ready[i] = now + _config.boardTunnelPushInterval;
                        anyCube = true;
                    }
                }
                if (t.remaining <= 0) Release(t);
            }
        }

        /// <summary>Số về 0: không đẩy nữa, thả mọi cube trong vùng để rơi theo vật lý bình thường</summary>
        void Release(Tunnel t)
        {
            t.released = true;
            for (int i = 0; i < t.mouth.width; i++)
                for (int y = t.mouth.yMax; y <= t.top[i]; y++)
                {
                    BoardBlock b = _grid.BlockAt(t.mouth.x + i, y);
                    if (IsZoneCube(b)) { b.PinnedByTunnel = false; b.PinnedSpeed = 0f; }
                }
            _board.RequestSettle();
        }

        bool AnyCubeAbove(Tunnel t)
        {
            for (int i = 0; i < t.mouth.width; i++)
                for (int y = t.mouth.yMax; y <= t.top[i]; y++)
                    if (IsZoneCube(_grid.BlockAt(t.mouth.x + i, y))) return true;
            return false;
        }

        /// <summary>Ô đáy cột đã trống thật chưa: cube thấp nhất (đang được đẩy lên) phải rời ô đáy ít nhất 0.75 ô rồi mới nhả cube kế, để hai cube không chồng lên nhau</summary>
        bool BottomClear(BoardBlock lowest, int x, int yBottom)
        {
            if (lowest == null) return true;
            float bottomY = _grid.CellToWorld(new RectInt(x, yBottom, 1, 1)).y;
            return lowest.transform.position.y - bottomY >= _grid.Cell * 0.75f;
        }

        void Move(BoardBlock b, int x, int y)
        {
            _grid.Occupy(b, false);
            b.Cells = new RectInt(x, y, 1, 1);
            _grid.Occupy(b, true);
            b.SetTarget(_grid.CellToWorld(b.Cells));
        }

        /// <summary>Nhả một cube từ hàng đợi: nó trồi ra từ miệng tunnel và chạy lên ô đáy cột</summary>
        void Emit(Tunnel t, int x, int yBottom)
        {
            string color = null;
            for (int q = 0; q < t.queue.Count; q++)
            {
                TunnelBatch b = t.queue[q];
                if (b.Cubes <= 0) continue;
                color = b.Color;
                b.Cubes--;
                t.queue[q] = b;
                break;
            }
            if (color == null) { t.remaining = 0; return; }
            t.remaining--;
            if (t.view != null) t.view.SetCount(t.remaining);

            var item = new BoardItemData { Kind = BoardItemKind.Cube, ColorCode = color, Hp = 1, Cells = new RectInt(x, yBottom, 1, 1) };
            BoardBlock nb = _board.Spawn(item);
            if (nb == null) return;
            // cube xuất hiện ngay ở ô đáy cột (sát mép trên miệng tunnel); các cube phía trên đã được đẩy lên trong Tick
            Pin(nb);
        }
    }
}
