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
        [Tooltip("Dời hình hòm gỗ (và số máu) theo trục Y, đơn vị ô lưới; âm = thấp xuống. Chỉ dời hình, ô va chạm vẫn đúng lưới")] public float crateOffsetY = -0.123f;
        [Tooltip("Rèm từng ô (ModularPrimitiveCurtain/CurtainContainer) – mảnh vải Top/Mid/Bot theo hàng xóm")]
        public GameObject curtainModularPrefab;
        [Tooltip("Rèm gộp (CombinedCubicCurtain/Chain Container) – 3 xích treo huy hiệu: CurtainChain (ngắn), 2x, 3x")]
        public GameObject curtainChainPrefab;
        [Tooltip("Tỉ lệ xích + con lắc so với prefab gốc (1 = nguyên bản)")] public float curtainChainScale = 0.85f;
        [Tooltip("Xích + con lắc nhô ra trước mặt vải bao nhiêu ô CSV (tránh dính vải)")] public float curtainChainOffsetZ = 0.45f;
        [Header("Rèm /cur – thời gian & kích thước")]
        [Tooltip("Số giây vải rèm rút lên khi mở")] public float curtainLiftSeconds = 0.35f;
        [Tooltip("Con lắc đứng yên bao lâu sau khi rèm mở rồi mới biến mất (giây)")] public float curtainMedalSeconds = 0.3f;
        [Tooltip("Mảnh vải phóng to để chồng khít, không hở khe (1.15 = hằng số ScaleOverrideOfPrimitives của game gốc)")] public float curtainPieceOverlap = 1.15f;
        [Tooltip("Rèm cao từ bao nhiêu hàng CSV thì dùng xích dài (3x)")] public int curtainLongChainRows = 12;
        [Tooltip("Rèm cao từ bao nhiêu hàng CSV thì dùng xích vừa (2x)")] public int curtainMediumChainRows = 7;
        [Tooltip("Đường kính số trên con lắc (đơn vị ô CSV)")] public float curtainMedalDiameter = 1.25f;
        [Tooltip("Số nổi lên trước mặt con lắc (đơn vị ô CSV)")] public float curtainLabelLift = 0.3f;
        [Tooltip("Mảnh gạch bay lên con lắc: số giây bay")] public float curtainPieceFlightSeconds = 0.45f;
        [Tooltip("Mảnh gạch bay lên con lắc: độ cong quỹ đạo (đơn vị cỡ mảnh)")] public float curtainPieceArc = 2.5f;
        [Header("Bom /bomb – hiệu ứng")]
        [Tooltip("Số giây bom biến mất khi bi chạm")] public float bombVanishSeconds = 0.3f;
        [Tooltip("Bom phồng tối đa bao nhiêu lần trước khi co")] public float bombPopScale = 1.18f;
        [Tooltip("Phần thời gian đầu (0–1) dùng để phồng")] [Range(0.05f, 0.95f)] public float bombPopPortion = 0.3f;
        [Tooltip("Số nổi ra trước mặt bom (tỉ lệ theo bề ngang bom)")] public float bombLabelLift = 0.06f;
        [Header("Tunnel trên bàn /btu – tấm số")]
        [Tooltip("Số giây tunnel biến mất khi hết việc")] public float boardTunnelBreakSeconds = 0.25f;
        [Tooltip("Cỡ tấm số (đơn vị ô CSV)")] public float boardTunnelBadgeScale = 0.9f;
        [Tooltip("Tấm số nhô ra trước mặt tunnel (đơn vị ô CSV)")] public float boardTunnelBadgeDepth = 0.8f;
        [Tooltip("Chữ số nổi lên trước tấm số (đơn vị ô CSV)")] public float boardTunnelLabelLift = 0.12f;
        [Header("Mechanics mới – prefab & vật liệu")]
        [Tooltip("Giỏ /ba (BlockerBasket): ống không hướng trong khay đạn, mở khi một hộp kề bên được lấy đi")] public GameObject basketPrefab;
        [Tooltip("Xoay thân giỏ (prefab gốc dựng khung \"nhìn từ trên\")")] public Vector3 basketEuler = new Vector3(-90f, 0f, 0f);
        [Tooltip("Cỡ giỏ so với ô khay")] public float basketSize = 1f;
        [Tooltip("Dời vị trí giỏ so với tâm ô khay, đơn vị ô khay: X sang phải, Y lên trên, Z ra trước camera (âm = ra trước). Ammo nhỏ trong giỏ đi theo. Đổi lúc Play thấy ngay")] public Vector3 basketOffset = Vector3.zero;
        [Tooltip("Góc mở của nắp giỏ (độ, quanh trục Z của nắp)")] public float basketCoverOpenAngle = 110f;
        [Tooltip("Tông màu nhân vào vật liệu giỏ (Basket.mat gốc để trắng nên lên nhạt/hồng; game gốc đo được cam nâu RGB ≈ 173,100,40). Trắng = giữ nguyên")] public Color basketTint = new Color(1f, 0.97f, 0.55f, 1f);
        [Tooltip("Độ sáng vùng bóng của giỏ (_SColor, gốc 0.46). Giảm = giỏ đậm hơn")] [Range(0f, 1f)] public float basketShadowBrightness = 0.46f;
        [Tooltip("Số giây nắp giỏ mở/đóng")] public float basketCoverSeconds = 0.25f;
        [Tooltip("Cỡ chữ số đếm trên giỏ (nhân với cỡ ô khay)")] public float basketLabelSize = 6f;
        [Tooltip("Số trên giỏ nhô ra trước mặt giỏ (đơn vị ô khay)")] public float basketLabelDepth = 1f;
        [Tooltip("DEBUG: tick để mấy cục ammo nhỏ LUÔN hiện trong giỏ (cả khi giỏ còn đóng), kéo basketMiniOffsets/trainTubeMiniScale/trainTubeHoldTilt chỉnh live; kết hợp với basketHideCover để thấy rõ")] public bool basketDebugShowMinis;
        [Tooltip("DEBUG: tick để ẨN nắp giỏ, nhìn rõ mấy cục ammo nhỏ bên trong (đổi lúc Play thấy ngay)")] public bool basketHideCover;
        [Tooltip("Số giây giỏ co lại biến mất sau khi nhả hộp cuối")] public float basketVanishSeconds = 0.4f;
        [Tooltip("Giỏ trống nằm lại bao lâu (nắp dựng đứng) sau khi mấy ammo đã nhảy ra rồi mới co biến mất (giây)")] public float basketEmptyStaySeconds = 0.8f;
        [Tooltip("Số giây mỗi hộp bay từ giỏ tới ô trống của nó")] public float basketFlightSeconds = 0.45f;
        [Tooltip("Prefab ống TrainEngine gốc (mesh Cube.008 gắn xương) cho cảnh hộp bay ra khỏi giỏ; trống = hộp trượt thẳng")] public GameObject trainEnginePrefab;
        [Tooltip("Bề ngang ống bay so với ô khay")] public float trainTubeWidth = 0.8f;
        [Tooltip("Độ cong quỹ đạo ống bay (đơn vị ô khay)")] public float trainTubeArc = 1.2f;
        [Tooltip("Trễ giữa hai ống bay liên tiếp từ cùng một giỏ (giây)")] public float trainTubeStagger = 0.03f;
        [Tooltip("Cỡ cục ammo nhỏ nằm trong giỏ so với hộp đủ cỡ (video: ~0.45)")] public float trainTubeMiniScale = 0.45f;
        [Tooltip("Độ nghiêng ngẫu nhiên của mỗi cục ammo nhỏ khi nằm trong giỏ (0 = chúi thẳng vào camera; 0.35 ≈ 19°)")] public float trainTubeHoldTilt = 0.35f;
        [Tooltip("Sau khi nắp bắt đầu mở bao lâu thì mấy cục ammo nhỏ hiện lên trong giỏ (giây)")] public float basketRevealDelay = 0.12f;
        [Tooltip("Mấy cục ammo nhỏ nằm trong giỏ bao lâu rồi mới nhảy ra (giây; video ≈ 0.35)")] public float basketHoldSeconds = 0.35f;
        [Tooltip("Vị trí 3 cục ammo nhỏ trong giỏ (đơn vị ô khay, lệch so với tâm giỏ; lấy lần lượt, vòng lại nếu nhiều hơn 3)")] public Vector2[] basketMiniOffsets = { new Vector2(-0.18f, 0.05f), new Vector2(0.18f, 0.05f), new Vector2(0f, -0.15f) };
        [Tooltip("Chỉ báo ammo đôi /dc (BlockerAmmoDoubleColorIndicator): hộp nhỏ màu của hộp thứ hai")] public GameObject doubleColorPrefab;
        [Tooltip("Mũi tên đổi màu /acs (BlockerAmmoColorSwapArrow): biểu tượng xoay trên hộp")] public GameObject swapArrowPrefab;
        [Tooltip("Tunnel trên bàn (ModularPrimitiveBoardTunnel) – mảnh Left/Mid/Right × Front/Back")] public GameObject boardTunnelPrefab;
        [Tooltip("Tunnel trên bàn gộp (CombinedCubicBoardTunnel) – lấy tấm số (Model/3D Text)")] public GameObject boardTunnelBadgePrefab;
        [Tooltip("Lùi tấm tunnel ra sau (đơn vị ô CSV) so với mặt trước của gạch, để gạch kề che viền và cube trồi lên không đè lên tấm")] public float boardTunnelDepthShift = 0.45f;
        [Tooltip("Số giây tối thiểu giữa hai lần tunnel nhả cube trong cùng một cột (nhỏ = nhả liên tục)")] public float boardTunnelPushInterval = 0.02f;
        [Tooltip("Tốc độ cube bị tunnel đẩy lên, tính bằng ô lưới mỗi giây")] public float boardTunnelPushSpeed = 40f;
        [Tooltip("Xoay cụm mảnh tunnel: prefab gốc dựng trong khung \"nhìn từ trên\" (trục lên của prefab = z) nên xoay −90° quanh X")] public Vector3 boardTunnelEuler = new Vector3(-90f, 0f, 0f);
        [Tooltip("Board Qmark: lớp phủ dấu \"?\" cho gạch 1 ô (QMark World.mat, shader Custom/HiddenBlocker)")] public Material qmarkBoardMaterial;
        [Tooltip("Board Qmark: lớp phủ cho khối gộp nhiều ô (QMark World Big Cube.mat)")] public Material qmarkBoardBigMaterial;
        [Tooltip("Màu nền của lớp phủ \"?\" (material gốc để đen; video gốc đo được RGB ≈ 51,66,74)")] public Color qmarkBackground = new Color(0.2f, 0.26f, 0.29f, 1f);
        [Tooltip("Độ bóng của lớp phủ \"?\" (gốc 0.5 làm khối trên sáng hơn khối dưới do phản xạ môi trường; 0 = đồng đều)")] [Range(0f, 1f)] public float qmarkGlossiness = 0f;
        [Tooltip("Thời gian dấu \"?\" tan biến khi lộ màu")] public float qmarkDissolveSeconds = 0.6f;
        [Tooltip("Bom (CombinedCubicBomb) – quả TNT, ngòi, hiệu ứng nổ")] public GameObject bombPrefab;
        [Tooltip("Góc ngả của quả bom về phía camera (độ) cho có chiều sâu 3D; 0 = đứng thẳng")] [Range(0f, 45f)] public float bombTiltDegrees = 20f;
        [Tooltip("Độ sáng vùng bóng của quả bom (màu _SColor của TNT.mat; gốc 0.22 → tối). Tăng cho sáng hơn")] [Range(0f, 1f)] public float bombShadowBrightness = 0.62f;
        [Tooltip("Ngưỡng vùng sáng (_RampThreshold của TNT.mat; gốc 0.686). Giảm = vùng sáng rộng hơn")] [Range(0f, 1f)] public float bombRampThreshold = 0.4f;
        [Tooltip("Bề ngang quả bom so với bề ngang các ô nó chiếm")] public float bombWidthRatio = 0.95f;
        [Tooltip("Sau khi bom nổ (hết giờ) chờ bao nhiêu giây cho hiệu ứng nổ chạy gần hết rồi mới hiện popup thua")] public float bombLoseDelay = 1.4f;
        [Tooltip("Số giây của 1 nhịp đếm ngược (user chốt: giây bình thường = 1)")] public float bombSecondsPerTick = 1f;
        public GameObject ammoPrefab;
        public GameObject tunnelPrefab;
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
        public Material iceMaterial;
        public Material qmarkMaterial;

        [Header("Theme theo độ khó")]
        public Theme normalTheme;
        public Theme hardTheme;
        [Tooltip("Crab Mode: bộ màu/vật liệu riêng (khay cát…). Dùng cho MỌI độ khó")] public Theme crabTheme;
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

        [Header("Crab Mode")]
        [Tooltip("Crab Mode: số ô cát trống bao quanh bàn (cua đi vòng trong vùng này)")] [Min(1)] public int crabSandMargin = 4;
        [Tooltip("Crab Mode: bề ngang tối đa của tranh (đv thế giới); tranh căn giữa theo arena")] public float crabBoardWidth = 8.0f;
        [Tooltip("Crab Mode: chiều cao tối đa của tranh")] public float crabBoardMaxHeight = 11f;
        [Tooltip("Crab Mode: y mép dưới tranh (chừa chỗ cho hàng vỏ sò bên dưới)")] public float crabBoardBottomY = -4.3f;
        [Tooltip("Crab Mode: chừa thêm khoảng trống (đv thế giới) giữa đáy khay ammo và hàng nút booster")] public float crabTrayBottomPad = 0.7f;
        [Tooltip("Crab Mode: thứ tự level chơi thử. Lv n trong game = level gốc crabLevelOrder[n-1]; quá danh sách thì dùng số level như cũ. Trống = không đổi")] public int[] crabLevelOrder = new int[0];
        [Tooltip("Crab Mode: thời gian hộp bay từ khay vào máy xay (giây)")] public float crabBoxFlyDuration = 0.42f;
        [Tooltip("Crab Mode: ống bay vồng lên trên miệng máy bao cao trước khi rơi vào (đv thế giới; bản cũ 1.2)")] public float crabFlightApex = 0.35f;
        [Tooltip("Prefab cua (có component Crab)")] public GameObject crabPrefab;
        [Tooltip("Tốc độ cua chạy (đv thế giới / giây)")] public float crabSpeed = 3.5f;
        [Tooltip("Thời gian cua chui ra khỏi máy (giây)")] public float crabHatchTime = 0.2f;
        [Tooltip("Thời gian cua cạp một khối (giây)")] public float crabChompTime = 0.35f;
        [Tooltip("Thời gian cua biến mất sau khi cạp (giây)")] public float crabPoofTime = 0.25f;
        [Tooltip("Mỗi máy xay nhả bao nhiêu cua / giây")] public float crabSpawnRate = 20f;
        [Tooltip("Tối đa bao nhiêu cua một máy được nhả dồn trong một khung hình")] public float crabSpawnBurst = 3f;
        [Tooltip("Cua mới nhả lệch ngẫu nhiên quanh miệng máy (đv thế giới) cho khỏi chồng lên nhau")] public float crabSpawnJitter = 0.15f;
        [Tooltip("Số cua tối đa chạy cùng lúc")] public int maxCrabs = 40;
        [Tooltip("Cỡ cua (đv thế giới)")] public float crabScale = 0.5f;
        [Tooltip("Độ sâu z của cua (âm = gần camera hơn khối)")] public float crabZ = -1.5f;
        [Tooltip("Cỡ vụn thạch mỗi nhát cạp (theo cỡ ô)")] public float crabNibbleFxSize = 0.8f;
        [Tooltip("Cỡ làn bụi khi cua lặn xuống cát")] public float crabPoofFxSize = 0.55f;
        [Tooltip("Màu bụi cát khi cua lặn")] public Color crabBurrowDustColor = new Color(1f, 0.86f, 0.6f, 1f);
        [Tooltip("Số lần bo góc đường chạy (0 = đi gấp khúc theo ô)")] [Range(0, 3)] public int crabPathSmoothing = 2;
        [Tooltip("Mỗi con cua lệch làn ngẫu nhiên tối đa bao nhiêu ô")] public float crabLaneJitter = 0.3f;

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
        /// <summary>Số level gốc (trong bộ 550) ứng với Lv hiện tại</summary>
        public int SourceLevel(int id) => crabLevelOrder != null && id >= 1 && id <= crabLevelOrder.Length && crabLevelOrder[id - 1] > 0 ? crabLevelOrder[id - 1] : id;

        public Theme ThemeFor(LevelDifficulty d) =>
            crabTheme != null && crabTheme.trayPanel != null ? crabTheme :
            d == LevelDifficulty.SuperHard ? superHardTheme : d == LevelDifficulty.Hard ? hardTheme : normalTheme;
    }
}
