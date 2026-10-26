namespace BounceBlast.Gameplay
{
    /// <summary>Gạch 1 ô có màu: chỉ bi cùng màu trừ máu</summary>
    public class CubeKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.Cube;
        public override bool IsColorTarget => true;
        public override float HitPunch => 0.18f;
        public override bool ShearMesh => false;
        public override bool CubeLook => true;
    }
}
