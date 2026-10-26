using System.Collections.Generic;
using UnityEngine;

namespace BounceBlast.UI
{
    /// <summary>Kiểu chuyển trang "xuất hiện so le"</summary>
    [DisallowMultipleComponent]
    public class PageTransition : MonoBehaviour
    {
        [Tooltip("Các thành phần theo thứ tự xuất hiện")] [SerializeField] UiAppear[] items = new UiAppear[0];
        [Tooltip("Giây giữa hai thành phần liên tiếp")] [SerializeField] float stagger = 0.05f;
        [Tooltip("Trễ trước thành phần đầu tiên")] [SerializeField] float startDelay;
        [Tooltip("Tắt để chuyển trang không có hiệu ứng")] [SerializeField] bool playOnEnter = true;

        /// <summary>Xử lý chuẩn bị chuyển trang</summary>
        public void Prepare(int dir = 0)
        {
            if (!playOnEnter) return;
            foreach (UiAppear a in items) if (a != null) a.Prepare(dir);
        }

        /// <summary>Xử lý chạy hiệu ứng chuyển trang</summary>
        public void Play(int dir = 0)
        {
            if (!playOnEnter) { Finish(); return; }
            for (int i = 0; i < items.Length; i++) if (items[i] != null) items[i].Play(startDelay + i * stagger, dir);
        }

        /// <summary>Xử lý kết thúc chuyển trang</summary>
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
