# Shader Ripper

Tool đục **mã GLSL** của shader từ game Unity Android (`.xapk`, `.apk` hoặc thư mục `assets/bin/Data`).

## Vì sao không dùng AssetRipper?

AssetRipper chỉ dịch ngược được shader **DirectX (DXBC)**, kể cả khi chọn chế độ *Decompilation*. Game Android chỉ chứa GLES/Vulkan nên nó âm thầm xuất ra *dummy shader*: chỉ còn khối Properties, material bị hồng/sai màu.

Với GLES, Unity lưu shader dưới dạng **mã GLSL dạng chữ** trong một blob nén LZ4. Tool này giải nén blob, đọc bảng mục lục, rồi ghép từng đoạn mã với **keyword** của variant tương ứng.

## Cài & chạy

```bash
python -m pip install UnityPy
```

```bash
python shader_ripper.py "D:/toolGame/Bounce+Blast_3.7.6_APKPure.xapk" -o shader_rip --skip-builtin
```

Hoặc kéo thả file game vào `rip_shaders.bat`.

| Tham số | Ý nghĩa |
| --- | --- |
| `-o <thư mục>` | Nơi xuất (mặc định `./shader_rip`) |
| `-f <chuỗi>` | Chỉ lấy shader có tên chứa chuỗi (lặp nhiều lần được), VD `-f Toony -f Ice` |
| `--skip-builtin` | Bỏ shader có sẵn của Unity / TMP / Spine / URP |
| `--list` | Chỉ in danh sách tên shader |

## Kết quả

```
shader_rip/
├── _summary.md                     # bảng tất cả shader + số variant
└── ELCAPITAN_IceBlockerWorld/
    ├── _shaderlab.txt              # Properties, SubShader, Pass, Tags
    ├── _states.txt                 # Cull / ZWrite / ZTest / Blend / ColorMask / Stencil từng pass
    ├── _index.tsv                  # file ↔ keyword
    └── s0p0_FORWARDBASE_Vertex_GLES3_1.glsl
```

- Mỗi file `.glsl` chứa cả `#ifdef VERTEX` lẫn `#ifdef FRAGMENT`. Dòng đầu ghi keyword của variant đó.
- Shader có nhiều variant (VD Toony Colors Pro 2 có ~1.000): tra `_index.tsv` để tìm đúng variant mà material đang bật (xem `m_ValidKeywords` trong file `.mat`).
- Shader ghi **"không có code"** là shader đã bị Unity strip khỏi bản build, không lấy lại được.
- Nền tảng Vulkan/Metal được lưu thô thành `.bin`. Muốn đọc SPIR-V thì dùng `spirv-cross`.

## Bước 2 – Dịch tự động sang `.shader` Unity: `glsl_to_unity.py`

GLSL rip ra không dán thẳng vào Unity được, nhưng nó do hlslcc sinh ra nên rất "máy móc". `glsl_to_unity.py` dịch **từng dòng** sang HLSL theo luật cố định và **không thay đổi phép tính nào**, nên màu sắc giống game gốc:

| GLSL (hlslcc) | HLSL Unity |
| --- | --- |
| `vec3`, `ivec2`, `bvec4` | `float3`, `int2`, `bool4` |
| `vec3(x)` (1 tham số) | `((float3)(x))` |
| `mix`, `fract`, `inversesqrt`, `dFdx` | `lerp`, `frac`, `rsqrt`, `ddx` |
| `texture(s, uv)` / `textureLod` | `tex2D` / `tex2Dlod` (`texCUBE`, `UNITY_SAMPLE_TEXCUBE` cho cubemap) |
| `greaterThanEqual(a, b)` | `((a) >= (b))` |
| `hlslcc_mtx4x4unity_ObjectToWorld[i]` | `transpose(unity_ObjectToWorld)[i]` (cột i) |
| `in_POSITION0`, `vs_TEXCOORD0`, `SV_Target0` | struct `appdata` / `v2f`, giá trị trả về |
| `#ifdef UNITY_ADRENO_ES3 … #else … #endif` | giữ nhánh `#else` |

Cách tool xử lý keyword:
- Keyword của material (VD `TCP2_SPECULAR`, `_NORMALMAP`) → `#pragma shader_feature_local`. Mỗi tổ hợp keyword có trong bản build được trỏ tới **đúng variant gốc**.
- Keyword hệ thống (`DIRECTIONAL`, `SHADOWS_SCREEN`, `INSTANCING_ON`…) không ảnh hưởng tới material, nên tool chọn 1 bản chuẩn là `DIRECTIONAL + LIGHTPROBE_SH`.
- Render state, Tags, Properties lấy từ `_passes.json` / `_shaderlab.txt`. Các drawer riêng của TCP2 được bỏ (giữ `Toggle`, `Enum`…).
- Có `--guid-from` thì file `.meta` mang **GUID gốc** của bản AssetRipper, nên material cũ tự trỏ đúng.

```bash
python glsl_to_unity.py D:/toolGame/shader_rip -o Assets/_Project/Shaders/Ripped --guid-from D:/toolGame/game/ExportedProject/Assets --file-map example_filemap.json
```

- `--file-map`: json `{tên shader: tên file}` để ghi đè đúng file `.shader` đã có, giữ nguyên `.meta`.
- `-f`: lọc shader theo tên như ripper.

**Ghép khung `.shader` gốc + công thức `.glsl`:**
- Khung (Properties: tên, nhãn, giá trị mặc định, thuộc tính ẩn `_SrcBlend/_DstBlend`; Tags; render state) lấy từ `_shaderlab.txt` / `_passes.json` – chính là phần mà file `.shader` "dummy" của AssetRipper còn giữ. Drawer riêng của TCP2 đổi sang drawer có sẵn (`TCP2ToggleNoKeyword`→`ToggleUI`, `TCP2HeaderToggle(KW)`→`Toggle(KW)`), drawer không có tương đương thì bỏ.
- Công thức màu lấy từ GLSL, dịch từng dòng, không đổi phép tính.
- **GPU instancing như game gốc**: nếu bản build có variant `INSTANCING_ON`, tool đọc khối `UnityInstancing_<Tên>` (VD `CubeProps { _CubeShadeOffset; _UseBakedCubeShadeOffset; }`) và sinh `UNITY_INSTANCING_BUFFER_START/…/END`, `UNITY_ACCESS_INSTANCED_PROP`, `UNITY_SETUP_INSTANCE_ID`, `#pragma multi_compile_instancing`. Ma trận dùng `unity_ObjectToWorld` của UnityCG (tự thành mảng instancing khi bật) → `MaterialPropertyBlock` + instancing gộp lệnh vẽ được.
- Mỗi khối code có chú thích `// ---- vN: <file GLSL>  |  keyword material: …` để đối chiếu với bản gốc.

**Giới hạn** (khác với game gốc):
- Bỏ pass ForwardAdd, ShadowCaster, Deferred, Meta. Game chỉ dùng 1 đèn directional nên ForwardAdd không ảnh hưởng.
- Không dùng variant nhận bóng đổ (`SHADOWS_SCREEN`).
- Vòng lặp có lấy mẫu texture được gắn `[unroll(8)]`, vì D3D bắt buộc unroll. Phép tính không đổi.
- Shader bị strip khỏi build (không có code) thì không dịch được và phải tự viết.

Kết quả trong dự án này: `Assets/_Project/Shaders/Ripped/`. Có 25 shader dịch tự động, compile 0 lỗi, dùng cho ~180 material. Riêng `ELCAPITAN/ToonyColors/Default` bị strip khỏi build nên vẫn là bản viết tay.
