# Bản đồ cơ chế ↔ asset gốc ↔ script hiển thị

Nguyên tắc: **dùng nguyên prefab/mesh/material gốc rip được; code chỉ làm phần việc mà script gốc (đã bị strip khi rip) từng làm** –
đặt vị trí, chọn mảnh, đẩy blend shape, văng mảnh. Luật chơi (ai nứt, ai mở) nằm ở controller, KHÔNG nằm ở các view này.

| Cơ chế (token CSV) | Asset gốc dùng | Script hiển thị | Việc script làm | Đối chiếu ảnh `Mechanics/` |
|---|---|---|---|---|
| Băng `/ice-N` | `Ammunation.prefab` → `Ice Model` (IceCube 1–4, IceCubeNFracture, SnowExplosion), `Ice.mat` (giống hệt bản gốc) | `Ammo/Blockers/IceBlockerView` | bật đúng mức băng theo số lần còn lại, văng mảnh `_cell` + tuyết khi nứt, phóng ×1.18 | hộp băng nguyên vẹn / vỡ 1 lần |
| Cửa `/door-N` | `AmmunationBlockerCover.prefab` → `Cover` (SkinnedMesh, blend shape 0 = đóng, 90 = mở), `Cover.mat` | `AmmoBox.CreateCover / ApplyCover` | đặt lên miệng hố, đẩy trọng số blend shape | hộp đóng mở |
| Nhả khối `/tu-XD` | `BlockerAmmoTunnel.prefab` → `Model/Tunnel` (2 slot vật liệu: thân + vành), `Tunnel_0.mat` | `Ammo/AmmoTunnel` | xoay theo hướng, **vành (slot 0) mang màu hộp kế tiếp**, thân giữ thép xanh | hộp nhả khối hướng lên / sang trái |
| Hộp gỗ `/cr-hp-id` | `ModularPrimitiveCrate.prefab` (mảnh Corner/Side/Mid + `Exploder/CrateFracture`), `CombinedCubicCrate` → `PlaneMid`, `Crate.mat` | `Board/Visuals/CrateView` + `ModularPrimitiveBuilder` | ghép mảnh theo hàng xóm, ván chéo từ PlaneMid, văng CrateFracture khi vỡ | hộp gỗ |
| Cột nhà `/wl` | `ModularPrimitiveColumn.prefab`, `CombinedCubicColumn` (huy hiệu) | `BoardController.CreateColumnVisual` + `ModularPrimitiveBuilder` | ghép mảnh 8 hướng | – |
| Hộp `?` `/qm` | `Ammunation` → `Qmark Model`, `QMark Mat Cube.mat` (`URP_QMark.shader`) | `AmmoBox.ApplyAppearance` | ẩn jelly, lộ màu khi thông đường | (có trong ảnh băng) |

Dùng chung: `Core/DebrisFx` (văng mảnh vỡ), `Board/Visuals/ModularPrimitiveBuilder` (ghép mảnh theo tên hậu tố hàng xóm).

## Asset còn thiếu / chưa làm (đang ở `_Archive/TinhNangChuaLam`)
Khoá/xích (`/lck`, `/ch`, `/chk`), bom (`/bomb`), rèm (`/cur`), keo (`/gl`), đá (`/st`), `/ba`, `/btu` (tunnel trên bàn), `/dc`, `/acs`, `/bqm`.
Prefab + mesh còn trong kho; token vẫn đang bị parser coi là vật cản tĩnh. Số lần xuất hiện trong 550 level: awl 21232, cr 3156, st 2747, cur 2308, qm 2117, wl 1276, door 1050, gl 725, bqm 566, ice 420, tu 379, acs 230, ch 194, ba 162, btu 142, chk 97, dc 84, lck 50, bomb 48.

## Còn lệch so với ảnh gốc (đã biết)
- Tunnel: vành màu của bản mình dày hơn (gốc chỉ một vạch mảnh ở miệng + đường cong mảnh phía sau).
- Băng: gốc xanh trời bão hoà hơn, bản mình hơi tím nhạt.
- Hộp gỗ: ván chéo chưa có 2 đinh ở hai đầu (PlaneLeft/PlaneRight chứa đinh nhưng là cột dọc full chiều cao – cần đặt lại).
