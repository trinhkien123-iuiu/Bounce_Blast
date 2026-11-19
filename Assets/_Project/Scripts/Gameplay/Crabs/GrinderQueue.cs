using System;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Hàng chờ gồm các máy xay (đặt sẵn trong Hierarchy, mặc định 5). Nhận hộp ammo vừa bấm và đưa vào máy trống đầu tiên</summary>
    public class GrinderQueue : MonoBehaviour
    {
        [Tooltip("Các máy xay, từ trái sang phải (kéo vào từ Hierarchy)")] public GrinderSlot[] slots;

        [Header("Nhãn trạng thái (TextMeshPro đặt sẵn trong scene)")]
        [Tooltip("Hiện \"WAITING n/5\" (n = số máy đang bận). Trống = không hiện")] public TMPro.TMP_Text statusLabel;

        [Header("Thử nghiệm (bước 1, chưa có cua)")]
        [Tooltip("> 0: mỗi chừng này giây mỗi máy tự 'sinh' 1 cua ảo để thử vòng trống/bận. 0 = tắt (khi cua thật có rồi)")]
        public float debugDrainInterval = 0.4f;

        /// <summary>(số máy bận, tổng số máy) — dùng cho nhãn WAITING n/5</summary>
        public event Action<int, int> OnBusyChanged;

        GameConfig _config;
        BoardController _board;
        CrabManager _crabs;
        float _spawnTimer;
        readonly System.Collections.Generic.List<GrinderSlot> _order = new System.Collections.Generic.List<GrinderSlot>();
        float _drainTimer;
        int _lastBusy = -1;

        public int Count => slots != null ? slots.Length : 0;
        public bool HasFree => FirstFree() != null;
        public bool IsBusy
        {
            get { foreach (GrinderSlot s in slots) if (s != null && !s.IsFree) return true; return false; }
        }

        /// <summary>Hàng chờ còn tiến triển được: còn cua đang chạy, có ammo đang bay, hoặc có ammo nhả được cua ngay bây giờ.
        /// False = kẹt (mọi ammo trong máy đều chờ block cùng màu mà không có đường ăn)</summary>
        public bool IsProgressing
        {
            get
            {
                if (_crabs != null && _crabs.ActiveCount > 0) return true;
                if (_board == null || _board.Reach == null) return IsBusy;
                foreach (GrinderSlot s in slots)
                {
                    if (s == null || s.IsFree) continue;
                    if (!s.Landed) return true;
                    if (s.remaining > 0 && _board.Reach.AnyEatable(_board.Blocks, s.ColorCode)) return true;
                }
                return false;
            }
        }

        public int BusyCount
        {
            get { int n = 0; foreach (GrinderSlot s in slots) if (s != null && !s.IsFree) n++; return n; }
        }

        /// <summary>crabs = null: chưa có cua thật, dùng cua ảo (debugDrainInterval)</summary>
        public void Init(GameConfig config, BoardController board = null, CrabManager crabs = null)
        {
            _config = config; _board = board; _crabs = crabs;
            if (slots != null) foreach (GrinderSlot s in slots) if (s != null) { s.SetConfig(config); if (s.crusher != null) s.crusher.Config = config; }
        }

        GrinderSlot FirstFree()
        {
            if (slots == null) return null;
            foreach (GrinderSlot s in slots) if (s != null && s.IsFree) return s;
            return null;
        }

        /// <summary>Hộp vừa rời khay: bay vào máy trống đầu tiên. Gọi sau khi LaunchManager đã kiểm tra HasFree</summary>
        public void Receive(AmmoBox box)
        {
            GrinderSlot slot = FirstFree();
            if (slot == null)
            {
                Debug.LogWarning("[GrinderQueue] Hết máy trống mà vẫn nhận hộp – bỏ hộp");
                box.Finish();
                return;
            }
            Color tint = _board != null && _board.Colors != null ? _board.Colors.ColorOf(box.Data.ColorCode) : Color.white;
            slot.Assign(box, tint);
            box.transform.SetParent(slot.transform, true);
            box.Flight.DockWidth = slot.dockWidth;
            box.Flight.DockRotation = slot.DockRotation;
            float dur = _config != null ? (_config.crabMode ? _config.crabBoxFlyDuration : _config.boxFlyDuration) : 0.6f;
            box.Flight.FlyTo(slot.DockPosition, dur, slot.OnFlightDone, slot.OnLanded);
            NotifyIfChanged();
        }

        void Update()
        {
            if (_crabs != null && _board != null && _board.Reach != null) SpawnScheduler();
            else if (debugDrainInterval > 0f)
            {
                _drainTimer += Time.deltaTime;
                if (_drainTimer >= debugDrainInterval)
                {
                    _drainTimer = 0f;
                    foreach (GrinderSlot s in slots) if (s != null) s.SpendCharge();
                }
            }
            NotifyIfChanged();
        }

        float[] _acc, _recheck;

        /// <summary>Mỗi máy xay tự nhả theo tốc độ crabSpawnRate (cua/giây), KHÔNG đợi máy khác. Một lần BFS lấy cả danh sách block
        /// cùng màu ăn được (gần → xa) nên nhả được nhiều cua trong cùng khung hình. Máy đang chờ (chưa có đường ăn) chỉ dò lại mỗi 0.1 giây</summary>
        void SpawnScheduler()
        {
            float dt = Time.deltaTime;
            if (_acc == null || _acc.Length != slots.Length) { _acc = new float[slots.Length]; _recheck = new float[slots.Length]; }
            _order.Clear();
            for (int i = 0; i < slots.Length; i++)
            {
                GrinderSlot s = slots[i];
                if (s == null || s.IsFree || !s.Landed || s.remaining <= 0) { _acc[i] = 1f; _recheck[i] = 0f; continue; }
                _order.Add(s);
            }
            _order.Sort((a, b) => a.Seq.CompareTo(b.Seq));
            BoardReach reach = _board.Reach;
            foreach (GrinderSlot s in _order)
            {
                int i = System.Array.IndexOf(slots, s);
                _acc[i] = Mathf.Min(_acc[i] + dt * _config.crabSpawnRate, Mathf.Max(1f, _config.crabSpawnBurst));
                if (s.IsWaiting) { _recheck[i] -= dt; if (_recheck[i] > 0f) continue; _recheck[i] = 0.1f; }
                else if (_acc[i] < 1f) continue;
                if (!_crabs.CanSpawn) break;

                string color = s.ColorCode;
                Vector3 start = s.DockPosition;
                var cands = reach.Search(_board.Blocks, color, start);
                int spawned = 0;
                bool any = false;
                foreach (BoardReach.Candidate c in cands)
                {
                    if (!_crabs.CanReserve(c.Block)) continue;
                    any = true;
                    if (_acc[i] < 1f || !_crabs.CanSpawn) break;
                    _crabs.Spawn(color, start, reach.MakeTarget(c));
                    _acc[i] -= 1f;
                    spawned++;
                    if (s.SpendCharge()) break; // nhả hết → máy trống
                }
                if (!any) { s.SetWaiting(true); _acc[i] = Mathf.Min(_acc[i], 1f); }
                else s.SetWaiting(false);
            }
        }

        void NotifyIfChanged()
        {
            int busy = BusyCount;
            if (busy == _lastBusy) return;
            _lastBusy = busy;
            OnBusyChanged?.Invoke(busy, Count);
            if (statusLabel != null) statusLabel.SetText("WAITING {0}/{1}", busy, Count);
        }

        /// <summary>Dọn hết máy khi đổi level</summary>
        public void ClearAll()
        {
            if (slots != null) foreach (GrinderSlot s in slots) if (s != null) s.ResetSlot();
            _lastBusy = -1;
            _spawnTimer = 0f;
            if (_crabs != null) _crabs.ClearAll();
        }
    }
}
