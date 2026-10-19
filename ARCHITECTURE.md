# BOUNCE BLAST (bản dựng lại) — BẢN ĐỒ KIẾN TRÚC

> Đọc file này TRƯỚC khi sửa dự án. Mục tiêu: **muốn đổi một thứ thì biết ngay vào đâu**, không phải đoán, không phải làm lại từ đầu.
> Tài liệu chi tiết từng mảng: `Docs/kien-truc/ui.md` (UI), `Docs/lich-su/tai-cau-truc.md` (lịch sử tái cấu trúc),
> `Docs/lich-su/qua-trinh-khoi-phuc-va-core-gameplay.md` (nhật ký kỹ thuật), `CODING_CONVENTION.md` (quy ước code).

- **Engine:** Unity 2022.3.62f3, Built-in RP, Gamma color space
- **Thể loại:** puzzle bắn bi – dựng lại giống game gốc Bounce Blast (asset được tách từ bản gốc, giữ GUID)
- **Dữ liệu level:** 550 level, CSV của game gốc
- **Nguyên tắc dự án:** thứ gì nhìn thấy / chỉnh được thì **nằm sẵn trong Hierarchy hoặc trong asset cấu hình**, không dựng ngầm bằng code

---

## 1. Cấu trúc thư mục

```
Assets/
├── _Project/                         ← MỌI thứ do ta làm / chốt
│   ├── Scenes/MenuGame.unity         ← màn hình chính (index 0 trong Build Settings)
│   ├── Scenes/Gameplay.unity         ← màn chơi (index 1)
│   ├── Scripts/                      ← assembly BounceBlast.Runtime (BounceBlast.Runtime.asmdef)
│   │   ├── Gameplay/{Ammo,Balls,Board,Conveyor,Core,Data,Managers}   ← namespace BounceBlast.Gameplay
│   │   ├── UI/                       ← namespace BounceBlast.UI: GameUI (điều phối) + các View + hiệu ứng UI
│   │   ├── Menu/                     ← namespace BounceBlast.Menu: MenuManager (scene MenuGame)
│   │   └── Debug/                    ← namespace BounceBlast.Cheats: công cụ cheat (chỉ Editor / Development Build / BB_CHEATS)
│   ├── Editor/                       ← công cụ chỉ chạy trong Editor (BounceBlast.Editor.asmdef, namespace BounceBlast.EditorTools)
│   ├── Data/                         ← CẤU HÌNH & DỮ LIỆU (ScriptableObject, level)
│   │   ├── GameConfig.asset          ← ĐIỂM CHỈNH CHÍNH: thông số gameplay, prefab, theme, âm thanh
│   │   ├── UiSkin.asset              ← prefab UI + font + sprite thanh sức chứa
│   │   ├── ColorLibrary.asset        ← bảng màu gạch / hộp / bi
│   │   ├── Levels/LevelSet_original.asset + Levels/original/…   ← 550 level (110 chunk × 5)
│   │   └── Sheets/constants/         ← bảng hằng số gốc (chỉ tham khảo, code không đọc)
│   ├── Prefabs/
│   │   ├── Gameplay/{Ammo,Balls,Board,VFX}
│   │   └── UI/                       ← OverlayCanvas, GameCanvas (HUD), CapacityBar, 4 popup, BoosterBar
│   ├── Art/                          ← THEO TÍNH NĂNG; mỗi nơi gom đủ mesh + material + texture
│   │   ├── Ammo/ Balls/ Board/ Conveyor/ Environment/ VFX/
│   │   ├── UI/{Booster,Capacity,Common,Hud,Menu,Popup,Win}/   ← font Mikado (royal_kingdom_font.ttf) + font TMP ở UI/Common/Fonts
│   │   └── Shared/{Colors,Materials,Physics,Textures}/   ← dùng chung nhiều tính năng
│   ├── Audio/                        ← âm thanh đang dùng
│   └── Shaders/                      ← shader dịch từ GLSL gốc (giữ GUID)
├── _Archive/                         ← KHÔNG dùng nhưng không xoá (xem mục 5)
└── TextMesh Pro/                     ← thư viện ngoài (font mặc định LiberationSans SDF đã đổi nguồn sang Mikado)
```

Gốc repo: `ARCHITECTURE.md` (file này), `CODING_CONVENTION.md`, `README.md`, `Docs/lich-su/roadmap-cu.md`, `Docs/`, `Tools/` (script Python rip asset).
Không đưa lên git (`.gitignore`): `Captures/` (ảnh/video tạm), `Screenshots/` (ảnh chụp kiểm thử – để NGOÀI `Assets`), `__pycache__`, cấu hình máy riêng (`.claude/settings.local.json`, `.mcp.json`), `*.csproj`/`*.sln` (Unity tự sinh).

**Namespace:** Gameplay ← UI ← Menu (UI và Gameplay tham chiếu lẫn nhau qua `using`); Cheats chỉ phụ thuộc Gameplay/UI, không ai phụ thuộc Cheats ngoài `GameManager.CheatLose` (cũng bọc `#if`).
**Bàn chơi (`Scripts/Gameplay/Board/`), mỗi lớp một việc:** `BoardController` (điều phối: dựng màn, rơi, bi trúng/khối vỡ, báo thắng) · `BoardGrid` (ô nào có khối nào) · `BoardTargeting` (bi ngắm khối nào) · `BlockVisualFactory` (dựng hình khối) · `BlockBreakFx` (hiệu ứng vỡ) · `BoardBlock` (một khối: máu, rơi, nảy) · `Kinds/` (luật riêng từng loại khối). Tiện ích prefab dùng chung: `Core/ModelUtil`.
**Khay & hộp đạn (`Scripts/Gameplay/Ammo/`):** `AmmoGridController` (đặt hộp/tunnel, bấm & bắn, truy vấn cho booster) · `AmmoPathRules` (luật thông đường BFS) · `AmmoTrayView` (hình khay, hố, nắp hố) · `AmmoBox` (một hộp trong khay: trạng thái, bấm, lún, băng, hộp "?") · `AmmoBoxFlight` (bay lên máng như ống thạch, dừng khi hết sức chứa, đậu trong máy xay, nhãn số bi; gọi qua `box.Flight`) · `AmmoDoorView` (cửa kính 2 cánh) · `AmmoFlightTube` (mesh ống thạch gắn xương). Dựng dự phòng đèn/camera UI: `Core/SceneRig`. Mũi tên máng: `Conveyor/LaneArrows`.
**Loại khối trên bàn:** mỗi loại (gạch, khối gộp, hòm gỗ, tường, cột nhà) là một lớp trong `Scripts/Gameplay/Board/Kinds/` giữ luật + cách dựng + cách vỡ; code khác chỉ hỏi `BlockKinds.Of(kind)`. Thêm loại mới: `Docs/kien-truc/them-loai-khoi.md`.

---

## 2. MUỐN ĐỔI X → VÀO ĐÂU

| Muốn đổi… | Vào đâu |
|---|---|
| Tốc độ / nảy / độ lệch bi, tốc độ máng, tốc độ quay lưỡi cắt, rung máy xay | `Data/GameConfig.asset` → nhóm **Bi**, **Máng**, **Máy xay** |
| Tư thế hộp đạn trong khay (nghiêng, dài, dịch, cỡ, độ lún) | `Data/GameConfig.asset` → **Hộp đạn – tư thế**. Kéo thanh trượt cả khi đang Play, thoát Play vẫn giữ số |
| Cỡ / bo / độ sâu miệng hố khay đạn, viền khay | `GameConfig` → **Khay đạn – hình khối** (nạp lại level mới thấy) |
| Độ cao thanh đậu trong máy xay, góc/độ dài ống thạch khi hết sức chứa | `GameConfig` → **Hộp đạn – đậu trong máy xay** / **lơ lửng** |
| Dải vát, độ dày khối gộp; độ phóng gạch; bề ngang cột | `GameConfig` → **Gạch trên bàn** |
| Bố cục camera: mép trên khung, độ thu nhỏ tối đa, cỡ ô khay mong muốn | `GameConfig` → **Bố cục màn hình** |
| Thời gian giữ màn logo + pháo hoa khi thắng | `GameConfig.winFireworksTime` |
| Nắp hố (dải thạch, thời gian đóng) | `GameConfig` → **Nắp hố** |
| Prefab gạch / hộp / bi / cột / VFX nổ | Kéo prefab khác vào ô tương ứng ở `GameConfig` (nhóm **Prefab gốc**) |
| Màu gạch / hộp / bi | `Data/ColorLibrary.asset` → material màu nằm ở `Art/Shared/Colors/` |
| Theme theo độ khó (nền, máng, mũi tên, màu khung) | `GameConfig` → Normal / Hard / SuperHard Theme (material ở `Art/Environment/`) |
| Âm thanh, nhạc nền | `GameConfig` → **Âm thanh** (file ở `Audio/`) |
| **Làm level mới (bộ custom)** | Cửa sổ `Tools/Bounce Blast/Level Editor` (Ctrl+Shift+L) – mỗi level một asset `Data/Levels/custom/Level_xxx.asset` (`LevelData`), thứ tự trong `LevelPack_custom`; chơi bộ này: `GameConfig` → **Level Source = Custom**. Hướng dẫn: `Docs/huong-dan/level-editor.md` |
| **Dữ liệu một level** | `Data/Levels/original/sheets_a_b/BoardProps_v3.txt` (a..b = 5 level) → bảng gạch + hộp đạn; `TunnelProps_v3.txt` → ống |
| Thêm bộ level mới | Thả CSV vào `Data/Levels/<bộ>/sheets_a_b/` rồi menu `Tools/Bounce Blast/Data/Build Level Sets` |
| Hình / kích thước / vị trí nút booster, badge Level, nút Settings | Mở prefab ở `Prefabs/UI/` (BoosterBar, GameCanvas…) hoặc chọn object trong `UI/HUD` / `UI/Popups` rồi sửa; sprite ở `Art/UI/` |
| Chữ / bố cục popup Thua, Thắng, Settings, Hết sức chứa | Prefab tương ứng ở `Prefabs/UI/`; để **xem** popup tắt: chọn nó → nút **Xem trước** ở Inspector |
| Hình quả bóng / gạch / khay / máy xay / nền | Thư mục `Art/<tính năng>/` tương ứng |
| Hiệu ứng nổ / vỡ gạch | `Prefabs/Gameplay/VFX/` + ô VFX trong `GameConfig` |
| Đèn chiếu gạch / thạch | Scene: `Camera & Lights/Directional Light 1 & Cubes…`, `…(Cube, view-aligned)`, `…& Jellies` |
| Tường vật lý lòng bàn (bi nảy) | Scene: `World/ArenaWalls` – chọn để thấy Gizmo; sửa điểm EdgeCollider2D hoặc chuột phải → Dựng lại từ GameConfig.arena |
| Nạp level, level bắt đầu (`startLevel`) | `Managers/LevelManager` |
| Luật / hình / cách vỡ của một loại khối (gạch, khối gộp, hòm, tường, cột); thêm loại khối mới | `Scripts/Gameplay/Board/Kinds/` – hướng dẫn `Docs/kien-truc/them-loai-khoi.md` |
| Luật bắn hộp, sức chứa, hết nước đi | `Managers/LaunchManager` |
| Thắng / thua, trạng thái game | `Managers/GameManager` |
| Logic booster | `Managers/BoosterManager` |
| Tự chơi để test (autoPlay) | `Managers/DebugManager` |
| Công cụ cheat khi test trên máy (nhảy level, thắng/thua, tốc độ, FPS) | Prefab `Prefabs/UI/CheatPanel` (đặt sẵn trong Gameplay) + `Scripts/Debug/`; luật cheat ở `CheatRegistry`. Mở: 5 chạm góc trên-trái / 3 ngón / F1. Chỉ có ở Editor, Development Build hoặc khi thêm define `BB_CHEATS`. Thiết kế: `Docs/thiet-ke/cheat-tool.md` |
| Khoá fps, tự hạ độ phân giải trên máy yếu | `Scripts/Gameplay/Core/FrameRateBootstrap.cs`, `AdaptiveResolution.cs` (tự chạy, không cần đặt vào scene) |
| Logic cân camera theo màn hình | `Scripts/Gameplay/Core/CameraFitter.cs` (số chỉnh ở `GameConfig` → Bố cục màn hình) |
| Màn hình chính: hình, bố cục, nút, tab | 6 prefab đặt cạnh nhau dưới `UI/` của MenuGame, mỗi cái một View: `MenuHomeCanvas` [HomeView], `ShopCanvas` [ShopPanelView], `MenuSettingsCanvas` [MenuSettingsView], `MenuBottomBar` [BottomBarView], `MenuToast` [ToastView], `ProfileCanvas` [ProfilePopupView] (đều ở `Prefabs/UI/`); hình ở `Art/UI/{Menu,Shop,Profile,Streak}/`; điều phối ở `Scripts/Menu/MenuManager.cs`; luật Bounce Streak ở `Scripts/Gameplay/Core/BounceStreak.cs` |
| Level / xu đang lưu, chuyển scene | `Scripts/Gameplay/Core/PlayerData.cs`, `SceneFlow.cs` |
| Camera UI (xa bàn chơi ở x −1000) | Scene: `Camera & Lights/UI Camera` (ô `GameManager.uiCamera`) |

---

## 2b. Scene `MenuGame.unity` (màn hình chính)

```
Camera & Lights   Main Camera (ortho, chỉ để vẽ UI)
Managers          MenuManager [MenuManager] (config + 5 View + mảng trang theo tab) · AudioManager
UI                MenuHomeCanvas [HomeView]          trang Home (Play, xu/tim, avatar, thanh Bounce Streak [StreakBarView] lồng bên trong)
                  ShopCanvas [ShopPanelView]         trang Shop (tắt sẵn)
                  MenuSettingsCanvas [MenuSettingsView] trang Settings (tắt sẵn)
                  MenuBottomBar [BottomBarView]      thanh 5 tab (order 5, luôn nằm trên trang)
                  ProfileCanvas [ProfilePopupView]   popup Profile – đổi tên, chọn avatar (order 30, tắt sẵn)
                  MenuToast [ToastView]              dòng thông báo (order 50)
                  EventSystem
```
- Gồm: nút **Play** (3 kiểu Normal/Hard/Super Hard theo độ khó level kế tiếp), xu, tim, thanh 5 tab (Home, Shop, Settings chạy; **Journey / Collection chưa làm** → bấm hiện "coming soon"); bấm tab thì `MenuManager` bật đúng một trang, tắt các trang khác, trang Settings (âm thanh / nhạc / rung).
- Luồng: `MenuGame` ──Play──▶ `Gameplay` ──Settings ▸ Home──▶ `MenuGame` (`SceneFlow`). Tiến độ (level, xu) ở `PlayerData` (PlayerPrefs). Vào Gameplay từ Menu thì chơi tiếp level đã lưu; bấm Play thẳng scene Gameplay thì dùng `LevelManager.startLevel`.
- Đổi hình menu → `Art/UI/Menu/`; đổi nhạc menu → `GameConfig.musicMenu`; đổi bố cục → mở prefab của trang tương ứng (`MenuHomeCanvas`, `ShopCanvas`…). Mỗi Canvas gốc là Screen Space - Camera, khoảng cách 1; ô camera nối trong scene (override cố ý).

## 3. Scene `Gameplay.unity` — Hierarchy

```
Camera & Lights   Main Camera, 3 đèn Directional, UI Camera
World             những thứ NHÌN THẤY / chạm được trong bàn chơi
 ├─ Level/Board - Default   khung bàn 3D gốc (Conveyor_Container, Environment Base, Gradient_Back, Crushers)
 ├─ Board  AmmoGrid  Conveyor  Balls  ArenaWalls  VFX
Managers          LOGIC điều phối (mỗi cái một việc, xem mục 4b)
 ├─ GameManager    trạng thái + thắng/thua + nối các manager (composition root)
 ├─ LevelManager   nạp level          ├─ InputManager    bấm / chạm
 ├─ LaunchManager  bắn hộp + sức chứa  ├─ BoosterManager  booster
 ├─ DebugManager   autoPlay            ├─ AudioManager    âm thanh
 └─ CameraFitter   cân camera          └─ Theme           áp theme theo độ khó
CheatPanel        [CheatHost + CheatPanelView + CheatPerfOverlay] – tự huỷ ở bản phát hành
UI                [GameUI] điều phối, kéo sẵn 7 View
 ├─ HUD       OverlayCanvas [OverlayView] · HUD (GameCanvas) [HudView] · CapacityBar [CapacityBarView]
 ├─ Popups    FailPopup · SuccessPopup · Settings · OutOfCapacityPopup · BoosterBuyPopup   (tắt sẵn)
 └─ EventSystem
```

**Quy tắc UI:** mỗi panel = 1 prefab đặt sẵn + 1 script `XxxView` giữ tham chiếu bằng Inspector; `GameUI` chỉ ra lệnh. Chi tiết, khuôn mẫu, các bẫy đã dính: `Docs/kien-truc/ui.md`.

---

## 4. Luồng chạy chính

1. `GameManager.Awake`: đọc `GameConfig`, lấy các hệ thống + manager đặt sẵn (thiếu thì tự tạo), làm phẳng khung bàn 3D (`ViewSpace.FlattenStaticScene`), gọi `Init(...)` cho từng manager và nối sự kiện.
2. `LevelManager.Start` đợi 1 khung hình (để Canvas có kích thước thật) rồi `LoadLevel` → `LevelParser.Load` → `BoardController.Build` → `AmmoGridController.Build` → `CameraFitter.Fit` → `ThemeApplier.Apply` → phát `OnLevelLoaded`.
3. `OnLevelLoaded`: `GameManager` chuyển `Playing`; `LaunchManager` đặt lại sức chứa; `BoosterManager` nạp lại booster.
4. `InputManager` bắt chạm → `LaunchManager.TryLaunch` (hoặc `BoosterManager.UseHammer`) → máng → máy xay nhả bi → bi nảy phá gạch.
5. Bàn sạch → `GameManager` Won → `GameUI.PlayWinSequence` → `LevelManager.LoadNext`. `LaunchManager.OnGameOver` (hết sức chứa / hết nước đi) → `GameManager` Lost → `FailPopupView`.

**Thứ cố ý vẫn dựng bằng code (có lý do):** mesh khối gộp (chữ L/T…), mặt khay + miệng hố, ống thạch gắn xương, gạch/hộp/bi theo level, ảnh chớp sáng tạm lúc thắng. Hình dạng của chúng phụ thuộc dữ liệu level hoặc màn hình nên không đặt sẵn được; thông số hình khối đều đã ra `GameConfig`.


## 4b. Các manager (scene Gameplay)

Trước đây một `GameManager` ôm hết (~460 dòng); nay **một manager = một việc**, mỗi cái là một object dưới `Managers`.

### 4b.1 Ai làm gì

| Manager | Việc | Không làm |
|---|---|---|
| `GameManager` | Dựng + nối các manager (`Awake`), giữ **trạng thái** `Loading → Playing → Won / Lost`, quyết định thắng / thua, thưởng xu, lưu level | Không đọc chuột, không bắn hộp, không dựng bàn |
| `LevelManager` | `LoadLevel / Restart / LoadNext`, dựng bàn + khay, cân camera, theme + nhạc, báo UI; ô `startLevel`, `continueFromSave` | Không biết luật thắng thua |
| `InputManager` | Đọc chuột / touch → "bấm hộp" hoặc "bấm khối (búa)" → ra lệnh cho Launch / Booster | Không chứa luật |
| `LaunchManager` | Bắn hộp, **sức chứa** (`Capacity`), Out Of Capacity, phát hiện hết nước đi | Không quyết định thua (chỉ báo) |
| `BoosterManager` | Bấm booster, booster lên cò, búa, đẩy số lượng lên UI | |
| `DebugManager` | `autoPlay` (tự chơi để test) – tắt khi build; công cụ cheat trên máy là prefab `CheatPanel` riêng | |
| `AudioManager` | Âm thanh / nhạc (đặt sẵn trong scene, dữ liệu clip ở `GameConfig`) | |
| `CameraFitter`, `ThemeApplier`, `GameUI` | Camera / theme / giao diện | |

### 4b.2 Giao tiếp: lệnh trực tiếp + sự kiện (Observer)

- **Lệnh** (biết rõ ai làm): tham chiếu truyền qua `Init(...)` lúc `GameManager.Awake` – ví dụ `InputManager` gọi `launch.TryLaunch(box)`. Không dùng `FindObjectOfType` hay Singleton để các manager tìm nhau.
- **Thông báo** (ai quan tâm thì nghe): C# `event`.

| Sự kiện | Phát bởi | Ai nghe |
|---|---|---|
| `OnLevelLoading` | LevelManager | GameManager (dừng coroutine, state Loading), LaunchManager (huỷ chờ) |
| `OnLevelLoaded(level)` | LevelManager | GameManager (state Playing), LaunchManager (reset sức chứa), BoosterManager (reset booster) |
| `OnGameOver(lý do)` | LaunchManager | GameManager (thua) |
| `OnBoardCleared` | BoardController | GameManager (thắng) |
| `OnStateChanged / OnWin / OnLose` | GameManager | (sẵn để mở rộng: thống kê, quảng cáo…) |
| `OnBoosterPressed` | GameUI | BoosterManager |

`GameManager` báo trạng thái xuống các manager bằng `SetPlaying(bool)` – manager chỉ xử lý input / deadlock / autoPlay khi đang `Playing`.

### 4b.3 Thêm một manager mới
1. Tạo `Xxx : MonoBehaviour` trong `Scripts/Gameplay/Managers/`, có `Init(...)` nhận phụ thuộc, không `Find`.
2. Thêm ô `public Xxx xxxManager;` ở `GameManager`, tạo object dưới `Managers`, kéo vào ô; gọi `Init` trong `GameManager.Awake`.
3. Nếu cần biết trạng thái: thêm `SetPlaying`, gọi trong `GameManager.SetState`.
4. Ghi vào bảng ở mục 4b.1.

---

## 5. `_Archive` — không phải rác

`Assets/_Archive/` chứa 820+ asset game hiện **không dùng**, chuyển vào chứ không xoá (GUID giữ nguyên, kéo ngược ra là dùng được):
- `TinhNangChuaLam/` — asset của tính năng game gốc mà bản dựng lại **chưa làm**: khoá (`AmmunationBlockerLock`), nắp lớn (`BlockerAmmoMegaCover`), đá (`BlockerStone`), rèm, bom, xích, keo… cùng prefab và cấu hình theme gốc (blue/concentric/wideboard). Khi làm tính năng nào thì lấy từ đây. Danh sách "prefab nào kéo theo asset nào": `Docs/tham-khao/asset-chua-dung.csv`.
- `MoCoi/` — không prefab nào trỏ tới (phần lớn sprite UI gốc, âm thanh chưa gắn, shader giả).
Muốn dọn: xoá cả thư mục `_Archive` trong Unity khi chắc chắn không cần thêm tính năng nào của game gốc.

---

## 6. Công cụ trong menu `Tools/Bounce Blast/`

| Menu | Việc |
|---|---|
| `Data/Build Level Sets` | Dựng lại `LevelSet_*.asset` từ `Data/Levels/` (khi thêm / đổi level) |
| Inspector của mọi popup: **Xem trước / Tắt xem trước** | Bật popup tắt sẵn để xem và sửa trong Editor; lưu scene sẽ tự tắt (`PopupSaveGuard`) |
| `Cheat/Build panel prefab`, `Cheat/Place panel in open scene` | Dựng lại prefab CheatPanel / đặt vào scene đang mở |
| `UI/Thêm phản hồi cho mọi nút` | Gắn `UiButtonFeedback` (lún/nảy) cho mọi nút trong prefab UI |
| (đã tắt) `Art/…`, `Setup Gameplay Scene` | Công cụ dùng một lần, đã chạy xong — xem đầu file nguồn để bật lại |

Công cụ Python rip asset ở `Tools/` (`ShaderRipper`, `UnityYaml`) trỏ tới đường dẫn **cũ** (`Assets/_Ripped/...`); chỉ dùng được khi rip lại từ đầu, không dùng để sửa dự án hiện tại.

---

## 7. Quy tắc khi làm việc

1. **Thay asset:** tìm theo `Art/<tính năng>/`; thay file **giữ nguyên .meta/GUID** (ghi đè file, đừng xoá-tạo-lại) thì mọi tham chiếu tự giữ.
2. **Thêm prefab/asset mới:** đặt đúng thư mục theo bảng mục 1, kéo vào ô của `GameConfig`/`UiSkin`. **Không** dùng `Resources.Load` bằng chuỗi (`Resources/` đã được dọn sạch, giữ như vậy).
3. **Di chuyển file:** luôn di chuyển **trong Unity** (kéo trong Project window hoặc `AssetDatabase.MoveAsset`) để GUID không đổi.
4. **Số tinh chỉnh:** đưa ra `GameConfig` hoặc `[SerializeField]`, đừng `const` giấu trong code. Code không được ghi đè giá trị đã chỉnh trong Inspector/scene.
5. **UI mới:** theo khuôn View (mục 3 + `Docs/kien-truc/ui.md` mục 3, 4).
6. **Sau mỗi thay đổi lớn:** compile sạch → chạy thử vài level (2, 41 có cửa, 44 Hard, 71 có cột, 79 SuperHard) → ghi `Docs/`.
