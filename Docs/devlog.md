# Devlog

Ghi ngắn mỗi buổi làm: **làm gì – gặp vấn đề gì – quyết định gì**. Mới nhất ở trên.

## 2026-10-05 — Chép asset UI/font/vật lý từ bản rip

- Phân tích lại `ExportedProject`, truy vết dependency từ các canvas cốt lõi trong `ManagerScene` → chép 385 file (~35 MB) vào `_Ripped/UI`, `_Ripped/Font/TMP`, `_Ripped/PhysicsMaterial`, `_Ripped/AudioMixer` (giữ GUID). Chi tiết: `Docs/reference/ripped-assets-analysis.md`.
- Remap font/material TMP và `Image` từ DLL gốc sang package; shader dummy của UI → `UI/Default`.
- Phát hiện: điều kiện thua gốc là **Capacity** (giới hạn bi trên bàn / deadlock); bi dùng vật liệu friction 0 – bounciness 1; có map đầy đủ sự kiện → clip âm thanh.

## 2026-10-02 — Phase 1: refactor core & tách god class

- Tách `LevelData.cs` (POCO thuần) và `LevelCsvParser.cs` (static parser) độc lập khỏi MonoBehaviour, sửa bug B4 (đọc toàn bộ số hàng đạn thay vì giới hạn 12 hàng) và bug B8 (màu 'p' chuẩn hóa).
- `BounceBlastLevelSpawner.cs` chỉ nhận `LevelData` để Instantiate GameObjects vào scene.
- Tách `AmmoBalancer.cs` giải quyết bug B3 (tính đạn chính xác cho group shared HP).
- Thêm `ColorPalette.cs` và `BoardLayoutConfig.cs` (ScriptableObject) cùng tool tạo config mặc định `ConfigsAssetCreator.cs`.
- Xóa bỏ alias `BounceBlastGameplayManager`, chuyển toàn bộ tham chiếu sang `GameplayManager.Instance`.
- Tách `GameplayManager.cs` thành các module độc lập: `BallPool.cs` (quản lý pool bi), `AmmoGrid.cs` (quản lý mở khóa ô đạn), `BoardState.cs` (quản lý trạng thái bàn & homing target), `ColumnCollapse.cs` (quản lý dồn cột), `InputController.cs` (xử lý touch/mouse).
- Viết tài liệu thiết kế chuẩn `Docs/design/core-loop.md` mô tả luồng gameplay cốt lõi (sửa B10).


## 2026-10-02 — Phase 0: dọn dẹp & sắp xếp lại

- Khởi tạo git (nhánh `main`) + Git LFS cho ảnh/âm thanh/font. Commit `caaa0ac` = bản gốc trước khi dọn, cần gì cứ lấy lại từ đó.
- Code của mình chuyển vào `Assets/_Project/` (Scripts chia `Core/Level/Units`, Editor, Shaders, Scenes).
- `_ImportedBounceBlast` → `_Ripped`. Xóa 1.513 file rip không được asset nào tham chiếu (chủ yếu sprite/texture UI của game gốc). Giữ lại toàn bộ prefab rip, audio, font. Đã kiểm tra: không còn tham chiếu GUID nào bị hỏng.
- `Resources/sheets_levels`: chỉ giữ `original` và `dailychallenge` (bỏ 14 bộ, ~3.000 file). Xóa thư mục `concentrictheme backup old`.
- `SampleScene` → `Gameplay.unity`, thêm vào Build Settings. Scene từ 13,4 MB còn 1,5 MB.
  - **Nguyên nhân:** Spawner sinh level trong Edit Mode (`OnValidate/OnEnable`) và cả level bị lưu vào scene.
  - **Sửa:** container `_SpawnedLevelObjects` dùng `HideFlags.DontSaveInEditor` khi không ở Play Mode.
- Đổi tên tool "Bảng Điều Khiển Tuần 1" → "Level Inspector".
- `design_note/` → `Docs/design/`. Thêm `Docs/reference/csv-format.md`.
- Assets: 361 MB → 134 MB.

## 2026-09-30 — Tuần 2: core gameplay

- CubeUnit, AmmoBoxUnit, BouncingBall, GameplayManager; conveyor 2 máng, homing, dồn cột, crate.

## 2026-09-29 — Khởi tạo

- Tạo project Unity 2022.3.62f3, import asset rip từ AssetRipper.
