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

## 4. Refactor: đưa hằng số tinh chỉnh ra Inspector

### 4.1 Vì sao
Dự án đang nghiêng hẳn về dựng object bằng code (47 chỗ `new GameObject()` / 14 chỗ `Instantiate`), nên nhiều giá trị tinh chỉnh nằm rải rác dưới dạng `const` trong code: muốn đổi phải sửa code + chờ compile. Chuẩn phổ biến trong ngành là prefab + dữ liệu; bước rẻ nhất và không rủi ro là đưa các con số ra `GameConfig` (ScriptableObject đã có sẵn trong Inspector).

Đo trước khi làm, để trả lời lo ngại "tách ra có lag không": dựng 2000 object bằng code 16.7 ms, bằng `Instantiate` 14.1 ms → prefab KHÔNG chậm hơn, còn nhanh hơn chút. Thời gian load level (97–339 ms) không nằm ở việc tạo object mà ở sinh mesh khối gộp + dọn level cũ.

### 4.2 Đã chuyển sang GameConfig (20 giá trị, có Header + Tooltip)
| Nhóm | Trường |
|---|---|
| Bố cục màn hình | `frameTopY` 8.43, `viewHalfWidth` 5.98, `viewMaxZoomOut` 1.3, `viewMinHalfHeight` 11.6, `ammoTargetCell` 1.45, `ammoBoosterGap` 0.2 |
| Đậu máy xay | `dockHeightRatio` 2.6, `dockBottomRatio` 0.3 |
| Lơ lửng hết sức chứa | `hoverAngle` 28, `hoverLength` 3, `hoverTime` 0.38, `doorExtraSink` 1.6 |
| Khay đạn | `trayPanelPad` 0.10, `trayPanelRadius` 0.22, `traySlotSize` 0.92, `traySlotRadius` 0.2 |
| Gạch | `blockBevel` 0.14, `blockDepth` 1.1, `cubeOverlap` 1.10, `columnWidthRatio` 0.6 |

Ghi chú kỹ thuật:
- `AmmoBox.DockHeightRatio/DockBottomRatio` từ `public const` thành property đọc config; giữ `DefaultDockHeightRatio` cho `ConveyorLane.WaitPosition` (có thể gọi trước `Init`).
- `GameManager.AmmoBoxShift` trước hardcode −0.218, nay tính từ `ViewSpace.LegacyShift(-0.6)`.
- `BoardController.BuildSolidMesh` (static) nhận thêm tham số bevel/depth; khoá cache `_solidMeshes` thêm bevel+depth để chỉnh trong Inspector là mesh dựng lại ngay.
- Xoá `AmmoBox.SinkDrop` (khai báo nhưng không nơi nào dùng).

### 4.3 Kiểm chứng
- Compile sạch; load 7 level (44, 74, 76, 79, 2, 1, 10) không lỗi console.
- Bố cục giữ nguyên: Lv44 ô khay 1.45 / orthoSize 13.87 / camY −5.332, trùng số trước refactor. Ảnh: Captures/refactor3_cmp.png (trái trước, phải sau — khác duy nhất là badge booster do state khác phiên).
- Thử đổi `blockBevel` 0.14 → 0.45 rồi load lại: dải vát khối gộp dày lên rõ (Captures/refactor3_bevel_cmp.png) → chỉnh Inspector ăn ngay, cache mesh tự làm mới. Đã trả về 0.14.
- Thời gian load không xấu đi: 94–125 ms (trước 97–339 ms; chênh lệch chủ yếu do lần đo trước là lần dựng mesh đầu tiên, chưa có cache).

## 5. Refactor mục 2: thanh booster thành prefab (dùng art gốc)

### 5.1 Trước
`GameUI.BuildBoosters` dựng toàn bộ bằng code, và quan trọng hơn: **sprite nút là tự vẽ** – `MakeRing()` sinh texture vòng tròn tím viền vàng, `MakeTriangleSprite()` sinh tam giác cảnh báo. `UiSkin.boosterFrame`/`plusBadge` có khai báo nhưng trỏ sai vào `Circle.asset` (menu Setup khớp tên lỏng) và code không dùng tới.

### 5.2 Tìm lại art gốc
`Assets/_Project/UI/Prefabs/Boosters.prefab` (1539 object, đã tách từ ManagerScene nhưng chưa nối vào game) chứa nút gốc dạng **world-space SpriteRenderer**, kèm cả shop/tutorial/currency. Lấy ra được các sprite thật:
| Vai trò | Asset gốc |
|---|---|
| Nút (khung + icon trong MỘT sprite) | `_Ripped/Sprite/Magical Hand`, `Hammer`, `Shuffle`, `Capacity` |
| Ô số còn lượt | `_Ripped/Sprite/Number` |
| Nút "+" khi hết lượt | `_Ripped/UI/Sprite/Add_BTN (1)_mir` (vẽ bằng `MirroredImage`, lật ngang) |

Mũi tên cảnh báo: `Arrow_0` trong "Canvas Of Booster Arrows" là mũi tên cong, **không phải** cái dùng ở đây – đối chiếu `lose_coche.mp4` f486–510 thì nó là **tam giác vàng chỉ xuống**, đúng như hàm tự vẽ cũ. Trong `_Ripped` không có sprite tam giác nào khớp, nên xuất hàm vẽ cũ ra asset `Assets/_Project/UI/Sprites/BoosterWarnArrow.png` để nằm trong prefab (sửa/thay được), không vẽ trong code nữa.

### 5.3 Sau
- Prefab mới `Assets/_Project/UI/Prefabs/BoosterBar.prefab`:
  `BoosterBar / Strip(+GoldLine) / Buttons / {MagicHand, Hammer, Shuffle, Capacity} / {Badge(+Count), BadgeAdd, WarnArrow}`
- `UiSkin`: 6 trường sprite booster → còn **1 trường** `boosterBarPrefab`. Art nằm trong prefab, không qua UiSkin nữa.
- `GameUI.BuildBoosters` chỉ Instantiate + tìm con theo tên + gắn `onClick`; bỏ `MakeRing` và `MakeTriangleSprite`. `_boosters` đổi từ tuple sang class `BoosterUi` để giữ thêm `badgeAdd`.
- `SetBooster`: còn lượt → hiện `Badge` (ô số); hết lượt → hiện `BadgeAdd` (nút "+" xanh gốc). Trước đây tô màu xanh đè lên sprite tím nên ra sai màu.
- `GameplaySetup.cs` (menu Setup) cập nhật theo.
- GameUI: 1009 → 972 dòng; `new GameObject()` trong GameUI 7 → 6; toàn dự án 47 → 46.

### 5.4 Kiểm chứng
- Compile sạch, load Lv44/79/2 không lỗi console.
- Bấm nút: `onClick` bắn đúng `BoosterType.Hammer`.
- Dải nền vẫn đổi tông theo độ khó: Hard đỏ (0.745, 0.165, 0.173), SuperHard navy (0.149, 0.157, 0.278), Normal tím (0.278, 0.180, 0.886).
- Mũi tên cảnh báo bật đúng ở Hammer + Capacity khi sức chứa đầy.
- Ảnh: Captures/booster_cmp2.png (trước/sau), Captures/booster_vs_goc.png (so với video gốc).

### 5.5 Tách luôn Canvas (user: "ủa nhưng mà phần canvas UI đâu")
Bước 5.3 mới tách thanh booster; bản thân `OverlayCanvas` (Canvas + CanvasScaler + GraphicRaycaster) cùng Vignette và Toast vẫn `new GameObject()` trong `BuildOverlay` → trong Editor vẫn không thấy/sửa được lớp UI.
- Prefab mới `Assets/_Project/UI/Prefabs/OverlayCanvas.prefab`: `OverlayCanvas / BoosterBar (prefab lồng) + Vignette + Toast`. `BoosterBar` lồng dạng prefab nên sửa BoosterBar.prefab là cả hai cùng đổi.
- `UiSkin`: `boosterBarPrefab` → `overlayCanvasPrefab`; bỏ luôn trường `vignette` (sprite đã nằm trong prefab).
- `BuildOverlay` giờ chỉ Instantiate + `UseCamera`/`SetupScaler` (camera không serialize được) + tìm `Vignette`/`Toast` theo tên; `BuildBoosters` → `BindBoosters` (bar có sẵn trong prefab, không tạo nữa).
- **Sửa luôn chỗ lệch ở 5.5 cũ**: trong prefab, Vignette đặt SAU BoosterBar nên vignette đỏ phủ lên cả hàng nút như game gốc (lose_coche f498). Trước đó vignette tạo trước nên nằm dưới, nút vẫn tím sáng khi sức chứa 100%. Ảnh: Captures/overlay_vs_goc.png.
- Kiểm chứng: trạng thái thường không đổi (Captures/overlay_normal_cmp.png); popup Settings vẫn mở đúng; load Lv44 không lỗi console.

### 5.6 Còn dựng bằng code trong GameUI (5 chỗ, đều có lý do)
| Dòng | Việc | Vì sao giữ |
|---|---|---|
| 76 | `EventSystem` | chỉ tạo khi scene chưa có; là singleton hạ tầng, không phải UI |
| 439 | canvas bọc popup | bọc prefab popup gốc cho vừa màn hình, không có hình ảnh riêng |
| 773, 948, 957 | helper `Img()` / `Txt()` / node rỗng | dùng cho các phần còn lại (HUD warn, coin bay, confetti…) – chưa tách |
Tam giác cảnh báo gốc vẫn to hơn bản làm lại một chút và mũi gần sát mép nút hơn.

### 5.7 Số liệu
GameUI 1009 → 971 dòng; `new GameObject()` trong GameUI 7 → 5, toàn dự án 47 → 45. Prefab UI: thêm `BoosterBar.prefab`, `OverlayCanvas.prefab`, sprite `BoosterWarnArrow.png`.

### 5.8 Đưa UI vào Hierarchy (user: "phần canvas trên hierarchy chưa có mà")
5.5 mới tạo prefab dạng FILE; scene vẫn chỉ có `Systems/GameManager`, mở Editor không thấy UI đâu. Nguyên nhân gốc: `GameManager.Awake` tạo mọi hệ thống bằng `CreateChild<T>()` nên không hệ nào có tham chiếu Inspector.
Sửa:
- `GameUI.Spawn(prefab, name)` đổi thành: **có sẵn con đúng tên trong scene thì dùng luôn**, chưa có mới Instantiate từ prefab. Áp cho cả OverlayCanvas, HUD và CapacityBar → scene trống vẫn chạy được như cũ.
- `GameManager` thêm trường `public GameUI ui` (Header "Scene"): gán thì dùng cái trong scene, bỏ trống thì tự tạo như trước.
- Scene `Gameplay.unity` nay có:
```
Systems
  GameManager        (ui -> Systems/UI)
  UI                 [GameUI]
    OverlayCanvas    <prefab>  (BoosterBar + Vignette + Toast)
    HUD (GameCanvas) <prefab>
    CapacityBar      <prefab>
```
Cả 3 là prefab instance nên sửa prefab là scene theo, và ngược lại chỉnh trong scene thì Apply ngược lại prefab được.
- Kiểm chứng: chỉ 1 GameUI và 1 OverlayCanvas lúc chạy (không nhân đôi), Lv44 hiện đúng như trước (Captures/hier_cmp.png), console sạch, scene đã lưu.

### 5.9 Thanh CAPACITY hỏng trong Edit mode (user: "sao lại có thanh CAPACITY bị lỗi kìaaa")
Sau 5.8, mở Editor thấy thanh CAPACITY trôi giữa màn hình, chữ bị đè, và cả màn phủ một lớp xám. Nguyên nhân: `CapacityBar.prefab` vẫn giữ nguyên trạng thái gốc, còn code mới chỉnh nó lúc chạy:
- root ở (5.34, −12.10, −0.41) rot X 270 scale 0.726 → nằm sai chỗ, dựng ngang;
- `Canvas Base/Image` = lớp xám phủ toàn màn của prefab gốc, code `SetActive(false)` lúc chạy;
- `endless icon (1)` bật sẵn → đè lên chữ CAPACITY, code cũng tắt lúc chạy.
Sửa: **nung các giá trị đó thẳng vào prefab** (root (5.30, −6.77, −0.60) rot 0 scale 0.62; `Canvas` pos 0 rot 0 scale y ×0.8; tắt `Canvas Base` và `endless icon (1)`), rồi **bỏ phần đặt tư thế khỏi `BuildCapacityBar`** – giữ lại đúng thứ không serialize được là `Canvas.worldCamera` + `sortingOrder`. Tránh được cả rủi ro code nhân scale y ×0.8 lần thứ hai lên giá trị đã nung. Instance trong scene còn override tư thế cũ nên phải đồng bộ lại và lưu scene.

**Lỗi thứ hai lòi ra khi kiểm chứng:** `FitCamera` đo vị trí hàng HUD và hàng booster TRÊN MÀN HÌNH, nhưng `Start()` gọi `LoadLevel` ngay khung hình đầu, lúc Canvas chưa có kích thước thật → đo sai, nhánh thu nhỏ không chạy, bàn chơi bị nhỏ hơn mức cần (Lv44: orthoSize 12.624 / ô khay 1.22 thay vì 13.871 / 1.45) và chỉ đúng lại khi đổi level. Sửa: `Start()` thành coroutine, `yield return null` một khung hình rồi mới `LoadLevel`. Kiểm chứng: vào Play thẳng Lv44 giờ ra đúng 13.871 / 1.450 ngay từ đầu.

Ảnh: Captures/capfix_cmp.png (Edit mode trước/sau, và Play Lv44).
Còn lại ở Edit mode: lòng bàn và nền dưới có màu xám xanh vì `ThemeApplier` chỉ tô màu khi nạp level — vào Play là đúng màu.

## 6. Refactor mục 1: tư thế hộp đạn chuyển sang GameConfig

### 6.1 Vấn đề
Tư thế hộp trong khay (nghiêng / dài cột jelly / dịch / cỡ / độ lún) nằm ở biến **static** `AmmoBoxTuning`, được component `AmmoLayoutTuner` (gắn trên GameManager) ghi đè. Chính doc của component đã mô tả chỗ đau: *"Thoát Play thì giá trị trên Inspector bị Unity trả về: muốn giữ thì Copy Component trong lúc Play rồi Paste Component Values sau khi dừng"*. Tức là mỗi lần tinh chỉnh phải chép số thủ công, rất dễ mất.

### 6.2 Sửa
- `GameConfig` thêm lớp `[Serializable] AmmoPose` (tiltX / length / offset / scale, đủ Range + Tooltip) và 3 trường: `ammoActivePose`, `ammoInactivePose`, `ammoSinkDepth`. Mặc định = bộ số user đã chốt (Docs 3.28).
- `GameConfig.OnValidate()` gọi `AmmoBoxTuning.Bump()` → kéo thanh trượt là hộp dựng lại tư thế ngay.
- `AmmoBoxTuning` tách sang file riêng `Ammo/AmmoBoxTuning.cs`, không còn giữ dữ liệu: chỉ đọc từ `Config` (GameManager gán trong Awake), có giá trị dự phòng nếu chưa gán.
- `AmmoLayoutTuner.cs` thành stub rỗng (giữ lại để scene cũ không báo missing script); component đã gỡ khỏi GameManager trong scene. **Xoá được file này nếu không cần.**
- Vì là ScriptableObject asset chứ không phải component trong scene, Unity KHÔNG hoàn tác khi thoát Play.

### 6.3 Kiểm chứng
- Compile sạch; load Lv44/74/79/2 không lỗi console.
- Giá trị khởi đầu khớp bộ đã chốt: active tilt −55 / len 0.6 / off (−0.01, 0.69, 0) / scale 1.038; inactive tilt −78 / len 0.4 / off (0, 0.07, 0) / scale 1.06; sink 1.1.
- Đổi lúc đang Play (tilt −55 → −80, len 0.6 → 1.1): hộp đổi ngay trên màn (Captures/tuner_cmp.png).
- **Thoát Play: giá trị vẫn là −80 / 1.1** → đúng mục tiêu, không phải Copy/Paste Component nữa. Sau đó đã trả về bộ số cũ và chụp đối chiếu khớp hoàn toàn (Captures/tuner_restore_cmp.png).

### 6.4 Việc ngoài ý muốn
Khi dọn script hỏng, mình đã gỡ luôn một MonoBehaviour mồ côi trên `KPConveyorSlot_Left` (script guid 45ead289…, file không còn trong dự án từ trước). Nó vốn đã hỏng nên không chạy gì, nhưng đây là thay đổi user không yêu cầu — khôi phục được bằng git nếu cần.

### 6.5 Chưa làm trong mục 1
Khay đạn (mặt khay, miệng hố, sàn giếng trong `AmmoGridController`) vẫn dựng bằng mesh sinh lúc chạy. Hình dạng phụ thuộc ô nào có trong level nên không gói thành prefab được, giống mesh khối gộp trên bàn. Các thông số hình khối của khay đã đưa ra Inspector ở mục 4.

### 6.6 Thông số miệng hố khay (user hỏi)
`GameConfig` → mục **Khay đạn – hình khối**, 7 thông số, chỉnh xong nạp lại level là thấy (hình khối khay dựng lúc nạp level, không đổi tức thì như tư thế hộp):
| Trường | Mặc định | Tác dụng |
|---|---|---|
| `traySlotSize` | 0.92 | cạnh miệng hố so với ô khay; nhỏ đi = hố bé, gờ giữa 2 hố dày lên |
| `traySlotRadius` | 0.2 | bo góc miệng hố (0 = góc vuông) |
| `trayHoleTopInset` | 0 | hạ mép TRÊN của miệng hố xuống |
| `trayPanelDepth` | 0.35 | độ sâu mặt khay so với mặt hộp |
| `trayHoleDepth` | 3.0 | độ sâu đáy giếng – phải sâu hơn hộp đã lún + cửa đóng, không thì hộp xuyên đáy |
| `trayPanelPad` | 0.10 | viền mặt khay chìa ra ngoài ô |
| `trayPanelRadius` | 0.22 | bo góc ngoài mặt khay |

Hai cái đầu đã đưa ra ở mục 4; `trayHoleTopInset`, `trayPanelDepth`, `trayHoleDepth` bổ sung lần này (trước là `const HoleTopInset / TrayPanelZ / TrayFloorZ`). Dọn luôn 2 biến chết trong `AmmoGridController`: `lips` (MeshBuilder khai báo không dùng) và `lipH`.

Kiểm chứng: đổi `traySlotSize` 0.92 → 0.68 và `traySlotRadius` 0.2 → 0.45, nạp lại Lv44 thì miệng hố nhỏ và tròn hẳn (Captures/hole_cmp.png); trả về giá trị cũ thì khớp lại như ban đầu (Captures/hole_restore_cmp.png). Console sạch.

Chưa đưa ra Inspector: dải thạch đọng ở mép trên hố và các sợi nhỏ giọt trong `AmmoHoleLid` (`band = cell*0.09`, `DripIn`, `LidDelay`, `LidTime`) – đó là hiệu ứng nắp hố khi hộp bay đi, không phải hình khối miệng hố.

### 6.7 Đưa nốt hằng số tinh chỉnh còn lại ra GameConfig
Sau mục 4 và 6.6 vẫn còn một loạt `const` chỉ sửa được bằng cách mở code. Đưa nốt 13 cái ảnh hưởng tới hình ảnh / cảm giác:

| Nhóm | Trường | Mặc định | Trước đây |
|---|---|---|---|
| Bi | `ballMinVerticalSpeed` / `ballDeflectDeg` / `ballAimMinBounces` | 1.6 / 8 / 3 | `Ball.MinVerticalSpeed / DeflectDeg / LosMinBounces` |
| Máng | `laneArrowSpacing` / `laneArrowSpeed` | 1.25 / 1.5 | `ConveyorLane.ArrowSpacing / ArrowSpeed` |
| Máy xay | `crusherSpinSpeed` / `crusherShakeMagnitude` / `crusherShakeFrequency` / `crusherShakeDamping` | 800 / 0.06 / 28 / 6 | `CrusherFx` const |
| Nắp hố | `holeLidBand` / `holeLidDripTime` / `holeLidDelay` / `holeLidTime` | 0.09 / 0.18 / 0.55 / 0.22 | `AmmoHoleLid.DripIn / LidDelay / LidTime` + `band` |

Cách nối config: `Ball` đọc qua `_owner.Config` (BallManager đã giữ sẵn); `ConveyorLane` dùng `_config` sẵn có; `CrusherFx` thêm trường `public GameConfig Config` do `ConveyorSystem` gán; `AmmoHoleLid.Init` nhận thêm tham số `GameConfig`. Tất cả đều có giá trị dự phòng nếu config null.

Kiểm chứng: compile sạch, Lv44 chạy 40 bi không lỗi console; `CrusherFx.Config` và `AmmoHoleLid._config` đều trỏ đúng GameConfig lúc chạy. Test lưỡi cắt: đặt `_spinTimer` dài rồi so 2 trạng thái — `crusherSpinSpeed = 0` thì góc lưỡi đứng yên (13.378 → 13.378), `= 800` thì quay (13.378 → 327.414). (Hai phép đo trước đó không hợp lệ: lưỡi chỉ quay khi `_spinTimer > 0`, và `localEulerAngles` bị cuộn nên không đo được độ/giây.)

**Lưu ý:** trong lúc làm mục này, giá trị trong GameConfig.asset đã bị user chỉnh tay (active −68 / 0.62, inactive −80.2 / 0.52, traySlotSize 0.9 — khác mặc định −55 / 0.6, −78 / 0.4, 0.92). Đã GIỮ NGUYÊN, không ghi đè. Con số mặc định trong bảng trên là giá trị khởi tạo trong code, không phải giá trị hiện tại của asset.

## 7. Mỗi prefab một script quản lý riêng (user yêu cầu)

### 7.1 Ý
Trước đó prefab chỉ là "vỏ": `GameUI` ôm hết logic rồi đi tìm con bằng `transform.Find("tên")` lúc chạy. Cách chuẩn trong nghề là **mỗi prefab có script của chính nó**, giữ tham chiếu con bằng Inspector (`[SerializeField]`) và phơi ra API; script cha chỉ ra lệnh. Lợi: đổi tên/đổi chỗ con trong prefab không làm vỡ code, và nhìn Inspector là biết prefab cần những mảnh nào.

### 7.2 `BoosterBarView` (trên gốc BoosterBar.prefab)
- Danh sách `Entry` (một dòng mỗi nút): `type`, `frame`, `button`, `badge`, `count`, `badgeAdd`, `warnArrow` – kéo thả trong Inspector.
- Tự lo: gắn `onClick` (Awake), đổi số lượt / nút "+" xanh (`SetBooster`), tông dải nền (`SetStripColor`), mũi tên nhấp nhô (`SetWarn` + `LateUpdate`).
- Phơi `ButtonsRow` cho `GameManager.FitCamera` đo mép trên hàng nút.
- `Validate()` trả về danh sách ô tham chiếu còn trống → GameUI log lỗi rõ ràng ngay khi dựng, thay vì null lặng lẽ.
- Mũi tên: chỉ Hammer & Capacity giữ `WarnArrow`, 2 nút kia xoá hẳn node thừa trong prefab (trước đây tạo đủ 4 rồi Destroy 2 lúc chạy).

### 7.3 `CapacityBarView` (trên gốc CapacityBar.prefab)
- Tham chiếu: `worldCanvas`, `fill`, `label`, `infiniteIcon`.
- Tự lo vẽ % / chữ CAPACITY / icon vô hạn / màu thanh (`SetFill`), kể cả mẹo chỉ dựng chuỗi khi số % đổi.
- `Bind(camera, sortingOrder, font, material)` nhận những thứ không serialize được.

### 7.4 GameUI gọn lại
Bỏ hẳn: lớp nội bộ `BoosterUi`, dict `_boosters`, `_boosterStrip`, `_boosterBar`, `_warnArrows`, `_warnOn`, `_capacityRoot`, `_capacityFill`, `_capacityText`, `_capacityInfinite`, `_capacityKey`. `SetBooster`/`SetCapacity` nay chỉ chuyển tiếp lệnh. GameUI: 971 → 879 dòng.

### 7.5 Kiểm chứng
Compile sạch; Lv44 chạy không lỗi console; `Validate()` của cả hai view trả "đủ tham chiếu" lúc chạy. Bấm nút Shuffle → sự kiện bắn đúng `BoosterType.Shuffle` và nút chuyển sang badge "+" xanh. Sức chứa đầy → mũi tên bật đúng ở Hammer + Capacity, dải nền đúng tông Hard (0.745, 0.165, 0.173). Bắn 1 hộp → thanh hiện "20%", fillAmount 0.200, màu xanh lá (Captures/view_cmp.png, view_capacity_c.png).

Ghi chú quy trình: khi nối tham chiếu bằng script Editor, chuỗi tự kiểm tra kiểu `"Canvas="+(cv!=null)` in ra False dù thực tế đã nối đúng — phải đọc lại `SerializedObject` của asset đã lưu mới là bằng chứng tin được.

### 7.6 Chưa làm
`GameUI` còn 36 chỗ `Find(...)`, gần hết nằm ở **HUD và 4 popup gốc** (GameCanvas, Fail, Success, Settings, OutOfCapacity). Đó là prefab rip nguyên bản, tên node do game gốc đặt; muốn làm tương tự thì thêm `HudView` / `PopupView` và kéo khá nhiều ô tham chiếu.

### 7.7 Đưa popup vào Hierarchy (user: "trong UI Canvas vẫn thiếu, ví dụ chưa có setting")
Sau 5.8 scene chỉ có OverlayCanvas / HUD / CapacityBar; 4 popup vẫn `Instantiate` lúc chạy rồi `Destroy` khi đóng nên không thấy trong Editor.
- `OpenPopup` nay ưu tiên popup ĐẶT SẴN trong scene (con của UI, đúng tên): bật nó lên, mở lại CanvasGroup; chưa đặt thì dựng từ prefab như trước.
- `ClosePopup`: popup đặt sẵn thì `SetActive(false)` (huỷ là mất luôn, lần sau phải dựng lại); popup dựng lúc chạy thì vẫn `Destroy`. Cờ `_popupPlaced` phân biệt.
- Đã đặt vào `Systems/UI`, TẮT sẵn: `FailPopup`, `SuccessPopup`, `Settings`, và `OutOfCapacity` (prefab gốc không có Canvas riêng nên bọc trong một Canvas + CanvasScaler như code vẫn làm).
- `Wire()` đã `RemoveAllListeners()` trước khi gắn nên dùng lại cùng một object không bị cộng dồn sự kiện.

**Lỗi lòi ra:** `HUD (GameCanvas)` trong scene đang TẮT (prefab gốc để tắt, lúc chạy `Spawn` mới bật). Đó là lý do ảnh Edit mode ở 5.9 không thấy badge Lv. Đã bật trong scene.

Kiểm chứng: Lv44, mở Settings → đúng 1 object (không nhân đôi), popup hiện đúng (Captures/popup_settings_s.png); đóng lại → còn trong scene, `active=False`, `IsPopupOpen=False`; console sạch.

Hierarchy hiện tại:
```
Systems/UI  [GameUI]
  OverlayCanvas   (BoosterBar[BoosterBarView] + Vignette + Toast)
  HUD (GameCanvas)
  CapacityBar     [CapacityBarView]
  FailPopup       (tắt)
  SuccessPopup    (tắt)
  Settings        (tắt)
  OutOfCapacity   (tắt, bọc Canvas riêng)
```

### 7.8 Bấm hộp đạn lúc trúng lúc không (user báo lỗi, 2 lần)
Có **hai nguyên nhân chồng nhau**; lần sửa đầu mới trúng một.

**(a) Dò hộp theo ô lưới trong khi hộp vẽ lệch ô.** `BoxAtWorld` suy ô từ toạ độ bấm, nhưng hộp được vẽ cao hơn tâm ô theo `ammoActivePose.offset` → bấm vào chỗ nhìn thấy hộp thì trượt. Lần sửa đầu đổi sang dò theo `_visual.position`.

**(b) Lần sửa đầu vẫn sai** (user: "do mình chỉnh ở GameConfig, mình vẫn chưa click được"). `_visual.position` là GỐC XOAY của model, không phải chỗ nhìn thấy: đo với bộ số hiện tại (tilt −68, length 0.62, offset y 0.51) thì gốc xoay **cao hơn tâm phần nhìn thấy 0.71**, còn vùng bấm chỉ rộng ½ ô = 0.725 → chỉ bấm được mép trên hộp, bấm thân dưới là hụt, và độ lệch đổi theo mọi thông số trong GameConfig. Sửa: `AmmoBox.TryGetVisibleBounds` gộp bounds mọi renderer đang bật (bỏ particle/trail/line) = khung hình thật; `BoxAtWorld` coi một hộp là "trúng" khi điểm bấm nằm trong khung hình thật HOẶC trong ô lưới của nó (hộp lún vẫn bấm được để ra hiệu ứng từ chối), nhiều hộp cùng trúng thì lấy hộp có tâm hình gần nhất.

**(c) Nguyên nhân thứ hai, thật sự chặn chuột: `FailPopup` bật sẵn trong scene.** Scene lưu `FailPopup` với `m_IsActive: 1`. Prefab gốc để CanvasGroup alpha 0 nhưng vẫn `blocksRaycasts` → popup **trong suốt phủ cả màn hình, nuốt mọi cú bấm** (đo: `IsOverUiButton` = true tại cả 3 điểm thử trên hộp, kẻ chặn là `FailPopup/.../BTN-Try Again`). Không rõ ai bật (có thể kéo nhầm trong Hierarchy); scene còn lưu `CapacityBar` ở trạng thái tắt.
Sửa 3 lớp: (1) scene: tắt 4 popup, bật `CapacityBar`, lưu; (2) code: `GameUI.HideScenePopups()` chạy cuối `Build` – popup đặt sẵn luôn bắt đầu TẮT bất kể scene lưu gì; (3) popup Thắng (`PlayWinSequence`) cũng đặt cờ `_popupPlaced` để đóng thì tắt chứ không `Destroy` mất object đặt sẵn.

**Kiểm chứng (Lv44):** cố ý bật `FailPopup` trong scene rồi vào Play → tự tắt (chốt chặn chạy); 3 hộp bấm được × 5 điểm trong khung hình thật (giữa, ±35% dọc, ±30% ngang) đi đúng đường `ScreenPointToRay → PointOnPlane → BoxAtWorld`: **bị UI chặn 0/15, trúng sai hộp 0/15**. Trước đó cùng phép thử cho thấy UI chặn 3/6. Console sạch. Đã trả `FailPopup` về tắt và lưu scene.

### 7.9 `HudView` (HUD: badge Level + nút Settings)
`GameCanvas.prefab` thêm script `HudView` trên gốc. Tham chiếu kéo trong Inspector: `canvas`, `badge`, `levelText`, `difficultyText`, `settingsIcon`, `settingsButton`.
**Kiểu dáng theo độ khó nay nằm TRONG View** (3 struct `Style`: `normal` / `hard` / `superHard`), mỗi style giữ: sprite badge, sprite nút Settings, cỡ + vị trí badge, vùng chữ "Lv n", nhãn độ khó (`"Hard"` / `"Super Hard"` / trống) và danh sách `onlyHere` (đối tượng chỉ hiện ở độ khó đó – 2 dải `CurrencyBg` mỗi độ khó). Số liệu giữ NGUYÊN so với `LayoutLevelBadge` cũ (203×90 @ (40,−43) cho Normal; 203×164 @ (40,−17) cho Hard/Super Hard).
API: `Apply(level, difficulty)`, `event OnSettingsPressed`, `SettingsRect` (GameManager đo tâm dọc để đặt mép trên khung máng), `Canvas`, `Validate()`.
**Nung vào prefab** những thiết lập trước đây code đặt lại mỗi lần: badge `Image` type Simple / preserveAspect off / anchor-pivot góc trên-trái; hai `TMP_Text` margin 0, không wrap, overflow Overflow, autosize (10–44 và 8–30), canh giữa; vùng chữ độ khó anchor (0.22,0.05)–(0.78,0.3).
**Dọn:** xoá khỏi `UiSkin` 7 trường sprite HUD (`levelBadge`, `settingsButton`, `levelBadgeNormal/Hard/SuperHard`, `settingsHard`, `settingsSuperHard`) và các dòng tương ứng trong `GameplaySetup`; xoá khỏi `GameUI` 7 biến HUD, `LayoutLevelBadge` (~50 dòng) và `ApplyDifficultyVariants` áp lên HUD; `BuildHud`/`SetLevel` chỉ còn lấy View + gọi `Apply`.
**Kiểm chứng:** compile sạch; `Validate()` đọc lại từ asset đã lưu = đủ tham chiếu; Lv2/44/79 ra đúng badge (`Level` 203×90 / `Hard Level` 203×164 / `Super Hard Level` 203×164), chữ "Lv n", nhãn độ khó và nút Settings tương ứng; `HudCenterScreenY` vẫn 2202.8 (bố cục camera không đổi); bấm Settings → `OnSettingsPressed` bắn và popup Settings mở; ảnh HUD sau refactor trùng trước refactor (Captures/hudview_cmp.png).

### 7.10 `PopupViewBase` + `FailPopupView`
Bốn popup sắp phải làm giống nhau (bật, mở lại CanvasGroup do prefab gốc để alpha 0, bật khung theo độ khó, nảy vào, tắt) nên tách phần chung thành lớp nền `PopupViewBase`: `OpenBase(difficulty)`, `Hide()`, `Canvas`, `Validate()` + hook `CollectMissing`. Ba mảng `normalOnly/hardOnly/superHardOnly` thay cho `ApplyDifficultyVariants` dò theo tên; `content` + `rootGroup` thay cho `PopIn` + `Find("Content Top")`.
`FailPopupView` (trên gốc `FailCanvas.prefab`): `levelText`, `retryButton`, `closeButton`, `event OnRetry`, `Show(level, difficulty)`. Nối trong prefab: content = `Content Top`; khung độ khó = `NormalLevel/HardLevel/SuperHard` của tab tiêu đề.
`GameUI`: thêm `GetPopupView<T>` (ưu tiên bản đặt sẵn trong scene kể cả đang tắt, chưa có thì dựng từ prefab; tự `Validate`), `PrepareCanvas`, `_activeView`; `ClosePopup`/`IsPopupOpen`/`HideScenePopups` hiểu cả View lẫn popup cũ chưa chuyển. `ShowFail` còn ~12 dòng, hết `SetText`/`Wire`/`Find`.
Kiểm chứng (Lv44): `Validate()` đọc từ asset = đủ; popup hiện đúng khung đỏ Hard, "Level 44", nhãn "Hard" (Captures/fail_view_hard_s.png); Try Again → `onRetry` gọi 1 lần, popup tắt, `timeScale` về 1; nút X → cũng gọi lại, tắt; 3 độ khó bật đúng khung (Normal/Hard/Super); luôn đúng 1 `FailPopupView` trong scene; console sạch. Lưu ý: chụp ảnh ngay trong cùng khung hình với `ShowFail` sẽ bắt đúng lúc alpha = 0 (đang fade) → phải chụp sau vài khung hình.

### 7.11 `OutOfCapacityPopupView`
`OutOfCapacityPanel.prefab` (gốc) không có Canvas riêng, trước đây `OpenPopup(..., wrapInCanvas: true)` dựng host `new GameObject` + Canvas + CanvasScaler lúc chạy. Nay có prefab bọc mới `Assets/_Project/UI/Prefabs/OutOfCapacityPopup.prefab`: gốc = Canvas (Overlay, order 30) + CanvasScaler 1242×2688 + GraphicRaycaster + CanvasGroup + `OutOfCapacityPopupView`; bên trong lồng panel gốc `OutOfCapacityPanel` (prefab lồng prefab, bật `active` ở bản lồng vì prefab gốc để tắt). `AB_EGP` ("PLAY ON?" quảng cáo) vốn đã tắt sẵn trong prefab gốc nên không cần code tắt nữa.
View: `playOnButton`, `giveUpButton`, `event OnPlayOn / OnGiveUp`, `Show(difficulty)`; kế thừa `PopupViewBase` (nảy vào, CanvasGroup, khung độ khó – prefab này không có khung độ khó nên 3 mảng để trống).
`UiSkin.outOfCapacityPrefab` + `GameplaySetup` trỏ sang prefab bọc. Scene: bỏ host dựng tay `OutOfCapacity`, đặt `OutOfCapacityPopup` (tắt sẵn).
Kiểm chứng (Lv44) bằng luồng THẬT: đặt sức chứa = 1 rồi bắn hộp → sau 0.6 s panel tự hiện, giống ảnh game (Captures/ooc_view_s.png); **Play On** → panel đóng, `timeScale` 1, sức chứa 1 → 1.25, hộp bay đi, `state=Playing`; **Give Up** → panel đóng, `state=Lost`, popup Thua (`FailPopupView`) tự mở; `Validate()` đọc từ asset = đủ; luôn đúng 1 view; console sạch.

### 7.12 `SettingsPopupView`
`SettingsCanvas.prefab` thêm `SettingsPopupView` (kế thừa `PopupViewBase`): `closeButton`, `restartButton`, `homeButton` và 3 `ToggleRow` (`sound`, `music`, `vibration`; mỗi hàng = `button` + `onState` (Parent Active) + `offState` (Parent Passive)). Mỗi công tắc **tự giữ trạng thái bật/tắt và tự đổi hình**, chỉ báo ra ngoài bằng `OnSoundChanged / OnMusicChanged / OnVibrationChanged`; việc ghi vào `AudioManager.SoundOn/MusicOn` và `PlayerPrefs "bb_vibration"` là của GameUI (View không biết đến). Thứ tự giữ nguyên như cũ: báo ra ngoài TRƯỚC rồi mới phát tiếng bấm (để lúc vừa tắt âm thì tiếng bấm không kêu). `Home` vẫn chơi lại màn như trước (`OnRestart`).
**Nung vào prefab** (trước đây code làm lại mỗi lần mở): `Release Debugger Opener` tắt; 7 ảnh `Icon` (Support/Volume/Music/Vibration on-off) đặt cỡ theo CÙNG công thức của `FixTrimmedIcons` cũ (sprite cạnh dài > 200 → cỡ × 173.35/512, còn lại `preserveAspect`) vì sprite bị cắt gọn. Xoá `SetupToggle` và `FixTrimmedIcons` khỏi GameUI; `ShowSettings` còn ~25 dòng.
Kiểm chứng (Lv44): `Validate()` đọc từ asset = đủ; mở → `timeScale` 0; bấm 3 công tắc: `SoundOn` True→False, `MusicOn` True→False, `bb_vibration` 1→0, hình chuyển xanh (BẬT) ↔ xám có dấu gạch (TẮT), icon không méo (Captures/settings_view_cmp.png); Close → `onClose` 1 lần; Restart và Home → `onRestart` mỗi nút 1 lần; popup tắt, `timeScale` về 1; luôn đúng 1 view trong scene; console sạch. (Bài học đo: chụp ảnh trong cùng lệnh với lúc đổi trạng thái sẽ bắt trạng thái SAU khi đã trả về – phải giữ trạng thái qua nhiều khung hình rồi mới chụp.)

### 7.13 `SuccessPopupView` + dọn `GameUI` (xong 5/5 panel)
**`SuccessPopupView`** (trên gốc `SuccessCanvas.prefab`, kế thừa `PopupViewBase` với `UsesPopIn = false` vì có chuỗi riêng) SỞ HỮU cả chuỗi màn thắng: màn logo "BOUNCE BLAST" (badge → 6 chữ b-o-u-n-c-e → blast) + pháo giấy + mưa pháo + pháo hoa → giữ `winFireworksTime` (rải 2 tiếng pháo) → bảng thắng nảy lên → đống xu rơi → chớp sáng + tia → chữ PERFECT! → sao lấp lánh → vệt sáng quét nút Continue/x2. API: `IEnumerator Play(level, difficulty, coins, fireworksHold)` + `event OnContinue`. ~25 tham chiếu `[SerializeField]` thay cho mọi `Find`: `container`, `levelText`, `prizeText`, `continueButton`, `x2Button`, `alwaysHidden[]`, `dim`, `logoArea/logo/logoAnimation/logoBadge/logoBlast`, `logoLetters[6]` (mỗi chữ = nền + chữ chính), `confettiBursts/confettiRain/fireworks`, `areaShine/coinRoot/coinIcon/perfect/burstEffects[]`. 4 texture hiệu ứng (pháo giấy, quầng sáng, tia, sao) chuyển từ `UiSkin` vào View.
`Dim` (nền đen che bàn chơi, trước tạo bằng `new GameObject` mỗi lần thắng) nay là node nằm sẵn trong prefab, tắt sẵn. Ảnh chớp sáng/tia (`FxImage`) vẫn dựng bằng code vì là vật thể sống vài trăm ms.
**Phát hiện khi tách:** popup Thắng đặt sẵn nên được DÙNG LẠI qua nhiều màn, nhưng code cũ giả định mỗi lần là bản mới → (1) vị trí đống xu `localPosition + CoinLift` cộng dồn 45 mỗi lần thắng; (2) mưa pháo bị `Destroy(rain, 6f)` nên từ lần thắng thứ hai không còn mưa. Sửa: ghi vị trí gốc ở `Awake` rồi luôn đặt lại từ đó; mưa pháo `SetActive(false)` sau 6 s thay vì huỷ (và bật lại khi chơi).
**Dọn `GameUI` (920 → 419 dòng):** xoá `OpenPopup`, `PopIn`, `EaseOutBack`, `Wire`, `SetText`, `FindButton`, `Find`, `Img`, `Txt`, `Stretch`, `ApplyDifficultyVariants`, `PanelEffects`, `PlayLogoEffects`, `AnimateLogo`, `Pop`, `Pulse`, `FxImage`, `FixTrimmedSprites`, `CoinLift`, trường `_popup`/`_popupPlaced`/`PopupNames`; `UiSkin` bỏ 4 trường texture + `GameplaySetup` bỏ 4 dòng. `HideScenePopups` nay tắt mọi `PopupViewBase` bằng `GetComponentsInChildren`. `IsPopupOpen => _activeView != null`.
**Kiểm chứng (luồng THẬT):**
- Thắng lần 1 qua `HandleWin`: bảng thắng đúng "Level 2", PERFECT!, đống xu, "50", Continue + x2, vệt sáng quét nút (Captures/win_seq.png). `y` xu = 45.00 (gốc 0 + CoinLift). Continue → popup tắt, level 2→3, `state=Playing`, `timeScale` 1.
- Thắng lần 2 liền sau (giữ logo 40 s để chụp): logo BOUNCE BLAST đủ 14/14 phần tử + pháo hoa nổ (Captures/win2_logo_seq.png); `y` xu vẫn 45.00 → KHÔNG cộng dồn. Đã trả `winFireworksTime` về 2.4.
- Smoke test sau khi xoá mã chết: Settings/Fail/OutOfCapacity mở → `timeScale` 0, đóng → 1; mở Settings rồi mở Fail ngay → chỉ 1 popup bật; trong scene đúng 4 `PopupViewBase`, 1 `HudView`, 1 `BoosterBarView`, 1 `CapacityBarView`; console sạch.
Lưu ý quy trình: Editor ở nền chỉ dựng ~1 khung hình mỗi lệnh MCP nên các animation "chạy chậm" khi quan sát — không phải lỗi. Và lỡ đổi tên object `GameManager` để ghi nhớ giá trị rồi phải trả lại: đừng dùng tên object làm biến tạm.
**Còn lại (xem bảng mục 6 của kien-truc-ui-dung.md):** `Vignette` + `Toast` chưa có View riêng; `GameUI.Spawn()` vẫn tìm HUD/CapacityBar/OverlayCanvas theo tên thay vì giữ tham chiếu `[SerializeField]`.

### 7.14 Xem trước popup trong Editor (user: "ấn vào các prefab đang unactive thì không thấy panel gì")
Hai nguyên nhân: (1) `FailCanvas`/`SuccessCanvas` gốc có `CanvasGroup` alpha 0 (SettingsCanvas còn `interactable`/`blocksRaycasts` tắt) → bật trong Editor vẫn trong suốt; (2) Canvas Overlay vẽ trong Scene view thành khung 1273×2688 đơn vị ở gốc toạ độ, bàn chơi chỉ ~10 đơn vị nên chọn xong không thấy gì gần bàn.
Sửa: nung `alpha 1 / interactable / blocksRaycasts` cho mọi `CanvasGroup` của 4 prefab popup (runtime `OpenBase` vốn đã ép lại, nên hành vi không đổi; popup tắt sẵn nên không chặn chuột); thêm `PopupViewEditor` (Inspector có nút **Xem trước** / **Tắt xem trước**, hộp cảnh báo khi đang bật, báo `Validate()` lỗi ngay trong Inspector) và `PopupSaveGuard` (`EditorSceneManager.sceneSaving` tắt mọi `PopupViewBase` đang bật, log `[PopupSaveGuard]`).
Kiểm chứng (Edit mode): bật từng popup → canvas bật, alpha 1, 0 CanvasGroup con còn alpha < 1, rect = 1273×2688; lưu scene khi cả 4 popup đang bật → **4 → 0** popup bật sau khi lưu, log `[PopupSaveGuard]` đủ 4 dòng; console sạch. Không chụp được ảnh Game view ở Edit mode (Editor ở nền không vẽ khung hình) nên bằng chứng là thuộc tính, không phải ảnh.

### 7.15 HUD trong suốt ở Edit mode (user: "UI nào hiện lên màn hình cũng phải hiển thị ở chỗ Canvas, sao không thấy nút Settings, panel Settings…")
Ảnh Scene view chỉ có thanh booster trong khung Canvas. Dò chuỗi từ nút Settings lên gốc: `HUD (GameCanvas)` có `CanvasGroup` **alpha 0, interactable/blocksRaycasts tắt** (prefab gốc để vậy vì script fade-in gốc đã mất; runtime `GameUI.Spawn()` mới ép lên 1) → badge Lv và nút Settings trong suốt. Đã nung `alpha 1 / interactable / blocksRaycasts` vào `GameCanvas.prefab` (đây là CanvasGroup duy nhất còn lệch trong mọi UI hiện có: đã quét cả 7 mục trong `Systems/UI`).
Thêm bản đồ "mỗi UI nằm ở đâu" vào `kien-truc-ui-dung.md`: Overlay (OverlayCanvas, HUD) nằm trong khung Canvas trắng; `CapacityBar` là canvas **World Space** (đo: renderMode WorldSpace, rect 904×124, lossyScale 0.0074, toạ độ (5.30, −6.77)) nên nằm dưới bàn chơi chứ không trong khung Canvas – cố ý; 4 popup tắt sẵn, dùng nút Xem trước.
Kiểm chứng: Edit mode – Nút Settings, Badge Level, Thanh booster đều "nhìn thấy" (không node nào tắt, không CanvasGroup alpha 0 trên đường lên gốc); Play – bấm Settings → `OnSettingsPressed` bắn, popup mở; `HudCenterScreenY` = 2202.8, `orthoSize` = 13.87 ổn định qua 3 lần `LoadLevel`; console sạch. (Một số đo thoáng qua 1855 ngay khi vừa vào Play không tái hiện – Canvas chưa bố trí xong.) Chưa có ảnh Edit mode vì Editor ở nền không vẽ khung hình.

### 7.16 Edit mode lệch Play (user: "khi chạy play vị trí các UI bị khác đi, vẫn gọi sinh ra từ code hả")
**Trả lời:** các PANEL không còn sinh từ code (Play: đúng 7 prefab đặt sẵn + EventSystem, không nhân đôi). Nhưng code vẫn (a) TẠO `UI Camera` và `EventSystem` lúc chạy, (b) GHI ĐÈ cấu hình UI lúc chạy → vị trí lệch. Đo Edit mode vs Play:
- **Badge Level**: prefab 348×174 tại (137,−99) → lúc chạy `HudView.Apply` ép thành 203×90 tại (40,−43) (số đo từ ảnh gốc 858×1908) – nhỏ hơn và dời chỗ.
- **Canvas**: prefab/scene là `Screen Space - Overlay` order 1–2; lúc chạy `GameUI.UseCamera` đổi thành `Screen Space - Camera` + UI Camera + order 5/10/30.
**Sửa:** (1) nung bố cục style Normal (cỡ, vị trí, sprite, vùng chữ "Lv n") vào `GameCanvas.prefab` – style Hard/Super Hard vẫn do `Apply` đổi theo độ khó; (2) đặt `UI Camera` sẵn trong `Camera & Lights` (cùng thông số `CreateUiCamera`: ortho 10, x −1000, clear Depth, depth +1) và thêm ô `GameManager.uiCamera` (bỏ trống thì vẫn tự tạo như cũ); (3) đặt `EventSystem` sẵn dưới `Systems/UI`; (4) 6 Canvas trong scene đặt sẵn `Screen Space - Camera` + UI Camera + `sortingOrder` (Overlay 5, HUD 10, popup 30) + `planeDistance` đúng công thức `1.2 − order×0.01` của `UseCamera` → `UseCamera` lúc chạy ghi lại đúng giá trị đã có.
**Kiểm chứng (toạ độ MÀN HÌNH qua UI Camera, Screen 1080×2280, Lv2 Normal):** badge x 34..206 y 2167..2244; nút Settings x 947..1074 y 2139..2266; hàng booster x 52..1028 y 17..238 – **Edit mode và Play giống hệt từng số**. Khi chạy: đúng 1 `UI Camera`, 1 `EventSystem`, 6 canvas đều `ScreenSpaceCamera / UI Camera` đúng order; console sạch.
**Còn sinh từ code (không phải UI):** `GameManager.Awake` vẫn `CreateChild<T>()` cho `Board`, `AmmoGrid`, `Conveyor`, `Balls`, `Theme` và nhóm `VFX`; cùng toàn bộ gạch/hộp/bi/khay dựng theo level. Đó là các hệ thống gameplay (xem mục 4 về lý do không đưa hết thành prefab).
**Lưu ý:** badge ở Edit mode luôn là kiểu Normal; mở level Hard/Super Hard thì `Apply` đổi cỡ/sprite theo độ khó nên lúc chạy sẽ khác Edit mode – đúng thiết kế, không phải lỗi.

## 8. Tái cấu trúc dự án (giai đoạn B–G) và hoàn thiện UI
- Bỏ mọi `Resources.Load`: prefab gốc nằm ở `GameConfig`; level qua `LevelSet` (110 chunk, 550 level); `Resources/` trống.
- Art chuyển theo tính năng (`_Project/Art/...`), asset không dùng vào `_Archive`; mọi di chuyển bằng `AssetDatabase.MoveAsset`. Kiểm: 1966 GUID cũ còn đủ, 0 tham chiếu hỏng mới, level 2/41/44/71/79 trước-sau giống hệt.
- `.asmdef`: `BounceBlast.Runtime`, `BounceBlast.Editor`.
- UI: thêm `OverlayView` (booster + viền cảnh báo + toast), `GameUI` chỉ còn điều phối với 7 View kéo sẵn; bỏ `Spawn()`.
- Scene: Board/AmmoGrid/Conveyor/Balls/Theme/VFX và 3 đèn Directional nung sẵn; còn tạo lúc chạy: `ArenaWalls`, mesh khối gộp, mặt khay (phụ thuộc level/màn hình). Chi tiết: `ARCHITECTURE.md`, `Docs/ke-hoach-tai-cau-truc.md`.

## 9. Tách CameraFitter và đặt sẵn ArenaWalls
- `FitCamera`/`AmmoAreaBottom` tách khỏi `GameManager` thành component `CameraFitter` (đặt sẵn `Systems/GameManager/CameraFitter`, ô `GameManager.cameraFitter`); logic và số giữ nguyên, tự cân lại khi đổi tỉ lệ màn hình.
- `ArenaWalls` là component riêng + `EdgeCollider2D` đặt sẵn, 11 điểm nung vào scene (Gizmo khi chọn; `EnsureBuilt` chỉ dựng khi collider trống). `GameManager` không còn tạo tường lúc chạy.
- Kiểm: compile sạch, Play Lv1 bố cục khung–HUD–khay–booster đúng, autoPlay phá gạch 192→158, bi nằm trong tường, console không lỗi.
- Thư mục rỗng chờ user xoá (tool bị chặn xoá): `_Ripped`, `Resources`, `_Project/Configs`, `_Project/UI`, `Scripts/Gameplay/UI`, `Assets/Screenshots`.

## 10. Scene MenuGame (màn hình chính)
- Quyết định: **scene riêng** (Menu nhẹ, không nạp bàn chơi 3D; mỗi scene một việc). Build Settings: MenuGame (0), Gameplay (1).
- Lấy UI gốc `MainMenuCanvas` (831 object, bỏ Shop/Leaderboards/Collections) → `Prefabs/UI/MainMenuCanvas.prefab`; thêm `MainMenuView`, `MenuManager`, `PlayerData`, `SceneFlow`; nhạc menu `GameConfig.musicMenu` (cyberwave, lấy từ `_Archive`).
- Gameplay: nút Home trong Settings giờ về MenuGame (trước đây = chơi lại); thắng cộng 50 xu và lưu level qua `PlayerData`.
- Kiểm bằng click giả trong Play: Menu hiện Level + xu + tim đúng; tab Shop báo coming soon; tab Settings mở trang, bật/tắt Sound đổi `AudioManager.SoundOn`; Home quay lại; Play → Gameplay đúng level đã lưu; Settings ▸ Home → MenuGame; console không lỗi.
- Chưa làm (cố ý): Shop, Journey, Collection, hệ thống tim (đồng hồ hồi tim ẩn, tim luôn 5), Daily Challenge / offer button.

## 11. Tách GameManager thành nhiều manager + sắp lại Hierarchy
- `GameManager` (~460 dòng) → `GameManager` (điều phối, trạng thái, thắng/thua) + `LevelManager` + `InputManager` + `LaunchManager` + `BoosterManager` + `DebugManager`; `AudioManager` đặt sẵn trong scene (`Init` dùng lại component có sẵn). Chi tiết: `Docs/kien-truc-manager.md`.
- Hierarchy Gameplay: `Camera & Lights` / `World` / `Managers` / `UI(HUD, Popups)`; MenuGame: `Camera & Lights` / `Managers` / `UI`. Bỏ nhóm `Systems`.
- Script `GameManager.cs`, `AudioManager.cs` chuyển vào `Scripts/Gameplay/Managers/` (MoveAsset, giữ GUID). Ô `startLevel / continueFromSave` chuyển sang `LevelManager`, `autoPlay` sang `DebugManager` (giá trị cũ vốn là mặc định).
- Kiểm bằng Play: nạp level, thắng (autoPlay) → Continue → level kế, Out Of Capacity → Give Up → Lose → Retry, booster Hammer/Capacity, Menu → Gameplay. Console sạch.

## 12. Khối gộp (nhiều máu) thụt vào so với gạch 1 ô
- Đo ở Lv4 (Play): mặt trước gạch 1 ô ở z ≈ −0,404, khối gộp 4×8 ở z ≈ −0,042 → khối thụt ~0,36 so với gạch, bị gạch kề che mép.
- Sửa: thêm `GameConfig.blockFrontZ` (mặc định −0,36, nhóm "Gạch trên bàn"); `CreateSolidVisual` đặt `Visual.localPosition.z` = giá trị đó. Đo lại: mặt trước khối gộp z ≈ −0,402 (khớp gạch). Muốn đẩy ra thêm / lùi lại → chỉnh số này (0 = như cũ).
- Chỉ áp cho khối gộp dựng mesh liền (CombinedCube, chữ L/T…). Hòm gỗ (Crate) và cột dùng đường dựng khác, chưa đo.
- (tiếp mục 12) Đáy khối gộp: phép xiên `y += z·tan20` đẩy mép dưới của dải vát (z = b) lên b·tan → đáy khối co lên khỏi mép ô. Đã hạ cả mesh xuống `b·tan` trong `BuildSolidMesh` → mép dưới ngoài cùng đúng mép ô (đo Lv4: mesh đáy −3,812, collider −3,804). Dải vát sẫm mỏng ở đáy vẫn còn (là mặt vát hướng xuống, vốn có). Gạch 1 ô còn nhô thêm ~0,057 do `cubeOverlap`.
- (tiếp mục 12) Kéo đáy bằng gạch 1 ô: thêm `GameConfig.blockBottomExtra` (mặc định 0,12 ô, nhóm "Gạch trên bàn") – `BuildSolidMesh` dời các đỉnh ở đáy xuống thêm đoạn này. Đo Lv4: đáy khối nhô khỏi collider 0,059, gạch 1 ô nhô 0,057 (khớp). 0 = đáy đúng mép ô.
- (tiếp mục 12) Thụt đỉnh: thêm `GameConfig.blockTopTrim` (mặc định 0,23 ô) – `BuildSolidMesh` dời các đỉnh ở đỉnh xuống thêm đoạn này (dải mặt trên giữ nguyên độ dày, chỉ dời xuống). Đo Lv4: khối đỉnh nhô 0,059 / đáy nhô 0,059; gạch 1 ô đỉnh 0,057 / đáy 0,057 (khớp). 0 = như cũ.
- (tiếp mục 12) Sửa lại mặt trên: lần trước mới khớp vị trí mép mà dải mặt trên vẫn dày gấp đôi gạch 1 ô (đo pixel Lv4: khối 12 px, gạch 6 px; gạch kề phía trên còn che bớt dải của khối nên số đo thế giới không phản ánh hết). Đã chỉnh trong GameConfig: `blockDepth` 1,1 → **0,2** (dải mặt trên ≈ bề rộng dải vát, bằng gạch 1 ô) và `blockTopTrim` → **0,13** (mép trên đúng ranh hàng gạch; cho phép số âm để đẩy lên). Đo pixel sau chỉnh: dải mặt trên ~7 px, mép trên khớp ranh hàng gạch hai bên.
- Bài học: so mép bằng pixel màn hình (có che khuất), không chỉ bằng bounds thế giới.

## 13. Hàng gạch đáy bị gờ sàn của khung che
- Lv7 (20 cột × 24 hàng): đo pixel – hàng đáy cao 31 px nhưng gờ sàn xanh của khung (vẽ trước gạch) phủ từ y = 1137, chỉ lộ ~14 px (~nửa hàng; hai hàng đáy cùng màu nhìn như thiếu một hàng). `arena.yMin = −6,35` (đo theo góc nhìn cũ) thấp hơn mép sàn thật ~0,23 khi nhìn thẳng.
- Sửa: thêm `GameConfig.boardOffsetY` (mặc định 0,24, nhóm "Gạch trên bàn"): `BoardController` nâng gốc lưới lên; `ArenaWalls.Rebuild` nâng sàn vật lý của bi cùng đoạn (đã dựng lại + lưu scene: điểm sàn y = −6,11).
- Đã kiểm trên cả 550 level: đỉnh gạch cao nhất 5,49 (+0,24 = 5,73) < trần 7,18 → không chạm trần. Đo pixel sau sửa: hai hàng đáy đều lộ đủ (mép đáy gạch ngay trên gờ sàn). Chạy tự chơi Lv7 không lỗi console.
- Không đổi `arena` (nên băng chuyền, máy xay không đổi).

## 14. Nhấn giữ hộp đạn → lắc như thạch
- `AmmoBox.SetHeld(bool)`: khi giữ, `ApplyScale` nhún co giãn theo chiều cao (cao ↑ thì ngang ↓, giữ thể tích), lắc nghiêng quanh trục Z và co nhẹ khi bị nhấn; thả thì tắt dần (`_heldBlend`). Không đụng tới logic bắn.
- Thông số ở `GameConfig → Hộp đạn – lắc khi nhấn giữ`: `heldAmplitude` 0,10 · `heldFrequency` 5 Hz · `heldRoll` 4° · `heldPressIn` 0,04 · `heldBlendIn` 0,12 s · `heldBlendOut` 0,25 s.
- `InputManager` đổi luật bấm (cờ `fireOnRelease`, mặc định bật): chạm xuống hộp bấm được → bắt đầu giữ (lắc); **thả tay trên đúng hộp đó mới bắn**; kéo ra khỏi hộp rồi thả = huỷ. Tắt cờ để về cách cũ (bắn ngay lúc chạm). Búa (Hammer) vẫn tác dụng ngay lúc chạm. Magic Hand (hộp chưa bấm được) cũng giữ → thả → bắn.
- Kiểm trong Play: bật `SetHeld(true)` → rotZ ±4°, chiều cao/ngang bounds dao động; `SetHeld(false)` → rotZ 0, bounds bằng hộp khác (tắt sạch). Console sạch. **Chưa** kiểm đường chuột/touch thật qua `InputManager` (chỉ gọi hàm).

## 15. Cách B: thạch thật cho hộp đạn (thay cách A ở mục 14)
- Cách A (nhân scale / xoay cả khối) bị đánh giá cứng, không có độ trễ giữa các phần. Thay bằng **`JellyDeformer`** (`Scripts/Gameplay/Ammo/JellyDeformer.cs`, tự gắn vào renderer thân thạch khi hộp lần đầu bị giữ).
- Mesh thân thạch (`Cube`, 208 đỉnh, đọc được, **không có xương / skin**) là cột dài trục Y; đầu nắp hướng camera đung đưa, đáy neo trong hố. Biến dạng từng đỉnh bằng 3 lò xo giảm chấn (nhún dọc thân, đung đưa X, đung đưa Z) bị đẩy bởi sóng sin → thạch chạy trễ pha, vọt quá rồi dội lại; phình ngang giữ thể tích + sóng phình chạy dọc thân.
- Lúc yên trả mesh gốc dùng chung (gom batch); mesh bản sao giữ `bounds` gốc nên căn / dò hộp không rung theo. Hộp bay đi / lơ lửng thì `ResetNow`. `PlayDenied` cũng hích thạch.
- Thông số (`GameConfig → Hộp đạn – thạch khi nhấn giữ`): `heldStretch` 0,12 · `heldSway` 0,12 · `heldDriveFrequency` 5 · `jellyFrequency` 6 · `jellyDamping` 0,12 · `jellyPressKick` 1,2 · `jellyReleaseKick` 2,0 · `heldBlendIn` 0,12 · `heldBlendOut` 0,15. Các thông số cũ của cách A (`heldAmplitude`, `heldFrequency`, `heldRoll`, `heldPressIn`) đã bỏ.
- Kiểm Play: giữ → đỉnh mesh lệch (x −1,29…1,06; chiều dài thu 0,71× lúc nhún mạnh), ảnh chụp thấy hộp nghiêng/phình kiểu thạch; thả → `enabled=false`, mesh về `Cube`, bounds bằng hộp khác; giữ rồi bắn ngay → hộp vẫn bay, tiêu hao capacity bình thường; console sạch. Chưa kiểm bằng chuột/touch thật; chưa kiểm cảm giác chuyển động liên tục (chỉ chụp từng khung).
- (tiếp mục 15) Giảm lắc cho nhẹ, không lấn hộp kề: `heldStretch` 0,03 · `heldSway` 0,03 · `jellyDamping` 0,2 · `jellyPressKick` 0,5 · `jellyReleaseKick` 0,9; chặn cứng trong code ±5% (nhún) / ±5% (đung đưa). Đo 300 khung khi giữ (Lv4): hộp lệch ra khỏi silhouette tối đa trái 0,076 / phải 0,079 / dưới 0,017 / trên 0,059 (đơn vị thế giới) so với khoảng hở tới hộp kề 0,151 → không chạm hộp bên cạnh. Bản đầu (0,12) lệch 0,125 sát khoảng hở nên lấn.

## 16. Phân biệt click và nhấn giữ
- `InputManager.holdToWobble` (mặc định 1 s, Inspector): chạm xuống hộp → chỉ ghi nhận (`_pressTime`), **chưa lắc**; giữ ≥ ngưỡng và ngón vẫn trên hộp thì `SetHeld(true)`; ngón trượt ra khỏi hộp thì tắt lắc, trượt lại vào thì lắc tiếp. Thả tay trên đúng hộp (nhanh hay lâu) vẫn bắn; thả ngoài hộp = huỷ.
- Click nhanh = không bao giờ có cú lắc (trước đây lắc ngay lúc chạm nên click cũng bị hiểu thành giữ).
- Đã kiểm: biên dịch sạch, game chạy, trường `holdToWobble` = 1 trên scene. Chưa thử được bằng chuột/touch thật (không giả lập được `Input`).
- (tiếp mục 16) Kéo ngón ra khỏi hộp **vẫn lắc tiếp** (tay người chơi che hộp, muốn dời tay mà vẫn thấy hộp lắc). Thả trên hộp = bắn, thả ngoài hộp = huỷ. Chưa kiểm bằng chuột/touch thật.
- (tiếp mục 16) Luật nhấn giữ hộp đạn chốt: giữ ≥ `holdToWobble` = **0,5 s** thì lắc; kéo ngón ra vẫn lắc; thả trên hộp = bắn; thả ngoài hộp = huỷ (lắc tắt dần, không bắn). Click nhanh (< 0,5 s) không lắc. Ngưỡng mặc định trong code và trên scene Gameplay đều 0,5.
- (tiếp mục 16) Click bị trễ: bắn lúc THẢ tay nên chậm hơn bắn lúc chạm đúng bằng thời gian ngón đè (~0,1 s). Không thể vừa bắn ngay lúc chạm vừa cho "giữ để lắc" trên cùng một hộp (hộp đã bay đi). Chọn ưu tiên tốc độ click: scene Gameplay đặt `InputManager.fireOnRelease = false` (bắn ngay lúc chạm). Hiệu ứng giữ-lắc vẫn nằm trong code (`holdToWobble`, `SetHeld`) và chỉ chạy khi bật lại `fireOnRelease`.

## 17. Phản hồi tức thì lúc chạm (bù độ trễ bắn lúc thả)
- Bật lại `InputManager.fireOnRelease` (scene Gameplay): bắn lúc thả tay, giữ ≥ `holdToWobble` (0,5 s) thì lắc thạch.
- Để click không bị cảm giác trễ, lúc **chạm xuống** hộp bấm được: (1) hộp lún nhỏ lại ngay (`AmmoBox.SetPressed`, `GameConfig.pressInScale` 0,06 trong `pressInTime` 0,05 s); (2) phát ngay tiếng `TapAmmo`; lúc thả mới bắn và chỉ phát tiếng `BoxLaunch` (`LaunchManager.TryLaunch(..., tapSoundDone: true)`). Huỷ (thả ngoài hộp) thì hộp bật lại.
- Kiểm Play: nhấn → bounds hộp co 6% (1,530 → 1,438); bắn khi đang nhấn → hộp bay bình thường; console sạch. Chưa kiểm cảm giác trên chuột / điện thoại thật; chưa thêm rung điện thoại (Handheld.Vibrate rung dài ~0,4 s, không hợp cú chạm).

## 18. Hết sức chứa: hộp dừng giữa chuyến bay THẬT (thay ống nghiêng sang phải)
- Lỗi: khi bấm hộp làm vượt sức chứa, `AmmoBox.Hover()` dựng một ống thạch ngả cố định ~28° sang phải (bám video gốc) → luôn nghiêng một hướng, khác hoạt ảnh bay lên máng trong game (hộp bên trái bay sang máng trái…).
- Sửa: bỏ `Hover/HoverRoutine` (và 3 thông số `hoverAngle/hoverLength/hoverTime`). Hộp vượt sức chứa chạy **đúng `FlyRoutine`** (cùng ống thạch, cùng quỹ đạo, cùng chiều tới máng sẽ dùng) rồi **dừng** ở tiến độ `GameConfig.hoverHoldProgress` (mặc định 0,5; 0 = nén lấy đà, 1 = tới máng). Dừng bằng cờ trong `FlyRoutine` (`dt = 0`), không phụ thuộc `Time.timeScale` (popup Out Of Capacity / Thua đặt timeScale 0 rồi 1 rồi 0 nên timeScale đơn thuần không đủ).
- API: `ConveyorSystem.HoldBoxMidFlight(box)` chọn máng (`PickLane`, tách khỏi `Send`) → `AmmoBox.HoldFlightAt(...)`; `AmmoBox.HeldLane` giữ máng đã chọn. Chọn Play On → `LaunchNow → grid.Launch → BeginLaunch` (không huỷ coroutine đang giữ) → `Send` dùng đúng máng đó → `FlyTo` thả giữ, hộp **bay tiếp từ chỗ dừng** tới máng (không bật lại từ đầu). Give Up → hộp đứng yên dưới popup Thua.
- Kiểm Play (Lv88): hộp ô (6,0) dừng thành ống ngắn nghiêng về máng phải; hộp ô (2,0) dừng thành ống dài cong về máng TRÁI (đúng chiều bay thật); Play On → bay tiếp, cắm vào máy xay, game chạy tiếp; Give Up → trạng thái Lost, hộp vẫn dừng; console sạch.

## 19. Booster + máy xay đổi theo độ khó, chỉnh chữ badge Hard / Super Hard
- Gốc (`artconfigs/original/SpriteConfig.asset` → `spritesBooster`): mỗi nút booster có `sprite` (Normal) + `spriteHardOverride` + `spriteSuperHardOverride`. Bản dựng lại mới chỉ đổi màu dải nền, hình nút luôn là Normal.
- Sửa: `BoosterBarView` thêm 3 bộ `Style` (Normal / Hard / Super Hard: magicHand, hammer, shuffle, capacity, ô số) + `ApplyDifficulty`, `GameUI.SetLevel` gọi. 12 asset (Hard/Super Hard Hammer, Magical Hand, Shuffle, Capacity, Super Hard Number + 3 trang atlas) chuyển từ `_Archive` ra `Art/UI/Booster/{Sprites,Textures}`; `Hard Number` chuyển từ `Art/UI/Menu` sang `Booster` (GUID giữ nguyên). Đã gán trong prefab `OverlayCanvas`.
- Máy xay: vỏ nhìn thấy là `Shredder/Plane.028` (material "Tunnel", xanh thép) – trước giờ không đổi. `ThemeApplier` nhân bản material này (và cụm dự phòng `polySurface11`) theo theme: dời tông (hue) bằng chênh lệch hue của `groundColor` so với Normal, nhân độ bão hòa theo tỉ lệ bão hòa, độ sáng theo căn bậc hai tỉ lệ sáng, nhân thêm `Theme.crusherBrightness` (mặc định 1). Super Hard (nền xám) → vỏ xám; Hard (nền đỏ) → vỏ đỏ sẫm; Normal giữ nguyên. Mốc Normal đặt bởi `GameManager` (`SetReferenceGround`). Lưỡi cắt không đổi.
- Badge Hard / Super Hard: chữ "Super Hard" nằm sát / đè viền dưới tab, "Lv n" thấp hơn tâm panel khoảng 3 px (số đo tâm ngang đều khớp < 1 px). `HudView.Style` thêm `difficultyTextMin/Max` theo từng độ khó; Hard & Super Hard: vùng "Lv n" (0,14–0,86 × 0,38–0,74), vùng chữ độ khó (0,22–0,78 × 0,08–0,33) → chữ nằm gọn trong tab, "Lv n" giữa panel.
- Kiểm Play (Lv88/89 Super Hard, Lv44 Hard): nút booster + ô số tối (Super Hard) / đỏ (Hard); vỏ máy xay xám / đỏ sẫm; badge gọn; console sạch. So mắt với ảnh gốc, chưa đo từng màu.

## 20. Thử dùng mảnh tường module gốc (ModularPrimitiveAmmoWall) cho khay đạn
- Câu hỏi: dùng mesh gốc thay cho mesh khay tự dựng có tối ưu / gọn hơn không?
- Thử: nhân bản prefab `ModularPrimitiveAmmoWall - Wallbase` cho từng biến thể (49 mảnh, tên = tập ô kề: L, TL, T, TR, R, BR, B, BL…) rồi đặt vào cảnh ở camera thẳng.
- Kết quả: (1) nhìn thẳng chỉ ra các ô vuông phẳng (trắng / navy / hồng lỗi shader) vì vật liệu `c1 - Frame`, `lambert`, `standardSurface` của mảnh gốc không dùng được sau khi rip; khác biệt 3D (gờ, viền) chỉ lộ khi camera nghiêng 20° như game gốc – đúng ghi chú cũ trong `AmmoGridController` ("autotile 3D có gờ / khe nối lộ ra khi nhìn nghiêng"). (2) Lv89 (khay 8×8, 41 ô tường): mảnh gốc ~6,2 submesh và ~179 tam giác mỗi ô → ~254 draw call, ~7,3k tam giác nếu mỗi ô một prefab (chưa batching); khay tự dựng hiện tại: 5 mesh, ~5,1k tam giác.
- Kết luận: giữ khay dựng gộp mesh (ít draw call hơn); dùng mảnh gốc chỉ có lợi nếu muốn đúng nghĩa 3D nghiêng như gốc, và phải gộp mesh + viết lại vật liệu. Chưa đổi gì trong game.

## 21. Vành sáng của "ván che" khay đạn (tray rim)
- Ảnh gốc Super Hard Lv89: khay là tấm ván tối phủ mọi ô không phải hố, có **vành sáng mảnh** bo tròn chạy theo hình khay và quanh miệng từng hố; bản dựng lại chỉ có mặt khay phẳng không vành.
- Thêm `AmmoGridController.BuildTrayRim` → mesh `TrayRim` (cùng bộ dựng gộp mesh, thêm 1 draw call): (1) vòng quanh miệng mỗi hố (ô trống "-" kề nhau gộp liền, cạnh gộp không vẽ vành); (2) vành theo hình khay: đoạn thẳng ở cạnh giáp ô không phải hố + cung bo ở góc lồi, đầu mút nối liền ở cạnh chạy tiếp hoặc dừng ở góc lõm (không còn vạch thừa). Màu = màu mặt khay theo theme pha trắng `trayRimLighten`.
- Thông số `GameConfig → Khay đạn – hình khối`: `trayRimWidth` 0,035 ô (0 = tắt; gờ giữa 2 hố chỉ 0,08 ô nên không đặt quá 0,035) · `trayRimLighten` 0,35.
- Kiểm Play: Lv89 (Super Hard, xám), Lv44 (Hard, đỏ), Lv5 (Normal, xanh) – vành đúng màu theo theme, bám đúng hình khay, không vạch thừa; còn một điểm nhỏ ở chỗ hai ô trống chỉ chạm nhau ở góc (lệch vài px). So mắt với ảnh gốc chưa đo.
- (tiếp mục 21) Độ dày tấm ván: camera game nhìn thẳng (chiếu ép dọc) nên chỉ thấy các mặt cạnh HƯỚNG LÊN của khối (như gạch ở bàn). Thêm `GameConfig.trayPlateDepth` (0,38 ô): các cạnh dưới của cụm hố (= cạnh trên của tường) dày thành dải cao = dày × tan20° (tối đa 0,14 ô để không lấn miệng hố). Các cạnh trái / phải / trên chỉ còn vành mảnh vì mặt cạnh đó không lộ khi nhìn thẳng. Chưa có đổ bóng của ván lên mặt khay.
- **HOÀN TÁC mục 21 (theo yêu cầu):** đã bỏ `BuildTrayRim`, `TrayRim`, `trayRimWidth`, `trayRimLighten`, `trayPlateDepth`. Khay đạn về đúng như trước khi thêm vành / ván (các mesh `TrayWallLip`, `Backdrop`, `TrayPanel`, `TraySlotFloors`, `TrayWells`). Mục 20 (kết quả thử mảnh module gốc) vẫn là kết luận hiện hành: giữ khay dựng gộp mesh.

## 22. Popup mua booster (BoosterBuyPopup)

- Nguồn gốc: `BoosterBuyPanel` trong `Resources/asyncload/Boosters.prefab` của bản rip, tách bằng `ui_extract.py` → `Prefabs/UI/BoosterBuyPanel.prefab` (165 object, không phải chép asset nào thêm). Bọc trong `Prefabs/UI/BoosterBuyPopup.prefab` (Canvas + CanvasGroup + `BoosterBuyPopupView`), đặt sẵn tắt trong `Gameplay.unity` dưới `Popups`, nối vào `GameUI._boosterBuy` và `UiSkin.boosterBuyPrefab`.
- Một khung dùng chung cho 4 booster; `BoosterBuyPopupView.entries` giữ tên, mô tả, icon, giá, số lượt. Chữ lấy từ `Localization.txt` gốc: tiêu đề `booster_title_*`, mô tả `tutorial_booster_*`. Giá hiển thị 1800 (Magic Hand, Hammer, Shuffle) và 2600 (Capacity), nhãn ×3.
- Luồng: bấm nút booster khi hết lượt → `BoosterManager.HandleBooster` gọi `GameUI.ShowBoosterBuy` (game tạm dừng như các popup khác) → bấm nút xanh = nhận 3 lượt MIỄN PHÍ (chưa làm cửa hàng coin), bấm X = đóng.
- Asset kéo từ `_Archive` ra `Art/UI/Booster/Sprites`: 4 `* Icon shadow`, 4 `Booster Icon - *`, `Orange Number`, `Coin Shop Button_mir`, `line_mir` (+ 2 atlas texture sang `Booster/Textures`).
- Sửa so với bản tách: root panel gốc scale 0 (do script gốc tự nảy) → đặt scale 1 + stretch; dòng mô tả bật xuống dòng + auto-size; thêm `Glow` (RawImage, `glow7.png` + `glow6_ADD`) thay cho hệ hạt UI Particle đã bị bỏ (tia sáng nay là ảnh tĩnh, không xoay/lấp lánh). Nút xem quảng cáo (RV) bị tắt.
- Kiểm Play: hết Hammer/Capacity → popup đúng tên/icon/giá; bấm mua Capacity 0 → 3, popup đóng, game chạy tiếp; console sạch. Chưa kiểm: popup ở độ khó Hard/Super Hard (khung xanh dùng chung, chưa có ảnh gốc đổi tông), bấm bằng chuột thật.
- (tiếp mục 22, chỉnh sau góp ý) Quầng sáng giờ XOAY: `Scripts/UI/UiSpin.cs` (xoay theo `unscaledDeltaTime` vì game đang tạm dừng) trên 2 lớp `Glow` (22°/s) và `Glow 2` (−35°/s, nhỏ hơn) đặt trong `GlowArea` có `RectMask2D` để tia không tràn ra ngoài ô kem (sprite ô kem có đệm trong suốt ~90 đơn vị trên/dưới nên khung cắt thụt 100). Nút mua phóng ×1,2 (`KPIAP Currency Spend Button`) cho khớp tỉ lệ ảnh gốc (đo ở 858×1908: nút ~48% bề ngang khung). Đối chiếu khung, ruy-băng, ô kem, nút với ảnh gốc ở cùng tỉ lệ: khớp.
- (tiếp mục 22, lần 2) Nguyên nhân "méo": icon booster `boosterImage` bị kéo giãn ngang (ô 250×250 nhưng sprite không vuông, chưa bật Preserve Aspect) → bật `preserveAspect`, ô 245. Chỉ giữ MỘT nguồn sáng (`Glow`, xoay, alpha 0,6, cỡ 520) – bỏ lớp `Glow 2`. Đồng xu ×0,9 và nhãn ×3 ×0,92 (khi phóng nút ×1,2 hai thứ này to hơn gốc ~10%); chữ mô tả tối đa 56, nâng lên 14 đơn vị. Đối chiếu cạnh nhau với `Booster/4.jpg` (858×1908): khung, ruy-băng, ô kem, nút khớp.
