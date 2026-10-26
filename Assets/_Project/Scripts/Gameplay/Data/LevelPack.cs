using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Bộ level tự làm: danh sách LevelData theo thứ tự chơi</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Level Pack", fileName = "LevelPack_custom")]
    public class LevelPack : ScriptableObject
    {
        public string packName = "custom";
        public List<LevelData> levels = new List<LevelData>();

        public int Count => levels.Count;

        /// <summary>Level số levelId (bắt đầu từ 1)</summary>
        public LevelData Get(int levelId) => levelId >= 1 && levelId <= levels.Count ? levels[levelId - 1] : null;

        /// <summary>Dùng cho lấy số thứ tự level trong bộ</summary>
        public int IndexOf(LevelData data) => levels.IndexOf(data) + 1;
    }

    /// <summary>Bộ level đang chơi</summary>
    public enum LevelSource { Original, Custom }
}
