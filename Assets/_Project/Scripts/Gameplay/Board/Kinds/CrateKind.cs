using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hòm gỗ: bi màu nào cũng trừ máu và nảy tiếp</summary>
    public class CrateKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.Crate;
        public override bool HitByAnyColor => true;
        public override bool ShearMesh => false;
        /// <summary>Dùng cho chọn vật liệu hòm gỗ</summary>
        public override Material Material(GameConfig config, ColorLibrary colors, BoardItemData item) => config.crateMaterial;

        /// <summary>Xử lý dựng hình hòm gỗ</summary>
        public override BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts)
        {
            GameConfig cfg = board.Config;
            return new BlockView
            {
                visual = CrateView.Build(parent, item, board.CellSize, cfg.crateModularPrefab, cfg.cratePrefab, parent.gameObject.layer).transform,
                label = board.Visuals.CreateLabel(parent, item, size),
            };
        }

        /// <summary>Xử lý khi hòm vỡ: lộ vật bên trong</summary>
        public override void OnBroken(BoardController board, BoardBlock block)
        {
            CrateView crate = block.GetComponentInChildren<CrateView>();
            if (crate != null) crate.Break();
            Vfx.Spawn(board.Config.crateBreakVfx, block.transform.position, new Color(0.85f, 0.6f, 0.35f),
                      Mathf.Max(block.Cells.width, block.Cells.height) * board.CellSize);
            AudioManager.Play(Sfx.CrateBreak);
            Vector2Int shift = block.Cells.min - block.OriginCells.min;
            foreach (BoardItemData inner in block.Contents)
            {
                RectInt r = inner.Cells;
                r.position += shift;
                inner.Cells = r;
                board.Spawn(inner);
            }
        }
    }
}
