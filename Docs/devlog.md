# Devlog

Ghi ngắn mỗi buổi làm: **làm gì – gặp vấn đề gì – quyết định gì**. Mới nhất ở trên.

## 2026-10-18 – Mỗi lớp một việc (chữ S trong SOLID)
- **GameManager:** dựng dự phòng đèn gạch + camera UI chuyển sang `Core/SceneRig`; bỏ `targetFrameRate` trùng (chỉ còn `FrameRateBootstrap`).
- **ConveyorLane:** mũi tên trên máng tách thành `Conveyor/LaneArrows`.
- **BoardController (1.095 → ~190 dòng):** `BoardGrid` (ô nào có khối nào, khối lộ ra / rơi được), `BoardTargeting` (bi ngắm khối nào + cache), `BlockVisualFactory` (dựng hình khối, nhãn số, cache mesh), `BlockBreakFx` (hiệu ứng vỡ thạch), `Core/ModelUtil` (tiện ích prefab dùng chung cho gạch, hộp, bi). Bỏ code chết `JoinedCellMesh`, `FitVisual`, `FlattenShading`. Mã cắt nguyên văn theo hàm bằng script, không gõ lại.
- **AmmoGridController (653 → 296):** `AmmoTrayView` (hình khay/hố/nắp), `AmmoPathRules` (luật thông đường).
- **AmmoBox (726 → 387):** `AmmoBoxFlight` (bay + đậu + nhãn số bi; máng gọi `box.Flight.*`), `AmmoDoorView` (cửa kính). So từng dòng với bản cũ: khác biệt chỉ là chỗ chuyển lớp + hàm `ToPlane` không ai dùng.
- Kiểm thử Unity: số khối/nhãn/khối cần phá ở L12/44/49/73/79 giống hệt trước khi tách; phá hòm lộ đúng 96 khối; tự chơi L79/L104: bi nảy, khối gộp mất máu, 2 máy xay xay nối tiếp, băng nứt; L104 cửa đảo trạng thái mỗi lần bắn (đóng: trọng số 0 hiện, mở: 90 ẩn); hết sức chứa → hộp dừng giữa chuyến bay đúng máng, Play On → bay tiếp vào máy xay, nhả đủ 20 bi; console sạch. Chưa thử trên Oppo.
- Lưu ý khi test bằng MCP: Editor hay tự bật Pause sau vài giây Play (không do code – không có `Debug.Break`, console sạch); nếu thấy hình đứng yên thì kiểm tra nút Pause.

## 2026-10-18 – Loại khối thành lớp riêng (SOLID: mở rộng không sửa code cũ)
- Trước: 18 chỗ rẽ nhánh theo `BoardItemKind` rải ở `BoardBlock`, `BoardController` (dựng/vỡ/ngắm/hình), `LevelParser` → thêm một loại khối phải sửa 5 file.
- Giờ: `Board/Kinds/` – `BlockKind` (lớp gốc, mặc định = gạch thường) + `CubeKind`, `CombinedCubeKind`, `CrateKind`, `WallKind`, `ColumnKind`, bảng tra `BlockKinds`. Mỗi lớp giữ luật (`CountsForWin`, `HitByAnyColor`, `IsStatic`, `IsColorTarget`, `Accepts`, `ShowsHpLabel`, `HitPunch`), cách dựng (`Build`, `ShearMesh`, `CubeLook`, `Material`) và cách vỡ (`OnBroken`). `BoardBlock.Rules` trỏ tới lớp luật; code cột nhà chuyển hẳn sang `ColumnKind`, hòm vỡ sang `CrateKind`. Ngoài `Kinds/` và chỗ `LevelParser` đọc token, không còn chỗ nào so sánh `BoardItemKind`.
- Không đổi luật: đối chiếu từng nhánh với code cũ; chạy thử L12/L44/L49/L73/L79: số khối theo loại, số khối cần phá (cột không tính), nhãn số, phá 4 hòm L79 lộ đúng 96 khối, cột trừ máu không tiêu hao bi, gạch chỉ nhận bi cùng màu, phá hết L12 báo thắng; console sạch.
- Hướng dẫn thêm loại mới (ví dụ khối bom): `Docs/them-loai-khoi.md`.

## 2026-10-18 – Dọn cấu trúc dự án (sau checkpoint `f39a500`)
- **Git:** `.gitignore` thêm `Captures/`, `Screenshots/` (đưa ra ngoài `Assets`), `__pycache__`, cấu hình máy riêng; gỡ khỏi git nhưng giữ file trên máy (bạn tự commit + push).
- **Cheat chỉ có ở bản test:** code `Scripts/Debug` bọc `#if UNITY_EDITOR || DEVELOPMENT_BUILD || BB_CHEATS` (các component giữ khai báo field để prefab không mất dữ liệu; `CheatHost` ở bản phát hành tự huỷ object). Bỏ cờ `allowInReleaseBuild` – thay bằng define `BB_CHEATS`. Kiểm: biên dịch cả cấu hình Editor và cấu hình giả lập Release (bỏ UNITY_EDITOR/DEVELOPMENT_BUILD) đều 0 lỗi.
- **Namespace theo thư mục:** `BounceBlast.UI`, `BounceBlast.Menu`, `BounceBlast.Cheats` (không dùng `…Debug` để khỏi che `UnityEngine.Debug`); `BoosterType` chuyển về `Gameplay/Core/BoosterType.cs`. Đổi namespace không làm mất tham chiếu (Unity nối script theo GUID): 0 script mất trong 2 scene + mọi prefab.
- **`BoardController` (1.095 dòng) chia `partial`:** `.cs` (dựng khối + lưới, 194 dòng), `.CombinedMesh.cs`, `.Visuals.cs`, `.Hits.cs`, `.Queries.cs`. Không đổi logic: so từng dòng với bản cũ chỉ khác phần đầu file.
- **Font:** `royal_kingdom_font.ttf` đang dùng chuyển từ `TextMesh Pro/Fonts` sang `Art/UI/Common/Fonts` (giữ GUID); bản trùng ở gốc `_Project` (không ai dùng) đưa vào `_Archive/MoCoi`.
- Cập nhật `ARCHITECTURE.md`, `CODING_CONVENTION.md` (quy ước namespace, mỗi MonoBehaviour một file, chia partial, bọc cheat).
- Chạy thử: màn 60 nạp đủ 1344 khối, cheat mở được, auto-play 25 s phá 294 khối, console sạch.

## 2026-10-16 – Tối ưu hiệu năng cho mobile (đo bằng ProfilerRecorder, Editor, quality Medium, auto-play)
- Điểm nóng thật ở màn dày (≈250 bi): `Ball.OnCollisionEnter2D` → `TryGetLineOfSightTarget` → 5 × `Physics2D.CircleCast` mỗi lần nảy ≈ 17 ms/khung. Thay bằng duyệt thẳng trên lưới ô (`FirstBlockOnPath`, 3 tia, tối đa 3 ứng viên): PlayerLoop 28 → ~7 ms, khung tệ nhất 88 → ~33 ms (màn 150).
- `BoardBlock`: khối đứng yên là Rigidbody2D **Static** (trước: 1284 vật thể Kinematic); chỉ Kinematic khi rơi. Physics2D.Simulate màn 60: ~10 → ~3.4 ms.
- Tắt đổ bóng ở quality Very Low/Low/Medium (Android mặc định Medium) và vSync = 0: ảnh chụp có/không bóng giống hệt → bóng chỉ tốn GPU.
- `FrameRateBootstrap`: 60 fps, `Time.maximumDeltaTime` 0.05 (trước 0.333), mobile chặn rộng ≤ 1080 px; `AdaptiveResolution`: fps < 45 trong 2 cửa sổ 3 s liền → hạ độ phân giải ×0.85 (sàn 640 px).
- Chưa làm / chưa chắc: rác bộ nhớ (GC) ~35–60 KB/khung đo trong Editor lẫn chi phí của Editor (baseline dao động 25–58 KB) → cần đo trên máy (FPS overlay + Development Build). Cần kiểm tra lại hành vi ngắm bi sau khi đổi LOS (ít khác biệt nhưng chưa test chơi tay).

## 2026-10-16 – Số HP trên gạch lớn rõ hơn
- BoardController.CreateLabel: FaceDilate 0.18 (chữ đậm), viền đặc 0.2 màu tối alpha 1, softness 0 (trước: viền alpha 0.8 mềm làm số xám, nhoè). Kiểm tra bằng ảnh Level 12.

## 2026-10-16 – Sàn collider theo boardOffsetY
- ArenaWalls.EnsureBuilt gọi SyncFloor: kéo 2 điểm đáy của EdgeCollider2D (đã nung sẵn trong scene) về arena.yMin + boardOffsetY mỗi lần vào game. Trước đây sàn đứng yên khi đổi Board Offset Y.
- Đo (offset 0.1): đáy collider = -6.25 = -6.35 + 0.1.

## 2026-10-16 – Công cụ cheat (MVP)
- Thêm `Scripts/Debug/Cheat*` + prefab `Prefabs/UI/CheatPanel` (đặt sẵn trong Gameplay.unity, dựng bằng menu Tools ▸ Bounce Blast ▸ Cheat).
- Mở panel: chạm 5 lần góc trên-trái, chạm 3 ngón, hoặc F1 (Editor). Chỉ chạy ở Editor / Development Build.
- Tab Level (đi tới level, restart, trước/sau, WIN/LOSE), Resources (coin, booster, capacity vô hạn, reset save), Speed (x0.25–x4, pause, auto-play, FPS overlay).
- Kiểm thử Editor: mở panel, 3 tab dựng đúng, speed x2 + FPS overlay chạy, CheatLose gọi được; chưa thử trên Oppo.
- Lưu ý: mỗi MonoBehaviour phải một file riêng (CheatRows.cs chứa 2 class làm mất script khi gắn vào prefab).

## 2026-10-15 — Feedback: gạch bo tròn, chữ Capacity méo, VFX khối lớn

- **Gạch bo tròn:** mesh rip `pCube35` chỉ 30 đỉnh (vát phẳng, góc nhọn); `cubeOverlap 1.10` lại che hết góc → ô vuông dính nhau. Thêm `RoundedCubeMesh` (mesh "gối": mặt trước -Z phẳng, mép + 4 góc bo, cùng kích thước mặt trước pCube35) dùng cho gạch 1 ô; `GameConfig` thêm `cubeCornerRadius 0.25`, `cubeEdgeRound 0.15`, `cubeBodyDepth 0.97`, `cubeOverlap` 1.10 → 1.02. Khối gộp lớn vẫn dùng mesh rip giãn 9-slice. Đặt `cubeCornerRadius = 0` để quay về mesh rip. Còn: chấm nền hồng ở giao điểm 4 viên (gốc tối hơn).
- **Chữ CAPACITY bẹp:** canvas thanh ép dọc ×0.8 (cho dày đúng gốc) + camera game ép dọc thêm cos20 → chữ bẹp ~25%. `CapacityBarView.UnsquashLabel` đặt scale y của chữ = x / cos20 trong hệ thế giới (đặt tuyệt đối, không nhân dồn).
- **VFX khối lớn (>1 HP):** trước đây phóng 1 hiệu ứng ×(tối đa 3) lên cả khối → vài giọt rất to. Giờ nổ như gạch 1 HP: mỗi ô 1 cụm (`BoardController.MaxBigBreakBursts` = 16, lấy cách đều).
- **Khối gộp bị hở sau khi đổi cubeOverlap** (Lv 49: khe tối giữa khối 200/120 và gạch kề): `blockTopTrim` 0.13→0.08 và `blockBottomExtra` 0.12→0.07 bù đúng phần gạch 1 ô không còn nhô ra nữa (1.10→1.02). Đã kiểm Lv 49 + Lv 44, hết khe; tương đương chỉnh Solid Py ≈ +0.04 như user làm tay, nhưng nằm trong config nên áp cho mọi khối.
- **Cập nhật cùng ngày (user báo vẫn hở ở vài level):** quét 100 level bằng ảnh chụp Game view (ảnh có khối vs ảnh ẩn hết khối; thành phần liên thông "giống nền" mỏng & dài = khe). Với trim 0.08 / extra 0.07 còn khe ngang 2–8px ở Lv 6, 23, 37, 39, 42, 49, 54 (mép TRÊN khối gộp tụt xuống). Chốt `blockTopTrim = 0`, `blockBottomExtra = 0`: Lv 1–100 hết khe ngang/dọc, đáy khối ngang đáy hàng gạch. Số cũ (0.13/0.12) là bù cho cubeOverlap 1.10 – không còn cần.
- **Thanh CAPACITY tràn màu ra ngoài viền:** `Fill Bar Mask` cùng cỡ với `Bg` nên góc bo của mask lớn hơn góc ngoài của viền → vệt màu thò ra ở 2 đầu thanh (rõ khi đầy). Thu mask vào `sizeDelta (-8, -8)` trong `CapacityBar.prefab` (phần thò ra vốn nằm dưới viền nên không mất hình). Đã kiểm 2 đầu ở 100%.
- Quy trình: chụp Game view trong Editor qua cầu nối tạm (đã xoá), so từng bước với ảnh gốc `ảnh game gốc.jpg`.

## 2026-10-15 — Đổi font toàn game sang royal_kingdom_font (Mikado)

- Đổi nguồn của 3 font asset TMP sang `Assets/TextMesh Pro/Fonts/royal_kingdom_font.ttf` NGUYÊN TẠI CHỖ (giữ GUID, atlas, material outline → mọi prefab/scene/material tự dùng font mới, không phải gán lại): `UI/Common/Fonts/Fredoka-Bold SDF` (UI), `Board/Fonts/Fredoka-Bold SDF 1` (số HP trên bàn – `GameConfig.worldFont`), `TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF` (font mặc định TMP). Tên asset vẫn là Fredoka/Liberation nhưng bên trong là glyph Mikado.
- Bộ ký tự dựng sẵn (Static, 1024², cỡ lấy mẫu 52/86, padding 10/9 như cũ): ASCII + Latin-1 + dấu nháy/gạch/… (font mặc định 86pt chỉ ASCII + dấu câu vì hết chỗ). Thiếu ký tự nào (VD tiếng Việt có dấu) thì phải dựng lại atlas.
- Bẫy: atlas là file PNG riêng; sửa atlas trong bộ nhớ không ghi ra PNG; atlas SDF phải là Alpha8 (ghi RGBA32 thì glyph bị xếp 4 lần, chữ vỡ vụn). Làm đúng: dựng ở font asset tạm → chép bảng glyph + ghi PNG (alpha = SDF).
- `UI/Common/Fonts/LiberationSans SDF.asset` là asset hỏng sẵn (Missing Script, không nạp được) → 3 canvas đang trỏ vào nó rơi về font mặc định TMP, nay cũng ra Mikado.

## 2026-10-16 — Tối ưu màn dày đặc (L60: 1344 khối, ~140 bi)

Đo bằng ProfilerRecorder trong Editor (L60, 4 hộp vừa bấm): avg 68 → **95 fps**, khung chậm nhất 91 → **24 ms**, PlayerLoop 6.4 → 3.5 ms, FixedUpdate script 1.05 → 0.06 ms, LateUpdate 1.35 → 0.08 ms, Update 1.23 → 0.56 ms, tam giác 465k → **201k**, GC không tăng.
1. **`BoardBlock` ngủ khi đứng yên:** mỗi khối chạy FixedUpdate + Update + LateUpdate mỗi khung (1344 khối × 3) và Rigidbody2D nội suy. Giờ component tắt (`enabled=false`, nội suy None) và chỉ bật khi khối rơi (`SetTarget`) hoặc bị trúng (`TakeHit`); tự ngủ lại khi xong. Kiểm: phá 6 khối thấp nhất → 241 khối rơi đúng ô (lệch 0.000), 0 khối còn thức.
2. **Bộ nhớ đệm ngắm bi (`BoardController.RefreshLosCache`):** `TryGetLineOfSightTarget` mỗi lần duyệt cả 1344 khối + `IsExposed` (iterator cấp phát) cho từng bi va chạm. Giờ lưu danh sách khối lộ ra theo màu, chỉ dựng lại khi lưới đổi (`_gridVersion`); `IsExposed` không cấp phát.
3. **`RoundedCubeMesh` thưa hơn** (góc 6→3, mép 4→2 đoạn): ~310 → ~110 tam giác/viên; ở cỡ gạch ~25 px không thấy khác.
- Còn lại (chưa làm): giảm số bi cùng lúc, tắt Collider đối với lớp khối bên trong không bao giờ bị chạm, dynamic batching cho gạch (87 batch hiện ổn). Chưa đo trên Oppo.

## 2026-10-16 — [ĐÃ HOÀN TÁC theo user: về màu nền gốc; `maskAlpha`/`cameraBackground` vẫn còn trong Theme, đặt 1 / alpha 0] Nền sau bàn sáng hơn (bản D · xanh tím sáng, theme Normal)

- Nền sau bàn = 5 lớp: màu camera, lớp gradient `Mask NORMAL` (sprite xanh đậm đục), `Plane` (t4-surface), `Backdrop` (t4-wall, nền khay), `TrayWallLip` (tính từ t4-wall + t4-bg). Bản đã chọn: so sánh 4 bản ở `Docs/palette-previews/nen-sau-ban.png|md`.
- Áp: `t4-wall._BaseColor` → (0.30, 0.28, 0.64) (gờ khay tự sáng theo); `Theme` thêm 2 trường `maskAlpha` (1 = như gốc; Normal = 0.25) và `cameraBackground` (alpha 0 = dùng groundColor; Normal = (0.40, 0.38, 0.82)) – tách khỏi `groundColor` vì groundColor còn quyết định màu khung/máy xay/mũi tên. Hard / Super Hard không đổi.
- **Sửa lại theo user:** chỉ đổi nền NGOÀI; nền TRONG lòng bàn gạch (`Plane` / `t4-surface`) trả về màu gốc (0.169, 0.180, 0.357). Kiểm: pixel lòng bàn trước/sau giống hệt (40, 43, 92). Nền ngoài (màu camera, `maskAlpha`, `t4-wall` nền khay) giữ bản D.
- Hoàn tác nền ngoài: `git checkout -- Assets/_Project/Art/Shared/Materials/t4-wall.mat` và đặt `maskAlpha` 1, `cameraBackground` alpha 0 trong GameConfig › normalTheme.

## 2026-10-16 — Hộp đầu tiên chưa khớp miệng máy xay lúc máy bắt đầu xay (user phát hiện)

- User nhận xét đúng: hộp đầu "chưa kịp scale/đặt vị trí đúng miệng máy xay như hộp hàng chờ". Hộp hàng chờ rơi xuống đã đúng dáng ngay; hộp bay lên thì máy xay chạy từ lúc đầu ống chạm (0.455 s) nhưng thân ống còn nghiêng/phình/to hơn miệng cho tới khi hết pha lắng (0.855 s) → ~0.4 s "lệch" đúng khoảng user thấy khựng.
- Sửa (`AmmoBox.FlyRoutine`): pha lắng bắt đầu sớm (u = 0.6) và kết thúc 0.1 s sau khi hết bay; đầu ống kéo về chỗ đậu cùng lúc (`head = Lerp(head, total, sm)`); máy xay chạy khi u = 0.8.
- Đo (góc nghiêng so với trục máy xay / bề ngang xương / vị trí đáy): t 0.30 s: 68° / 0.61 / lệch 1.4; t 0.45 s: 2° / 0.54; t 0.50 s: 0° / 0.49; t 0.65 s: 0° / **0.42 = bề ngang đậu** – tức lúc máy xay chạy (0.515 s) ống đã thẳng khít trục, bề ngang chỉ còn lệch ~12% và hết ở 0.65 s. Chưa thử trên máy thật.

## 2026-10-16 — Hộp ĐẦU TIÊN lên máy xay khựng 0.4–0.5 s (hàng chờ thì mượt)

- Dấu hiệu: chỉ hộp đầu khựng, các hộp sau (dùng lại pool) mượt → "khởi động nguội". Lúc hộp đầu chạm máy xay cùng một khung hình xảy ra: dựng bi đầu tiên (`Instantiate` prefab + Rigidbody2D + collider + trail, pool trống), tạo material vệt bi từng màu, vẽ lần đầu hệ hạt máy xay (40 hạt) → biên dịch shader trên GLES3. Suy luận từ code, chưa đo trên Oppo.
- Sửa: `BallManager.Prewarm` (GameManager gọi lúc vào màn): `SetPass` shader bi + vệt bi từng màu, rồi dựng sẵn `GameConfig.ballPrewarm` (60) viên vào pool, 4 viên/khung; `CrusherFx.WarmUp` phát thử 1 hạt trong suốt ở 2 hệ hạt. Muốn đổi số bi dựng sẵn chỉnh `Ball Prewarm` trong GameConfig.
- Chưa kiểm trên máy thật; trong Editor mọi khung sau lúc bấm đều < 40 ms (chỉ khung bấm 44 ms).

## 2026-10-16 — Hộp chui vào máy xay bị giật (phân tích video máy thật)

Video `20261016-0430` (30 fps, thu từ máy): hộp bay lên tới máy xay thì ống nghiêng/phình, đứng gần như yên ~3 khung rồi "búng" thẳng đứng. Đo trong Editor (mỗi bước = một frame game) lúc hết chuyến bay (≈0.85 s): xương đuôi nhảy **2.06 đơn vị**, xương đầu nhảy 1.48, nhãn số bi nhảy 11.8. Các nguyên nhân (mỗi cái là một bước nhảy lúc chuyển từ dáng bay sang dáng đậu):
1. **Lật đầu-đuôi:** `Build` (bay) đặt xương 11 (đầu) ở ĐÁY máy xay, `PoseStraight` (đậu) đặt xương 0 ở đáy → thân hộp lật ngược đúng lúc đậu. Sửa: `PoseStraight` theo cùng chiều chuyến bay (`AmmoFlightTube.HeadAtBottom`, đặt false để về kiểu cũ).
2. **Đoạn thẳng cuối quỹ đạo ngắn hơn thanh đậu** (cung bo nuốt mất) → đuôi lệch 0.58. Sửa: pha LẮNG 0.22 s cuối chuyến bay kéo từng xương (vị trí + góc) về đúng dáng đậu (`BlendToStraight`) thay vì dừng đột ngột khi lò xo chưa lắng.
3. **Lò xo đuôi tích phân theo dt khung hình** (explicit Euler, dt ≤ 0.05) → rung khác nhau theo fps máy, hay hết giờ mà chưa lắng rồi "búng". Sửa: bước cố định 1/120 s.
4. **Nhãn số bi + vị trí hộp** nhảy khi `transform.position` đổi từ tâm thân sang chỗ đậu → nhãn neo vào chỗ đậu ngay từ lúc bay; vị trí hộp nội suy mượt trong pha lắng.
5. **Khựng khi chạm máy xay:** tạo `TextMeshPro` nhãn số bi và dựng mesh gắn xương lần đầu ngay lúc hộp tới → dựng sẵn lúc nạp hộp (`EnsureDockLabel`, `AmmoFlightTube.Prewarm`).
- Kết quả đo lại: sau 0.7 s mọi bước đều ≤ 0.094 đơn vị/frame (trước 2.06), nhãn đứng yên (0.000). Chưa thử trên máy thật.

## 2026-10-16 — Máy xay: xay ngay khi hộp tới + hàng chờ liền mạch

- Nguyên nhân khựng: (1) `FlyRoutine` chỉ gọi `done` SAU khi lò xo đuôi lắng (≤ ~1 s) rồi máng mới `Dock` → máy xay đứng im dù đầu ống đã chạm; (2) hộp xếp hàng bay thẳng vào máy xay đang bận rồi mới `MoveTo` chỗ chờ; (3) hộp cạn thì biến mất, hộp kế mới trượt vào → hở nhịp; (4) `PickLane` không tính hộp đang bay nên bấm nhanh hay dồn một máng.
- Sửa (`ConveyorLane`, `AmmoBox`, `ConveyorSystem`): máng có **đường ống `_pipe`** theo thứ tự bấm; `Reserve` ngay lúc bấm → hộp bay thẳng tới chỗ của nó (máy xay hoặc chỗ chờ phía trên, không chồng lên hộp đang xay); `HeadArrived` (đầu ống chạm, u ≥ 0.7) → hộp đầu hàng xay **luôn**, đuôi còn lắng thì kệ; còn ≤ 4 bi thì hộp kế được kéo xuống sát máy xay (preload) và nối tiếp **giữ nhịp nhả bi** (không nhả dồn, không hụt). `AmmoBox.MoveTo` khi đang bay thì hoãn tới lúc yên vị thay vì cắt chuyến bay.
- Đo trong Editor (3 hộp 32/28/36 bi bấm cùng lúc): máy xay chạy sau ≈ 0,47 s kể từ lúc bấm (trước ≈ 1 s), khoảng cách lớn nhất giữa hai viên bi 0,058 / 0,068 s (nhịp chuẩn ~0,05 s) kể cả lúc chuyển từ hộp 28 sang hộp 36 – không còn hụt nhịp. Chưa thử trên máy thật.

## 2026-10-16 — Feedback từ bản build Oppo: Settings, nút bấm, panel thắng

- **Settings (menu) méo + to:** (1) 6 icon `Icon` không `preserveAspect` nên bị kéo vuông trong khi sprite gốc không vuông (Volume 363×308, Music 282×291, Vibration 352×361) → bật `preserveAspect` ở MainMenuCanvas + SettingsCanvas; (2) 3 khung hàng `SoundArea/MusicArea/Vibration` scale 1.21 và khung nền dùng sprite `_mir` + MirroredImage kéo giãn → đổi sang sprite đầy đủ `Setting Home Buttons` (Sliced), scale hàng về 1, container thu 0.9.
- **Phản hồi nút bấm:** thêm `UiButtonFeedback` (lún khi nhấn, nảy khi thả, nở nhẹ khi rê chuột; lò xo, không phụ thuộc timeScale) + menu `Tools ▸ Bounce Blast ▸ UI ▸ Thêm phản hồi cho mọi nút` (đã gắn 54 nút / 8 prefab; nút mới thêm sau thì chạy lại menu).
- **Panel thắng cháy sáng trên máy:** `AreaShine` là hạt cộng sáng (`glow6_ADD`, UI/Additive) phóng ×250, `maxParticleSize` 0.5 → độ sáng phụ thuộc kích thước/độ phân giải màn nên Editor ổn còn máy thật cháy trắng. Thay bằng `AreaGlow` (RawImage `fx_glow`, alpha-blend, alpha 0.5 thở ±15%) nằm sẵn trong prefab; `AreaShine` chỉ còn là phương án dự phòng khi chưa gán `areaGlow`. Chưa kiểm trên máy thật.

## 2026-10-15 — Đổi bảng màu sang bộ C · Neon Fresh

- Màu cũ bị chê "nhem nhuốc": bóng (SColor) tối/đục, `_IndirectIntensity` thấp, độ lệch sáng-tối ngẫu nhiên mỗi gạch rộng (±0.1) → loang bẩn. Đã dựng 4 bộ A–D, render thật trong game để chọn (`Docs/palette-previews/`), user chọn **C**.
- Áp vào 32 material màu (cube + hộp đạn + bi, mỗi mã 3 file) qua `_BaseColor/_HColor/_SColor`, `_IndirectIntensity` 0.70; `ColorLibrary.shadeOffset` ±0.015 (trước ±0.05–0.15), đen = xanh than đậm thay vì xám. Chỉ đổi giá trị, không đổi shader/GUID.
- Hoàn tác: `git checkout -- Assets/_Project/Art/Shared/Colors Assets/_Project/Data/ColorLibrary.asset` (hộp đạn/bi dùng material cùng thư mục màu).

## 2026-10-15 — Cơ chế khay/bàn dùng asset gốc (băng, cửa, tunnel, hộp gỗ)

- Đối chiếu 6 ảnh `Mechanics/` với game. Bản đồ cơ chế ↔ asset ↔ script: `Docs/co-che-asset-map.md`.
- **Băng:** bỏ bản sao jelly + số đếm tự dựng; dùng `Ice Model` gốc (4 mức + mảnh vỡ + tuyết) – `IceBlockerView`. Gốc không có số trên băng.
- **Cửa:** bỏ 2 tấm phẳng + tấm lót dựng bằng code (~100 dòng); dùng mesh `Cover` gốc + blend shape (có vạch sáng chéo và khe giữa như ảnh).
- **Tunnel:** màu hộp kế tiếp đổ nhầm vào thân (slot 1) → đúng là **vành (slot 0)**; thân giữ thép xanh như gốc.
- **Hộp gỗ:** ghép mảnh `ModularPrimitiveCrate` (kéo ra khỏi `_Archive`) + ván chéo từ `PlaneMid` + văng `CrateFracture`; tách `CrateView`, `ModularPrimitiveBuilder` (dùng chung với cột nhà) và `DebrisFx`.
- Phát hiện: token chưa hỗ trợ (st, cur, gl, bqm, ch, ba, btu, acs, lck, bomb, dc) đều coi là vật cản tĩnh – xem danh sách trong file bản đồ.

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
