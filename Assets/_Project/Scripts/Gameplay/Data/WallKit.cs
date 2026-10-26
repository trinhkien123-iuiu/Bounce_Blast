using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Bộ khối tường module gốc (ModularPrimitiveAmmoWall</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Wall Kit")]
    public class WallKit : ScriptableObject
    {
        public const int L = 1, TL = 2, T = 4, TR = 8, R = 16, BR = 32, B = 64, BL = 128;
        static readonly string[] Names = { "L", "TL", "T", "TR", "R", "BR", "B", "BL" };

        public string[] keys;
        public Mesh[] meshes;
        readonly Dictionary<int, int> _cache = new Dictionary<int, int>();

        /// <summary>Biến thể khớp nhất với tập lân cận</summary>
        public Mesh Get(int mask) => meshes[Index(mask)];

        int Index(int mask)
        {
            if (_cache.TryGetValue(mask, out int best)) return best;
            int bestCost = int.MaxValue;
            for (int i = 0; i < keys.Length; i++)
            {
                int diff = Mask(keys[i]) ^ mask, cost = 0;
                for (int b = 0; b < 8; b++)
                    if ((diff & (1 << b)) != 0) cost += b % 2 == 0 ? 4 : 1;
                if (cost < bestCost) { bestCost = cost; best = i; }
            }
            return _cache[mask] = best;
        }

        static int Mask(string key)
        {
            int mask = 0;
            foreach (string part in key.Split(new[] { '-' }, System.StringSplitOptions.RemoveEmptyEntries)) mask |= 1 << System.Array.IndexOf(Names, part);
            return mask;
        }
    }
}
