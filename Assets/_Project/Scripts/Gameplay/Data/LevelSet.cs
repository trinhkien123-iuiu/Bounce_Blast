using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Danh mục dữ liệu level của một bộ</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Level Set")]
    public class LevelSet : ScriptableObject
    {
        /// <summary>Dùng cho một cụm 5 level liên tiếp</summary>
        [Serializable]
        public class Chunk
        {
            [Tooltip("Level đầu của chunk (chunk gồm 5 level liên tiếp)")] public int firstLevel;
            public int lastLevel;
            [Tooltip("BoardProps_v3.txt – bảng gạch + hộp đạn")] public TextAsset board;
            [Tooltip("TunnelProps_v3.txt – các ống/tunnel (có thể trống)")] public TextAsset tunnels;
            [Tooltip("TunnelPropsBoard_v3.txt – hàng đợi của tunnel trên bàn /btu (có thể trống)")] public TextAsset boardTunnels;
        }

        [Tooltip("Tên bộ để nhận diện (original, dailychallenge…)")] public string setName = "original";
        public List<Chunk> chunks = new List<Chunk>();

        /// <summary>Tìm chunk chứa level; null nếu không có</summary>
        public Chunk Find(int levelId)
        {
            foreach (Chunk c in chunks)
                if (levelId >= c.firstLevel && levelId <= c.lastLevel) return c;
            return null;
        }

        /// <summary>Level lớn nhất có dữ liệu</summary>
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
