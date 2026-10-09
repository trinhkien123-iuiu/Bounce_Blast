using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Đọc level từ Resources/sheets_levels/&lt;bộ&gt;/sheets_a_b/{BoardProps_v3, TunnelProps_v3}.
    /// Quy ước đã đối chiếu với video game gốc (level 39, Multiplier = 2):
    ///   - Mỗi ô CSV thành Multiplier x Multiplier ô trên lưới bàn.
    ///   - Khối gộp (màu-hp-group) và hòm gỗ (cr-hp-id): máu = hp * Multiplier^2  (y-30 → 120, cr-50 → 200).
    ///   - Hộp đạn: số bi = n * Multiplier^2 (p-10 → 40 bi; 40/240 capacity = 16% như trên thanh Capacity).
    /// </summary>
    public static class LevelParser
    {
        const int CubesStartCol = 5;
        const int AmmoColCount = 8;

        public static LevelModel Load(string levelSet, int levelId)
        {
            int chunkStart = ((levelId - 1) / 5) * 5 + 1;
            string folder = $"sheets_levels/{levelSet}/sheets_{chunkStart}_{chunkStart + 4}";
            TextAsset board = Resources.Load<TextAsset>($"{folder}/BoardProps_v3");
            TextAsset tunnels = Resources.Load<TextAsset>($"{folder}/TunnelProps_v3");
            if (board == null)
            {
                Debug.LogError($"[LevelParser] Không tìm thấy Resources/{folder}/BoardProps_v3");
                return null;
            }
            return Parse(board.text, tunnels != null ? tunnels.text : null, levelId);
        }

        public static LevelModel Parse(string boardCsv, string tunnelCsv, int levelId)
        {
            List<string[]> rows = ReadCsv(boardCsv);
            if (rows.Count < 3) return null;

            int sepCol = Array.IndexOf(rows[0], "$");
            if (sepCol < 0) sepCol = 37;
            int cubeCols = sepCol - CubesStartCol;
            int ammoStart = sepCol + 1;

            if (!FindLevelBlock(rows, levelId, out int start, out int end))
            {
                Debug.LogError($"[LevelParser] Không có level {levelId}");
                return null;
            }

            string[] head = rows[start];
            var level = new LevelModel { Id = levelId };
            string hard = Cell(head, 1).ToUpperInvariant();
            level.Difficulty = hard == "SHARD" || hard == "SUPERHARD" ? LevelDifficulty.SuperHard
                             : hard == "HARD" ? LevelDifficulty.Hard : LevelDifficulty.Normal;
            level.Capacity = int.TryParse(Cell(head, 2), out int cap) ? cap : 300;
            string size = Cell(head, 3);
            if (size.Contains("x"))
            {
                string[] d = size.Split('x');
                int.TryParse(d[0], out level.BoardCols);
                int.TryParse(d[1], out level.BoardRows);
            }
            level.IsFigurative = Cell(head, 4).Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            level.Multiplier = int.TryParse(Cell(head, ammoStart + AmmoColCount), out int m) && m > 0 ? m : 1;

            int lineCount = end - start;
            ParseBoard(level, rows, start, lineCount, cubeCols);
            ParseAmmo(level, rows, start, lineCount, ammoStart, tunnelCsv);
            ComputeTotals(level);
            return level;
        }

        // ------------------------------------------------------------------ Bàn chơi
        static void ParseBoard(LevelModel level, List<string[]> rows, int start, int lineCount, int cubeCols)
        {
            int mult = level.Multiplier;
            int mm = mult * mult;
            var groups = new Dictionary<string, BoardItemData>();
            var groupCells = new Dictionary<BoardItemData, List<Vector2Int>>();
            var crates = new Dictionary<string, BoardItemData>();
            var loose = new List<BoardItemData>();

            for (int line = 0; line < lineCount; line++)
            {
                string[] row = rows[start + line];
                for (int c = 0; c < cubeCols; c++)
                {
                    string cell = Cell(row, CubesStartCol + c);
                    if (cell.Length == 0 || cell == "-") continue;

                    var rect = new RectInt(c * mult, (lineCount - 1 - line) * mult, mult, mult);

                    if (cell.StartsWith("/wl-"))
                    {
                        // Cột nhà (ModularPrimitiveColumn gốc): "/wl-hp-id", các ô cùng id gộp thành một cột, máu = hp·m²
                        string[] w = cell.Substring(1).Split('-');
                        int whp = w.Length > 1 && int.TryParse(w[1], out int wh) ? wh : 25;
                        string key = "wl|" + (w.Length > 2 ? w[2] : "0");
                        if (!groups.TryGetValue(key, out BoardItemData col))
                        {
                            col = new BoardItemData { Kind = BoardItemKind.Column, Hp = whp * mm, Cells = rect };
                            groups[key] = col;
                            groupCells[col] = new List<Vector2Int>();
                            loose.Add(col);
                        }
                        col.Cells = Union(col.Cells, rect);
                        for (int dy = 0; dy < rect.height; dy++)
                            for (int dx = 0; dx < rect.width; dx++)
                                groupCells[col].Add(new Vector2Int(rect.x + dx, rect.y + dy));
                        continue;
                    }
                    if (cell.StartsWith("/"))
                    {
                        // wl / bomb / lck / btu ... : chưa hỗ trợ cơ chế riêng → vật cản tĩnh
                        loose.Add(new BoardItemData { Kind = BoardItemKind.Wall, Cells = rect });
                        continue;
                    }

                    string[] parts = cell.Split('/');
                    string[] t = parts[0].Split('-');
                    string color = t[0];
                    int hp = t.Length > 1 && int.TryParse(t[1], out int h) ? h : 1;
                    string group = t.Length > 2 ? t[2] : null;

                    // Phần sau dấu '/': hòm gỗ che cube (cr-hp-id). st-/cur-: chưa hỗ trợ → bỏ qua lớp phủ.
                    string crateKey = null;
                    int crateHp = 0;
                    for (int p = 1; p < parts.Length; p++)
                    {
                        string[] b = parts[p].Split('-');
                        if (b[0] == "cr")
                        {
                            crateHp = b.Length > 1 && int.TryParse(b[1], out int ch) ? ch : 50;
                            crateKey = b.Length > 2 ? b[2] : "0";
                        }
                    }

                    List<BoardItemData> target = loose;
                    if (crateKey != null)
                    {
                        if (!crates.TryGetValue(crateKey, out BoardItemData crate))
                        {
                            crate = new BoardItemData { Kind = BoardItemKind.Crate, Hp = crateHp * mm, Cells = rect };
                            crates[crateKey] = crate;
                        }
                        crate.Cells = Union(crate.Cells, rect);
                        target = crate.Contents;
                    }

                    if (group != null)
                    {
                        string key = (crateKey ?? "") + "|" + group;
                        if (!groups.TryGetValue(key, out BoardItemData g))
                        {
                            g = new BoardItemData { Kind = BoardItemKind.CombinedCube, ColorCode = color, Hp = hp * mm, Cells = rect };
                            groups[key] = g;
                            groupCells[g] = new List<Vector2Int>();
                            target.Add(g);
                        }
                        g.Cells = Union(g.Cells, rect);
                        for (int dy = 0; dy < rect.height; dy++)
                            for (int dx = 0; dx < rect.width; dx++)
                                groupCells[g].Add(new Vector2Int(rect.x + dx, rect.y + dy));
                    }
                    else if (hp > 1 && mult > 1)
                    {
                        target.Add(new BoardItemData { Kind = BoardItemKind.CombinedCube, ColorCode = color, Hp = hp * mm, Cells = rect });
                    }
                    else
                    {
                        // ô thường → Multiplier^2 cube nhỏ
                        for (int dy = 0; dy < mult; dy++)
                            for (int dx = 0; dx < mult; dx++)
                                target.Add(new BoardItemData
                                {
                                    Kind = BoardItemKind.Cube,
                                    ColorCode = color,
                                    Hp = hp,
                                    Cells = new RectInt(rect.x + dx, rect.y + dy, 1, 1)
                                });
                    }
                }
            }

            // Nhóm gộp không phủ kín hình chữ nhật bao (vd "r-20-1" hình chữ L ở level 12) → lưu mặt nạ ô
            foreach (var kv in groupCells)
            {
                BoardItemData g = kv.Key;
                if (kv.Value.Count >= g.Cells.width * g.Cells.height) continue;
                g.Mask = new List<Vector2Int>();
                foreach (Vector2Int c in kv.Value) g.Mask.Add(c - g.Cells.min);
            }

            level.BoardItems.AddRange(loose);
            level.BoardItems.AddRange(crates.Values);
        }

        // ------------------------------------------------------------------ Khay đạn
        static void ParseAmmo(LevelModel level, List<string[]> rows, int start, int lineCount, int ammoStart, string tunnelCsv)
        {
            int mm = level.Multiplier * level.Multiplier;
            Dictionary<char, List<string>> tunnelLists = ParseTunnelLists(tunnelCsv, level.Id);

            int lastRow = -1;
            for (int line = 0; line < lineCount; line++)
                for (int c = 0; c < AmmoColCount; c++)
                    if (Cell(rows[start + line], ammoStart + c).Length > 0) lastRow = line;

            level.AmmoCols = AmmoColCount;
            level.AmmoRows = lastRow + 1;
            level.AmmoCells = new AmmoCellData[AmmoColCount, Mathf.Max(1, level.AmmoRows)];

            for (int r = 0; r < level.AmmoRows; r++)
            {
                for (int c = 0; c < AmmoColCount; c++)
                {
                    string cell = Cell(rows[start + r], ammoStart + c);
                    var data = new AmmoCellData();
                    level.AmmoCells[c, r] = data;

                    if (cell.Length == 0 || cell.StartsWith("/awl")) { data.Kind = AmmoCellKind.Wall; data.SolidWall = cell.Length > 0; continue; }
                    if (cell == "-") { data.Kind = AmmoCellKind.Empty; continue; }

                    if (cell.StartsWith("/tu-") || cell.StartsWith("tu-"))
                    {
                        string code = cell.Substring(cell.IndexOf("tu-", StringComparison.Ordinal) + 3);
                        char id = code.Length > 0 ? char.ToUpperInvariant(code[0]) : 'A';
                        char dir = code.Length > 1 ? char.ToUpperInvariant(code[1]) : 'D';
                        var tunnel = new AmmoTunnelData { Id = id, Direction = DirFromChar(dir) };
                        if (tunnelLists.TryGetValue(id, out List<string> list))
                            foreach (string entry in list)
                            {
                                AmmoBoxData box = ParseBox(entry, mm);
                                if (box != null) tunnel.Queue.Add(box);
                            }
                        data.Kind = AmmoCellKind.Tunnel;
                        data.Tunnel = tunnel;
                        continue;
                    }

                    AmmoBoxData parsed = ParseBox(cell, mm);
                    data.Kind = parsed != null ? AmmoCellKind.Box : AmmoCellKind.Empty;
                    data.Box = parsed;
                }
            }
        }

        static AmmoBoxData ParseBox(string code, int ballMultiplier)
        {
            string[] parts = code.Split('/');
            string[] t = parts[0].Split('-');
            if (t.Length < 2 || !int.TryParse(t[1], out int n)) return null;

            var box = new AmmoBoxData { ColorCode = t[0], Balls = n * ballMultiplier };
            for (int i = 1; i < parts.Length; i++)
            {
                string[] b = parts[i].Split('-');
                if (b[0] == "ice") box.IceHits = b.Length > 1 && int.TryParse(b[1], out int k) ? k : 1;
                else if (b[0] == "qm") box.Hidden = true;
                else if (b[0] == "door")
                {
                    box.Door = b.Length > 1 && int.TryParse(b[1], out int dv) ? dv : 1;
                    box.DoorClosed = box.Door == 1;
                }
            }
            return box;
        }

        static Vector2Int DirFromChar(char d)
        {
            switch (d)
            {
                case 'L': return new Vector2Int(-1, 0);
                case 'R': return new Vector2Int(1, 0);
                case 'U': return new Vector2Int(0, -1);
                default: return new Vector2Int(0, 1);
            }
        }

        static Dictionary<char, List<string>> ParseTunnelLists(string csv, int levelId)
        {
            var result = new Dictionary<char, List<string>>();
            if (string.IsNullOrEmpty(csv)) return result;
            List<string[]> rows = ReadCsv(csv);
            if (rows.Count < 3 || !FindLevelBlock(rows, levelId, out int start, out int end)) return result;

            string[] headers = rows[1];
            for (int c = 1; c < headers.Length; c++)
            {
                if (headers[c].Length == 0) continue;
                var list = new List<string>();
                for (int r = start; r < end; r++)
                {
                    string v = Cell(rows[r], c);
                    if (v.Length > 0 && v != "-") list.Add(v);
                }
                if (list.Count > 0) result[char.ToUpperInvariant(headers[c][0])] = list;
            }
            return result;
        }

        // ------------------------------------------------------------------ Tiện ích
        static void ComputeTotals(LevelModel level)
        {
            foreach (BoardItemData item in level.BoardItems) AddHp(level, item);
            for (int r = 0; r < level.AmmoRows; r++)
                for (int c = 0; c < level.AmmoCols; c++)
                {
                    AmmoCellData cell = level.AmmoCells[c, r];
                    if (cell == null) continue;
                    if (cell.Box != null) AddBalls(level, cell.Box);
                    if (cell.Tunnel != null) foreach (AmmoBoxData b in cell.Tunnel.Queue) AddBalls(level, b);
                }
        }

        static void AddHp(LevelModel level, BoardItemData item)
        {
            if (item.Kind == BoardItemKind.Cube || item.Kind == BoardItemKind.CombinedCube)
            {
                level.HpByColor.TryGetValue(item.ColorCode, out int v);
                level.HpByColor[item.ColorCode] = v + item.Hp;
            }
            foreach (BoardItemData c in item.Contents) AddHp(level, c);
        }

        static void AddBalls(LevelModel level, AmmoBoxData box)
        {
            level.TotalBalls += box.Balls;
            level.BallsByColor.TryGetValue(box.ColorCode, out int v);
            level.BallsByColor[box.ColorCode] = v + box.Balls;
        }

        static RectInt Union(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin), yMin = Mathf.Min(a.yMin, b.yMin);
            int xMax = Mathf.Max(a.xMax, b.xMax), yMax = Mathf.Max(a.yMax, b.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        static bool FindLevelBlock(List<string[]> rows, int levelId, out int start, out int end)
        {
            start = -1;
            end = rows.Count;
            for (int i = 2; i < rows.Count; i++)
            {
                if (!int.TryParse(Cell(rows[i], 0), out int id)) continue;
                if (id == levelId) start = i;
                else if (start >= 0) { end = i; break; }
            }
            return start >= 0;
        }

        static List<string[]> ReadCsv(string text)
        {
            var list = new List<string[]>();
            foreach (string line in text.Split('\n'))
            {
                string l = line.TrimEnd('\r');
                if (l.Length > 0) list.Add(l.Split(','));
            }
            return list;
        }

        static string Cell(string[] row, int index) => index < row.Length ? row[index].Trim() : "";
    }
}
