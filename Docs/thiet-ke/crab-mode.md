# Crab Mode — thiết kế (bản 2, 19/11)

Nguồn tham chiếu:
- Ảnh mẫu bố cục (tranh pixel trên cát, cua bò quanh tranh, hàng chờ 5 ô "WAITING 4/5" bên dưới).
- Demo web đã chạy: `D:\GitHub\IdeaGame\crab-chomp` (luật nằm ở `src/core.js`; đây là **bản mẫu luật** để port sang Unity).
- Video `Asset_MainGame_Bounce Blast/ý tưởng game mới.mp4` (khung hình ở `frames/y_tuong_game_moi`).

Ý chính: giữ **cơ chế khay ammo của Bounce Blast** (bấm hộp có đường ra theo BFS, tunnel, băng, cửa…), nhưng **bàn chơi đổi hẳn**:
không còn máng băng chuyền, không còn tường/arena bao quanh, không còn bi. Thay bằng **5 máy xay xếp hàng làm hàng chờ** và **cua** bò ra ăn block.

## 1. Vòng chơi

```
Bấm ammo (có đường ra)  ─►  còn máy xay trống? ─không─► không nhận (rung); cả 5 bận + không ai ăn được = thua
        │ có
        ▼
ammo bay vào máy trống đầu tiên (máy KHÔNG gắn màu nào: ammo màu gì cũng vào được, chỉ cần còn slot trống);
        ammo ngồi trong máy giữ nguyên màu + số cua còn lại của nó, đèn máy ĐỎ (đang chiếm)
        ▼
máy chỉ "kích hoạt" khi có block CÙNG MÀU đang có đường ăn (xem mục 2)
   - chưa có đường ăn: máy chờ, hiện đồng hồ cát
   - có: máy xoay, nhả 1 con cua, số trên máy giảm 1, rồi nhả tiếp theo nhịp
        ▼
cua chạy theo đường BFS tới ô kề block mục tiêu, cạp (CHOMP) → block vỡ → cua biến mất theo
        ▼
máy nhả hết số cua → máy TRỐNG lại (đèn XANH) ngay, cua đang chạy vẫn tiếp tục
```

Nhãn trên hàng chờ: `WAITING n/5` (n = số máy đang bận).

## 2. "Có đường ăn" (khác bản 1)
Bàn không còn tường. Quanh tranh là cát trống, nên dùng **flood fill 4 hướng từ viền ngoài** qua các ô trống
(giống `computeReach` trong demo):
- Ô trống nối được ra viền ngoài = "đường đi được".
- Block **ăn được** = có ít nhất 1 ô kề (4 hướng) là ô đi được.
- Block bị ăn đi thì flood fill chạy lại, lộ thêm block bên trong → ăn từ ngoài vào trong.
- Cua đi đường BFS ngắn nhất trên các ô đi được, tới ô kề block rồi cạp.

Thay thế `BoardGrid.IsExposed` (hở mặt trên/hai bên) trong mode này. `IsExposed` vẫn giữ cho mode cũ.

## 3. Luật chốt

| Mục | Quyết định |
|---|---|
| Số máy xay | 5, xếp 1 hàng, mỗi máy là slot trống không gắn màu, có đèn tín hiệu xanh/đỏ; ammo đang ngồi trong máy hiện màu + số cua còn lại của chính nó |
| Số cua của 1 ammo | bằng số đạn của ammo (`AmmoBoxData.Balls`), như cách sinh bi trước đây; ammo đôi (`Next`) sinh tiếp phần sau |
| 1 cua | = 1 hit lên 1 block cùng màu; cạp xong cua biến mất cùng block |
| Máy trống lại khi | ammo **nhả hết cua** (không chờ cua cạp xong) |
| Ammo nào vào máy nào | không hạn chế: ammo màu bất kỳ vào máy trống đầu tiên; điều kiện duy nhất là còn slot trống |
| Chưa có đường ăn | máy đứng chờ, vẫn bị chiếm, hiện đồng hồ cát |
| Hai ammo cùng màu | KHÔNG đợi nhau (đổi 19/11): mọi máy xay có block cùng màu đang có đường ăn đều xay ngay, song song |
| Trần số cua chạy cùng lúc | `MAX_CRABS` (chỉnh trong GameConfig) |
| Chọn mục tiêu | block cùng màu có đường ăn **gần nhất** theo đường BFS; block đã được cua khác đặt trước thì bỏ qua |

Puzzle: chọn ammo có màu đang có đường ăn để máy không bị kẹt, đồng thời mở đường cho các màu phía sau.

## 4. Thắng / thua
- **Thắng:** hết block cần phá (như `DestructibleCount == 0` của Bounce Blast).
- **Thua "Queue full":** cả 5 máy bận, không máy nào nhả được cua (không block cùng màu có đường ăn), và không còn cua nào đang chạy.
- **Thua "No more moves":** hàng chờ chưa đầy nhưng khay không còn hộp bấm được, không còn cua chạy, không máy nào nhả được.
- Bỏ `CapacitySystem` và popup Out Of Capacity trong mode này; HUD thay thanh sức chứa bằng nhãn `WAITING n/5`.

## 5. Điều đã đổi so với bản 1
- Bỏ khái niệm "bàn có tường, khối rơi": tranh đứng yên, dùng flood fill thay cho `IsExposed`.
- Cua spawn ở máy xay rồi bò ra quanh tranh (không phải đi trong lòng bàn có tường).
- Máy hiện số cua còn lại và đồng hồ cát khi chờ; cua biến mất sau khi cạp.
- Hai ammo cùng màu có thứ tự ưu tiên.

## 6. Phần code Unity
**Giữ nguyên:** `AmmoGridController` (+ `AmmoBox`, `AmmoPathRules`, tunnel, băng, cửa…), `LevelParser`, `LevelManager`, `BoardBlock` (hit/hp), `BoardController` (dựng khối, báo thắng), UI popup.

**Mới (`Scripts/Gameplay/Crabs/`, namespace `BounceBlast.Gameplay`):**
- `GrinderSlot` — một máy xay (không gắn màu): trạng thái (Empty / Flying / Waiting / Spawning), ammo đang chiếm (màu, số còn lại), đèn xanh/đỏ, đồng hồ cát.
- `GrinderQueue` — 5 `GrinderSlot` đặt sẵn trong Hierarchy; `TryReserve()`; bộ lập lịch nhả cua theo nhịp + ưu tiên cùng màu; sự kiện `OnQueueFull`.
- `BoardReach` — flood fill từ viền, BFS đường đi, tìm block mục tiêu gần nhất (port từ `computeReach`/`bfs`/`findTargetOn` của demo), cache theo `BoardGrid.Version`.
- `Crab`, `CrabManager` — pool cua, trạng thái hatch → walk → chomp → poof; hủy đặt trước nếu đường hỏng.

**Sửa:**
- `LaunchManager.TryLaunch`: dùng `GrinderQueue.TryReserve` thay kiểm tra Capacity; `CheckDeadlock` theo mục 4.
- `GameManager`: gắn `GrinderQueue`/`CrabManager`, bỏ `ConveyorSystem`/`BallManager`/`ArenaWalls` khi `GameConfig.crabMode` bật.
- Camera/nền: bỏ tường & băng chuyền, nền cát (prefab/asset đặt sẵn trong Hierarchy).

**Theo quy tắc "cảnh phải chỉnh tay được":** 5 máy xay là prefab đặt sẵn trong scene (tái dùng mesh `Crusher` gốc), kèm đèn tín hiệu; vị trí, khoảng cách chỉnh trong Hierarchy. Các số (tốc độ cua, nhịp nhả, `MAX_CRABS`, thời gian cạp) nằm trong `GameConfig`.

## 7. Thứ tự làm
1. Prefab 5 máy xay + `GrinderQueue`: bấm ammo bay vào máy trống, đèn đổi màu, nhãn `WAITING n/5`. Chưa có cua.
2. `BoardReach`: flood fill + hiển thị block ăn được (debug gizmo).
3. `Crab` + `CrabManager` (hình cua tạm), nhả cua theo mục tiêu, cạp, block vỡ.
4. Luật ưu tiên cùng màu, đồng hồ cát, điều kiện thua, HUD.
5. Bỏ tường + băng chuyền + block rơi + thanh CAPACITY (ĐÃ XONG 19/11: `GameManager.hideInCrabMode` liệt kê object ẩn, chỉnh trong Inspector). **Không làm nền cát**: giữ giao diện/mesh Unity hiện tại theo yêu cầu.
6. Rà level: chạy solver trên level Unity, so với `tools/eval.js` của demo.

## 8. Đã chốt (19/11)
1. Bản đầu **chỉ block thường, không rơi**; cột, hòm, rèm, bom, tunnel bàn, "?" làm sau.
2. Block nhiều máu (`Hp > 1`) cho phép: cần nhiều cua, đặt trước theo số hit còn lại.
3. Cua dựng tạm bằng hình khối; thay model thật sau.
4. Booster bỏ khỏi mode này ở bản đầu.
