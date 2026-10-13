# ROADMAP — Bounce Blast Clone

> Cách dùng: làm **từ trên xuống**, xong task nào tick `[x]` task đó. Không nhảy Phase khi Phase trước còn task ⭐ (bắt buộc).
> Cập nhật lần cuối: 2026-10-02

## Tổng quan tiến độ

| Phase | Nội dung | Trạng thái |
| --- | --- | --- |
| 0 | Dọn nền móng (git, thư mục, scene, docs) | 🟨 Gần xong (còn push GitHub + test Play) |
| 1 | Refactor code core + sửa bug đã biết | ⬜ |
| 2 | Hoàn thiện core loop (thua, capacity, tunnel) | ⬜ |
| 3 | Vật cản & cơ chế đặc biệt | ⬜ |
| 4 | Game flow & UI thật | ⬜ |
| 5 | Juice: VFX, âm thanh, animation | ⬜ |
| 6 | Build Android & tối ưu | ⬜ |
| 7 | (Tùy chọn) Meta: booster, coin, daily | ⬜ |

**Đã có (trước roadmap):** đọc CSV level → sinh cube/đạn/crate/blocker (hình ảnh); bấm hộp đạn + mở khóa dấu `+`; bi chạy conveyor → nảy 2D + homing; trừ HP cùng màu; dồn cột; crate; thắng + qua level (OnGUI); tool editor sửa material/missing script; shader toon.

---

## Phase 0 — Dọn nền móng ✅ (2026-10-02)

Mục tiêu: có lịch sử thay đổi để quay lại được, thư mục rõ ràng giữa "code mình" và "asset rip".

- [x] ⭐ `git init` (nhánh `main`) + commit snapshot bản gốc `caaa0ac`
- [x] ⭐ Git LFS cho `*.png *.jpg *.tga *.psd *.fbx *.ogg *.wav *.mp3 *.ttf *.otf *.pdf`
- [ ] ⭐ Tạo repo **private** trên GitHub và push (`git remote add origin ...` → `git push -u origin main`)
- [x] ⭐ Tạo `Assets/_Project/`: `Scripts/{Core,Level,Units}`, `Editor/`, `Shaders/`, `Scenes/`
- [x] Đổi tên `_ImportedBounceBlast/` → `_Ripped/`, xóa 1.513 file rip không được tham chiếu (361 MB → 134 MB, đã kiểm tra không hỏng GUID)
- [x] Xóa `concentrictheme backup old/` trong Resources
- [x] Bỏ 14 bộ level không dùng, giữ `original` + `dailychallenge`
- [x] Scene `SampleScene` → `Gameplay.unity` (13,4 MB → 1,5 MB), thêm vào Build Settings; Spawner không lưu object Edit Mode vào scene nữa (`HideFlags.DontSaveInEditor`)
- [x] `design_note/` → `Docs/design/` (đánh dấu tài liệu lưu trữ), tạo `Docs/devlog.md`, `Docs/reference/csv-format.md`
- [x] Đổi tên tool "Bảng Điều Khiển Tuần 1" → "Level Inspector"
- [ ] ⭐ Mở Unity, Play `Gameplay.unity` kiểm tra level 1 và 21 chơi bình thường (batchmode đã biên dịch OK + nạp được level 21, chưa test bấm chơi)

**Xong khi:** clone repo về máy khác, mở Unity, Play `Gameplay.unity` chạy được level 1.

---

## Phase 1 — Refactor core + sửa bug (3–5 ngày)

Mục tiêu: tách "god class" để thêm cơ chế mới không phải sửa file 960 dòng.

### 1.1 Tách code
- [x] ⭐ `LevelData` (POCO thuần) + `LevelCsvParser` (static, không phụ thuộc MonoBehaviour) — tách từ `BounceBlastLevelSpawner.ParseAndSpawnFromCsv`
- [x] ⭐ `LevelSpawner` chỉ nhận `LevelData` và Instantiate
- [x] `AmmoBalancer` — tách logic tự cân bằng đạn
- [x] `ColorPalette` (ScriptableObject) thay cho `ColorMap` hard-code; cân nhắc đọc từ `sheets_constants/Colors_v3.txt`
- [x] `BoardLayoutConfig` (ScriptableObject): `minX/maxX/minY/maxY`, `ammoTopY`, waypoint 2 máng, vị trí tường/nêm — hiện đang là số "magic" rải trong `GameplayManager`
- [x] Tách `GameplayManager` → `InputController` (click hộp đạn), `BallPool`, `AmmoGrid` (mở khóa `+`), `BoardState` (đếm cube, win/lose), `ColumnCollapse`
- [x] Bỏ lớp alias `public static class BounceBlastGameplayManager` (tàn dư đổi tên từ design note) → dùng thẳng `GameplayManager`
- [ ] Đặt `GameplayManager` sẵn trong scene thay vì `AddComponent` lúc runtime từ Spawner
- [x] Spawner không tự respawn trong `OnValidate/OnEnable` nữa (mỗi lần sửa Inspector là DestroyImmediate + sinh lại hàng nghìn object) → dùng nút ContextMenu / Editor button

### 1.2 Bug đã biết

| # | Mức | Vị trí | Mô tả | Trạng thái | Hướng sửa |
| --- | --- | --- | --- | --- | --- |
| B1 | 🔴 | `Core/GameplayManager.cs:392`, `:564` | Cube bị crate che đang `SetActive(false)` → bị xóa vĩnh viễn khỏi homing / dồn cột | ✅ Đã sửa | Dùng cờ `isCovered` và `SetCovered()` |
| B2 | 🔴 | `Core/GameplayManager.cs` `TriggerColumnCollapse` | Dồn cột bỏ qua tường/đá/crate → cube rơi xuyên vật cản | 🟨 Đã lọc covered | Dồn theo ô trống thật |
| B3 | 🟠 | `Level/BounceBlastLevelSpawner.cs:601` | Cube cùng `groupId` chỉ cộng HP 1 lần vào bảng cân bằng | ✅ Đã sửa | Xử lý qua `AmmoBalancer` |
| B4 | 🟠 | `Level/BounceBlastLevelSpawner.cs:434` | Lưới đạn chỉ đọc tối đa 12 hàng CSV | ✅ Đã sửa | Đọc toàn bộ hàng level trong `LevelCsvParser` |
| B5 | 🟠 | `Units/BouncingBall.cs:483` | `OnCollisionStay2D` gọi lại `OnCollisionEnter2D` làm rung/dính tường | ✅ Đã sửa | Chỉ xử lý triệt tiêu khi chạm cube cùng màu hoặc crate |
| B6 | 🟡 | `Core/GameplayManager.cs:220` | Hộp đạn bị ẩn ngay khi bấm → mất đếm ngược và scale | ✅ Đã sửa | Giữ hiển thị đếm ngược khi xả bi, xả xong mới ẩn |
| B7 | 🟡 | `Units/BouncingBall.cs:79` | `gravityScale = 0.25` xung đột với ép tốc độ arcade | ✅ Đã sửa | `gravityScale = 0` vận tốc arcade mượt mà |
| B8 | 🟡 | `Level/BounceBlastLevelSpawner.cs:84` | Dòng màu `"p"` có biểu thức thừa | ✅ Đã sửa | Chuẩn hóa màu |
| B9 | 🟡 | `Core/GameplayManager.cs` | `Physics2D.IgnoreLayerCollision("Ball")` nhưng prefab chưa gán layer | ✅ Đã sửa | Gán layer "Ball" tự động trong `BouncingBall.Awake` |
| B10 | 🟡 | `Docs/design/tuan-02-...md` | Tài liệu lệch code | ✅ Đã sửa | Viết `Docs/design/core-loop.md` mô tả luật |

**Xong khi:** không file nào > ~300 dòng, B1–B5 đã sửa, level 1–20 chơi thắng được không kẹt.

---

## Phase 2 — Hoàn thiện core loop (3–5 ngày)

> Trước khi làm: quay video game gốc vài level, ghi luật vào `Docs/design/core-loop.md`.

- [ ] ⭐ **Điều kiện thua**: vượt **Capacity** (số bi trên bàn > sức chứa) hoặc deadlock — xem class gốc `Capacity` + panel *Out Of Capacity* ([phân tích §4.1](Docs/reference/ripped-assets-analysis.md))
- [ ] ⭐ State machine màn chơi: `Loading → Playing → Won / Lost`
- [ ] Hành vi bi khi hết mục tiêu cùng màu: thu về / chờ / quay lại conveyor (đối chiếu game gốc)
- [ ] **Ammo Tunnel**: hộp đạn tunnel nhả lần lượt các hộp trong `TunnelProps_v3` (hiện chỉ hiện số)
- [ ] Hộp đạn **Ice** (`/ice`): cần N lượt mới vỡ băng; **Question mark** (`/qm`): lộ màu khi được mở khóa
- [ ] Restart level, chọn level nhanh (debug)
- [ ] Tool editor: chạy validate cân bằng đạn cho toàn bộ 550 level, xuất báo cáo

**Xong khi:** chơi liên tục 30 level đầu có thắng/thua/chơi lại.

---

## Phase 3 — Vật cản & cơ chế (mỗi cơ chế 0.5–1 ngày)

Prefab đã có sẵn trong `Resources/prefabs/`; class gốc để tra field nằm trong `ExportedProject/Assets/Scripts/Assembly-CSharp/Blocker*.cs`.

Trên bàn:
- [ ] Wall (`/wl-`) — chặn bi, không phá được
- [ ] Stone (`/st-`) — phủ lên cube
- [ ] Curtain (`/cur-`)
- [ ] Lock + Key (`/lck-`, `CubeKeyVariant`)
- [ ] Bomb (`/bomb-`) — nổ phá vùng
- [ ] Board Tunnel (`/btu-`, `TunnelPropsBoard_v3`)
- [ ] Cube Growing / Shrinking / Multiplying (`CombinedCubicCubes Variant - *`)

Dưới khay đạn:
- [ ] Chain + ChainKey
- [ ] Arrow / ColorSwapArrow
- [ ] DoubleColor
- [ ] MegaCover / Cover / Lock

Mỗi cơ chế = 1 class trong `_Project/Scripts/Blockers/` + 1 level test + mô tả trong `Docs/design/`.

---

## Phase 4 — Game flow & UI (3–4 ngày)

- [ ] Thay `OnGUI` bằng uGUI: HUD (level, tiến độ %), popup Win / Lose
- [x] Màn hình Home (nút Play, level hiện tại) – scene MenuGame
- [ ] Lưu tiến độ (`PlayerPrefs` → JSON) — level đã qua
- [ ] Settings: âm thanh, rung
- [x] Scene flow: `MenuGame ⇄ Gameplay` (chưa có Boot)
- [ ] Safe area / tỉ lệ màn hình dọc (9:16 → 9:21)

---

## Phase 5 — Juice (2–4 ngày)

- [ ] VFX vỡ cube (`Block Break Particle`), vỡ crate, nổ bomb
- [ ] Âm thanh (56 clip trong `_Ripped/AudioClip`): bắn, nảy, vỡ, thắng/thua
- [ ] Animation: hộp đạn bay vào máng, cube nảy khi trúng, rơi cột có bounce
- [ ] Haptic trên Android
- [ ] Theme / ArtConfig (bluetheme, concentrictheme, wideboardtheme) theo độ khó

---

## Phase 6 — Build & tối ưu (2–3 ngày)

- [ ] ⭐ Build APK chạy trên máy thật
- [ ] Profiler: 60 FPS với ~300 bi cùng lúc; xem lại `GetComponentInParent` mỗi va chạm, `MaterialPropertyBlock` tạo mới mỗi lần
- [ ] Pool cho cube/VFX (hiện Instantiate/Destroy cả level)
- [ ] Thay `Resources.Load` bằng reference trực tiếp / Addressables; giảm kích thước build
- [ ] Input: hỗ trợ touch chuẩn (hiện dùng `Input.GetMouseButtonDown`)

---

## Phase 7 — Meta (tùy chọn)

- [ ] Booster (tham khảo `AB_AlternativeBoosters`, `AB_EGPBoosters`)
- [ ] Coin & phần thưởng
- [ ] Daily challenge (`sheets_levels/dailychallenge`)

---

## Nhật ký thay đổi lớn

| Ngày | Việc |
| --- | --- |
| 2026-09-29 | Tạo project Unity, import asset rip |
| 2026-09-30 | Tuần 2: core gameplay (cube, hộp đạn, bi, manager) |
| 2026-10-02 | Review dự án, viết README + ROADMAP |
| 2026-10-02 | Phase 0: git + LFS, sắp xếp `_Project/` & `_Ripped/`, dọn 1.513 asset thừa + 14 bộ level, scene 13 MB → 1,5 MB |
