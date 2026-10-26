using System;
using System.Collections.Generic;
using UnityEngine;
using BounceBlast.Gameplay;
using BounceBlast.UI;

#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS
namespace BounceBlast.Cheats
{
    /// <summary>Danh sách cheat (MVP: nhóm A Level · B Resources · C Speed)</summary>
    public static class CheatRegistry
    {
        public const string GroupLevel = "Level";
        public const string GroupResources = "Resources";
        public const string GroupSpeed = "Speed";
        public static readonly string[] Groups = { GroupLevel, GroupResources, GroupSpeed };

        static bool _infiniteCapacity, _paused, _confirmReset;
        static float _speedBeforePause = 1f;

        /// <summary>Gọi lại sau mỗi lần nạp level</summary>
        public static void ReapplyPersistent(GameManager gm)
        {
            if (gm != null && gm.launchManager != null) gm.launchManager.Capacity.Infinite = _infiniteCapacity;
        }

        /// <summary>Dựng danh sách các mục cheat cho bản test</summary>
        public static List<CheatItem> Build(GameManager gm, CheatPerfOverlay overlay)
        {
            var list = new List<CheatItem>();
            void Add(CheatItem i) => list.Add(i);
            CheatItem Btn(string g, string label, Action a) => new CheatItem { group = g, label = label, kind = CheatKind.Button, onClick = a };
            CheatItem Tgl(string g, string label, Func<bool> get, Action<bool> set) => new CheatItem { group = g, label = label, kind = CheatKind.Toggle, get = get, set = set };

            LevelManager lm = gm.levelManager;

            Add(new CheatItem
            {
                group = GroupLevel, label = "Go to level", kind = CheatKind.Stepper,
                initial = () => lm.currentLevel > 0 ? lm.currentLevel : Mathf.Max(1, PlayerData.Level), min = 1, max = gm.config != null ? gm.config.MaxLevel : 550,
                go = n => { PlayerData.Level = n; lm.LoadLevel(n); },
            });
            Add(Btn(GroupLevel, "Restart level", () => lm.Restart()));
            Add(Btn(GroupLevel, "Previous level", () => lm.LoadLevel(Mathf.Max(1, lm.currentLevel - 1))));
            Add(Btn(GroupLevel, "Next level", () => lm.LoadNext()));
            Add(Btn(GroupLevel, "WIN now (break all blocks)", () => { foreach (BoardBlock b in new List<BoardBlock>(gm.board.Blocks)) gm.board.DestroyBlock(b); }));
            Add(Btn(GroupLevel, "LOSE now", () => gm.CheatLose()));

            Add(Btn(GroupResources, "+1000 coins", () => PlayerData.Coins += 1000));
            Add(Btn(GroupResources, "Coins = 0", () => PlayerData.Coins = 0));
            Add(Btn(GroupResources, "+5 every booster", () =>
            {
                foreach (BoosterType t in Enum.GetValues(typeof(BoosterType))) gm.boosterManager.Boosters.Add(t, 5);
            }));
            Add(Tgl(GroupResources, "Infinite capacity", () => _infiniteCapacity, v => { _infiniteCapacity = v; ReapplyPersistent(gm); }));
            Add(Btn(GroupResources, "Capacity +25%", () => gm.launchManager.Capacity.Increase(0.25f)));
            Add(new CheatItem
            {
                group = GroupResources, kind = CheatKind.Button,
                dynamicLabel = () => _confirmReset ? "TAP AGAIN: wipe save (level 1, coins 0)" : "Reset save data",
                onClick = () =>
                {
                    if (!_confirmReset) { _confirmReset = true; return; }
                    _confirmReset = false;
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    lm.LoadLevel(1);
                },
            });

            foreach (float s in new[] { 0.25f, 0.5f, 1f, 2f, 4f })
            {
                float speed = s;
                Add(Btn(GroupSpeed, "Game speed x" + speed, () => SetSpeed(speed)));
            }
            Add(Tgl(GroupSpeed, "Pause", () => _paused, v =>
            {
                _paused = v;
                if (v) { _speedBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f; SetSpeed(0f); }
                else SetSpeed(_speedBeforePause);
            }));
            Add(Tgl(GroupSpeed, "Auto-play", () => gm.debugManager != null && gm.debugManager.autoPlay, v => { if (gm.debugManager != null) gm.debugManager.autoPlay = v; }));
            Add(Tgl(GroupSpeed, "FPS overlay", () => overlay != null && overlay.Visible, v => { if (overlay != null) overlay.Visible = v; }));
            return list;
        }

        static void SetSpeed(float s)
        {
            Time.timeScale = s;
            Time.fixedDeltaTime = 0.02f * Mathf.Max(0.0001f, s > 0f ? s : 1f);
        }
    }
}
#endif
