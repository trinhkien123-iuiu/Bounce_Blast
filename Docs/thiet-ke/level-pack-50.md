# Bộ 50 level mới + mechanic mới — thiết kế

> Mục tiêu: một bộ level **riêng** (không đụng 550 level gốc) với hình ảnh mới và mechanic mới, đủ để trình diễn sự sáng tạo.
> Trạng thái: **bản nháp chờ duyệt**. Mục "Cần chốt" ở cuối mỗi mechanic là chỗ cần quyết trước khi code.
> Ngày: 2026-10-19.

---

## 1. Cấu trúc bộ level

- **50 level = 5 chương × 10 màn.** Mỗi chương một chủ đề hình + một mechanic mới; màn 10 của mỗi chương là màn "boss" kết hợp những gì đã học.
- Lưu ở bộ riêng `Assets/_Project/Data/Levels/custom/` (cùng định dạng CSV gốc), chọn bộ level trong `GameConfig`/cheat để chạy.
- Màn đầu tiên có mechanic mới phải **dễ** (dạy cách chơi), độ khó tăng dần trong chương, nghỉ nhẹ ở màn đầu chương sau.

| Chương | Màn | Chủ đề hình (gợi ý, chưa chốt) | Mechanic mới | Booster mở khoá |
| --- | --- | --- | --- | --- |
| 1 | 1–10 | Trái cây / đồ ăn | Cơ bản + **gạch nhún đàn hồi** (M4) | — |
| 2 | 11–20 | Động vật | **Ammo 2 màu** (M8) | **Đóng băng Capacity** (M5) |
| 3 | 21–30 | Biển | **Ammo nhiễm độc** (M1) | — |
| 4 | 31–40 | Vũ trụ | **Gạch mù + Ammo kính lúp** (M2) | — |
| 5 | 41–50 | Lễ hội / Việt Nam | **Ammo pháo** (M3) + kết hợp tất cả | — |

Để sau: booster laser (M6), booster bi cầu vồng (M7), nền menu động (M9), linh vật gợi ý khi rảnh (M10).

---

## 2. Mechanic đã chốt hướng

### M1 — Ammo nhiễm độc (khay)

**Luật**
- Hộp nhiễm độc có lớp phủ chất độc (axit xanh sủi bọt), giống cách băng / hộp "?" phủ lên hộp.
- Mỗi hộp nhiễm có **bộ đếm N lượt bắn** (N đặt theo level, ví dụ 3). Mỗi lần người chơi bắn một hộp bất kỳ mà hộp nhiễm **chưa được bắn**, bộ đếm giảm 1. Về 0 → **lây** sang hộp kề (trên/dưới/trái/phải), rồi bộ đếm đặt lại N.
- Các hộp nhiễm nối liền nhau là một **cụm**. Bấm vào một hộp nhiễm (đang thông đường) → **cả cụm cùng bay lên máng** (mỗi hộp vẫn nhả đúng màu, số bi của mình).
- Nếu tổng bi của cả cụm vượt sức chứa còn lại → **hết sức chứa = thua** (dùng luồng Out Of Capacity sẵn có).
- Ý nghĩa chiến thuật: xử lý ổ độc **sớm** khi còn nhỏ, hoặc để lớn rồi dọn một lần nếu capacity đủ.

**Hình / VFX**: lớp axit trên nắp hộp, bọt sủi, khi sắp lây (bộ đếm = 1) thì nhấp nháy cảnh báo; khi lây có giọt độc bắn sang hộp bên cạnh.

**Code**: blocker mới của khay (tách lớp riêng như `AmmoDoorView`/`IceBlockerView`), luật đếm + lây trong `AmmoGridController.Launch`; bắn cả cụm = gọi `Launch` cho từng hộp của cụm (BFS), kiểm capacity tổng trước. Token CSV đề xuất: `màu-sốbi/tox-N`.

**Cần chốt**
- a) Lây sang **1 hộp kề ngẫu nhiên** hay **mọi hộp kề**? *(đề xuất: 1 hộp ngẫu nhiên — mọi hộp kề sẽ bùng nổ quá nhanh)*
- b) Hộp đang có băng / cửa đóng có bị lây không? *(đề xuất: không — băng/cửa "bảo vệ")*
- c) Trong cụm có hộp chưa thông đường thì có bay theo cụm không? *(đề xuất: có — đó là cái giá của cụm độc)*

### M2 — Gạch mù + Ammo kính lúp (bàn + khay)

**Luật**
- **Gạch mù** trên bàn: phủ màu xám có dấu "?". Bi **không trừ máu** được (nảy ra như tường) và bi **không ngắm** tới gạch mù.
- **Ammo kính lúp**: hộp đặc biệt trong khay (biểu tượng kính lúp, không mang bi hoặc mang ít bi). Người chơi phải **tìm đường** cho hộp này thông ra (luật thông đường thường). Khi hộp kính lúp vào máy xay → kính lúp quét bàn, **lộ màu** gạch mù → từ đó gạch mù thành gạch thường.
- Ý nghĩa: mục tiêu phụ trong khay — phải dọn đường tới hộp kính lúp đúng lúc, nếu không bàn sẽ kẹt các vùng mù.

**Code**: loại khối mới `MysteryKind` trong `Board/Kinds` (luật: không nhận bi, không là mục tiêu, có hàm `Reveal`); hộp kính lúp là kiểu hộp mới; khi đậu máy xay gọi `board.RevealMystery(...)`.

**Cần chốt**
- a) Một kính lúp lộ **toàn bộ** gạch mù hay **một vùng** (ví dụ 1 nhóm / 1 hàng)? *(đề xuất: một nhóm theo id, để level có thể có 2–3 kính lúp)*
- b) Hộp kính lúp có mang bi không? *(đề xuất: không — để không ảnh hưởng cân bi)*
- c) Khi chưa có kính lúp mà chỉ còn gạch mù → coi là bế tắc (thua) như hiện tại?

### M3 — Ammo pháo (khay)

**Phương án A — Pháo nạp hộp** *(đề xuất dùng)*
- Ô pháo giữ một hàng chờ hộp (như tunnel). **Cứ mỗi 2 lượt bắn** của người chơi, pháo bắn 1 hộp tiếp theo vào **một ô trống ngẫu nhiên** trong khay (hộp bay vòng cung, rơi vào ô).
- Hộp trong pháo tính vào tổng bi của level ⇒ luật "tổng bi = tổng máu" vẫn đúng.
- Hết ô trống thì pháo chờ.

**Phương án B — Pháo băng**
- Cứ **20 giây** bắn băng vào một hộp ngẫu nhiên (hộp bị thêm 1 lớp băng).
- Lưu ý: đây là áp lực **thời gian**, khác hẳn kiểu câu đố theo lượt của game (người chơi đang suy nghĩ cũng bị phạt). Nếu dùng, đề xuất đổi thành **theo lượt** (mỗi 3 lượt bắn) cho nhất quán.

**Code**: mở rộng từ `AmmoTunnel` (hàng chờ hộp) + hoạt ảnh bay trong khay. Token CSV: `/can-<id>` + danh sách hộp như `TunnelProps_v3`.

**Cần chốt**: dùng A, B, hay cả hai (A ở chương 5, B làm biến thể màn boss)? Ô đích "ngẫu nhiên" có cần cố định theo seed để level luôn chơi được như lúc thiết kế không? *(đề xuất: có seed — để bot kiểm thử được)*

### M4 — Gạch nhún đàn hồi khi bi chạm sai màu (bàn, cảm giác)

- Bi chạm gạch **khác màu** (không trừ máu) → gạch **nhún đàn hồi** nhẹ (co rồi bật như thạch), kèm âm "bộp".
- Hiện chỉ có âm thanh; hình nảy (`HitPunch`) chỉ chạy khi gạch bị trừ máu.
- Lưu ý hiệu năng: màn dày có hàng trăm va chạm mỗi giây → nhún chỉ đổi scale của gạch bị chạm (đã có cơ chế "đánh thức – ngủ" của `BoardBlock`), giới hạn số gạch nhún cùng lúc.

**Cần chốt**: chỉ gạch bị chạm nhún, hay lan nhẹ sang gạch kề? *(đề xuất: chỉ gạch bị chạm)*

### M5 — Booster thứ 5: Đóng băng Capacity

- Bấm → thanh capacity **đóng băng 10 giây**: lớp băng phủ thanh, **nứt dần** theo thời gian, nứt hết thì vỡ ra và thanh trở lại bình thường.
- Trong lúc đóng băng, hộp bắn lên **không cộng vào capacity** (bắn thoải mái).

**Cần chốt**: bi đã bắn trong 10 giây đó **có tính vào capacity sau khi tan băng** không?
*(đề xuất: không — bi bắn trong lúc đóng băng là "miễn phí". Nếu có tính, người chơi dễ thua ngay khi băng tan vì vượt sức chứa.)*

**Code**: `BoosterType` thêm `FreezeCapacity`; `CapacitySystem` thêm trạng thái đóng băng (bỏ qua cộng); `CapacityBarView` thêm lớp băng + 3–4 mức nứt (có thể dùng lại art băng của hộp đạn); nút booster thứ 5 trên `BoosterBar` prefab.

### M8 — Ammo 2 màu (khay)

- Hộp mang **2 màu**, tự đổi màu theo nhịp (ví dụ mỗi 1,5 s, có hiệu ứng chuyển). Bấm lúc đang màu nào → **toàn bộ bi** của hộp là màu đó.
- Cần căn thời điểm: đây là mechanic **có yếu tố phản xạ** đầu tiên của game.

**Vấn đề cân bi (quan trọng)**: luật "tổng bi mỗi màu = tổng máu màu đó" không còn cố định, vì kết quả phụ thuộc người chơi chọn màu nào. Đề xuất:
- Level có hộp 2 màu (A/B) thì **gạch hai màu A và B có dư một lượng = số bi của hộp đó ở cả hai màu**, cụ thể: thêm một khối "chung" có thể phá bằng A hoặc B; hoặc
- Đơn giản hơn: level cấp **dư bi** cho A và B một lượng bằng hộp 2 màu (chọn sai thì vẫn thắng được nhưng tốn capacity hơn).

**Cần chốt**: chu kỳ đổi màu bao lâu? Chọn cách cân bi nào? *(đề xuất: cách 2 — dư bi, phạt bằng capacity chứ không làm level hết đường thắng)*

**Code**: kiểu hộp mới (tách lớp hình riêng), màu hiện tại quyết định `Data.ColorCode` lúc `Launch`. Token CSV: `màuA+màuB-sốbi`.

---

## 3. Để sau (đã ghi nhận)

| # | Ý tưởng | Ghi chú |
| --- | --- | --- |
| M6 | Booster laser phá một đường gạch | Chờ ý tưởng cơ chế |
| M7 | Booster biến một Ammo thành bi cầu vồng | Chờ ý tưởng cơ chế |
| M9 | Nền MenuGame động | Tính sau |
| M10 | Linh vật cầm búa gõ Ammo khi người chơi nghĩ lâu | Tính sau. Đây là "gợi ý khi rảnh" (idle hint); "LiveOps" thường chỉ sự kiện theo thời gian |

---

## 4. Công cụ cần làm trước khi làm level

1. **Level Editor (Unity):** xem trước bàn + khay; **nhập ảnh PNG pixel art → tự sinh bàn gạch** (khớp màu gần nhất trong bảng màu); **tự sinh khay** từ tổng máu từng màu; kéo thả đặt blocker (băng, cửa, "?", tunnel, nhiễm độc, kính lúp, pháo, 2 màu); lưu ra CSV vào `Levels/custom/`.
2. **Kiểm tra tự động:** cân bi/máu (tính cả pháo, 2 màu, kính lúp), báo level không thắng được.
3. **Bot chơi thử:** chạy auto-play nhiều lần mỗi màn → tỉ lệ thắng, số lần sát tràn capacity → xếp độ khó.
4. **Chọn bộ level** trong cheat ("Go to level" chạy được bộ `custom`).

## 5. Thứ tự làm

| Đợt | Việc | Kết quả để demo |
| --- | --- | --- |
| 1 | Level Editor + nhập ảnh + tự sinh khay + kiểm tra | Làm được level mới trong vài phút |
| 2 | M4 nhún + chương 1 (10 màn) | 10 màn hình mới chơi được |
| 3 | M5 booster đóng băng + M8 ammo 2 màu + chương 2 | 20 màn |
| 4 | M1 nhiễm độc + chương 3 | 30 màn |
| 5 | M2 gạch mù + kính lúp + chương 4 | 40 màn |
| 6 | M3 pháo + chương 5 + bot đo độ khó, xếp lại | 50 màn |

Mỗi mechanic khay mới là **một lớp riêng** (như `AmmoDoorView`, `IceBlockerView`); mỗi loại khối bàn mới là **một lớp trong `Board/Kinds`** (`Docs/kien-truc/them-loai-khoi.md`) — không phình lại `AmmoBox` / `BoardController`.
