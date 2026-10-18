using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Lớp nền cho script quản lý của MỌI popup (Fail, Success, Settings, OutOfCapacity).
    /// Lo phần dùng chung để mỗi popup chỉ còn viết phần riêng của nó:
    ///  - bật popup, mở lại các CanvasGroup (prefab gốc để alpha 0 + script gốc tự fade-in, ta không có script đó);
    ///  - bật đúng khung theo độ khó (<see cref="normalOnly"/> / <see cref="hardOnly"/> / <see cref="superHardOnly"/>);
    ///  - hiệu ứng nảy vào (fade nền + EaseOutBack khung) thay cho tween DOTween của bản gốc;
    ///  - tắt popup (KHÔNG huỷ – popup đặt sẵn trong Hierarchy, xem Docs/kien-truc-ui-dung.md).
    /// Lớp con thêm tham chiếu riêng bằng [SerializeField], sự kiện riêng và hàm Show(...) riêng.
    /// </summary>
    public abstract class PopupViewBase : MonoBehaviour
    {
        [Header("Chung cho mọi popup")]
        [Tooltip("Canvas của popup – GameUI gán camera + thứ tự vẽ lúc chạy")]
        [SerializeField] Canvas canvas;
        [Tooltip("CanvasGroup gốc – dùng để fade nền khi nảy vào")]
        [SerializeField] CanvasGroup rootGroup;
        [Tooltip("Khung nảy vào (scale 0 → 1 kiểu EaseOutBack); để trống = không nảy")]
        [SerializeField] Transform content;
        [Tooltip("Chỉ hiện khi độ khó Normal")] [SerializeField] GameObject[] normalOnly = new GameObject[0];
        [Tooltip("Chỉ hiện khi độ khó Hard")] [SerializeField] GameObject[] hardOnly = new GameObject[0];
        [Tooltip("Chỉ hiện khi độ khó Super Hard")] [SerializeField] GameObject[] superHardOnly = new GameObject[0];

        const float PopTime = 0.3f;

        public Canvas Canvas => canvas != null ? canvas : GetComponentInChildren<Canvas>(true);
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Popup có hiệu ứng nảy vào chung không. Popup Thắng tự chạy chuỗi riêng nên tắt.</summary>
        protected virtual bool UsesPopIn => true;

        /// <summary>Bật popup + đặt khung theo độ khó + chạy hiệu ứng nảy. Lớp con gọi trong Show(...) của nó.</summary>
        protected void OpenBase(LevelDifficulty difficulty)
        {
            gameObject.SetActive(true);
            foreach (CanvasGroup cg in GetComponentsInChildren<CanvasGroup>(true)) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
            SetGroup(normalOnly, difficulty == LevelDifficulty.Normal);
            SetGroup(hardOnly, difficulty == LevelDifficulty.Hard);
            SetGroup(superHardOnly, difficulty == LevelDifficulty.SuperHard);
            if (UsesPopIn) StartCoroutine(PopIn());
        }

        /// <summary>Tắt popup (vẫn nằm trong scene để lần sau bật lại).</summary>
        public virtual void Hide() => gameObject.SetActive(false);

        static void SetGroup(GameObject[] objs, bool on)
        {
            if (objs == null) return;
            foreach (GameObject go in objs) if (go != null) go.SetActive(on);
        }

        IEnumerator PopIn()
        {
            if (rootGroup == null) rootGroup = GetComponent<CanvasGroup>();
            for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / PopTime)   // unscaled: game đang tạm dừng khi hiện popup Thua
            {
                if (rootGroup != null) rootGroup.alpha = Mathf.Clamp01(k * 2f);
                if (content != null) content.localScale = Vector3.one * EaseOutBack(k);
                yield return null;
            }
            if (rootGroup != null) rootGroup.alpha = 1f;
            if (content != null) content.localScale = Vector3.one;
        }

        protected static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
        }

        /// <summary>Lớp con thêm tên các ô tham chiếu riêng còn trống vào danh sách.</summary>
        protected virtual void CollectMissing(List<string> missing) { }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ) – GameUI gọi lúc dựng để lỗi hiện sớm, rõ ràng.</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (Canvas == null) miss.Add("canvas");
            if (rootGroup == null && GetComponent<CanvasGroup>() == null) miss.Add("rootGroup");
            if (UsesPopIn && content == null) miss.Add("content");
            CollectMissing(miss);
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
