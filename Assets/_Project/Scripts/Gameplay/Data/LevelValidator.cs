using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Kiểm tra một level tự làm trước khi chơi</summary>
    public static class LevelValidator
    {
        /// <summary>Dùng cho mức độ của lỗi kiểm level</summary>
        public enum Severity { Error, Warning }

        /// <summary>Dùng cho một lỗi hoặc cảnh báo của level</summary>
        public struct Issue
        {
            public Severity severity;
            public string message;
            public Issue(Severity s, string m) { severity = s; message = m; }
        }

        /// <summary>Một dòng của bảng cân bi</summary>
        public struct ColorBalance
        {
            public string color;
            public int hp, balls;
            public bool Ok => hp == balls;
        }

        /// <summary>Dùng cho kết quả kiểm một level</summary>
        public class Report
        {
            public LevelModel model;
            public readonly List<Issue> issues = new List<Issue>();
            public readonly List<ColorBalance> balance = new List<ColorBalance>();
            public int clickableAtStart, maxBox, totalBalls;
            public bool HasErrors { get { foreach (Issue i in issues) if (i.severity == Severity.Error) return true; return false; } }
        }

        /// <summary>Xử lý kiểm tra level có hợp lệ</summary>
        public static Report Check(LevelData data, ColorLibrary colors)
        {
            var rep = new Report();
            if (data == null) { rep.issues.Add(new Issue(Severity.Error, "Chưa chọn level")); return rep; }
            LevelModel m = LevelParser.Build(data.ToTokens(1));
            rep.model = m;
            if (m == null) { rep.issues.Add(new Issue(Severity.Error, "Không dựng được level")); return rep; }

            var colorsUsed = new SortedSet<string>(System.StringComparer.Ordinal);
            foreach (string c in m.HpByColor.Keys) colorsUsed.Add(c);
            foreach (string c in m.BallsByColor.Keys) colorsUsed.Add(c);
            foreach (string c in colorsUsed)
            {
                m.HpByColor.TryGetValue(c, out int hp);
                m.BallsByColor.TryGetValue(c, out int balls);
                rep.balance.Add(new ColorBalance { color = c, hp = hp, balls = balls });
                if (hp != balls)
                    rep.issues.Add(new Issue(Severity.Error, balls < hp
                        ? $"Màu '{c}': thiếu {hp - balls} bi (máu {hp}, bi {balls}) – không thể thắng"
                        : $"Màu '{c}': thừa {balls - hp} bi (máu {hp}, bi {balls})"));
                if (colors != null && colors.Get(c) == null)
                    rep.issues.Add(new Issue(Severity.Error, $"Mã màu '{c}' không có trong ColorLibrary"));
            }
            if (m.HpByColor.Count == 0) rep.issues.Add(new Issue(Severity.Error, "Bàn chưa có gạch màu nào"));

            int cols = m.AmmoCols, rows = Mathf.Max(1, m.AmmoRows);
            var kind = new AmmoCellKind[cols, rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    AmmoCellData d = r < m.AmmoRows ? m.AmmoCells[c, r] : null;
                    kind[c, r] = d != null ? d.Kind : AmmoCellKind.Wall;
                }
            var paths = new AmmoPathRules();
            paths.Compute(kind);
            for (int r = 0; r < m.AmmoRows; r++)
                for (int c = 0; c < cols; c++)
                {
                    AmmoCellData d = m.AmmoCells[c, r];
                    if (d == null) continue;
                    if (d.Box != null)
                    {
                        rep.maxBox = Mathf.Max(rep.maxBox, d.Box.Balls);
                        bool frozen = d.Box.IceHits > 0, doorClosed = d.Box.Door > 0 && d.Box.DoorClosed;
                        if (paths.IsOpen(new Vector2Int(c, r)) && !frozen && !doorClosed) rep.clickableAtStart++;
                        if (d.Box.Balls <= 0) rep.issues.Add(new Issue(Severity.Error, $"Hộp ở ô ({c},{r}) có 0 bi"));
                    }
                    if (d.Tunnel != null)
                    {
                        Vector2Int o = new Vector2Int(c, r) + d.Tunnel.Direction;
                        if (o.x < 0 || o.y < 0 || o.x >= cols || o.y >= m.AmmoRows || kind[o.x, o.y] == AmmoCellKind.Wall)
                            rep.issues.Add(new Issue(Severity.Error, $"Tunnel {d.Tunnel.Id} ở ô ({c},{r}) nhả hộp ra ngoài khay / vào tường"));
                        if (d.Tunnel.Queue.Count == 0)
                            rep.issues.Add(new Issue(Severity.Warning, $"Tunnel {d.Tunnel.Id} chưa có hộp nào trong hàng chờ"));
                        foreach (AmmoBoxData b in d.Tunnel.Queue) rep.maxBox = Mathf.Max(rep.maxBox, b.Balls);
                    }
                }
            rep.totalBalls = m.TotalBalls;
            if (rep.totalBalls > 0 && rep.clickableAtStart == 0)
                rep.issues.Add(new Issue(Severity.Error, "Lúc bắt đầu không có hộp nào bấm được (hàng 0 trống / bị chặn / bị băng)"));

            if (m.Capacity <= 0) rep.issues.Add(new Issue(Severity.Error, "Capacity phải > 0"));
            else
            {
                if (rep.maxBox > m.Capacity)
                    rep.issues.Add(new Issue(Severity.Error, $"Có hộp {rep.maxBox} bi > capacity {m.Capacity}: không bao giờ bắn được"));
                else if (rep.maxBox * 2 > m.Capacity)
                    rep.issues.Add(new Issue(Severity.Warning, $"Capacity {m.Capacity} < 2 × hộp lớn nhất ({rep.maxBox}): rất dễ thua"));
                if (rep.totalBalls > 0 && m.Capacity >= rep.totalBalls)
                    rep.issues.Add(new Issue(Severity.Warning, $"Capacity {m.Capacity} ≥ tổng bi {rep.totalBalls}: không có áp lực sức chứa"));
            }
            if (colorsUsed.Count > 7) rep.issues.Add(new Issue(Severity.Warning, $"{colorsUsed.Count} màu – khá nhiều, cân nhắc giảm"));
            return rep;
        }
    }
}
