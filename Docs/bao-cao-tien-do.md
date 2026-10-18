# Báo cáo tiến độ — Bounce Blast (bản làm lại)

> Từ khởi tạo dự án **29/09/2026** đến **16/10/2026** (18 ngày lịch).
> Nguồn số liệu: `Docs/devlog.md`, lịch sử git, `Docs/lich-su/qua-trinh-khoi-phuc-va-core-gameplay.md`, `ARCHITECTURE.md`, ngày sửa file trong repo.
> **Lưu ý trung thực:** git chỉ có 4 commit (09/10 ×2, 13/10 ×2); phần lớn công việc từ 14–16/10 **chưa commit**. Ngày của các mốc 06–08/10 và 10–12/10 được suy ra từ ngày sửa file, không phải nhật ký từng giờ. Dự án không ghi lại số giờ làm, nên báo cáo tính theo **ngày**.

---

## 1. Tóm tắt một trang

| Hạng mục | Kết quả |
| --- | --- |
| Mục tiêu | Làm lại game **Bounce Blast 3.7.x** (Unity 2022.3.62f3, Built-in RP, Gamma) giống bản gốc về màu sắc, cơ chế, UI, hiệu năng; chạy được trên Android |
| Cách làm | Rip asset gốc (AssetRipper) → tự viết tool khôi phục shader và UI → viết lại core gameplay theo video/ảnh gốc → tái cấu trúc → sửa theo phản hồi bản build trên máy Oppo |
| Công cụ tự viết | `Tools/ShaderRipper` (rip GLSL + dịch máy sang HLSL), `Tools/UnityYaml/ui_extract.py` (tách canvas UI gốc), `ArtReorganizer`, `UiButtonFeedbackInstaller`, `CheatPanelBuilder` |
| Đã làm được | 25 shader khôi phục (biên dịch 0 lỗi), UI gốc khôi phục thành prefab, core gameplay chơi được tới hàng trăm màn, cơ chế băng/cửa/tunnel/hộp gỗ/khối L/máy xay, bảng màu mới, font mới, tối ưu hiệu năng, công cụ cheat để test trên mobile |
| Chưa chốt / còn lại | Kết quả trên **máy Oppo thật** cho các sửa gần nhất (hiệu năng, máy xay mượt, glare panel thắng); nhiều loại khối chưa làm (xem mục 6); chưa commit đợt 14–16/10 |

---

## 2. Dòng thời gian theo ngày

| Ngày | Việc chính | Kết quả / số liệu | Chứng cứ |
| --- | --- | --- | --- |
| **29/09** | Khởi tạo dự án Unity 2022.3.62f3, import asset rip từ AssetRipper | Project chạy được, còn nhiều asset thô | devlog |
| **30/09** | Tuần 2 – core gameplay đầu tiên: khối, hộp đạn, bi nảy, 2 máng băng chuyền, ngắm bi, dồn cột, hòm gỗ | Bản chơi thử đầu tiên (code đoán luật, sau đó viết lại) | devlog |
| **02/10** | **Phase 0 – dọn dẹp:** khởi tạo git + LFS, đưa code vào `Assets/_Project`, xoá 1.513 file rip không dùng, giữ 2 bộ level | Assets từ 361 MB xuống 134 MB; scene từ 13,4 MB xuống 1,5 MB | devlog |
| **02/10** | **Phase 1 – refactor:** tách parser level, bộ cân bằng đạn, module bi/ô đạn/bàn/cột/input; sửa các bug B3, B4, B8, B10; viết `core-loop.md` | Code core chia module thay vì một class lớn | devlog, `Docs/lich-su/roadmap-cu.md (core-loop.md cũ đã bỏ)` |
| **05/10** | **Khôi phục shader:** AssetRipper chỉ ra shader dummy (game gốc chỉ có GLES). Tự viết tool đục mã GLSL trong blob shader và **dịch máy GLSL→HLSL**, giữ GUID gốc. Đổi project sang **Gamma**, chép thông số ánh sáng từ scene gốc | 25 shader biên dịch 0 lỗi; màu khớp bản gốc | `Docs/qua-trinh-...md` mục 1 |
| **05/10** | **Khôi phục UI:** tách nguyên canvas UI gốc từ `ManagerScene` thành prefab, khôi phục `MirroredImage` cùng GUID; nối logic vào `GameUI` | 385 file (~35 MB) UI/font/vật lý/audio chép vào, giữ GUID | devlog, mục 2 |
| **05/10 → 07/10** | **Viết lại core gameplay theo video + chữ ký class gốc:** luật Capacity, khay đạn (hố, cửa, băng, tunnel), máy xay, khối L, theme theo ArtConfig, ngắm bi theo `BouncingBall`; 15 đợt sửa theo ảnh/video | Chơi được các màn đầu; chi tiết từng đợt ở mục 3.4–3.40 của tài liệu quá trình | `Docs/qua-trinh-...md` |
| **07/10** | Chốt camera ortho nhìn thẳng, tool chỉnh tay tư thế hộp, bộ số tư thế do người dùng chốt, màn thắng/pháo hoa/xu | Một hướng nhìn thống nhất | memory dự án, tài liệu quá trình |
| **09/10** | **Commit "09/10"** (141 file): ghi lại toàn bộ quá trình khôi phục + kiến trúc UI | Mốc lưu đầu tiên của phần khôi phục | git `0ea4437` |
| **10 → 12/10** | **Kiến trúc UI đúng chuẩn:** mỗi panel = prefab đặt sẵn trong Hierarchy + một script View riêng; `GameUI` chỉ điều phối; thanh booster và capacity thành prefab | Dễ review với người có kinh nghiệm hơn; viết `kien-truc/ui.md` | `Docs/kien-truc/ui.md` |
| **12 → 13/10** | **Tái cấu trúc toàn dự án (A–G):** Art xếp theo tính năng, asset không dùng vào `_Archive` (không xoá), bỏ `Resources` gọi bằng chuỗi, `ARCHITECTURE.md`, kiến trúc manager, kiểm tra asset MenuGame | Đã chuyển file bằng `AssetDatabase.MoveAsset` để giữ GUID | `Docs/lich-su/tai-cau-truc.md`, `ARCHITECTURE.md (mục 4b)`, `tham-khao/menugame-asset-goc.md` |
| **13/10** | **Commit "13/10" và "demo 13/10"** (3.797 file đổi) | Bản demo đầu tiên | git `adb3575`, `059fb2e` |
| **15/10** | **Cơ chế khay/bàn dùng asset gốc:** băng (Ice Model gốc), cửa (mesh Cover + blend shape), tunnel (sửa màu vành), hộp gỗ (ghép mảnh gốc + ván chéo), tách `CrateView`/`ModularPrimitiveBuilder`/`DebrisFx`; bản đồ cơ chế ↔ asset ↔ script | Giảm code tự dựng (ví dụ cửa ~100 dòng); phát hiện 11 token chưa hỗ trợ | `Docs/kien-truc/co-che-asset-map.md`, devlog |
| **15/10** | **Bảng màu mới "C · Neon Fresh":** dựng 4 bộ A–D, render thật trong game để người dùng chọn; áp lên 32 material | Hết màu "nhem nhuốc"; có lệnh hoàn tác bằng git | `Docs/lich-su/mau-sac/`, devlog |
| **15/10** | **Gạch bo tròn** (`RoundedCubeMesh`), **sửa chữ CAPACITY bị bẹp**, **VFX khối lớn** như gạch 1 HP, **hết khe hở** giữa khối gộp (quét 100 level bằng ảnh), **thanh CAPACITY tràn viền** | Lv 1–100 hết khe ngang/dọc | devlog |
| **15/10** | **Đổi font toàn game sang royal_kingdom_font** giữ GUID; sửa lỗi atlas SDF vỡ (Alpha8 vs RGBA32) | Mọi prefab tự dùng font mới | devlog |
| **16/10 (sáng)** | **Hướng dẫn build Android** trên máy Oppo (xử lý lỗi Unity Hub 3.11.1 không cài được NDK/JDK; ARM64 + IL2CPP + GLES3) | Bản build đầu chạy trên máy thật | hội thoại |
| **16/10** | **Sửa theo phản hồi máy Oppo:** icon Settings méo, nút bấm không có phản hồi (thêm `UiButtonFeedback` cho 54 nút / 8 prefab), panel thắng cháy sáng (thay hạt cộng sáng bằng `AreaGlow`) | Chưa kiểm trên máy thật | devlog |
| **16/10** | **Máy xay giật/khựng (phân tích 2 video quay từ máy):** hộp bay thành ống thạch lật đầu-đuôi, lò xo đuôi phụ thuộc fps, khởi động nguội ở hộp đầu; sửa bằng pipeline hàng chờ, xay ngay khi đầu ống chạm, prewarm bi và shader | Xương nhảy 2.06 → ≤ 0.094 đơn vị/khung; máy xay chạy sau 0.47 s thay vì ~1 s | devlog (4 mục) |
| **16/10** | **Nền sau bàn sáng hơn → hoàn tác theo yêu cầu** (giữ lại 2 tham số `maskAlpha`, `cameraBackground` đặt về mặc định) | Về màu nền gốc | devlog |
| **16/10** | **Tối ưu màn dày đặc (L60, 1344 khối):** khối ngủ khi đứng yên, bộ nhớ đệm ngắm bi, mesh gạch thưa hơn | 68 → 95 fps (Editor), khung chậm nhất 91 → 24 ms, tam giác 465k → 201k | devlog |
| **16/10** | **Công cụ cheat (MVP):** mở bằng 5 chạm góc trên-trái / 3 ngón / F1; tab Level, Resources, Speed; FPS overlay; chỉ chạy ở Editor/Development Build | Đã chạy trong Editor; chưa thử trên Oppo | `Docs/thiet-ke/cheat-tool.md`, devlog |
| **16/10** | **Lỗi sàn collider không theo Board Offset Y** và **số HP trên gạch lớn bị mờ**: sửa `ArenaWalls.SyncFloor`, viền chữ đặc + chữ đậm | Đáy collider đo đúng -6.25; số rõ hơn khi so ảnh | devlog |
| **16/10** | **Hiệu năng cho mobile (đo thật):** thủ phạm là 5 lần `CircleCast`/lần nảy (~17 ms/khung) → thay bằng duyệt lưới ô; khối đứng yên thành Static; tắt đổ bóng ở mức Medium; khoá 60 fps, giới hạn bước thời gian, tự hạ độ phân giải khi fps thấp | PlayerLoop 28 → ~7 ms; khung tệ nhất 88 → ~33 ms; vật lý màn 60: ~10 → ~3.4 ms (đo trong Editor) | devlog |

---

## 3. Cách làm việc giữa bạn và Claude

| Bước | Cách thực hiện |
| --- | --- |
| 1. Nguồn sự thật | Video và ảnh chụp game gốc, asset đã rip, chữ ký class decompile — **đo ảnh gốc trước khi sửa**, không đoán |
| 2. Phản hồi | Bạn gửi ảnh, video hoặc mô tả lỗi (hầu hết từ bản build trên máy Oppo) → Claude phân tích nguyên nhân → sửa → **chụp lại trong Editor để kiểm** |
| 3. Quyết định | Với thứ mang tính thẩm mỹ (bảng màu, nền sau bàn), Claude **dựng nhiều bản để bạn chọn** rồi mới áp; bạn từng yêu cầu hoàn tác (nền sáng hơn) và việc đó được làm trọn |
| 4. Ưu tiên asset gốc | Khôi phục asset/shader/UI gốc bằng tool, giữ GUID, thay vì dựng lại bằng code; chỉ dựng bằng code khi không có cách khác |
| 5. Kiến trúc | Mỗi panel UI một prefab + một View script; `GameConfig` chứa số chỉnh được; cấu trúc đọc được bởi người review |
| 6. Kiểm chứng | Dùng UnityMCP để điều khiển Editor, chụp màn hình, đo bằng ProfilerRecorder; ghi từng thay đổi vào `devlog.md` |
| 7. An toàn | Không xoá file khi chưa được phép; di chuyển bằng `AssetDatabase.MoveAsset`; mọi sửa lớn có lệnh hoàn tác ghi trong devlog |

---

## 4. Số liệu đo được

| Chỉ số | Trước | Sau | Ghi chú |
| --- | --- | --- | --- |
| Dung lượng Assets | 361 MB | 134 MB | Phase 0, 02/10 |
| Dung lượng scene Gameplay | 13,4 MB | 1,5 MB | Phase 0 |
| Shader khôi phục | 0 (dummy) | 25, 0 lỗi biên dịch | 05/10 |
| fps màn 60 (Editor) | 68 | 95 | Đợt tối ưu thứ nhất (khối ngủ + cache ngắm) |
| Khung chậm nhất màn 60 | 91 ms | 24 ms | cùng đợt |
| Tam giác màn 60 | 465 nghìn | 201 nghìn | cùng đợt |
| Xử lý mỗi khung (PlayerLoop) màn 150, ~250 bi | ~28 ms | ~7 ms | Đợt tối ưu thứ hai (bỏ CircleCast) |
| Khung chậm nhất màn 150 | 88 ms | ~33 ms | cùng đợt |
| Vật lý màn 60 | ~10 ms | ~3.4 ms | Khối Static thay Kinematic |
| Độ nhảy xương hộp lúc vào máy xay | 2.06 đơn vị | ≤ 0.094 đơn vị/khung | 16/10 |
| Độ trễ máy xay sau khi bấm | ~1 s | ~0.47 s | 16/10 |

Tất cả số trên đo **trong Editor** (quality Medium, auto-play), nên chỉ để so sánh trước/sau, chưa phải số trên điện thoại.

---

## 5. Vấn đề gặp phải và cách giải quyết

| Vấn đề | Nguyên nhân | Cách giải |
| --- | --- | --- |
| Material hồng / sai màu | Shader GLES không dịch ngược được | Tự viết tool rip GLSL + dịch máy sang HLSL |
| Màu vẫn lệch | Game gốc dùng Gamma + thông số ánh sáng riêng | Chép y nguyên từ scene gốc |
| UI tự dựng méo | Sprite `_mir` chỉ là nửa hình | Tách nguyên canvas UI gốc, khôi phục `MirroredImage` |
| Font mới bị vỡ chữ | Atlas SDF ghi sai định dạng (RGBA thay vì Alpha8) | Dựng ở font asset tạm, chép bảng glyph và ghi PNG đúng |
| Khe hở giữa khối gộp | Bù `cubeOverlap` sai | Quét 100 level bằng ảnh, chốt các giá trị về 0 |
| Máy xay giật | Lật đầu-đuôi ống, lò xo theo fps, khởi động nguội | Pipeline hàng chờ, bước cố định 1/120, prewarm |
| Panel thắng cháy sáng chỉ trên máy | Hạt cộng sáng phụ thuộc kích thước/độ phân giải | Thay bằng ảnh alpha-blend |
| Màn dày lag | Ngắm bi bằng CircleCast tốn nhất | Duyệt lưới ô, khối Static, tắt đổ bóng |
| Cheat không mở trên build | Cheat tự huỷ ở bản Release | Cần bật Development Build (đã hướng dẫn) |
| Sàn va chạm không theo Board Offset Y | Điểm collider nung sẵn trong scene | `SyncFloor` đồng bộ lúc vào game |
| Tool Unity Hub báo "Validation Failed" cho NDK/JDK | Lỗi Hub 3.11.1 | Cài qua Hub theo hướng dẫn |

---

## 6. Còn lại / việc tiếp theo

| Mục | Trạng thái |
| --- | --- |
| Xác nhận hiệu năng, độ mượt máy xay, panel thắng trên **máy Oppo** | Chờ build mới và báo FPS (dùng cheat → FPS overlay) |
| Rác bộ nhớ (GC) ~35–60 KB/khung | Chưa tách khỏi chi phí Editor; cần đo trên máy |
| Kiểm hành vi ngắm bi sau khi đổi sang duyệt lưới | Cần chơi thử bằng tay vài màn |
| Chưa làm các loại khối/cơ chế: keo, xích, khoá, bom, `acs`, `mdoor`, `ba`, cùng vài blocker bàn | Xem `Docs/kien-truc/co-che-asset-map.md` |
| Chưa làm: Home/coin/mạng, booster UI gốc đầy đủ | Danh sách trong memory dự án |
| Commit đợt 14–16/10 | **Chưa commit** — bạn tự commit checkpoint |
| Ý tưởng mechanic mới cho bản cập nhật | Đã liệt kê trong hội thoại (khối bom, dịch chuyển, nam châm, 3 sao, combo) |

---

## 7. Tài liệu liên quan trong repo

| Tài liệu | Nội dung |
| --- | --- |
| `Docs/devlog.md` | Nhật ký ngắn từng đợt, mới nhất ở trên |
| `Docs/lich-su/qua-trinh-khoi-phuc-va-core-gameplay.md` | Quá trình chi tiết khôi phục shader/UI và dựng core (40+ mục) |
| `Docs/kien-truc/ui.md` | Quy tắc kiến trúc UI |
| `ARCHITECTURE.md (mục 4b)`, `ARCHITECTURE.md` | Kiến trúc manager và cấu trúc dự án |
| `Docs/lich-su/tai-cau-truc.md` | Kế hoạch tái cấu trúc A–G |
| `Docs/kien-truc/co-che-asset-map.md` | Bản đồ cơ chế ↔ asset ↔ script |
| `Docs/thiet-ke/cheat-tool.md` | Thiết kế công cụ cheat |
| `Docs/lich-su/mau-sac/` | Các bản màu và nền đã so sánh |
