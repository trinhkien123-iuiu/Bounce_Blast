using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.UI
{
    /// <summary>
    /// Kiểu chuyển trang "xuất hiện so le": gắn trên gốc một trang (Home, Shop, Settings…), giữ danh sách <see cref="UiAppear"/>
    /// theo THỨ TỰ xuất hiện. <see cref="MenuPager"/> gọi <see cref="Prepare"/> khi trang vừa lộ ra (các thành phần ẩn đi)
    /// và <see cref="Play"/> khi trang tới nơi (lần lượt hiện, cách nhau <see cref="stagger"/> giây).
    /// Thêm / bớt / đổi thứ tự thành phần ngay trong Inspector; chuột phải ▸ "Lấy các UiAppear con" để điền lại theo thứ tự Hierarchy.
    /// </summary>
    [DisallowMultipleComponent]
    public class PageTransition : MonoBehaviour
    {
        [Tooltip("Các thành phần theo thứ tự xuất hiện")] [SerializeField] UiAppear[] items = new UiAppear[0];
        [Tooltip("Giây giữa hai thành phần liên tiếp")] [SerializeField] float stagger = 0.05f;
        [Tooltip("Trễ trước thành phần đầu tiên")] [SerializeField] float startDelay;
        [Tooltip("Tắt để chuyển trang không có hiệu ứng")] [SerializeField] bool playOnEnter = true;

        /// <param name="dir">+1 = trang này nằm bên phải trang vừa rời, −1 = bên trái (UiAppear có followDirection dùng để chọn chiều trượt)</param>
        public void Prepare(int dir = 0)
        {
            if (!playOnEnter) return;
            foreach (UiAppear a in items) if (a != null) a.Prepare(dir);
        }

        public void Play(int dir = 0)
        {
            if (!playOnEnter) { Finish(); return; }
            for (int i = 0; i < items.Length; i++) if (items[i] != null) items[i].Play(startDelay + i * stagger, dir);
        }

        public void Finish()
        {
            foreach (UiAppear a in items) if (a != null) a.Finish();
        }

#if UNITY_EDITOR
        [ContextMenu("Lấy các UiAppear con")]
        void Collect()
        {
            var list = new List<UiAppear>(GetComponentsInChildren<UiAppear>(true));
            items = list.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
