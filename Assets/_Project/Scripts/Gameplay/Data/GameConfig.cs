using System;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tham chiếu asset + thông số gameplay (một chỗ để tinh chỉnh).</summary>
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

        /// <summary>Level lớn nhất của bộ đang chơi.</summary>
        public int MaxLevel => levelSource == LevelSource.Custom ? Mathf.Max(1, customLevels != null ? customLevels.Count : 1) : maxLevel;
        /// <summary>Tên bộ đang chơi (dùng làm khoá lưu tiến độ riêng từng bộ).</summary>
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
        public Rect arena = new Rect(1.07f, -6.35f, 8.46f, 14.0f); // mép trong nhìn thấy của khung (đo lại)
        public float arenaTopY = 7.45f;
        [Tooltip("Điểm thả bi vào bàn: phải nằm DƯỚI mép bịt cửa phun (y = 6.92) và trên nêm tam giác (đỉnh y = 6.30)")]
        public Vector2 dispenserOutlet = new Vector2(5.30f, 6.62f);

        [Header("Bi")]
        public float ballRadius = 0.17f;
        public float ballSpeed = 11f;
        public float ballSpawnSpread = 55f;      // độ (± so với hướng thẳng xuống)
        public float homingStrength = 0.12f;     // bẻ hướng nhẹ về cube cùng màu khi nảy
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
        public float ammoCellMax = 1.70f;       // ảnh gốc: ô khay ≈ 1.7 (7 cột phủ kín bề ngang)
        public float ammoAreaWidth = 11.9f;
        public float ammoAreaTop = -7.9f;
        public float ammoAreaBottom = -16f;     // mép trên thanh booster
        [Tooltip("Thời gian (giây) logo + pháo hoa/confetti chạy sau khi chữ BOUNCE BLAST hiện xong, rồi mới hiện bảng thắng")]
        public float winFireworksTime = 2.4f;
        public float ammoActiveScale = 0.9f;     // Ammunation.scaleOfActive (nhô khỏi hố)
        public float ammoInactiveScale = 0.88f;  // khít miệng hố (SlotSize 0.92, chừa viền mảnh)

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
        [Tooltip("Bề dày dải thạch đọng ở mép trên hố (theo ô)")]
        public float holeLidBand = 0.09f;
        [Tooltip("Thời gian các sợi thạch chảy dài xuống (giây)")]
        public float holeLidDripTime = 0.18f;
        [Tooltip("Chờ bao lâu rồi nắp mới đóng (giây)")]
        public float holeLidDelay = 0.55f;
        [Tooltip("Thời gian nắp đóng (giây)")]
        public float holeLidTime = 0.22f;

        [Header("Board gravity")]
        public float fallSpeed = 9f;

        /// <summary>Tư thế một nhóm hộp đạn trong khay (active / chưa active).</summary>
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

        /// <summary>Chỉnh trong Inspector (kể cả lúc đang Play) → hộp trong khay dựng lại tư thế ngay.</summary>
        void OnValidate() => AmmoBoxTuning.Bump();

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
            public Material ammoCell;   // sàn rãnh của ô khay đạn
            public Material ammoWall;   // khối tường nổi quanh/giữa khay (ô /awl + viền)
            public Color hudTint = Color.white;
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
            public Material trayWall;   // type 8: t4/t6/t3-wall
            public Material trayPanel;  // type 7: t4-bg / t6-bg 2 / t3-bg2
            public Material trayHole;   // type 5: Hole-1/2/3
        }

        public Theme ThemeFor(LevelDifficulty d) =>
            d == LevelDifficulty.SuperHard ? superHardTheme : d == LevelDifficulty.Hard ? hardTheme : normalTheme;
    }
}
