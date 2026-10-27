using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Rèm "/cur-hp-id-màu": phủ lên các ô cùng id; con lắc đếm ngược theo số gạch 1 máu cùng màu bị phá, về 0 thì mở rèm (video curtainMechanics + fixBug)</summary>
    public class CurtainKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.Curtain;
        public override bool ShearMesh => false;
        public override bool HasLabel => false;
        public override float HitPunch => 0f;

        /// <summary>Bi không trừ máu rèm (bi chỉ nảy ra như chạm tường); số ở con lắc giảm khi gạch 1 máu cùng màu vỡ (BoardController.TickCurtains)</summary>
        public override bool Accepts(BoardBlock block, string ballColor) => false;

        /// <summary>Số máu nằm trên huy hiệu của rèm, không có nhãn riêng</summary>
        public override bool ShowsHpLabel(BoardBlock block) => false;

        /// <summary>Xử lý dựng hình rèm: vải + xích + huy hiệu</summary>
        public override BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts)
        {
            Material medal = !string.IsNullOrEmpty(item.ColorCode) ? board.Colors.CubeMat(item.ColorCode) : null;
            CurtainView view = CurtainView.Build(parent, item, board.CellSize, board.Multiplier, board.Config, board.Visuals, medal, parent.gameObject.layer);
            return new BlockView { visual = view.transform };
        }

        /// <summary>Xử lý khi rèm bị trúng nhưng chưa vỡ: huy hiệu đung đưa, đổi số, tiếng hit huy hiệu</summary>
        public override void OnHit(BoardBlock block)
        {
            CurtainView view = block.GetComponentInChildren<CurtainView>();
            if (view != null) view.Hit(block.Hp);
            AudioManager.Play(Sfx.IceCrack);
        }

        /// <summary>Xử lý khi rèm vỡ: kéo rèm lên, lộ vật bên dưới</summary>
        public override void OnBroken(BoardController board, BoardBlock block)
        {
            CurtainView view = block.GetComponentInChildren<CurtainView>();
            if (view != null) view.Break();
            Vector3 top = block.transform.position + new Vector3(0f, block.Cells.height * board.CellSize * 0.5f, 0f);
            board.BreakFx.SpawnBurst(top, block.ColorCode, block.Cells.width * board.CellSize * 0.8f);
            AudioManager.Play(Sfx.CurtainOpen);
            Vector2Int shift = block.Cells.min - block.OriginCells.min;
            foreach (BoardItemData inner in block.Contents)
            {
                RectInt r = inner.Cells;
                r.position += shift;
                inner.Cells = r;
                BoardBlock revealed = board.Spawn(inner);
                if (revealed != null) revealed.HideLabelFor(CurtainView.LiftDuration);
            }
        }
    }
}
