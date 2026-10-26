using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BounceBlast.Gameplay
{
    /// <summary>HÌNH của khay hộp đạn (không có luật)</summary>
    public class AmmoTrayView
    {
        readonly GameConfig _config;
        readonly GameConfig.Theme _theme;
        readonly ColorLibrary _colors;
        readonly AmmoCellKind[,] _kind;
        readonly int _cols, _rows;
        readonly float _cell;
        readonly float _pitchY;
        readonly float[] _rowZ;
        readonly Vector2 _origin;
        readonly Transform _trayRoot;
        readonly List<GameObject> _tiles = new List<GameObject>();

        public AmmoTrayView(GameConfig config, GameConfig.Theme theme, ColorLibrary colors, AmmoCellKind[,] kind,
                            float cell, Vector2 origin, Transform root, float pitchY = 0f, float[] rowZ = null)
        {
            _pitchY = pitchY > 0f ? pitchY : cell;
            _rowZ = rowZ;
            _config = config;
            _theme = theme;
            _colors = colors;
            _kind = (AmmoCellKind[,])kind.Clone();
            _cols = kind.GetLength(0);
            _rows = kind.GetLength(1);
            _cell = cell;
            _origin = origin;
            _trayRoot = root;
        }

        Vector3 CellWorld(Vector2Int c) => new Vector3(_origin.x + c.x * _cell, _origin.y - c.y * _pitchY, -0.6f);

        /// <summary>Khay gốc (mesh HoleAll đặt sẵn trong scene, lộ đủ lưới 8×7 hố)</summary>
        public void BuildCover(WallKit kit, Func<int, int, Matrix4x4> wallPiece)
        {
            Material wallMat = _theme != null ? _theme.trayWall : null;
            if (wallMat == null && _config.ammoTrayWallPrefab != null)
                foreach (Renderer rend in _config.ammoTrayWallPrefab.GetComponentsInChildren<Renderer>(true))
                    if (rend.name == "4xRounded") { wallMat = rend.sharedMaterial; break; }
            if (kit == null || wallMat == null) return;
            Func<int, int, bool> isSlot = (c, r) => c >= 0 && r >= 0 && c < _cols && r < _rows && _kind[c, r] != AmmoCellKind.Wall;
            BuildWallBlocks(kit, wallPiece, wallMat, isSlot);
        }

        const int WallMarginX = 6, WallMarginBottom = 7;

        /// <summary>Che bằng bộ khối tường module gốc</summary>
        void BuildWallBlocks(WallKit kit, Func<int, int, Matrix4x4> wallPiece, Material wallMat, Func<int, int, bool> isSlot)
        {
            int[,] dir = { { -1, 0 }, { -1, -1 }, { 0, -1 }, { 1, -1 }, { 1, 0 }, { 1, 1 }, { 0, 1 }, { -1, 1 } };
            var body = new List<CombineInstance>();
            for (int r = 0; r < _rows + WallMarginBottom; r++)
                for (int c = -WallMarginX; c < _cols + WallMarginX; c++)
                {
                    if (isSlot(c, r)) continue;
                    int mask = 0;
                    for (int i = 0; i < 8; i++) if (r + dir[i, 1] >= 0 && !isSlot(c + dir[i, 0], r + dir[i, 1])) mask |= 1 << i;
                    body.Add(new CombineInstance { mesh = kit.Get(mask), transform = wallPiece(c, r) });
                }
            var mesh = new Mesh { name = "TrayWallBlocks", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(body.ToArray(), true, true);
            var go = new GameObject("TrayWallBlocks");
            go.transform.SetParent(_trayRoot, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = wallMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _tiles.Add(go);
            BuildOutline(wallPiece, isSlot);
        }

        const float OutlineGap = 0f, OutlineWidth = 0.1f, OutlineCornerOut = 0.12f, OutlineCornerIn = 0.2f, OutlineHeight = 0.55f;
        const int OutlinePx = 48;

        /// <summary>Viền sáng mảnh chạy quanh vùng ô chơi được như gốc</summary>
        void BuildOutline(Func<int, int, Matrix4x4> wallPiece, Func<int, int, bool> isSlot)
        {
            if (_config.trayOutlineMaterial == null) return;
            var slots = new List<Vector2Int>();
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                    if (isSlot(c, r)) slots.Add(new Vector2Int(c, r));
            if (slots.Count == 0) return;
            const int PadX = WallMarginX, Pad = 1;
            Func<int, int, bool> open = (c, r) => r < 0 || isSlot(c, r);
            var fillets = new List<(Vector2Int cell, int dx, int dy)>();
            for (int r = 0; r <= _rows; r++)
                for (int c = -PadX; c < _cols + PadX; c++)
                    if (!open(c, r))
                        for (int dx = -1; dx <= 1; dx += 2)
                            for (int dy = -1; dy <= 1; dy += 2)
                                if (open(c + dx, r) && open(c, r + dy)) fillets.Add((new Vector2Int(c, r), dx, dy));

            int w = (_cols + 2 * PadX) * OutlinePx, h = (_rows + 2 * Pad) * OutlinePx;
            var px = new Color32[w * h];
            Color32 col = _theme != null ? (Color32)_theme.trayOutline : new Color32(255, 255, 255, 255);
            float aa = 1f / OutlinePx;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = (x + 0.5f) / OutlinePx - PadX - 0.5f, cy = (y + 0.5f) / OutlinePx - Pad - 0.5f;
                    float d = OpenDistance(slots, fillets, open, cx, cy);
                    float a = Mathf.Clamp01((d - OutlineGap) / aa + 0.5f) * Mathf.Clamp01((OutlineGap + OutlineWidth - d) / aa + 0.5f);
                    if (a > 0f) px[(h - 1 - y) * w + x] = new Color32(col.r, col.g, col.b, (byte)(a * 255f));
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            tex.SetPixels32(px);
            tex.Apply(false, true);

            Vector3 Corner(int c, int r, float sx, float sz) => wallPiece(c, r).MultiplyPoint3x4(new Vector3(sx * 0.5f, OutlineHeight, sz * 0.5f));
            var mesh = new Mesh { name = "TrayOutline" };
            mesh.SetVertices(new List<Vector3> {
                Corner(-PadX, -Pad, -1f, 1f), Corner(_cols - 1 + PadX, -Pad, 1f, 1f),
                Corner(_cols - 1 + PadX, _rows - 1 + Pad, 1f, -1f), Corner(-PadX, _rows - 1 + Pad, -1f, -1f) });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(0f, 0f) });
            mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            var go = new GameObject("TrayOutline");
            go.transform.SetParent(_trayRoot, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(_config.trayOutlineMaterial) { mainTexture = tex };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _tiles.Add(go);
        }

        /// <summary>Khoảng cách (theo ô) từ điểm tới vùng hở đã bo góc</summary>
        static float OpenDistance(List<Vector2Int> slots, List<(Vector2Int cell, int dx, int dy)> fillets, Func<int, int, bool> isSlot, float x, float y)
        {
            float d = y + 0.5f;
            foreach (Vector2Int s in slots)
            {
                int sx = x >= s.x ? 1 : -1, sy = y >= s.y ? 1 : -1;
                float rad = !isSlot(s.x + sx, s.y) && !isSlot(s.x, s.y + sy) ? OutlineCornerOut : 0f;
                float qx = Mathf.Abs(x - s.x) - 0.5f + rad, qy = Mathf.Abs(y - s.y) - 0.5f + rad;
                float box = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - rad;
                d = Mathf.Min(d, box);
            }
            foreach (var f in fillets)
            {
                float ox = f.cell.x + f.dx * (0.5f - OutlineCornerIn), oy = f.cell.y + f.dy * (0.5f - OutlineCornerIn);
                if ((x - ox) * f.dx < 0f || (y - oy) * f.dy < 0f || Mathf.Abs(x - f.cell.x) > 0.5f || Mathf.Abs(y - f.cell.y) > 0.5f) continue;
                d = Mathf.Min(d, OutlineCornerIn - new Vector2(x - ox, y - oy).magnitude);
            }
            return d;
        }

        /// <summary>Đặt tấm cửa kính của hộp có cửa khít miệng hố ô đó</summary>
        public void FitDoorCover(AmmoBox box, Vector2Int cell)
        {
            HoleRect(cell, out Vector3 hc, out Vector2 hs);
            box.SetCoverPlacement(hc, hs, PlateRotation ?? Quaternion.identity);
        }

        float TrayPanelZ => _config.trayPanelDepth;
        float TrayFloorZ => _config.trayHoleDepth;
        float PanelPad => _config.trayPanelPad;
        float PanelRadius => _config.trayPanelRadius;
        float SlotSize => _config.traySlotSize;
        float SlotRadius => _config.traySlotRadius;
        float HoleTopInset => _config.trayHoleTopInset;

        float PanelZ => -0.6f + _cell * (TrayPanelZ + 0.6f);

        Material _trayTileMat, _trayWallMat, _trayBaseMat;
        /// <summary>Khay gốc: hướng mặt khay HoleAll</summary>
        public Quaternion? PlateRotation;
        const float PlateLift = 0.02f;

        Vector3 PlatePoint(Vector2Int cell, float lift)
        {
            Vector3 c = CellWorld(cell);
            Vector3 p = new Vector3(c.x, c.y, _rowZ[Mathf.Clamp(cell.y, 0, _rowZ.Length - 1)]);
            return p + PlateRotation.Value * Vector3.back * (lift * _cell);
        }

        readonly Dictionary<Vector2Int, AmmoHoleLid> _lids = new Dictionary<Vector2Int, AmmoHoleLid>();
        readonly Dictionary<string, Material> _jellyMats = new Dictionary<string, Material>();

        /// <summary>Hố vừa trống: thạch nhỏ giọt rồi nắp trượt lên đậy</summary>
        public void SpawnHoleLid(Vector2Int cell, string colorCode)
        {
            RemoveLid(cell);
            if (_trayTileMat == null || _trayWallMat == null) return;
            float z = _rowZ != null ? _rowZ[Mathf.Clamp(cell.y, 0, _rowZ.Length - 1)] - 0.05f
                                    : PanelZ + 0.012f;
            const float dy = 0f;
            Vector3 c = CellWorld(cell);
            var go = new GameObject($"HoleLid_{cell.x}_{cell.y}");
            go.transform.SetParent(_trayRoot, false);
            go.transform.position = new Vector3(c.x, c.y + dy, z);
            if (PlateRotation.HasValue) go.transform.SetPositionAndRotation(PlatePoint(cell, PlateLift), PlateRotation.Value);
            float s = _cell * (_rowZ != null ? _config.holeLidSize : SlotSize) * 0.5f;
            float sy = s, top = sy - HoleTopInset * _cell;
            if (!_jellyMats.TryGetValue(colorCode ?? "", out Material jm) || jm == null)
            {
                jm = new Material(_trayBaseMat != null ? _trayBaseMat : _trayTileMat) { name = "HoleJelly " + colorCode };
                if (jm.HasProperty("_BaseColor")) jm.SetColor("_BaseColor", _colors.ColorOf(colorCode));
                _jellyMats[colorCode ?? ""] = jm;
            }
            var lid = go.AddComponent<AmmoHoleLid>();
            lid.Init(_cell, s * (_rowZ != null ? _config.holeLidWidthScale : 1f), -sy, top, _cell * (_rowZ != null ? _config.holeLidRadius : SlotRadius), _trayWallMat, jm, _trayTileMat, _config);
            _lids[cell] = lid;
            _tiles.Add(go);
        }

        /// <summary>Xử lý bỏ nắp hố ở một ô</summary>
        public void RemoveLid(Vector2Int cell)
        {
            if (_lids.TryGetValue(cell, out AmmoHoleLid l) && l != null) Object.Destroy(l.gameObject);
            _lids.Remove(cell);
        }

        /// <summary>Tâm &amp; kích thước miệng lỗ của ô</summary>
        void HoleRect(Vector2Int cell, out Vector3 center, out Vector2 size)
        {
            if (_rowZ != null)
            {
                float hw = _cell * _config.holeLidSize * 0.5f;
                center = PlateRotation.HasValue ? PlatePoint(cell, PlateLift * 1.5f) : CellWorld(cell);
                size = new Vector2(2f * hw, 2f * hw);
                return;
            }
            float s = _cell * SlotSize * 0.5f, top = s - HoleTopInset * _cell;
            float z = PanelZ - 0.01f;
            const float dy = 0f;
            Vector3 c = CellWorld(cell);
            center = new Vector3(c.x, c.y + (top - s) * 0.5f + dy, z);
            size = new Vector2(2f * s, top + s);
        }

        /// <summary>Xử lý dựng mặt khay và hố</summary>
        public void Build()
        {
            Material baseMat = _theme != null && _theme.trayPanel != null ? _theme.trayPanel
                             : _theme != null && _theme.ammoCell != null ? _theme.ammoCell : _config.ammoCellMaterial;
            Material wallMat = _theme != null ? _theme.trayWall : null;
            if (wallMat == null)
            {
                var prefab = _config.ammoTrayWallPrefab;
                if (prefab != null)
                    foreach (Renderer rend in prefab.GetComponentsInChildren<Renderer>(true))
                        if (rend.name == "4xRounded") { wallMat = rend.sharedMaterial; break; }
                if (wallMat == null) wallMat = baseMat;
            }
            Material slotMat = null;
            if (baseMat != null)
            {
                slotMat = new Material(baseMat) { name = baseMat.name + " (Tile)" };
                if (slotMat.HasProperty("_BaseColor")) slotMat.SetColor("_BaseColor", slotMat.GetColor("_BaseColor") * 1.2f);
            }
            Material lipMat = null;
            if (baseMat != null && wallMat != null && baseMat.HasProperty("_BaseColor") && wallMat.HasProperty("_BaseColor"))
            {
                lipMat = new Material(baseMat) { name = "Tray Lip" };
                lipMat.SetColor("_BaseColor", Color.Lerp(wallMat.GetColor("_BaseColor"), baseMat.GetColor("_BaseColor"), 0.7f));
            }

            _trayTileMat = slotMat; _trayWallMat = wallMat; _trayBaseMat = baseMat;
            if (!_config.trayProcedural) return;
            Func<int, int, bool> emptyAt = (c, r) => c >= 0 && r >= 0 && c < _cols && r < _rows && _kind[c, r] == AmmoCellKind.Empty;
            Func<int, int, bool> isSlot = (c, r) => c >= 0 && r >= 0 && c < _cols && r < _rows && _kind[c, r] != AmmoCellKind.Wall;

            float panelZ = -0.6f + _cell * (TrayPanelZ + 0.6f);
            float backZ = panelZ + 0.02f;
            float top = _origin.y + _cell * 0.5f + 0.15f, bottom = top - 30f;
            {
                float hc = _cell * 0.5f;
                Vector2 c00 = CellWorld(new Vector2Int(0, 0)), cNN = CellWorld(new Vector2Int(_cols - 1, _rows - 1));
                float gx0 = c00.x - hc, gx1 = cNN.x + hc, gy1 = c00.y + hc, gy0 = cNN.y - hc;
                float L = -40f, R = 40f;
                var lipMesh = new MeshBuilder();
                var topMesh = new MeshBuilder();
                float raise = _cell * (PanelPad + 0.14f);
                Action<float, float, float, float, bool> quad = (x0, y0, x1, y1, lipBelow) =>
                {
                    if (x1 - x0 <= 0f || y1 - y0 <= 0f) return;
                    lipMesh.AddPolygon(new List<Vector2> { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) }, Vector2.zero);
                    float ty0 = y0;
                    topMesh.AddPolygon(new List<Vector2> { new Vector2(x0, ty0), new Vector2(x1, ty0), new Vector2(x1, y1), new Vector2(x0, y1) }, Vector2.zero);
                };
                quad(L, bottom, gx0, top, false);
                quad(gx1, bottom, R, top, false);
                for (int c = 0; c < _cols; c++)
                {
                    float cx = CellWorld(new Vector2Int(c, 0)).x;
                    quad(cx - hc, gy1, cx + hc, top, isSlot(c, 0));
                }
                quad(gx0, bottom, gx1, gy0, false);
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
            float h = _cell * 0.5f, pad = _cell * PanelPad, rad = _cell * PanelRadius;
            float s = _cell * SlotSize * 0.5f, sr = _cell * SlotRadius;
            for (int r = 0; r < _rows; r++)
                for (int c = 0; c < _cols; c++)
                {
                    if (!isSlot(c, r)) continue;
                    Vector2 ctr = CellWorld(new Vector2Int(c, r));
                    bool L = !isSlot(c - 1, r), R = !isSlot(c + 1, r), T = !isSlot(c, r - 1), B = !isSlot(c, r + 1);
                    float x0 = -h - (L ? pad : 0f), x1 = h + (R ? pad : 0f), y0 = -h - (B ? pad : 0f), y1 = h + (T ? pad : 0f);
                    bool eL = emptyAt(c - 1, r), eR = emptyAt(c + 1, r), eT = emptyAt(c, r - 1), eB = emptyAt(c, r + 1);
                    bool me = emptyAt(c, r);
                    float ix0 = me && eL ? -h : -s, ix1 = me && eR ? h : s, iy1 = me && eT ? h : s, iy0 = me && eB ? -h : -s;
                    float rBR = (me && (eR || eB)) ? 0f : sr, rTR = (me && (eR || eT)) ? 0f : sr, rTL = (me && (eL || eT)) ? 0f : sr, rBL = (me && (eL || eB)) ? 0f : sr;
                    panel.AddRing(RoundedRect(x0, y0, x1, y1, B && R ? rad : 0f, T && R ? rad : 0f, T && L ? rad : 0f, B && L ? rad : 0f),
                                  RoundedRect(ix0, iy0, ix1, iy1, rBR, rTR, rTL, rBL), ctr);
                    slots.AddPolygon(RoundedRect(-h, -h, h, h, 0f, 0f, 0f, 0f), ctr);
                }
            for (int r = -1; r <= _rows; r++)
                for (int c = -1; c <= _cols; c++)
                {
                    if (isSlot(c, r)) continue;
                    Vector2 ctr = CellWorld(new Vector2Int(c, r));
                    for (int dx = -1; dx <= 1; dx += 2)
                        for (int dy = -1; dy <= 1; dy += 2)
                        {
                            if (!isSlot(c + dx, r) || !isSlot(c, r + dy)) continue;
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
            GameObject floorGo = CreateFlat("TraySlotFloors", slots.Build(), wallMat, -0.6f + _cell * (TrayFloorZ + 0.6f));
            if (wallMat != null)
            {
                Material well = new Material(wallMat) { name = "Tray Well" };
                if (well.HasProperty("_BaseColor")) well.SetColor("_BaseColor", wallMat.GetColor("_BaseColor") * 0.8f);
                var wv = new List<Vector3>(); var wt = new List<int>(); var wn = new List<Vector3>();
                float zTop = panelZ, zBot = -0.6f + _cell * (TrayFloorZ + 0.6f);
                for (int r = 0; r < _rows; r++)
                    for (int c = 0; c < _cols; c++)
                    {
                        if (!isSlot(c, r) || emptyAt(c, r)) continue;
                        Vector2 ctr = CellWorld(new Vector2Int(c, r));
                        List<Vector2> ring = RoundedRect(-s, -s, s, s, sr, sr, sr, sr);
                        int o = wv.Count, n = ring.Count;
                        foreach (Vector2 p in ring) { wv.Add(new Vector3(ctr.x + p.x, ctr.y + p.y, zTop)); wn.Add(-(Vector3)p.normalized); }
                        foreach (Vector2 p in ring) { wv.Add(new Vector3(ctr.x + p.x, ctr.y + p.y, zBot)); wn.Add(-(Vector3)p.normalized); }
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
            go.transform.position = new Vector3(0f, 0f, z);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _tiles.Add(go);
            return go;
        }

        /// <summary>Đa giác lồi hình chữ nhật bo góc</summary>
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
            if (r <= 0f) { for (int i = 0; i <= steps; i++) pts.Add(corner); return; }
            Vector2 c = corner + new Vector2(sx * r, sy * r);
            for (int i = 0; i <= steps; i++)
            {
                float a = (startDeg + 90f * i / steps) * Mathf.Deg2Rad;
                pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
        }

        /// <summary>Gom nhiều đa giác lồi phẳng</summary>
        class MeshBuilder
        {
            readonly List<Vector3> _v = new List<Vector3>();
            readonly List<int> _t = new List<int>();

            /// <summary>Dùng cho thêm một đa giác vào mesh đang dựng</summary>
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
                    _t.Add(center); _t.Add(b); _t.Add(a);
                }
            }

            /// <summary>Quạt tam giác từ pts[0] (đa giác hình sao quanh đỉnh đầu), vẽ cả 2 mặt</summary>
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

            /// <summary>Vành giữa 2 đa giác cùng số đỉnh, cùng chiều</summary>
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

            /// <summary>Xử lý dựng mặt khay và hố</summary>
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
    }
}
