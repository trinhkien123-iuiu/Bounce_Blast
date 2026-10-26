using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Vật cản tĩnh không phá được</summary>
    public class WallKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.Wall;
        public override bool CountsForWin => false;
        public override bool IsStatic => true;
        public override bool HasLabel => false;
        /// <summary>Dùng cho chọn vật liệu vật cản tĩnh</summary>
        public override Material Material(GameConfig config, ColorLibrary colors, BoardItemData item) => config.wallMaterial;
    }
}
