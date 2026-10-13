# Báo cáo chạy khô – Art theo tính năng

> Sinh tự động bởi `Tools/Bounce Blast/Art/Phân tích (chạy khô – chỉ đọc)`. **Chưa di chuyển file nào.**
> Danh sách đầy đủ từng file: `art_move_plan.csv` (cột `from` → `to`).

Tổng asset art đã xét: **1260**

## Theo trạng thái
| Trạng thái | Số file | Ý nghĩa |
|---|---:|---|
| Owned | 420 | Thuộc đúng 1 tính năng → `Art/<Tính năng>/<Loại>` |
| Shared | 58 | Dùng bởi ≥ 2 tính năng → `Art/Shared/<Loại>` |
| UsedNoOwner | 0 | Game dùng nhưng không gốc nào của tính năng chạm tới (vd chỉ qua config) → `Art/Shared/<Loại>` |
| UnusedBacklog | 272 | Chưa dùng nhưng prefab của TÍNH NĂNG CHƯA LÀM còn trỏ tới (bom, khoá, xích, keo…) → `_Archive/TinhNangChuaLam` |
| UnusedOrphan | 510 | Mồ côi thật sự, không prefab/asset nào trỏ tới → `_Archive/MoCoi` |

## Theo tính năng (chỉ asset đang dùng)
| Tính năng | Tổng | Animation | Audio | Fonts | Materials | Meshes | Physics | Shaders | Sprites | Textures |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Ammo | 94 | 5 | 0 | 0 | 23 | 60 | 0 | 0 | 1 | 5 |
| Audio | 19 | 0 | 19 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| Balls | 4 | 0 | 0 | 0 | 2 | 2 | 0 | 0 | 0 | 0 |
| Board | 38 | 0 | 0 | 1 | 6 | 30 | 0 | 0 | 0 | 1 |
| Conveyor | 31 | 0 | 0 | 0 | 5 | 18 | 0 | 0 | 3 | 5 |
| Environment | 37 | 0 | 0 | 0 | 24 | 2 | 0 | 0 | 3 | 8 |
| Shared | 24 | 0 | 0 | 0 | 14 | 0 | 1 | 0 | 0 | 9 |
| Shared/Colors | 30 | 0 | 0 | 0 | 30 | 0 | 0 | 0 | 0 | 0 |
| UI/Booster | 8 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | 3 |
| UI/Capacity | 12 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 6 | 6 |
| UI/Common | 34 | 0 | 0 | 2 | 10 | 0 | 0 | 0 | 10 | 12 |
| UI/Hud | 13 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 12 | 1 |
| UI/Popup | 68 | 3 | 0 | 0 | 3 | 0 | 0 | 0 | 43 | 19 |
| UI/Win | 55 | 2 | 0 | 0 | 3 | 0 | 0 | 0 | 40 | 10 |
| VFX | 11 | 0 | 0 | 0 | 6 | 0 | 0 | 0 | 0 | 5 |

## Asset không dùng, theo thư mục nguồn
| Thư mục | Số file |
|---|---:|
| `Assets/_Ripped/UI` | 237 |
| `Assets/_Ripped/Mesh` | 161 |
| `Assets/_Ripped/Material` | 126 |
| `Assets/_Ripped/Sprite` | 104 |
| `Assets/_Ripped/Texture2D` | 75 |
| `Assets/_Ripped/AudioClip` | 37 |
| `Assets/_Ripped/Shader` | 13 |
| `Assets/Resources/configs` | 10 |
| `Assets/_Ripped/Font` | 7 |
| `Assets/_Ripped/AnimationClip` | 4 |
| `Assets/_Ripped/PhysicsMaterial` | 3 |
| `Assets/_Ripped/AnimatorController` | 2 |
| `Assets/Resources/materials` | 2 |
| `Assets/_Ripped/AudioMixer` | 1 |

## Gốc đã dùng để phân loại
| Tính năng | Gốc | Số phụ thuộc |
|---|---|---:|
| Ammo | `Assets/Resources/prefabs/Ammunation.prefab` | 59 |
| Ammo | `Assets/Resources/prefabs/AmmunationBlockerCover.prefab` | 63 |
| Ammo | `Assets/Resources/prefabs/BlockerAmmoTunnel.prefab` | 24 |
| Ammo | `Assets/Resources/prefabs/ModularPrimitiveAmmoWall - Wallbase.prefab` | 47 |
| Ammo | `GameConfig.ammoCellMaterial` | 2 |
| Ammo | `GameConfig.ammoPrefab` | 59 |
| Ammo | `GameConfig.tunnelPrefab` | 24 |
| Audio | `GameConfig.musicHard` | 1 |
| Audio | `GameConfig.musicNormal` | 1 |
| Audio | `GameConfig.musicSuperHard` | 1 |
| Audio | `GameConfig.sounds` | 16 |
| Balls | `Assets/Resources/prefabs/Ball.prefab` | 8 |
| Balls | `GameConfig.ballPrefab` | 8 |
| Balls | `GameConfig.trailMaterial` | 1 |
| Board | `Assets/Resources/prefabs/CombinedCubicColumn.prefab` | 7 |
| Board | `Assets/Resources/prefabs/CombinedCubicCrate.prefab` | 11 |
| Board | `Assets/Resources/prefabs/CombinedCubicCubes.prefab` | 5 |
| Board | `Assets/Resources/prefabs/Cube.prefab` | 8 |
| Board | `Assets/Resources/prefabs/ModularPrimitiveColumn.prefab` | 30 |
| Board | `GameConfig.combinedCubePrefab` | 5 |
| Board | `GameConfig.crateMaterial` | 2 |
| Board | `GameConfig.cratePrefab` | 11 |
| Board | `GameConfig.cubePrefab` | 8 |
| Board | `GameConfig.iceMaterial` | 3 |
| Board | `GameConfig.qmarkMaterial` | 3 |
| Board | `GameConfig.wallMaterial` | 2 |
| Board | `GameConfig.worldFont` | 6 |
| Conveyor | `scene:Level/Board - Default/Conveyor_Container` | 12 |
| Conveyor | `scene:Level/Board - Default/Crushers` | 31 |
| Environment | `GameConfig.hardTheme` | 13 |
| Environment | `GameConfig.normalTheme` | 15 |
| Environment | `GameConfig.superHardTheme` | 13 |
| Environment | `scene:Level/Board - Default/Environment Base (1)` | 22 |
| Environment | `scene:Level/Board - Default/Gradient_Back` | 3 |
| Shared/Colors | `ColorLibrary` | 38 |
| UI/Booster | `Assets/_Project/UI/Prefabs/BoosterBar.prefab` | 25 |
| UI/Booster | `Assets/_Project/UI/Prefabs/OverlayCanvas.prefab` | 30 |
| UI/Capacity | `Assets/_Project/UI/Prefabs/CapacityBar.prefab` | 28 |
| UI/Capacity | `UiSkin.capacityBarPrefab` | 28 |
| UI/Capacity | `UiSkin.capacityBg` | 2 |
| UI/Capacity | `UiSkin.capacityFill` | 2 |
| UI/Common | `UiSkin.font` | 6 |
| UI/Common | `UiSkin.fontOutlineMaterial` | 3 |
| UI/Hud | `Assets/_Project/UI/Prefabs/GameCanvas.prefab` | 52 |
| UI/Popup | `Assets/_Project/UI/Prefabs/FailCanvas.prefab` | 68 |
| UI/Popup | `Assets/_Project/UI/Prefabs/OutOfCapacityPanel.prefab` | 52 |
| UI/Popup | `Assets/_Project/UI/Prefabs/OutOfCapacityPopup.prefab` | 56 |
| UI/Popup | `Assets/_Project/UI/Prefabs/SettingsCanvas.prefab` | 82 |
| UI/Popup | `UiSkin.addCapacityButton` | 2 |
| UI/Popup | `UiSkin.brokenHeart` | 2 |
| UI/Popup | `UiSkin.closeButton` | 2 |
| UI/Popup | `UiSkin.greenButton` | 2 |
| UI/Popup | `UiSkin.popupHard` | 2 |
| UI/Popup | `UiSkin.popupNormal` | 2 |
| UI/Popup | `UiSkin.popupSuperHard` | 2 |
| UI/Popup | `UiSkin.redButton` | 2 |
| UI/Popup | `UiSkin.sadFace` | 2 |
| UI/Win | `Assets/_Project/UI/Prefabs/SuccessCanvas.prefab` | 101 |
| VFX | `Assets/Resources/prefabs/Block Break Particle Variant.prefab` | 3 |
| VFX | `Assets/Resources/prefabs/MetalExplosionBig.prefab` | 9 |
| VFX | `Assets/Resources/prefabs/RespawnExplosion.prefab` | 8 |
| VFX | `Assets/Resources/prefabs/StarExplosion.prefab` | 8 |
| VFX | `GameConfig.ammoLaunchVfx` | 8 |
| VFX | `GameConfig.confettiVfx` | 8 |
| VFX | `GameConfig.crateBreakVfx` | 9 |
| VFX | `GameConfig.cubeBreakVfx` | 3 |
