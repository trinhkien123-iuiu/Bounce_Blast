# Định dạng level CSV

Vị trí: `Assets/Resources/sheets_levels/<bộ level>/sheets_<a>_<b>/` — mỗi thư mục chứa 5 level (`a`..`b`).
Bộ level đang giữ trong project: `original` (mặc định), `dailychallenge`. Các bộ khác (`wideboard`, `openboard`, `levelfunnelv*`...) lấy lại từ `D:\toolGame\game\ExportedProject\Assets\Resources\sheets_levels\` khi cần.

| File | Nội dung |
| --- | --- |
| `BoardProps_v3.txt` | Bàn chơi + lưới hộp đạn |
| `TunnelProps_v3.txt` | Danh sách hộp đạn nằm trong mỗi tunnel của khay đạn |
| `TunnelPropsBoard_v3.txt` | Tunnel trên bàn chơi |
| `../Constants.txt` | Hằng số của bộ level |

## BoardProps_v3

- Dòng 0: header nhóm (`Id, HardLevel, Capacity, BoardSize, IsFigurative, CUBES..., $, AMMUNITIONS..., Multiplier`)
- Dòng 1: chỉ số cột
- Từ dòng 2: dữ liệu. Một level bắt đầu ở dòng có `Id` và kéo dài tới dòng trước `Id` kế tiếp. Dòng đầu CSV = hàng **trên cùng** của bàn.

| Cột | Ý nghĩa |
| --- | --- |
| `Id` | Số level |
| `HardLevel` | `-` = Normal / Hard / SuperHard |
| `Capacity` | Sức chứa bi (đã parse, **chưa dùng**) |
| `BoardSize` | `cột x hàng`, VD `20x32` |
| `IsFigurative` | Level vẽ hình |
| `CUBES` 0..31 | Ô bàn chơi (xem bên dưới) |
| `$` | Cột phân cách |
| `AMMUNITIONS` 0..7 | Ô khay đạn (xem bên dưới) |
| `Multiplier` | Mỗi ô CSV nhân thành `N×N` cube |

### Ô CUBES

| Mã | Ý nghĩa |
| --- | --- |
| `-` | Trống |
| `màu-hp` | Cube, VD `r-20` |
| `màu-hp-groupId` | Cube thuộc nhóm (share HP?) |
| `/wl-...` | Tường |
| `/bomb-...` | Bom |
| `/lck-...` | Khóa |
| `/btu-...` | Board tunnel |
| `màu-hp/cr-hp-id` | Cube bị hòm gỗ `id` che, mỗi tấm `hp` máu |
| `màu-hp/st-...` | Cube bị đá phủ |
| `màu-hp/cur-...` | Cube bị rèm che |

### Ô AMMUNITIONS

| Mã | Ý nghĩa |
| --- | --- |
| `màu-sốbi` | Hộp đạn, VD `p-8` |
| `.../ice` | Hộp đạn bị băng |
| `.../qm` | Hộp đạn bí ẩn (dấu ?) |
| `/tu-<id><hướng>` | Tunnel, VD `/tu-AL` = tunnel A hướng trái |
| `/awl-...` | Ô không có hộp |

## Mã màu

`g` green · `r` red · `b` blue · `y` yellow · `o` orange · `p` pink · `w` white · `bl` black · `br` brown · `pr` purple · `c` cyan · `te` teal · `pe` peach · `si` silver · `gr` dark green · `gra` gray · `wg` warm gold · `bb` bronze brown · `sb` skin beige

Mã vật cản (không tính là màu): `cr` crate · `wl` wall · `st` stone · `cur` curtain · `lck` lock · `bomb` · `btu` board tunnel.

Màu RGB hiện nằm trong `ColorMap` của [BounceBlastLevelSpawner.cs](../../Assets/_Project/Scripts/Level/BounceBlastLevelSpawner.cs). Bảng màu gốc: `Assets/Resources/sheets_constants/Colors_v3.txt`.
