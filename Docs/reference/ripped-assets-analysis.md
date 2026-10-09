# Phân tích bản rip `ExportedProject` & những gì đã chép sang

> Ngày: 2026-10-05 · Nguồn: `D:\toolGame\game\ExportedProject` (AssetRipper, ~10.300 asset)
> Danh sách file chi tiết: [ripped-import-manifest.txt](ripped-import-manifest.txt)

## 1. Bản rip gồm những gì

| Thư mục | Số file | Dùng được? | Ghi chú |
| --- | --- | --- | --- |
| `Game/Scenes/ManagerScene.unity` | 54 MB, ~4.800 GameObject | 🔍 Tham khảo | Chứa **toàn bộ UI** (HUD, Win, Fail, Settings, Home, Shop…) + manager. Script đều là stub nên không chạy được, nhưng mở trong Unity để xem layout/vị trí. |
| `Scripts/Assembly-CSharp` | 1.175 `.cs` | 🔍 Tham khảo | Chỉ có chữ ký (IL2CPP). Tra tên class/field/enum. **Không chép vào project** (lỗi compile, xung đột tên). |
| `Shader/` (36), `Resources/shaders` (11) | | ❌ | Toàn bộ là **dummy shader** (`//DummyShaderTextExporter`) — chỉ còn khối `Properties`. Dùng để biết tên thuộc tính khi tự viết lại. |
| `Sprite/` (1.189) + `Texture2D/` (430) | | ✅ một phần | Sprite là file `.asset` trỏ vào trang atlas `sactx-*.png`. Phần lớn là UI liveops/shop/collection. |
| `Resources/sheets_levels` | 4.135 | ✅ đã có | 16 bộ level; project giữ `original` + `dailychallenge`. |
| `Resources/asyncload*` | | ✅ một phần | Icon booster/feature, PhysicsMaterial2D. Prefab bên trong là tính năng liveops. |
| `Resources/fonts & materials` | 39 | ✅ Fredoka | Font chữ chính của game là **Fredoka Bold** (TMP SDF). |
| `MonoBehaviour/` (138) | | 🔍 | ScriptableObject: collection, offer giả, Spine, TextAnimator… gần như không có số liệu gameplay. |
| `Plugins/` (217) | | ❌ | DLL SDK: Voodoo, AppLovin, Firebase, Adjust, Nakama, Spine, DOTween… |
| Còn lại trong `Resources/` | | ❌ | iap, banners, cities, profileicons, leaderboard, firebase, sdk… — meta/ads, không cần cho clone. |

## 2. Đã chép sang project (385 file, ~35 MB)

Cách chọn: **truy vết dependency** từ các canvas cốt lõi trong `ManagerScene` (HUD, Win, Fail, Out Of Capacity, Play On, Settings, Home, thanh Capacity, Tutorial, Screen Fader, confetti), sau đó loại IAP / profile / Spine. Chép kèm `.meta` → **giữ nguyên GUID**.

| Đích | Nội dung |
| --- | --- |
| `_Ripped/UI/Sprite/` (219) + `_Ripped/UI/Texture2D/` (71) | Sprite UI + các trang atlas chứa chúng |
| `_Ripped/UI/Icons/` (28 cặp png+asset) | Icon **Booster** (Hammer, Magic Hand, Shuffle, Capacity Increase, Centered Shredder) và **Feature** (Ice, Crate, Tunnel, Lock&Key, TNT, Curtain, Rock, QMark, Chain…) — dùng cho popup "mở khóa cơ chế mới" |
| `_Ripped/UI/AnimationClip/`, `AnimatorController/` | `Window.controller` (mở/đóng popup), `OpenSettings`/`CloseSettings`, `LoadingRotate`, `Banner` |
| `_Ripped/UI/Material/` | Glow/star cho particle UI, `UI Mirror X/XY` |
| `_Ripped/Font/TMP/` | `Fredoka-Bold SDF` (+ bản `SDF 1`, `SDF_WORLD` cho chữ 3D) + 10 material (outline, shadow, multi-layer…) |
| `_Ripped/PhysicsMaterial/` | `M-Bounce 2D` (friction 0, bounciness 1), `M-NoBounce Zero Friction 2D`, `M-NoBounce With Friction 2D`, `Conveyor Walls` |
| `_Ripped/AudioMixer/` | `Default Audio Mixer` (nhóm SFX / Music / Voice / Environment) |

**Đã sửa tham chiếu sau khi chép:**
- Font asset: `m_Script` trỏ DLL TMP của game → script `TMP_FontAsset` của package `com.unity.textmeshpro 3.0.7`.
- Material Fredoka: shader dummy → `TextMeshPro/Distance Field` và `TextMeshPro/Mobile/Distance Field` (Essentials trong `Assets/TextMesh Pro/Shaders`). Bản "2 Pass" không có trong TMP 3.0.7 → dùng Mobile thay thế.
- `UI-Effect-Shiny.mat`, `UI_AlwaysOnTopParticle.mat`: shader dummy → `UI/Default` (mất hiệu ứng shiny, không bị hồng).
- `Banner Animation.anim`: tham chiếu `Image` trong `UnityEngine.UI.dll` → script `Image` của package uGUI.
- Đã kiểm tra: mọi GUID trong các file mới đều trỏ tới asset có thật trong project/package.

> ⚠️ Nếu font Fredoka lỗi hiển thị trong TMP 3.0.7: tải **Fredoka** (Google Fonts, giấy phép OFL) rồi tạo lại SDF bằng *Window → TextMeshPro → Font Asset Creator*. Atlas rip vẫn dùng để so sánh.

## 3. Sprite theo từng màn hình (để dựng lại UI ở Phase 4)

| Màn hình | Số sprite | Sprite chính |
| --- | --- | --- |
| HUD trong game (`GameCanvas`) | 14 | `Coin and Level BG_mir`, `Level`, `LevelPart`, `Progress_Bar_Back_mir`, `Progress_Bar_Orange_mir`, `SliderPart_mir`, `Settings`, `Coin`, `Heart-Coin BG_mir` |
| Thanh Capacity | 7 | `CapacityBg`, `CapacityBg Mask`, `CapacityGreen`, `CapacityWhite`, `CapacityThermometer`, `Vignette`, `infinity` |
| Thắng (`SuccessCanvas`) | 49 | `Well Done Text`, `Normal Pop-up (2)_mir`, `NewFeatureBanner_mir`, `New-Feature-Unlocked-…`, `CompletedStarActive`, `BannerMid_mir`, `BanneSide_mir`, podium Normal/Hard/SuperHard |
| Thua (`FailCanvas`) | 22 | Bộ 9-slice `9SliceBeigeMidPart*`, `9sliceTop*`, `9SliceRedFrame`, `BrokenHeart Icon`, `Sad_Face_Emoji`, `Close` |
| Hết capacity | 15 | `AddCapacityButton`, `Give Up Button`, `Activate Button (1)`, `x2 Button`, `BrokenHeart Icon` |
| Play On (hồi sinh) | 33 | `Free Play On`, `RV Button_mir`, `LoseScreenSpeechBubble (1)`, `Capacity Icon shadow`, `Heart Infinity` |
| Settings | 31 | `Settings Home Pop-up`, `Music on/off`, `Sound_Icon`, `Volume on/off`, `Vibration on/off`, `On/Off Button_mir`, `Setting Home Buttons` |
| Home (`MainMenuCanvas`) | 116 | `BG_0`, `BigHome`, `BigShop`, `BigSettings`, `BigArea`, `Play Button`, booster icons, nút shop… |

Mẹo: trong Unity, gõ tên sprite vào ô search của cửa sổ Project (thư mục `_Ripped/UI/Sprite`) để xem preview. Muốn xem layout gốc: mở `ExportedProject` bằng Unity 2022.3 → scene `ManagerScene`.

## 4. Phát hiện về gameplay (cập nhật cho Phase 2–5)

### 4.1 Điều kiện thua = **Capacity**, không phải "hết đạn"
Class gốc `Capacity` có `currentUsedCapacity / currentMaxCapacity`, `ogCapacityPerNode`, `ogNumCapacityNodes`, `extraCapacity`, `IsInDeadlock`, vignette chuyển màu ở 0/50/75/100 %, `isOutOfCapacity`. UI có panel **"End Game – Out Of Capacity"** (nút *Add Capacity* / *Give Up*) và **"Play On"**.
→ Luật: số bi đang nằm trên bàn bị giới hạn; vượt sức chứa (hoặc deadlock: không bi nào còn mục tiêu) thì thua. Hiển thị dạng `{used}/{max}` hoặc `%`. Cột `Capacity` đã có trong CSV level.

### 4.2 Vật lý bi
- Bi dùng `M-Bounce 2D`: **friction 0, bounciness 1** (`BounceCombine = Maximum`). Thành máng dùng `Conveyor Walls`.
- `Ball` có tham số quỹ đạo quay quanh (orbit) khi chờ: `orbitSpringK 20`, `orbitDamping 6`, `orbitPlaneK 10`, `orbitPlaneD 4`, `orbitSpeedJitter 0.15`.

### 4.3 Conveyor (`KPConveyorManager`, prefab `Board - *`)
- Máng chính: `numOfConveyorSlots 15`, `numOfIndicatorArrows 15`, `fadeOutAtEnd` (clip 0.7 → 0.9), `pathCacheSampleCount 64`, khởi tạo 5 slot/frame.
- Máng phụ (2 slot): xoay khi tới cuối (`endRotationAtEnd (0,-45,270)`), thu nhỏ `(0.06,0,0.06)` từ clip 0.9.
- `KPConveyorCrusher`: rung `shakeMagnitude 0.05`, `shakeFrequency 25`, `dampening 5`.
- Hộp đạn có hiệu ứng "thạch" `KPJellyCubeMovement`: `lerpSpeed 5`, `distBetweenJoints 0.2`.

### 4.4 Thứ tự mở khóa cơ chế (`sheets_constants/Features_v3.txt`)
CombinedCube 4 · AmmoQmark 8 · AmmoTunnel 13 · AmmoIce 21 · Crate 31 · AmmoCover 41 · AmmoGlue 61 · Column 71 · Stone 91 · Curtain 111 · Lock 121 · Bomb 151 · AmmoBasket 201 · AmmoChain 251 · BoardTunnel 301 · AmmoColorSwap 351 · BoardQMark 401 · AmmoDoubleColor 451
→ Nên làm Phase 3 **theo đúng thứ tự này** (level nào dùng cơ chế nào đã rõ).

### 4.5 Âm thanh (`KPAudioManager` trong ManagerScene, enum `KPAudioName`)
Tất cả clip đã có trong `_Ripped/AudioClip`.

| Sự kiện | Clip |
| --- | --- |
| ButtonClicked | `button1` |
| Error | `SFX Error` |
| UIAppear | `bubble-pop-293342 (1)` |
| Shoot (xả bi) | `etfx_shoot_bubble2` |
| BallStartMoving | `SFX 230326 - Click` |
| BallRoll | `SFX 260725 ball rolling 1/2/3` (random) |
| BalloonPop | `etfx_pop_balloon` |
| HyperCasual_ClickedOnObject (bấm hộp đạn) | `arabaya tıkaldığımlzda` |
| HyperCasual_ClickedOnObject_v2 | `bubble-pop-293342 (1)` |
| HyperCasual_MissionCompleted / _v2 | `SFX 241025_tsidilin__bubble-pop-out` / `pop-cartoon-328167` |
| CrateSmashed | `SFX 210725 crate breaked` |
| CurtainOpen | `SFX 210725 reload & open curtain` |
| Unlocked (khóa) | `SFX 210725 unlock lock` |
| StoneBreak | `SFX 241025 CPI Stone Break` |
| HitPendant | `SFX 220725 hit_Pendant` |
| Hit_Ice | `etfx_explosion_snow` |
| Hit_Magic | `SFX 120126 hit magic` |
| BombExploded / BombDefused | `SFX 310725 Bomb Big` / `Bomb Small` |
| CutAndFill_BlockCutSound | `SFX 251025 Block cut sound` |
| PinballCollectBall | `freesound_community-whoopwav-14650` |
| Confetti | `SFX 250725 confetti` |
| Coin | `Coin 2` |
| Win / Purchase | `win` / `SFX 290725 win 1` |
| Lose / LoseHeartPanel | `SFX 290725 fail 1` / `SFX 290725 fail 2` |
| Music_MainMenu | `cyberwave-orchestra-puzzle-game-bright-casual…` |
| Music_InGame_Normal / Hard / SuperHard | `geoffharvey-solve-the-riddle` / `denis-pavlov…marimba…` / `denis-pavlov…arcade-technology` |

### 4.6 Class gốc nên tra khi làm UI / flow
`Garawell/Managers/Menu/GamePanel`, `WinPanel`, `FailPanel`, `LoadingPanel`, `PanelManager` · `KPHeartSystemPanel_OutOfCapacityPanel`, `KPHeartSystemPanel_PlayOnPanel` · `Capacity`, `CapacityText` · `SettingsPanel` · `MainMenuPanel`, `MainPanelManager` · `KPTutorialInfoPanel` · `KPHardLevelUIElement` (đổi màu UI theo độ khó Normal/Hard/SuperHard).

## 5. Không chép (và lý do)

- **Scene gốc** — 54 MB, script stub; dùng làm tài liệu tham khảo tại chỗ.
- **Shader** — dummy, không có code. Tự viết lại (đã có `BounceBlast_Toon.shader`). Tên property quan trọng: `ELCAPITAN/IceBlockerWorld` (`_IceBaseColor`, `_GlobalStoneTex`, `_RimStrength`, `_RimPower`), `Garawell_QMark_BIRP_Fixed`, `Custom_HiddenBlocker`.
- **Spine** (GiftBox, WeeklyAdventure, skeleton_*) — cần Spine runtime, chỉ dùng cho liveops.
- **Bộ level khác** (wideboard, openboard, levelfunnel…) — cần thì chép lại `Resources/sheets_levels/<bộ>` (giữ `.meta`).
- **Resources/number** (200 sprite số) — chưa thấy prefab core nào dùng.
- SDK / Ads / IAP / Leaderboard / LiveOps / Collection / Cities.
