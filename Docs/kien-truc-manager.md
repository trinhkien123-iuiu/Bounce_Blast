# Kiến trúc MANAGER (scene Gameplay)

Trước đây một `GameManager` ôm hết: nạp level, bấm, bắn hộp, sức chứa, booster, thắng/thua, tự chơi… (~460 dòng). Nay chia theo **một manager = một việc**, mỗi cái là một object đặt sẵn dưới `Managers` trong Hierarchy.

## 1. Ai làm gì

| Manager | Việc | Không làm |
|---|---|---|
| `GameManager` | Dựng + nối các manager (`Awake`), giữ **trạng thái** `Loading → Playing → Won / Lost`, quyết định thắng / thua, thưởng xu, lưu level | Không đọc chuột, không bắn hộp, không dựng bàn |
| `LevelManager` | `LoadLevel / Restart / LoadNext`, dựng bàn + khay, cân camera, theme + nhạc, báo UI; ô `startLevel`, `continueFromSave` | Không biết luật thắng thua |
| `InputManager` | Đọc chuột / touch → "bấm hộp" hoặc "bấm khối (búa)" → ra lệnh cho Launch / Booster | Không chứa luật |
| `LaunchManager` | Bắn hộp, **sức chứa** (`Capacity`), Out Of Capacity, phát hiện hết nước đi | Không quyết định thua (chỉ báo) |
| `BoosterManager` | Bấm booster, booster lên cò, búa, đẩy số lượng lên UI | |
| `DebugManager` | `autoPlay`, lệnh cheat (chuột phải) – tắt khi build | |
| `AudioManager` | Âm thanh / nhạc (đặt sẵn trong scene, dữ liệu clip ở `GameConfig`) | |
| `CameraFitter`, `ThemeApplier`, `GameUI` | Camera / theme / giao diện | |

## 2. Giao tiếp: lệnh trực tiếp + sự kiện (Observer)

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

## 3. Thêm một manager mới
1. Tạo `Xxx : MonoBehaviour` trong `Scripts/Gameplay/Managers/`, có `Init(...)` nhận phụ thuộc, không `Find`.
2. Thêm ô `public Xxx xxxManager;` ở `GameManager`, tạo object dưới `Managers`, kéo vào ô; gọi `Init` trong `GameManager.Awake`.
3. Nếu cần biết trạng thái: thêm `SetPlaying`, gọi trong `GameManager.SetState`.
4. Ghi vào bảng ở mục 1 + `ARCHITECTURE.md`.

## 4. Đã kiểm tra (Play, console sạch)
Nạp level; tự chơi thắng (192 → 0 gạch); popup thắng → Continue → level kế; ép hết sức chứa → Out Of Capacity → Give Up → popup Thua → Retry nạp lại; bấm booster Hammer / Capacity (đúng số lượng, búa phá 1 khối); MenuGame → Play → Gameplay đúng level đã lưu.
Chưa kiểm bằng chuột thật (chỉ gọi hàm / click giả): đường bấm hộp qua `InputManager`.
