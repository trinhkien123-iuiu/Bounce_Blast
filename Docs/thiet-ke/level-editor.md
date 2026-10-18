# Thiết kế Level Editor (công cụ xếp level) — GIAI ĐOẠN 1 ĐÃ LÀM (2026-10-19)

> **Hướng dẫn sử dụng đầy đủ: `../huong-dan/level-editor.md`.** Tóm tắt:
> **Mở:** `Tools ▸ Bounce Blast ▸ Level Editor ▸ Mở Level Editor` (Ctrl+Shift+L).
> 1. Cột trái: **+ Level mới** (hoặc **Nhập** số level gốc để làm mẫu).
> 2. Tab **Bàn gạch**: chọn màu trong bảng, cọ (B) / tô vùng (G) / tẩy (E) / hút màu (I); **Chọn (S)** kéo vùng → Gộp khối / Thêm hòm / Cột nhà (ô số cạnh nút = máu).
> 3. Tab **Khay đạn**: chọn màu + số bi → bấm ô đặt hộp; Ô trống / Tường / Ngoài khay / Tunnel; **Sửa ô** để chỉnh băng, "?", cửa. Viền xanh = hộp bấm được ngay.
> 4. Cột phải: capacity, multiplier, kích thước; **bảng cân bi** phải toàn ✔, mục **Lỗi** phải trống.
> 5. **▶ Chơi thử (F5)**: mở Gameplay chơi đúng level đang sửa (dùng được cheat ×4, auto-play).
> 6. Muốn chơi cả bộ trong game / trên máy: `GameConfig ▸ Level Source = Custom` (tiến độ lưu riêng, không đụng bộ gốc).
>
> Đã kiểm: khứ hồi 550 level gốc (CSV → LevelData → game) **khớp 100%** (menu `Level Editor ▸ Kiểm tra khứ hồi 550 level gốc`);
> nhập level 12 chơi giống hệt bản CSV; level làm mới từ đầu chơi tới thắng.
> Chưa làm: giai đoạn 2 (nhập ảnh, tự sinh khay), giai đoạn 3 (bot đo độ khó).

> Mục tiêu: làm được **một level mới trong vài phút** (vẽ / nhập ảnh → khay tự sinh → chỉnh → chơi thử), không gõ tay CSV, không bao giờ sai luật cân bi.
> Phục vụ bộ 50 level ở `level-pack-50.md`. Ngày: 2026-10-19.

---

## 1. Phạm vi

**Có trong tool**
- Tạo / mở / nhân bản / xoá level của bộ level **riêng** (`custom`); không sửa 550 level gốc (chỉ **nhập** để tham khảo hoặc làm bản sao).
- Vẽ bàn gạch, đặt khối gộp, hòm gỗ, cột nhà; đặt máu.
- Nhập ảnh PNG pixel art → bàn gạch.
- Xếp khay đạn: hộp (màu, số bi), băng, cửa, hộp "?", tunnel, ô trống, ô tường; tự sinh khay từ bàn.
- Thông số level: capacity, multiplier, độ khó, kích thước bàn/khay.
- Kiểm tra lỗi (cân bi, khay kẹt, capacity…), chơi thử ngay level đang sửa.

**Chưa có (để sau)**
- Bot tự chơi đo độ khó (giai đoạn 3).
- Mechanic mới (nhiễm độc, kính lúp, pháo, 2 màu) — thêm vào tool khi làm tới chương đó (mỗi mechanic thêm một "bút" đặt và một luật kiểm tra).

---

## 2. Quyết định lớn: lưu level ở dạng nào?

| | **A. Ghi ra CSV gốc** (`BoardProps_v3`, gom 5 level/thư mục) | **B. Mỗi level một asset** `LevelData` (ScriptableObject) — *đề xuất* |
| --- | --- | --- |
| Sửa một level | Phải đọc–ghi lại cả file 5 level | Chỉ đúng một file |
| Thêm mechanic mới | Phải chế thêm token vào chuỗi (`r-8/tox-3`), dễ sai | Thêm field có tên rõ ràng |
| Xem / sửa nhanh | Mở Excel, khó đọc | Inspector + cửa sổ editor |
| Git diff, review | Một dòng CSV dài, khó đọc | YAML từng field, dễ xem ai sửa gì |
| Tương thích game | Dùng ngay parser cũ | Cần thêm một đường nạp `LevelData → LevelModel` (nhỏ) |

**Đề xuất B**, kèm:
- **Nhập CSV → LevelData** (để lấy level gốc làm mẫu / so sánh).
- **Xuất LevelData → CSV** (tuỳ chọn, nếu sau này cần trả về định dạng gốc).

Game vẫn chạy đúng như cũ vì mọi nguồn đều đổ về **`LevelModel`** (bàn, khối, khay) — phần chơi không cần biết level đến từ đâu.

---

## 3. Dữ liệu

### 3.1 `LevelData` (một level = một file `Assets/_Project/Data/Levels/custom/Level_001.asset`)

| Nhóm | Field | Ghi chú |
| --- | --- | --- |
| Chung | `title`, `chapter`, `difficulty`, `capacity`, `multiplier`, `notes` | `title`/`notes` cho người thiết kế (vd "boss chương 2") |
| Bàn | `boardCols`, `boardRows`, `cells[]` (mã màu + HP từng ô, rỗng = trống) | Lưu theo **ô thiết kế** (chưa nhân multiplier), giống CSV |
| Vật trên bàn | `groups[]` (khối gộp: id, màu, HP, danh sách ô), `crates[]` (id, HP, ô), `columns[]` (id, HP, ô) | Ô thuộc nhóm thì không còn là gạch lẻ |
| Khay | `ammoCols`, `ammoRows`, `ammoCells[]` (loại ô: trống / tường / hộp / tunnel; hộp: màu, số bi, băng, "?", cửa) | |
| Tunnel | `tunnels[]` (id, ô, hướng, hàng chờ hộp) | |
| Ảnh nguồn | `sourceImage` (tham chiếu PNG nếu nhập từ ảnh) | Để nhập lại khi sửa ảnh |
| Mở rộng | (sau) `toxic`, `magnifier`, `cannon`, `dualColor`… | Mỗi mechanic mới thêm field |

### 3.2 `LevelPack` (danh sách level của bộ `custom`)
- Thứ tự level 1..N (kéo thả đổi thứ tự trong editor), mỗi phần tử trỏ tới một `LevelData`.
- `GameConfig` thêm lựa chọn **bộ level đang chơi**: `original` (LevelSet cũ) hoặc `custom` (LevelPack). Cheat "Go to level" dùng theo bộ đang chọn.

### 3.3 Đường nạp
`LevelParser.Load(...)` → nếu bộ là `LevelPack` thì `LevelDataConverter.ToModel(levelData)`, ngược lại đọc CSV như cũ. `ToModel` dựng đủ `BoardItems` (nhân multiplier như parser), `AmmoCells`, `HpByColor`, `BallsByColor`.

---

## 4. Giao diện (một cửa sổ, `Tools ▸ Bounce Blast ▸ Level Editor`)

```
┌──────────────┬─────────────────────────────────────────────┬──────────────────────┐
│ DANH SÁCH    │  [Bàn gạch]  [Khay đạn]   ← tab              │ THÔNG SỐ             │
│ Level 001 ★  │ ┌─────────────────────────────────────────┐ │ Tên  [Cá heo     ]   │
│ Level 002    │ │                                         │ │ Chương [3] Khó [Hard]│
│ Level 003 ⚠  │ │        lưới ô (zoom, kéo, cuộn)         │ │ Capacity [ 210 ]     │
│ …            │ │                                         │ │ Multiplier [2]       │
│              │ │                                         │ │ Bàn 20×32  Khay 5×7  │
│ [+ Mới]      │ └─────────────────────────────────────────┘ │──────────────────────│
│ [Nhân bản]   │ CÔNG CỤ: 🖌 Cọ  🪣 Tô vùng  ⌫ Tẩy  ▭ Chọn    │ CÂN BI THEO MÀU      │
│ [Nhập CSV]   │ MÀU: ■r ■b ■g ■y ■o ■p ■c ... (bảng màu)    │ r  HP 320 | bi 320 ✔ │
│ [Nhập ảnh]   │ HP: [1]  Khối gộp  Hòm gỗ  Cột nhà          │ b  HP 160 | bi 140 ✖ │
│              │                                             │──────────────────────│
│              │                                             │ LỖI / CẢNH BÁO       │
│              │                                             │ ✖ Thiếu 20 bi xanh   │
│              │                                             │ ⚠ Khay: 0 hộp bấm được│
│              │                                             │──────────────────────│
│              │                                             │ [Tự sinh khay]       │
│              │                                             │ [▶ Chơi thử] [Lưu]   │
└──────────────┴─────────────────────────────────────────────┴──────────────────────┘
```

- **Ô vẽ màu thật** (lấy từ `ColorLibrary`), số HP hiện trên ô khi > 1; khối gộp / hòm / cột vẽ viền nhóm.
- Tab **Khay đạn**: mỗi ô hiện màu + số bi; biểu tượng băng / cửa / "?" / tunnel; ô **bấm được ngay** (theo luật thông đường thật – dùng lại `AmmoPathRules`) có viền sáng.
- Phím tắt: B cọ, G tô vùng, E tẩy, Ctrl+Z hoàn tác (dùng Undo của Unity), Ctrl+S lưu, F5 chơi thử.
- ★ = đã kiểm tra không lỗi; ⚠ = còn cảnh báo.

---

## 5. Chức năng chi tiết

### 5.1 Vẽ bàn gạch
- Cọ: tô một ô với màu + HP đang chọn. Tô vùng: các ô liền nhau cùng màu. Tẩy: về trống.
- **Khối gộp**: chọn vùng (kéo chữ nhật hoặc tô) → "Gộp" → nhập HP chung. Hình chữ L/T được (danh sách ô).
- **Hòm gỗ**: chọn vùng → "Thêm hòm" → HP. Gạch bên trong vẫn vẽ bình thường (hòm vỡ thì lộ ra).
- **Cột nhà**: chọn vùng dọc → "Thêm cột" → HP.

### 5.2 Nhập ảnh PNG → bàn gạch
1. Chọn ảnh (pixel art, ví dụ 20×32; ảnh to hơn thì thu nhỏ về kích thước bàn, giữ tỉ lệ, căn giữa).
2. Mỗi pixel: trong suốt (alpha < 50%) → ô trống; còn lại → **màu gần nhất** trong `ColorLibrary` (so khoảng cách trong không gian màu Lab cho giống mắt người).
3. Tuỳ chọn **giới hạn số màu** (ví dụ tối đa 6 màu: gộp các màu ít ô vào màu gần nhất) — số màu quyết định độ khó.
4. HP mặc định cho mọi ô (thường 1), chỉnh sau bằng cọ.
5. Hiện ảnh gốc và bàn sau khi đổi **cạnh nhau** để so.

### 5.3 Tự sinh khay đạn
Đầu vào: tổng máu từng màu (tính cả khối gộp, gạch trong hòm; **không** tính cột nhà, hòm — vì bi trúng hòm/cột không mất).
1. Chia máu mỗi màu thành các hộp: kích thước hộp theo mẫu game gốc (bội số thường gặp như 8/12/16/20…, đặt trong `GameConfig` hoặc trong tool), cộng lại **đúng bằng** tổng máu màu đó.
2. Xếp hộp vào khay theo **chiến lược** chọn được:
   - *Dễ*: màu cần sớm (gạch ở hàng dưới cùng / lộ ra trước) nằm hàng gần lối ra.
   - *Trộn*: xen kẽ màu, buộc phải dọn đường.
   - *Khó*: màu cần sớm bị chôn sâu.
3. Đề xuất **capacity** = tổng bi 2–3 hộp lớn nhất + một phần dư (theo độ khó), người thiết kế chỉnh lại.
4. Sau khi sinh: chỉnh tay (đổi chỗ, đặt băng/cửa/"?", gộp/tách hộp) — **bảng cân bi cập nhật ngay**.

### 5.4 Kiểm tra (chạy liên tục)
| Mức | Kiểm tra |
| --- | --- |
| ✖ Lỗi | Bi màu X ≠ máu màu X (từng màu) |
| ✖ Lỗi | Không có hộp nào bấm được lúc bắt đầu |
| ✖ Lỗi | Có hộp lớn hơn capacity (không bao giờ bắn được) |
| ✖ Lỗi | Ô vượt kích thước bàn/khay, nhóm rỗng, tunnel hướng ra ngoài khay |
| ⚠ Cảnh báo | Capacity < hộp lớn nhất × 2 (rất dễ thua), > tổng bi (không có áp lực) |
| ⚠ Cảnh báo | Màu có trên bàn nhưng chỉ có ở hộp bị chôn sâu nhất |
| ⚠ Cảnh báo | Quá nhiều màu so với chương (theo `level-pack-50.md`) |

### 5.5 Chơi thử
- Nút **▶ Chơi thử**: lưu → mở scene Gameplay → vào thẳng level đang sửa (bằng một "level tạm" đặt qua `EditorPrefs`, không đổi `startLevel` thật). Dừng Play thì quay lại editor đúng level đó.
- Trong lúc chơi dùng được cheat (tốc độ ×4, auto-play, WIN/LOSE) để thử nhanh.

### 5.6 Nhập CSV gốc
- Chọn số level gốc (1–550) → tạo `LevelData` y hệt → dùng làm mẫu hoặc so sánh. Kiểm thử: nhập level gốc rồi chơi phải giống hệt chơi từ CSV.

---

## 6. Cấu trúc code (mỗi lớp một việc)

| File | Việc |
| --- | --- |
| `Scripts/Gameplay/Data/LevelData.cs` | ScriptableObject dữ liệu một level (chỉ dữ liệu) |
| `Scripts/Gameplay/Data/LevelPack.cs` | Danh sách level của một bộ |
| `Scripts/Gameplay/Data/LevelDataConverter.cs` | `LevelData` → `LevelModel` (game dùng) |
| `Scripts/Gameplay/Data/LevelValidator.cs` | Các luật kiểm tra (dùng chung cho editor và bot sau này) |
| `Editor/LevelEditor/LevelEditorWindow.cs` | Cửa sổ: bố cục 3 cột, tab, phím tắt |
| `Editor/LevelEditor/BoardPainter.cs` | Vẽ + công cụ cọ / tô / tẩy / nhóm trên lưới bàn |
| `Editor/LevelEditor/TrayPainter.cs` | Vẽ + chỉnh khay đạn |
| `Editor/LevelEditor/ImageImporter.cs` | PNG → bàn gạch (khớp màu, giới hạn màu) |
| `Editor/LevelEditor/AmmoGenerator.cs` | Tự sinh khay + đề xuất capacity |
| `Editor/LevelEditor/CsvLevelImporter.cs` | CSV gốc → `LevelData` (dùng lại `LevelParser`) |
| `Editor/LevelEditor/PlaytestLauncher.cs` | Lưu + mở Gameplay vào thẳng level |

Luật thông đường của khay dùng lại `AmmoPathRules` (không viết lại). Màu lấy từ `ColorLibrary`.

---

## 7. Giai đoạn (mỗi giai đoạn dùng được ngay)

| GĐ | Nội dung | Xong khi |
| --- | --- | --- |
| **1** | `LevelData` / `LevelPack` / converter / chọn bộ level; cửa sổ editor: danh sách, vẽ bàn, xếp khay, thông số, kiểm tra cân bi, chơi thử; nhập CSV gốc | Nhập level gốc 12 → chơi giống hệt bản CSV; tạo 1 level mới bằng tay chơi được tới thắng |
| **2** | Nhập ảnh PNG + giới hạn màu; tự sinh khay + 3 chiến lược + đề xuất capacity; đủ luật kiểm tra | Từ một ảnh 20×32 ra level chơi được trong < 5 phút |
| **3** | Bot chơi thử N lần → tỉ lệ thắng, số lần sát tràn capacity → gợi ý độ khó; xếp thứ tự level theo độ khó | Báo cáo độ khó cho cả bộ 10 level chương 1 |

---

## 8. Cần chốt

1. **Lưu dạng B** (mỗi level một asset) — đồng ý? *(đề xuất: có)*
2. Bộ level riêng tên `custom` và chọn bộ trong `GameConfig` — hay muốn tên khác (vd `pack50`)?
3. Kích thước hộp mặc định khi tự sinh (bội số 4? danh sách 8/12/16/20/24?) — *đề xuất: lấy thống kê từ 550 level gốc ở GĐ 2.*
4. Ảnh nhập: bạn tự vẽ pixel art (Aseprite/Piskel…) hay lấy ảnh có sẵn để thu nhỏ? *(cả hai đều hỗ trợ; ảnh thu nhỏ thường cần chỉnh tay nhiều hơn)*
