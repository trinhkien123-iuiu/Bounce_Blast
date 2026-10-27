using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tunnel trên bàn "/btu-X": thanh kim loại đứng yên; các cột phía trên nó luôn được đẩy dồn lên trên và tunnel nhả cube mới ở đáy cột
    /// (BoardTunnels). Bi chỉ nảy, búa không phá được; hết hàng đợi và hết cube phía trên thì tunnel biến mất</summary>
    public class BoardTunnelKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.BoardTunnel;
        public override bool IsStatic => true;
        public override bool ShearMesh => false;
        public override bool HasLabel => false;
        public override bool Tappable => false;
        public override bool ShowsHpLabel(BoardBlock block) => false;
        public override bool Accepts(BoardBlock block, string ballColor) => false;

        /// <summary>Xử lý dựng hình tunnel: các mảnh gốc + tấm số</summary>
        public override BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts)
        {
            BoardTunnelView view = BoardTunnelView.Build(parent, item, board.CellSize, board.Multiplier, board.Config, board.Visuals, parent.gameObject.layer);
            return new BlockView { visual = view.transform };
        }

        /// <summary>Tunnel hết việc: thu nhỏ biến mất</summary>
        public override void OnBroken(BoardController board, BoardBlock block)
        {
            BoardTunnelView view = block.GetComponentInChildren<BoardTunnelView>();
            if (view != null) view.Break();
        }
    }
}
