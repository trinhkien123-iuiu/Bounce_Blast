using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Bộ level tự làm: danh sách <see cref="LevelData"/> theo thứ tự chơi (level 1 = phần tử đầu). Sửa thứ tự trong Level Editor.</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Level Pack", fileName = "LevelPack_custom")]
    public class LevelPack : ScriptableObject
    {
        public string packName = "custom";
        public List<LevelData> levels = new List<LevelData>();

        public int Count => levels.Count;

        /// <summary>Level số <paramref name="levelId"/> (bắt đầu từ 1); null nếu không có.</summary>
        public LevelData Get(int levelId) => levelId >= 1 && levelId <= levels.Count ? levels[levelId - 1] : null;

        public int IndexOf(LevelData data) => levels.IndexOf(data) + 1;
    }

    /// <summary>Bộ level đang chơi.</summary>
    public enum LevelSource { Original, Custom }
}
