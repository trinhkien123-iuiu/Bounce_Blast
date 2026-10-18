using System;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// 4 booster dưới đáy màn hình (gốc: Booster_MagicHand / Hammer / Shuffle / CapacityIncrease):
    ///  - Magic Hand: bấm được hộp đạn chưa thông đường.
    ///  - Hammer: đập vỡ 1 khối trên bàn.
    ///  - Shuffle: xáo màu các hộp còn trong khay.
    ///  - Capacity: +25% sức chứa.
    /// Số lượng lưu PlayerPrefs; mặc định mỗi loại 2 lượt.
    /// </summary>
    public class BoosterSystem
    {
        public event Action OnChanged;
        public BoosterType? Armed { get; private set; }

        public int Count(BoosterType t) => PlayerPrefs.GetInt("bb_booster_" + t, 2);

        public bool HasAny
        {
            get
            {
                foreach (BoosterType t in Enum.GetValues(typeof(BoosterType))) if (Count(t) > 0) return true;
                return false;
            }
        }

        public void Add(BoosterType t, int n)
        {
            PlayerPrefs.SetInt("bb_booster_" + t, Count(t) + n);
            OnChanged?.Invoke();
        }

        public void Consume(BoosterType t)
        {
            PlayerPrefs.SetInt("bb_booster_" + t, Mathf.Max(0, Count(t) - 1));
            if (Armed == t) Armed = null;
            OnChanged?.Invoke();
        }

        public void Toggle(BoosterType t)
        {
            Armed = Armed == t ? (BoosterType?)null : t;
            OnChanged?.Invoke();
        }

        public void ResetArmed()
        {
            Armed = null;
            OnChanged?.Invoke();
        }
    }
}
