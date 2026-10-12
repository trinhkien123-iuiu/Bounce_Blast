using System;
using UnityEngine;

namespace BounceBlast.Gameplay
{
    /// <summary>Tham chiếu asset + thông số gameplay (một chỗ để tinh chỉnh).</summary>
    [CreateAssetMenu(menuName = "Bounce Blast/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Level")]
        public string levelSet = "original";
        public int maxLevel = 550;

        [Header("Prefab gốc (Resources/prefabs)")]
        public GameObject cubePrefab;
        public GameObject combinedCubePrefab;
        public GameObject cratePrefab;
        public GameObject ammoPrefab;
        public GameObject tunnelPrefab;
        public GameObject ballPrefab;
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

        [Header("Bố cục màn hình (camera tự cân theo HUD & hàng booster – xem GameManager.FitCamera)")]
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

        [Header("Hộp đạn – lơ lửng khi hết sức chứa")]
        [Tooltip("Độ ngả của ống thạch so với phương thẳng đứng (độ)")]
        public float hoverAngle = 28f;
        [Tooltip("Chiều dài ống thạch = tỉ lệ này × bề ngang hộp")]
        public float hoverLength = 3f;
        [Tooltip("Thời gian ống bật ra khỏi hố (giây)")]
        public float hoverTime = 0.38f;
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

        [Header("Gạch trên bàn")]
        [Tooltip("Dải vát quanh mép khối gộp (chữ L, chữ T…), theo ô")]
        public float blockBevel = 0.14f;
        [Tooltip("Độ dày khối gộp theo chiều sâu (theo ô) – quyết định bề rộng dải mặt trên nhìn thấy")]
        public float blockDepth = 1.1f;
        [Tooltip("Phóng gạch 1 ô để 4 góc bo tròn không hở nền (prefab gốc ×1.05)")]
        public float cubeOverlap = 1.10f;
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
            [Tooltip("Sprite gradient nền (Mask / MaskHard / MaskSuperHard) – màu đã nằm sẵn trong sprite, KHÔNG tô thêm")]
            public Sprite backgroundMask;
            public Material trayWall;   // type 8: t4/t6/t3-wall
            public Material trayPanel;  // type 7: t4-bg / t6-bg 2 / t3-bg2
            public Material trayHole;   // type 5: Hole-1/2/3
        }

        public Theme ThemeFor(LevelDifficulty d) =>
            d == LevelDifficulty.SuperHard ? superHardTheme : d == LevelDifficulty.Hard ? hardTheme : normalTheme;
    }
}
