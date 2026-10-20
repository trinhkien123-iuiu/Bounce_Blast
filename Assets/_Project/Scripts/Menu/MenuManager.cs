using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using BounceBlast.Gameplay;
using BounceBlast.UI;

namespace BounceBlast.Menu
{
    /// <summary>
    /// Điều phối scene MenuGame (màn hình chính). Mỗi phần UI là một prefab đặt cạnh nhau dưới <c>UI/</c>, có View riêng:
    /// <see cref="HomeView"/> (trang Home), <see cref="ShopPanelView"/> (trang Shop), <see cref="MenuSettingsView"/> (trang Settings),
    /// <see cref="BottomBarView"/> (thanh 5 tab), <see cref="ToastView"/> (dòng thông báo) và <see cref="ProfilePopupView"/> (popup Profile).
    /// MenuManager nghe sự kiện của các View, bật đúng trang theo tab, ghi dữ liệu người chơi rồi chuyển sang Gameplay.
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        [Header("Asset")]
        [Tooltip("Dùng chung với Gameplay: danh sách level (đọc độ khó), âm thanh, nhạc")] [SerializeField] GameConfig config;

        [Tooltip("Âm thanh – bỏ trống thì tự gắn vào object này lúc chạy")] [SerializeField] AudioManager audioManager;

        [Header("Scene (prefab đặt sẵn dưới UI/)")]
        [FormerlySerializedAs("view")] [SerializeField] HomeView home;
        [SerializeField] ShopPanelView shop;
        [SerializeField] MenuSettingsView settings;
        [SerializeField] BottomBarView bottomBar;
        [SerializeField] ToastView toast;
        [Tooltip("Popup Profile (bấm avatar ở Home)")] [SerializeField] ProfilePopupView profile;

        [Header("Tab")]
        [Tooltip("Vuốt ngang chuyển trang (UI/MenuPager) – giữ danh sách trang theo tab")] [SerializeField] MenuPager pager;
        [Tooltip("Tab đang chọn lúc mở Menu")] [SerializeField] int startTab = 2;
        [Tooltip("Vị trí tab Shop (nút '+' cạnh xu / tim mở tab này)")] [SerializeField] int shopTab = 0;

        bool _ready;
        float _clock;

        void Awake()
        {
            Time.timeScale = 1f;
            AudioManager.Init(audioManager != null ? audioManager.gameObject : gameObject, config.sounds);

            string missing = Validate();
            if (missing != null) { Debug.LogError("MenuManager: chưa nối đủ: " + missing); return; }
            _ready = true;

            home.OnPlay += SceneFlow.GoToGameplay;
            home.OnOpenShop += () => SelectTab(shopTab);
            home.OnComingSoon += () => toast.Show("Coming soon!");
            home.OnOpenProfile += () => profile.Show(PlayerData.ProfileName, PlayerData.Avatar);
            home.Streak.OnClaim += ClaimStreak;

            bottomBar.OnTabClicked += SelectTab;
            pager.OnPageChanged += bottomBar.Highlight;

            settings.OnSoundChanged += v => AudioManager.SoundOn = v;
            settings.OnMusicChanged += v => AudioManager.MusicOn = v;
            settings.OnVibrationChanged += v => PlayerPrefs.SetInt("bb_vibration", v ? 1 : 0);

            shop.OnBuy += Buy;
            shop.OnFreeCoin += ClaimFreeCoins;

            profile.OnClose += profile.Hide;
            profile.OnSave += SaveProfile;
        }

        void Start()
        {
            if (!_ready) return;
            PlayerData.UseLevelSet(config.ActiveSetName);
            int level = Mathf.Min(PlayerData.Level, config.MaxLevel);
            LevelModel model = LevelParser.Load(config, level);
            home.SetLevel(level, model != null ? model.Difficulty : LevelDifficulty.Normal);
            RefreshWallet();
            RefreshStreak(true);
            shop.SetFreeCoinReadyAt(PlayerData.FreeCoinReadyAt);
            home.SetAvatar(profile.Avatar(PlayerData.Avatar));
            profile.Hide();
            settings.SetValues(AudioManager.SoundOn, AudioManager.MusicOn, PlayerPrefs.GetInt("bb_vibration", 1) == 1);
            pager.Show(startTab, false);
            bottomBar.Highlight(startTab);
            AudioManager.PlayMusic(config.musicMenu);
        }

        void Update()
        {
            // đồng hồ tim vô hạn đếm lùi mỗi giây
            if (!_ready) return;
            pager.Locked = profile.gameObject.activeSelf;   // popup Profile đang mở thì không vuốt trang
            _clock -= Time.unscaledDeltaTime;
            if (_clock > 0f) return;
            _clock = 1f;
            home.SetHearts(PlayerData.MaxHearts, PlayerData.InfiniteHeartsLeft);
            home.Streak.SetTimer(BounceStreak.TimeLeft);
        }

        void SaveProfile(string playerName, int avatar)
        {
            if (string.IsNullOrEmpty(playerName)) { toast.Show("Please enter a name!"); return; }
            PlayerData.ProfileName = playerName;
            PlayerData.Avatar = avatar;
            home.SetAvatar(profile.Avatar(avatar));
            profile.Hide();
            toast.Show("Profile saved!");
        }

        /// <summary>Chọn một tab: trượt sang trang của nó. Tab chưa có trang → chỉ báo "coming soon", không đổi tab.</summary>
        void SelectTab(int index)
        {
            if (index < 0 || index >= pager.Count) return;
            if (!pager.HasPage(index))
            {
                toast.Show(bottomBar.Title(index) + " - coming soon!");
                return;
            }
            pager.Show(index, true);
        }

        /// <summary>Bản dựng lại không có mua hàng thật: bấm mua là nhận luôn phần thưởng của gói.</summary>
        void Buy(ShopOfferItem offer)
        {
            PlayerData.Coins += offer.coins;
            if (offer.boosters > 0)
            {
                var boosters = new BoosterSystem();
                foreach (BoosterType t in Enum.GetValues(typeof(BoosterType))) boosters.Add(t, offer.boosters);
            }
            PlayerData.AddInfiniteHearts(offer.infiniteHeartMinutes);
            if (offer.noAds) PlayerData.NoAds = true;
            AudioManager.Play(Sfx.Magic);
            toast.Show(offer.title + " purchased!");
            RefreshWallet();
        }

        void ClaimFreeCoins(int coins)
        {
            PlayerData.Coins += coins;
            PlayerData.FreeCoinReadyAt = DateTime.UtcNow.AddHours(shop.FreeCoinCooldownHours);
            shop.SetFreeCoinReadyAt(PlayerData.FreeCoinReadyAt);
            AudioManager.Play(Sfx.Magic);
            toast.Show("+" + coins + " coins!");
            RefreshWallet();
        }

        void ClaimStreak()
        {
            int minutes = BounceStreak.Claim();
            if (minutes <= 0) return;
            toast.Show("Unlimited lives +" + ShopPanelView.FormatMinutes(minutes) + "!");
            RefreshStreak(false);
            RefreshWallet();
        }

        void RefreshWallet()
        {
            home.SetCoins(PlayerData.Coins);
            shop.SetCoins(PlayerData.Coins);
            home.SetHearts(PlayerData.MaxHearts, PlayerData.InfiniteHeartsLeft);
        }

        void RefreshStreak(bool snap)
        {
            home.Streak.SetTimer(BounceStreak.TimeLeft);   // đọc trước: hết đợt thì chuỗi về 0
            home.Streak.Set(BounceStreak.Wins, BounceStreak.Target, BounceStreak.Reward, snap);
        }

        /// <summary>Báo ô tham chiếu nào còn trống (null nếu đủ).</summary>
        string Validate()
        {
            var miss = new List<string>();
            Check(home, "home", home != null ? home.Validate() : null, miss);
            Check(shop, "shop", shop != null ? shop.Validate() : null, miss);
            Check(settings, "settings", settings != null ? settings.Validate() : null, miss);
            Check(bottomBar, "bottomBar", bottomBar != null ? bottomBar.Validate() : null, miss);
            Check(toast, "toast", toast != null ? toast.Validate() : null, miss);
            Check(profile, "profile", profile != null ? profile.Validate() : null, miss);
            Check(pager, "pager", pager != null ? pager.Validate() : null, miss);
            if (bottomBar != null && pager != null && pager.Count != bottomBar.Count) miss.Add("pager.pages (cần " + bottomBar.Count + " phần tử)");
            return miss.Count == 0 ? null : string.Join(", ", miss);
        }

        static void Check(UnityEngine.Object view, string name, string inner, List<string> miss)
        {
            if (view == null) miss.Add(name);
            else if (inner != null) miss.Add(name + "(" + inner + ")");
        }
    }
}
