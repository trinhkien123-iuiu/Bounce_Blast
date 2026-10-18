using System;

// Chỉ biên dịch ở Editor, Development Build, hoặc khi có Scripting Define Symbol BB_CHEATS (xem Docs/design/cheat-tool.md).
#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS
namespace BounceBlast.Cheats
{
    public enum CheatKind { Button, Toggle, Stepper }

    /// <summary>
    /// Một dòng trong panel cheat. Chỉ là DỮ LIỆU (tên + hàm gọi) – không biết gì về UI.
    /// Thêm cheat mới = thêm một <see cref="CheatItem"/> trong <see cref="CheatRegistry"/>.
    /// </summary>
    public class CheatItem
    {
        public string group;
        public string label;
        public CheatKind kind;

        // Button
        public Action onClick;

        // Toggle
        public Func<bool> get;
        public Action<bool> set;

        // Stepper (chọn một số rồi bấm Go)
        public Func<int> initial;
        public int min = 1, max = 999;
        public Action<int> go;
        public string goLabel = "GO";

        /// <summary>Nhãn động (VD nút "tap again to confirm"); null = dùng <see cref="label"/>.</summary>
        public Func<string> dynamicLabel;

        public string Label => dynamicLabel != null ? dynamicLabel() : label;
    }
}
#endif
