using System;
using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Khay hộp đạn phía dưới bàn (gốc: GridAmmo + GridOccupantAmmo).
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
        bool[,] _reach;
        float _cell;
        Vector2 _origin; // tâm ô (0,0)
        readonly List<GameObject> _tiles = new List<GameObject>();
        Transform _boxRoot;
        Transform _trayRoot;

        public float CellSize => _cell;

        GameConfig.Theme _theme;

        /// <summary>Mép dưới vùng khay (y thế giới, theo góc nhìn cũ như ammoAreaTop) – GameManager đặt theo nút booster trên màn hình thật; NaN = dùng config.</summary>
        public float AreaBottomOverride = float.NaN;

        /// <summary>Số cột/hàng khay thực sự hiển thị (bỏ ô tường "/awl" ở rìa) – dùng chung cho Build và GameManager.FitCamera.</summary>
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
            _reach = new bool[_cols, _rows];

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

            BuildTray(level);

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

        // ---------- Khay ----------
        // Ảnh/video gốc: dưới thanh capacity là mảng tối liền (màu tường t4-wall), trên đó một mặt khay sáng
        // phẳng, liền khối, bo góc ôm đúng hình các ô chơi được (chữ T, chữ L…); mỗi ô là một LỖ bo tròn
        // (đáy sáng nằm sâu phía sau) – hộp chưa thông đường lún trong lỗ, chỉ lộ nắp (AmmoBox.SinkDepth). Bộ autotile 3D gốc (ModularPrimitiveAmmoWall) có gờ/khe nối lộ ra
        // khi nhìn nghiêng nên khay được dựng thành mesh phẳng; vật liệu vẫn lấy từ asset gốc
        // (M-Plane theo theme cho mặt khay, t4-wall của prefab cho nền).
        const string TilePrefabPath = "prefabs/ModularPrimitiveAmmoWall - Wallbase";
        const float TrayPanelZ = 0.35f;  // sau lưng hộp jelly (hộp ở z = -0.6, nghiêng)
        const float TrayFloorZ = 3.0f;   // đáy giếng: sau cả hộp có cửa đóng (SinkDepth 1.1 + DoorExtraSink 1.6, cột dài ~1.5 ô)
        const float PanelPad = 0.10f;    // viền mặt khay chìa ra ngoài ô (theo cell)
        const float PanelRadius = 0.22f; // bo góc ngoài (theo cell)
        const float SlotSize = 0.92f, SlotRadius = 0.2f; // đo từ ảnh gốc: gờ giữa 2 hố chỉ ~0.08 ô, hộp rộng ~0.8 ô nằm trong hố
        const float HoleTopInset = 0f; // theo cell – miệng hố vuông vức như thân hộp

        float PanelZ => -0.6f + _cell * (TrayPanelZ + 0.6f);

        Material _trayTileMat, _trayWallMat, _trayBaseMat;
        readonly Dictionary<Vector2Int, AmmoHoleLid> _lids = new Dictionary<Vector2Int, AmmoHoleLid>();
        readonly Dictionary<string, Material> _jellyMats = new Dictionary<string, Material>();

        /// <summary>Hố vừa trống: thạch nhỏ giọt rồi nắp trượt lên đậy (video gốc).</summary>
        void SpawnHoleLid(Vector2Int cell, string colorCode)
        {
            RemoveLid(cell);
            if (_trayTileMat == null || _trayWallMat == null) return;
            float z = PanelZ + 0.012f; // ngay sau vành mặt khay → vành che mép
            const float dy = 0f; // camera thẳng: các lớp khay cùng x,y là thẳng hàng
            Vector3 c = CellWorld(cell);
            var go = new GameObject($"HoleLid_{cell.x}_{cell.y}");
            go.transform.SetParent(_trayRoot, false);
            go.transform.position = new Vector3(c.x, c.y + dy, z);
            float s = _cell * SlotSize * 0.5f, top = s - HoleTopInset * _cell;
            if (!_jellyMats.TryGetValue(colorCode ?? "", out Material jm) || jm == null)
            {
                jm = new Material(_trayBaseMat != null ? _trayBaseMat : _trayTileMat) { name = "HoleJelly " + colorCode };
                if (jm.HasProperty("_BaseColor")) jm.SetColor("_BaseColor", _colors.ColorOf(colorCode));
                _jellyMats[colorCode ?? ""] = jm;
            }
            var lid = go.AddComponent<AmmoHoleLid>();
            lid.Init(_cell, s, -s, top, _cell * SlotRadius, _trayWallMat, jm, _trayTileMat);
            _lids[cell] = lid;
            _tiles.Add(go);
        }

        void RemoveLid(Vector2Int cell)
        {
            if (_lids.TryGetValue(cell, out AmmoHoleLid l) && l != null) Destroy(l.gameObject);
            _lids.Remove(cell);
        }

        /// <summary>Tâm & kích thước miệng lỗ của ô (đã bù độ nghiêng camera như CreateFlat) – cho tấm kính cửa.</summary>
        void HoleRect(Vector2Int cell, out Vector3 center, out Vector2 size)
        {
            float s = _cell * SlotSize * 0.5f, top = s - HoleTopInset * _cell;
            float z = PanelZ - 0.01f; // ngay trước vành mặt khay; hộp có cửa đóng lún hẳn ra sau mặt khay (AmmoBox)
            const float dy = 0f; // camera thẳng: các lớp khay cùng x,y là thẳng hàng
            Vector3 c = CellWorld(cell);
            center = new Vector3(c.x, c.y + (top - s) * 0.5f + dy, z);
            size = new Vector2(2f * s, top + s);
        }

        void BuildTray(LevelModel level)
        {
            // Vật liệu theo ArtConfig gốc: type 7 = mặt khay, type 8 = tường/nền tối, type 5 = lỗ ô
            Material baseMat = _theme != null && _theme.trayPanel != null ? _theme.trayPanel
                             : _theme != null && _theme.ammoCell != null ? _theme.ammoCell : _config.ammoCellMaterial;
            Material wallMat = _theme != null ? _theme.trayWall : null;
            if (wallMat == null)
            {
                var prefab = Resources.Load<GameObject>(TilePrefabPath);
                if (prefab != null)
                    foreach (Renderer rend in prefab.GetComponentsInChildren<Renderer>(true))
                        if (rend.name == "4xRounded") { wallMat = rend.sharedMaterial; break; } // t4-wall
                if (wallMat == null) wallMat = baseMat;
            }
            // Ảnh gốc (coin_panel.mp4, khay trống): mỗi ô là một viên gạch bo tròn SÁNG hơn mặt khay (77,86,197 so với
            // rãnh 70,78,176) → đáy lỗ dùng màu mặt khay sáng thêm ~13% (không dùng Hole-x vì tối hơn mặt khay).
            Material slotMat = null;
            if (baseMat != null)
            {
                slotMat = new Material(baseMat) { name = baseMat.name + " (Tile)" };
                if (slotMat.HasProperty("_BaseColor")) slotMat.SetColor("_BaseColor", slotMat.GetColor("_BaseColor") * 1.2f);
            }
            // Gờ (mặt trước) của khối tường tối nổi lên: dải màu trung gian ngay dưới mép dưới tường
            Material lipMat = null;
            if (baseMat != null && wallMat != null && baseMat.HasProperty("_BaseColor") && wallMat.HasProperty("_BaseColor"))
            {
                lipMat = new Material(baseMat) { name = "Tray Lip" };
                lipMat.SetColor("_BaseColor", Color.Lerp(wallMat.GetColor("_BaseColor"), baseMat.GetColor("_BaseColor"), 0.7f));
            }

            _trayTileMat = slotMat; _trayWallMat = wallMat; _trayBaseMat = baseMat;
            Func<int, int, bool> emptyAt = (c, r) => c >= 0 && r >= 0 && c < _cols && r < _rows && _kind[c, r] == AmmoCellKind.Empty;
            Func<int, int, bool> isSlot = (c, r) => c >= 0 && r >= 0 && c < _cols && r < _rows && _kind[c, r] != AmmoCellKind.Wall;

            // Nền tối nằm CÙNG độ sâu với mặt khay (ngay sau nó), khoét lỗ đúng các ô slot: cùng mặt khay che phần
            // hộp đang lún vượt ra ngoài lỗ (ảnh gốc: hộp chưa thông đường chỉ lộ nắp).
            float panelZ = -0.6f + _cell * (TrayPanelZ + 0.6f);
            float backZ = panelZ + 0.02f;
            // bù độ hạ y của CreateFlat để mép trên nền tối vẫn nằm ngay dưới thanh capacity
            float top = _origin.y + _cell * 0.5f + 0.15f, bottom = top - 30f;
            {
                float hc = _cell * 0.5f;
                Vector2 c00 = CellWorld(new Vector2Int(0, 0)), cNN = CellWorld(new Vector2Int(_cols - 1, _rows - 1));
                float gx0 = c00.x - hc, gx1 = cNN.x + hc, gy1 = c00.y + hc, gy0 = cNN.y - hc;
                float L = -40f, R = 40f;
                // Tường tối là khối nổi: lớp gờ (mặt trước, màu trung gian) phủ toàn vùng tường phía sau; lớp mặt trên tối
                // cùng hình nhưng mép dưới nâng lên ở chỗ giáp ô gạch → mép dưới tường lộ dải gờ như ảnh gốc.
                var lipMesh = new MeshBuilder();
                var topMesh = new MeshBuilder();
                float raise = _cell * (PanelPad + 0.14f); // mặt khay chìa vào tường PanelPad → gờ lộ ra 0.12 ô
                Action<float, float, float, float, bool> quad = (x0, y0, x1, y1, lipBelow) =>
                {
                    if (x1 - x0 <= 0f || y1 - y0 <= 0f) return;
                    lipMesh.AddPolygon(new List<Vector2> { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) }, Vector2.zero);
                    float ty0 = y0; // camera thẳng: bỏ dải gờ giả 3D (nhìn như một lớp thứ hai đè dưới mép tường tối)
                    topMesh.AddPolygon(new List<Vector2> { new Vector2(x0, ty0), new Vector2(x1, ty0), new Vector2(x1, y1), new Vector2(x0, y1) }, Vector2.zero);
                };
                quad(L, bottom, gx0, top, false);                // trái lưới
                quad(gx1, bottom, R, top, false);                // phải lưới
                for (int c = 0; c < _cols; c++)                  // trên lưới: từng cột, gờ chỉ ở chỗ giáp ô gạch hàng 0
                {
                    float cx = CellWorld(new Vector2Int(c, 0)).x;
                    quad(cx - hc, gy1, cx + hc, top, isSlot(c, 0));
                }
                quad(gx0, bottom, gx1, gy0, false);              // dưới lưới
                for (int r = 0; r < _rows; r++)
                    for (int c = 0; c < _cols; c++)
                    {
                        if (isSlot(c, r)) continue;
                        Vector2 p = CellWorld(new Vector2Int(c, r));
                        quad(p.x - hc, p.y - hc, p.x + hc, p.y + hc, isSlot(c, r + 1));
                    }
                CreateFlat("TrayWallLip", lipMesh.Build(), lipMat != null ? lipMat : wallMat, backZ + 0.01f);
                CreateFlat("Backdrop", topMesh.Build(), wallMat, backZ);
            }

            var panel = new MeshBuilder();
            var slots = new MeshBuilder();
            var lips = new MeshBuilder();
            float h = _cell * 0.5f, pad = _cell * PanelPad, rad = _cell * PanelRadius;
            float s = _cell * SlotSize * 0.5f, sr = _cell * SlotRadius, lipH = _cell * 0.11f;
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    if (!isSlot(c, r)) continue;
                    Vector2 ctr = CellWorld(new Vector2Int(c, r));
                    bool L = !isSlot(c - 1, r), R = !isSlot(c + 1, r), T = !isSlot(c, r - 1), B = !isSlot(c, r + 1);
                    float x0 = -h - (L ? pad : 0f), x1 = h + (R ? pad : 0f), y0 = -h - (B ? pad : 0f), y1 = h + (T ? pad : 0f);
                    // Ô trống "-" (không có hộp/tunnel): ảnh gốc vẽ thành một mảng tối liền, các ô trống kề nhau KHÔNG có gờ ngăn
                    bool eL = emptyAt(c - 1, r), eR = emptyAt(c + 1, r), eT = emptyAt(c, r - 1), eB = emptyAt(c, r + 1);
                    bool me = emptyAt(c, r);
                    float ix0 = me && eL ? -h : -s, ix1 = me && eR ? h : s, iy1 = me && eT ? h : s, iy0 = me && eB ? -h : -s;
                    float rBR = (me && (eR || eB)) ? 0f : sr, rTR = (me && (eR || eT)) ? 0f : sr, rTL = (me && (eL || eT)) ? 0f : sr, rBL = (me && (eL || eB)) ? 0f : sr;
                    panel.AddRing(RoundedRect(x0, y0, x1, y1, B && R ? rad : 0f, T && R ? rad : 0f, T && L ? rad : 0f, B && L ? rad : 0f),
                                  RoundedRect(ix0, iy0, ix1, iy1, rBR, rTR, rTL, rBL), ctr);
                    // đáy lỗ = viên gạch sáng bo tròn (phủ kín ô để nhìn nghiêng qua lỗ không lọt nền tối)
                    slots.AddPolygon(RoundedRect(-h, -h, h, h, 0f, 0f, 0f, 0f), ctr);
                }
            // Góc lõm của khay (góc lồi của tường) bo tròn như ảnh gốc: lấp phần góc vuông ngoài cung tròn bằng màu mặt khay
            for (int r = -1; r <= _rows; r++)
                for (int c = -1; c <= _cols; c++)
                {
                    if (isSlot(c, r)) continue;
                    Vector2 ctr = CellWorld(new Vector2Int(c, r));
                    for (int dx = -1; dx <= 1; dx += 2)
                        for (int dy = -1; dy <= 1; dy += 2)
                        {
                            if (!isSlot(c + dx, r) || !isSlot(c, r + dy)) continue;
                            // dy lưới +1 = xuống dưới (world -y)
                            Vector2 P = ctr + new Vector2(dx * (h - pad), -dy * (h - pad));
                            Vector2 Q = P + new Vector2(-dx * rad, dy * rad);
                            var fan = new List<Vector2> { P };
                            const int steps = 6;
                            for (int i = 0; i <= steps; i++)
                            {
                                float a = i / (float)steps * Mathf.PI * 0.5f;
                                fan.Add(Q + new Vector2(dx * Mathf.Sin(a), -dy * Mathf.Cos(a)) * rad);
                            }
                            panel.AddFan(fan);
                        }
                }
            CreateFlat("TrayPanel", panel.Build(), baseMat, panelZ);
            // Lòng hố = giếng tối (ảnh gốc Lv44: hộp nằm trong hố tối, ô trống cũng tối); nắp AmmoHoleLid mới mang màu gạch sáng
            GameObject floorGo = CreateFlat("TraySlotFloors", slots.Build(), wallMat, -0.6f + _cell * (TrayFloorZ + 0.6f));
            // đáy giếng thẳng dưới miệng hố (cùng độ lệch y với mặt khay)
            // Mỗi ô là một ngăn (giếng) độc lập: 4 vách nối miệng hố (mặt khay) xuống đáy → nhìn nghiêng không thấy rỗng/thông nhau
            if (wallMat != null)
            {
                Material well = new Material(wallMat) { name = "Tray Well" };
                if (well.HasProperty("_BaseColor")) well.SetColor("_BaseColor", wallMat.GetColor("_BaseColor") * 0.8f);
                var wv = new List<Vector3>(); var wt = new List<int>(); var wn = new List<Vector3>();
                float zTop = panelZ, zBot = -0.6f + _cell * (TrayFloorZ + 0.6f);
                const float tan = 0f;
                // vách thẳng góc mặt khay (ngăn thật): nhìn nghiêng từ trên thấy vách trên của giếng che phần hộp phía sau
                float dyTop = -(zTop + 0.6f) * tan, dyBot = dyTop;
                for (int r = 0; r < _rows; r++)
                    for (int c = 0; c < _cols; c++)
                    {
                        if (!isSlot(c, r) || emptyAt(c, r)) continue;
                        Vector2 ctr = CellWorld(new Vector2Int(c, r));
                        List<Vector2> ring = RoundedRect(-s, -s, s, s, sr, sr, sr, sr);
                        int o = wv.Count, n = ring.Count;
                        foreach (Vector2 p in ring) { wv.Add(new Vector3(ctr.x + p.x, ctr.y + p.y + dyTop, zTop)); wn.Add(-(Vector3)p.normalized); }
                        foreach (Vector2 p in ring) { wv.Add(new Vector3(ctr.x + p.x, ctr.y + p.y + dyBot, zBot)); wn.Add(-(Vector3)p.normalized); }
                        for (int i = 0; i < n; i++)
                        {
                            int j = (i + 1) % n;
                            wt.Add(o + i); wt.Add(o + j); wt.Add(o + n + j);
                            wt.Add(o + i); wt.Add(o + n + j); wt.Add(o + n + i);
                            wt.Add(o + i); wt.Add(o + n + j); wt.Add(o + j);
                            wt.Add(o + i); wt.Add(o + n + i); wt.Add(o + n + j);
                        }
                    }
                var wm = new Mesh { name = "TrayWells", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                wm.SetVertices(wv); wm.SetNormals(wn); wm.SetTriangles(wt, 0); wm.RecalculateBounds();
                var wgo = new GameObject("TrayWells");
                wgo.transform.SetParent(_trayRoot, false);
                wgo.transform.position = Vector3.zero; wgo.transform.rotation = Quaternion.identity;
                wgo.AddComponent<MeshFilter>().sharedMesh = wm;
                var wmr = wgo.AddComponent<MeshRenderer>();
                wmr.sharedMaterial = well;
                wmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _tiles.Add(wgo);
            }
        }

        GameObject CreateFlat(string name, Mesh mesh, Material mat, float z)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_trayRoot, false);
            go.transform.position = new Vector3(0f, 0f, z); // camera thẳng: không cần bù độ sâu
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _tiles.Add(go);
            return go;
        }

        /// <summary>Đa giác lồi hình chữ nhật bo góc (thứ tự ngược chiều kim đồng hồ, bán kính từng góc: BR, TR, TL, BL).</summary>
        static List<Vector2> RoundedRect(float x0, float y0, float x1, float y1, float rBR, float rTR, float rTL, float rBL)
        {
            var pts = new List<Vector2>();
            AddCorner(pts, new Vector2(x1, y0), rBR, -1f, 1f, 270f);
            AddCorner(pts, new Vector2(x1, y1), rTR, -1f, -1f, 0f);
            AddCorner(pts, new Vector2(x0, y1), rTL, 1f, -1f, 90f);
            AddCorner(pts, new Vector2(x0, y0), rBL, 1f, 1f, 180f);
            return pts;
        }

        static void AddCorner(List<Vector2> pts, Vector2 corner, float r, float sx, float sy, float startDeg)
        {
            const int steps = 6;
            // góc vuông vẫn sinh đủ steps+1 điểm (trùng nhau) để vành ngoài/vành trong của AddRing khớp số đỉnh
            if (r <= 0f) { for (int i = 0; i <= steps; i++) pts.Add(corner); return; }
            Vector2 c = corner + new Vector2(sx * r, sy * r);
            for (int i = 0; i <= steps; i++)
            {
                float a = (startDeg + 90f * i / steps) * Mathf.Deg2Rad;
                pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
        }

        /// <summary>Gom nhiều đa giác lồi phẳng (mặt quay về camera, -z) vào một mesh.</summary>
        class MeshBuilder
        {
            readonly List<Vector3> _v = new List<Vector3>();
            readonly List<int> _t = new List<int>();

            public void AddPolygon(List<Vector2> pts, Vector2 offset)
            {
                int center = _v.Count;
                Vector2 sum = Vector2.zero;
                foreach (Vector2 p in pts) sum += p;
                _v.Add(sum / pts.Count + offset);
                foreach (Vector2 p in pts) _v.Add(p + offset);
                for (int i = 0; i < pts.Count; i++)
                {
                    int a = center + 1 + i, b = center + 1 + (i + 1) % pts.Count;
                    _t.Add(center); _t.Add(b); _t.Add(a); // nhìn từ -z: chiều kim đồng hồ = mặt trước
                }
            }

            /// <summary>Quạt tam giác từ pts[0] (đa giác hình sao quanh đỉnh đầu), vẽ cả 2 mặt.</summary>
            public void AddFan(List<Vector2> pts)
            {
                int o = _v.Count;
                foreach (Vector2 p in pts) _v.Add(p);
                for (int i = 1; i + 1 < pts.Count; i++)
                {
                    _t.Add(o); _t.Add(o + i); _t.Add(o + i + 1);
                    _t.Add(o); _t.Add(o + i + 1); _t.Add(o + i);
                }
            }

            /// <summary>Vành giữa 2 đa giác cùng số đỉnh, cùng chiều (ngoài/trong) – dùng cho mặt khay có lỗ.</summary>
            public void AddRing(List<Vector2> outer, List<Vector2> inner, Vector2 offset)
            {
                int n = outer.Count, o = _v.Count, i0 = o + n;
                foreach (Vector2 p in outer) _v.Add(p + offset);
                foreach (Vector2 p in inner) _v.Add(p + offset);
                for (int i = 0; i < n; i++)
                {
                    int j = (i + 1) % n;
                    _t.Add(o + i); _t.Add(i0 + i); _t.Add(i0 + j);
                    _t.Add(o + i); _t.Add(i0 + j); _t.Add(o + j);
                }
            }

            public Mesh Build()
            {
                var m = new Mesh { name = "TrayFlat" };
                m.SetVertices(_v);
                var normals = new List<Vector3>(_v.Count);
                var uvs = new List<Vector2>(_v.Count);
                for (int i = 0; i < _v.Count; i++) { normals.Add(Vector3.back); uvs.Add(Vector2.zero); }
                m.SetNormals(normals);
                m.SetUVs(0, uvs);
                m.SetTriangles(_t, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        AmmoBox PlaceBox(AmmoBoxData data, Vector2Int cell, bool fromTunnel)
        {
            var go = new GameObject($"Ammo_{data.ColorCode}_{cell.x}_{cell.y}");
            go.transform.SetParent(_boxRoot, false);
            go.transform.position = CellWorld(cell);
            var box = go.AddComponent<AmmoBox>();
            RemoveLid(cell); // hộp mới vào ô (tunnel nhả ra…) → bỏ nắp
            box.Init(data, cell, _cell, _config, _colors);
            if (box.HasDoor) { HoleRect(cell, out Vector3 hc, out Vector2 hs); box.SetCoverPlacement(hc, hs, _cell * SlotRadius); }
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

        // ------------------------------------------------------------------ Luật thông đường
        static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        void Recompute(bool instant = false)
        {
            // BFS từ lối ra (phía trên hàng 0) qua các ô trống
            Array.Clear(_reach, 0, _reach.Length);
            var q = new Queue<Vector2Int>();
            for (int c = 0; c < _cols; c++)
                if (_kind[c, 0] == AmmoCellKind.Empty) { _reach[c, 0] = true; q.Enqueue(new Vector2Int(c, 0)); }
            while (q.Count > 0)
            {
                Vector2Int p = q.Dequeue();
                foreach (Vector2Int d in Dirs)
                {
                    Vector2Int n = p + d;
                    if (!IsFreeCell(n) || _reach[n.x, n.y]) continue;
                    _reach[n.x, n.y] = true;
                    q.Enqueue(n);
                }
            }

            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    AmmoBox box = _boxes[c, r];
                    if (box == null) continue;
                    bool open = IsOpen(new Vector2Int(c, r));
                    if (open && box.IsHidden) box.Reveal();
                    box.SetClickable(open && !box.IsFrozen && !box.IsDoorClosed, instant);
                }
        }

        bool IsOpen(Vector2Int p)
        {
            if (p.y == 0) return true;
            foreach (Vector2Int d in Dirs)
            {
                Vector2Int n = p + d;
                if (InGrid(n) && _reach[n.x, n.y]) return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ Bấm / bắn hộp
        public AmmoBox BoxAtWorld(Vector2 world)
        {
            int c = Mathf.RoundToInt((world.x - _origin.x) / _cell);
            int r = Mathf.RoundToInt((_origin.y - world.y) / _cell);
            var p = new Vector2Int(c, r);
            return InGrid(p) ? _boxes[c, r] : null;
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
            SpawnHoleLid(cell, box.Data.ColorCode);
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
                    box.MoveTo(CellWorld(t.OutputCell), 0.25f);
                    changed = true;
                }
            }
        }

        // ------------------------------------------------------------------ Truy vấn cho GameManager / booster
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
            _tiles.Clear();
            _lids.Clear();
            _boxes = null;
            _tunnels = null;
            _kind = null;
            _reach = null;
        }
    }
}
