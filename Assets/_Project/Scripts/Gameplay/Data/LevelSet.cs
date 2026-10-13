using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Danh mục dữ liệu level của một bộ (original, dailychallenge…): mỗi "chunk" là 5 level liền nhau (sheets_a_b) kèm hai file CSV
    /// BoardProps_v3 và TunnelProps_v3. Thay cho việc tải bằng chuỗi `Resources.Load("sheets_levels/...")`: tham chiếu TextAsset
    /// nằm thẳng trong asset nên đổi tên / đổi chỗ file CSV không làm hỏng âm thầm, và tìm dữ liệu level chỉ cần mở asset này.
    ///
    /// Tạo/cập nhật tự động từ thư mục Data/Levels/&lt;bộ&gt;/ bằng menu Tools/Bounce Blast/Data/Build Level Sets.
    /// </summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Level Set")]
    public class LevelSet : ScriptableObject
    {
        [Serializable]
        public class Chunk
        {
            [Tooltip("Level đầu của chunk (chunk gồm 5 level liên tiếp)")] public int firstLevel;
            public int lastLevel;
            [Tooltip("BoardProps_v3.txt – bảng gạch + hộp đạn")] public TextAsset board;
            [Tooltip("TunnelProps_v3.txt – các ống/tunnel (có thể trống)")] public TextAsset tunnels;
        }

        [Tooltip("Tên bộ để nhận diện (original, dailychallenge…)")] public string setName = "original";
        public List<Chunk> chunks = new List<Chunk>();

        /// <summary>Tìm chunk chứa level; null nếu không có.</summary>
        public Chunk Find(int levelId)
        {
            foreach (Chunk c in chunks)
                if (levelId >= c.firstLevel && levelId <= c.lastLevel) return c;
            return null;
        }

        /// <summary>Level lớn nhất có dữ liệu (0 nếu trống).</summary>
        public int MaxLevel
        {
            get
            {
                int m = 0;
                foreach (Chunk c in chunks) m = Mathf.Max(m, c.lastLevel);
                return m;
            }
        }
    }
}
