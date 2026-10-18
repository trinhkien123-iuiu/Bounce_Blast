# Nền sau bàn – 4 bản sáng hơn (chưa áp vào project)

Ảnh: `nen-sau-ban.png` (render thật trong game). Nền sau bàn gồm 5 lớp (theme Normal):

| Lớp | Object | Hiện tại |
|---|---|---|
| Màu camera | `Main Camera.backgroundColor` (= `groundColor` của theme) | (0.28, 0.31, 0.68) |
| Lớp phủ gradient | `Mask NORMAL` (SpriteRenderer, sprite `Mask`, xanh đậm 28,34,113 đục) | alpha 1.0 |
| Mặt nền sau bàn | `Plane` – material `t4-surface` (`_BaseColor`) | (0.169, 0.180, 0.357) |
| Nền khay đạn | `Backdrop` – material `t4-wall` (`_BaseColor`) | (0.128, 0.139, 0.274) |
| Viền khay | `TrayWallLip` – `_BaseColor` | (0.225, 0.248, 0.538) |

| Bản | Camera bg | Mask alpha | Plane | Backdrop | TrayWallLip |
|---|---|---|---|---|---|
| A · Sáng nhẹ | (giữ) | 0.55 | .22 .235 .46 | .19 .21 .41 | .30 .33 .66 |
| B · Sáng vừa | .34 .38 .78 | 0.30 | .30 .34 .66 | .26 .30 .58 | .38 .43 .80 |
| C · Xanh trời | .30 .55 .95 | 0.22 | .22 .42 .78 | .18 .36 .68 | .28 .52 .92 |
| D · Xanh tím sáng | .40 .38 .82 | 0.25 | .36 .33 .72 | .30 .28 .64 | .45 .42 .86 |
