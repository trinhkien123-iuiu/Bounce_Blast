using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>ONBOARDING ở những level đầu</summary>
    public class OnboardingManager : MonoBehaviour
    {
        const string SeenPrefix = "bb_onboard_";

        GameConfig _config;
        GameUI _ui;
        LevelManager _level;
        BoosterManager _boosters;
        AmmoGridController _grid;
        BallManager _balls;
        Camera _cam;
        OnboardingView _view;

        /// <summary>Đang khoá đầu vào: chỉ cho bấm Target</summary>
        public bool Blocking { get; private set; }
        public AmmoBox Target { get; private set; }

        /// <summary>Xử lý khởi tạo hướng dẫn</summary>
        public void Init(GameConfig config, GameUI ui, LevelManager level, BoosterManager boosters, AmmoGridController grid, BallManager balls, Camera cam, OnboardingView view)
        {
            _config = config; _ui = ui; _level = level; _boosters = boosters; _grid = grid; _balls = balls; _cam = cam; _view = view;
            if (_view != null) { string miss = _view.Validate(); if (miss != null) Debug.LogError("OnboardingCanvas chưa nối đủ tham chiếu: " + miss); }
            _level.OnLevelLoading += Abort;
            _level.OnLevelLoaded += m => StartCoroutine(Run(m));
        }

        bool _paused;

        void Abort()
        {
            StopAllCoroutines();
            if (_paused) { Time.timeScale = 1f; _paused = false; }
            Blocking = false; Target = null;
            if (_view != null && _view.IsShown) _view.Hide();
        }

        static bool Seen(string key) => PlayerPrefs.GetInt(SeenPrefix + key, 0) == 1;
        static void MarkSeen(string key) => PlayerPrefs.SetInt(SeenPrefix + key, 1);

        /// <summary>Các cơ chế "New Feature" có mặt trong level</summary>
        public static List<string> FeaturesIn(LevelModel m)
        {
            var ids = new List<string>();
            foreach (BoardItemData it in m.BoardItems)
            {
                if (it.Kind == BoardItemKind.CombinedCube && !ids.Contains("BigCube")) ids.Add("BigCube");
                if (it.Kind == BoardItemKind.Crate && !ids.Contains("Crate")) ids.Add("Crate");
                if (it.Kind == BoardItemKind.Column && !ids.Contains("Column")) ids.Add("Column");
            }
            for (int x = 0; x < m.AmmoCols; x++)
                for (int y = 0; y < m.AmmoRows; y++)
                {
                    AmmoCellData c = m.AmmoCells[x, y];
                    if (c == null) continue;
                    if (c.Kind == AmmoCellKind.Tunnel && !ids.Contains("Tunnel")) ids.Add("Tunnel");
                    if (c.Kind == AmmoCellKind.Box && c.Box != null)
                    {
                        if (c.Box.Hidden && !ids.Contains("Qmark")) ids.Add("Qmark");
                        if (c.Box.IceHits > 0 && !ids.Contains("Ice")) ids.Add("Ice");
                        if (c.Box.Door > 0 && !ids.Contains("Cover")) ids.Add("Cover");
                    }
                }
            return ids;
        }

        IEnumerator Run(LevelModel model)
        {
            yield return null;

            bool done = false;
            _boosters.PlayPendingUnlocks(() => done = true);
            while (!done) yield return null;

            foreach (string id in FeaturesIn(model))
            {
                if (Seen("feature_" + id)) continue;
                GameConfig.FeatureIntro intro = _config.FeatureIntroOf(id);
                if (intro == null) continue;
                MarkSeen("feature_" + id);
                done = false;
                _ui.ShowFeatureIntro(intro.title, intro.description, intro.icon, model.Difficulty, () => done = true);
                while (!done) yield return null;
            }

            int level = _level.CurrentLevel;
            if (level == _config.onboardTapLevel && !Seen("tap")) yield return TapStep();
            else if (level == _config.onboardCapacityLevel && !Seen("capacity")) yield return CapacityStep();
            else if (level == _config.onboardSpeedLevel && !Seen("speed")) yield return SpeedStep();
        }

        IEnumerator TapStep()
        {
            AmmoBox box = _grid.FirstClickable();
            if (box == null) yield break;
            Target = box; Blocking = true;
            float r = Screen.height * 0.06f;
            _view.Show("Tap to select", ScreenOf(box), new Vector2(r, r), r, true, true, false, Screen.height * 0.17f);
            while (box != null && !box.IsLaunched)
            {
                _view.Move(ScreenOf(box));
                yield return null;
            }
            Blocking = false; Target = null;
            _view.Hide();
            MarkSeen("tap");
        }

        IEnumerator CapacityStep()
        {
            Transform bar = _ui.CapacityBarTransform;
            if (bar == null) yield break;
            Rect rect = ScreenRectOf(bar);
            _view.Show("Watch the capacity!\nThe area can only hold a limited number of balls!", rect.center, rect.size * 0.5f + new Vector2(24f, 20f), rect.height * 0.5f, true, false, true, -Screen.height * 0.17f);
            float prev = Time.timeScale;
            Time.timeScale = 0f; _paused = true;
            yield return null;
            while (!Input.GetMouseButtonDown(0) && !(Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)) yield return null;
            Time.timeScale = prev > 0f ? prev : 1f; _paused = false;
            _view.Hide();
            MarkSeen("capacity");
        }

        IEnumerator SpeedStep()
        {
            while (_balls.ActiveCount == 0) yield return null;
            Vector2 pos = _cam.WorldToScreenPoint(_grid.HoldSpotWorld());
            _view.Show("HOLD WALL\nTO SPEED UP!", pos, Vector2.zero, 0f, false, true, false, Screen.height * 0.2f);
            float t = 0f;
            while (Time.timeScale <= 1.01f && t < 12f) { t += Time.unscaledDeltaTime; yield return null; }
            _view.Hide();
            MarkSeen("speed");
        }

        Vector2 ScreenOf(AmmoBox b) => _cam.WorldToScreenPoint(b.transform.position);

        /// <summary>Khung màn hình của một RectTransform</summary>
        Rect ScreenRectOf(Transform t)
        {
            var corners = new Vector3[4];
            var rt = t as RectTransform ?? t.GetComponentInChildren<RectTransform>();
            rt.GetWorldCorners(corners);
            Vector2 a = _cam.WorldToScreenPoint(corners[0]), b = _cam.WorldToScreenPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }
    }
}
