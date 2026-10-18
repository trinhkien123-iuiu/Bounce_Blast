using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Khay hộp đạn phía dưới bàn (gốc: GridAmmo + GridOccupantAmmo): đặt hộp / tunnel vào ô, bấm & bắn hộp, truy vấn cho booster.
    /// Hình khay ở <see cref="AmmoTrayView"/>, luật thông đường ở <see cref="AmmoPathRules"/>.
    /// Luật (đối chiếu video): hộp đứng yên trong ô, KHÔNG trượt. Một hộp bấm được khi nó ở hàng sát lối ra
    /// (hàng 0) hoặc kề một ô trống có đường thông ra lối ra (BFS qua các ô trống).
    /// Mỗi lần bắn 1 hộp: băng trên mọi hộp khác nứt 1 nấc; hộp "?" lộ màu khi thông đường;
    /// tunnel nhả hộp kế tiếp vào ô phía trước khi ô đó trống.
    /// </summary>
    public class AmmoGridController : MonoBehaviour
    {
        public event Action<AmmoBox> OnBoxLaunched;

        GameConfig _config;
        ColorLibrary _colors;
        int _cols, _rows;
        AmmoCellKind[,] _kind;
        AmmoBox[,] _boxes;
        AmmoTunnel[,] _tunnels;
        readonly AmmoPathRules _paths = new AmmoPathRules();
        AmmoTrayView _tray;
        float _cell;
        Vector2 _origin; // tâm ô (0,0)
        Transform _boxRoot;
        Transform _trayRoot;

        public float CellSize => _cell;

        GameConfig.Theme _theme;

        /// <summary>Mép dưới vùng khay (y thế giới, theo góc nhìn cũ như ammoAreaTop) – GameManager đặt theo nút booster trên màn hình thật; NaN = dùng config.</summary>
        public float AreaBottomOverride = float.NaN;

        /// <summary>Số cột/hàng khay thực sự hiển thị (bỏ ô tường "/awl" ở rìa) – dùng chung cho Build và CameraFitter.</summary>
        public static Vector2Int VisibleExtent(LevelModel level)
        {
            int cMin = int.MaxValue, cMax = -1, rMax = 0;
            for (int r = 0; r < Mathf.Max(1, level.AmmoRows); r++)
                for (int c = 0; c < level.AmmoCols; c++)
                {
                    AmmoCellData d = level.AmmoCells[c, r];
                    if (d == null || d.Kind == AmmoCellKind.Wall) continue;
                    cMin = Mathf.Min(cMin, c); cMax = Mathf.Max(cMax, c); rMax = Mathf.Max(rMax, r);
                }
            return cMax < 0 ? new Vector2Int(1, 1) : new Vector2Int(cMax - cMin + 1, rMax + 1);
        }

        public void Build(LevelModel level, GameConfig config, ColorLibrary colors)
        {
            _config = config;
            _theme = config.ThemeFor(level.Difficulty);
            _colors = colors;
            _cols = level.AmmoCols;
            _rows = Mathf.Max(1, level.AmmoRows);
            _kind = new AmmoCellKind[_cols, _rows];
            _boxes = new AmmoBox[_cols, _rows];
            _tunnels = new AmmoTunnel[_cols, _rows];

            // Khung hiển thị: căn giữa & co ô theo các ô chơi được; ô "/awl" vẽ theo lưới, được phép tràn ra mép
            // màn hình / chìm dưới thanh booster như game gốc (ô gốc ≈ 1.7)
            int cMin = _cols, cMax = -1, rMax = 0;
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    AmmoCellData d = level.AmmoCells[c, r];
                    _kind[c, r] = d != null ? d.Kind : AmmoCellKind.Wall;
                    if (_kind[c, r] == AmmoCellKind.Wall) continue;
                    cMin = Mathf.Min(cMin, c); cMax = Mathf.Max(cMax, c); rMax = Mathf.Max(rMax, r);
                }
            if (cMax < 0) { cMin = 0; cMax = 0; }

            int visCols = cMax - cMin + 1, visRows = rMax + 1;
            float availH = config.ammoAreaTop - (float.IsNaN(AreaBottomOverride) ? config.ammoAreaBottom : AreaBottomOverride);
            _cell = Mathf.Min(config.ammoCellMax, availH / visRows, config.ammoAreaWidth / visCols);
            float centerX = config.arena.center.x;
            // ammoAreaTop đo theo góc nhìn cũ ở mặt hộp z = -0.6 → chuyển sang camera thẳng
            _origin = new Vector2(centerX - (cMin + cMax) * 0.5f * _cell, config.ammoAreaTop - _cell * 0.5f + ViewSpace.LegacyShift(-0.6f));

            _boxRoot = new GameObject("Boxes").transform;
            _boxRoot.SetParent(transform, false);
            _trayRoot = new GameObject("Tray").transform;
            _trayRoot.SetParent(transform, false);

            _tray = new AmmoTrayView(config, _theme, colors, _kind, _cell, _origin, _trayRoot);
            _tray.Build();

            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    if (_kind[c, r] == AmmoCellKind.Wall) continue;
                    AmmoCellData d = level.AmmoCells[c, r];
                    if (d.Kind == AmmoCellKind.Box) PlaceBox(d.Box, new Vector2Int(c, r), false);
                    else if (d.Kind == AmmoCellKind.Tunnel) PlaceTunnel(d.Tunnel, new Vector2Int(c, r));
                }

            Recompute(true);
        }

        public Vector3 CellWorld(Vector2Int c) => new Vector3(_origin.x + c.x * _cell, _origin.y - c.y * _cell, -0.6f);

        AmmoBox PlaceBox(AmmoBoxData data, Vector2Int cell, bool fromTunnel)
        {
            var go = new GameObject($"Ammo_{data.ColorCode}_{cell.x}_{cell.y}");
            go.transform.SetParent(_boxRoot, false);
            go.transform.position = CellWorld(cell);
            var box = go.AddComponent<AmmoBox>();
            _tray?.RemoveLid(cell); // hộp mới vào ô (tunnel nhả ra…) → bỏ nắp
            box.Init(data, cell, _cell, _config, _colors);
            if (box.HasDoor && _tray != null) _tray.FitDoorCover(box, cell);
            _boxes[cell.x, cell.y] = box;
            _kind[cell.x, cell.y] = AmmoCellKind.Box;
            return box;
        }

        void PlaceTunnel(AmmoTunnelData data, Vector2Int cell)
        {
            var go = new GameObject($"Tunnel_{data.Id}");
            go.transform.SetParent(transform, false);
            go.transform.position = CellWorld(cell);
            var tunnel = go.AddComponent<AmmoTunnel>();
            tunnel.Init(data, cell, _cell, _config, _colors);
            _tunnels[cell.x, cell.y] = tunnel;
        }

        bool InGrid(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < _cols && p.y < _rows;
        bool IsFreeCell(Vector2Int p) => InGrid(p) && _kind[p.x, p.y] == AmmoCellKind.Empty;

        // ------------------------------------------------------------------ Luật thông đường (AmmoPathRules)
        void Recompute(bool instant = false)
        {
            _paths.Compute(_kind);
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    AmmoBox box = _boxes[c, r];
                    if (box == null) continue;
                    bool open = _paths.IsOpen(new Vector2Int(c, r));
                    if (open && box.IsHidden) box.Reveal();
                    box.SetClickable(open && !box.IsFrozen && !box.IsDoorClosed, instant);
                }
        }

        // ------------------------------------------------------------------ Bấm / bắn hộp
        /// <summary>
        /// Hộp ở chỗ vừa bấm. Một hộp "trúng" khi điểm bấm nằm trong khung hình thật của nó (thân jelly nhô khỏi hố)
        /// HOẶC trong ô lưới của nó (để hộp đã lún vẫn bấm được, bị từ chối có lắc). Nhiều hộp cùng trúng thì lấy hộp
        /// có tâm hình gần chỗ bấm nhất. Không suy ra ô từ toạ độ: hộp được VẼ lệch khỏi tâm ô theo
        /// GameConfig.ammoActivePose nên cách đó trượt; và cũng không dùng gốc xoay của model vì nó lệch khỏi phần nhìn thấy.
        /// </summary>
        public AmmoBox BoxAtWorld(Vector2 world)
        {
            if (_boxes == null) return null;
            AmmoBox best = null;
            float bestDist = float.MaxValue;
            float half = _cell * 0.5f;
            foreach (AmmoBox b in _boxes)
            {
                if (b == null || b.IsLaunched) continue;
                Vector3 cell = CellWorld(b.Cell);
                bool inCell = Mathf.Abs(world.x - cell.x) <= half && Mathf.Abs(world.y - cell.y) <= half;
                Vector2 center = cell;
                bool inVisible = false;
                if (b.TryGetVisibleBounds(out Bounds vb))
                {
                    inVisible = world.x >= vb.min.x && world.x <= vb.max.x && world.y >= vb.min.y && world.y <= vb.max.y;
                    center = vb.center;
                }
                if (!inCell && !inVisible) continue;
                float d = Mathf.Max(Mathf.Abs(world.x - center.x), Mathf.Abs(world.y - center.y));
                if (d < bestDist) { bestDist = d; best = b; }
            }
            return best;
        }

        /// <summary>Lấy hộp ra khỏi khay (đã kiểm tra capacity ở GameManager). forced = booster Magic Hand.</summary>
        public void Launch(AmmoBox box, bool forced = false)
        {
            if (box == null || box.IsLaunched) return;
            if (!forced && !box.IsClickable) return;

            Vector2Int cell = box.Cell;
            _boxes[cell.x, cell.y] = null;
            _kind[cell.x, cell.y] = AmmoCellKind.Empty;
            if (box.IsHidden) box.Reveal();
            _tray?.SpawnHoleLid(cell, box.Data.ColorCode);
            box.BeginLaunch();
            box.transform.SetParent(transform.parent, true);

            // Băng chỉ nứt khi hộp KỀ (dấu cộng: trên/dưới/trái/phải) được bắn; cửa kính thì mọi cửa đảo trạng thái.
            foreach (AmmoBox other in _boxes) if (other != null) other.ToggleDoor();
            foreach (Vector2Int d in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
            {
                Vector2Int n = cell + d;
                if (InGrid(n) && _boxes[n.x, n.y] != null) _boxes[n.x, n.y].CrackIce();
            }

            OnBoxLaunched?.Invoke(box);
            StepTunnels();
            Recompute();
        }

        void StepTunnels()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (AmmoTunnel t in _tunnels)
                {
                    if (t == null || t.Remaining == 0 || !IsFreeCell(t.OutputCell)) continue;
                    AmmoBoxData data = t.Pop();
                    AmmoBox box = PlaceBox(data, t.OutputCell, true);
                    box.transform.position = CellWorld(t.Cell);
                    box.Flight.MoveTo(CellWorld(t.OutputCell), 0.25f);
                    changed = true;
                }
            }
        }

        // Truy vấn cho GameManager / booster
        public bool AnyClickable()
        {
            if (_boxes == null) return false;
            foreach (AmmoBox b in _boxes) if (b != null && b.IsClickable) return true;
            return false;
        }

        public int RemainingBoxes()
        {
            int n = 0;
            if (_boxes != null)
            {
                foreach (AmmoBox b in _boxes) if (b != null) n++;
            }
            if (_tunnels != null)
            {
                foreach (AmmoTunnel t in _tunnels) if (t != null) n += t.Remaining;
            }
            return n;
        }

        public bool AnyRemainingOfColorsNeeded(Func<string, bool> needed)
        {
            if (_boxes != null)
            {
                foreach (AmmoBox b in _boxes) if (b != null && b.Data != null && needed != null && needed(b.Data.ColorCode)) return true;
            }
            if (_tunnels != null)
            {
                foreach (AmmoTunnel t in _tunnels)
                    if (t != null && t.Data != null && t.Data.Queue != null && needed != null)
                        foreach (AmmoBoxData d in t.Data.Queue) if (d != null && needed(d.ColorCode)) return true;
            }
            return false;
        }

        /// <summary>Booster Shuffle: xáo màu/số bi giữa các hộp còn trong khay (giữ nguyên vị trí, băng, "?").</summary>
        public void ShuffleColors()
        {
            if (_boxes == null) return;
            var list = new List<AmmoBox>();
            foreach (AmmoBox b in _boxes) if (b != null && b.Data != null) list.Add(b);
            var datas = new List<(string, int)>();
            foreach (AmmoBox b in list) datas.Add((b.Data.ColorCode, b.Data.Balls));
            for (int i = datas.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (datas[i], datas[j]) = (datas[j], datas[i]);
            }
            for (int i = 0; i < list.Count; i++)
            {
                AmmoBox old = list[i];
                if (old == null || old.Data == null) continue;
                var data = new AmmoBoxData { ColorCode = datas[i].Item1, Balls = datas[i].Item2, IceHits = old.Data.IceHits, Hidden = old.Data.Hidden };
                Vector2Int cell = old.Cell;
                Destroy(old.gameObject);
                PlaceBox(data, cell, false);
            }
            Recompute(true);
        }

        public void ClearAll()
        {
            foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _tray = null;
            _boxes = null;
            _tunnels = null;
            _kind = null;
        }
    }
}
