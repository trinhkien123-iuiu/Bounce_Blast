using System;

#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS
namespace BounceBlast.Cheats
{
    /// <summary>Dùng cho phân loại một mục cheat: nút, công tắc hoặc bộ tăng giảm</summary>
    public enum CheatKind { Button, Toggle, Stepper }

    /// <summary>Một dòng trong panel cheat</summary>
    public class CheatItem
    {
        public string group;
        public string label;
        public CheatKind kind;

        public Action onClick;

        public Func<bool> get;
        public Action<bool> set;

        public Func<int> initial;
        public int min = 1, max = 999;
        public Action<int> go;
        public string goLabel = "GO";

        /// <summary>Nhãn động (VD nút "tap again to confirm")</summary>
        public Func<string> dynamicLabel;

        public string Label => dynamicLabel != null ? dynamicLabel() : label;
    }
}
#endif
