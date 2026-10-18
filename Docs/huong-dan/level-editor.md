# Hướng dẫn dùng Level Editor

> Công cụ làm level cho bộ **custom** (bộ 50 level mới). Không đụng 550 level gốc.
> Áp dụng cho **giai đoạn 1** (2026-10-19): chưa có nhập ảnh PNG và tự sinh khay.
> Thiết kế / lý do kỹ thuật: `../thiet-ke/level-editor.md`.

---

## 1. Mở công cụ

- Menu **Tools ▸ Bounce Blast ▸ Level Editor ▸ Mở Level Editor**, hoặc phím **Ctrl + Shift + L**.
- Cửa sổ có 3 cột:

| Cột | Nội dung |
| --- | --- |
| **Trái** | Danh sách level của bộ custom, nút tạo / nhân bản / xoá, nhập level gốc |
| **Giữa** | Hai tab **Bàn gạch** và **Khay đạn** để vẽ |
| **Phải** | Thông số level, kích thước, **bảng cân bi**, **lỗi / cảnh báo**, nút **Chơi thử** và **Lưu** |

---

## 2. Quy trình làm một level

1. **Tạo level**: bấm **+ Level mới** (bàn 10×16 ô thiết kế, Multiplier 2, khay 5×4 ô trống).
   Hoặc **Nhân bản** một level đang có, hoặc **Nhập** một level gốc làm mẫu (gõ số 1–550 rồi bấm Nhập).
2. **Đặt tên, chương, độ khó** ở cột phải.
3. **Vẽ bàn gạch** (tab Bàn gạch, mục 3).
4. **Xếp khay đạn** (tab Khay đạn, mục 4) cho tới khi **bảng cân bi toàn ✔**.
5. **Chỉnh capacity** cho tới khi mục **Lỗi / cảnh báo** không còn lỗi đỏ.
6. **▶ Chơi thử (F5)** → chơi, chỉnh, lặp lại.
7. **Lưu (Ctrl+S)**. Thứ tự level trong bộ chỉnh bằng nút ▲ ▼ ở cột trái.

---

## 3. Tab "Bàn gạch"

### Công cụ

| Công cụ | Phím | Cách dùng |
| --- | --- | --- |
| **Cọ** | B | Chọn màu ở bảng màu, đặt **Máu**, rồi bấm / kéo chuột trên lưới |
| **Tô vùng** | G | Bấm một ô: mọi ô liền kề giống ô đó đổi sang màu đang chọn |
| **Tẩy** | E | Bấm / kéo để xoá về ô trống |
| **Hút màu** | I | Bấm một gạch để lấy màu + máu của nó, rồi tự chuyển về Cọ |
| **Chọn** | S | Kéo chuột chọn vùng chữ nhật, rồi dùng các nút bên dưới |

### Thao tác trên vùng chọn (công cụ Chọn)

| Nút | Tác dụng | Ô số bên cạnh |
| --- | --- | --- |
| **Gộp khối** | Các gạch trong vùng thành **một khối gộp** dùng chung máu (một màu – lấy màu ô đầu) | Máu khối |
| **Tách khối** | Bỏ gộp, trở lại gạch lẻ | |
| **Thêm hòm** | Phủ **hòm gỗ** lên các gạch trong vùng (hòm vỡ thì lộ gạch bên dưới) | Máu hòm |
| **Bỏ hòm** | Gỡ hòm | |
| **Cột nhà** | Biến vùng thành **cột nhà** (bi màu nào cũng trừ máu, không tính vào thắng) | Máu cột |
| **Tô màu** / **Xoá** | Tô cả vùng bằng màu đang chọn / xoá cả vùng | |

### Cách đọc lưới

- Ô tối = trống. Số trên gạch = máu (khi > 1).
- Khối gộp có **viền đậm** quanh khối; số máu hiện ở góc trên-trái khối.
- Dải nâu ở nửa trên ô = có **hòm gỗ** phủ. Ô màu kem chữ **C** = cột nhà. Ô đen dấu **✕** = vật cản chưa hỗ trợ (game coi là tường).
- Dòng chữ nhỏ trên lưới cho biết **mã** của ô dưới chuột (đúng như trong CSV).

### Lưu ý về con số

- Lưới là **ô thiết kế**. Trong game mỗi ô thành **Multiplier × Multiplier** gạch (Multiplier 2 → 1 ô = 4 gạch).
- Máu **khối gộp / hòm / cột** và **số bi** trong khay được game **nhân Multiplier²**. Gạch lẻ máu 1 thì mỗi gạch nhỏ máu 1.
- Cột phải ghi "Bàn trong game: 20×32 gạch" để biết kích thước thật.

---

## 4. Tab "Khay đạn"

### Công cụ

| Công cụ | Cách dùng |
| --- | --- |
| **Hộp** | Chọn màu, **Số bi**, Băng (0 = không), Hộp "?", Cửa (Không / Đóng / Mở) → bấm ô để đặt hộp |
| **Ô trống** | Ô không có hộp (đường đi để các hộp phía sau thông ra) |
| **Tường** / **Ngoài khay** | Ô không dùng (không vẽ khay ở đó) |
| **Tunnel** | Đặt **Id** (A, B…) và **hướng nhả** rồi bấm ô. Hàng chờ hộp của tunnel sửa ở cột phải |
| **Sửa ô** | Bấm một ô để sửa chi tiết ở cột phải (đổi loại ô, màu, số bi, băng, "?", cửa) |

### Cách đọc lưới

- **Hàng trên cùng = hàng 0 = sát lối ra** (hộp ở đây bấm được trước).
- Số trên hộp = **số bi thật trong game** (đã nhân Multiplier²); thanh công cụ có ghi "= … bi trong game".
- **Viền xanh lá** quanh hộp = hộp **bấm được ngay** lúc bắt đầu (theo đúng luật thông đường của game).
- Viền xanh nhạt + ❄N = hộp bị **băng** N lớp. "?" = hộp bí ẩn. ▣ = cửa đóng, □ = cửa mở lúc đầu.
- Ô tunnel ghi `TA↓ / 3 hộp` = tunnel A, nhả xuống dưới, còn 3 hộp trong hàng chờ.

### Luật cần nhớ (game kiểm giúp)

- **Tổng bi mỗi màu = tổng máu gạch màu đó** (tính cả khối gộp, gạch dưới hòm; **không** tính hòm và cột nhà).
- Phải có ít nhất **một hộp bấm được** lúc bắt đầu.
- Không hộp nào **lớn hơn capacity**.

---

## 5. Cột phải: kiểm tra

- **Cân bi theo màu**: mỗi dòng `màu · máu · bi · ✔ / +N / −N`. Phải toàn ✔ thì mới thắng được (−N = thiếu bi, +N = thừa bi).
- **Lỗi** (đỏ) – phải sửa: thiếu/thừa bi, mã màu không có trong bảng màu, không hộp nào bấm được, hộp > capacity, tunnel nhả ra ngoài khay…
- **Cảnh báo** (vàng) – nên xem: capacity < 2 × hộp lớn nhất (rất dễ thua), capacity ≥ tổng bi (không có áp lực), quá 7 màu…
- Dòng tóm tắt: tổng bi · hộp lớn nhất · số hộp bấm được lúc đầu.

---

## 6. Chơi thử

- **▶ Chơi thử** hoặc **F5**: tự lưu → mở scene Gameplay → chơi đúng level đang sửa. Level còn lỗi thì hỏi lại trước.
- Trong lúc chơi dùng **cheat** (F1): tốc độ ×4, auto-play, WIN/LOSE để thử nhanh.
- Restart / thắng rồi "Next" đều chơi lại level đang thử. Dừng Play để quay lại sửa.

## 7. Chơi cả bộ custom (trong game / build lên máy)

1. Mở `Assets/_Project/Data/GameConfig.asset` → **Level Source = Custom**.
2. Bộ custom chơi theo thứ tự trong danh sách Level Editor; tiến độ lưu **riêng**, không ghi đè tiến độ bộ gốc.
3. Đổi lại **Original** để về 550 level gốc.

---

## 8. Phím tắt

| Phím | Việc |
| --- | --- |
| Ctrl + Shift + L | Mở Level Editor |
| B / G / E / I / S | Cọ / Tô vùng / Tẩy / Hút màu / Chọn (tab Bàn gạch) |
| Ctrl + Z / Ctrl + Y | Hoàn tác / làm lại (mọi thao tác vẽ đều hoàn tác được) |
| Ctrl + S | Lưu |
| F5 | Chơi thử |

## 9. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
| --- | --- |
| "Màu 'x': thiếu N bi" | Thêm hộp màu đó / tăng số bi, hoặc bớt gạch màu đó. Nhớ số bi hiển thị đã nhân Multiplier² |
| "Không có hộp nào bấm được" | Đặt hộp ở **hàng trên cùng**, hoặc mở đường bằng **Ô trống** nối từ hàng trên xuống; kiểm tra băng / cửa đóng |
| "Có hộp N bi > capacity" | Tăng capacity hoặc chia hộp đó thành nhiều hộp nhỏ |
| Hộp không có viền xanh dù nằm hàng trên | Hộp đang ở hàng 0 nhưng bị băng / cửa đóng – vẫn bấm được sau khi băng vỡ / cửa mở |
| Khối gộp tô lệch màu | Khối gộp chỉ có một màu: dùng Chọn → Tô màu cả khối, rồi Gộp khối lại |
| Muốn xem level gốc làm thế nào | Cột trái → Nhập số level gốc → xem / chơi thử, xoá khi xong |

## 10. Kiểm tra sau khi sửa code level (cho lập trình viên)

- Menu **Tools ▸ Bounce Blast ▸ Level Editor ▸ Kiểm tra khứ hồi 550 level gốc**: phải ra "550 khớp, 0 lệch".
  Chạy lại mỗi khi sửa `LevelParser`, `LevelData`, `LevelTokens`.
