using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BounceBlast.Gameplay;
using UnityEditor;
using UnityEngine;

namespace BounceBlast.EditorTools
{
    /// <summary>Crab Mode – rà toàn bộ level: mô phỏng luật mới (khay ammo đủ cơ chế + N máy xay + cua ăn block có đường ăn từ viền)
    /// rồi tìm một chuỗi bấm thắng bằng DFS có giới hạn. Xuất Docs/thiet-ke/crab-mode-level-audit.{md,csv}.
    /// Mô hình theo lượt: sau mỗi lần bấm, mọi máy xay nhả hết cua có thể nhả (cua ăn ngay) rồi mới tới lần bấm sau.</summary>
    public static class CrabLevelAudit
    {
        static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        // ------------------------------------------------------------------ board
        class Board
        {
            public int FW, FH, M, Rows;
            public int[] cell;      // -1 trống, -2 vật cản (tường / block đặc biệt), >=0 chỉ số block ăn được
            public string[] color;
            public int[] hp;
            public bool[] hidden;
            public List<int>[] cells;
            public int left;        // số block ăn được còn lại
            public bool[] reach;

            public Board Clone()
            {
                var b = (Board)MemberwiseClone();
                b.cell = (int[])cell.Clone(); b.hp = (int[])hp.Clone(); b.hidden = (bool[])hidden.Clone(); b.reach = (bool[])reach.Clone();
                return b;
            }

            public void ComputeReach()
            {
                Array.Clear(reach, 0, reach.Length);
                var q = new Queue<int>();
                void Seed(int x, int y) { int i = y * FW + x; if (cell[i] == -1 && !reach[i]) { reach[i] = true; q.Enqueue(i); } }
                for (int x = 0; x < FW; x++) { Seed(x, 0); Seed(x, FH - 1); }
                for (int y = 0; y < FH; y++) { Seed(0, y); Seed(FW - 1, y); }
                while (q.Count > 0)
                {
                    int i = q.Dequeue(), cx = i % FW, cy = i / FW;
                    foreach (Vector2Int d in Dirs)
                    {
                        int nx = cx + d.x, ny = cy + d.y;
                        if (nx < 0 || ny < 0 || nx >= FW || ny >= FH) continue;
                        int n = ny * FW + nx;
                        if (!reach[n] && cell[n] == -1) { reach[n] = true; q.Enqueue(n); }
                    }
                }
                // Board Qmark: "?" tan khi hở mặt trên hoặc hai bên (ô trống thật)
                for (int b = 0; b < hidden.Length; b++)
                {
                    if (!hidden[b] || hp[b] <= 0) continue;
                    foreach (int i in cells[b])
                    {
                        int x = i % FW, y = i / FW;
                        if (y + 1 >= M + Rows || Free(x, y + 1) || Free(x - 1, y) || Free(x + 1, y)) { hidden[b] = false; break; }
                    }
                }
            }

            bool Free(int x, int y) => x >= 0 && y >= 0 && x < FW && y < FH && cell[y * FW + x] == -1;

            public bool Eatable(int b)
            {
                if (hp[b] <= 0 || hidden[b]) return false;
                foreach (int i in cells[b])
                {
                    int x = i % FW, y = i / FW;
                    foreach (Vector2Int d in Dirs)
                    {
                        int nx = x + d.x, ny = y + d.y;
                        if (nx >= 0 && ny >= 0 && nx < FW && ny < FH && reach[ny * FW + nx]) return true;
                    }
                }
                return false;
            }

            public bool AnyEatable(string c)
            {
                for (int b = 0; b < hp.Length; b++) if (hp[b] > 0 && Same(color[b], c) && Eatable(b)) return true;
                return false;
            }

            /// <summary>Ăn tối đa n hit màu c (block gần viền dưới trước, như cua chạy từ máy xay). Trả về số hit đã ăn</summary>
            public int Eat(string c, int n)
            {
                int eaten = 0;
                while (eaten < n)
                {
                    int best = -1, bestY = int.MaxValue;
                    for (int b = 0; b < hp.Length; b++)
                    {
                        if (hp[b] <= 0 || !Same(color[b], c) || !Eatable(b)) continue;
                        int y = int.MaxValue; foreach (int i in cells[b]) y = Mathf.Min(y, i / FW);
                        if (y < bestY) { bestY = y; best = b; }
                    }
                    if (best < 0) break;
                    int take = Mathf.Min(hp[best], n - eaten);
                    hp[best] -= take; eaten += take;
                    if (hp[best] == 0) { foreach (int i in cells[best]) cell[i] = -1; left--; ComputeReach(); }
                }
                return eaten;
            }
        }

        // ------------------------------------------------------------------ tray
        class Box { public string color; public int balls, ice, door; public bool closed; public int swap; public Box next; }
        class Tun { public int x, y; public Vector2Int dir; public bool basket, open; public Vector2Int pref; public List<Box> q; }

        class Tray
        {
            public int C, R;
            public AmmoCellKind[] kind;
            public Box[] box;
            public List<Tun> tuns;
            public HashSet<int> swapOff;

            public Tray Clone()
            {
                var t = new Tray { C = C, R = R, kind = (AmmoCellKind[])kind.Clone(), box = new Box[box.Length], tuns = new List<Tun>(), swapOff = new HashSet<int>(swapOff) };
                for (int i = 0; i < box.Length; i++) if (box[i] != null) t.box[i] = Copy(box[i]);
                foreach (Tun u in tuns) t.tuns.Add(new Tun { x = u.x, y = u.y, dir = u.dir, basket = u.basket, open = u.open, pref = u.pref, q = new List<Box>(u.q) });
                return t;
            }

            static Box Copy(Box b) => new Box { color = b.color, balls = b.balls, ice = b.ice, door = b.door, closed = b.closed, swap = b.swap, next = b.next };
            int I(int x, int y) => y * C + x;
            bool In(int x, int y) => x >= 0 && y >= 0 && x < C && y < R;

            bool[] Reach()
            {
                var r = new bool[C * R]; var q = new Queue<int>();
                for (int x = 0; x < C; x++) if (kind[I(x, 0)] == AmmoCellKind.Empty) { r[I(x, 0)] = true; q.Enqueue(I(x, 0)); }
                while (q.Count > 0)
                {
                    int i = q.Dequeue(), cx = i % C, cy = i / C;
                    foreach (Vector2Int d in Dirs)
                    {
                        int nx = cx + d.x, ny = cy + d.y;
                        if (!In(nx, ny) || r[I(nx, ny)] || kind[I(nx, ny)] != AmmoCellKind.Empty) continue;
                        r[I(nx, ny)] = true; q.Enqueue(I(nx, ny));
                    }
                }
                return r;
            }

            public List<int> Clickable()
            {
                bool[] r = Reach(); var list = new List<int>();
                for (int i = 0; i < box.Length; i++)
                {
                    Box b = box[i]; if (b == null || b.ice > 0 || (b.door > 0 && b.closed)) continue;
                    int x = i % C, y = i / C; bool open = y == 0;
                    foreach (Vector2Int d in Dirs) { int nx = x + d.x, ny = y + d.y; if (In(nx, ny) && r[I(nx, ny)]) open = true; }
                    if (open) list.Add(i);
                }
                return list;
            }

            /// <summary>Lấy hộp ở ô i ra (đúng thứ tự AmmoGridController.Launch)</summary>
            public Box Launch(int i)
            {
                Box b = box[i]; box[i] = null;
                int x = i % C, y = i / C;
                kind[i] = b.next != null ? AmmoCellKind.Box : AmmoCellKind.Empty;
                foreach (Box o in box) if (o != null && o.door > 0) o.closed = !o.closed;
                foreach (Vector2Int d in Dirs)
                {
                    int nx = x + d.x, ny = y + d.y; if (!In(nx, ny)) continue;
                    Box o = box[I(nx, ny)]; if (o != null && o.ice > 0) o.ice--;
                    foreach (Tun u in tuns) if (u.basket && u.x == nx && u.y == ny) { u.pref = new Vector2Int(x, y); u.open = true; }
                }
                Swap(b.swap);
                StepTunnels();
                if (b.next != null) box[i] = Copy(b.next);
                return b;
            }

            void Swap(int used)
            {
                if (used > 0) swapOff.Add(used);
                var groups = new Dictionary<int, List<Box>>();
                foreach (Box o in box)
                {
                    if (o == null || o.swap <= 0 || swapOff.Contains(o.swap)) continue;
                    if (!groups.TryGetValue(o.swap, out var l)) groups[o.swap] = l = new List<Box>();
                    l.Add(o);
                }
                foreach (var l in groups.Values)
                {
                    if (l.Count < 2) continue;
                    string first = l[0].color;
                    for (int k = 0; k < l.Count - 1; k++) l[k].color = l[k + 1].color;
                    l[l.Count - 1].color = first;
                }
            }

            void StepTunnels()
            {
                bool changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (Tun u in tuns)
                    {
                        if (u.basket || u.q.Count == 0) continue;
                        int ox = u.x + u.dir.x, oy = u.y + u.dir.y;
                        if (!In(ox, oy) || kind[I(ox, oy)] != AmmoCellKind.Empty) continue;
                        box[I(ox, oy)] = Copy(u.q[0]); u.q.RemoveAt(0); kind[I(ox, oy)] = AmmoCellKind.Box; changed = true;
                    }
                }
                foreach (Tun u in tuns)
                {
                    if (!u.basket || !u.open || u.q.Count == 0) continue;
                    var free = new List<Vector2Int>();
                    for (int cx = 0; cx < C; cx++) for (int cy = 0; cy < R; cy++) if (kind[I(cx, cy)] == AmmoCellKind.Empty) free.Add(new Vector2Int(cx, cy));
                    if (free.Count > 1) free.Remove(u.pref);
                    int fx = u.x;
                    free.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : Mathf.Abs(a.x - fx) != Mathf.Abs(b.x - fx) ? Mathf.Abs(a.x - fx).CompareTo(Mathf.Abs(b.x - fx)) : a.x.CompareTo(b.x));
                    int n = Mathf.Min(free.Count, u.q.Count);
                    for (int k = 0; k < n; k++) { int j = I(free[k].x, free[k].y); box[j] = Copy(u.q[0]); u.q.RemoveAt(0); kind[j] = AmmoCellKind.Box; }
                    if (u.q.Count == 0) kind[I(u.x, u.y)] = AmmoCellKind.Empty;
                }
            }

            public bool Empty
            {
                get { foreach (Box b in box) if (b != null) return false; foreach (Tun u in tuns) if (u.q.Count > 0) return false; return true; }
            }
        }

        class Slot { public string color; public int rem; }

        class State
        {
            public Board board; public Tray tray; public Slot[] slots;
            public State Clone()
            {
                var s = new State { board = board.Clone(), tray = tray.Clone(), slots = new Slot[slots.Length] };
                for (int i = 0; i < slots.Length; i++) if (slots[i] != null) s.slots[i] = new Slot { color = slots[i].color, rem = slots[i].rem };
                return s;
            }

            /// <summary>Mọi máy xay nhả hết cua có thể nhả</summary>
            public void Settle()
            {
                bool moved = true;
                while (moved)
                {
                    moved = false;
                    for (int i = 0; i < slots.Length; i++)
                    {
                        Slot s = slots[i]; if (s == null) continue;
                        int e = board.Eat(s.color, s.rem);
                        if (e > 0) { moved = true; s.rem -= e; if (s.rem <= 0) slots[i] = null; }
                    }
                }
            }

            public ulong Hash()
            {
                ulong h = 1469598103934665603UL;
                void Mix(int v) { h ^= (ulong)(uint)v; h *= 1099511628211UL; }
                foreach (int v in board.hp) Mix(v);
                for (int i = 0; i < tray.box.Length; i++) { Box b = tray.box[i]; Mix(b == null ? -1 : (b.color.GetHashCode() ^ (b.balls << 8) ^ (b.ice << 16) ^ (b.closed ? 1 << 20 : 0))); }
                foreach (Tun u in tray.tuns) Mix(u.q.Count + (u.open ? 1000 : 0));
                var sl = new List<int>();
                foreach (Slot s in slots) sl.Add(s == null ? 0 : (s.color.GetHashCode() ^ (s.rem << 10)));
                sl.Sort(); foreach (int v in sl) Mix(v);
                return h;
            }
        }

        static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        // ------------------------------------------------------------------ build from LevelModel
        static Box ToBox(AmmoBoxData d) => d == null ? null : new Box { color = d.ColorCode, balls = d.Balls, ice = d.IceHits, door = d.Door, closed = d.DoorClosed, swap = d.SwapGroup, next = ToBox(d.Next) };

        static State Build(LevelModel m, int margin, int slotCount, List<string> specials, out bool specialCountsForWin)
        {
            specialCountsForWin = false;
            var bd = new Board { M = margin, Rows = m.BoardRows, FW = m.BoardCols + margin * 2, FH = m.BoardRows + margin * 2 };
            bd.cell = new int[bd.FW * bd.FH]; for (int i = 0; i < bd.cell.Length; i++) bd.cell[i] = -1;
            var colors = new List<string>(); var hps = new List<int>(); var hid = new List<bool>(); var cl = new List<List<int>>();
            foreach (BoardItemData it in m.BoardItems)
            {
                var cs = new List<int>();
                RectInt r = it.Cells;
                if (it.Mask == null) { for (int x = r.xMin; x < r.xMax; x++) for (int y = r.yMin; y < r.yMax; y++) cs.Add((y + margin) * bd.FW + x + margin); }
                else foreach (Vector2Int mm in it.Mask) cs.Add((r.y + mm.y + margin) * bd.FW + r.x + mm.x + margin);
                bool edible = it.Kind == BoardItemKind.Cube || it.Kind == BoardItemKind.CombinedCube;
                if (!edible)
                {
                    if (it.Kind != BoardItemKind.Wall && !specials.Contains(it.Kind.ToString())) specials.Add(it.Kind.ToString());
                    if (BlockKinds.Of(it.Kind).CountsForWin) specialCountsForWin = true;
                    foreach (int i in cs) if (i >= 0 && i < bd.cell.Length) bd.cell[i] = -2;
                    continue;
                }
                int idx = colors.Count;
                colors.Add(it.ColorCode); hps.Add(Mathf.Max(1, it.Hp)); hid.Add(it.Hidden); cl.Add(cs);
                foreach (int i in cs) if (i >= 0 && i < bd.cell.Length) bd.cell[i] = idx;
            }
            bd.color = colors.ToArray(); bd.hp = hps.ToArray(); bd.hidden = hid.ToArray(); bd.cells = cl.ToArray(); bd.left = colors.Count;
            bd.reach = new bool[bd.cell.Length];
            bd.ComputeReach();

            int C = m.AmmoCols, R = Mathf.Max(1, m.AmmoRows);
            var tr = new Tray { C = C, R = R, kind = new AmmoCellKind[C * R], box = new Box[C * R], tuns = new List<Tun>(), swapOff = new HashSet<int>() };
            for (int x = 0; x < C; x++)
                for (int y = 0; y < R; y++)
                {
                    AmmoCellData d = m.AmmoCells[x, y];
                    int i = y * C + x;
                    tr.kind[i] = d != null ? d.Kind : AmmoCellKind.Wall;
                    if (d == null) continue;
                    if (d.Kind == AmmoCellKind.Box) tr.box[i] = ToBox(d.Box);
                    if (d.Kind == AmmoCellKind.Tunnel && d.Tunnel != null)
                    {
                        var u = new Tun { x = x, y = y, dir = d.Tunnel.Direction, basket = d.Tunnel.Basket, q = new List<Box>() };
                        foreach (AmmoBoxData b in d.Tunnel.Queue) u.q.Add(ToBox(b));
                        tr.tuns.Add(u);
                    }
                }
            return new State { board = bd, tray = tr, slots = new Slot[slotCount] };
        }

        // ------------------------------------------------------------------ search
        class Result { public bool win; public int nodes; public string reason = ""; public int bestLeft = int.MaxValue; public List<string> path; }

        static Result Solve(State start, int budget)
        {
            var res = new Result();
            var seen = new HashSet<ulong>();
            var path = new List<string>();
            start.Settle();
            bool Dfs(State s)
            {
                if (s.board.left == 0) { res.path = new List<string>(path); return true; }
                if (++res.nodes > budget) return false;
                if (!seen.Add(s.Hash())) return false;
                res.bestLeft = Mathf.Min(res.bestLeft, s.board.left);
                int free = Array.IndexOf(s.slots, null);
                if (free < 0) { if (res.reason == "") res.reason = "Queue full"; return false; }
                List<int> moves = s.tray.Clickable();
                if (moves.Count == 0) { if (res.reason == "") res.reason = "No more moves"; return false; }
                // ưu tiên: màu đang có đường ăn, ăn được nhiều nhất trước; màu chưa ăn được để sau
                var scored = new List<KeyValuePair<int, int>>();
                foreach (int mv in moves)
                {
                    Box b = s.tray.box[mv];
                    int sc = s.board.AnyEatable(b.color) ? 1000 - b.balls : -b.balls;
                    scored.Add(new KeyValuePair<int, int>(mv, sc));
                }
                scored.Sort((a, c) => c.Value.CompareTo(a.Value));
                var tried = new HashSet<string>();
                foreach (var kv in scored)
                {
                    Box b0 = s.tray.box[kv.Key];
                    State n = s.Clone();
                    Box b = n.tray.Launch(kv.Key);
                    n.slots[free] = new Slot { color = b.color, rem = b.balls };
                    n.Settle();
                    path.Add(b0.color + b0.balls + "@" + (kv.Key % s.tray.C) + "," + (kv.Key / s.tray.C));
                    if (Dfs(n)) return true;
                    path.RemoveAt(path.Count - 1);
                    if (res.nodes > budget) return false;
                }
                return false;
            }
            res.win = Dfs(start);
            if (!res.win && res.nodes > budget) res.reason = "Hết ngân sách tìm (" + budget + " nút) – " + (res.reason == "" ? "chưa rõ" : res.reason);
            return res;
        }

        // ------------------------------------------------------------------ entry
        [MenuItem("Tools/Bounce Blast/Crab Mode – rà level")]
        public static void RunMenu() => Debug.Log(Run());

        /// <summary>Chạy rà; trả về dòng tóm tắt</summary>
        public static string Run(int slotCount = 5, int budget = 4000, int from = 1, int to = 0)
        {
            GameConfig c0 = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
            if (to <= 0) to = c0.MaxLevel;
            var ids = new List<int>(); for (int i = from; i <= to; i++) ids.Add(i);
            return RunList(ids, slotCount, budget, null);
        }

        /// <summary>Rà một danh sách level (kèm nhãn cơ chế cho từng level để ghi vào báo cáo)</summary>
        public static string RunList(List<int> levels, int slotCount, int budget, Dictionary<int, string> tags)
        {
            GameConfig cfg = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/_Project/Data/GameConfig.asset");
            var csv = new StringBuilder("level,difficulty,result,reason,blocks,blocks_left_best,nodes,specials,special_counts_for_win\n");
            int win = 0, lose = 0, special = 0, budgetHit = 0, err = 0;
            var loseList = new List<string>(); var specialList = new List<string>();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var winList = new List<string>();
            foreach (int id in levels)
            {
                string tag = tags != null && tags.TryGetValue(id, out string tg) ? tg : "";
                LevelModel m = null;
                try { m = LevelParser.Load(cfg, id); } catch (Exception e) { Debug.LogWarning($"[CrabAudit] lv{id}: {e.Message}"); }
                if (m == null) { err++; csv.AppendLine($"{id},,ERROR,parse,,,,,"); continue; }
                var specials = new List<string>();
                State s = Build(m, Mathf.Max(1, cfg.crabSandMargin), slotCount, specials, out bool specialWin);
                int blocks = s.board.left;
                Result r = Solve(s, budget);
                string result = r.win ? "WIN" : r.nodes > budget ? "UNKNOWN" : "LOSE";
                if (r.win) win++; else if (result == "UNKNOWN") budgetHit++; else lose++;
                if (specialWin) { special++; specialList.Add($"{id} ({string.Join("/", specials)})"); }
                if (r.win) winList.Add($"| {id} | {tag} | {blocks} | {r.path.Count} | {r.nodes} |");
                if (!r.win) loseList.Add($"| {id} ({tag}) | {m.Difficulty} | {result} | {r.reason} | {blocks} | {(r.bestLeft == int.MaxValue ? blocks : r.bestLeft)} | {string.Join("/", specials)} |");
                csv.AppendLine($"{id},{m.Difficulty},{result},{r.reason.Replace(',', ';')},{blocks},{(r.bestLeft == int.MaxValue ? blocks : r.bestLeft)},{r.nodes},{string.Join("/", specials)},{specialWin}");
            }
            string dir = "Docs/thiet-ke";
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "crab-mode-level-audit.csv"), csv.ToString(), new UTF8Encoding(false));
            var md = new StringBuilder();
            md.AppendLine("# Crab Mode – rà level (tự sinh bởi Tools/Bounce Blast/Crab Mode – rà level)\n");
            md.AppendLine($"Ngày chạy: {DateTime.Now:dd/MM/yyyy HH:mm} · {slotCount} máy xay · ngân sách {budget} nút/level · {sw.Elapsed.TotalSeconds:F0}s\n");
            md.AppendLine("Mô hình: khay ammo đủ cơ chế (thông đường, băng, cửa, ammo đôi, đổi màu, tunnel, giỏ); máy xay không gắn màu; cua ăn block cùng màu có đường ăn từ viền; block đứng yên. Theo lượt: sau mỗi lần bấm mọi máy nhả hết cua có thể nhả. Block đặc biệt (hòm, cột, đá, rèm, bom, tunnel bàn) coi là vật cản chưa ăn được.\n");
            md.AppendLine($"| Kết quả | Số level |\n|---|---|\n| Thắng được | {win} |\n| Không thắng được (đã duyệt hết) | {lose} |\n| Chưa kết luận (hết ngân sách tìm) | {budgetHit} |\n| Lỗi đọc level | {err} |\n| Có block đặc biệt tính thắng (Crab Mode chưa hỗ trợ) | {special} |\n");
            md.AppendLine("## Level thắng được\n\n| Level | Cơ chế | Block | Số lần bấm | Nút đã duyệt |\n|---|---|---|---|---|");
            foreach (string l in winList) md.AppendLine(l);
            md.AppendLine();
            md.AppendLine("## Level không thắng / chưa kết luận\n\n| Level | Độ khó | Kết quả | Lý do | Block | Còn lại ít nhất | Block đặc biệt |\n|---|---|---|---|---|---|---|");
            foreach (string l in loseList) md.AppendLine(l);
            md.AppendLine("\n## Level có block đặc biệt tính thắng\n\n" + string.Join(", ", specialList));
            File.WriteAllText(Path.Combine(dir, "crab-mode-level-audit.md"), md.ToString(), new UTF8Encoding(false));
            return $"win={win} lose={lose} unknown={budgetHit} err={err} specialCountsForWin={special} time={sw.Elapsed.TotalSeconds:F0}s";
        }
    }
}
