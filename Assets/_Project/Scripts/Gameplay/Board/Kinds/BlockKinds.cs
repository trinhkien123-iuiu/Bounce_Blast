using System.Collections.Generic;

namespace BounceBlast.Gameplay
{
    /// <summary>Bảng tra loại khối → luật của nó</summary>
    public static class BlockKinds
    {
        static readonly Dictionary<BoardItemKind, BlockKind> All = new Dictionary<BoardItemKind, BlockKind>();

        static BlockKinds()
        {
            Register(new CubeKind());
            Register(new CombinedCubeKind());
            Register(new CrateKind());
            Register(new WallKind());
            Register(new ColumnKind());
            Register(new StoneKind());
            Register(new CurtainKind());
            Register(new BombKind());
            Register(new BoardTunnelKind());
        }

        static void Register(BlockKind k) => All[k.Kind] = k;

        /// <summary>Dùng cho lấy loại khối theo kiểu dữ liệu</summary>
        public static BlockKind Of(BoardItemKind kind)
        {
            if (All.TryGetValue(kind, out BlockKind k)) return k;
            throw new KeyNotFoundException("Chưa đăng ký BlockKind cho " + kind + " (thêm vào BlockKinds)");
        }
    }
}
