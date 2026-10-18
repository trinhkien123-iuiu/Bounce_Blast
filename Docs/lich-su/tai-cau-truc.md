# Kế hoạch tái cấu trúc dự án (đề xuất – CHỜ DUYỆT, chưa di chuyển file nào)

> Mục tiêu của user: sau này **thay asset / chỉnh sửa phải biết tìm ở đâu**, không phải làm lại từ đầu.
> Tham khảo `D:\GitHub\Last-Fisherman` – lấy cái hợp lý, không sao chép nguyên.

---

## 1. Hiện trạng Bounce_Blast – chỗ nào khiến "khó tìm"

| Vị trí | Số file | Vấn đề |
|---|---|---|
| `Assets/_Ripped/` | **1245** | Xếp theo **loại Unity** (Mesh 273, Material 232, UI 394, Sprite 125, Texture2D 116…). Muốn thay hình quả bóng phải đoán mesh/material/texture nằm ở 3 thư mục khác nhau. Lẫn cả asset không dùng (~446 cái đã liệt kê trước đây). |
| `Assets/Resources/` | 422 | `prefabs/` (51 prefab gốc), `configs/` (30), `sheets_levels/` (335 file level). Code gọi bằng **chuỗi** `Resources.Load("prefabs/...")` → đổi tên/đổi chỗ là hỏng âm thầm. |
| `Assets/_Project/` | 88 | Code + cấu hình của mình. Cấu trúc code ổn (`Scripts/Gameplay/{Ammo,Balls,Board,Conveyor,Core,Data,UI}`), nhưng prefab UI nằm lẻ trong `_Project/UI/Prefabs`, prefab gameplay nằm tận `Resources/prefabs`. |
| Mã cứng | 4 chỗ | `Resources.Load` prefab: `AmmoBox.cs:117`, `AmmoGridController.cs:177`, `BoardController.cs:453, 511`. Phần còn lại đã đi qua `GameConfig`/`UiSkin` (tốt). |
| Tài liệu | rải rác | `Docs/` có nhật ký nhưng **chưa có bản đồ "muốn đổi X thì vào đâu"**. |

## 2. Học được gì từ Last-Fisherman

**Lấy:**
- Gốc `Assets/_Project/` tách bạch với asset ngoài (`TextMesh Pro`, plugin) – đã làm giống.
- Dữ liệu tĩnh tách khỏi code: thư mục `Data/` chứa ScriptableObject, **mỗi thực thể một asset**, tra theo ID → "đổi chỉ số cá" = mở đúng 1 asset. (Bounce_Blast tương đương: `GameConfig`, `UiSkin`, `ColorLibrary`, theme.)
- Có sẵn `ARCHITECTURE.md` (bản đồ module + "khi làm X thì sửa file nào") và `CODING_CONVENTION.md` ở gốc → người mới / AI không phải đọc lại toàn bộ.
- Quy ước đặt tên prefab/asset nhất quán (`Bait_…`, `FD_…`, `ID_…`).

**Không lấy (vì chính Last-Fisherman làm chưa tốt):**
- `Prefabs/` để **phẳng** 21 file lẫn đủ loại (`Fish 1/2/3`, `Rod`, `FISHING_ROD`…) → khó tìm đúng cái mình bàn. Bounce_Blast sẽ chia thư mục con theo tính năng.
- Không dùng `.asmdef` → đổi 1 dòng là compile lại toàn bộ. Bounce_Blast nên tách `Gameplay` / `UI` / `Editor`.
- Lạm dụng Singleton + HUD vẽ bằng `OnGUI`. Bounce_Blast đã đi hướng View + prefab nên không cần.

## 3. Cấu trúc đích đề xuất

```
Assets/
├── _Project/                         ← MỌI thứ do ta làm/chốt
│   ├── Scenes/
│   ├── Scripts/
│   │   ├── Gameplay/{Ammo,Balls,Board,Conveyor,Core,Data}
│   │   ├── UI/                        ← View + PopupViewBase (gộp UI/ và Gameplay/UI/ hiện đang tách đôi)
│   │   └── Editor/                    ← công cụ Editor
│   ├── Data/                          ← ScriptableObject: GameConfig, UiSkin, ColorLibrary, Theme*…
│   │   └── Levels/                    ← 335 file level (thay cho Resources/sheets_levels)
│   ├── Prefabs/
│   │   ├── Gameplay/                  ← Ball, Cube, Crate, Ammo…, VFX/ (thay cho Resources/prefabs)
│   │   └── UI/                        ← OverlayCanvas, HUD, 4 popup, BoosterBar, CapacityBar
│   ├── Art/                           ← THEO TÍNH NĂNG, mỗi tính năng gom đủ mesh+material+texture
│   │   ├── Board/  Balls/  Ammo/  Conveyor/  Environment/  VFX/
│   │   ├── UI/{Hud,Booster,Popup,Win,Common}/
│   │   └── Shared/                    ← dùng chung nhiều tính năng
│   ├── Audio/   Fonts/   Shaders/
├── ThirdParty/                        ← TextMesh Pro, MCPForUnity…
└── Resources/                         ← CHỈ còn thứ bắt buộc load theo tên (nếu có); mục tiêu: rỗng
```
Gốc repo: thêm `ARCHITECTURE.md` (bản đồ + "muốn đổi X thì vào đâu"), `CODING_CONVENTION.md`; `Docs/` giữ nhật ký.

## 4. Các giai đoạn (mỗi giai đoạn tự đứng được, kiểm chứng trước khi sang giai đoạn sau)

| # | Việc | Rủi ro | Cách làm an toàn |
|---|---|---|---|
| **0** | **Chốt checkpoint git** | – | Cây làm việc đang có RẤT nhiều thay đổi chưa commit. Phải commit trước khi di chuyển file để quay lại được. |
| **A** | Viết `ARCHITECTURE.md` + `CODING_CONVENTION.md`; thêm README ngắn vào từng thư mục lớn | Không | Chỉ thêm file |
| **B** | Bỏ 4 chỗ `Resources.Load` cứng → ô tham chiếu trong `GameConfig` | Thấp | Kéo thả prefab vào Inspector; chạy thử |
| **C** | Chuyển `Resources/prefabs` → `_Project/Prefabs/Gameplay/` (chia thư mục con) | Thấp–vừa | Dùng `AssetDatabase.MoveAsset` → **GUID giữ nguyên** nên mọi tham chiếu không vỡ; chạy thử 5 level |
| **D** | Chuyển level `sheets_levels` → `_Project/Data/Levels`; `LevelParser` đọc qua tham chiếu thay vì chuỗi `Resources` | Vừa | Kiểm tra nạp được cả 550 level |
| **E** | **Art theo tính năng**: phân tích phụ thuộc (`AssetDatabase.GetDependencies`) từ prefab/scene/SO đang dùng, gán mỗi asset về tính năng sở hữu nó; asset dùng chung → `Shared/` | **Cao nhất** (1245 file) | (1) chạy khô, xuất BÁO CÁO "asset → thư mục đích" cho user duyệt; (2) chỉ sau khi duyệt mới chuyển, theo từng đợt nhỏ; (3) `MoveAsset` giữ GUID; (4) so ảnh chụp trước/sau cho 5 level |
| **F** | Asset không ai tham chiếu (≈446 + 14 shader trùng) | Vừa | Xuất danh sách để **user xoá** (mình bị chặn xoá file) hoặc gom vào thư mục `_Archive/` |
| **G** | Tách `.asmdef`: `BounceBlast.Gameplay`, `BounceBlast.UI`, `BounceBlast.Editor` | Vừa | Làm sau cùng; compile + chạy thử |

## 5. Những điều KHÔNG đổi
- `GameConfig`/`UiSkin` là điểm kéo-thả asset chính (đã đúng hướng).
- Mesh sinh theo level, ống thạch, `FitCamera` (xem `kien-truc/ui.md` mục 7).
- GUID gốc của asset rip (bản đồ shader/UI dựa vào GUID – xem memory `shader-rip-workflow`, `ui-extract-workflow`): mọi di chuyển đều qua Unity để GUID không đổi. Các script trong `Tools/` có đường dẫn cứng tới `Assets/_Ripped/...` sẽ được cập nhật cùng lúc.

---

## 6. Quyết định của user (đã chốt)
1. **Checkpoint git:** user TỰ commit (GitHub Desktop/Fork) rồi báo; chưa có checkpoint thì KHÔNG di chuyển file nào.
2. **Art:** sắp xếp **theo tính năng** (Balls, Board, Ammo, Conveyor, Environment, VFX, UI/…, Shared).
3. **Asset không dùng:** gom vào `Assets/_Archive` (giữ GUID, không xoá).

## 7. Kết quả chạy khô (đã làm, CHỈ ĐỌC – không file nào bị di chuyển)
Công cụ: `Assets/_Project/Editor/ArtReorganizer.cs` → menu `Tools/Bounce Blast/Art/Phân tích (chạy khô – chỉ đọc)`. Báo cáo: `Docs/lich-su/tai-cau-truc-art/bao-cao-tom-tat.md`, danh sách từng file `from → to` ở `Docs/lich-su/tai-cau-truc-art/art_move_plan.csv`.

**1258 asset art đã xét:**
| Trạng thái | Số file | Đích |
|---|---:|---|
| Thuộc đúng 1 tính năng | 418 | `_Project/Art/<Tính năng>/<Loại>` |
| Dùng chung | 58 | `_Project/Art/Shared/<Loại>` (hoặc `UI/Common` nếu chỉ UI dùng chung; bảng màu → `Shared/Colors`; âm thanh → `_Project/Audio`) |
| Không dùng – **của tính năng chưa làm** | 272 | `_Archive/TinhNangChuaLam/…` |
| Không dùng – **mồ côi thật** | 510 | `_Archive/MoCoi/…` |

**Kiểm chứng con số:** duyệt GUID độc lập bằng Python (từ đúng các gốc: scene, GameConfig, UiSkin, ColorLibrary, 4 prefab tải theo chuỗi) cho kết quả **476 dùng / 782 không dùng, khớp tuyệt đối với Unity**, không lệch file nào.

**Lưu ý quan trọng khi đọc con số 782:** "không dùng" ≠ rác.
- 272 file thuộc prefab của các tính năng game gốc mà bản clone **chưa làm** (khoá `AmmunationBlockerLock` 49, nắp lớn `BlockerAmmoMegaCover` 42, đá `BlockerStone` 25, rèm `CombinedCubicCurtain` 13, bom `CombinedCubicBomb` 12, xích `BlockerAmmoChain`, keo `GlueObject`…). Muốn làm các tính năng này sau thì cần chúng → đó là lý do chọn **gom vào `_Archive` thay vì xoá**; cột `note` trong CSV ghi rõ prefab nào còn trỏ tới.
- 510 file mồ côi: phần lớn `_Ripped/UI` (206), `Sprite` (96), `Material` (66), `Texture2D` (53), `AudioClip` (37 – âm thanh gốc chưa gắn vào `GameConfig.sounds`), `Shader` (13 – shader giả đã được thay bằng bản dịch từ GLSL ở `_Project/Shaders`).
- Con số 446 từng nêu trước đây là phép đếm cũ, phạm vi hẹp hơn; số liệu hiện tại thay thế nó.

**Cần user soát trước khi chuyển:** quy tắc gán tính năng cho prefab nằm trong `FeatureForPrefab()` (theo tên prefab). Ví dụ đã sửa: `ModularPrimitiveAmmoWall` thuộc **Ammo** chứ không phải Board. Xem CSV, thấy file nào xếp sai thì báo để chỉnh quy tắc rồi chạy lại.

## 8. Trạng thái các giai đoạn (CUỐI)
| # | Việc | Trạng thái |
|---|---|---|
| 0 | Checkpoint git | **XONG** (commit `adb3575`) |
| A | `ARCHITECTURE.md` + `CODING_CONVENTION.md` + README thư mục | **XONG** |
| B | Bỏ `Resources.Load` cứng → `GameConfig` | **XONG** |
| C | `Resources/prefabs` → `_Project/Prefabs/Gameplay/` | **XONG** |
| D | `sheets_levels` → `_Project/Data/Levels` + `LevelSet` | **XONG** (550/550 level đọc được) |
| E | Art theo tính năng | **XONG** (0 GUID mất, 0 tham chiếu hỏng mới; 5 level so hình trước/sau giống hệt) |
| F | Asset không dùng → `_Archive` | **XONG** (TinhNangChuaLam + MoCoi) |
| G | `.asmdef` (Runtime + Editor) | **XONG** |
| + | Hệ thống GameManager + đèn nung sẵn vào scene; `OverlayView` thay Vignette/Toast/Spawn | **XONG** |

Việc còn lại cho user: commit; xoá thư mục rỗng `Assets/_Ripped`, `Assets/Resources`, `_Project/Configs`, `_Project/UI` (mình bị chặn xoá); tuỳ chọn xoá `AmmoLayoutTuner.cs` (stub rỗng) và `_Archive` khi chắc chắn không cần. `Tools/UnityYaml/ui_extract.py` còn đường dẫn `_Ripped` cũ (công cụ rip một lần).
