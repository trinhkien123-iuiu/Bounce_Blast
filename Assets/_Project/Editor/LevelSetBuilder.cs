using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BounceBlast.Gameplay;
using UnityEditor;
using UnityEngine;

namespace BounceBlast.EditorTools
{
    /// <summary>Dựng/cập nhật các asset LevelSet từ thư mục Assets/_Project/Data/Levels/&amp;lt;bộ&amp;gt;/sheets_a_b/</summary>
    public static class LevelSetBuilder
    {
        const string LevelsDir = "Assets/_Project/Data/Levels";
        static readonly Regex ChunkName = new Regex(@"^sheets_(\d+)_(\d+)$");

        /// <summary>Xử lý dựng danh mục bộ level</summary>
        [MenuItem("Tools/Bounce Blast/Data/Build Level Sets")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(LevelsDir)) { Debug.LogError("[LevelSetBuilder] Không có thư mục " + LevelsDir); return; }
            int sets = 0, chunks = 0;
            foreach (string setDir in AssetDatabase.GetSubFolders(LevelsDir))
            {
                string setName = Path.GetFileName(setDir);
                if (!AssetDatabase.GetSubFolders(setDir).Any(d => ChunkName.IsMatch(Path.GetFileName(d)))) continue;
                string assetPath = $"{LevelsDir}/LevelSet_{setName}.asset";
                var set = AssetDatabase.LoadAssetAtPath<LevelSet>(assetPath);
                if (set == null) { set = ScriptableObject.CreateInstance<LevelSet>(); AssetDatabase.CreateAsset(set, assetPath); }
                set.setName = setName;
                set.chunks.Clear();
                foreach (string chunkDir in AssetDatabase.GetSubFolders(setDir))
                {
                    Match m = ChunkName.Match(Path.GetFileName(chunkDir));
                    if (!m.Success) continue;
                    set.chunks.Add(new LevelSet.Chunk
                    {
                        firstLevel = int.Parse(m.Groups[1].Value),
                        lastLevel = int.Parse(m.Groups[2].Value),
                        board = AssetDatabase.LoadAssetAtPath<TextAsset>(chunkDir + "/BoardProps_v3.txt"),
                        tunnels = AssetDatabase.LoadAssetAtPath<TextAsset>(chunkDir + "/TunnelProps_v3.txt"),
                    });
                    chunks++;
                }
                set.chunks = set.chunks.OrderBy(c => c.firstLevel).ToList();
                EditorUtility.SetDirty(set);
                sets++;
                Debug.Log($"[LevelSetBuilder] {assetPath}: {set.chunks.Count} chunk, level lớn nhất {set.MaxLevel}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[LevelSetBuilder] Xong: {sets} bộ, {chunks} chunk.");
        }
    }
}
