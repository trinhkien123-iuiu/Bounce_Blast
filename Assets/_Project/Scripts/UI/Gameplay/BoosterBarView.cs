using System;
using System.Collections;
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
            [Tooltip("Hình nút khi booster còn khoá (ổ khoá vàng trên nền đúng tông độ khó)")] public Sprite locked;
            [Tooltip("Ô số tròn (Number / Hard Number / Super Hard Number)")] public Sprite number;
            [Tooltip("Màu dải nền dưới cùng ở độ khó này (đo từ ảnh gốc). Alpha 0 = giữ màu đang có")] public Color stripColor = new Color(1f, 1f, 1f, 0f);

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
        [Tooltip("Chữ \"Reach Lv N\" nổi lên trên nút khoá khi bấm vào (một cái dùng chung, dời tới nút được bấm)")]
        [SerializeField] TextMeshProUGUI unlockHint;
        [Tooltip("Độ cao chữ nhắc so với tâm nút (đơn vị canvas)")]
        [SerializeField] float unlockHintRaise = 150f;
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
        Style _style;
        readonly HashSet<BoosterType> _locked = new HashSet<BoosterType>();
        Coroutine _hintRoutine;
        // cỡ nút / vị trí mũi tên trong prefab – hiệu ứng chỉ cộng/nhân lên trên
        readonly Dictionary<Transform, Vector3> _baseScale = new Dictionary<Transform, Vector3>();
        readonly Dictionary<RectTransform, Vector2> _basePos = new Dictionary<RectTransform, Vector2>();

        Vector3 BaseScale(Transform t) { Vector3 v; if (!_baseScale.TryGetValue(t, out v)) _baseScale[t] = v = t.localScale; return v; }
        Vector2 BasePos(RectTransform t) { Vector2 v; if (!_basePos.TryGetValue(t, out v)) _basePos[t] = v = t.anchoredPosition; return v; }

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
            foreach (Entry e in boosters) if (e != null && e.warnArrow != null) BasePos(e.warnArrow);   // ghi vị trí gốc trước khi nhấp nhô
            // mũi tên nhấp nhô khi sức chứa gần đầy (video gốc lose_coche)
            float y = 8f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 1.6f)) * 26f;
            foreach (Entry e in boosters)
            {
                if (e == null || e.warnArrow == null) continue;
                bool show = _warnOn && !_locked.Contains(e.type);   // nút còn khoá thì không nhắc dùng
                if (e.warnArrow.gameObject.activeSelf != show) e.warnArrow.gameObject.SetActive(show);
                if (show) e.warnArrow.anchoredPosition = BasePos(e.warnArrow) + new Vector2(0f, y);
            }
        }

        /// <summary>Đổi hình 4 nút + ô số + màu dải nền theo độ khó (Normal xanh, Hard tím, Super Hard tối – theo SpriteConfig gốc).</summary>
        public void ApplyDifficulty(LevelDifficulty difficulty)
        {
            Style s = difficulty == LevelDifficulty.SuperHard ? superHard : difficulty == LevelDifficulty.Hard ? hard : normal;
            _style = s;
            if (strip != null && s.stripColor.a > 0f) strip.color = s.stripColor;
            foreach (Entry e in boosters)
            {
                if (e == null) continue;
                Sprite sp = _locked.Contains(e.type) && s.locked != null ? s.locked : s.For(e.type);
                if (sp != null && e.frame != null) e.frame.sprite = sp;
                if (s.number != null && e.badge != null)
                {
                    Image img = e.badge.GetComponent<Image>();
                    if (img != null) img.sprite = s.number;
                }
            }
        }

        /// <summary>Bật/tắt mũi tên nhắc dùng booster.</summary>
        public void SetWarn(bool on) => _warnOn = on;

        /// <summary>Cập nhật một nút: còn lượt thì hiện ô số, hết lượt thì hiện nút "+" xanh.</summary>
        public void SetBooster(BoosterType type, int count, bool armed)
        {
            Entry e = Find(type);
            if (e == null) return;
            if (_locked.Contains(type)) { ShowLockedLook(e); return; }
            bool has = count > 0;
            if (e.badge != null) e.badge.SetActive(has);
            if (e.badgeAdd != null) e.badgeAdd.SetActive(!has);
            if (has && e.count != null) e.count.text = count.ToString();
            if (e.frame != null)
            {
                e.frame.rectTransform.localScale = BaseScale(e.frame.rectTransform) * (armed ? 1.12f : 1f);
                e.frame.color = armed ? new Color(1f, 0.9f, 0.5f) : Color.white;
            }
        }

        void ShowLockedLook(Entry e)
        {
            if (e.badge != null) e.badge.SetActive(false);
            if (e.badgeAdd != null) e.badgeAdd.SetActive(false);
            if (e.frame != null)
            {
                e.frame.rectTransform.localScale = BaseScale(e.frame.rectTransform);
                e.frame.color = Color.white;
                if (_style != null && _style.locked != null) e.frame.sprite = _style.locked;
            }
        }

        /// <summary>Khoá / mở nút booster (khoá: hiện ổ khoá, ẩn số lượt; mở: trả hình nút của độ khó).</summary>
        public void SetLocked(BoosterType type, bool locked)
        {
            Entry e = Find(type);
            if (e == null) return;
            if (locked) { _locked.Add(type); ShowLockedLook(e); return; }
            _locked.Remove(type);
            if (_style != null && e.frame != null) e.frame.sprite = _style.For(type);
        }

        public bool IsLocked(BoosterType type) => _locked.Contains(type);

        /// <summary>Chữ "Reach Lv N" nổi lên trên nút đang khoá rồi mờ dần (video gốc: bấm nút khoá).</summary>
        public void ShowUnlockHint(BoosterType type, int level)
        {
            Entry e = Find(type);
            if (e == null || e.frame == null || unlockHint == null) return;
            if (_hintRoutine != null) StopCoroutine(_hintRoutine);
            _hintRoutine = StartCoroutine(HintRoutine(e, level));
        }

        IEnumerator HintRoutine(Entry e, int level)
        {
            unlockHint.text = "Reach\nLv " + level;
            RectTransform rt = unlockHint.rectTransform;
            rt.position = e.frame.rectTransform.position;
            Vector2 baseP = rt.anchoredPosition + Vector2.up * unlockHintRaise;
            unlockHint.gameObject.SetActive(true);
            const float fadeIn = 0.12f, hold = 1.0f, fadeOut = 0.35f;
            for (float t = 0f; t < fadeIn + hold + fadeOut; t += Time.unscaledDeltaTime)
            {
                float a = t < fadeIn ? t / fadeIn : t < fadeIn + hold ? 1f : 1f - (t - fadeIn - hold) / fadeOut;
                float rise = Mathf.Clamp01(t / fadeIn);
                rt.anchoredPosition = baseP + Vector2.up * (12f * (1f - rise));
                unlockHint.alpha = Mathf.Clamp01(a);
                yield return null;
            }
            unlockHint.gameObject.SetActive(false);
            _hintRoutine = null;
        }

        /// <summary>Toạ độ màn hình của nút (dùng đặt hiệu ứng hạt thế giới lên đúng chỗ).</summary>
        public Vector2 ButtonScreenPos(BoosterType type)
        {
            Entry e = Find(type);
            if (e == null || e.frame == null) return new Vector2(Screen.width * 0.5f, 0f);
            Canvas c = e.frame.canvas;
            Camera cam = c != null && c.renderMode != RenderMode.ScreenSpaceOverlay ? c.worldCamera : null;
            return RectTransformUtility.WorldToScreenPoint(cam, e.frame.rectTransform.position);
        }

        /// <summary>Mở khoá: ổ khoá rung lắc rồi bật ra thành nút booster (phồng lên rồi về cỡ gốc). onPop gọi lúc nút bật ra.</summary>
        public IEnumerator PlayUnlock(BoosterType type, Action onPop, Action onDone)
        {
            Entry e = Find(type);
            if (e == null || e.frame == null) { SetLocked(type, false); onPop?.Invoke(); onDone?.Invoke(); yield break; }
            RectTransform rt = e.frame.rectTransform;
            Vector3 baseScale = BaseScale(rt);
            Quaternion baseRot = rt.localRotation;
            if (_style != null && _style.locked != null) e.frame.sprite = _style.locked;
            yield return new WaitForSecondsRealtime(0.5f);   // chờ màn chơi hiện rõ rồi mới mở khoá
            const float shake = 0.7f;
            for (float t = 0f; t < shake; t += Time.unscaledDeltaTime)
            {
                float k = t / shake;
                rt.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * 70f) * 14f * (1f - k * 0.4f));
                rt.localScale = baseScale * (1f + 0.12f * k);
                yield return null;
            }
            rt.localRotation = baseRot;
            _locked.Remove(type);
            if (_style != null) e.frame.sprite = _style.For(type);
            onPop?.Invoke();
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                float k = t / 0.4f;
                rt.localScale = baseScale * Mathf.Lerp(1.35f, 1f, 1f - Mathf.Pow(1f - k, 3f)) * (1f + 0.08f * Mathf.Sin(k * 14f) * (1f - k));
                yield return null;
            }
            rt.localScale = baseScale;
            yield return new WaitForSecondsRealtime(0.7f);   // cho người chơi kịp nhìn nút vừa mở rồi mới hiện popup
            onDone?.Invoke();
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
