using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Phần hình của một khối do <see cref="BlockKind.Build"/> dựng ra.</summary>
    public struct BlockView
    {
        public Transform visual;
        public TextMeshPro label;
        /// <summary>null = một BoxCollider2D phủ cả khối.</summary>
        public List<Rect> colliders;
    }

    /// <summary>
    /// MỌI điều riêng của một LOẠI khối trên bàn nằm ở đây: luật (bi nào trừ máu, có tính vào điều kiện thắng không…),
    /// cách dựng hình và cách vỡ. <see cref="BoardBlock"/>, <see cref="BoardController"/>, <see cref="LevelParser"/> chỉ hỏi
    /// lớp này, không tự rẽ nhánh theo <see cref="BoardItemKind"/>.
    ///
    /// Thêm loại khối mới (ví dụ khối bom): xem hướng dẫn trong <c>Docs/kien-truc/them-loai-khoi.md</c> –
    /// thêm giá trị enum, một lớp con của BlockKind, một dòng trong <see cref="BlockKinds"/>, và token trong LevelParser.
    /// Mặc định của lớp gốc = gạch thường (theo màu, có số khi nhiều máu, vỡ ra giọt thạch cùng màu).
    /// </summary>
    public abstract class BlockKind
    {
        public abstract BoardItemKind Kind { get; }

        // ------------------------------------------------------------------ Luật
        /// <summary>Phải phá hết loại này mới thắng.</summary>
        public virtual bool CountsForWin => true;
        /// <summary>Bi màu nào cũng trừ máu, bi KHÔNG bị tiêu hao (nảy tiếp), bi không chủ động ngắm (trừ khi hết khối cùng màu).</summary>
        public virtual bool HitByAnyColor => false;
        /// <summary>Đứng yên mãi, không rơi khi mất chỗ đỡ.</summary>
        public virtual bool IsStatic => false;
        /// <summary>Bi cùng màu trừ máu và ngắm thẳng tới; máu được tính vào ngân sách bi của level.</summary>
        public virtual bool IsColorTarget => false;

        public virtual bool Accepts(BoardBlock block, string ballColor) =>
            HitByAnyColor || (IsColorTarget && string.Equals(ballColor, block.ColorCode, System.StringComparison.OrdinalIgnoreCase));

        public virtual bool ShowsHpLabel(BoardBlock block) => HitByAnyColor || block.MaxHp > 1;

        /// <summary>Độ nảy hình khi bị trúng (tỉ lệ phóng to tối đa).</summary>
        public virtual float HitPunch => 0.04f;

        // ------------------------------------------------------------------ Hình
        /// <summary>Có nhãn số máu khi dựng bằng đường mặc định.</summary>
        public virtual bool HasLabel => true;
        /// <summary>Xiên mesh cho mặt trước phẳng khít lưới (khối nhiều ô / tường). Gạch 1 ô, hòm, cột tự lo hình nên không xiên.</summary>
        public virtual bool ShearMesh => true;
        /// <summary>Kiểu gạch 1 ô: vân sáng/tối ngẫu nhiên, mesh bo tròn, lật model cho mặt trên hướng camera (xem CreateVisual).</summary>
        public virtual bool CubeLook => false;
        public virtual Material Material(GameConfig config, ColorLibrary colors, BoardItemData item) => colors.CubeMat(item.ColorCode);

        /// <param name="parts">các hình chữ nhật con khi khối có hình dạng (chữ L…); null/1 phần = khối chữ nhật</param>
        public virtual BlockView Build(BoardController board, BoardItemData item, Transform parent, Vector2 size, List<RectInt> parts) =>
            board.Visuals.BuildDefaultView(item, parent, size, parts, HasLabel);

        // ------------------------------------------------------------------ Vỡ
        /// <summary>Hiệu ứng / hệ quả khi khối vỡ (khối đã được gỡ khỏi lưới, GameObject sắp bị huỷ).</summary>
        public virtual void OnBroken(BoardController board, BoardBlock block) => board.BreakFx.SpawnJellyBreak(block, perCell: false);
    }
}
