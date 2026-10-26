using System.Collections.Generic;
using System.Linq;
using System.Text;
using BounceBlast.Gameplay;
using UnityEditor;
using UnityEngine;

namespace BounceBlast.EditorTools
{
    /// <summary>Kiểm tra khứ hồi cho TOÀN BỘ level gốc</summary>
    public static class LevelRoundTripCheck
    {
        /// <summary>Xử lý kiểm level ghi ra rồi đọc lại còn đúng</summary>
        [MenuItem("Tools/Bounce Blast/Level Editor/Kiểm tra khứ hồi 550 level gốc")]
        public static void Run()
        {
            GameConfig config = LevelEditorUtil.FindConfig();
            if (config == null || config.levels == null) { Debug.LogError("[RoundTrip] Không tìm thấy GameConfig.levels"); return; }
            int ok = 0, fail = 0, skipped = 0;
            var sb = new StringBuilder();
            var data = ScriptableObject.CreateInstance<LevelData>();
            for (int id = 1; id <= config.levels.MaxLevel; id++)
            {
                LevelSet.Chunk chunk = config.levels.Find(id);
                if (chunk == null || chunk.board == null) { skipped++; continue; }
                LevelTokens tokens = LevelParser.ReadTokens(chunk.board.text, chunk.tunnels != null ? chunk.tunnels.text : null, id);
                if (tokens == null) { skipped++; continue; }
                LevelModel direct = LevelParser.Build(tokens);
                data.FromTokens(tokens);
                int outside = LevelData.CountTokensOutside(tokens, data.boardCols);
                LevelModel viaData = LevelParser.Build(data.ToTokens(id));
                string a = Signature(direct), b = Signature(viaData);
                if (a == b && outside == 0) ok++;
                else
                {
                    fail++;
                    if (fail <= 10) sb.Append("L").Append(id).Append(outside > 0 ? $" (bỏ {outside} ô ngoài bề ngang)" : "").Append(": ").Append(FirstDiff(a, b)).Append('\n');
                }
            }
            Object.DestroyImmediate(data);
            string msg = $"[RoundTrip] {ok} khớp, {fail} lệch, {skipped} bỏ qua";
            if (fail == 0) Debug.Log(msg);
            else Debug.LogError(msg + "\n" + sb);
        }

        /// <summary>Chuỗi mô tả đầy đủ một LevelModel</summary>
        public static string Signature(LevelModel m)
        {
            var sb = new StringBuilder();
            sb.Append($"diff={m.Difficulty} cap={m.Capacity} board={m.BoardCols}x{m.BoardRows} mult={m.Multiplier} fig={m.IsFigurative}\n");
            foreach (string s in m.BoardItems.Select(Item).OrderBy(x => x, System.StringComparer.Ordinal)) sb.Append(s).Append('\n');
            sb.Append($"ammo={m.AmmoCols}x{m.AmmoRows}\n");
            for (int r = 0; r < m.AmmoRows; r++)
                for (int c = 0; c < m.AmmoCols; c++)
                {
                    AmmoCellData d = m.AmmoCells[c, r];
                    if (d == null) { sb.Append($"[{c},{r}]null\n"); continue; }
                    sb.Append($"[{c},{r}]{d.Kind} solid={d.SolidWall} {BoxSig(d.Box)}");
                    if (d.Tunnel != null)
                    {
                        sb.Append($" tu={d.Tunnel.Id} dir={d.Tunnel.Direction} q=");
                        foreach (AmmoBoxData b in d.Tunnel.Queue) sb.Append(BoxSig(b)).Append(';');
                    }
                    sb.Append('\n');
                }
            sb.Append($"balls={m.TotalBalls}\n");
            foreach (var kv in m.HpByColor.OrderBy(k => k.Key, System.StringComparer.Ordinal)) sb.Append($"hp {kv.Key}={kv.Value}\n");
            foreach (var kv in m.BallsByColor.OrderBy(k => k.Key, System.StringComparer.Ordinal)) sb.Append($"bi {kv.Key}={kv.Value}\n");
            return sb.ToString();
        }

        static string Item(BoardItemData i)
        {
            var sb = new StringBuilder($"{i.Kind} {i.ColorCode} hp={i.Hp} {i.Cells}");
            if (i.Mask != null) sb.Append(" mask=").Append(string.Join(";", i.Mask.Select(v => v.x + "," + v.y).OrderBy(x => x, System.StringComparer.Ordinal)));
            if (i.Contents.Count > 0) sb.Append(" {").Append(string.Join("|", i.Contents.Select(Item).OrderBy(x => x, System.StringComparer.Ordinal))).Append('}');
            return sb.ToString();
        }

        static string BoxSig(AmmoBoxData b) => b == null ? "-" : $"{b.ColorCode}:{b.Balls}:ice{b.IceHits}:qm{b.Hidden}:door{b.Door}{b.DoorClosed}";

        static string FirstDiff(string a, string b)
        {
            string[] la = a.Split('\n'), lb = b.Split('\n');
            for (int i = 0; i < Mathf.Max(la.Length, lb.Length); i++)
            {
                string x = i < la.Length ? la[i] : "<hết>", y = i < lb.Length ? lb[i] : "<hết>";
                if (x != y) return $"dòng {i}: CSV «{x}» ≠ LevelData «{y}»";
            }
            return "khác (không rõ dòng)";
        }
    }

    /// <summary>Tiện ích dùng chung cho các công cụ Level Editor</summary>
    public static class LevelEditorUtil
    {
        public const string CustomDir = "Assets/_Project/Data/Levels/custom";
        public const string PackPath = CustomDir + "/LevelPack_custom.asset";

        /// <summary>Dùng cho tìm GameConfig trong project</summary>
        public static GameConfig FindConfig()
        {
            string guid = AssetDatabase.FindAssets("t:GameConfig").FirstOrDefault();
            return guid != null ? AssetDatabase.LoadAssetAtPath<GameConfig>(AssetDatabase.GUIDToAssetPath(guid)) : null;
        }

        /// <summary>Dùng cho tìm ColorLibrary trong project</summary>
        public static ColorLibrary FindColors()
        {
            string guid = AssetDatabase.FindAssets("t:ColorLibrary").FirstOrDefault();
            return guid != null ? AssetDatabase.LoadAssetAtPath<ColorLibrary>(AssetDatabase.GUIDToAssetPath(guid)) : null;
        }

        /// <summary>Bộ level custom (tạo nếu chưa có) và gán vào GameConfig.customLevels</summary>
        public static LevelPack EnsurePack()
        {
            if (!AssetDatabase.IsValidFolder(CustomDir)) AssetDatabase.CreateFolder("Assets/_Project/Data/Levels", "custom");
            var pack = AssetDatabase.LoadAssetAtPath<LevelPack>(PackPath);
            if (pack == null)
            {
                pack = ScriptableObject.CreateInstance<LevelPack>();
                AssetDatabase.CreateAsset(pack, PackPath);
            }
            GameConfig config = FindConfig();
            if (config != null && config.customLevels == null)
            {
                config.customLevels = pack;
                EditorUtility.SetDirty(config);
            }
            return pack;
        }
    }
}
