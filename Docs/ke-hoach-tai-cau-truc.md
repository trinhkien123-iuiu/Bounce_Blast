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
- Mesh sinh theo level, ống thạch, `FitCamera` (xem `kien-truc-ui-dung.md` mục 7).
- GUID gốc của asset rip (bản đồ shader/UI dựa vào GUID – xem memory `shader-rip-workflow`, `ui-extract-workflow`): mọi di chuyển đều qua Unity để GUID không đổi. Các script trong `Tools/` có đường dẫn cứng tới `Assets/_Ripped/...` sẽ được cập nhật cùng lúc.
