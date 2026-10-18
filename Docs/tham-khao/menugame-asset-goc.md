# MenuGame — phân tích 5 ảnh gốc và kiểm tra asset

Nguồn ảnh: `D:\GitHub\Asset_MainGame_Bounce Blast\MenuGame` (5 ảnh chụp từ game gốc).
Nguồn đối chiếu asset: `ManagerScene.unity` trong `D:\toolGame\game\ExportedProject` (cây `MainMenuCanvas`) — duyệt toàn bộ GUID tham chiếu (đệ quy 6 cấp) rồi so với `Assets/` của dự án.
Phân loại: **PROJECT** = đã dùng được trong `_Project`; **ARCHIVE** = có trong dự án nhưng đang nằm `_Archive` (kéo ra là dùng); **RIPONLY** = chỉ có trong bản rip, chưa vào dự án; **MISSING** = GUID không có ở đâu.

## 1. Menu gốc có gì (theo 5 ảnh)

| Tab | Nội dung thấy trong ảnh |
|---|---|
| **Home** | Avatar hồ sơ (góc trái); xu `450` (+); tim `5 Full` (+); thanh phần thưởng có huy chương `0/1`, đồng hồ `3d 7h`, tim vô hạn `10m`; **6 nút sự kiện** hai bên (kính thiên văn 13d7h, núi lửa "Start", bảng ghi chú có dấu "!" 6d7h, tên lửa 1d4h, hộp quà 7h41m, cột đá "SALE" 2d7h); quái cam trong miệng cống; nút xanh `Level 48`; 5 tab dưới đáy (Shop có huy hiệu 1) |
| **Journey** | Tab nổi tên "Journey", chữ **Coming Soon** + ổ khoá vàng trên nền caro tím |
| **Shop** | Thanh xu; "Special Offers" (Starter Bundle: 3000 xu + 4 booster + tim vô hạn 1h, giá 137.000đ); "Bundles" (Small, Medium… có xu, booster, tim vô hạn, "No ADS") |
| **Collection** | Ảnh bếp "Kitchen" 3 nhân vật; hộp phần thưởng (3000 xu, thìa vàng "Grand Prize!", booster ×2); thanh `6/81` + rương; đồng hồ `8d 0h`; lưới 9 bộ thẻ (Breakfast, Christmas, Coffee, Dairy Products, Desserts, Fruit, Meats, Pastry, Sauces) mỗi bộ có thanh tiến độ + phần thưởng; nút (i), sao, gói thẻ ở rìa |
| **Settings** | Sound, Music, Vibration + **Support, Terms, Privacy, Restore Purchases** |

Bản dựng lại hiện có: Home (rút gọn, chưa có avatar, thanh phần thưởng, 6 nút sự kiện), Settings (3 công tắc), tab còn lại báo "coming soon" (bản gốc Journey cũng là "Coming Soon" nên khớp).

## 2. Kết quả kiểm tra asset (theo từng trang của MainMenuCanvas gốc)

| Trang | Số asset tham chiếu | PROJECT | ARCHIVE | RIPONLY | MISSING |
|---|---:|---:|---:|---:|---:|
| Home (Main Panel) | 64 | 40 | 20 | 3 | 1 |
| BottomPanel (5 tab) | 24 | 21 | 0 | 2 | 1 |
| Settings Panel | 92 | 78 | 8 | 4 | 2 |
| Journey (Leaderboards) | 69 | 29 | 32 | 7 | 1 |
| Collections Panel | 38 | 24 | 10 | 3 | 1 |
| Shop Panel | 124 | 54 | 53 | 16 | 1 |

(Có chồng lấn giữa các trang: Coin, Rounded, Play Button Press… dùng chung.)

### Đọc kết quả
- **Home + Settings + tab dưới đáy: đủ để chạy** như bản đang làm. Phần ARCHIVE của Home là sprite trạng thái nhấn (`Play Button Press`, `Hard/Super Hard Level Button Press`…), `Coin`, `Rounded`, `fx_star_white`, ảnh số cấp — kéo ra khỏi `_Archive` khi dùng.
- **RIPONLY ở Home/Settings/BottomPanel** chỉ là 2 shader TextMeshPro (project đã có shader TMP cùng tên trong package — không cần), `CoinAttraction.prefab` (hiệu ứng xu bay, tuỳ chọn), `IngameDebugConsole.prefab` (công cụ debug, bỏ).
- **MISSING (1–2 GUID mỗi trang):** tham chiếu tới thứ không có trong bản rip (hầu như chắc là script/SDK của game gốc). Chưa lần ra từng cái; không thấy ảnh hưởng hình ảnh ở bản đang làm.
- **Shop:** sprite phần lớn có (ARCHIVE). Thiếu 12 file dữ liệu sản phẩm IAP (`Starter Bundle`, `Coin Pack 1–6`…) chỉ là ScriptableObject cấu hình mua hàng, không phải hình; `loadingSpinnerIAP` (animation chờ). Shop cần hệ thống mua hàng/giá tiền nên chưa thể làm nguyên bản.
- **Journey:** `Coming Soon Button`, `Lock Icon` (ổ khoá) đã có trong `_Archive` → làm được ngay. Bảng xếp hạng thật cần prefab `Leaderboard Rank Slot` + dữ liệu online (không cần cho "Coming Soon").
- **Collection:** `Collections Panel` gốc chỉ có 98 object — lưới bộ thẻ được sinh lúc chạy từ prefab/dữ liệu. Các sprite của nó **chưa có trong dự án nhưng có trong bản rip**: `Card-1…9_mir`, `Card From Back`, `Empty Card`, `Chest`, `Kitchen Theme Icon`, các món ăn (`Cereal`, `Cheese`, `Cherry`, `Cookie`, `Pancake Stack`, `Meatball`, `BBQ Sauce`, `Hot Sauce`, `Nacho Sauce`, `Christmas Pudding`, `Lemon Cheesecake`, `Iced Coffee`…), `Card Package` (Spine). Chưa tìm thấy ảnh **banner bếp "Kitchen"** nhiều nhân vật theo tên — cần tìm thêm (có thể ở `Resources/images` hoặc `banners`).

## 3. Thứ trong ảnh mà dự án CHƯA có (hoặc chưa tìm thấy)

| Thứ trong ảnh | Tình trạng |
|---|---|
| Avatar hồ sơ góc trái Home | Có `Profile-1` trong dự án; còn `Profile-2…20` trong bản rip (thư mục `Resources/profileicons`) |
| 6 nút sự kiện hai bên Home (kính thiên văn, núi lửa, bảng ghi chú, tên lửa, hộp quà, cột đá) | Khung/nền nút (`MenuOffer*`) có một phần (`MenuOfferBGRed`, `MenuOfferGoldenFrame`, `MenuOfferGreenBar` đã vào dự án; các màu `BGBlue/Gray/Green/Orange/Turquoise` còn trong rip). **Icon bên trong (kính thiên văn, núi lửa, tên lửa, cột đá, bảng ghi chú) không tìm thấy theo tên trong bản rip** — nhiều khả năng là nội dung LiveOps tải từ máy chủ hoặc Spine (`ParisOfferIcon`, `GiftBox`), không có trong bản export |
| Thanh phần thưởng 0/1 + huy chương + tim vô hạn | `Medal` (ARCHIVE), `Heart Infinity` (ARCHIVE), `infinity` (có sẵn); thanh nền cần kiểm tra thêm |
| Hộp quà Spine, gói thẻ Spine | Có dữ liệu Spine trong rip (`GiftBox`, `Card Package`) nhưng dự án **chưa có Spine runtime** → không chạy được |
| Ảnh bếp "Kitchen" | Chưa tìm thấy |
| Hệ thống tim, mua hàng, thu thập thẻ, xếp hạng | Là **tính năng** (code + dữ liệu), không phải asset |

### 3b. Kiểm tra bổ sung trong `D:\toolGame\game\ExportedProject`
- **Có trong bản gốc (chưa vào dự án):** `Texture2D/WeeklyAdventure.png` (bảng ghi chú tím + bút chì, kèm Spine), `ParisOfferIcon.png` (tháp Eiffel, Spine), `LavaRushFailPanel`, `LavaPopUpTile`, `LavaText_mir` (sự kiện Lava Rush), `Travel Map`, `Helm_mir`, `Cruise Trip Text` + 21 ảnh thành phố `Resources/cities/*` (Journey), `BouncePassTextBar`, `Paris/London Offer BG/Text`, prefab `Paris/London Special Offer Variant`, `Starter Pack Offer Variant`…
- **Container nút sự kiện của Home trong scene gốc gần như rỗng** (chỉ có 1 nút mẫu với nền `MenuOfferBGRed`, `1_16`, `MenuOfferGreenBar`, `MenuOfferGoldenFrame`). Các nút trong ảnh (kính thiên văn, núi lửa, tên lửa, cột đá, hộp quà…) được **sinh lúc chạy** từ prefab/dữ liệu LiveOps, nên icon không nằm trong cây `MainMenuCanvas`. Chưa tìm được icon kính thiên văn, tên lửa, cột đá "SALE" theo tên trong bản export — nhiều khả năng nằm trong atlas (`sactx`, 200 trang) với tên khác hoặc tải từ máy chủ.
- Banner "Kitchen" nhiều nhân vật: chưa thấy (chỉ có 2 banner quảng cáo trong `Resources/banners`).

## 4. Kết luận
- **Làm được ngay bằng asset hiện có (không cần thêm file):** Home đầy đủ hơn (avatar `Profile-1`, thanh xu/tim, nút Play kèm trạng thái nhấn), trang Journey "Coming Soon" + ổ khoá, trang Settings đầy đủ (Support/Terms/Privacy/Restore chỉ cần bật lại).
- **Cần kéo asset từ `_Archive` ra:** sprite nhấn nút, `Coin`, `Rounded`, `Coming Soon Button`, `Lock Icon`, `Medal`, `Heart Infinity`.
- **Cần chép từ bản rip (có sẵn, chưa vào dự án):** toàn bộ sprite Collection, `Profile-2…20`, các `MenuOfferBG*` màu còn lại.
- **Không có/không dựng lại nguyên bản được:** icon 6 nút sự kiện (có thể là nội dung tải từ xa), ảnh banner Kitchen, các hiệu ứng Spine, dữ liệu IAP.
- Phương pháp: script `menu_assets.py` (đối chiếu GUID). Chưa kiểm tra từng ảnh Journey/Shop/Collection ở mức pixel (chưa dựng các trang đó).
