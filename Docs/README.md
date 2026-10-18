# Tài liệu dự án — mục lục

> Bắt đầu từ **`../ARCHITECTURE.md`** (bản đồ dự án: thư mục, "muốn đổi X thì vào đâu", các manager, luồng chạy).
> Quy ước code: **`../CODING_CONVENTION.md`**.

## Đọc theo mục đích

| Muốn… | Đọc |
| --- | --- |
| Hiểu cấu trúc dự án, tìm chỗ để sửa | `../ARCHITECTURE.md` |
| Sửa / thêm UI (panel, popup, HUD) | `kien-truc/ui.md` |
| Thêm loại khối mới trên bàn (bom, cổng…) | `kien-truc/them-loai-khoi.md` |
| Biết cơ chế nào dùng asset gốc nào | `kien-truc/co-che-asset-map.md` |
| Xem bản thiết kế tính năng | `thiet-ke/` |
| Làm level mới (Level Editor) | `huong-dan/level-editor.md` |
| Đọc file CSV level gốc | `tham-khao/csv-format.md` |
| Biết đã làm gì, khi nào, vì sao | `devlog.md` (mới nhất ở trên) · `bao-cao-tien-do.md` |

## Cấu trúc thư mục

```
Docs/
├── README.md               mục lục (file này)
├── devlog.md               nhật ký từng buổi: làm gì – vấn đề gì – quyết định gì
├── bao-cao-tien-do.md      báo cáo tiến độ 29/09 → 16/10 (để trình bày)
├── kien-truc/              CÁCH HỆ THỐNG ĐƯỢC TỔ CHỨC (đọc trước khi code)
│   ├── ui.md                   quy tắc UI: mỗi panel = prefab + 1 View; GameUI chỉ điều phối
│   ├── them-loai-khoi.md       hệ thống loại khối (Board/Kinds) + các bước thêm loại mới
│   └── co-che-asset-map.md     cơ chế ↔ prefab/mesh gốc ↔ script hiển thị
├── huong-dan/              CÁCH DÙNG CÔNG CỤ (cho người làm level / test)
│   └── level-editor.md         dùng Level Editor: vẽ bàn, xếp khay, cân bi, chơi thử, phím tắt, lỗi thường gặp
├── thiet-ke/               BẢN THIẾT KẾ TÍNH NĂNG (trước khi làm)
│   ├── cheat-tool.md           công cụ cheat test trên máy (đã làm MVP)
│   ├── level-pack-50.md        bộ 50 level mới + mechanic mới (đang chờ duyệt)
│   └── level-editor.md         thiết kế công cụ xếp level (GĐ1 đã làm)
├── tham-khao/              TRA CỨU
│   ├── csv-format.md           định dạng level CSV, mã màu, mã vật cản
│   ├── ripped-assets-analysis.md   phân tích bản rip game gốc & thứ đã chép sang
│   ├── menugame-asset-goc.md   phân tích ảnh MenuGame gốc ↔ asset
│   └── asset-chua-dung.csv     asset trong _Archive: thuộc tính năng chưa làm hay mồ côi
└── lich-su/                VIỆC ĐÃ XONG (chỉ để đọc lại / trình bày, không cần cập nhật)
    ├── qua-trinh-khoi-phuc-va-core-gameplay.md   nhật ký kỹ thuật chi tiết: khôi phục shader/UI, dựng core (05–13/10)
    ├── tai-cau-truc.md         kế hoạch + kết quả tái cấu trúc Art/Prefab (A–G)
    ├── roadmap-cu.md           roadmap ban đầu (02/10) – đã thay bằng devlog + thiết kế mới
    └── mau-sac/                quyết định bảng màu (chọn bộ C) & nền sau bàn (đã hoàn tác) + ảnh so sánh
```

## Quy ước giữ thư mục gọn

- Mỗi buổi làm: ghi **ngắn** vào `devlog.md`; chỉ tạo file mới khi đó là tài liệu sẽ được đọc lại (kiến trúc, thiết kế, tra cứu).
- Thiết kế làm xong và đã ghi vào devlog / ARCHITECTURE → chuyển sang `lich-su/` hoặc bỏ.
- Log, CSV sinh tự động từ tool chạy một lần: không giữ trong Docs (lịch sử git đã có).
- Ảnh / video tạm để ở `Captures/` (không đưa lên git), không để trong `Docs/`.
