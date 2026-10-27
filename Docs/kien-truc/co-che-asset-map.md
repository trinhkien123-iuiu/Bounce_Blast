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
| Rèm `/cur-hp-id-màu` | `ModularPrimitiveCurtain.prefab` → `CurtainContainer` (mảnh `CurtainTop/Mid/Bot-…` theo hàng xóm; `Main`, `MainDark`, `Gold_0`), `CombinedCubicCurtain.prefab` → `Chain Container` (`CurtainChain` / `2x` / `3x`, `ChainMedal.controller`: Idle/Hit/Break, `Gold 2`, `Pentagon`), clip `reload & open curtain` | `Board/Visuals/CurtainView` + `Board/Kinds/CurtainKind` | ghép vải theo hàng xóm, treo xích từ mép trên giữa rèm (dài/vừa/ngắn theo chiều cao ≥12 / ≥7 hàng), tô viền huy hiệu theo màu `-c`, đổi số + đung đưa khi trúng, vỡ thì kéo vải lên mép trên | `Asset_MainGame_Bounce Blast/Mechanics/curtainMechanics.mp4` (frame ở `frames/curtainMechanics`) |
| Bom `/bomb-timer-id` | `CombinedCubicBomb.prefab` (`Model/TNT` mesh `Circle.009` + `TNT.mat`, `BombFuse2` ngòi lặp, `GrenadeExplosionFire` nổ nhỏ, `NukeExplosionFire` nổ lớn), clip `Bomb Small` / `Bomb Big` | `Board/Visuals/BombView` + `Board/Kinds/BombKind` | dựng quả TNT vừa các ô chiếm, số đếm ngược trên dải vàng; đồng hồ chạy từ lúc có bi đầu tiên, hết giờ → nổ lớn + thua; bi chạm (`LevelParser.BombHits` = 1) → bom phồng nhẹ rồi biến mất, không phá gạch | `Mechanics/bombMechanics.mp4` (frame ở `frames/bombMechanics`) |
| Board Qmark `màu-hp[-nhóm]/bqm` | `QMark World.mat` (gạch 1 ô) / `QMark World Big Cube.mat` (khối gộp), shader `Custom/HiddenBlocker` (dấu "?" theo toạ độ thế giới, tan biến bằng `_DissolveAmount`) + `Question_Mark`, `Hidden`, `Gemini_Generated…` | `Board/Visuals/QMarkCover` + `BoardBlock.Hidden/Reveal` + `BoardGrid.IsUncovered` | phủ bản sao mesh to 1.005× lên khối; còn che thì bi không trúng (như vật cản) và ẩn số máu; khi hở mặt trên hoặc hai bên (ô trống thật) thì dấu "?" tan ~0.6s lộ màu thật | `Mechanics/qmarkMechanics.mp4` (frame ở `frames/qmarkMechanics`) |
| Tunnel trên bàn `/btu-X` (+ hàng đợi ở `TunnelPropsBoard_v3.txt`) | `ModularPrimitiveBoardTunnel.prefab` (mảnh `Board Tunnel Left/Mid/Right Front`; mesh `Board Tunnel Left/Mid/Right`, `Tunnel.mat`, `Edge.mat`), `CombinedCubicBoardTunnel.prefab` → `Model/3D Text/Number` (tấm số, `Number_1.mat`) | `Board/Visuals/BoardTunnelView` + `Board/BoardTunnels` + `Board/Kinds/BoardTunnelKind` | miệng tunnel đứng yên (bi nảy, búa không phá); mỗi cột phía trên miệng giữ cube dồn lên trên, gạch trên bị phá thì cube dưới đẩy lên, đáy cột tunnel nhả cube mới theo hàng đợi (mỗi "màu-N" = N×Multiplier² cube 1 máu); số trên tấm = cube còn lại; hết hàng đợi và hết cube thì tunnel biến mất | `Mechanics/boardTunnel.mp4` (level 305; frame ở `frames/boardTunnel`) |
| Hộp `?` `/qm` | `Ammunation` → `Qmark Model`, `QMark Mat Cube.mat` (`URP_QMark.shader`) | `AmmoBox.ApplyAppearance` | ẩn jelly, lộ màu khi thông đường | (có trong ảnh băng) |

Dùng chung: `Core/DebrisFx` (văng mảnh vỡ), `Board/Visuals/ModularPrimitiveBuilder` (ghép mảnh theo tên hậu tố hàng xóm).

## Asset còn thiếu / chưa làm (đang ở `_Archive/TinhNangChuaLam`)
Khoá/xích (`/lck`, `/ch`, `/chk`), keo (`/gl`), `/ba`, `/dc`, `/acs`.
Prefab + mesh còn trong kho; token vẫn đang bị parser coi là vật cản tĩnh. Số lần xuất hiện trong 550 level: awl 21232, cr 3156, st 2747, cur 2308, qm 2117, wl 1276, door 1050, gl 725, bqm 566, ice 420, tu 379, acs 230, ch 194, ba 162, btu 142, chk 97, dc 84, lck 50, bomb 48.

## Còn lệch so với ảnh gốc (đã biết)
- Tunnel: vành màu của bản mình dày hơn (gốc chỉ một vạch mảnh ở miệng + đường cong mảnh phía sau).
- Băng: gốc xanh trời bão hoà hơn, bản mình hơi tím nhạt.
- Hộp gỗ: ván chéo chưa có 2 đinh ở hai đầu (PlaneLeft/PlaneRight chứa đinh nhưng là cột dọc full chiều cao – cần đặt lại).
