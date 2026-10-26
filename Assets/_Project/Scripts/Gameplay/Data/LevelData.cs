using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>MỘT level tự làm (bộ custom), lưu thành một asset riêng</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Level Data", fileName = "Level_000")]
    public class LevelData : ScriptableObject
    {
        /// <summary>Dùng cho loại ô trên bàn trong trình sửa level</summary>
        public enum CellType { Empty, Cube, Column, Blocker }
        /// <summary>Dùng cho loại ô khay đạn trong trình sửa level</summary>
        public enum SlotType { None, Wall, Empty, Box, Tunnel }

        /// <summary>Dùng cho dữ liệu một ô bàn chơi</summary>
        [Serializable]
        public class BoardCell
        {
            public CellType type = CellType.Empty;
            [Tooltip("Mã màu (r, b, g…) – chỉ với Cube")] public string color = "";
            [Tooltip("Máu theo ô thiết kế (game nhân Multiplier² với khối gộp)")] public int hp = 1;
            [Tooltip("Id khối gộp (các ô cùng id + cùng hòm = một khối dùng chung máu); trống = gạch lẻ. Với Column: id cột")] public string group = "";
            [Tooltip("Id hòm gỗ che ô này; trống = không có hòm")] public string crate = "";
            public int crateHp = 50;
            [Tooltip("Lớp phủ chưa hỗ trợ của game gốc (st-…, cur-…) – giữ nguyên khi nhập/xuất")] public string overlay = "";
            [Tooltip("Mã nguyên văn của vật cản chưa hỗ trợ (/bomb-…, /lck-…) – game coi là tường")] public string raw = "";

            /// <summary>Dùng cho sao chép dữ liệu</summary>
            public BoardCell Clone() => (BoardCell)MemberwiseClone();
        }

        /// <summary>Dùng cho dữ liệu một hộp đạn</summary>
        [Serializable]
        public class Box
        {
            public string color = "r";
            [Tooltip("Số bi theo ô thiết kế (game nhân Multiplier²)")] public int balls = 8;
            [Tooltip("> 0: băng, cần N lượt bắn hộp kề")] public int ice;
            [Tooltip("Hộp \"?\" – lộ màu khi thông đường")] public bool hidden;
            [Tooltip("0 = không cửa; 1 = cửa đóng lúc đầu; 2 = cửa mở lúc đầu")] public int door;

            /// <summary>Dùng cho sao chép dữ liệu</summary>
            public Box Clone() => (Box)MemberwiseClone();
        }

        /// <summary>Dùng cho dữ liệu một ô khay đạn</summary>
        [Serializable]
        public class Slot
        {
            public SlotType type = SlotType.None;
            public Box box = new Box();
            [Tooltip("Tunnel: chữ cái id (A, B…)")] public string tunnelId = "A";
            [Tooltip("Tunnel: hướng nhả hộp L / R / U / D")] public string tunnelDir = "D";
            [Tooltip("Mã nguyên văn của ô tường (/awl…)")] public string raw = "/awl";

            /// <summary>Dùng cho sao chép dữ liệu</summary>
            public Slot Clone()
            {
                var s = (Slot)MemberwiseClone();
                s.box = box != null ? box.Clone() : new Box();
                return s;
            }
        }

        /// <summary>Dùng cho dữ liệu một ống nhả hộp</summary>
        [Serializable]
        public class Tunnel
        {
            public string id = "A";
            public List<Box> queue = new List<Box>();
        }

        [Header("Thông tin (cho người thiết kế)")]
        public string title = "";
        public int chapter = 1;
        [TextArea] public string notes = "";
        [Tooltip("Ảnh pixel art đã nhập (để nhập lại khi sửa ảnh)")] public Texture2D sourceImage;

        [Header("Thông số level")]
        public LevelDifficulty difficulty = LevelDifficulty.Normal;
        public int capacity = 200;
        [Min(1)] public int multiplier = 2;
        public bool isFigurative = true;

        [Header("Bàn gạch (ô thiết kế)")]
        [Min(1)] public int boardCols = 10;
        [Min(1)] public int boardRows = 16;
        [Tooltip("Kích thước bàn THẬT khai báo cho game (cột BoardSize của CSV). 0 = tự tính = ô thiết kế × Multiplier. " +
                 "Level nhập từ CSV gốc giữ số gốc (vd 20×32) vì bàn gốc không luôn bằng số dòng × Multiplier.")]
        public Vector2Int boardSizeOverride;
        [Tooltip("Thứ tự: dòng 0 = dòng TRÊN cùng; index = row * boardCols + col")]
        public List<BoardCell> board = new List<BoardCell>();

        [Header("Khay đạn")]
        [Min(1)] public int ammoCols = 8;
        [Min(1)] public int ammoRows = 6;
        [Tooltip("Hàng 0 = sát lối ra; index = row * ammoCols + col")]
        public List<Slot> ammo = new List<Slot>();
        public List<Tunnel> tunnels = new List<Tunnel>();

        /// <summary>Dùng cho lấy ô bàn theo cột, hàng</summary>
        public BoardCell Cell(int col, int row) { EnsureSize(); return board[row * boardCols + col]; }
        /// <summary>Dùng cho lấy ô khay theo cột, hàng</summary>
        public Slot AmmoSlot(int col, int row) { EnsureSize(); return ammo[row * ammoCols + col]; }

        /// <summary>Đảm bảo hai danh sách đủ số ô</summary>
        public void EnsureSize()
        {
            if (board.Count != boardCols * boardRows) board = Resize(board, BoardGridCols(), boardCols, boardRows, () => new BoardCell());
            if (ammo.Count != ammoCols * ammoRows) ammo = Resize(ammo, AmmoGridCols(), ammoCols, ammoRows, () => new Slot());
        }

        int _prevBoardCols = -1, _prevAmmoCols = -1;
        int BoardGridCols() => _prevBoardCols > 0 ? _prevBoardCols : boardCols;
        int AmmoGridCols() => _prevAmmoCols > 0 ? _prevAmmoCols : ammoCols;

        /// <summary>Xử lý đổi kích thước bàn chơi</summary>
        public void SetBoardSize(int cols, int rows)
        {
            EnsureSize();
            _prevBoardCols = boardCols;
            boardCols = Mathf.Max(1, cols); boardRows = Mathf.Max(1, rows);
            EnsureSize();
            _prevBoardCols = -1;
        }

        /// <summary>Xử lý đổi kích thước khay đạn</summary>
        public void SetAmmoSize(int cols, int rows)
        {
            EnsureSize();
            _prevAmmoCols = ammoCols;
            ammoCols = Mathf.Clamp(cols, 1, 8); ammoRows = Mathf.Max(1, rows);
            EnsureSize();
            _prevAmmoCols = -1;
        }

        static List<T> Resize<T>(List<T> old, int oldCols, int cols, int rows, Func<T> make)
        {
            var list = new List<T>(cols * rows);
            int oldRows = oldCols > 0 ? old.Count / oldCols : 0;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                    list.Add(c < oldCols && r < oldRows ? old[r * oldCols + c] : make());
            return list;
        }

        /// <summary>Dữ liệu → lưới mã như CSV gốc, để Build dựng LevelModel</summary>
        public LevelTokens ToTokens(int id)
        {
            EnsureSize();
            var t = new LevelTokens
            {
                Id = id,
                HardLevel = difficulty == LevelDifficulty.SuperHard ? "SHard" : difficulty == LevelDifficulty.Hard ? "Hard" : "-",
                Capacity = capacity,
                Multiplier = Mathf.Max(1, multiplier),
                BoardCols = boardSizeOverride.x > 0 ? boardSizeOverride.x : boardCols * Mathf.Max(1, multiplier),
                BoardRows = boardSizeOverride.y > 0 ? boardSizeOverride.y : boardRows * Mathf.Max(1, multiplier),
                IsFigurative = isFigurative,
                Board = new string[boardCols, boardRows],
                Ammo = new string[ammoCols, ammoRows],
            };
            for (int r = 0; r < boardRows; r++)
                for (int c = 0; c < boardCols; c++)
                    t.Board[c, r] = CellToken(board[r * boardCols + c]);
            for (int r = 0; r < ammoRows; r++)
                for (int c = 0; c < ammoCols; c++)
                    t.Ammo[c, r] = SlotToken(ammo[r * ammoCols + c]);
            foreach (Tunnel tu in tunnels)
            {
                if (tu == null || string.IsNullOrEmpty(tu.id)) continue;
                var list = new List<string>();
                foreach (Box b in tu.queue) list.Add(BoxToken(b));
                t.Tunnels[char.ToUpperInvariant(tu.id[0])] = list;
            }
            return t;
        }

        /// <summary>Dùng cho chuyển ô bàn sang mã CSV</summary>
        public static string CellToken(BoardCell c)
        {
            if (c == null) return "-";
            switch (c.type)
            {
                case CellType.Column: return "/wl-" + c.hp + "-" + (string.IsNullOrEmpty(c.group) ? "0" : c.group);
                case CellType.Blocker: return string.IsNullOrEmpty(c.raw) ? "/blk" : c.raw;
                case CellType.Cube:
                    var sb = new StringBuilder(c.color).Append('-').Append(Mathf.Max(1, c.hp));
                    if (!string.IsNullOrEmpty(c.group)) sb.Append('-').Append(c.group);
                    if (!string.IsNullOrEmpty(c.crate)) sb.Append("/cr-").Append(c.crateHp).Append('-').Append(c.crate);
                    if (!string.IsNullOrEmpty(c.overlay)) sb.Append('/').Append(c.overlay);
                    return sb.ToString();
                default: return "-";
            }
        }

        /// <summary>Dùng cho chuyển ô khay sang mã CSV</summary>
        public static string SlotToken(Slot s)
        {
            if (s == null) return "";
            switch (s.type)
            {
                case SlotType.Wall: return string.IsNullOrEmpty(s.raw) ? "/awl" : s.raw;
                case SlotType.Empty: return "-";
                case SlotType.Box: return BoxToken(s.box);
                case SlotType.Tunnel: return "/tu-" + (string.IsNullOrEmpty(s.tunnelId) ? "A" : s.tunnelId.Substring(0, 1)) + (string.IsNullOrEmpty(s.tunnelDir) ? "D" : s.tunnelDir.Substring(0, 1));
                default: return "";
            }
        }

        /// <summary>Dùng cho chuyển hộp sang mã CSV</summary>
        public static string BoxToken(Box b)
        {
            if (b == null) return "";
            var sb = new StringBuilder(b.color).Append('-').Append(Mathf.Max(0, b.balls));
            if (b.ice > 0) sb.Append("/ice-").Append(b.ice);
            if (b.hidden) sb.Append("/qm");
            if (b.door > 0) sb.Append("/door-").Append(b.door);
            return sb.ToString();
        }

        /// <summary>Đổ lưới mã (từ CSV gốc) vào asset này</summary>
        public void FromTokens(LevelTokens t)
        {
            string hard = (t.HardLevel ?? "").Trim().ToUpperInvariant();
            difficulty = hard == "SHARD" || hard == "SUPERHARD" ? LevelDifficulty.SuperHard : hard == "HARD" ? LevelDifficulty.Hard : LevelDifficulty.Normal;
            capacity = t.Capacity;
            multiplier = Mathf.Max(1, t.Multiplier);
            isFigurative = t.IsFigurative;

            boardCols = Mathf.Max(1, Mathf.Min(t.BoardDesignCols, Mathf.CeilToInt(t.BoardCols / (float)multiplier)));
            boardSizeOverride = new Vector2Int(t.BoardCols, t.BoardRows);
            int top = 0;
            while (top < t.BoardLines - 1 && LineEmpty(t, top)) top++;
            boardRows = Mathf.Max(1, t.BoardLines - top);
            board = new List<BoardCell>(boardCols * boardRows);
            for (int r = 0; r < boardRows; r++)
                for (int c = 0; c < boardCols; c++)
                    board.Add(ParseCell(t.Board[c, r + top]));

            ammoCols = Mathf.Max(1, t.AmmoCols);
            ammoRows = Mathf.Max(1, t.AmmoRows);
            ammo = new List<Slot>(ammoCols * ammoRows);
            for (int r = 0; r < ammoRows; r++)
                for (int c = 0; c < ammoCols; c++)
                    ammo.Add(c < t.AmmoCols && r < t.AmmoRows ? ParseSlot(t.Ammo[c, r]) : new Slot());

            tunnels = new List<Tunnel>();
            foreach (var kv in t.Tunnels)
            {
                var tu = new Tunnel { id = kv.Key.ToString() };
                foreach (string code in kv.Value) { Box b = ParseBox(code); if (b != null) tu.queue.Add(b); }
                tunnels.Add(tu);
            }
        }

        static bool LineEmpty(LevelTokens t, int line)
        {
            for (int c = 0; c < t.BoardDesignCols; c++)
            {
                string v = (t.Board[c, line] ?? "").Trim();
                if (v.Length > 0 && v != "-") return false;
            }
            return true;
        }

        /// <summary>Số cột mã (có nội dung) bị bỏ vì nằm ngoài bề ngang bàn</summary>
        public static int CountTokensOutside(LevelTokens t, int designCols)
        {
            int n = 0;
            for (int r = 0; r < t.BoardLines; r++)
                for (int c = designCols; c < t.BoardDesignCols; c++)
                {
                    string v = (t.Board[c, r] ?? "").Trim();
                    if (v.Length > 0 && v != "-") n++;
                }
            return n;
        }

        /// <summary>Xử lý đọc mã CSV thành ô bàn</summary>
        public static BoardCell ParseCell(string code)
        {
            var cell = new BoardCell();
            code = (code ?? "").Trim();
            if (code.Length == 0 || code == "-") return cell;
            if (code.StartsWith("/wl-"))
            {
                string[] w = code.Substring(1).Split('-');
                cell.type = CellType.Column;
                cell.hp = w.Length > 1 && int.TryParse(w[1], out int wh) ? wh : 25;
                cell.group = w.Length > 2 ? w[2] : "0";
                return cell;
            }
            if (code.StartsWith("/")) { cell.type = CellType.Blocker; cell.raw = code; return cell; }

            string[] parts = code.Split('/');
            string[] tk = parts[0].Split('-');
            cell.type = CellType.Cube;
            cell.color = tk[0];
            cell.hp = tk.Length > 1 && int.TryParse(tk[1], out int h) ? h : 1;
            cell.group = tk.Length > 2 ? tk[2] : "";
            var extra = new List<string>();
            for (int p = 1; p < parts.Length; p++)
            {
                string[] b = parts[p].Split('-');
                if (b[0] == "cr")
                {
                    cell.crateHp = b.Length > 1 && int.TryParse(b[1], out int ch) ? ch : 50;
                    cell.crate = b.Length > 2 ? b[2] : "0";
                }
                else if (parts[p].Length > 0) extra.Add(parts[p]);
            }
            cell.overlay = string.Join("/", extra);
            return cell;
        }

        /// <summary>Xử lý đọc mã CSV thành ô khay</summary>
        public static Slot ParseSlot(string code)
        {
            var s = new Slot();
            code = (code ?? "").Trim();
            if (code.Length == 0) { s.type = SlotType.None; return s; }
            if (code.StartsWith("/awl")) { s.type = SlotType.Wall; s.raw = code; return s; }
            if (code == "-") { s.type = SlotType.Empty; return s; }
            if (code.StartsWith("/tu-") || code.StartsWith("tu-"))
            {
                string id = code.Substring(code.IndexOf("tu-", StringComparison.Ordinal) + 3);
                s.type = SlotType.Tunnel;
                s.tunnelId = id.Length > 0 ? char.ToUpperInvariant(id[0]).ToString() : "A";
                s.tunnelDir = id.Length > 1 ? char.ToUpperInvariant(id[1]).ToString() : "D";
                return s;
            }
            Box box = ParseBox(code);
            if (box == null) { s.type = SlotType.Empty; return s; }
            s.type = SlotType.Box;
            s.box = box;
            return s;
        }

        /// <summary>Xử lý đọc mã CSV thành hộp</summary>
        public static Box ParseBox(string code)
        {
            string[] parts = (code ?? "").Split('/');
            string[] t = parts[0].Split('-');
            if (t.Length < 2 || !int.TryParse(t[1], out int n)) return null;
            var box = new Box { color = t[0], balls = n };
            for (int i = 1; i < parts.Length; i++)
            {
                string[] b = parts[i].Split('-');
                if (b[0] == "ice") box.ice = b.Length > 1 && int.TryParse(b[1], out int k) ? k : 1;
                else if (b[0] == "qm") box.hidden = true;
                else if (b[0] == "door") box.door = b.Length > 1 && int.TryParse(b[1], out int dv) ? dv : 1;
            }
            return box;
        }
    }
}
