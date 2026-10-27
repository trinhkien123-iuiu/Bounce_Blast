using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Bom "/bomb-timer-id": đếm ngược; bi chạm là gỡ (bom biến mất, không phá gạch), hết giờ chưa gỡ thì nổ và THUA level</summary>
    public class BombKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.Bomb;
        /// <summary>Bi màu nào cũng trừ máu bom và không bị tiêu hao</summary>
        public override bool HitByAnyColor => true;
        public override bool ShearMesh => false;
        public override bool HasLabel => false;
        public override float HitPunch => 0.08f;
        /// <summary>Bom đã nổ (hết giờ, đang chờ hiện popup thua) thì bi không gỡ được nữa</summary>
        public override bool Accepts(BoardBlock block, string ballColor)
        {
            BombView view = block.GetComponentInChildren<BombView>();
            return view == null || !view.Exploded;
        }

        /// <summary>Số trên bom là thời gian còn lại, không phải máu</summary>
        public override bool ShowsHpLabel(BoardBlock block) => false;

        /// <summary>Xử lý dựng hình bom: quả TNT + số đếm ngược</summary>
        public override BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts)
        {
            BombView view = BombView.Build(parent, item, board.CellSize, board.Config, board.Visuals, parent.gameObject.layer);
            return new BlockView { visual = view.transform };
        }

        /// <summary>Gỡ bom: ngòi tắt, bom biến mất (KHÔNG nổ, KHÔNG phá gạch nào – để số bi và số gạch của level vẫn khớp)</summary>
        public override void OnBroken(BoardController board, BoardBlock block)
        {
            BombView view = block.GetComponentInChildren<BombView>();
            if (view != null) view.Defuse();
        }
    }
}
