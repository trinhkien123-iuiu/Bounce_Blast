using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Khối lớn nhiều ô dùng chung máu</summary>
    public class CombinedCubeKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.CombinedCube;
        public override bool IsColorTarget => true;
        /// <summary>Kiểm tra có hiện số máu (khối gộp luôn hiện)</summary>
        public override bool ShowsHpLabel(BoardBlock block) => true;

        /// <summary>Xử lý dựng hình khối gộp</summary>
        public override BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts)
        {
            if (parts != null && parts.Count > 1) return base.Build(board, item, parent, size, parts);
            RectInt r = item.Cells;
            var full = new HashSet<Vector2Int>();
            for (int x = 0; x < r.width; x++) for (int y = 0; y < r.height; y++) full.Add(new Vector2Int(x, y));
            return new BlockView
            {
                visual = board.Visuals.CreateSolidVisual(parent, item, r, full),
                label = board.Visuals.CreateLabel(parent, item, size),
            };
        }

        /// <summary>Xử lý hiệu ứng khi khối gộp vỡ</summary>
        public override void OnBroken(BoardController board, BoardBlock block) => board.BreakFx.SpawnJellyBreak(block, perCell: true);
    }
}
