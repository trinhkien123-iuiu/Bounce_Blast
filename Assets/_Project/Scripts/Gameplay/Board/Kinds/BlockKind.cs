using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Phần hình của một khối do Build dựng ra</summary>
    public struct BlockView
    {
        public Transform visual;
        public TextMeshPro label;
        /// <summary>null = một BoxCollider2D phủ cả khối</summary>
        public List<Rect> colliders;
    }

    /// <summary>MỌI điều riêng của một LOẠI khối trên bàn nằm ở đây</summary>
    public abstract class BlockKind
    {
        public abstract BoardItemKind Kind { get; }

        /// <summary>Phải phá hết loại này mới thắng</summary>
        public virtual bool CountsForWin => true;
        /// <summary>Bi màu nào cũng trừ máu, bi KHÔNG bị tiêu hao</summary>
        public virtual bool HitByAnyColor => false;
        /// <summary>Đứng yên mãi, không rơi khi mất chỗ đỡ</summary>
        public virtual bool IsStatic => false;
        /// <summary>Bi cùng màu trừ máu và ngắm thẳng tới</summary>
        public virtual bool IsColorTarget => false;

        /// <summary>Kiểm tra khối nhận bi màu này</summary>
        public virtual bool Accepts(BoardBlock block, string ballColor) =>
            HitByAnyColor || (IsColorTarget && string.Equals(ballColor, block.ColorCode, System.StringComparison.OrdinalIgnoreCase));

        /// <summary>Kiểm tra có hiện số máu</summary>
        public virtual bool ShowsHpLabel(BoardBlock block) => HitByAnyColor || block.MaxHp > 1;

        /// <summary>Độ nảy hình khi bị trúng (tỉ lệ phóng to tối đa)</summary>
        public virtual float HitPunch => 0.04f;

        /// <summary>Có nhãn số máu khi dựng bằng đường mặc định</summary>
        public virtual bool HasLabel => true;
        /// <summary>Xiên mesh cho mặt trước phẳng khít lưới</summary>
        public virtual bool ShearMesh => true;
        /// <summary>Kiểu gạch 1 ô: vân sáng/tối ngẫu nhiên, mesh bo tròn, lật model cho mặt trên hướng camera</summary>
        public virtual bool CubeLook => false;
        /// <summary>Dùng cho chọn vật liệu của khối</summary>
        public virtual Material Material(GameConfig config, ColorLibrary colors, BoardItemData item) => colors.CubeMat(item.ColorCode);

        /// <summary>Xử lý dựng hình khối</summary>
        public virtual BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts) =>
            board.Visuals.BuildDefaultView(item, parent, size, parts, HasLabel);

        /// <summary>Dùng cho dải sáng/tối ngẫu nhiên của từng cube</summary>
        public virtual Vector2 ShadeRange(ColorLibrary.Entry entry) => entry != null ? entry.shadeOffset : Vector2.zero;

        /// <summary>Xử lý khi khối vừa bị trúng nhưng chưa vỡ</summary>
        public virtual void OnHit(BoardBlock block) { }

        /// <summary>Hiệu ứng / hệ quả khi khối vỡ</summary>
        public virtual void OnBroken(BoardController board, BoardBlock block) => board.BreakFx.SpawnJellyBreak(block, perCell: false);
    }
}
