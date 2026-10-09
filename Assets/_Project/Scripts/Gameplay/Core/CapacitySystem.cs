using System;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>
    /// Sức chứa bi (gốc: class Capacity). Used = số bi đã rời khay mà chưa bị tiêu hao (trên máng + trong bàn).
    /// Bấm 1 hộp làm Used vượt Max → "Out Of Capacity" (thua, hoặc dùng booster tăng sức chứa).
    /// Video: hộp 40 bi / capacity 240 → thanh nhảy 16% ngay khi bấm.
    /// </summary>
    public class CapacitySystem
    {
        public event Action OnChanged;

        public float Max { get; private set; }
        public float OriginalMax { get; private set; }
        public int Used { get; private set; }
        public bool Infinite { get; set; }
        /// <summary>Đang chờ quyết định Out Of Capacity: thanh hiện kín 100% dù chưa cộng bi của hộp vừa bấm.</summary>
        public bool ForceFull { get; set; }

        public float Fill => Infinite || Max <= 0f ? 0f : ForceFull ? 1f : Mathf.Min(1f, Used / Max);

        public void Reset(int capacity)
        {
            Max = OriginalMax = Mathf.Max(1, capacity);
            Used = 0;
            Infinite = false;
            ForceFull = false;
            OnChanged?.Invoke();
        }

        public bool CanAdd(int balls) => Infinite || Used + balls <= Max + 0.01f;

        public void Add(int balls)
        {
            Used += balls;
            OnChanged?.Invoke();
        }

        public void Remove(int balls = 1)
        {
            Used = Mathf.Max(0, Used - balls);
            OnChanged?.Invoke();
        }

        /// <summary>Booster Capacity Increase: +25% sức chứa gốc.</summary>
        public void Increase(float fractionOfOriginal = 0.25f)
        {
            Max += OriginalMax * fractionOfOriginal;
            OnChanged?.Invoke();
        }

        /// <summary>Màu thanh theo mốc 0 / 50 / 75 / 100% (gốc: colorZeroP, colorFiftyP, colorSeventyFiveP, colorOneHundredP).</summary>
        public static Color FillColor(float fill)
        {
            Color c0 = new Color(0.36f, 0.88f, 0.13f), c50 = new Color(1f, 0.83f, 0.05f);
            Color c75 = new Color(1f, 0.55f, 0.05f), c100 = new Color(1f, 0.17f, 0.17f);
            if (fill < 0.5f) return Color.Lerp(c0, c50, fill / 0.5f);
            if (fill < 0.75f) return Color.Lerp(c50, c75, (fill - 0.5f) / 0.25f);
            return Color.Lerp(c75, c100, Mathf.Clamp01((fill - 0.75f) / 0.25f));
        }
    }
}
