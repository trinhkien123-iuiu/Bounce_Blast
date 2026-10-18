using System.IO;
using BounceBlast.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BounceBlast.EditorTools
{
    /// <summary>
    /// Cửa sổ Level Editor (Tools ▸ Bounce Blast ▸ Level Editor): làm level cho bộ <c>custom</c>.
    /// Trái: danh sách level · Giữa: tab Bàn gạch / Khay đạn · Phải: thông số, cân bi theo màu, lỗi, Chơi thử.
    /// Thiết kế: Docs/thiet-ke/level-editor.md. Việc vẽ nằm ở <see cref="BoardPainter"/> / <see cref="TrayPainter"/>,
    /// kiểm tra ở <see cref="LevelValidator"/>, nhập CSV / chơi thử ở <see cref="LevelEditorActions"/>.
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        const float LeftW = 210f, RightW = 320f;

        LevelPack _pack;
        LevelData _level;
        ColorLibrary _colors;
        int _tab;
        Vector2 _listScroll, _rightScroll;
        int _importId = 1;
        LevelValidator.Report _report;
        bool _reportDirty = true;

        readonly BoardPainter _board = new BoardPainter();
        readonly TrayPainter _tray = new TrayPainter();

        [MenuItem("Tools/Bounce Blast/Level Editor/Mở Level Editor %#l")]
        public static void Open()
        {
            var w = GetWindow<LevelEditorWindow>("Level Editor");
            w.minSize = new Vector2(1000, 600);
        }

        void OnEnable()
        {
            _pack = LevelEditorUtil.EnsurePack();
            _colors = LevelEditorUtil.FindColors();
            if (_level == null && _pack.Count > 0) _level = _pack.levels[0];
            Undo.undoRedoPerformed += OnUndo;
        }

        void OnDisable() => Undo.undoRedoPerformed -= OnUndo;
        void OnUndo() { _reportDirty = true; Repaint(); }

        void OnGUI()
        {
            if (_pack == null) _pack = LevelEditorUtil.EnsurePack();
            HandleShortcuts();
            float h = position.height;
            GUILayout.BeginArea(new Rect(0, 0, LeftW, h), EditorStyles.helpBox);
            DrawList();
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(LeftW, 0, position.width - LeftW - RightW, h));
            DrawCenter(new Rect(0, 0, position.width - LeftW - RightW, h));
            GUILayout.EndArea();
            GUILayout.BeginArea(new Rect(position.width - RightW, 0, RightW, h), EditorStyles.helpBox);
            DrawRight();
            GUILayout.EndArea();
        }

        // ------------------------------------------------------------------ Trái: danh sách
        void DrawList()
        {
            EditorGUILayout.LabelField($"Bộ custom ({_pack.Count} level)", EditorStyles.boldLabel);
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll, GUILayout.Height(Mathf.Max(80f, position.height - 200f)));
            for (int i = 0; i < _pack.levels.Count; i++)
            {
                LevelData d = _pack.levels[i];
                string name = d != null ? $"{i + 1:000}  {(string.IsNullOrEmpty(d.title) ? d.name : d.title)}" : $"{i + 1:000}  (thiếu)";
                var style = new GUIStyle(EditorStyles.label);
                if (d == _level) { style.fontStyle = FontStyle.Bold; style.normal.textColor = new Color(1f, 0.8f, 0.2f); }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(name, style)) Select(d);
                if (GUILayout.Button("▲", GUILayout.Width(20)) && i > 0) Move(i, -1);
                if (GUILayout.Button("▼", GUILayout.Width(20)) && i < _pack.levels.Count - 1) Move(i, 1);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("+ Level mới")) Select(LevelEditorActions.CreateLevel(_pack, null));
            GUI.enabled = _level != null;
            if (GUILayout.Button("Nhân bản level đang chọn")) Select(LevelEditorActions.CreateLevel(_pack, _level));
            if (GUILayout.Button("Xoá level đang chọn") &&
                EditorUtility.DisplayDialog("Xoá level", $"Xoá '{_level.name}' khỏi bộ custom và xoá file asset?", "Xoá", "Huỷ"))
            {
                LevelEditorActions.DeleteLevel(_pack, _level);
                Select(_pack.Count > 0 ? _pack.levels[0] : null);
            }
            GUI.enabled = true;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Nhập level gốc (CSV) làm mẫu", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            _importId = Mathf.Clamp(EditorGUILayout.IntField(_importId, GUILayout.Width(50)), 1, 550);
            if (GUILayout.Button("Nhập")) { LevelData d = LevelEditorActions.ImportOriginal(_pack, _importId); if (d != null) Select(d); }
            EditorGUILayout.EndHorizontal();
        }

        void Move(int i, int dir)
        {
            Undo.RecordObject(_pack, "Đổi thứ tự level");
            (_pack.levels[i], _pack.levels[i + dir]) = (_pack.levels[i + dir], _pack.levels[i]);
            EditorUtility.SetDirty(_pack);
        }

        void Select(LevelData d)
        {
            _level = d;
            _board.ClearSelection();
            _reportDirty = true;
            GUI.FocusControl(null);
        }

        // ------------------------------------------------------------------ Giữa: vẽ
        void DrawCenter(Rect area)
        {
            if (_level == null) { EditorGUILayout.HelpBox("Chọn hoặc tạo một level ở cột trái.", MessageType.Info); return; }
            _tab = GUILayout.Toolbar(_tab, new[] { "Bàn gạch", "Khay đạn" }, GUILayout.Height(24));
            if (_tab == 0) _board.DrawToolbar(_level, _colors); else _tray.DrawToolbar(_level, _colors);
            string hover = _tab == 0 ? _board.hoverInfo : _tray.hoverInfo;
            EditorGUILayout.LabelField(hover, EditorStyles.miniLabel);
            Rect last = GUILayoutUtility.GetLastRect();
            float top = last.yMax + 4f;
            var gridArea = new Rect(0, top, area.width, area.height - top);
            if (_tab == 0) _board.DrawGrid(gridArea, _level, _colors, MarkChanged);
            else _tray.DrawGrid(gridArea, _level, _colors, MarkChanged);
            if (Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDrag) Repaint();
        }

        void MarkChanged() { _reportDirty = true; Repaint(); }

        // ------------------------------------------------------------------ Phải: thông số, kiểm tra
        void DrawRight()
        {
            if (_level == null) return;
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
            EditorGUILayout.LabelField(_level.name, EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var so = new SerializedObject(_level);
            EditorGUILayout.PropertyField(so.FindProperty("title"), new GUIContent("Tên"));
            EditorGUILayout.PropertyField(so.FindProperty("chapter"), new GUIContent("Chương"));
            EditorGUILayout.PropertyField(so.FindProperty("difficulty"), new GUIContent("Độ khó"));
            EditorGUILayout.PropertyField(so.FindProperty("capacity"), new GUIContent("Capacity"));
            EditorGUILayout.PropertyField(so.FindProperty("multiplier"), new GUIContent("Multiplier", "Mỗi ô thiết kế = N×N gạch; máu khối gộp, hòm và số bi nhân N²"));
            EditorGUILayout.PropertyField(so.FindProperty("notes"), new GUIContent("Ghi chú"));
            if (EditorGUI.EndChangeCheck()) { so.ApplyModifiedProperties(); MarkChanged(); }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Kích thước (ô thiết kế)", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            int bc = EditorGUILayout.IntField("Bàn cột × dòng", _level.boardCols);
            int br = EditorGUILayout.IntField(_level.boardRows, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            if (bc != _level.boardCols || br != _level.boardRows)
            {
                Undo.RecordObject(_level, "Đổi cỡ bàn");
                _level.SetBoardSize(Mathf.Clamp(bc, 1, 32), Mathf.Clamp(br, 1, 64));
                _level.boardSizeOverride = Vector2Int.zero;   // level tự làm: bàn thật = ô thiết kế × Multiplier
                EditorUtility.SetDirty(_level); MarkChanged();
            }
            EditorGUILayout.BeginHorizontal();
            int ac = EditorGUILayout.IntField("Khay cột × hàng", _level.ammoCols);
            int ar = EditorGUILayout.IntField(_level.ammoRows, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
            if (ac != _level.ammoCols || ar != _level.ammoRows)
            {
                Undo.RecordObject(_level, "Đổi cỡ khay");
                _level.SetAmmoSize(ac, Mathf.Clamp(ar, 1, 12));
                EditorUtility.SetDirty(_level); MarkChanged();
            }
            int m = Mathf.Max(1, _level.multiplier);
            Vector2Int real = _level.boardSizeOverride.x > 0 ? _level.boardSizeOverride : new Vector2Int(_level.boardCols * m, _level.boardRows * m);
            EditorGUILayout.LabelField($"Bàn trong game: {real.x}×{real.y} gạch", EditorStyles.miniLabel);

            if (_tab == 1) { EditorGUILayout.Space(); _tray.DrawInspector(_level, _colors); }

            // ---- kiểm tra
            if (_reportDirty || _report == null) { _report = LevelValidator.Check(_level, _colors); _reportDirty = false; }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cân bi theo màu (máu gạch ↔ bi trong khay)", EditorStyles.boldLabel);
            foreach (LevelValidator.ColorBalance b in _report.balance)
            {
                EditorGUILayout.BeginHorizontal();
                Rect sw = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14));
                EditorGUI.DrawRect(sw, _colors != null ? _colors.ColorOf(b.color) : Color.white);
                var st = new GUIStyle(EditorStyles.label) { normal = { textColor = b.Ok ? new Color(0.5f, 0.9f, 0.5f) : new Color(1f, 0.45f, 0.4f) } };
                GUILayout.Label($"{b.color,-3} máu {b.hp,5}   bi {b.balls,5}   {(b.Ok ? "✔" : (b.balls - b.hp > 0 ? "+" : "") + (b.balls - b.hp))}", st);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.LabelField($"Tổng bi {_report.totalBalls} · hộp lớn nhất {_report.maxBox} · bấm được lúc đầu {_report.clickableAtStart}", EditorStyles.miniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lỗi / cảnh báo", EditorStyles.boldLabel);
            if (_report.issues.Count == 0) EditorGUILayout.HelpBox("Không có lỗi – chơi được.", MessageType.Info);
            foreach (LevelValidator.Issue i in _report.issues)
                EditorGUILayout.HelpBox(i.message, i.severity == LevelValidator.Severity.Error ? MessageType.Error : MessageType.Warning);

            EditorGUILayout.Space();
            if (GUILayout.Button("▶ Chơi thử (F5)", GUILayout.Height(32))) Playtest();
            if (GUILayout.Button("Lưu (Ctrl+S)")) Save();
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ Phím tắt / hành động
        void HandleShortcuts()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown || EditorGUIUtility.editingTextField) return;
            if (e.keyCode == KeyCode.F5) { Playtest(); e.Use(); return; }
            if (e.keyCode == KeyCode.S && (e.control || e.command)) { Save(); e.Use(); return; }
            if (_tab != 0) return;
            switch (e.keyCode)
            {
                case KeyCode.B: _board.tool = BoardPainter.Tool.Brush; e.Use(); break;
                case KeyCode.G: _board.tool = BoardPainter.Tool.Fill; e.Use(); break;
                case KeyCode.E: _board.tool = BoardPainter.Tool.Erase; e.Use(); break;
                case KeyCode.I: _board.tool = BoardPainter.Tool.Pick; e.Use(); break;
                case KeyCode.S: _board.tool = BoardPainter.Tool.Select; e.Use(); break;
            }
        }

        void Save()
        {
            if (_level != null) EditorUtility.SetDirty(_level);
            EditorUtility.SetDirty(_pack);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Đã lưu"));
        }

        void Playtest()
        {
            if (_level == null) return;
            Save();
            if (_report != null && _report.HasErrors &&
                !EditorUtility.DisplayDialog("Level còn lỗi", "Level đang có lỗi (xem cột phải). Vẫn chơi thử?", "Chơi thử", "Huỷ")) return;
            LevelEditorActions.Playtest(_level);
        }
    }

    /// <summary>Các hành động trên bộ level: tạo / nhân bản / xoá / nhập level gốc / chơi thử.</summary>
    public static class LevelEditorActions
    {
        public const string GameplayScene = "Assets/_Project/Scenes/Gameplay.unity";

        public static LevelData CreateLevel(LevelPack pack, LevelData copyFrom)
        {
            LevelData d;
            if (copyFrom != null) d = Object.Instantiate(copyFrom);
            else
            {
                d = ScriptableObject.CreateInstance<LevelData>();
                d.boardCols = 10; d.boardRows = 16; d.multiplier = 2; d.capacity = 200;
                d.ammoCols = 5; d.ammoRows = 4;
                d.EnsureSize();
                foreach (LevelData.Slot s in d.ammo) s.type = LevelData.SlotType.Empty;
            }
            string path = AssetDatabase.GenerateUniqueAssetPath($"{LevelEditorUtil.CustomDir}/Level_{pack.Count + 1:000}.asset");
            AssetDatabase.CreateAsset(d, path);
            d.title = copyFrom != null ? copyFrom.title + " (bản sao)" : Path.GetFileNameWithoutExtension(path);
            Undo.RecordObject(pack, "Thêm level");
            pack.levels.Add(d);
            EditorUtility.SetDirty(pack);
            AssetDatabase.SaveAssets();
            return d;
        }

        public static void DeleteLevel(LevelPack pack, LevelData d)
        {
            pack.levels.Remove(d);
            EditorUtility.SetDirty(pack);
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(d));
            AssetDatabase.SaveAssets();
        }

        /// <summary>Nhập level gốc số <paramref name="id"/> thành một LevelData mới trong bộ custom (làm mẫu / so sánh).</summary>
        public static LevelData ImportOriginal(LevelPack pack, int id)
        {
            GameConfig config = LevelEditorUtil.FindConfig();
            LevelSet.Chunk chunk = config != null && config.levels != null ? config.levels.Find(id) : null;
            if (chunk == null || chunk.board == null) { Debug.LogError($"[Level Editor] Không tìm thấy level gốc {id}"); return null; }
            LevelTokens t = LevelParser.ReadTokens(chunk.board.text, chunk.tunnels != null ? chunk.tunnels.text : null, id);
            if (t == null) return null;
            LevelData d = CreateLevel(pack, null);
            Undo.RecordObject(d, "Nhập level gốc");
            d.FromTokens(t);
            d.title = $"Gốc {id}";
            d.notes = $"Nhập từ level gốc {id} (CSV).";
            EditorUtility.SetDirty(d);
            AssetDatabase.SaveAssets();
            return d;
        }

        /// <summary>Lưu scene đang mở (nếu người dùng đồng ý), mở Gameplay và Play đúng level đang sửa.</summary>
        public static void Playtest(LevelData d)
        {
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            SessionState.SetString(LevelManager.PlaytestKey, AssetDatabase.GetAssetPath(d));
            if (EditorSceneManager.GetActiveScene().path != GameplayScene) EditorSceneManager.OpenScene(GameplayScene);
            EditorApplication.isPlaying = true;
        }
    }
}
