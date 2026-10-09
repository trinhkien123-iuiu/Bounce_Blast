# Quá trình khôi phục asset & dựng lại core gameplay — Bounce Blast (clone)

> Ngày: 2026-10-05. Tài liệu ghi lại **làm gì – vì sao – bằng công cụ nào**, để đọc lại và trình bày.
> Nguồn tham khảo: `Bounce+Blast_3.7.6_APKPure.xapk`, bản AssetRipper `D:\toolGame\game\ExportedProject`,
> video/ảnh chơi game gốc trong `D:\GitHub\Asset_MainGame_Bounce Blast`.

---

## 0. Tóm tắt 1 phút

| Vấn đề | Nguyên nhân gốc | Cách giải |
| --- | --- | --- |
| Material hồng / sai màu | AssetRipper chỉ dịch ngược shader DirectX; game Android chỉ có GLES → xuất ra *dummy shader* | Tự viết tool đục **mã GLSL** trong blob shader, rồi **dịch máy GLSL → HLSL** từng dòng, giữ GUID gốc |
| Màu vẫn lệch dù shader đúng | Game gốc dùng **Gamma color space**, 2 đèn directional 0.585, ambient Trilight | Đổi project sang Gamma, chép y nguyên thông số ánh sáng từ scene gốc |
| UI tự dựng méo, kéo dãn | Panel dựng bằng code + sprite `_mir` chỉ là **nửa hình** | Tách **nguyên canvas UI gốc** từ ManagerScene thành prefab; khôi phục script `MirroredImage` (cùng GUID) |
| Bi không vào bàn | Điểm thả bi nằm trên mép bịt cửa phun → bi kẹt trong khe | Dời điểm thả xuống dưới mép (y 6.62) |
| Core cũ rời rạc, sai luật | Code cũ đoán luật | Viết lại core theo **video gốc + chữ ký class gốc** (Ball, Capacity, GridAmmo…) |

---

## 1. Khôi phục shader

### 1.1 Vì sao AssetRipper thất bại
- `ShaderExportMode: Decompile` của AssetRipper chỉ đọc **DXBC** (DirectX). APK Android chỉ có **GLES3** → âm thầm fallback *Dummy* (chỉ còn khối Properties).
- Nhưng với GLES, Unity lưu shader dưới dạng **mã GLSL dạng chữ** trong một blob nén **LZ4** → đọc được.

### 1.2 Tool `Tools/ShaderRipper/shader_ripper.py`
1. Giải nén `.xapk → .apk → assets/bin/Data`, đọc bằng **UnityPy**.
2. Với mỗi `Shader`: giải nén blob, đọc bảng mục lục `(offset, length, segment)`, mỗi `m_BlobIndex` → 1 đoạn GLSL.
3. Ghép **keyword** cho từng variant: `m_PlayerSubPrograms[].m_KeywordIndices` → `m_ParsedForm.m_KeywordNames`.
4. Xuất `_shaderlab.txt` (Properties/Tags), `_states.txt` + `_passes.json` (Cull/ZWrite/Blend/Stencil), `*.glsl` từng variant.

```bash
python Tools/ShaderRipper/shader_ripper.py "D:/toolGame/Bounce+Blast_3.7.6_APKPure.xapk" -o D:/toolGame/shader_rip --skip-builtin
```

### 1.3 Tool `Tools/ShaderRipper/glsl_to_unity.py` — dịch máy, KHÔNG viết tay
Lần đầu viết lại shader bằng tay → "nhìn vẫn không giống". Nên chuyển sang **dịch từng dòng** mã hlslcc:

| GLSL (hlslcc) | HLSL Unity |
| --- | --- |
| `vec3`, `bvec4`, `vec3(x)` | `float3`, `bool4`, `((float3)(x))` |
| `mix`, `fract`, `inversesqrt` | `lerp`, `frac`, `rsqrt` |
| `texture(s,uv)` | `tex2D` / `texCUBE` / `UNITY_SAMPLE_TEXCUBE` |
| `hlslcc_mtx4x4unity_ObjectToWorld[i]` | `transpose(unity_ObjectToWorld)[i]` (cột i) |
| `in_POSITION0`, `vs_TEXCOORD0` | struct `appdata`, `v2f` |

- Mỗi tổ hợp keyword material (VD `TCP2_SPECULAR + TCP2_SHADOW_LIGHT_COLOR`) → đúng variant gốc.
- `.meta` mang **GUID gốc** → ~180 material tự trỏ đúng, không gán lại.
- Kết quả: `Assets/_Project/Shaders/Ripped/` — 25 shader, **0 lỗi compile**.
- Bài học: lỗi regex `in` khớp nhầm `int`; vòng `while(true)` có lấy mẫu texture cần `[unroll(8)]` cho D3D.

### 1.4 Ánh sáng & color space (đọc từ `ManagerScene.unity` gốc)
- Color space: **Gamma**.
- `Directional Light 1 & Cubes and others`: 0.585, xoay (20, −12, 0), culling = mọi layer trừ `Ammunation`.
- `Directional Light 1 & Jellies`: 0.585, xoay (5.7, 0, 0), chỉ layer `Ammunation`.
- Ambient: Trilight (0.78 / 0.49 / 0.20).
- Camera gốc: ortho, **nghiêng xuống 20°** → giữ góc nghiêng, tính lại size/vị trí cho bàn của scene này.

---

## 2. Khôi phục UI gốc

### 2.1 Vì sao không tự dựng
Panel dựng bằng code bị méo. Sprite đuôi `_mir` chỉ chứa **nửa hình** — game gốc dùng script `MirroredImage` (448 chỗ) lật gương phần còn lại.

### 2.2 Tool `Tools/UnityYaml/ui_extract.py`
1. Đọc YAML `ManagerScene.unity` (19.858 object, đã "làm phẳng", không prefab lồng).
2. Lấy nguyên cây GameObject theo tên → ghi thành `.prefab`.
3. **Ánh xạ script trong DLL**: Unity định danh script nằm trong DLL bằng
   `fileID = int32(MD4("s\0\0\0" + namespace + class))` (`Tools/UnityYaml/md4.py`; kiểm chứng: `Image` = −765806418).
   → trỏ sang script của package uGUI / TextMeshPro trong project.
4. Script game có file cùng GUID trong project (`MirroredImage.cs`) → giữ; còn lại (UIText, UIButton, IAP…) → bỏ.
5. Asset thiếu (sprite, texture, material, animator) → chép từ bản rip, giữ GUID (`Assets/_Ripped/UI/Extra/`).

| Prefab (`Assets/_Project/UI/Prefabs/`) | Lấy từ |
| --- | --- |
| `GameCanvas` (HUD Lv + Settings) | `MenuManager/GameCanvas` |
| `CapacityBar` (world canvas) | `BOARDS/Capacity Text V4` |
| `FailCanvas`, `SettingsCanvas`, `SuccessCanvas` | `MenuManager/...` |
| `OutOfCapacityPanel` | `KP Heart System/.../Panel - End Game - Out Of Capacity` |
| `Boosters` | `Resources/asyncload/Boosters.prefab` |

### 2.3 `MirroredImage.cs` (khôi phục)
Kế thừa `Image`, giữ đúng field `m_MirrorHorizontally / m_MirrorVertically / m_UseSlicing` + GUID `9ca8d92f…`.
Vẽ phần gốc + phần lật UV (hỗ trợ 9-slice; cạnh nối giữa hình không có viền).

### 2.4 Nối logic (`GameUI.cs`)
- Script gốc bị bỏ → tìm phần tử **theo tên** rồi gắn sự kiện (`BTN-Try Again`, `BTN-Give Up`, `Bottom/Restart`…).
- Bật khung theo độ khó: `NormalLevel / HardLevel / SuperHard` (thay `KPHardLevelUIElement`).
- CanvasGroup gốc để alpha 0 (script gốc tự fade) → mở lại + tự viết fade/nảy.
- Màn thắng: màn đen → chữ **B-O-U-N-C-E** nảy lần lượt → **BLAST** → bảng Level / PERFECT / Rewards.

---

## 3. Core gameplay (viết lại, `Assets/_Project/Scripts/Gameplay/`)

### 3.1 Luật rút ra từ video + class gốc
| Luật | Bằng chứng |
| --- | --- |
| Multiplier 2: ô CSV → 2×2 cube; HP khối gộp/hòm & số bi × Multiplier² | `y-30` hiện **120**, `cr-50` hiện **200**; 40 bi / capacity 240 = **16%** trên thanh |
| Hộp đạn đứng yên; bấm được khi **thông đường ra lối thoát** (BFS) | video: lấy hộp ra, hộp khác không trượt |
| `ice-3`: mỗi lần bắn hộp bất kỳ, băng nứt 1 nấc | video giây 6→14 |
| Hộp `?` lộ màu khi thông đường | video giây 100 |
| Tunnel nhả hộp kế tiếp vào ô phía trước khi ô trống | số 8 → 7 → … |
| Hộp bay lên chân máng, bi chạy máng → cửa phun → nêm tam giác | video |
| Khối mất chỗ đỡ thì **rơi** | video giây 196–210 |
| Capacity = bi đã bắn chưa tiêu hao; vượt → *Out Of Capacity* | class `Capacity`, ảnh `Lose.jpg` |

### 3.2 Kiến trúc
```
GameManager ── LevelParser (CSV → LevelModel)
   ├─ BoardController ─ BoardBlock (cube / khối gộp / hòm / tường, trọng lực)
   ├─ AmmoGridController ─ AmmoBox, AmmoTunnel (BFS thông đường, băng, "?", tunnel)
   ├─ ConveyorSystem ─ ConveyorLane ×2 (dock → máng → cửa phun)
   ├─ BallManager ─ Ball (vật lý 2D tốc độ không đổi, nắn hướng, chống kẹt)
   ├─ CapacitySystem, BoosterSystem, ThemeApplier, AudioManager, Vfx
   └─ GameUI (prefab UI gốc)
```
- Toạ độ lòng bàn lấy từ **collider gốc** của `Board - Default` (x 0.84–9.74, y −6.35–7.5).
- `Tools/Bounce Blast/Setup Gameplay Scene`: sinh `ColorLibrary`, `GameConfig`, `UiSkin` (tìm asset gốc theo tên/GUID shader) và gắn `GameManager`.
- Theme theo độ khó: `Hole-1/2/3` (ô khay), `BG / M-Plane 2 / M-Plane 3` (nền), `Surface_0 + Side_0` (máng Normal).

### 3.3 Kiểm thử level 1
- `GameManager.autoPlay` (Inspector): tự bấm hộp khi đủ capacity → chạy hết màn: **192 khối → 0 → màn thắng**.
- Lỗi đã sửa: bi kẹt ở khe cửa phun (điểm thả 6.95 > mép bịt 6.92 → 6.62).

### 3.4 Đợt chỉnh theo ảnh/video gốc (khay, bi, máy xay, input)
So từng chi tiết với `PLay2.jpg` và khung hình cắt từ `video_game.mp4` (render camera ra ảnh 858×1908 đúng tỉ lệ ảnh gốc).
- **Bấm khay không ăn**: camera nghiêng 20° nên `ScreenToWorldPoint(z=0)` trả điểm trên near plane. Sửa: chiếu tia `ScreenPointToRay` xuống mặt phẳng z = −0.6 của hộp, đọc thêm touch (Device Simulator), chỉ chặn khi trỏ trúng UI có `Selectable`.
- **Bi bị xanh**: mesh `Sphere` chỉ có 1 submesh nhưng prefab gán 2 material (…, Blue) → Unity vẽ material thừa đè lên submesh cuối. Sửa: `sharedMaterials = { mat }`.
- **Bi méo/dẹt**: `FitVisual` scale theo bounds (bán cầu chỉ sâu 0.5, bounds tính cả đuôi). Sửa: scale đều theo đường kính 1.03; bán kính 0.17 (đo từ video).
- **Đuôi bi**: bỏ TrailRenderer tự chế, dùng lại tam giác `Trail` gốc trong prefab Ball (PerformantTrailShader, trắng 27%); xoay 90° quanh trục đuôi cho mặt tam giác hướng về camera, xoay cả bi theo vận tốc.
- **Khay đạn**: dựng lại bằng prefab gốc `ModularPrimitiveAmmoWall - Wallbase` — bộ autotile 47 biến thể, mỗi con đặt tên theo hàng xóm (`L-TL-T-TR-R-BR-B-BL`); bật đúng 1 biến thể theo ô xung quanh → mặt khay bo góc theo hình (chữ T, chữ L…). Nền tối = vật liệu `t4-wall` của chính prefab (cũng là màu ô `/awl`). Ô khay ≈ 1.7 (7 cột phủ hết bề ngang như ảnh gốc).
- **Hộp đạn**: vật liệu không viền (ảnh gốc không có viền đen; prefab gốc dùng shader Directional thường), nghiêng −57° để lộ mặt trên jelly.
- **Máy xay** (`Crusher LEFT/RIGHT` gốc ở chân 2 máng): hộp đậu trên máy xay, mỗi viên bi nhả ra thì 2 lưỡi `Cutter1/2` quay và cả khối rung (thông số gốc KPConveyorCrusher 0.05 / 25 / 5) — `CrusherFx.cs`.
- **Khung camera**: nửa bề ngang 5.98, đỉnh 9.12 (đo từ ảnh gốc: khung máng chiếm ~90% bề ngang, sát HUD).

### 3.5 Đợt sửa thứ 3 (UI méo, khay lỗ chỗ, đuôi bi, va chạm)
- **Logo màn thắng méo**: sprite chữ trong bản build bị cắt sát (vd `blast` 465×158) nhưng khung Image vẫn vuông (600×600) → bị kéo. Đặt khung = kích thước sprite × 1.29 (giữ tâm, chữ nền/chữ chính đúng tỉ lệ nhau).
- **Thanh CAPACITY méo**: canvas con của prefab gốc nằm ngang (X 70° so với root) → qua camera nghiêng bị dẹt còn 1/3. Cho thẳng mặt camera, ép nhẹ y 0.8 như ảnh gốc.
- **Khay lỗ chỗ**: bộ autotile 3D gốc lộ gờ/khe nối khi nhìn nghiêng → dựng khay thành mesh phẳng: mặt khay liền khối bo góc theo hình các ô chơi được + ô vuông bo tròn sáng hơn cho từng slot; vật liệu vẫn là M-Plane (theme) và t4-wall (nền). Hạ y theo độ sâu z để ô khớp đúng dưới hộp qua camera nghiêng.
- **Đuôi bi**: tam giác đuôi gốc nhuộm màu của chính viên bi (MaterialPropertyBlock `_ColorA`, alpha 0.7), to hơn 15%.
- **Va chạm** (xem dưới).

### 3.6 Đợt sửa thứ 4 (hộp lún/trồi, nút bo góc, khối nhiều máu)
- **Hộp chưa active lún vào khay**: mỗi ô khay là một LỖ thật (mặt khay = vành quanh lỗ, `MeshBuilder.AddRing`), đáy lỗ sáng nằm sâu phía sau; nền tối cùng độ sâu mặt khay, khoét đúng các ô slot. Hộp chưa thông đường lùi theo z 1.45 ô + hạ 0.3 ô (`AmmoBox.SinkDepth/SinkDrop`) → mép trước của lỗ che phần dưới, chỉ lộ nắp; khi thông đường thì trồi lên trước mặt khay (nội suy mượt).
- **Nút Replay/Home/Try Again thành viên thuốc**: `MirroredImage` chia viền 9-slice cho `pixelsPerUnit` thay vì `multipliedPixelsPerUnit` (prefab đặt Pixels Per Unit Multiplier = 2.6) → viền to gấp 2.6, bị ép thành hai đầu tròn. Sửa theo đúng `Image` gốc; preserveAspect chỉ áp cho kiểu Simple.
- **Khối nhiều máu méo bát giác**: mesh khối lớn gốc (`CombinedCubic`) được sinh lúc chạy bởi script đã bị strip (MeshFilter rỗng). Dùng mesh cube 1 ô nhưng giãn kiểu 9-slice 3D (`BoardController.SlicedCubeMesh`): đỉnh ở dải vát 20% ngoài mỗi cạnh giữ nguyên khoảng cách tới mép, phần giữa giãn → khối chữ nhật bo góc nhẹ như ảnh gốc.

### 3.7 Đợt sửa thứ 5 (tường, pháo hoa thắng, icon Settings)
- **Bi lấn vào tường**: collider lòng bàn lấy theo collider gốc (x 0.84/9.74, trần 7.75) lệch với mép trong NHÌN THẤY của khung. Đo lại trên ảnh render 2× → `arena` = x 1.07…9.53 (tâm vẫn 5.30, bàn cờ co theo), trần y 7.18 bo góc 0.25. Kiểm chứng: tâm bi luôn trong x 1.24…9.36.
- **Không có pháo hoa khi thắng**: các object pháo giấy gốc dùng plugin UI-particle (kiểu AssetKits ParticleImage) đã bị strip. Đọc thông số gốc trong `ManagerScene.unity` và dựng lại bằng `Scripts/UI/UiConfetti.cs`:
  - ConfettiBurst: nổ 100 mảnh, texture `confetti_0.png` 3×3, sống 1s, tốc độ 2–10, trọng lực.
  - ConfettiFullscreen: mưa giấy cầu vồng 300/s phủ màn. Object gốc bị xoay 180° nên phải reset rotation.
  - Pháo hoa `SF_Basic` (ParticleSystem gốc): bật scale và cho vẽ trên canvas.
- **Icon Settings méo**: sprite icon bị cắt sát trong atlas, khung vuông 173 → đặt khung = rect × 173/512; đồng thời sửa vạch mảnh giữa nút do MirroredImage lấy mẫu dính pixel kế bên ở mép nối (lùi UV nửa texel).

### 3.8 Đợt sửa thứ 6 (badge level, mũi tên máng, tam giác, theme theo ArtConfig)
- **ArtConfig gốc** (`Resources/configs/artconfigs/material configs/original/ArtConfig 1/2/3`): bảng `type → material` cho từng độ khó. Giải mã:
  - Vật thể nền: 0 nền sàn · 1 khung · 2 nền trong khung · 3 mặt bàn (M-Plane) · 4 ống · 5 lỗ ô (Hole-1/2/3) · 7 mặt khay (t4-bg / t6-bg 2 / t3-bg2) · 8 tường khay (t4/t6/t3-wall) · 9/13 mặt/hông băng chuyền.
  - `matPairsTop`: màu khối (Green_2, Red_1…), khớp ColorLibrary.
  - `ColorModularWallBoardGround`: màu nền sau bàn và tông khung (xanh / đỏ / xám). Đo từ ảnh gốc khớp gần tuyệt đối.
  - `ColorConveyorArrows`: màu mũi tên máng.
  - Các giá trị này đưa vào `GameConfig.Theme`; `ThemeApplier` tô Wall, tam giác, nền camera và gradient theo đó.
- **Badge level**: dùng sprite gốc theo độ khó (`Level` / `Hard Level_0` / `Super Hard Level`, có tab đầu lâu), chữ "Lv N" + "Hard / Super Hard"; nút Settings đổi `Hard Settings` / `Super Hard Settings`; dải booster đổi màu theo độ khó.
- **Mũi tên máng**: mỗi `KPConveyorSlot` gốc mang 1 sprite `ArrowL` trôi theo băng chuyền → `ConveyorLane.CreateArrows`: cách nhau 1.25, trôi 1.5 đơn vị/giây (đo từ video), xoay theo hướng chạy.
- **Tam giác**: `TriangleFx` – bi va vào thì phát hiệu ứng gốc `FlashExplosionBlue` (giới hạn 0.12s) + tam giác nảy nhẹ; màu tam giác theo theme.
- **Gạch khít**: bỏ hệ số 0.97 (khe tối giữa các khối).

### 3.9 Đợt sửa thứ 7 (khối chữ L, tunnel, cửa kính, bảng thắng, mũi tên)
- **Đọc sai level (vd Lv12)**: nhóm gộp `r-20-1` có hình chữ L nhưng bị gộp thành hình chữ nhật bao → đè lên các ô khác. Parser lưu mặt nạ ô (`BoardItemData.Mask`); `BoardController` chia mặt nạ thành ít hình chữ nhật con (`Decompose`), dựng hình + BoxCollider2D cho từng phần, dùng chung máu/nhãn; lưới/trọng lực/ô lộ ra đều tính theo ô thật (`BoardBlock.OccupiedCells`).
- **Decompile code gốc** (`ilspycmd` trên `AuxiliaryFiles/GameAssemblies/Assembly-CSharp.dll`): lấy được tên class/field (thân hàm bị strip). `BlockerType`: `door = AmmoCover`, `mdoor`, `ch`, `gl`, `acs`… làm bảng tra cơ chế.
- **Cửa kính `/door-N`** (BlockerCover): N=1 bắt đầu đóng, 2 bắt đầu mở; mỗi lần bắn 1 hộp mọi cửa đảo trạng thái; hộp có cửa đóng không bấm được. Tấm kính = mesh `Cover` của prefab gốc `AmmunationBlockerCover` (blend shape 0 = đóng, 90 = mở), đặt trên miệng lỗ.
- **Tunnel**: miệng ống quay đúng hướng nhả (`/tu-AR` → phải…), vành ống mang màu hộp sắp nhả.
- **Bảng thắng**: bật hiệu ứng gốc (tia sáng xu, sao, AreaShine), chữ PERFECT! bật lên, sửa sprite bị kéo (icon x2); pháo hoa `SF_Basic` chuyển World space, phóng từ đáy màn hình.
- **Mũi tên máng**: màu ≈ 0.66 × màu mặt máng (đo ảnh gốc), hiện dần khi ra khỏi máy xay và nhạt dần trên đoạn ngang tới ống xả.
- **Nền**: sprite `Mask/MaskHard/MaskSuperHard` để trắng (màu đã nằm trong sprite); lòng bàn dùng `t4/t6/t3-surface` (ArtConfig type 6).
- **Nạp lại màn trong cùng frame** không còn chồng 2 bàn (SetActive(false) trước Destroy).

### 3.10 Đợt sửa thứ 8 (khối chữ L liền khối, khay như gốc, hiệu ứng panel nhận vàng)
- **Khối chữ L bị tách đôi**: dữ liệu đọc đúng, lỗi ở hình. Chia hình theo lưới thô (mọi mép của các hình chữ nhật con) → mỗi mảnh có từng cạnh hoặc giáp hẳn mảnh khác hoặc là mép ngoài; cạnh giáp nhau thì kéo phần vát của mesh sang mảnh kề (`BoardController.JoinedCellMesh`). Mesh cube gốc nghiêng 10° nên mặt trên các mảnh lệch nhau → dời từng mảnh dọc hướng nhìn camera cho mặt trên trùng một mặt phẳng (vị trí trên màn hình không đổi).
- **Khay hở / không giống gốc** (đối chiếu khay trống trong `coin_panel.mp4`): ô = viên gạch sáng bo tròn (đáy lỗ sáng hơn mặt khay 20%), rãnh = mặt khay; tường tối là khối nổi: lớp gờ (mặt trước, màu trung gian) phía sau, lớp mặt trên tối nâng mép dưới ở chỗ giáp ô → lộ dải gờ dưới mỗi miệng tường; góc lõm của khay được lấp cung tròn (`MeshBuilder.AddFan`).
- **Panel nhận vàng** (phân tích `coin_panel.mp4`): (1) đống xu rơi từ to/cao về bục, (2) chớp sáng trắng + tia toả sau xu 2 nhịp, (3) sao lấp lánh liên tục quanh xu (`Scripts/UI/UiSparkles.cs`, texture `sparkle3`), (4) vệt sáng chéo quét qua nút Continue/x2 mỗi ~2.4s (`Scripts/UI/UiShineSweep.cs`, mặt nạ riêng để không cắt icon máy quay). Texture gốc: `fx_glow`, `glow7`, `sparkle3` (UiSkin).

### 3.11 Đợt sửa thứ 9 (khối lớn đồng màu, nắp đậy hố + thạch nhỏ giọt)
- **Khối lớn không liền khối**: nguyên nhân thật là đổ bóng — mesh cube 1 ô có pháp tuyến bo tròn, giãn to thì mỗi mảnh có dải sáng-tối riêng. Game gốc dùng tấm phẳng đồng màu → `BoardController.FlattenShading` đặt mọi pháp tuyến = mặt trên cho mesh giãn/ghép.
- **Nắp đậy hố** (video: sau khi bấm 1 hộp, hố trống còn thạch cùng màu đọng ở mép trên nhỏ giọt xuống, ~0.5s sau nắp màu gạch trượt từ dưới lên đậy kín): `Ammo/AmmoHoleLid.cs`, gọi trong `AmmoGridController.Launch`; ô được nhả hộp mới (tunnel) thì gỡ nắp.

### 3.12 Đợt sửa thứ 10 (tunnel, cửa thang máy, luật băng)
- **Tunnel (băng chuyền nhả hộp) sai tỉ lệ**: `FitVisual` ép khối vuông làm ống nằm ngang bị bẹp → giữ tỉ lệ mesh, cạnh lớn nhất ≈ 0.95 ô (`AmmoTunnel.FitMaxExtent`).
- **Cửa kính không khít miệng hố**: mesh Cover gốc là ô vuông bo góc lớn, kéo giãn không khớp lỗ → dựng lại 2 cánh phẳng đúng kích thước + bo góc của miệng hố, pivot ở mép ngoài, mở = 2 cánh trượt về 2 bên (như blend shape gốc); tấm lót sáng sau khe giữa. Cửa đóng thì hộp tụt sâu hẳn sau mặt khay (`DoorExtraSink`), mở thì trồi lên – kiểu thang máy, không còn lộ màu hộp.
- **Băng**: chỉ nứt khi hộp KỀ theo dấu cộng (trên/dưới/trái/phải) được bắn (trước đây nứt với mọi lần bắn).

### 3.13 Đợt sửa thứ 11 (kích thước hố đạn)
- Hố nhỏ, hộp tràn ra ngoài: miệng hố `SlotSize` 0.92 → 0.97 ô, bo góc 0.22, mép trên chỉ hạ 0.10 ô; hộp chưa active về đúng hằng số gốc `Ammunation.scaleOfInactive = 0.72` (active 0.8).
- Lòng hố là giếng tối (màu tường khay) như ảnh gốc Lv44 Hard; ô trống sau khi bắn được nắp sáng `AmmoHoleLid` đậy lại.

### 3.14 Đợt sửa thứ 12 (mỗi ô là một ngăn kín 4 vách)
- Trước: khay chỉ là các tấm phẳng (mặt khay có lỗ + đáy xa phía sau) → nhìn nghiêng thấy rỗng, các ô thông nhau, thân hộp lún thò sang ô khác.
- Giờ: mỗi ô có **giếng 4 vách** (`TrayWells`, vách thẳng góc mặt khay, bo góc theo miệng hố) + đáy riêng ngay dưới miệng hố.
- Miệng hố ôm thân hộp: `SlotSize 0.86` ô (hộp active 0.80, chưa active 0.72), `HoleTopInset 0`.
- Hộp lún: `SinkDepth 2.05` ô (cả cột nằm sau mặt khay), xoay trục thân về −70° (trùng hướng nhìn) khi lún, tụt THẲNG theo trục giếng (chỉ bù lệch y tới mặt khay) → không lọt sang giếng hàng dưới. Cửa đóng tụt thêm `DoorExtraSink 0.6`. Đáy giếng `TrayFloorZ 3.4`.

### 3.15 Đợt sửa thứ 13 (đối chiếu lại khay với ảnh gốc – sửa lại 3.14)
Đo trên ảnh gốc (Lv44 Hard / video Lv42): bản 3.14 đi sai hướng (hố thu nhỏ + vách giếng sâu). Cấu trúc thật:
- Mặt khay xanh (68,74,164); mỗi ô có hộp = một hố gần kín ô (`SlotSize 0.92`, gờ giữa 2 hố chỉ ~0.08 ô), lòng hố tối (47,50,101), hộp rộng ~0.8 ô nằm trong hố (cả active lẫn inactive ≈ 0.8 → `ammoInactiveScale 0.8`).
- **Ô trống "-" là một mảng tối liền**: các ô trống kề nhau KHÔNG có gờ ngăn (hố hợp nhất, bo góc ngoài) → `AmmoGridController.BuildTray` dùng `emptyAt`.
- Hộp chưa active chỉ lộ nắp (mặt trên), mép dưới bị gờ hố hàng dưới che: tilt −66°, lún `SinkDepth 1.1`, bù y 80% so với trục giếng. Giếng 4 vách (`TrayWells`) chỉ dựng cho ô có hộp (`TrayFloorZ 2.0`).
- Còn lệch: bóng/specular của hộp (shader Directional Cube) bóng và viền tối hơn ảnh gốc, mặt hộp gốc phẳng hơn.

### 3.16 Đợt sửa thứ 14 (phân cấp hộp active / chưa active)
- Active: nhô lên (tilt −59° lộ thân ~0.3 ô, scale 0.86, tiến về camera 0.3 ô, nhích lên 0.06 ô), sáng đủ màu, bóng đầy đủ.
- Chưa active: chìm trong hố (tilt −70°, scale 0.76, chỉ lộ nắp), tối đi 22% — hạ cả `_HColor/_SColor` (nếu chỉ hạ BaseColor thì highlight vẫn chói → hộp nhạt như phủ sương) và giảm specular.

### 3.17 Đợt sửa thứ 15 (căn tâm đạn trong hố, cửa che kín)
- **Đạn lệch tâm hố**: mesh jelly là cột dài ~1.9 ô nghiêng theo camera; `FitVisualUniform` căn theo tâm cả cột nên NẮP (phần nhìn thấy) lệch ~0.17 ô so với tâm hố. Giờ `AmmoBox.ComputeCapPoint` lấy tâm mặt nắp (đầu cột hướng camera) và `CenterCapOnHole` dời hộp trong mặt phẳng màn hình (camera ortho) cho nắp trùng tâm hố ở mọi trạng thái; đo kiểm: dx = dy = 0 px. Hộp active chỉ nhích lên 0.07 ô có chủ ý.
- **Cửa đóng không che hết đạn**: nắp hộp còn nằm trước mặt phẳng cánh cửa ~0.05 ô (nhô viền). `DoorExtraSink` 0.6 → 1.6 ô, `TrayFloorZ` 3.0 → nắp nằm sau cánh cửa, cửa đóng che kín 100%.

### Va chạm (đợt 3)
- Bỏ "homing" bẻ cong quỹ đạo liên tục (làm bi tụ đám, lượn vòng, xuyên qua chỗ bị chắn). Theo code `BouncingBall` cũ: bay thẳng tốc độ cố định, nảy phản xạ + lệch ngẫu nhiên ±8°, giữ vận tốc dọc tối thiểu 1.6, chống nảy ngang >0.2s; sau ≥3 lần nảy (hoặc 5s không trúng gì) mới ngắm thẳng vào khối cùng màu **chỉ khi CircleCast thấy đường bay thông** (`BoardController.TryGetLineOfSightTarget`).

---

## 4. Còn lại / hướng phát triển
- Cơ chế bàn chưa có: Stone, Curtain, Lock/Key, Bomb, Board Tunnel (đang coi là tường tĩnh).
- Booster đang dựng bằng code (prefab booster gốc là object 3D theo camera gốc); hòm gỗ chưa có thanh ván chéo.
- Màn Home, coin, tim (Play On mất 1 tim) chưa làm.
- Code core cũ (`Scripts/Core`, `Level`, `Configs`, `Units`) vẫn còn trong project, không được scene dùng — có thể xoá.

### 3.18 Số bi không đủ phá hết gạch (Lv93)
- Triệu chứng: bắn hết đạn mà còn gạch + hòm gỗ (số 173). Kiểm tra parser: với Lv12/39/50/60/85/92/93/94/95, tổng bi == tổng HP cube theo từng màu (Lv93: b88 pr72 r112 o88 c40 g200) → đọc level ĐÚNG.
- Nguyên nhân: hòm gỗ (`cr-100-5` → 400 HP) và tường `/wl-50-N` không nằm trong ngân sách bi, nhưng code cũ cho hòm hút bi mọi màu (mỗi bi trừ 1 HP rồi biến mất) → hòm ăn 227+ bi, thiếu bi cho cube.
- Sửa: `BoardController.HandleBallHit` – hòm vẫn bị trừ máu nhưng bi KHÔNG bị tiêu hao (nảy tiếp); `Ball.OnCollisionStay2D` bỏ qua hòm (chỉ đẩy bi ra). Đây là suy luận từ dữ liệu (chưa có video xác nhận cơ chế gốc).
- Lưu ý test: autoPlay hay kẹt capacity vì bi khác màu bay chờ (đạn xanh chờ hòm lộ ra) – không phải lỗi dữ liệu.

### 3.19 Cơ chế Out Of Capacity (video lose_coche.mp4, Lv44)
Phân tích video: thanh capacity đỏ từ ~75% (vignette đỏ, đã có); từ ~90% hiện 2 mũi tên vàng ▼ nhấp nhô chỉ vào booster Hammer và Capacity (+). Bấm hộp làm vượt: thanh nhảy kín 100%, hộp bật khỏi ô, kéo dài thành thanh chéo (nghiêng ~-38°) và LƠ LỬNG giữa không trung; ~0.6s sau màn tối + popup Out Of Capacity. Bi của hộp chưa được cộng.
Làm theo: `CapacitySystem.ForceFull` (Fill=1), `AmmoBox.Hover()/StopHover()` (HoverRoutine dùng unscaledDeltaTime, kẹp dt ≤0.05 – không kẹp thì frame dài của editor làm vòng lặp thoát ngay), `GameManager.TryLaunch → OutOfCapacityRoutine → LaunchNow` (Play On: +25% capacity rồi hộp bay tiếp; Give Up: thua), `GameUI` mũi tên cảnh báo (ngưỡng fill ≥ 0.9). Đã test: Play On → Max 250, Used 220, hộp lên máng.

### 3.20 Anim hộp bay lên máng (lose_coche.mp4)
Video: hộp bật lên thẳng đứng → kéo dài thành thanh xoay THEO HƯỚNG BAY (không quay vòng) → bay vòng cung lên-trái/phải tới chân máng (đầu thanh cong xuống) → dựng đứng lại đậu ở chân máng (~0.55s).
Code cũ: quay tròn 360° + nảy sin → sai. Mới (`AmmoBox.FlyRoutine`): quỹ đạo Bezier bậc 2 (điều khiển ở giữa + 2.2 ô cao), ease in-out, trục dài theo vận tốc (blend 0→1 trong 20% đầu, về thẳng đứng ở 25% cuối), stretch 1 + 1.6·sin(πt)^0.7, bề ngang 1.15·s; `boxFlyDuration` 0.38 → 0.6 (GameConfig + asset). Chưa làm: đầu thanh uốn cong khi vào máng (cần bẻ mesh).

### 3.21 Hộp bay = ống thạch dẻo (so 2 video ghi màn hình gốc vs bản làm lại, Lv67)
Bản 3.20 (thanh cứng xoay theo vận tốc) bị user chê "khựng, không uốn cong mượt". Soi từng frame video gốc: hộp là một ỐNG DẺO – đầu đi trước theo quỹ đạo, đuôi bám đúng đường đầu đã đi → rời hố gập khuỷu (đuôi còn đứng trong hố), giữa đường thẳng chéo, vào máng đầu cong cắm xuống, rồi co lại thành thanh đậu.
Làm: `AmmoFlightTube.cs` (mới) – quỹ đạo gấp khúc [ô → lên 0.9 ô → trên máng (1.5+0.9) → đáy thanh đậu] bo Chaikin 4 lần, mesh ống tròn 14 cạnh × 20 đoạn + 2 nắp bán cầu dựng lại mỗi frame (pháp tuyến hướng tâm → shader thạch có highlight; lần đầu bị lật mặt tam giác nên phẳng màu). `AmmoBox.FlyRoutine`: ẩn visual, head chạy ease in-out từ 0.9·hộp tới hết quỹ đạo, độ dài ống lerp(hộp → 1.5) + 0.7·sin, bán kính lerp(0.5·hộp → 0.3, e²), xong thì huỷ ống, hiện lại visual → lane.Receive (ShowDocked).
- 3.21b (user: "đừng vót tròn 2 đầu"): bỏ nắp bán cầu → 2 đầu PHẲNG vát mép nhỏ (bevel 0.22r), tiết diện siêu elip p=5 (vuông bo góc như thân hộp), 28 cạnh.
- 3.21c (user: "kéo giãn như lò xo, có stretch & squash"): FlyRoutine thành mô phỏng lò xo – nén lấy đà 0.07s (đầu tụt 22%), ĐẦU phóng theo ease in-out cubic, đuôi kẹt trong hố 10% thời gian rồi là khối lượng nối lò xo (K 260, damping 0.45) đuổi theo đầu → giãn từ đầu rồi giãn dần tới đuôi; đầu dừng ở máng thì đuôi lao quá đà → ống nén ngắn & phình, bật lại rồi yên (tối đa +0.35s). Bán kính bảo toàn thể tích r = r0·sqrt(dài nghỉ/dài hiện tại), kẹp 0.62–1.35.
- 3.21d (user video: khựng lúc bấm, đáp không lên trên máy xay, lag): 
  - Khựng đầu: bỏ pha nén 0.07s + đuôi giữ 10% + ease-in cubic (hộp đứng yên ~0.25s) → nén 0.035s, đuôi giữ 5%, đầu chạy ease-out `1-(1-u)^2.4` (phóng ngay). dt kẹp 0.05 (trước 0.033 làm anim chậm khi FPS thấp).
  - Đáp lên máy xay: `ConveyorSystem` tính lại dock từ bounds renderer của Crusher LEFT/RIGHT – chiếu mép trên máy xay theo trục up của camera (nghiêng 20°) về mặt phẳng máng z=-1.15 → đáy thanh đậu (dock−0.3) nằm đúng trên nóc máy xay (trước đây chồng lên máy xay).
  - Tối ưu `AmmoFlightTube`: số đỉnh cố định (20 cạnh × 21 vòng), mảng/tam giác cấp phát 1 lần, bảng tiết diện static, PointAt tìm tuần tự, bỏ InverseTransformPoint → đo 0.08 ms/lần dựng, 0 byte GC.
- 3.21e (video tua chậm của user): hộp đáp ĐÈ LÊN tâm 2 lưỡi cắt máy xay (Crusher */Shredder/Cutter1+Cutter2), che kín máy xay, bề ngang KHÔNG đổi (= bề ngang trong khay), rồi lún dần vào máy xay khi nhả bi.
  - `ConveyorSystem`: dock = trung điểm Cutter1/Cutter2, đặt z = -1.75 (trước lưỡi cắt) và bù y theo trục up camera → bỏ cách tính "đứng trên nóc" của 3.21d (sai).
  - `AmmoBox.ShowDocked`: rộng = BoxWidth (cell·ammoActiveScale), cao = 1.4·W·phần bi còn lại, đáy cố định ở dock − W/2 → lún xuống.
  - `FlyRoutine`: điểm đầu/cuối chiếu về mặt phẳng z = -2 theo camera (`ToPlane`), ống kết thúc đúng bằng thanh đậu (dài 1.4W, bán kính W/2); bề ngang chỉ đổi ±12% khi nén/giãn.
- 3.21f (user: "che mất cả hình shredder; kiểm tra Unity có anim không để tái sử dụng, vừa đúng vừa nhẹ"):
  - Tìm anim gốc: `_Ripped/AnimatorController/Ammo.controller` (Idle/Rotate… – quay hộp, không phải anim bay). Hộp gốc `RiggedCube (8)` vốn là mesh có xương (Armature/Bone…Bone.008, xem Hit.anim/Idle_0.anim) → game gốc uốn hộp bằng xương. Bản rip mất bindpose/SkinnedMeshRenderer, và mesh `_Ripped/Mesh/Cube_1` chỉ có đỉnh ở 2 đầu cột.
  - Giải pháp: `AmmoFlightTube` giờ GẮN LẠI XƯƠNG cho chính mesh gốc: chia nhỏ mesh theo trục Y (10 lát, dùng chung điểm giữa → 1823 đỉnh), 12 xương, trọng số tuyến tính, bindpose dịch theo Y; cache mesh dùng chung. Mỗi frame chỉ đặt 12 transform (0.004 ms) – Unity tự skin. Hình hộp giữ đúng mesh gốc (đầu phẳng, mép bo).
  - Đường bay: góc bo bằng CUNG TRÒN bán kính ≥ 1.3·bề ngang (Chaikin cho cung quá nhỏ → mặt trong khúc cua bị bóp nhọn); điểm vào máng nâng lên DockLen + 1.5.
  - Đậu: thân xương giữ lại làm thanh đậu (PoseStraight), rộng = 80% khung máy xay `Plane.028` (viền khung lộ 2 bên & dưới như video), cao 2·W, lún dần theo số bi; LateUpdate bám theo khi xếp hàng chờ.

### 3.22 Tối ưu hiệu năng + tâm đậu máy xay
Đo bằng ProfilerRecorder (Lv46 autoPlay, 300 frame, trong Editor):
| | Trước | Sau |
|---|---|---|
| Draw call | 369 (max 674) | 70 (max 147) |
| SetPass | 228 (max 408) | 47 (max 106) |
| Camera.Render | 3.11 ms | 1.86 ms |
| GC/frame | 39 KB | 29 KB (phần lớn do Editor) |
Nguyên nhân & sửa:
- Shader rip (CubeIsolatedOptimized, PerformantTrailShader) KHÔNG có biến thể instancing → MaterialPropertyBlock làm mỗi cube/đuôi bi 1 draw call. Đổi sang material dùng chung: cube = (màu × 4 mức vân `_CubeShadeOffset`), đuôi bi = 1 material/màu. Bật Dynamic Batching (Standalone/Android/iOS) để gộp các mesh nhỏ cùng material.
- `Vfx.Spawn` Instantiate+Destroy cho MỖI khối vỡ → pool theo prefab (tối đa 24 sống/prefab, tái dùng cái cũ nhất).
- Prefab gốc còn "3D Text/Text (TMP)" (TMP bị rip mất, còn MeshRenderer rỗng) ở mọi cube/hộp → `BoardController.RemoveStrayText`.
- Chữ % capacity chỉ dựng lại khi số đổi (`SetText("{0}%")`).
Tâm đậu: dock = tâm Cutter1/Cutter2 (world); phép chiếu theo camera nghiêng chuyển sang AmmoBox lúc bay (lúc dựng máng camera chưa nghiêng → lệch 15px). Bề ngang 72% bounds khung, đáy = tâm − 0.95W (sát mép trong đáy khung, như video gốc).
Dọn dự án: 446 asset không được scene/Resources/GameplaySetup tham chiếu (~48 MB, chủ yếu `_Ripped/GameObject`, `_Ripped/UI`) – danh sách `Captures/delete_list.txt`; lệnh xoá bị chế độ an toàn chặn → chờ user.
- 3.22b (user: "nên tăng đáy hộp lên"): đáy thanh đậu = tâm lưỡi cắt − 0.3W (trước 0.95W tràn xuống dưới khung). Lỗi thật sự làm hộp lòi dưới máy xay: hộp XẾP HÀNG CHỜ bị đặt PHÍA DƯỚI dock (`WaitPosition` trừ y) → chồng lên hộp đang đậu. Sửa: hộp chờ xếp chồng phía trên trong máng (bước = W·(2+0.15)).

### 3.23 Bi đâm xuyên & kẹt trong khối (Lv72)
Bắt tại trận bằng watcher (OverlapPoint tâm bi mỗi frame): `INSIDE Cube_c_11_25 moving True, ball bl` – khối đang RƠI đè lên bi khác màu. `Ball.FixedUpdate` ép vận tốc 11 đv/s mỗi bước, còn `OnCollisionStay2D` chỉ đẩy 0.02/bước → bi lún hẳn vào khối và kẹt (khối khác màu, đặc biệt gạch đen, không bao giờ tiêu hao được bi).
Sửa: `Ball.Depenetrate` dùng `Collider2D.Distance` (đã kiểm: dời bi theo `normal·distance` là tách ra) → đẩy bi ra hẳn + phản xạ vận tốc ra ngoài, áp cho mọi khối & tường trong OnCollisionStay; thêm lưới an toàn ~5 lần/giây (OverlapPoint tâm bi) phòng khi không có callback.

### 3.24 Cột nhà `/wl-hp-id` (video user Lv73)
Video gốc: cột Hy Lạp màu kem mảnh, có đầu & đế cột, huy hiệu tròn ghi máu giữa thân; bi MÀU NÀO cũng trừ 1 (91→88→…→3); hết máu vỡ vụn thành mảnh kem rơi, khối phía trên sụp xuống. Trước đây `/wl` bị đọc thành tường xám không phá được.
- Parser: `BoardItemKind.Column`, gộp theo id, máu = hp·m² (Lv73 `/wl-25-1` → 100, khớp video).
- Hình: asset gốc `Resources/prefabs/ModularPrimitiveColumn` – mỗi ô chọn mảnh có tên = tập ô hàng xóm (T = đế ColumnBottom, B = đầu ColumnTop, T-B = thân ColumnMid; cột rộng 2 ô: T-TR-R / L-TL-T / T-TR-R-BR-B / B-BL-L-TL-T / R-BR-B / B-BL-L), cả cột nghiêng 20° như cube. Huy hiệu = đĩa `Cylinder` (vật liệu Cream) của `CombinedCubicColumn` + số.
- Va chạm: collider chỉ bằng thân cột (60% bề ngang ô); bi trúng trừ máu, KHÔNG tiêu hao (như hòm – máu cột không nằm trong ngân sách bi), cột/hòm không phải mục tiêu ngắm (`IsTarget`), không tính vào điều kiện thắng.
- Vỡ: bật các mảnh vỡ gốc `Column*_cell` (5–6 mảnh/ô) + Rigidbody văng/rơi, tự huỷ ~1.2 s, kèm bụi kem.

### 3.25 Một hướng nhìn: camera thẳng (user yêu cầu bỏ camera nghiêng 20°)
Trước: camera gốc nghiêng 20° → mọi vật phải bù góc riêng (tan20 ở 6 chỗ trong khay, -70°/-59°/-62° ở cube/hộp/tunnel, ToPlane khi bay, phép chiếu dock…) → hay lệch (cột cao lệch hàng gạch, tâm máy xay sai 15px…).
Giờ (`Core/ViewSpace.cs`):
- Camera ortho rotation = identity. Camera nghiêng ép chiều dọc thế giới ×cos20 ≈ 0.94 → giữ y khung hình gốc bằng **ma trận chiếu ortho ép dọc** (`halfW = size·aspect`, `halfH = size/cos20`), tự tính lại khi đổi tỉ lệ màn hình.
- Khung bàn 3D gốc (`Level/Board - Default`, dựng cho camera nghiêng) được **là phẳng 1 lần** lúc khởi động: mỗi đỉnh mesh `y += z·tan20` (giữ z) → trên camera thẳng trông y hệt. Lưỡi máy xay (Cutter1/2, có quay) dời transform theo tâm quay.
- Vật dựng lúc chạy quay thẳng mặt về camera: cube/cột -70°→-90°, hộp -59°→-79° (chìm -90°), tunnel -62°→-82°; khay bỏ toàn bộ bù tan20 (chỉ dời gốc khay 1 lần theo ammoAreaTop đo kiểu cũ); máng đi qua `ViewSpace.FromLegacy`.
- Đèn riêng của thạch (chỉ layer Ammunation) xoay -20° cùng hộp để sáng-tối giữ nguyên.
- UI: camera riêng "UI Camera" (chiếu chuẩn, đặt x -1000) cho canvas Screen Space - Camera – nếu không, canvas cũng bị ma trận ép dọc kéo lệch. Thanh capacity world-space vẫn do camera game vẽ.
Kiểm: ảnh trước/sau Lv73/92/45 trùng bố cục; bấm khay khớp 22/22 ô (ScreenPointToRay); dock 2 máy xay đối xứng (y -5.79); hộp bay/đậu đúng lòng máy xay.
Quy tắc từ nay: **không nghiêng/bù góc riêng cho vật nào nữa** – vật mới quay thẳng về camera; số liệu đo theo góc nhìn cũ thì đi qua `ViewSpace.FromLegacy`.
- 3.25b (user): (1) máy xay bị vênh khi "là phẳng" → xoay CẢ KHỐI Crusher -20° quanh tâm khung Plane.028 + dời y theo độ sâu tâm (giống hệt nhìn qua camera nghiêng cũ); dock vẫn = tâm 2 lưỡi, 2 bên đối xứng y -5.79. (2) hộp active nhô thêm: lift 0.09 → 0.16 ô. (3) hộp chưa active khít miệng hố: ammoInactiveScale 0.76 → 0.88 (hố 0.92), active 0.86 → 0.90 để active không nhỏ hơn chưa active.
- 3.25c (user: bi & mũi tên thanh trên lệch xuống): đo mesh máng gốc sau khi là phẳng – lòng máng ngang giữa 2 gờ y 7.50|8.21 (tâm 7.86), máng dọc tâm x 0.49. Đường máng cũ topY 8.09 (dời FromLegacy z=-1.15 thành 7.67 – thấp 0.19) → bỏ FromLegacy, dời cả đường máng -0.23 cho topY = 7.86. Mũi tên & bi dùng chung đường này.
- 3.26 (ảnh mẫu user: hộp khay lộ nắp sáng + dải thân tối ngắn phía dưới, đè lên gờ khay; hộp active thân dài hơn): hộp chưa active KHÔNG còn lún sau mặt khay – `SinkDepth` 1.1 → 0.15 ô, tilt chưa active -85°, active -79° (bản trước -90° nên chỉ thấy nắp phẳng). Thử -78/-72 thân lộ quá dài (~0.6 ô) đè sang ô dưới. Cửa đóng vẫn tụt sâu (DoorExtraSink) nên cửa che kín như cũ. Chỉnh độ dài thân: InactiveTiltX / GridTiltX / lift trong AmmoBox.cs.
- 3.26b (user: "như 2 lớp đè nhau, nhìn kỹ mấy cái hố có viền"): (1) dải sáng ở mép dưới các vùng tối (AmmoGridController: lớp TrayWallLip nâng mép `raise`) là gờ giả 3D dựng cho camera nghiêng → bỏ (ty0 = y0), giờ vùng tối phẳng một lớp; (2) cột jelly 1.9 ô xuyên qua mặt khay → rút ngắn theo trục dài (InactiveLen 0.4, ActiveLen 0.6) và tăng nghiêng (-78 / -72) để dải thân lộ vẫn đủ; (3) còn dải thân dưới nắp thò nhẹ lên mép nắp hộp phía dưới (thân hộp trên đè sau hộp dưới) – giống ảnh mẫu, không phải lớp lỗi. Chưa xác định được 100% chỗ user chỉ: nếu vẫn thấy "viền" cần ảnh khoanh.

### 3.27 Tool chỉnh tay tư thế hộp khay: `AmmoLayoutTuner`
`Ammo/AmmoLayoutTuner.cs`: static `AmmoBoxTuning` (Pose Active/Inactive: tiltX, length, offset xyz theo ô, scale; SinkDepth; Version) + component `AmmoLayoutTuner` (ExecuteAlways, đã gắn sẵn lên GameManager của scene Gameplay). `AmmoBox.ApplyScale` đọc pose mỗi lần Version đổi → kéo thông số khi đang Play thì mọi hộp đổi ngay. Mặc định = giá trị đang chạy (active -72°/len 0.6/lift y 0.16, inactive -78°/len 0.4, sink 1.1). Đã kiểm: đặt activeOffset (0.3, 0.5) → hộp active dời +0.41 x, +0.46 y; inactive offset y -0.2 → -0.27. Thoát Play Unity trả Inspector về giá trị cũ → muốn giữ thì Copy Component khi đang Play rồi Paste Component Values sau khi dừng.

### 3.28 Bộ số tư thế user chốt + hộp mù theo cùng tư thế
- Mặc định mới (AmmoBoxTuning + AmmoLayoutTuner + giá trị đã lưu trên scene): active tilt -55 / len 0.6 / offset (-0.01, 0.69, 0) / scale 1.038; inactive tilt -78 / len 0.4 / offset (0, 0.07, 0) / scale 1.06; sink 1.1.
- Hộp mù `/qm`: mảnh "?" gốc (`Qmark Model`, mesh Cube.004 cao 4.8, shader URP/QMark) không đi theo thông số vì là nhánh riêng (jelly bị ẩn khi mù). Giờ: scale dọc theo `pose.length` như jelly, và `AlignQmarkToJelly` dời nó cho tâm nắp trùng đúng tâm nắp jelly (đã tính từ cùng tư thế/offset) → hộp mù ăn theo mọi thông số của AmmoLayoutTuner như hộp chưa active (kể cả active nếu hộp mù thông đường).

### 3.29 Màn thắng: kéo dài pháo hoa trước khi hiện bảng
`GameUI.PlayWinSequence`: sau khi chữ BOUNCE BLAST hiện xong, giữ màn logo + pháo hoa/confetti thêm `GameConfig.winFireworksTime` (mặc định 2.4 s, trước đây cứng 0.6 s) rồi mới hiện bảng Level/PERFECT/Continue. Đo: bảng chưa hiện ở 4 s kể từ lúc thắng, hiện ở 5 s; pháo hoa còn bắn tới ~4 s.

### 3.30 Bảng thắng: hết giật anim xu + dời xu lên
Nguyên nhân giật (đo log vị trí xu theo frame): (1) hoạt cảnh xu cộng dồn `unscaledDeltaTime` → frame không đều (8–24 ms) làm xu nhảy từng bước; (2) UiConfetti/UiSparkles tạo + Destroy hàng nghìn GameObject UI và mỗi mảnh nằm CHUNG Canvas với bảng thắng → mỗi frame cả canvas (TMP, ảnh, nút…) bị dựng lại lưới + GC spike.
Sửa: xu/chớp sáng tính theo thời gian tuyệt đối (`Time.unscaledTime`), ease-out cubic 0.38 s; confetti & sao lấp lánh dùng POOL (tái dùng mảnh, không Instantiate/Destroy) và nằm trong Canvas lồng riêng (overrideSorting) nên bảng không bị dựng lại theo từng mảnh. Đống xu dời lên `CoinLift = 45` (đơn vị canvas) để nằm trong vòng tròn bục.
Lưu ý khi đo: cửa sổ Editor không focus thì Unity hạ tốc độ khung (ProfilerRecorder chỉ ghi ~8 mẫu/s) – đo FPS phải để Game view hiện.

### 3.31 Khôi phục shader gốc từ GLSL (ghép khung .shader + công thức .glsl)
Phát hiện: 6 shader quyết định màu (TCP2 Hybrid Shader 2 / Directional / Directional Cube / Directional Cube (Outline), BounceBlast/CubeIsolatedOptimized, ELCAPITAN/ToonyColors/Default) trong `Shaders/Ripped` đều là CÙNG MỘT shader giả viết tay 161 dòng (đèn cứng (0.12,0.38,-0.92), nhân tông 1.05) – không phải bản dịch GLSL. File `.shader` của AssetRipper chỉ là dummy (Properties + frag trả trắng).
Làm:
- Nâng cấp `Tools/ShaderRipper/glsl_to_unity.py`: hỗ trợ GPU instancing theo variant `INSTANCING_ON` của bản build (khối `UnityInstancing_CubeProps`: `_CubeShadeOffset`, `_UseBakedCubeShadeOffset` → UNITY_INSTANCING_BUFFER + ACCESS_INSTANCED_PROP + multi_compile_instancing), chú thích variant ↔ file GLSL ↔ keyword. Khung Properties/Tags/state vẫn từ `_shaderlab.txt`/`_passes.json`.
- Chạy lại cho 5 shader có GLSL (giữ `.meta`/GUID → vật liệu tự trỏ đúng), 0 lỗi biên dịch. `ELCAPITAN/ToonyColors/Default` bị strip khỏi build → vẫn bản viết tay. Bản giả sao lưu ở `D:\toolGame\shader_backup_fake_20261009`.
- Shader thật nhạy hướng đèn: bỏ `ViewSpace.AlignLights` (xoay đèn thạch -20°) → với góc đèn gốc 5.7° hộp đậm màu như PLay2.jpg; xoay -20° thì nắp hộp bóng trắng.
Kết quả so ảnh gốc: hộp khay đậm, đều màu; gạch có khối sáng tối mềm + vệt bóng như gốc.
Còn lại: 13 dummy trùng tên ở `Assets/_Ripped/Shader/` + `Shaders/BounceBlast_Toon.shader` (không vật liệu nào dùng) nên xoá; code tô màu hộp (`AmmoBox.ApplyTint` sửa _HColor/_SColor/_Specular) viết cho shader giả – cần xem lại.

### 3.32 Màu nhạt / gạch "nhựa" so với game gốc (Lv44, ảnh user)
Đo HSV vùng bàn (pixel S>60): gốc S 199 V 233 | bản làm lại S 174 V 238. Vật liệu gạch (Purple/Black…) trùng 100% với ExportedProject; ambient Trilight & 2 đèn (màu, cường độ 0.585, góc) trùng scene gốc; post-process (URP Volume ColorAdjustments) không chạy ở Built-in RP → không phải nguyên nhân.
Nguyên nhân: đổi sang camera thẳng (3.25) xoay mọi khối thêm 20° về phía camera nhưng đèn giữ nguyên → mặt gạch hứng đèn trực diện hơn → sáng/bạc, vệt specular rộng ("nhựa").
Thử quét góc đèn (đo cùng điều kiện chụp): -20° → S 140 (tệ hơn); +10° S 188; **+20° S 196 V 225** (≈ gốc); +40° S 205 V 205 (tối). Thạch cũng vậy: +20° đậm, tươi như gốc.
Sửa (`GameManager.CreateCubeLight`): tách layer Cube khỏi đèn "Cubes and others" (đèn này còn chiếu khung bàn tĩnh – giữ nguyên) sang đèn riêng xoay +20° quanh X; đèn "Jellies" (chỉ layer Ammunation) xoay +20°. Kèm `BoardBlock`: giãn visual dọc ×1/cos20 + z theo hàng (bù ma trận ép dọc, chồng mép hàng như cảnh nghiêng gốc).
Ảnh so sánh: `D:\GitHub\Asset_MainGame_Bounce Blast\frames\compare_shader\` (side_final2.png, cube_light_sweep.png, jelly_sweep.png).

### 3.33 Xếp gạch khít & 3D như gốc (thay cho cách bù đèn ở 3.32)
Phân tích: game gốc đặt khối -90° (mặt trên → -Z) và camera nghiêng xuống 20° → thấy mặt trên + dải cạnh trên mỗi viên, các hàng chồng khít nhau, không lộ nền. Bản làm lại đặt -90° với camera thẳng → nhìn thẳng mặt, khe vát giữa các viên lộ nền.
Quy đổi chính xác sang camera thẳng: hướng khối trong hệ camera = Rx(-20)·Rx(-90) = **-110°**, đèn giữ nguyên góc gốc. Quét thử (-90/-110/-70 × đèn -20/0/+20) đo HSV: -110° + đèn gốc → S 198.5 / V 220 (gốc 199/233), hết khe, có dải sáng mặt trên. Áp: `BoardController.CreateVisual` model = -90 - LegacyPitch; `GameManager.CreateCubeLight` đèn gạch = góc gốc (bỏ +20° của 3.32). Đèn thạch vẫn +20° (khay đo riêng).
Kiểm Lv44 + Lv12 (khối chữ L) không lỗi nối. Còn khác: khối gộp lớn của gốc có viền sáng mảnh ở mép trên; bản làm lại phẳng do FlattenShading (cần thiết để khối L không lộ đường nối).

### 3.34 Viền sáng ngắt quãng ở mép/góc viên gạch
Ảnh gốc (zoom): mỗi viên là ô vuông bo góc kiểu "gối", viền sáng chạy theo mép trên rồi cong xuống ở 2 góc, góc giao 4 viên không lộ nền.
Nguyên nhân lệch (3 chỗ):
1. Prefab `Resources/prefabs/Cube.prefab` bị sửa (chưa commit) cho `CubeNew` xoay (10°,180°,0) – gốc là KHÔNG xoay (đối chiếu ExportedProject + git HEAD). Mặt vát lệch 10° nên specular thành 1 vạch thẳng giữa mặt. → trả về identity.
2. `FitVisual` co giãn từng trục để vừa ô → méo pháp tuyến mặt vát. → viên đơn dùng `FitVisualUniform` (co giãn đều như prefab).
3. Prefab gốc ×1.05; với camera thẳng góc bo tròn để lộ khe hình thoi → `CubeOverlap = 1.10`.
Mesh đúng là `pCube35` 30 đỉnh (bản 180 đỉnh là `MeshOfOldCube`, game không dùng). Đo: S 182 V 234 (gốc 199/233). Ảnh: `frames/compare_shader/cubenew_ov110_*.png`.
Còn khác: khối gộp lớn của gốc có viền sáng mép trên (bản làm lại phẳng do FlattenShading).

### 3.35 Mặt trên khối nhìn từ trên xuống (user: "nhìn như từ trên xuống, có mặt trên")
- Camera gốc xác nhận: ManagerScene/Main Camera ortho 17.5, nghiêng 20° (LevelCamera trong DefaultLevelScene là camera phối cảnh thiết kế, không dùng khi chơi). Prefab Cube gốc có `applyDefaultRotationOnInit: 1` → góc lấy từ `GameConfig.DefaultRotation` (static readonly, thân hàm bị strip → không đọc được số).
- Khối gộp chữ nhật bỏ FlattenShading (mất hết bóng mặt trên/vát) → `FlattenFrontFace`: chỉ dựng thẳng pháp tuyến mặt trước (+Y, n.y>0.5), giữ mặt cạnh/vát. Khối chữ L vẫn FlattenShading.
- Độ dày khối lớn 1.2 ô → 0.87×1.05 ô (= mesh gốc) cho dải mặt trên mảnh như gốc.
- Quét có hệ thống (model -90/-100/-105/-110 × đèn gạch 0/+10/+20, đo HSV bàn + khối cyan): tốt nhất **-110° + đèn +20°** → bàn S 195 V 222 (gốc 199/232). Đèn -20° (quy đổi lý thuyết) làm bạc màu (S ~145) → không dùng.
Còn lệch: khối gộp lớn nhạt hơn gốc (cyan S ~199 vs 231) – mesh khối gộp gốc do script CombinedCubic sinh lúc chạy (bị strip), bản làm lại giãn 9-slice từ pCube35.
- 3.35b (user chốt): xoay gạch về góc cũ **-90°** (mặt trên quay thẳng camera, Scene view không còn nghiêng), đèn gạch +20° (đèn gốc làm bạc: S 180→ dùng +20). Đo Lv44: bàn S 180 V 239 (gốc 199/232). Các sửa khác của 3.34–3.35 (prefab CubeNew không xoay, co giãn đều, chồng mép 1.10, FlattenFrontFace, độ dày 0.87) giữ nguyên.
- 3.35c (user: "xoay mặt phẳng lên trên"): đo mesh pCube35 – là hộp HỞ đáy & lưng, mặt phẳng lớn là -Z (mặt trước, 0.64) và +Y (mặt trên, 0.75), 4 mép vát. Hiểu đúng: khối gốc ĐỨNG THẲNG (model 0°), camera chếch 20° thấy mặt trước + dải mặt trên. Camera thẳng ⇒ model **-20°** (ngả về camera, mặt phẳng trên hướng lên). Đổi theo: 9-slice giãn x,y (mặt trước); FlattenFrontFace/FlattenShading theo -Z; JoinedCellMesh mặt phẳng x,y; nắn mặt trên các mảnh khối L theo pháp tuyến -Z. Đèn gạch +20° (quét 0/+20/+40 so gốc). Đo Lv44: bàn S 191 V 229 (gốc 199/232). Lv12 khối L không lộ nối.
- 3.35d (user: gạch to chưa 3D như gạch nhỏ): (1) trong `CreateVisual` nhánh khối nhiều ô có dòng đặt `model.localRotation = identity` (không do phiên này viết) → khối to đứng thẳng, mất dải mặt trên; đổi sang -20° như gạch nhỏ. (2) `FlattenFrontFace` ngưỡng z<-0.5 nuốt cả pháp tuyến mép vát (z≈-0.7) → z<-0.9. (3) Khối ghép L/T (JoinedCellMesh) dùng FlattenFrontFace thay FlattenShading → mép ngoài có vát. Kiểm Lv44/79/12: khối to có dải mặt trên + viền như gốc, khối T/L không lộ nối; màu khối to hơi đậm/tối hơn gốc.
- 3.35e (user: khối to xoay nguyên khối thì không khít): khối to/hòm/tường KHÔNG xoay nữa (model identity); `BoardController.ShearForView` xiên mesh: y += (z − zFront)·tan20, pháp tuyến ×Rx(-20) → nhìn thẳng thấy dải mặt trên & sáng tối như gạch 1 ô, mặt trước phẳng trong mặt bàn. Đo: mặt trước khối to z −0.31..−0.31 (phẳng), cùng dải với gạch nhỏ (−0.35..−0.21). Gạch 1 ô vẫn ngả -20° (nhỏ nên lệch không đáng kể).
- 3.35f (user: khối chữ T ghép từ nhiều ô không cùng cỡ → thừa chi tiết ở chỗ nối): khối gộp (CombinedCube, cả chữ nhật lẫn L/T) giờ là MỘT mesh liền `BoardController.CreateSolidVisual/BuildSolidMesh`: dò viền ngoài của mask (cạnh có hướng, bỏ cạnh chung) → đa giác vuông góc; mặt trước = đa giác thu vào 0.07 ô (cắt tai EarClip), dải vát 45° + vách sâu 0.4 ô chạy liên tục quanh viền; xiên y += z·tan20, pháp tuyến ×Rx(-20) như ShearForView. Cache theo hình dạng. Bỏ đường ghép mảnh cũ (JoinedCellMesh/9-slice không còn dùng cho khối gộp; hòm/tường vẫn dùng mesh cũ + ShearForView). Kiểm Lv44/79/12 không còn bậc nối.
- 3.35g (user: mặt trên khối lớn chưa hở nhiều như gạch 1 ô): SolidDepth 0.4 → 1.1 ô (dải mặt trên ≈ depth·tan20 ≈ 0.4 ô, gạch 1 ô sâu 1.07 ô ngả 20°), vát 0.08 → 0.14 ô. ("Sọc tách" thấy ở 3.35f là lỗi ghép ảnh so sánh chồng vùng, không phải lỗi game.)

### 3.36 Thua oan khi chỉ còn hòm gỗ (Lv79)
Nguyên nhân: 3.18 cho bi trúng hòm KHÔNG tiêu hao + loại hòm khỏi "mục tiêu" (`IsTarget`) → hết hộp đạn, chỉ còn hòm trên bàn thì `CheckDeadlock` (AnyBallHasTarget → HasTargetFor) thấy không bi nào có mục tiêu → Lose dù bi vẫn đang đập hòm.
Sửa (`BoardController`): `HasTargetFor` coi hòm còn máu (IsFreeHit && IsDestructible) là việc còn dang dở; `TryGetLineOfSightTarget` khi màu bi không còn khối cùng màu thì ngắm hòm gỗ (aimCrates) để bi phá hòm thay vì nảy vô định.
Kiểm: Lv79 xoá hết gạch chừa 4 hòm (560 máu), bắn 3 hộp → hòm vỡ hết (~1200 frame ×3), state vẫn Playing, gạch trong hòm lộ ra chơi tiếp.
- 3.29b (user: thêm 2 tiếng pháo ở màn thắng): `GameUI.PlayWinSequence` phát thêm `Sfx.Confetti` 2 lần trong lúc giữ màn pháo hoa (ở 25% và 60% của winFireworksTime), ngoài tiếng pháo khi chữ BLAST bật lên.

### 3.37 VFX vỡ gạch tung toé, cùng màu
Prefab gốc `Block Break Particle Variant`: hạt dạng mesh Cube, vật liệu cố định "Blue" (shader TCP2 không đọc màu hạt → startColor vô tác dụng, mảnh luôn xanh), chỉ 5 hạt, tốc độ 1–2, sống 0.4s.
Sửa: `Vfx.Spawn(..., material)` gán vật liệu cho ParticleSystemRenderer; `Vfx.Tune(prefab, action)` chỉnh thông số khi tạo instance trong pool. `BoardController.EnsureBreakTune`: burst 16–22, cầu bán kính 0.25, tốc độ 5–11, sống 0.45–0.85s, trọng lực 2.2, xoay 3D ngẫu nhiên + xoay theo thời gian, nhỏ dần cuối đời, size 0.28–0.55 (× cỡ ô); vật liệu = `ColorLibrary.CubeMat(màu gạch)`. Ảnh: frames/compare_shader/vfx_break2.png.
- 3.37b (user: "thô bạo như bê tông, nên nhẹ nhàng như thạch"): hạt hình cầu (`New-Sphere.fbx` dựng sẵn), burst 8–12, tốc độ 1.8–4.2 có hãm (limitVelocity 3.5, dampen 0.15), sống 0.4–0.7s, trọng lực 0.9, không xoay; cỡ phồng–xẹp theo thời gian 0.55→1.05→0.92→0 → mảnh bật mềm như thạch.

### 3.38 Bàn chơi bị HUD (Lv, Settings) che trên máy tai thỏ
Nguyên nhân: `FitCamera` đặt đỉnh khung máng (y 9.12) đúng mép trên màn hình – đo từ ảnh gốc chụp máy KHÔNG tai thỏ. Trên máy có tai thỏ (VD iPhone 12 Pro Max: 1284x2778, safeArea trên 141 px) HUD bị đẩy xuống dưới vùng an toàn nên đè lên khung.
Sửa (`GameManager.FitCamera`): lấy phần che phía trên `t = (Screen.height − safeArea.yMax)/Screen.height` (kẹp ≤ 20%), nới vùng nhìn `size /= 1 − t` và GIỮ NGUYÊN mép dưới thế giới → khung máng lùi xuống ngay dưới HUD (như ảnh gốc), cả cảnh co ~5%, khay đạn/nút trợ giúp không bị đẩy xuống. Tự fit lại khi safeArea đổi (xoay máy, đổi device Simulator). (Bản thử chỉ dời camera thì hàng khay cuối Lv79 bị nút trợ giúp che → bỏ.) Ảnh: Captures/hud_fit2.png.
- 3.38b (user: "board to hơn xíu, chạm đến viền nút setting"): `FitCamera` ưu tiên bề ngang như gốc – nửa bề ngang 5.98 ⇒ khung máng phủ ~90% bề ngang, mép ngoài chạm nút Settings (đo ảnh gốc Lv44: 773/858 = 90%); đỉnh khung đặt ngay dưới phần tai thỏ; chỉ máy bè mới bị kẹp theo chiều dọc (`MinHalfHeight` 11.6). Hệ quả: đáy thế giới thay đổi theo máy → `AmmoGridController.AreaBottomOverride` = mép trên hàng nút booster THẬT (`GameUI.BoosterTopScreenY` → `ScreenToWorldPoint` ở mặt hộp z −0.6, + khe 0.25), khay co ô cho vừa, không hàng nào bị nút che. Trên iPhone 12 Pro Max (màn thấp hơn máy chụp gốc + tai thỏ) ô khay nhỏ hơn gốc: Lv79 1.16, Lv44 0.96 (gốc ≈ 1.47). Ảnh: Captures/hud_fit4.png (Lv79), hud_fit5.png (Lv44).
- 3.38c (user: "mép trên của máng ở đúng đường vàng" = tâm hàng HUD; Ammo to hơn): bỏ việc lùi theo tai thỏ. `FitCamera` đặt mép trên NGOÀI khung máng (y thế giới 8.43, đo viền sáng đầu tiên trên ảnh chụp) ngang tâm nút Settings (`GameUI.HudCenterScreenY`) → Lv/Settings đè lên góc khung như game gốc, đúng mọi máy vì HUD tự theo vùng an toàn. Board dời lên ~0.7 đv → khay thêm chỗ: ô Lv79 1.16 → 1.39, Lv44 0.96 → 1.16, không hàng nào bị nút booster che. Ảnh: Captures/hud_fit7.png (Lv79), hud_fit8.png (Lv44).
- 3.38d (user: "ammo bé quá, to hơn, không che thanh capacity là được"): đáy vùng khay = TÂM hàng nút booster (`GameUI.BoosterMidScreenY`) thay vì mép trên → hàng cuối được chìm tối đa nửa dưới vào dải booster như ô "/awl" của game gốc; mép trên khay vẫn ngay dưới thanh Capacity. Galaxy S10e (1080x2280): ô Lv74 ≈0.95 → 1.32, Lv44 → 1.32 (gốc ≈1.45). Ảnh: Captures/s10e_74.png, s10e_44.png.
- 3.38e (user: "booster che mất ammo… chỉnh board bé lại xíu, vẫn giữ đường kẻ vàng, ammo dịch lên, dễ bấm, không quá bé, không đè nhau"): thay 3.38d bằng bố cục tự cân theo level trong `FitCamera`:
  1. Mép trên ngoài khung máng (`FrameTop` 8.43) luôn ngang tâm nút Settings.
  2. Tính chiều cao khay cần cho ô `AmmoTargetCell` = 1.45 (gốc Lv44) với số hàng thật (`AmmoGridController.VisibleExtent`): khoảng HUD→mép trên booster phải chứa FrameTop→ammoAreaTop + hàng·1.45 + khe 0.2.
  3. Không đủ thì thu nhỏ cả cảnh (nửa bề ngang từ 5.98 lên tối đa ×1.3), đỉnh khung vẫn ở tâm HUD → board nhỏ đi vừa đủ, khay dịch lên theo.
  4. `AmmoAreaBottom` = mép trên booster + khe (bỏ kẹp −16 của config, chính nó làm ô kẹt ở 1.35) → không ô nào bị booster đè.
  Galaxy S10e: Lv76/44/74 đều ô 1.45; board Lv76 ≈ 87%, Lv44/74 ≈ 82% bề ngang. Ảnh: Captures/bal_all.png.

### 3.39 Hộp "lơ lửng" khi hết sức chứa bị méo (mesh_lỗi.mp4)
Lỗi: `AmmoBox.HoverRoutine` cũ kéo giãn mesh hộp bằng scale không đều (ngang 0.7, dọc 2.4) + xoay −38° → thành que mảnh, bẹp, khác hẳn hộp lúc chơi; nằm cùng mặt phẳng khay nên còn bị thanh Capacity/mặt khay cắt.
Gốc (lose_coche f528–540, frames/lose_coche/hover_bar.png): hộp thành ỐNG THẠCH dày đúng bề ngang hộp, bo tròn 2 đầu, ngả ~30° sang phải, dài ~3 bề ngang, rời hẳn miệng hố, đứng yên dưới panel Out Of Capacity.
Sửa: dùng chính thân gắn xương `AmmoFlightTube` (như lúc bay): quỹ đạo hố → trồi 0.45 ô → chéo `HoverAngle` 28°, đầu vọt ra trước, đuôi theo kiểu ease-out-back (bật nhẹ như thạch), bề ngang giữ thể tích ±10%, dài `HoverLength` 3·bề ngang; ống nằm ở z −2.5 (trước khay & Capacity). Play On → `BeginLaunch` huỷ ống và bay như thường. Ảnh so sánh: Captures/hover_cmp.png (trái gốc, phải bản làm lại).

### 3.40 Thanh đậu trong máy xay dài hơn
User: "độ cao của ammo khi vào máy xay hơi ngắn… dài hơn, xay lâu hơn thì feeling oke hơn". `AmmoBox.DockHeightRatio` 2.0 → 2.6 (chiều cao thanh đậu = 2.6 × bề ngang lòng máy xay; đáy vẫn dưới tâm lưỡi cắt 0.3W) → phần nhô trên khung dài thêm ~35% (1.7W → 2.3W), thanh lún dần theo số bi còn lại nên nhìn như xay lâu hơn. Đường bay (`FlyRoutine` DockLen) và khoảng xếp hàng chờ trên dock (`ConveyorLane.WaitPosition`, step = W·(ratio+0.15)) tự theo hằng số này. Thời gian xay thực (= số bi × laneBallSpacing / laneSpeed ≈ 0.05 s/bi) KHÔNG đổi.
Kèm: `ThemeApplier.ApplyGround` tự tạo MaterialPropertyBlock nếu Apply chạy trước Init (lỗi null khi LoadLevel sớm ngay lúc vào Play làm LoadLevel dừng giữa chừng).
