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

        [Header("Máng (conveyor)")]
        public float laneSpeed = 5.5f;
        public float laneBallSpacing = 0.27f;
        public float boxFlyDuration = 0.6f;

        [Header("Khay đạn")]
        public float ammoCellMax = 1.70f;       // ảnh gốc: ô khay ≈ 1.7 (7 cột phủ kín bề ngang)
        public float ammoAreaWidth = 11.9f;
        public float ammoAreaTop = -7.9f;
        public float ammoAreaBottom = -16f;     // mép trên thanh booster
        [Tooltip("Thời gian (giây) logo + pháo hoa/confetti chạy sau khi chữ BOUNCE BLAST hiện xong, rồi mới hiện bảng thắng")]
        public float winFireworksTime = 2.4f;
        public float ammoActiveScale = 0.9f;     // Ammunation.scaleOfActive (nhô khỏi hố)
        public float ammoInactiveScale = 0.88f;  // khít miệng hố (SlotSize 0.92, chừa viền mảnh)

        [Header("Board gravity")]
        public float fallSpeed = 9f;

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
