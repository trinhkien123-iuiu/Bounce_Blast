"""
Shader Ripper – đục mã shader (GLSL) từ game Unity Android (.xapk / .apk / thư mục Data).

Vì sao cần tool này: AssetRipper chỉ dịch ngược được shader DirectX (DXBC). Game Android chỉ chứa
GLES/Vulkan nên AssetRipper xuất ra "dummy shader". Nhưng shader GLES được Unity lưu dạng *mã GLSL
dạng chữ* bên trong blob nén LZ4 → giải nén là đọc được nguyên logic.

Kết quả cho mỗi shader (thư mục riêng):
  _shaderlab.txt   Properties, SubShader/Pass, Tags (khung ShaderLab, chưa có code)
  _states.txt      Render state từng pass: Cull, ZWrite, ZTest, Blend, ColorMask, Stencil
  _index.tsv       Danh sách file variant + keyword tương ứng
  sXpY_<LightMode>_<Stage>_<blob>.glsl   Mã GLSL của từng variant (dòng đầu ghi keyword)
Thư mục gốc có _summary.md liệt kê toàn bộ shader.

Cách dùng:
  python shader_ripper.py <game.xapk|game.apk|thư mục Data> -o <thư mục ra> [--filter Toony --filter Ice]
  python shader_ripper.py game.xapk --list                 # chỉ liệt kê tên shader
  python shader_ripper.py game.xapk -o out --skip-builtin  # bỏ shader có sẵn của Unity

Cần: Python 3.9+, pip install UnityPy
"""
import argparse
import io
import json
import os
import re
import struct
import sys
import tempfile
import zipfile

try:
    import UnityPy
    from UnityPy.helpers import CompressionHelper
except ImportError:
    sys.exit("Thiếu UnityPy. Cài bằng:  python -m pip install UnityPy")

# Shader có sẵn của Unity/package – bỏ qua khi dùng --skip-builtin
BUILTIN = re.compile(r"^(Hidden/|Legacy Shaders/|Mobile/|Sprites/|Skybox/|Standard|Unlit/|Particles/|UI/Default|UI/Unlit|"
                     r"Spine/|TextMeshPro|Universal Render Pipeline/|GUI/|Nature/)")

# Mã nền tảng trong Shader.platforms (ShaderCompilerPlatform)
PLATFORMS = {0: "OpenGL", 4: "D3D11", 5: "GLES20", 9: "GLES3", 14: "Metal", 18: "Vulkan", 21: "Switch"}

GLSL_RUN = re.compile(rb"#ifdef VERTEX[\x09\x0a\x0d\x20-\x7e]+")
BLEND = {0: "Zero", 1: "One", 2: "DstColor", 3: "SrcColor", 4: "OneMinusDstColor", 5: "SrcAlpha",
         6: "OneMinusSrcColor", 7: "DstAlpha", 8: "OneMinusDstAlpha", 9: "SrcAlphaSaturate", 10: "OneMinusSrcAlpha"}
CULL = {0: "Off", 1: "Front", 2: "Back"}
ZTEST = {0: "Disabled", 1: "Never", 2: "Less", 3: "Equal", 4: "LEqual", 5: "Greater", 6: "NotEqual", 7: "GEqual", 8: "Always"}
STENCIL_COMP = ZTEST
STENCIL_OP = {0: "Keep", 1: "Zero", 2: "Replace", 3: "IncrSat", 4: "DecrSat", 5: "Invert", 6: "IncrWrap", 7: "DecrWrap"}


# ---------------------------------------------------------------- Nạp dữ liệu game
def collect_inputs(path, tmp):
    """Trả về danh sách file/thư mục cho UnityPy. Tự giải nén .xapk → .apk → assets/bin/Data."""
    if os.path.isdir(path):
        return [path]
    low = path.lower()
    if low.endswith(".xapk"):
        out = []
        with zipfile.ZipFile(path) as z:
            for name in z.namelist():
                if name.lower().endswith(".apk"):
                    out += extract_apk(io.BytesIO(z.read(name)), os.path.join(tmp, os.path.basename(name)))
        return out
    if low.endswith(".apk") or low.endswith(".zip"):
        with open(path, "rb") as f:
            return extract_apk(io.BytesIO(f.read()), os.path.join(tmp, os.path.basename(path)))
    return [path]


def extract_apk(stream, dest):
    """Chỉ lấy dữ liệu Unity trong APK (assets/bin/Data + bundle trong assets/)."""
    found = []
    with zipfile.ZipFile(stream) as z:
        for name in z.namelist():
            if name.startswith("assets/bin/Data/") or name.endswith((".bundle", ".unity3d", ".assets")):
                target = os.path.join(dest, name)
                os.makedirs(os.path.dirname(target), exist_ok=True)
                with open(target, "wb") as f:
                    f.write(z.read(name))
                found.append(target)
    data_dir = os.path.join(dest, "assets", "bin", "Data")
    return [data_dir] if os.path.isdir(data_dir) else found


# ---------------------------------------------------------------- Giải mã blob
def decompress_platform(shader, pi):
    """Giải nén blob của 1 nền tảng → list các segment (bytes)."""
    blob = bytes(shader.compressedBlob)
    offs, clens, dlens = shader.offsets[pi], shader.compressedLengths[pi], shader.decompressedLengths[pi]
    if not isinstance(offs, (list, tuple)):
        offs, clens, dlens = [offs], [clens], [dlens]
    segs = []
    for o, c, d in zip(offs, clens, dlens):
        o, c, d = (getattr(v, "value", v) for v in (o, c, d))
        segs.append(CompressionHelper.decompress_lz4(blob[o:o + c], d))
    return segs


def blob_entries(segs):
    """Bảng mục lục đầu blob: count, rồi (offset, length, segment) cho mỗi mục."""
    n = struct.unpack_from("<i", segs[0], 0)[0]
    return [struct.unpack_from("<iii", segs[0], 4 + 12 * i) for i in range(n)]


def entry_code(segs, entries, index):
    off, length, seg = entries[index]
    data = segs[seg][off:off + length]
    m = GLSL_RUN.search(data)
    return (m.group().decode("ascii", "replace"), data) if m else (None, data)


# ---------------------------------------------------------------- Render state
def val(prop, table=None):
    name = getattr(prop, "name", "")
    if name and name != "<noninit>":
        return f"[{name}]"
    v = int(prop.val) if float(prop.val).is_integer() else round(prop.val, 3)
    return table.get(v, v) if table else v


def describe_state(state):
    b = state.rtBlend0
    so = state.stencilOp
    sp = getattr(so, "pass_", None) or getattr(so, "pass")
    lines = [
        f"  Cull {val(state.culling, CULL)}   ZWrite {val(state.zWrite)}   ZTest {val(state.zTest, ZTEST)}",
        f"  Blend {val(b.srcBlend, BLEND)} {val(b.destBlend, BLEND)}, {val(b.srcBlendAlpha, BLEND)} {val(b.destBlendAlpha, BLEND)}"
        f"   ColorMask {val(b.colMask)}",
        f"  Stencil Ref {val(state.stencilRef)} Comp {val(so.comp, STENCIL_COMP)} Pass {val(sp, STENCIL_OP)}"
        f" ReadMask {val(state.stencilReadMask)} WriteMask {val(state.stencilWriteMask)}",
    ]
    tags = dict(state.m_Tags.tags) if hasattr(state.m_Tags, "tags") else {}
    if tags:
        lines.insert(0, "  Tags " + " ".join(f'"{k}"="{v}"' for k, v in tags.items()))
    return "\n".join(lines)


def raw(prop):
    """Giá trị state: tên property (VD '_Cull') hoặc số."""
    name = getattr(prop, "name", "")
    if name and name != "<noninit>":
        return name
    return float(prop.val)


def state_dict(st):
    b = st.rtBlend0
    so = st.stencilOp
    sp = getattr(so, "pass_", None) or getattr(so, "pass")
    return {
        "cull": raw(st.culling), "zwrite": raw(st.zWrite), "ztest": raw(st.zTest),
        "src": raw(b.srcBlend), "dst": raw(b.destBlend), "srcA": raw(b.srcBlendAlpha), "dstA": raw(b.destBlendAlpha),
        "colmask": raw(b.colMask), "offsetFactor": raw(st.offsetFactor), "offsetUnits": raw(st.offsetUnits),
        "stencilRef": raw(st.stencilRef), "stencilRead": raw(st.stencilReadMask), "stencilWrite": raw(st.stencilWriteMask),
        "stencilComp": raw(so.comp), "stencilPass": raw(sp), "stencilFail": raw(so.fail), "stencilZFail": raw(so.zFail),
    }


# ---------------------------------------------------------------- Xuất 1 shader
def safe(name):
    return re.sub(r'[\\/:*?"<>| ]', "_", name)


def rip_shader(shader, out_root):
    pf = shader.m_ParsedForm
    name = pf.m_Name
    folder = os.path.join(out_root, safe(name))
    os.makedirs(folder, exist_ok=True)

    try:
        lab = shader.export()
    except Exception as e:  # UnityPy chưa hỗ trợ hết định dạng mới – vẫn tiếp tục lấy GLSL
        lab = f"// UnityPy export lỗi: {e}\nShader \"{name}\""
    with open(os.path.join(folder, "_shaderlab.txt"), "w", encoding="utf-8") as f:
        f.write(lab)

    kw_names = list(getattr(pf, "m_KeywordNames", []) or [])
    states, index = [], []
    passes_json = []  # dữ liệu có cấu trúc cho glsl_to_unity.py
    platforms = list(shader.platforms)
    for pi, plat in enumerate(platforms):
        pname = PLATFORMS.get(plat, f"P{plat}")
        try:
            segs = decompress_platform(shader, pi)
            entries = blob_entries(segs)
        except Exception as e:
            index.append(f"!{pname}\tgiải nén lỗi: {e}")
            continue
        seen = set()
        for si, ss in enumerate(pf.m_SubShaders):
            for p_i, p in enumerate(ss.m_Passes):
                st = p.m_State
                tags = dict(st.m_Tags.tags) if hasattr(st.m_Tags, "tags") else {}
                lightmode = tags.get("LIGHTMODE", "none")
                if pi == 0:
                    states.append(f"SubShader {si} Pass {p_i} \"{st.m_Name}\" LightMode={lightmode}\n{describe_state(st)}")
                    passes_json.append({"subshader": si, "pass": p_i, "name": st.m_Name, "tags": tags,
                                        "subshader_tags": dict(ss.m_Tags.tags) if hasattr(ss.m_Tags, "tags") else {},
                                        "state": state_dict(st), "variants": []})
                pass_entry = next(e for e in passes_json if e["subshader"] == si and e["pass"] == p_i)
                for stage in ("progVertex", "progFragment"):
                    prog = getattr(p, stage, None)
                    if prog is None:
                        continue
                    groups = getattr(prog, "m_PlayerSubPrograms", None) or [getattr(prog, "m_SubPrograms", [])]
                    for group in groups:
                        for sp in group:
                            bi = sp.m_BlobIndex
                            if (bi, stage) in seen or bi >= len(entries):
                                continue
                            seen.add((bi, stage))
                            kws = sorted(kw_names[k] for k in sp.m_KeywordIndices if k < len(kw_names)) \
                                if kw_names else [str(k) for k in sp.m_KeywordIndices]
                            code, raw = entry_code(segs, entries, bi)
                            base = f"s{si}p{p_i}_{lightmode}_{stage[4:]}_{pname}_{bi}"
                            if code is not None:
                                fn = base + ".glsl"
                                with open(os.path.join(folder, fn), "w", encoding="utf-8") as f:
                                    f.write(f"// shader: {name}\n// keywords: {' '.join(kws)}\n{code}")
                            else:
                                fn = base + ".bin"  # Vulkan SPIR-V / Metal...: lưu thô (dùng spirv-cross để dịch)
                                with open(os.path.join(folder, fn), "wb") as f:
                                    f.write(raw)
                            index.append(f"{fn}\t{' '.join(kws)}")
                            pass_entry["variants"].append({"file": fn, "keywords": kws, "stage": stage[4:], "platform": pname})

    with open(os.path.join(folder, "_states.txt"), "w", encoding="utf-8") as f:
        f.write("\n\n".join(states))
    with open(os.path.join(folder, "_index.tsv"), "w", encoding="utf-8") as f:
        f.write("\n".join(index))
    with open(os.path.join(folder, "_passes.json"), "w", encoding="utf-8") as f:
        json.dump({"name": name, "fallback": pf.m_FallbackName, "passes": passes_json}, f, ensure_ascii=False, indent=1)
    glsl = sum(1 for line in index if line.split("\t")[0].endswith(".glsl"))
    return name, [PLATFORMS.get(p, p) for p in platforms], glsl, len(index)


# ---------------------------------------------------------------- Main
def main():
    for stream in (sys.stdout, sys.stderr):  # console Windows (cp125x) không in được tiếng Việt
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            pass
    ap =argparse.ArgumentParser(description="Đục mã GLSL của shader từ game Unity Android")
    ap.add_argument("input", help=".xapk / .apk / thư mục assets/bin/Data")
    ap.add_argument("-o", "--out", default="shader_rip", help="thư mục xuất (mặc định ./shader_rip)")
    ap.add_argument("-f", "--filter", action="append", default=[], help="chỉ lấy shader có tên chứa chuỗi này (lặp được)")
    ap.add_argument("--skip-builtin", action="store_true", help="bỏ shader có sẵn của Unity / TMP / Spine / URP")
    ap.add_argument("--list", action="store_true", help="chỉ liệt kê tên shader rồi thoát")
    args = ap.parse_args()

    # UnityPy giữ file mở tới khi thoát → bỏ qua lỗi khi xoá thư mục tạm
    with tempfile.TemporaryDirectory(ignore_cleanup_errors=True) as tmp:
        print("Đang đọc dữ liệu game...")
        env = UnityPy.load(*collect_inputs(args.input, tmp))
        shaders = []
        for obj in env.objects:
            if obj.type.name != "Shader":
                continue
            try:
                sh = obj.read()
                name = sh.m_ParsedForm.m_Name
            except Exception as e:
                print(f"  bỏ qua shader path_id={obj.path_id}: {e}")
                continue
            if args.skip_builtin and BUILTIN.match(name):
                continue
            if args.filter and not any(f.lower() in name.lower() for f in args.filter):
                continue
            shaders.append(sh)

        if args.list:
            for sh in sorted(shaders, key=lambda s: s.m_ParsedForm.m_Name):
                print(sh.m_ParsedForm.m_Name)
            print(f"Tổng: {len(shaders)} shader")
            return

        os.makedirs(args.out, exist_ok=True)
        rows = []
        for sh in shaders:
            try:
                name, plats, glsl, total = rip_shader(sh, args.out)
                rows.append((name, plats, glsl, total))
                print(f"  {glsl:5} GLSL / {total:5} mục   {name}")
            except Exception as e:
                print(f"  LỖI {sh.m_ParsedForm.m_Name}: {e}")

        with open(os.path.join(args.out, "_summary.md"), "w", encoding="utf-8") as f:
            f.write("| Shader | Nền tảng | Variant GLSL | Tổng mục |\n|---|---|---|---|\n")
            for name, plats, glsl, total in sorted(rows):
                note = " ⚠️ không có code (bị strip khỏi build?)" if total == 0 else ""
                f.write(f"| {name}{note} | {', '.join(map(str, plats))} | {glsl} | {total} |\n")
        print(f"\nXong {len(rows)} shader → {os.path.abspath(args.out)}  (xem _summary.md)")


if __name__ == "__main__":
    main()
