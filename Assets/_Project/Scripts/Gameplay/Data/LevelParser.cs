using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Đọc level từ LevelSet (danh mục tham chiếu tới BoardProps_v3 / TunnelProps_v3 của từng chunk 5 level)</summary>
    public static class LevelParser
    {
        /// <summary>Số lần bi chạm bom để gỡ nó: 1 (user chốt: "chạm được bi vào bomb để huỷ bomb")</summary>
        public const int BombHits = 1;
        const int CubesStartCol = 5;
        const int AmmoColCount = 8;

        /// <summary>Nạp level theo bộ đang chọn trong GameConfig</summary>
        public static LevelModel Load(GameConfig config, int levelId)
        {
            if (config != null && config.levelSource == LevelSource.Custom)
            {
                LevelData data = config.customLevels != null ? config.customLevels.Get(levelId) : null;
                if (data == null) { Debug.LogError($"[LevelParser] Bộ custom không có level {levelId}"); return null; }
                return Build(data.ToTokens(levelId));
            }
            return Load(config != null ? config.levels : null, levelId);
        }

        /// <summary>Xử lý nạp level theo số từ bộ level</summary>
        public static LevelModel Load(LevelSet levelSet, int levelId)
        {
            if (levelSet == null)
            {
                Debug.LogError("[LevelParser] GameConfig.levels chưa gán LevelSet");
                return null;
            }
            LevelSet.Chunk chunk = levelSet.Find(levelId);
            if (chunk == null || chunk.board == null)
            {
                Debug.LogError($"[LevelParser] LevelSet '{levelSet.name}' không có dữ liệu cho level {levelId}");
                return null;
            }
            return Parse(chunk.board.text, chunk.tunnels != null ? chunk.tunnels.text : null, levelId,
                         chunk.boardTunnels != null ? chunk.boardTunnels.text : null);
        }

        /// <summary>Xử lý đọc CSV thành dữ liệu level</summary>
        public static LevelModel Parse(string boardCsv, string tunnelCsv, int levelId, string boardTunnelCsv = null) =>
            Build(ReadTokens(boardCsv, tunnelCsv, levelId, boardTunnelCsv));

        /// <summary>CSV gốc (BoardProps_v3 + TunnelProps_v3 + TunnelPropsBoard_v3) → lưới mã của MỘT level</summary>
        public static LevelTokens ReadTokens(string boardCsv, string tunnelCsv, int levelId, string boardTunnelCsv = null)
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
            var t = new LevelTokens { Id = levelId };
            t.HardLevel = Cell(head, 1);
            t.Capacity = int.TryParse(Cell(head, 2), out int cap) ? cap : 300;
            string size = Cell(head, 3);
            if (size.Contains("x"))
            {
                string[] d = size.Split('x');
                int.TryParse(d[0], out t.BoardCols);
                int.TryParse(d[1], out t.BoardRows);
            }
            t.IsFigurative = Cell(head, 4).Equals("TRUE", StringComparison.OrdinalIgnoreCase);
            t.Multiplier = int.TryParse(Cell(head, ammoStart + AmmoColCount), out int m) && m > 0 ? m : 1;

            int lineCount = end - start;
            t.Board = new string[cubeCols, lineCount];
            for (int line = 0; line < lineCount; line++)
                for (int c = 0; c < cubeCols; c++)
                    t.Board[c, line] = Cell(rows[start + line], CubesStartCol + c);

            int lastRow = -1;
            for (int line = 0; line < lineCount; line++)
                for (int c = 0; c < AmmoColCount; c++)
                    if (Cell(rows[start + line], ammoStart + c).Length > 0) lastRow = line;
            t.Ammo = new string[AmmoColCount, lastRow + 1];
            for (int r = 0; r <= lastRow; r++)
                for (int c = 0; c < AmmoColCount; c++)
                    t.Ammo[c, r] = Cell(rows[start + r], ammoStart + c);

            t.Tunnels = ParseTunnelLists(tunnelCsv, levelId);
            t.BoardTunnels = ParseTunnelLists(boardTunnelCsv, levelId);
            return t;
        }

        /// <summary>Lưới mã → LevelModel (dùng chung cho level gốc và level tự làm)</summary>
        public static LevelModel Build(LevelTokens t)
        {
            if (t == null) return null;
            var level = new LevelModel { Id = t.Id };
            string hard = (t.HardLevel ?? "").Trim().ToUpperInvariant();
            level.Difficulty = hard == "SHARD" || hard == "SUPERHARD" ? LevelDifficulty.SuperHard
                             : hard == "HARD" ? LevelDifficulty.Hard : LevelDifficulty.Normal;
            level.Capacity = t.Capacity;
            level.BoardCols = t.BoardCols;
            level.BoardRows = t.BoardRows;
            level.IsFigurative = t.IsFigurative;
            level.Multiplier = t.Multiplier > 0 ? t.Multiplier : 1;

            ParseBoard(level, t);
            ParseAmmo(level, t);
            ComputeTotals(level);
            return level;
        }

        static void ParseBoard(LevelModel level, LevelTokens t)
        {
            int lineCount = t.BoardLines, cubeCols = t.BoardDesignCols;
            int mult = level.Multiplier;
            int mm = mult * mult;
            var groups = new Dictionary<string, BoardItemData>();
            var groupCells = new Dictionary<BoardItemData, List<Vector2Int>>();
            var crates = new Dictionary<string, BoardItemData>();
            var loose = new List<BoardItemData>();

            for (int line = 0; line < lineCount; line++)
            {
                for (int c = 0; c < cubeCols; c++)
                {
                    string cell = (t.Board[c, line] ?? "").Trim();
                    if (cell.Length == 0 || cell == "-") continue;

                    var rect = new RectInt(c * mult, (lineCount - 1 - line) * mult, mult, mult);

                    if (cell.StartsWith("/btu-"))
                    {
                        // "/btu-X": miệng tunnel trên bàn; hàng đợi lấy từ TunnelPropsBoard (mỗi "màu-N" = N×Multiplier² cube 1 máu)
                        string[] ub = cell.Substring(1).Split('-');
                        char tid = ub.Length > 1 && ub[1].Length > 0 ? char.ToUpperInvariant(ub[1][0]) : 'A';
                        string ukey = "btu|" + tid;
                        if (!groups.TryGetValue(ukey, out BoardItemData tun))
                        {
                            tun = new BoardItemData { Kind = BoardItemKind.BoardTunnel, TunnelId = tid, Hp = 1, Cells = rect };
                            if (t.BoardTunnels != null && t.BoardTunnels.TryGetValue(tid, out List<string> entries))
                                foreach (string entry in entries)
                                {
                                    string[] eb = entry.Split('-');
                                    if (eb.Length < 2 || !int.TryParse(eb[1], out int en) || en <= 0) continue;
                                    tun.Queue.Add(new TunnelBatch { Color = eb[0], Cubes = en * mm });
                                }
                            groups[ukey] = tun;
                            groupCells[tun] = new List<Vector2Int>();
                            loose.Add(tun);
                        }
                        tun.Cells = Union(tun.Cells, rect);
                        for (int dy = 0; dy < rect.height; dy++)
                            for (int dx = 0; dx < rect.width; dx++)
                                groupCells[tun].Add(new Vector2Int(rect.x + dx, rect.y + dy));
                        continue;
                    }
                    if (cell.StartsWith("/bomb-"))
                    {
                        // "/bomb-timer-id": các ô cùng id là một quả bom; bi trúng bom (số hit = BombHits) thì gỡ bom và nổ vùng quanh nó
                        string[] bb = cell.Substring(1).Split('-');
                        int timer = bb.Length > 1 && int.TryParse(bb[1], out int bt) ? bt : 90;
                        string bkey = "bomb|" + (bb.Length > 2 ? bb[2] : "0");
                        if (!groups.TryGetValue(bkey, out BoardItemData bomb))
                        {
                            bomb = new BoardItemData { Kind = BoardItemKind.Bomb, Hp = BombHits, Timer = timer, Cells = rect };
                            groups[bkey] = bomb;
                            groupCells[bomb] = new List<Vector2Int>();
                            loose.Add(bomb);
                        }
                        bomb.Cells = Union(bomb.Cells, rect);
                        for (int dy = 0; dy < rect.height; dy++)
                            for (int dx = 0; dx < rect.width; dx++)
                                groupCells[bomb].Add(new Vector2Int(rect.x + dx, rect.y + dy));
                        continue;
                    }
                    if (cell.StartsWith("/wl-"))
                    {
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
                        loose.Add(new BoardItemData { Kind = BoardItemKind.Wall, Cells = rect });
                        continue;
                    }

                    string[] parts = cell.Split('/');
                    string[] tk = parts[0].Split('-');
                    string color = tk[0];
                    int hp = tk.Length > 1 && int.TryParse(tk[1], out int h) ? h : 1;
                    string group = tk.Length > 2 ? tk[2] : null;

                    string crateKey = null, curtainColor = null;
                    int crateHp = 0, stoneHp = 0;
                    var coverKind = BoardItemKind.Crate;
                    bool hidden = false;
                    for (int p = 1; p < parts.Length; p++)
                    {
                        string[] b = parts[p].Split('-');
                        if (b[0] == "bqm") hidden = true;
                        if (b[0] == "st") stoneHp = b.Length > 1 && int.TryParse(b[1], out int sh) ? sh : 3;
                        if (b[0] == "cr")
                        {
                            crateHp = b.Length > 1 && int.TryParse(b[1], out int ch) ? ch : 50;
                            crateKey = b.Length > 2 ? b[2] : "0";
                        }
                        if (b[0] == "cur")
                        {
                            // "cur-hp-id-màu": rèm phủ các ô cùng id, màu chỉ là màu huy hiệu (máu rèm không tính vào bi)
                            coverKind = BoardItemKind.Curtain;
                            crateHp = b.Length > 1 && int.TryParse(b[1], out int rh) ? rh : 10;
                            crateKey = "cur|" + (b.Length > 2 ? b[2] : "0");
                            curtainColor = b.Length > 3 ? b[3] : null;
                        }
                    }

                    if (stoneHp > 0)
                    {
                        for (int dy = 0; dy < mult; dy++)
                            for (int dx = 0; dx < mult; dx++)
                            {
                                var one = new RectInt(rect.x + dx, rect.y + dy, 1, 1);
                                var stone = new BoardItemData { Kind = BoardItemKind.Stone, Hp = stoneHp, Cells = one };
                                stone.Contents.Add(new BoardItemData { Kind = BoardItemKind.Cube, ColorCode = color, Hp = hp, Cells = one });
                                loose.Add(stone);
                            }
                        continue;
                    }

                    List<BoardItemData> target = loose;
                    if (crateKey != null)
                    {
                        if (!crates.TryGetValue(crateKey, out BoardItemData crate))
                        {
                            crate = new BoardItemData { Kind = coverKind, ColorCode = curtainColor, Hp = crateHp * mm, Cells = rect };
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
                            g = new BoardItemData { Kind = BoardItemKind.CombinedCube, ColorCode = color, Hp = hp * mm, Cells = rect, Hidden = hidden };
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
                        target.Add(new BoardItemData { Kind = BoardItemKind.CombinedCube, ColorCode = color, Hp = hp * mm, Cells = rect, Hidden = hidden });
                    }
                    else
                    {
                        for (int dy = 0; dy < mult; dy++)
                            for (int dx = 0; dx < mult; dx++)
                                target.Add(new BoardItemData
                                {
                                    Kind = BoardItemKind.Cube,
                                    ColorCode = color,
                                    Hp = hp,
                                    Cells = new RectInt(rect.x + dx, rect.y + dy, 1, 1),
                                    Hidden = hidden
                                });
                    }
                }
            }

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

        static void ParseAmmo(LevelModel level, LevelTokens t)
        {
            int mm = level.Multiplier * level.Multiplier;
            Dictionary<char, List<string>> tunnelLists = t.Tunnels ?? new Dictionary<char, List<string>>();

            level.AmmoCols = AmmoColCount;
            level.AmmoRows = t.AmmoRows;
            level.AmmoCells = new AmmoCellData[AmmoColCount, Mathf.Max(1, level.AmmoRows)];

            for (int r = 0; r < level.AmmoRows; r++)
            {
                for (int c = 0; c < AmmoColCount; c++)
                {
                    string cell = c < t.AmmoCols ? (t.Ammo[c, r] ?? "").Trim() : "";
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
            foreach (TunnelBatch b in item.Queue)
            {
                level.HpByColor.TryGetValue(b.Color, out int q);
                level.HpByColor[b.Color] = q + b.Cubes;
            }
            if (BlockKinds.Of(item.Kind).IsColorTarget)
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
