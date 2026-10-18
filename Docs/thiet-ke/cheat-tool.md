# Thiết kế Cheat Tool (test trên mobile) – ĐÃ LÀM MVP (nhóm A Level, B Resources, C Speed + FPS overlay)

## 1. Hiện trạng
- `DebugManager` (gắn GameManager): `autoPlay` + ContextMenu "Win now" – chỉ dùng được trong Editor (chuột phải Inspector), không có đường vào trên điện thoại.
- Dữ liệu người chơi chỉ có `PlayerData.Level`, `PlayerData.Coins` (PlayerPrefs).
- Điểm nối sẵn có: `LevelManager.LoadLevel/Restart/LoadNext`, `LaunchManager.Capacity` (`Increase`, `Infinite`, `Reset`), `BoosterManager.Boosters` (BoosterSystem), `BoardController.DestroyBlock/Blocks`, `GameManager.HandleWin/Lose`, `Time.timeScale`, `ConveyorSystem`/`BallManager` (số bi).
- Chưa có: nút/gesture mở menu cheat, màn chọn level, thống kê hiệu năng trên máy.

## 2. Nguyên tắc
1. Chỉ có trong bản **dev**: bọc `#if BB_CHEATS || UNITY_EDITOR || DEVELOPMENT_BUILD`; bản phát hành không chứa code. (Bật bằng Development Build hoặc Scripting Define `BB_CHEATS` – không cần sửa code khi build test.)
2. Tách **lõi cheat (không UI)** khỏi **panel UI** (đúng quy tắc kiến trúc UI của dự án: mỗi panel = prefab + 1 View, GameUI chỉ điều phối).
3. Mỗi cheat là một hàm nhỏ, có tên; panel chỉ liệt kê và gọi. Thêm cheat mới = thêm 1 mục.
4. Cheat không đổi luật chơi thật: chỉ gọi các API sẵn có của manager (không sửa trực tiếp trường private).

## 3. Cách mở (trên điện thoại)
- Chạm **5 lần liên tiếp vào chữ "Lv N"** (hoặc 3 ngón giữ 1 s) → mở panel. Có nút nhỏ "×" đóng. Panel nổi trên mọi popup, không chặn game khi đóng.
- Phụ: biểu tượng bánh răng nhỏ trong góc chỉ hiện ở bản dev (tuỳ chọn).

## 4. Danh sách tính năng (xếp theo độ ưu tiên)

### A. Điều khiển màn chơi (rất hay dùng)
| Cheat | Mô tả | Gọi vào |
|---|---|---|
| Nhảy tới level N | ô nhập số + nút −/+, nút Go | `PlayerData.Level` + `LevelManager.LoadLevel` |
| Level kế / trước / chơi lại | 3 nút | `LoadNext` / `Restart` |
| Thắng ngay | phá hết khối | `BoardController.DestroyBlock` (có sẵn trong DebugManager) |
| Thua ngay (Out of capacity / No moves) | thử màn thua | `GameManager.Lose` |
| Bỏ qua tới bảng thắng | test popup thắng/logo/pháo | `UiGame.PlayWinSequence` |

### B. Tài nguyên & booster
| Cheat | Mô tả |
|---|---|
| +1000 xu / đặt xu | `PlayerData.Coins` |
| Booster: +5 mỗi loại / vô hạn | `BoosterSystem` |
| Sức chứa vô hạn (toggle) | `Capacity.Infinite` |
| Tăng sức chứa +25% | `Capacity.Increase` |
| Xoá dữ liệu lưu (reset tiến trình) | `PlayerPrefs.DeleteAll` có hỏi xác nhận |

### C. Tốc độ & tự chơi
| Cheat | Mô tả |
|---|---|
| Tốc độ game ×0.25 / ×1 / ×2 / ×4 | `Time.timeScale` (để quan sát hiệu ứng chậm hoặc chạy nhanh) |
| Tạm dừng / bước 1 khung | soi lỗi hình |
| Auto-play (toggle) | dùng lại `DebugManager.autoPlay` |
| Chọn hộp đạn bất kỳ bấm được (bỏ qua băng/cửa/?) | test cơ chế |

### D. Test cơ chế & hiệu ứng (hay gặp lỗi nhất)
| Cheat | Mô tả |
|---|---|
| Cho ammo bay ngay N hộp | test hàng chờ máy xay |
| Nứt băng tất cả / mở cửa tất cả / lộ "?" tất cả | `CrackIce`, `ToggleDoor`, `Reveal` |
| Phá 1 hòm gỗ / 1 khối lớn | test VFX vỡ |
| Spawn N bi | test tải |
| Hiện popup: Thắng / Thua / Out Of Capacity / Settings | test UI |
| Đổi theme tạm: Normal / Hard / Super Hard | test màu nền/khung |

### E. Chẩn đoán trên máy thật (giúp bắt lag như vụ khựng ammo)
| Mục | Nội dung |
|---|---|
| Overlay FPS | fps hiện tại, trung bình 1 s, khung chậm nhất (ms) |
| Đếm khung chậm | số khung > 33 ms trong 10 s gần nhất |
| Số liệu | số khối, bi đang bay, rigidbody ngủ/thức, bộ nhớ (MB) |
| Log trên màn hình | hiện 20 dòng `Debug.Log/Warning/Error` gần nhất (đỡ phải cắm adb) |
| Chụp màn hình | lưu PNG vào thư mục ứng dụng + hiện đường dẫn |
| Sao chép "báo cáo lỗi" | thiết bị, fps, level, log cuối → clipboard |

### F. Tuỳ chọn hiển thị
- Bật/tắt: khung debug collider, đường ngắm bi, viền hộp; chọn target 30/60 fps; bật/tắt VFX nặng; bật/tắt bóng/hiệu ứng để so lag.

## 5. Kiến trúc đề xuất
```
Scripts/Debug/                      (asmdef riêng, #if BB_CHEATS…)
  CheatService.cs        – lõi: danh sách Cheat {id, nhóm, tên, Action/Func}, trạng thái toggle
  CheatRegistry.cs       – đăng ký các cheat A–F (mỗi nhóm một hàm Register…)
  PerfOverlay.cs         – FPS/khung chậm/bộ nhớ (E)
  OnScreenLog.cs         – bắt Application.logMessageReceived
  CheatPanelView.cs      – View của prefab CheatPanel (tab theo nhóm, danh sách nút/toggle/ô số)
  CheatGesture.cs        – nhận 5 lần chạm / 3 ngón để mở panel
Prefabs/UI/CheatPanel.prefab – dựng sẵn trong Hierarchy (đúng quy tắc UI của dự án)
```
- Panel dùng scroll + tab (Level · Tài nguyên · Tốc độ · Cơ chế · Hiệu năng); nút to để bấm bằng ngón tay.
- Trạng thái toggle (autoplay, infinite capacity, overlay) lưu `PlayerPrefs` riêng `cheat_*` để giữ qua lần mở; có nút "Reset cheat".

## 6. Giai đoạn làm
1. **MVP** (≈ nửa buổi): gesture + panel + nhóm A, B, C + FPS overlay.
2. Nhóm D (cơ chế) + OnScreenLog + chụp màn hình.
3. Nhóm E đầy đủ + báo cáo lỗi + nhóm F.

## 7. Rủi ro / lưu ý
- Quên tắt cheat khi phát hành → dùng define + kiểm tra trong script build (cảnh báo nếu `BB_CHEATS` bật ở bản Release).
- `Time.timeScale` ≠ 1 ảnh hưởng popup dùng thời gian thật (đã dùng `unscaledTime` ở nhiều chỗ) → reset về 1 khi đóng panel.
- Cheat "thắng ngay" bỏ qua điều kiện thắng thật; đừng dùng để kết luận về cân bằng level.
