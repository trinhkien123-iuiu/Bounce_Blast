using System;
using UnityEngine;
using BounceBlast.UI;

namespace BounceBlast.Gameplay
{
    /// <summary>4 booster dưới đáy màn hình</summary>
    public class BoosterSystem
    {
        public event Action OnChanged;
        public BoosterType? Armed { get; private set; }

        /// <summary>Dùng cho lấy số lượt còn lại của booster</summary>
        public int Count(BoosterType t) => PlayerPrefs.GetInt("bb_booster_" + t, 0);

        /// <summary>Xử lý cộng lượt booster</summary>
        public void Add(BoosterType t, int n)
        {
            PlayerPrefs.SetInt("bb_booster_" + t, Count(t) + n);
            OnChanged?.Invoke();
        }

        /// <summary>Xử lý trừ một lượt booster</summary>
        public void Consume(BoosterType t)
        {
            PlayerPrefs.SetInt("bb_booster_" + t, Mathf.Max(0, Count(t) - 1));
            if (Armed == t) Armed = null;
            OnChanged?.Invoke();
        }

        /// <summary>Xử lý bật/tắt booster đang lên cò</summary>
        public void Toggle(BoosterType t)
        {
            Armed = Armed == t ? (BoosterType?)null : t;
            OnChanged?.Invoke();
        }

        /// <summary>Xử lý bỏ lên cò</summary>
        public void ResetArmed()
        {
            Armed = null;
            OnChanged?.Invoke();
        }
    }
}
