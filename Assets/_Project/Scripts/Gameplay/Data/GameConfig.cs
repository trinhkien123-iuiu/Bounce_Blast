using System;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tham chiếu asset + thông số gameplay</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Level")]
        [Tooltip("Danh mục dữ liệu level (Data/Levels). Tạo bằng menu Tools/Bounce Blast/Data/Build Level Sets")]
        public LevelSet levels;
        [Tooltip("Số level của bộ gốc")] public int maxLevel = 550;
        [Tooltip("Bộ level đang chơi: Original = 550 level CSV gốc; Custom = bộ tự làm bằng Level Editor")]
        public LevelSource levelSource = LevelSource.Original;
        [Tooltip("Bộ level tự làm (Data/Levels/custom/LevelPack_custom.asset)")] public LevelPack customLevels;

        /// <summary>Level lớn nhất của bộ đang chơi</summary>
        public int MaxLevel => levelSource == LevelSource.Custom ? Mathf.Max(1, customLevels != null ? customLevels.Count : 1) : maxLevel;
        /// <summary>Tên bộ đang chơi (dùng làm khoá lưu tiến độ riêng từng bộ)</summary>
        public string ActiveSetName => levelSource == LevelSource.Custom ? "custom" : "original";

        [Header("Prefab gốc (Resources/prefabs)")]
        public GameObject cubePrefab;
        public GameObject combinedCubePrefab;
        [Tooltip("Hòm gỗ gộp (CombinedCubicCrate) – lấy ván chéo mặt trước (Model/Planes)")]
        public GameObject cratePrefab;
        [Tooltip("Hòm gỗ từng ô (ModularPrimitiveCrate) – mảnh thân hòm theo hàng xóm + mảnh vỡ CrateFracture")]
        public GameObject crateModularPrefab;
        public GameObject ammoPrefab;
        public GameObject tunnelPrefab;
        public GameObject ballPrefab;
        [Tooltip("Nắp cửa kính của hộp đạn (AmmunationBlockerCover) – lấy material của node Cover")]
        public GameObject ammoCoverPrefab;
        [Tooltip("Mảng tường khay đạn (ModularPrimitiveAmmoWall - Wallbase) – lấy material t4-wall làm nền khay")]
        public GameObject ammoTrayWallPrefab;
        [Tooltip("Cột /wl trên bàn (ModularPrimitiveColumn) – các mảnh thân cột")]
        public GameObject columnPrefab;
        [Tooltip("Cột gộp (CombinedCubicColumn) – lấy huy hiệu số máu")]
        public GameObject combinedColumnPrefab;
        public GameObject cubeBreakVfx;
        public GameObject crateBreakVfx;
        public GameObject ammoLaunchVfx;
        public GameObject confettiVfx;

        [Header("Font chữ 3D (số máu, số băng, số tunnel)")]
        public TMPro.TMP_FontAsset worldFont;

        [Header("Material")]
        public Material wallMaterial;
        public Material crateMaterial;
        public Material ammoCellMaterial;
        public Material ammoFrameMaterial;
        public Material trailMaterial;
        public Material iceMaterial;
        public Material qmarkMaterial;

        [Header("Theme theo độ khó")]
        public Theme normalTheme;
        public Theme hardTheme;
        public Theme superHardTheme;

        [Header("Âm thanh")]
        public SoundEntry[] sounds;
        [Tooltip("Nhạc nền màn hình chính (MenuGame)")] public AudioClip musicMenu;
        public AudioClip musicNormal;
        public AudioClip musicHard;
        public AudioClip musicSuperHard;

        [Header("Bàn chơi (toạ độ world, lấy từ collider gốc của Board - Default)")]
        public Rect arena = new Rect(1.07f, -6.35f, 8.46f, 14.0f);
        public float arenaTopY = 7.45f;
        [Tooltip("Điểm thả bi vào bàn: phải nằm DƯỚI mép bịt cửa phun (y = 6.92) và trên nêm tam giác (đỉnh y = 6.30)")]
        public Vector2 dispenserOutlet = new Vector2(5.30f, 6.62f);

        [Header("Bi")]
        public float ballRadius = 0.17f;
        public float ballSpeed = 11f;
        public float ballSpawnSpread = 55f;
        [Tooltip("Tốc độ dọc tối thiểu sau mỗi lần nảy – chống bi nảy ngang mãi giữa 2 vách")]
        public float ballMinVerticalSpeed = 1.6f;
        [Tooltip("Lệch ngẫu nhiên (độ) mỗi lần nảy, tránh quỹ đạo lặp vô hạn")]
        public float ballDeflectDeg = 8f;
        [Tooltip("Số lần nảy tối thiểu trước khi bi được phép ngắm thẳng vào khối cùng màu")]
        public int ballAimMinBounces = 3;

        [Header("Máng (conveyor)")]
        [Tooltip("Số bi dựng sẵn vào pool lúc vào màn (rải vài viên mỗi khung). Hộp đầu tiên lên máy xay không phải dựng bi → hết khựng trên máy yếu")]
        [Range(0, 200)] public int ballPrewarm = 60;
        public float laneSpeed = 5.5f;
        public float laneBallSpacing = 0.27f;
        public float boxFlyDuration = 0.6f;
        [Tooltip("Khoảng cách giữa 2 mũi tên chạy trên máng")]
        public float laneArrowSpacing = 1.25f;
        [Tooltip("Tốc độ mũi tên chạy trên máng")]
        public float laneArrowSpeed = 1.5f;

        [Header("Khay đạn")]
        public float ammoCellMax = 1.70f;
        public float ammoAreaWidth = 11.9f;
        public float ammoAreaTop = -7.9f;
        public float ammoAreaBottom = -16f;
        [Tooltip("Thời gian (giây) logo + pháo hoa/confetti chạy sau khi chữ BOUNCE BLAST hiện xong, rồi mới hiện bảng thắng")]
        public float winFireworksTime = 2.4f;
        public float ammoActiveScale = 0.9f;
        public float ammoInactiveScale = 0.88f;
        [Tooltip("Cỡ hộp so với bước giữa các hố (1 = như cũ). Nhỏ hơn 1 = khoảng cách hố tương đối dãn ra, hộp (kể cả hộp băng/cửa kính to) không dính nhau; vị trí hộp vẫn theo tâm hố")]
        [Range(0.5f, 1.2f)] public float ammoBoxVsHole = 1f;

        [Header("Bố cục màn hình (camera tự cân theo HUD & hàng booster – xem CameraFitter)")]
        [Tooltip("Mép trên NGOÀI của khung máng (y thế giới). Camera đặt mốc này ngang tâm hàng HUD (Lv, Settings) như game gốc")]
        public float frameTopY = 8.43f;
        [Tooltip("Nửa bề ngang vùng nhìn khi KHÔNG thu nhỏ – 5.98 cho khung máng phủ ~90% bề ngang như game gốc")]
        public float viewHalfWidth = 5.98f;
        [Tooltip("Thu nhỏ tối đa khi màn hình thấp, để khay đạn đủ chỗ (1 = không thu, 1.3 = cảnh nhỏ đi ~23%)")]
        public float viewMaxZoomOut = 1.3f;
        [Tooltip("Nửa chiều cao tối thiểu của vùng nhìn – chặn máy bè (tablet) phóng cảnh quá to")]
        public float viewMinHalfHeight = 11.6f;
        [Tooltip("Cỡ ô khay mong muốn; thiếu chỗ thì camera thu nhỏ cảnh vừa đủ để đạt (ảnh gốc Lv44 ≈ 1.45)")]
        public float ammoTargetCell = 1.45f;
        [Tooltip("Khe giữa đáy khay đạn và mép trên hàng nút booster (đơn vị thế giới)")]
        public float ammoBoosterGap = 0.2f;

        [Header("Hộp đạn – tư thế trong khay")]
        [Tooltip("Hộp ĐANG bấm được (nhô khỏi hố)")]
        public AmmoPose ammoActivePose = new AmmoPose { tiltX = -55f, length = 0.6f, offset = new Vector3(-0.01f, 0.69f, 0f), scale = 1.038f };
        [Tooltip("Hộp CHƯA tới lượt (lún trong hố)")]
        public AmmoPose ammoInactivePose = new AmmoPose { tiltX = -78f, length = 0.4f, offset = new Vector3(0f, 0.07f, 0f), scale = 1.06f };
        [Tooltip("Độ lún của hộp chưa tới lượt (theo ô, ra xa camera). Lớn = chìm sau mặt khay, nhỏ = nổi ngang mặt khay")]
        [Range(0f, 2f)] public float ammoSinkDepth = 1.1f;

        [Header("Onboarding level đầu (gốc: KPTutorialPanel / KPBoosterOnBoarder / holdWallToSpeedUp)")]
        [Tooltip("Level hiện chữ \"Tap to select\" + tay chỉ vào hộp bấm được")] public int onboardTapLevel = 1;
        [Tooltip("Level nhắc \"Watch the capacity!\"")] public int onboardCapacityLevel = 2;
        [Tooltip("Level nhắc \"HOLD WALL TO SPEED UP!\"")] public int onboardSpeedLevel = 3;

        /// <summary>Một cơ chế mới (popup "New Feature" hiện lần đầu cơ chế xuất hiện trong level)</summary>
        [Serializable]
        public class FeatureIntro
        {
            [Tooltip("BigCube, Qmark, Tunnel, Ice, Crate, Cover, Column – khớp OnboardingManager.FeaturesIn")] public string id;
            public string title;
            [TextArea(1, 3)] public string description;
            public Sprite icon;
        }
        public System.Collections.Generic.List<FeatureIntro> featureIntros = new System.Collections.Generic.List<FeatureIntro>();

        /// <summary>Dùng cho lấy nội dung popup cơ chế mới theo id</summary>
        public FeatureIntro FeatureIntroOf(string id) => featureIntros.Find(f => f != null && f.id == id);

        [Header("Mở khoá booster theo level (gốc: KPBoosterDependencies.UnlockLevel)")]
        [Tooltip("Level bắt đầu dùng được Magic Hand")] public int unlockMagicHand = 6;
        [Tooltip("Level bắt đầu dùng được Hammer")] public int unlockHammer = 9;
        [Tooltip("Level bắt đầu dùng được Shuffle")] public int unlockShuffle = 12;
        [Tooltip("Level bắt đầu dùng được Capacity")] public int unlockCapacity = 15;

        [Tooltip("Số lượt miễn phí để thử mỗi khi một booster được mở khoá")] public int freeBoosterOnUnlock = 2;

        /// <summary>Level mở khoá của một booster</summary>
        public int BoosterUnlockLevel(BoosterType t)
        {
            switch (t)
            {
                case BoosterType.MagicHand: return unlockMagicHand;
                case BoosterType.Hammer: return unlockHammer;
                case BoosterType.Shuffle: return unlockShuffle;
                default: return unlockCapacity;
            }
        }

        /// <summary>Booster kế tiếp sẽ mở khoá sau level level</summary>
        public bool NextBoosterUnlock(int level, out BoosterType type, out float from, out float to)
        {
            type = BoosterType.MagicHand; from = to = 0f;
            int next = int.MaxValue, prev = 0;
            foreach (BoosterType t in System.Enum.GetValues(typeof(BoosterType)))
            {
                int u = BoosterUnlockLevel(t);
                if (u > level && u < next) { next = u; type = t; }
                if (u <= level && u > prev) prev = u;
            }
            if (next == int.MaxValue) return false;
            float span = Mathf.Max(1, next - prev);
            from = Mathf.Clamp01((level - prev) / span);
            to = Mathf.Clamp01((level + 1 - prev) / span);
            return true;
        }

        /// <summary>Hình icon (không viền nút) của một booster</summary>
        public Sprite BoosterIcon(BoosterType t)
        {
            switch (t)
            {
                case BoosterType.MagicHand: return boosterMagicSprite;
                case BoosterType.Hammer: return boosterHammerSprite;
                case BoosterType.Shuffle: return boosterShuffleSprite;
                default: return boosterPlusSprite;
            }
        }

        [Header("Booster VFX")]
        [Tooltip("Icon booster dùng làm hình trong hiệu ứng (búa vung, tay phép, mũi tên xáo, dấu cộng)")]
        public Sprite boosterHammerSprite, boosterMagicSprite, boosterShuffleSprite, boosterPlusSprite;
        [Tooltip("Búa phá vùng bán kính bao nhiêu ô quanh điểm đập (1 ≈ vùng 3×3 gạch nhỏ, 2.4 ≈ 5×5, gạch ở rìa vẫn vỡ nếu vòng chạm tới)")]
        [Range(0.6f, 4f)] public float hammerRadiusCells = 2.4f;
        [Tooltip("Hai khối liên tiếp trong vùng vỡ cách nhau bao nhiêu giây theo mỗi ô khoảng cách từ tâm (lan sóng)")]
        public float hammerWaveDelay = 0.045f;
        [Tooltip("Cỡ chuẩn của hiệu ứng tay phép / xáo / cộng (đơn vị thế giới)")]
        public float boosterFxUnit = 1.5f;

        [Header("Hộp đóng băng (khối băng bọc ngoài hộp)")]
        [Tooltip("Scale cục bộ của từng mức băng (IceCube…IceCube4), áp lên khối băng đã co theo hộp")]
        public Vector3 iceStageScale = new Vector3(1.13298f, 2.119927f, 0.79747f);
        [Tooltip("Vị trí cục bộ của từng mức băng")]
        public Vector3 iceStagePos = new Vector3(-0.08f, -0.14f, 0f);
        [Tooltip("Băng rộng hơn hộp mỗi bên bao nhiêu (theo ô). Nhỏ để 2 hộp băng kề nhau không dính")]
        public float icePadSide = 0.01f;
        [Tooltip("Băng dày thêm phía trên hộp (theo ô)")]
        public float icePadTop = 0.22f;
        [Tooltip("Băng dày thêm phía dưới hộp (theo ô)")]
        public float icePadBottom = 0.05f;

        [Header("Ammo tunnel (/tu-XD) – áp dụng khi nạp level")]
        [Tooltip("Cạnh lớn nhất của tunnel trên màn hình (theo ô khay)")]
        [Range(0.3f, 1.5f)] public float tunnelSize = 0.95f;
        /// <summary>Dùng cho tư thế riêng của thân ống (node Model/Tunnel) theo hướng nhả</summary>
        [Serializable]
        public class TunnelPose
        {
            public bool use;
            public Vector3 position, euler, scale = Vector3.one;
        }
        [Header("Tư thế thân ống theo hướng nhả (đo từ bản chỉnh tay; use tắt = giữ mặc định)")]
        public TunnelPose tunnelPoseRight = new TunnelPose(), tunnelPoseLeft = new TunnelPose(), tunnelPoseUp = new TunnelPose(), tunnelPoseDown = new TunnelPose();

        /// <summary>Dùng cho lấy tư thế thân ống theo hướng nhả</summary>
        public TunnelPose TunnelPoseOf(Vector2Int dir) => dir.x > 0 ? tunnelPoseRight : dir.x < 0 ? tunnelPoseLeft : dir.y < 0 ? tunnelPoseUp : tunnelPoseDown;

        [Tooltip("Lùi ống ra sau (theo ô, +z = xa camera) để tường khay luôn nằm đè lên ống")] [Range(0f, 1.5f)] public float tunnelBackShift = 0.4f;
        [Tooltip("Xê số đếm dọc hướng nhả (theo ô): dương = về phía vành, âm = về phía đáy ống")] [Range(-0.3f, 0.3f)] public float tunnelLabelShift = 0f;
        [Tooltip("Vành miệng ống nhô ra khỏi mép hố bao nhiêu ô (0 = trùng đúng mép hố)")] [Range(-0.3f, 0.3f)] public float tunnelRimOverhang = 0.03f;
        [Tooltip("Độ nghiêng của mesh tunnel quanh trục X (độ). Mesh gốc là ống hở một đầu: −82° thì miệng ống hướng lên màn hình")]
        public float tunnelTiltX = -82f;
        [Tooltip("Xoay quanh trục nhìn (độ) cho miệng ống quay về hướng nhả hộp: lên / xuống / trái / phải. Gốc: 0 / 180 / 90 / −90")]
        public float tunnelAngleUp = 0f, tunnelAngleDown = 180f, tunnelAngleLeft = 90f, tunnelAngleRight = -90f;

        [Header("Hộp đạn – bay lên máng")]
        [Tooltip("Chiều dài ống thạch lúc rời hố (theo bề ngang hộp). Mesh gốc Cube_1 dài 2.5 lần bề ngang, ống bay giãn thêm → video gốc đo được ~3.4–3.5 lần. 0.9 = như bản cũ (ngắn, hình khối)")]
        [Range(0.9f, 4.5f)] public float ammoFlightLength = 3.4f;

        [Header("Hộp đạn – đậu trong máy xay")]
        [Tooltip("Chiều cao thanh đậu = tỉ lệ này × bề ngang lòng máy xay. Lớn hơn = thanh dài hơn, nhìn như xay lâu hơn")]
        public float dockHeightRatio = 2.6f;
        [Tooltip("Đáy thanh đậu nằm dưới tâm 2 lưỡi cắt bao nhiêu lần bề ngang (lớn hơn = hộp chìm sâu hơn)")]
        public float dockBottomRatio = 0.3f;

        [Header("Hộp đạn – dừng giữa chuyến bay khi hết sức chứa")]
        [Tooltip("Khi bấm hộp làm vượt sức chứa, hộp chạy ĐÚNG hoạt ảnh bay lên máng như bình thường rồi DỪNG ở tiến độ này (0 = vừa nén lấy đà, 1 = tới máng). Chọn Play On thì bay tiếp từ chỗ dừng.")]
        [Range(0.05f, 0.9f)] public float hoverHoldProgress = 0.5f;
        [Tooltip("Hộp lún thêm bao nhiêu ô khi cửa kính đóng, để nắp hộp nằm sau cánh cửa")]
        public float doorExtraSink = 1.6f;

        [Header("Khay đạn – hình khối")]
        [Tooltip("Bật: khay (nền tường, mặt khay, hố, nắp) dựng bằng code theo LevelData. Tắt: không dựng – dùng mesh khay gốc HoleAll đặt sẵn trong scene (World/Level/Board - Default/HoleAll)")]
        public bool trayProcedural = true;
        [Tooltip("Bộ khối tường module gốc dùng che ô ngoài level của khay gốc (trống = tấm phẳng tự sinh)")]
        public WallKit wallKit;
        [Tooltip("Vật liệu Unlit/Transparent cho viền khay (texture viền tô màu Theme.trayOutline do code vẽ)")]
        public Material trayOutlineMaterial;
        [Tooltip("Khay gốc: hộp đặt ở độ sâu miệng hố của hàng mình (mặt khay gốc nghiêng → mép dưới miệng hố che phần dưới thân hộp như game gốc). Số này dời thêm (theo ô): âm = nhô ra phía camera, dương = lún vào ống")]
        public float trayBoxDepth = 0f;
        [Tooltip("Viền mặt khay chìa ra ngoài ô (theo ô)")]
        public float trayPanelPad = 0.10f;
        [Tooltip("Bo góc ngoài của mặt khay (theo ô)")]
        public float trayPanelRadius = 0.22f;
        [Tooltip("Cạnh miệng hố so với ô khay (0.92 = chừa gờ mảnh giữa 2 hố như ảnh gốc). Nhỏ đi = hố bé, gờ giữa 2 hố dày lên")]
        [Range(0.5f, 1f)] public float traySlotSize = 0.92f;
        [Tooltip("Bo góc miệng hố (theo ô). 0 = góc vuông")]
        [Range(0f, 0.5f)] public float traySlotRadius = 0.2f;
        [Tooltip("Hạ mép TRÊN của miệng hố xuống (theo ô). 0 = hố vuông vức như thân hộp")]
        [Range(0f, 0.3f)] public float trayHoleTopInset = 0f;
        [Tooltip("Độ sâu mặt khay so với mặt hộp (theo ô) – mặt khay nằm sau lưng hộp jelly")]
        public float trayPanelDepth = 0.35f;
        [Tooltip("Độ sâu đáy giếng (theo ô) – phải sâu hơn cả hộp đã lún + cửa đóng, nếu không sẽ thấy hộp xuyên đáy")]
        public float trayHoleDepth = 3.0f;

        [Header("Hộp đạn – thạch khi nhấn giữ (JellyDeformer)")]
        [Tooltip("Biên độ nhún co giãn dọc thân (0.12 = ±12% chiều dài)")] public float heldStretch = 0.03f;
        [Tooltip("Biên độ đung đưa ngang của đầu nắp (theo bề ngang hộp)")] public float heldSway = 0.03f;
        [Tooltip("Nhịp lắc (Hz) – lực đẩy vào thạch")] public float heldDriveFrequency = 5f;
        [Tooltip("Độ cứng của thạch: tần số dao động riêng (Hz). Cao = cứng, rung nhanh")] public float jellyFrequency = 6f;
        [Tooltip("Độ giảm chấn (0.05 = rung lâu, 0.3 = tắt nhanh). Thấp thì dội lại nhiều")] public float jellyDamping = 0.2f;
        [Tooltip("Hộp lún nhỏ lại NGAY lúc chạm xuống (phản hồi tức thì, bù độ trễ bắn lúc thả). 0.06 = nhỏ đi 6%")] public float pressInScale = 0.06f;
        [Tooltip("Thời gian (giây) hộp lún xuống / bật lại khi chạm / thả")] public float pressInTime = 0.05f;
        [Tooltip("Cú nhún xuống lúc bắt đầu nhấn")] public float jellyPressKick = 0.5f;
        [Tooltip("Cú bật lên lúc thả tay (và lúc bị từ chối)")] public float jellyReleaseKick = 0.9f;
        [Tooltip("Thời gian (giây) lực lắc mạnh dần khi bắt đầu giữ")] public float heldBlendIn = 0.12f;
        [Tooltip("Thời gian (giây) lực lắc tắt khi thả (thạch vẫn dao động tiếp theo độ giảm chấn)")] public float heldBlendOut = 0.15f;

        [Header("Gạch trên bàn")]
        [Tooltip("Dải vát quanh mép khối gộp (chữ L, chữ T…), theo ô")]
        public float blockBevel = 0.14f;
        [Tooltip("Độ dày khối gộp theo chiều sâu (theo ô) – quyết định bề rộng dải mặt trên nhìn thấy")]
        public float blockDepth = 1.1f;
        [Tooltip("Kéo khối gộp (nhiều máu) về phía camera cho mặt trước ngang mặt trước gạch 1 ô. Âm = gần camera hơn, 0 = như cũ (thụt vào). Gạch 1 ô có mặt trước ở z≈-0.40.")]
        public float blockFrontZ = -0.36f;
        [Tooltip("Nâng cả bàn gạch lên (đv thế giới) để hàng gạch đáy không bị gờ sàn của khung che. Sàn vật lý của bi cũng nâng theo (ArenaWalls). 0 = hàng đáy bị che một phần.")]
        public float boardOffsetY = 0.24f;
        [Tooltip("Kéo đáy khối gộp xuống thêm (theo ô) cho bằng mép gạch 1 ô (gạch 1 ô nhô ra do cubeOverlap). 0 = đáy đúng mép ô.")]
        public float blockBottomExtra = 0.12f;
        [Tooltip("Dời đỉnh khối gộp xuống (dương) hoặc lên (âm), theo ô, để mép trên bằng mép gạch 1 ô. 0 = không dời.")]
        public float blockTopTrim = 0.23f;
        [Tooltip("Phóng gạch 1 ô để 4 góc bo tròn không hở nền (prefab gốc ×1.05)")]
        public float cubeOverlap = 1.10f;
        [Tooltip("Bán kính bo 4 góc gạch 1 ô (theo bề rộng ô). Game gốc bo tròn rõ hơn mesh pCube35 rip được. 0 = dùng nguyên mesh rip.")]
        [Range(0f, 0.45f)] public float cubeCornerRadius = 0.2f;
        [Tooltip("Độ bo của mép mặt trên gạch 1 ô (theo bề rộng ô, không vượt bán kính góc)")]
        [Range(0.02f, 0.45f)] public float cubeEdgeRound = 0.12f;
        [Tooltip("Độ dày thân gạch 1 ô (theo bề rộng ô). Mesh rip gốc 0.97; mỏng hơn thì bớt lộ thành gạch do góc nhìn nghiêng 20°")]
        [Range(0.1f, 1.5f)] public float cubeBodyDepth = 0.97f;
        [Tooltip("Bề ngang cột (/wl) so với ô")]
        public float columnWidthRatio = 0.6f;

        [Header("Máy xay – lưỡi cắt & rung")]
        [Tooltip("Tốc độ quay của 2 lưỡi cắt (độ/giây)")]
        public float crusherSpinSpeed = 800f;
        [Tooltip("Biên độ rung mỗi lần nhả bi")]
        public float crusherShakeMagnitude = 0.06f;
        [Tooltip("Tần số rung (Hz)")]
        public float crusherShakeFrequency = 28f;
        [Tooltip("Tốc độ tắt rung – lớn = tắt nhanh")]
        public float crusherShakeDamping = 6f;

        [Header("Nắp hố (sau khi hộp bay khỏi khay)")]
        [Tooltip("Cỡ miệng hố (theo ô) – vành trên cùng của mesh Hole gốc rộng ~0.96 ô")]
        [Range(0.3f, 1f)] public float holeLidSize = 0.96f;
        [Tooltip("Nhân riêng chiều NGANG của nắp (1 = bằng chiều cao)")]
        [Range(0.6f, 1f)] public float holeLidWidthScale = 0.92f;
        [Tooltip("Bo góc miệng hố (theo ô) – vành trên mesh Hole gốc ≈ 0.30 ô")]
        [Range(0f, 0.5f)] public float holeLidRadius = 0.30f;
        [Tooltip("Bề dày dải thạch đọng ở mép trên hố (theo ô)")]
        public float holeLidBand = 0.17f;
        [Tooltip("Thời gian các sợi thạch chảy dài xuống (giây)")]
        public float holeLidDripTime = 0.18f;
        [Tooltip("Chờ bao lâu rồi nắp mới đóng (giây)")]
        public float holeLidDelay = 0.55f;
        [Tooltip("Thời gian nắp đóng (giây)")]
        public float holeLidTime = 0.22f;

        /// <summary>Tư thế một nhóm hộp đạn trong khay</summary>
        [Serializable]
        public class AmmoPose
        {
            [Tooltip("Độ nghiêng quanh trục X: -90 = nắp thẳng mặt camera; xa -90 thì lộ thân dưới nắp nhiều hơn")]
            [Range(-90f, -45f)] public float tiltX = -55f;
            [Tooltip("Hệ số chiều dài cột jelly (1 = nguyên bản ~1.9 ô)")]
            [Range(0.1f, 1.2f)] public float length = 0.6f;
            [Tooltip("Dịch theo bề rộng 1 ô: x phải+, y lên+, z ra xa camera+")]
            public Vector3 offset;
            [Tooltip("Nhân thêm vào cỡ hộp (1 = theo ammoActiveScale/ammoInactiveScale)")]
            [Range(0.5f, 1.3f)] public float scale = 1f;
        }

        /// <summary>Chỉnh trong Inspector (kể cả lúc đang Play) → hộp trong khay dựng lại tư thế ngay</summary>
        void OnValidate() => AmmoBoxTuning.Bump();

        /// <summary>Dùng cho bộ màu và vật liệu theo độ khó</summary>
        [Serializable]
        public class Theme
        {
            public bool useColors;
            public Color backgroundTop = new Color(0.27f, 0.29f, 0.62f);
            public Color backgroundBottom = new Color(0.17f, 0.18f, 0.40f);
            public Material plane;
            public Material conveyorSurface;
            public Material conveyorSide;
            public Material conveyorGround;
            public Material ammoCell;
            public Material ammoWall;
            [Header("Theo ArtConfig gốc (Resources/configs/artconfigs/material configs/original)")]
            [Tooltip("ColorModularWallBoardGround: màu nền sau bàn + tông khung")]
            public Color groundColor = new Color(0.282f, 0.314f, 0.682f);
            [Tooltip("ColorConveyorArrows: màu mũi tên trên máng")]
            public Color arrowColor = new Color(0.341f, 0.388f, 0.494f);
            [Tooltip("Màu tam giác chia bi (đo từ ảnh gốc, gần màu nền)")]
            public Color pinsColor = new Color(0.282f, 0.314f, 0.682f);
            [Tooltip("Hệ số sáng của khung (Wall) so với groundColor")]
            public float wallTint = 1.6f;
            [Tooltip("Hệ số nhân độ sáng thêm cho vỏ máy xay (vỏ đổi tông / bão hòa / sáng theo groundColor của theme so với Normal; Super Hard xám → vỏ xám)")]
            public float crusherBrightness = 1f;
            [Tooltip("Sprite gradient nền (Mask / MaskHard / MaskSuperHard) – màu đã nằm sẵn trong sprite, KHÔNG tô thêm")]
            public Sprite backgroundMask;
            [Tooltip("Độ đặc của lớp gradient nền (1 = như gốc; nhỏ hơn thì nền sáng hơn vì lộ màu camera phía sau)")]
            [Range(0f, 1f)] public float maskAlpha = 1f;
            [Tooltip("Màu nền camera (phía sau lớp gradient). Alpha = 0 → dùng groundColor. Tách riêng groundColor vì groundColor còn quyết định màu khung/máy xay")]
            public Color cameraBackground = new Color(0f, 0f, 0f, 0f);
            public Material trayWall;
            public Material trayPanel;
            public Material trayHole;
            [Tooltip("Màu đường viền sáng quanh vùng ô chơi của khay (đo từ ảnh gốc)")]
            public Color trayOutline = new Color(0.40f, 0.44f, 0.86f);
        }

        /// <summary>Dùng cho lấy theme theo độ khó</summary>
        public Theme ThemeFor(LevelDifficulty d) =>
            d == LevelDifficulty.SuperHard ? superHardTheme : d == LevelDifficulty.Hard ? hardTheme : normalTheme;
    }
}
