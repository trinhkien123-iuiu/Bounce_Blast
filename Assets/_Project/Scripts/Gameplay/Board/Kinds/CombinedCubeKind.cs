using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Khối lớn nhiều ô dùng chung máu (token màu-hp-groupId), có thể hình chữ L/T: như gạch theo màu, luôn hiện số,
    /// dựng thành một mesh liền theo viền, vỡ thì nổ mỗi ô một cụm như gạch 1 ô.
    /// </summary>
    public class CombinedCubeKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.CombinedCube;
        public override bool IsColorTarget => true;
        public override bool ShowsHpLabel(BoardBlock block) => true;

        public override BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts)
        {
            if (parts != null && parts.Count > 1) return base.Build(board, item, parent, size, parts);   // hình chữ L…
            RectInt r = item.Cells;
            var full = new HashSet<Vector2Int>();
            for (int x = 0; x < r.width; x++) for (int y = 0; y < r.height; y++) full.Add(new Vector2Int(x, y));
            return new BlockView
            {
                visual = board.Visuals.CreateSolidVisual(parent, item, r, full),
                label = board.Visuals.CreateLabel(parent, item, size),
            };
        }

        public override void OnBroken(BoardController board, BoardBlock block) => board.BreakFx.SpawnJellyBreak(block, perCell: true);
    }
}
