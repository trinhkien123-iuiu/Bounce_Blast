namespace BounceBlast.Gameplay
{
    /// <summary>Gạch 1 ô có màu: chỉ bi cùng màu trừ máu (bi bị tiêu hao), nảy mạnh khi trúng, vỡ ra giọt thạch cùng màu.</summary>
    public class CubeKind : BlockKind
    {
        public override BoardItemKind Kind => BoardItemKind.Cube;
        public override bool IsColorTarget => true;
        public override float HitPunch => 0.18f;
        public override bool ShearMesh => false;   // gạch 1 ô dùng mesh bo tròn riêng, không xiên
        public override bool CubeLook => true;
    }
}
