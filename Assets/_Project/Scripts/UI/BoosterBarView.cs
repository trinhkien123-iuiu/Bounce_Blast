using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BounceBlast.Gameplay;

namespace BounceBlast.UI
{
    /// <summary>
    /// Script quản lý của prefab BoosterBar – gắn ngay trên gốc prefab.
    ///
    /// Giữ tham chiếu tới các con bằng Inspector (không đi tìm theo tên lúc chạy), và phơi ra API cho GameUI:
    /// bấm nút, đổi số lượt, tô màu dải nền theo độ khó, bật mũi tên nhắc dùng booster.
    /// Thêm/bớt/đổi chỗ nút thì sửa trong prefab rồi kéo lại ô tham chiếu, không phải đụng code.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoosterBarView : MonoBehaviour
    {
        /// <summary>Một nút booster và các mảnh của nó.</summary>
        [Serializable]
        public class Entry
        {
            public BoosterType type;
            [Tooltip("Cả nút (sprite gốc: khung + icon)")] public Image frame;
            [Tooltip("Nút bấm – để trống thì lấy Button trên frame")] public Button button;
            [Tooltip("Ô số tròn, hiện khi còn lượt")] public GameObject badge;
            [Tooltip("Chữ số lượt còn lại")] public TextMeshProUGUI count;
            [Tooltip("Nút \"+\" xanh, hiện khi hết lượt")] public GameObject badgeAdd;
            [Tooltip("Mũi tên vàng nhắc dùng – để trống nếu nút này không cần")] public RectTransform warnArrow;
        }

        /// <summary>Bộ hình các nút booster ở một độ khó (gốc: SpriteConfig.spritesBooster – sprite / spriteHardOverride / spriteSuperHardOverride).</summary>
        [Serializable]
        public class Style
        {
            public Sprite magicHand;
            public Sprite hammer;
            public Sprite shuffle;
            public Sprite capacity;
            [Tooltip("Ô số tròn (Number / Hard Number / Super Hard Number)")] public Sprite number;

            public Sprite For(BoosterType t)
            {
                switch (t)
                {
                    case BoosterType.MagicHand: return magicHand;
                    case BoosterType.Hammer: return hammer;
                    case BoosterType.Shuffle: return shuffle;
                    default: return capacity;
                }
            }
        }

        [Tooltip("Dải nền dưới cùng (đổi tông theo độ khó)")]
        [SerializeField] Image strip;
        [Tooltip("Hàng chứa 4 nút – dùng để đo mép trên khi cân bố cục màn hình")]
        [SerializeField] RectTransform buttonsRow;
        [SerializeField] List<Entry> boosters = new List<Entry>();

        [Header("Hình nút theo độ khó (để trống một ô = giữ hình đang có)")]
        [SerializeField] Style normal = new Style();
        [SerializeField] Style hard = new Style();
        [SerializeField] Style superHard = new Style();

        /// <summary>Bấm vào một nút booster.</summary>
        public event Action<BoosterType> OnPressed;

        /// <summary>Hàng nút (GameUI đo mép trên của nó để cân bàn chơi).</summary>
        public RectTransform ButtonsRow => buttonsRow;

        bool _warnOn;

        void Awake()
        {
            foreach (Entry e in boosters)
            {
                if (e == null || e.frame == null) continue;
                if (e.button == null) e.button = e.frame.GetComponent<Button>();
                if (e.button == null) continue;
                BoosterType captured = e.type;
                e.button.onClick.AddListener(() => { AudioManager.Play(Sfx.Button); OnPressed?.Invoke(captured); });
            }
        }

        void LateUpdate()
        {
            // mũi tên nhấp nhô khi sức chứa gần đầy (video gốc lose_coche)
            float y = 8f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 1.6f)) * 26f;
            foreach (Entry e in boosters)
            {
                if (e == null || e.warnArrow == null) continue;
                if (e.warnArrow.gameObject.activeSelf != _warnOn) e.warnArrow.gameObject.SetActive(_warnOn);
                if (_warnOn) e.warnArrow.anchoredPosition = new Vector2(0f, y);
            }
        }

        /// <summary>Đổi hình 4 nút + ô số theo độ khó (Normal xanh, Hard tím, Super Hard tối – theo SpriteConfig gốc).</summary>
        public void ApplyDifficulty(LevelDifficulty difficulty)
        {
            Style s = difficulty == LevelDifficulty.SuperHard ? superHard : difficulty == LevelDifficulty.Hard ? hard : normal;
            foreach (Entry e in boosters)
            {
                if (e == null) continue;
                Sprite sp = s.For(e.type);
                if (sp != null && e.frame != null) e.frame.sprite = sp;
                if (s.number != null && e.badge != null)
                {
                    Image img = e.badge.GetComponent<Image>();
                    if (img != null) img.sprite = s.number;
                }
            }
        }

        /// <summary>Tông dải nền theo độ khó (màu đo từ ảnh gốc).</summary>
        public void SetStripColor(Color c) { if (strip != null) strip.color = c; }

        /// <summary>Bật/tắt mũi tên nhắc dùng booster.</summary>
        public void SetWarn(bool on) => _warnOn = on;

        /// <summary>Cập nhật một nút: còn lượt thì hiện ô số, hết lượt thì hiện nút "+" xanh.</summary>
        public void SetBooster(BoosterType type, int count, bool armed)
        {
            Entry e = Find(type);
            if (e == null) return;
            bool has = count > 0;
            if (e.badge != null) e.badge.SetActive(has);
            if (e.badgeAdd != null) e.badgeAdd.SetActive(!has);
            if (has && e.count != null) e.count.text = count.ToString();
            if (e.frame != null)
            {
                e.frame.rectTransform.localScale = Vector3.one * (armed ? 1.12f : 1f);
                e.frame.color = armed ? new Color(1f, 0.9f, 0.5f) : Color.white;
            }
        }

        Entry Find(BoosterType type)
        {
            foreach (Entry e in boosters) if (e != null && e.type == type) return e;
            return null;
        }

        /// <summary>Báo ô tham chiếu nào còn trống (gọi khi dựng để lỗi hiện sớm, rõ ràng).</summary>
        public string Validate()
        {
            var miss = new List<string>();
            if (strip == null) miss.Add("strip");
            if (buttonsRow == null) miss.Add("buttonsRow");
            foreach (Entry e in boosters)
            {
                if (e == null) { miss.Add("(entry rỗng)"); continue; }
                if (e.frame == null) miss.Add(e.type + ".frame");
                if (e.badge == null) miss.Add(e.type + ".badge");
                if (e.count == null) miss.Add(e.type + ".count");
            }
            if (boosters.Count == 0) miss.Add("danh sách nút rỗng");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }
    }
}
