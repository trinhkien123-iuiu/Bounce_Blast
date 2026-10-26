using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Khay hộp đạn phía dưới bàn</summary>
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
        float[] _rowZ;
        float _pitchY;
        Vector2 _origin;

        [Tooltip("Mesh khay gốc HoleAll (Hole.asset, đặt ở đúng transform của game gốc). Dùng khi GameConfig.trayProcedural tắt: mỗi level dựng một bản là phẳng theo góc nhìn gốc, hộp đặt đúng tâm hố")]
        [SerializeField] MeshRenderer originalTray;
        Vector3 _trayBaseScale, _trayBasePos;
        const float WallSlide = 0.15f;
        const float WallLift = 0.05f;
        const float HolePitch = 1.5746125f;
        const float HolePlateZ = 0.71425885f;
        Transform _boxRoot;
        Transform _trayRoot;

        public float CellSize => _cell;

        GameConfig.Theme _theme;

        /// <summary>Mép dưới vùng khay (y thế giới, theo góc nhìn cũ như ammoAreaTop)</summary>
        public float AreaBottomOverride = float.NaN;

        /// <summary>Số cột/hàng khay thực sự hiển thị</summary>
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

        /// <summary>Xử lý dựng khay đạn và hộp theo level</summary>
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
            _origin = new Vector2(centerX - (cMin + cMax) * 0.5f * _cell, config.ammoAreaTop - _cell * 0.5f + ViewSpace.LegacyShift(-0.6f));
            _pitchY = _cell;
            _rowZ = null;

            _boxRoot = new GameObject("Boxes").transform;
            _boxRoot.SetParent(transform, false);
            _trayRoot = new GameObject("Tray").transform;
            _trayRoot.SetParent(transform, false);

            bool useOriginalTray = !config.trayProcedural && originalTray != null;
            if (useOriginalTray) BuildOriginalTray(cMin, cMax, centerX, visRows, availH);
            _tray = new AmmoTrayView(config, _theme, colors, _kind, _cell, _origin, _trayRoot, _pitchY, _rowZ);
            if (useOriginalTray) _tray.PlateRotation = originalTray.transform.rotation * Quaternion.Euler(180f, 0f, 0f);
            _tray.Build();
            if (useOriginalTray) _tray.BuildCover(config.wallKit, WallPiece);

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

        /// <summary>Dùng cho đổi ô khay sang toạ độ thế giới</summary>
        public Vector3 CellWorld(Vector2Int c) => new Vector3(_origin.x + c.x * _cell, _origin.y - c.y * _pitchY, BoxZ(c.y));

        /// <summary>Độ sâu đặt hộp: khay tự sinh = mặt hộp -0.6</summary>
        float BoxZ(int row)
        {
            if (_rowZ == null || _rowZ.Length == 0) return -0.6f;
            return _rowZ[Mathf.Clamp(row, 0, _rowZ.Length - 1)] + _config.trayBoxDepth * _cell;
        }

        /// <summary>Điểm (mặt hộp z = -0.6) nằm trong khu khay đạn</summary>
        public bool IsInAmmoArea(Vector2 world) => _boxes != null && world.y <= _origin.y + _cell;

        /// <summary>Khay đúng mesh gốc HoleAll</summary>
        Matrix4x4 WallPiece(int c, int r)
        {
            Transform t = originalTray.transform;
            float lift = WallLift * HolePitch;
            float slide = -WallSlide * lift * t.TransformDirection(Vector3.forward).y / t.TransformDirection(Vector3.up).y;
            return t.localToWorldMatrix * Matrix4x4.TRS(new Vector3((c - 3.5f) * HolePitch, (r - 3f) * HolePitch + slide, HolePlateZ + lift - 0.5f * HolePitch),
                                                        Quaternion.Euler(90f, 0f, 0f), Vector3.one * HolePitch);
        }

        void BuildOriginalTray(int cMin, int cMax, float centerX, int visRows, float availH)
        {
            Transform t = originalTray.transform;
            if (_trayBaseScale == Vector3.zero) { _trayBaseScale = t.localScale; _trayBasePos = t.position; }
            t.localScale = _trayBaseScale;
            t.position = _trayBasePos;
            float baseCell = Mathf.Abs(t.TransformPoint(new Vector3(HolePitch, 0f, HolePlateZ)).x - t.TransformPoint(new Vector3(0f, 0f, HolePlateZ)).x);
            float basePitchY = Mathf.Abs(t.TransformPoint(new Vector3(0f, HolePitch, HolePlateZ)).y - t.TransformPoint(new Vector3(0f, 0f, HolePlateZ)).y);
            float cellForHeight = availH / (visRows * basePitchY / baseCell);
            float cell = Mathf.Min(_config.ammoCellMax, cellForHeight, _config.ammoAreaWidth / (cMax - cMin + 1));
            float grow = cell / baseCell;
            Vector3 topEdge = new Vector3(0f, -3.5f * HolePitch, HolePlateZ);
            float topY = t.TransformPoint(topEdge).y;
            t.localScale = _trayBaseScale * grow;
            t.position += new Vector3(0f, topY - t.TransformPoint(topEdge).y, 0f);
            Vector3 pos = t.position;
            pos.x += centerX - (t.TransformPoint(new Vector3(((cMin + cMax) * 0.5f - 3.5f) * HolePitch, 0f, 0f)).x);
            t.position = pos;
            Vector3 Hole(int c, int r) => t.TransformPoint(new Vector3((c - 3.5f) * HolePitch, (r - 3f) * HolePitch, HolePlateZ));
            Vector3 h00 = Hole(0, 0), h10 = Hole(1, 0), h01 = Hole(0, 1);
            _origin = new Vector2(h00.x, h00.y);
            _cell = Mathf.Abs(h10.x - h00.x);
            _pitchY = Mathf.Abs(h00.y - h01.y);
            _rowZ = new float[_rows];
            for (int r = 0; r < _rows; r++) _rowZ[r] = Hole(0, r).z;
            if (_theme != null && _theme.trayHole != null) originalTray.sharedMaterial = _theme.trayHole;
        }

        AmmoBox PlaceBox(AmmoBoxData data, Vector2Int cell, bool fromTunnel)
        {
            var go = new GameObject($"Ammo_{data.ColorCode}_{cell.x}_{cell.y}");
            go.transform.SetParent(_boxRoot, false);
            go.transform.position = CellWorld(cell);
            var box = go.AddComponent<AmmoBox>();
            _tray?.RemoveLid(cell);
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
            go.transform.position = CellWorld(cell) + new Vector3(0f, 0f, _config.tunnelBackShift * _cell);   // lùi vào sau để tường khay che lên ống
            var tunnel = go.AddComponent<AmmoTunnel>();
            tunnel.Init(data, cell, _cell, _config, _colors);
            _tunnels[cell.x, cell.y] = tunnel;
        }

        bool InGrid(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < _cols && p.y < _rows;
        bool IsFreeCell(Vector2Int p) => InGrid(p) && _kind[p.x, p.y] == AmmoCellKind.Empty;

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
                    box.SetClickable(open && !box.IsFrozen && !box.IsDoorClosed, box.IsFrozen || (open && !box.IsDoorClosed), open, instant);
                }
        }

        /// <summary>Hộp ở chỗ vừa bấm. Một hộp "trúng" khi điểm bấm nằm trong khung hình thật của nó</summary>
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

        /// <summary>Lấy hộp ra khỏi khay (đã kiểm tra capacity ở GameManager)</summary>
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

        /// <summary>Hộp bấm được đầu tiên (hàng trên cùng trước)</summary>
        public Vector3 HoldSpotWorld()
        {
            float cx = _origin.x + (_cols - 1) * 0.5f * _cell;
            return new Vector3(cx, _origin.y - (_rows - 0.2f) * _pitchY, BoxZ(Mathf.Max(0, _rows - 1)));
        }

        /// <summary>Dùng cho tìm hộp bấm được đầu tiên</summary>
        public AmmoBox FirstClickable()
        {
            AmmoBox best = null;
            if (_boxes == null) return null;
            foreach (AmmoBox b in _boxes)
                if (b != null && b.IsClickable && (best == null || b.transform.position.y > best.transform.position.y)) best = b;
            return best;
        }

        /// <summary>Kiểm tra còn hộp nào bấm được</summary>
        public bool AnyClickable()
        {
            if (_boxes == null) return false;
            foreach (AmmoBox b in _boxes) if (b != null && b.IsClickable) return true;
            return false;
        }

        /// <summary>Vị trí (world) các hộp còn trong khay + tâm khay</summary>
        public List<Vector3> BoxPositions(out Vector3 center, out float radius)
        {
            var list = new List<Vector3>(); center = Vector3.zero; radius = _cell;
            if (_boxes == null) return list;
            foreach (AmmoBox b in _boxes) if (b != null && b.Data != null) { list.Add(CellWorld(b.Cell)); center += list[list.Count - 1]; }
            if (list.Count == 0) return list;
            center /= list.Count;
            foreach (Vector3 p in list) radius = Mathf.Max(radius, Vector3.Distance(p, center));
            radius += _cell * 0.6f;
            return list;
        }

        /// <summary>Xử lý xáo màu các hộp còn trong khay</summary>
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
                PlaceBox(data, cell, false).Pop();
            }
            Recompute(true);
        }

        /// <summary>Xử lý xoá toàn bộ khay và hộp</summary>
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
