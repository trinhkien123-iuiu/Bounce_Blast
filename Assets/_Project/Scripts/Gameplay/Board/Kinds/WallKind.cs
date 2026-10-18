using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Vật cản tĩnh không phá được (cũng là chỗ rơi về của các token chưa hỗ trợ): không rơi, không số, không tính vào thắng.</summary>
    public class WallKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.Wall;
        public override bool CountsForWin => false;
        public override bool IsStatic => true;
        public override bool HasLabel => false;
        public override Material Material(GameConfig config, ColorLibrary colors, BoardItemData item) => config.wallMaterial;
    }
}
