"""
glsl_to_unity.py – dịch shader đã đục (output của shader_ripper.py) thành file .shader dùng được trong Unity
Built-in RP, GIỮ NGUYÊN 100% phép tính của bản build gốc (không viết lại bằng tay).

Nguyên tắc:
  * Mã GLSL do hlslcc sinh ra rất "máy móc" → dịch từng dòng sang HLSL theo luật cố định
    (vecN→floatN, mix→lerp, fract→frac, texture→tex2D, hlslcc_mtx4x4X[i]→transpose(X)[i], ...).
  * Mỗi tổ hợp keyword của material (VD TCP2_SPECULAR + TCP2_SHADOW_LIGHT_COLOR) → đúng variant gốc tương ứng.
    Keyword "hệ thống" (DIRECTIONAL, SHADOWS_SCREEN, INSTANCING_ON...) không đổi được code nên chọn 1 bản chuẩn:
    ưu tiên DIRECTIONAL + LIGHTPROBE_SH (ánh sáng chính + ambient probe), không bóng đổ.
  * GPU instancing: nếu bản build có variant INSTANCING_ON → đọc khối `UnityInstancing_<Tên>` (VD CubeProps:
    _CubeShadeOffset, _UseBakedCubeShadeOffset) và sinh UNITY_INSTANCING_BUFFER + UNITY_ACCESS_INSTANCED_PROP,
    #pragma multi_compile_instancing. Công thức vẫn lấy từ variant thường (giống hệt variant instancing, chỉ khác
    nguồn dữ liệu ma trận / thuộc tính) → MaterialPropertyBlock + instancing hoạt động như game gốc.
  * Mỗi khối code ghi chú variant lấy từ file GLSL nào và ứng với keyword nào (dễ đối chiếu).
  * Render state (Cull/ZWrite/Blend/Stencil...), Tags, Properties lấy từ dữ liệu gốc.
  * .meta mang GUID của shader trong bản AssetRipper → material cũ tự trỏ đúng.

Cách dùng:
  python glsl_to_unity.py <thư mục output ripper> -o <Assets/.../Shaders> [--guid-from <ExportedProject/Assets>]
                          [-f "Toony"] [--name-map map.json]

Giới hạn (ghi rõ để biết khi so với game gốc):
  - Bỏ pass ForwardAdd / ShadowCaster / Deferred / Meta (game chỉ dùng 1 đèn directional).
  - Không nhận bóng đổ (variant SHADOWS_SCREEN bị bỏ qua).
"""
import argparse
import hashlib
import json
import os
import re
import sys

SYSTEM_KEYWORDS = {
    "DIRECTIONAL", "LIGHTPROBE_SH", "SHADOWS_SCREEN", "VERTEXLIGHT_ON", "INSTANCING_ON", "LOD_FADE_CROSSFADE",
    "POINT", "SPOT", "POINT_COOKIE", "DIRECTIONAL_COOKIE", "SHADOWS_DEPTH", "SHADOWS_CUBE", "SHADOWS_SOFT",
    "SHADOWS_SHADOWMASK", "LIGHTMAP_ON", "DIRLIGHTMAP_COMBINED", "DYNAMICLIGHTMAP_ON", "LIGHTMAP_SHADOW_MIXING",
    "FOG_LINEAR", "FOG_EXP", "FOG_EXP2", "STEREO_INSTANCING_ON", "UNITY_SINGLE_PASS_STEREO", "STEREO_MULTIVIEW_ON",
    "STEREO_CUBEMAP_RENDER_ON", "UNITY_HDR_ON", "PROCEDURAL_INSTANCING_ON", "DOTS_INSTANCING_ON", "SOFTPARTICLES_ON",
    "UNITY_UI_CLIP_RECT_HACK",
}
# Thứ tự ưu tiên bộ keyword hệ thống khi chọn variant chuẩn
SYSTEM_PREFERENCE = [{"DIRECTIONAL", "LIGHTPROBE_SH"}, {"DIRECTIONAL"}, {"LIGHTPROBE_SH"}, set()]
SKIP_LIGHTMODES = {"FORWARDADD", "SHADOWCASTER", "DEFERRED", "META", "PREPASSBASE", "PREPASSFINAL", "MOTIONVECTORS"}

# Uniform do Unity khai báo sẵn (UnityShaderVariables / UnityLightingCommon / UnityCG) → không khai báo lại
BUILTIN_UNIFORM = re.compile(
    r"^(unity_\w+|_WorldSpaceCameraPos|_WorldSpaceLightPos0|_LightColor0|_Time|_SinTime|_CosTime|_ScreenParams|"
    r"_ProjectionParams|_ZBufferParams|glstate_\w+|_LightShadowData|_LightPositionRange|_LightProjectionParams|"
    r"_ShadowMapTexture|_SpecColor|_LightTexture0|_LightTextureB0|unity_DeltaTime)$")

# Tên unity_* mà UnityCG.cginc KHÔNG khai báo sẵn → vẫn phải tự khai báo
UNDECLARED_BUILTINS = {"unity_NHxRoughness"}

SEMANTIC = {"in_POSITION0": "POSITION", "in_NORMAL0": "NORMAL", "in_TANGENT0": "TANGENT", "in_COLOR0": "COLOR"}
TYPE_MAP = {"vec": "float", "ivec": "int", "uvec": "uint", "bvec": "bool"}


# ---------------------------------------------------------------- Đọc GLSL
def split_stages(text):
    v_start = text.find("#ifdef VERTEX")
    f_start = text.find("#ifdef FRAGMENT")
    if v_start < 0 or f_start < 0:
        return None, None
    return text[v_start + len("#ifdef VERTEX"):f_start], text[f_start + len("#ifdef FRAGMENT"):]


def preprocess(src):
    """Bỏ chỉ thị preprocessor; với #ifdef UNITY_ADRENO_ES3 giữ nhánh #else (code chuẩn)."""
    out, stack = [], []  # stack: (đang_bỏ, là_khối_adreno)
    for line in src.splitlines():
        s = line.strip()
        if s.startswith("#"):
            if re.match(r"#if(def)?\s+UNITY_ADRENO_ES3", s):
                stack.append(["adreno", True])
            elif s.startswith("#if"):
                stack.append(["other", False])
            elif s.startswith("#else") and stack and stack[-1][0] == "adreno":
                stack[-1][1] = False
            elif s.startswith("#endif") and stack:
                stack.pop()
            continue
        if any(skip for kind, skip in stack if kind == "adreno"):
            continue
        out.append(line)
    return "\n".join(out)


DECL = re.compile(r"^\s*(?:layout\([^)]*\)\s*)?(?:UNITY_LOCATION\(\d+\)\s*)?(?:flat\s+|noperspective\s+|centroid\s+)*"
                  r"(?:(uniform|in|out|const)\s+)?(?:(?:highp|mediump|lowp|precise)\s+)?(\w+)\s+(\w+)\s*(\[\s*\d+\s*\])?\s*(=.*)?;\s*$")


def parse_stage(src):
    """Trả về (uniforms, inputs, outputs, globals, body)."""
    src = preprocess(src)
    src = re.sub(r"\bUNITY_BINDING\(\d+\)\s*uniform\s+\w+\s*\{", "", src)
    src = src.replace("UNITY_UNIFORM", "uniform")
    main = re.search(r"void\s+main\s*\(\s*\)\s*\{", src)
    if not main:
        raise ValueError("không thấy main()")
    head, rest = src[:main.start()], src[main.end():]
    depth, end = 1, None
    for i, ch in enumerate(rest):
        if ch == "{":
            depth += 1
        elif ch == "}":
            depth -= 1
            if depth == 0:
                end = i
                break
    body = rest[:end]

    uniforms, inputs, outputs, globals_ = [], [], [], []
    in_struct = False
    for line in head.splitlines():
        s = line.strip()
        if not s or s.startswith("//") or s in ("};", "}"):
            in_struct = False if s in ("};", "}") else in_struct
            continue
        if s.startswith("struct "):
            in_struct = True
            continue
        if in_struct:
            continue
        m = DECL.match(s)
        if not m:
            continue
        qual, typ, name, arr, init = m.groups()
        arr = (arr or "").replace(" ", "")
        if qual == "uniform" or (qual is None and (typ.startswith("sampler") or BUILTIN_UNIFORM.match(name) or
                                                    name.startswith("hlslcc_mtx") or name.startswith("_"))):
            uniforms.append((typ, name, arr))
        elif qual == "in":
            inputs.append((typ, name, arr))
        elif qual == "out":
            outputs.append((typ, name, arr))
        else:
            globals_.append((qual, typ, name, arr, init))
    return uniforms, inputs, outputs, globals_, body


# ---------------------------------------------------------------- Dịch biểu thức
CALL = re.compile(r"\b([A-Za-z_]\w*)\s*(\[\s*\d+\s*\])?\s*\(")
SIMPLE_RENAME = {
    "fract": "frac", "inversesqrt": "rsqrt", "dFdx": "ddx", "dFdy": "ddy", "roundEven": "round",
    "floatBitsToUint": "asuint", "floatBitsToInt": "asint", "uintBitsToFloat": "asfloat", "intBitsToFloat": "asfloat",
    "mix": "lerp",
}
COMPARE = {"greaterThanEqual": ">=", "greaterThan": ">", "lessThanEqual": "<=", "lessThan": "<", "equal": "==", "notEqual": "!="}


def split_args(s):
    args, depth, cur = [], 0, ""
    for ch in s:
        if ch in "([":
            depth += 1
        elif ch in ")]":
            depth -= 1
        if ch == "," and depth == 0:
            args.append(cur.strip())
            cur = ""
        else:
            cur += ch
    if cur.strip():
        args.append(cur.strip())
    return args


def hlsl_type(t):
    m = re.match(r"^(vec|ivec|uvec|bvec)([234])$", t)
    if m:
        return TYPE_MAP[m.group(1)] + m.group(2)
    m = re.match(r"^mat([234])$", t)
    if m:
        return f"float{m.group(1)}x{m.group(1)}"
    return {"sampler2D": "sampler2D", "samplerCube": "samplerCUBE", "sampler3D": "sampler3D"}.get(t, t)


def convert(expr, cube_samplers, spec_cubes):
    out, i = "", 0
    while True:
        m = CALL.search(expr, i)
        if not m:
            out += expr[i:]
            return out
        name, arr = m.group(1), m.group(2)
        # tìm ngoặc đóng tương ứng
        depth, j = 1, m.end()
        while j < len(expr) and depth:
            if expr[j] == "(":
                depth += 1
            elif expr[j] == ")":
                depth -= 1
            j += 1
        inner = convert(expr[m.end():j - 1], cube_samplers, spec_cubes)
        args = split_args(inner)
        out += expr[i:m.start()]
        i = j

        if arr:  # vec4[4](a,b,c,d) → {a,b,c,d}
            out += "{" + ", ".join(args) + "}"
            continue
        t = hlsl_type(name)
        if re.match(r"^(vec|ivec|uvec|bvec)[234]$", name) or name in ("float", "int", "uint", "bool"):
            out += f"(({t})({args[0]}))" if len(args) == 1 else f"{t}({', '.join(args)})"
        elif name in COMPARE:
            out += f"(({args[0]}) {COMPARE[name]} ({args[1]}))"
        elif name == "mod":
            out += f"(({args[0]}) - ({args[1]}) * floor(({args[0]}) / ({args[1]})))"
        elif name == "atan" and len(args) == 2:
            out += f"atan2({args[0]}, {args[1]})"
        elif name == "texture":
            s = args[0]
            if s in spec_cubes:
                out += f"UNITY_SAMPLE_TEXCUBE({s}, {args[1]})"
            elif s in cube_samplers:
                out += f"texCUBE({s}, {args[1]})"
            else:
                out += f"tex2D({s}, ({args[1]}).xy)"
        elif name == "textureLod":
            s = args[0]
            if s in spec_cubes:
                out += f"UNITY_SAMPLE_TEXCUBE_LOD({s}, {args[1]}, {args[2]})"
            elif s in cube_samplers:
                out += f"texCUBElod({s}, float4({args[1]}, {args[2]}))"
            else:
                out += f"tex2Dlod({s}, float4(({args[1]}).xy, 0.0, {args[2]}))"
        else:
            out += f"{SIMPLE_RENAME.get(name, name)}({', '.join(args)})"


def matrices(code):
    # hlslcc_mtx4x4X[k] = cột k của ma trận X; mảng ma trận (VD unity_WorldToShadow[16]) → X[k/4] cột k%4
    def repl(m):
        name, idx = m.group(1), m.group(2)
        if idx.isdigit() and name in ("unity_WorldToShadow",):
            k = int(idx)
            return f"transpose({name}[{k // 4}])[{k % 4}]"
        return f"transpose({name})[{idx}]"
    return re.sub(r"hlslcc_mtx4x4(\w+)\[([^\]]+)\]", repl, code)


def translate_body(body, renames, cube_samplers, spec_cubes, ret):
    code = re.sub(r"\b(highp|mediump|lowp|precise)\s+", "", body)
    code = matrices(code)
    code = convert(code, cube_samplers, spec_cubes)
    for t in ("vec", "ivec", "uvec", "bvec"):
        code = re.sub(rf"\b{t}([234])\b", lambda m, t=t: TYPE_MAP[t] + m.group(1), code)
    for old, new in renames.items():
        code = re.sub(rf"\b{re.escape(old)}\b", new, code)
    code = re.sub(r"\breturn\s*;", f"return {ret};", code)
    # Vòng lặp có lấy mẫu texture: D3D cần unroll được (hlslcc dùng while(true)+break) → gợi ý số lần tối đa
    if re.search(r"\b(tex2D|texCUBE)\s*\(", code):
        code = re.sub(r"(?m)^(\s*)(for\s*\(|while\s*\()", r"\1[unroll(8)] \2", code)
    return code


# ---------------------------------------------------------------- Instancing
INST_STRUCT = re.compile(r"struct\s+(\w+)Array_Type\s*\{(.*?)\};", re.S)
INST_MEMBER = re.compile(r"(?:(?:highp|mediump|lowp)\s+)?(\w+)\s+(\w+)\s*;")


def instanced_props(folder, pass_info):
    """{tên thuộc tính: (tên buffer, kiểu GLSL)} từ các variant INSTANCING_ON của pass (bỏ buffer có sẵn của Unity)."""
    props = {}
    for v in pass_info["variants"]:
        if "INSTANCING_ON" not in v["keywords"] or not v["file"].endswith(".glsl"):
            continue
        text = open(os.path.join(folder, v["file"]), encoding="utf-8").read()
        for buf, members in INST_STRUCT.findall(text):
            if buf.startswith("unity_"):
                continue
            for typ, name in INST_MEMBER.findall(members):
                if not name.startswith("hlslcc_mtx"):
                    props[name] = (buf, typ)
    return props


def has_instancing(pass_info):
    return any("INSTANCING_ON" in v["keywords"] for v in pass_info["variants"])


# ---------------------------------------------------------------- Sinh 1 variant
def build_variant(glsl, tag, inst=None, instancing=False):
    inst = inst or {}
    vsrc, fsrc = split_stages(glsl)
    if vsrc is None:
        raise ValueError("thiếu VERTEX/FRAGMENT")
    vu, vin, vout, vglob, vbody = parse_stage(vsrc)
    fu, fin, fout, fglob, fbody = parse_stage(fsrc)

    uniforms = {}
    for typ, name, arr in vu + fu:
        if name.startswith("hlslcc_mtx4x4") or typ.endswith("Shadow"):
            continue
        if BUILTIN_UNIFORM.match(name) and name not in UNDECLARED_BUILTINS:
            continue
        if name in inst:
            continue  # khai báo trong UNITY_INSTANCING_BUFFER
        uniforms[name] = (typ, arr)
    cube_samplers = {n for n, (t, a) in uniforms.items() if t == "samplerCube"}
    spec_cubes = {name for typ, name, arr in vu + fu if typ == "samplerCube" and name.startswith("unity_SpecCube")}

    varyings = [(t, n, a) for t, n, a in vout if n != "gl_Position"]
    frag_out = [n for t, n, a in fout]

    lines = []
    lines.append(f"struct appdata_{tag} {{")
    for idx, (t, n, a) in enumerate(vin):
        sem = SEMANTIC.get(n) or (re.sub(r"^in_", "", n) if n.startswith("in_TEXCOORD") else f"TEXCOORD{idx}")
        lines.append(f"    {hlsl_type(t)} {n} : {sem};")
    if instancing:
        lines.append("    UNITY_VERTEX_INPUT_INSTANCE_ID")
    lines.append("};")
    lines.append(f"struct v2f_{tag} {{")
    lines.append("    float4 pos : SV_POSITION;")
    for idx, (t, n, a) in enumerate(varyings):
        lines.append(f"    {hlsl_type(t)} {n} : TEXCOORD{idx};")
    if instancing:
        lines.append("    UNITY_VERTEX_INPUT_INSTANCE_ID")
    lines.append("};")

    # ----- vertex
    vren = {n: f"v.{n}" for t, n, a in vin}
    vren.update({n: f"o.{n}" for t, n, a in varyings})
    vren["gl_Position"] = "o.pos"
    lines.append(f"v2f_{tag} vert_{tag}(appdata_{tag} v) {{")
    lines.append(f"    v2f_{tag} o = (v2f_{tag})0;")
    if instancing:
        lines.append("    UNITY_SETUP_INSTANCE_ID(v);")
        lines.append("    UNITY_TRANSFER_INSTANCE_ID(v, o);")
    lines += local_decls(vglob)
    lines.append(inst_access(translate_body(vbody, vren, cube_samplers, spec_cubes, "o"), inst))
    lines.append("    return o;")
    lines.append("}")

    # ----- fragment
    fren = {n: f"i.{n}" for t, n, a in fin if any(n == vn for _, vn, _ in varyings)}
    fren["gl_FrontFacing"] = "(vface > 0)"
    fren["gl_FragCoord"] = "i.pos"
    target = frag_out[0] if frag_out else "SV_Target0"
    lines.append(f"float4 frag_{tag}(v2f_{tag} i, float vface : VFACE) : SV_Target {{")
    lines.append(f"    float4 {target} = 0;")
    if instancing:
        lines.append("    UNITY_SETUP_INSTANCE_ID(i);")
    lines += local_decls(fglob)
    lines.append(inst_access(translate_body(fbody, fren, cube_samplers, spec_cubes, target), inst))
    lines.append(f"    return {target};")
    lines.append("}")
    return "\n".join(lines), uniforms


def inst_access(code, inst):
    for name, (buf, _) in inst.items():
        code = re.sub(rf"\b{re.escape(name)}\b", f"UNITY_ACCESS_INSTANCED_PROP({buf}, {name})", code)
    return code


def local_decls(globs):
    out = []
    for qual, typ, name, arr, init in globs:
        if name in ("gl_Position",):
            continue
        prefix = "const " if qual == "const" else ""
        t = hlsl_type(re.sub(r"\b(highp|mediump|lowp)\s+", "", typ))
        if init:
            init_conv = convert(init[1:].strip(), set(), set())
            out.append(f"    {prefix}{t} {name}{arr} = {init_conv};")
        else:
            out.append(f"    {t} {name}{arr};")
    return out


# ---------------------------------------------------------------- ShaderLab
ZTEST = {0: "Always", 1: "Never", 2: "Less", 3: "Equal", 4: "LEqual", 5: "Greater", 6: "NotEqual", 7: "GEqual", 8: "Always"}
CULL = {0: "Off", 1: "Front", 2: "Back"}
BLEND = {0: "Zero", 1: "One", 2: "DstColor", 3: "SrcColor", 4: "OneMinusDstColor", 5: "SrcAlpha",
         6: "OneMinusSrcColor", 7: "DstAlpha", 8: "OneMinusDstAlpha", 9: "SrcAlphaSaturate", 10: "OneMinusSrcAlpha"}
SCOMP = {0: "Always", 1: "Never", 2: "Less", 3: "Equal", 4: "LEqual", 5: "Greater", 6: "NotEqual", 7: "GEqual", 8: "Always"}
SOP = {0: "Keep", 1: "Zero", 2: "Replace", 3: "IncrSat", 4: "DecrSat", 5: "Invert", 6: "IncrWrap", 7: "DecrWrap"}
COLMASK = {15: "RGBA", 14: "RGB", 0: "0", 8: "A", 7: "RGB"}


def sv(v, table=None):
    if isinstance(v, str):
        return f"[{v}]"
    iv = int(v)
    return table.get(iv, str(iv)) if table else (str(iv) if float(v).is_integer() else str(v))


def state_lines(st):
    out = [f"Cull {sv(st['cull'], CULL)}", f"ZWrite {sv(st['zwrite'], {0: 'Off', 1: 'On'})}",
           f"ZTest {sv(st['ztest'], ZTEST)}"]
    src, dst, srcA, dstA = (sv(st[k], BLEND) for k in ("src", "dst", "srcA", "dstA"))
    if not (src == "One" and dst == "Zero" and srcA == "One" and dstA == "Zero"):
        out.append(f"Blend {src} {dst}, {srcA} {dstA}" if (srcA, dstA) != (src, dst) else f"Blend {src} {dst}")
    cm = st["colmask"]
    if isinstance(cm, str) or int(cm) != 15:
        out.append(f"ColorMask {sv(cm, COLMASK)}")
    if not (isinstance(st["offsetFactor"], float) and st["offsetFactor"] == 0 and st["offsetUnits"] == 0):
        out.append(f"Offset {sv(st['offsetFactor'])}, {sv(st['offsetUnits'])}")
    default_stencil = (st["stencilRef"] == 0 and st["stencilComp"] in (0, 8) and st["stencilPass"] == 0)
    if not default_stencil:
        out += ["Stencil {", f"    Ref {sv(st['stencilRef'])}", f"    ReadMask {sv(st['stencilRead'])}",
                f"    WriteMask {sv(st['stencilWrite'])}", f"    Comp {sv(st['stencilComp'], SCOMP)}",
                f"    Pass {sv(st['stencilPass'], SOP)}", f"    Fail {sv(st['stencilFail'], SOP)}",
                f"    ZFail {sv(st['stencilZFail'], SOP)}", "}"]
    return out


def tag_line(tags):
    pretty = {"LIGHTMODE": "LightMode", "QUEUE": "Queue", "RENDERTYPE": "RenderType", "IGNOREPROJECTOR": "IgnoreProjector",
              "SHADOWSUPPORT": "ShadowSupport", "CANUSESPRITEATLAS": "CanUseSpriteAtlas", "PREVIEWTYPE": "PreviewType",
              "DISABLEBATCHING": "DisableBatching"}
    lm_values = {"FORWARDBASE": "ForwardBase", "ALWAYS": "Always", "VERTEX": "Vertex"}
    parts = []
    for k, v in tags.items():
        key = pretty.get(k.upper(), k)
        if key == "LightMode":
            v = lm_values.get(v.upper(), v)
        parts.append(f'"{key}"="{v}"')
    return "Tags { " + " ".join(parts) + " }" if parts else ""


def properties_block(shaderlab):
    m = re.search(r"^Properties \{\n(.*?)\n\}\n(?:SubShader|Fall|CustomEditor|\})", shaderlab, re.S | re.M)
    if not m:
        return []
    out = []
    for raw in m.group(1).splitlines():
        s = raw.strip()
        if not s:
            continue
        name = re.sub(r"^(\s*\[[^\]]*\]\s*)*", "", s).split("(")[0].strip()
        if name.startswith("__"):
            continue
        # chỉ giữ drawer có sẵn trong Unity; drawer riêng (TCP2...) → bỏ để không lỗi inspector
        attrs = re.findall(r"\[([^\[\]]*)\]", s.split('"')[0])
        rest = re.sub(r"^(\s*\[[^\]]*\]\s*)*", "", s)
        keep = []
        for a in attrs:
            if a.startswith(("TCP2HeaderToggle(",)):
                keep.append("Toggle(" + a[len("TCP2HeaderToggle("):])
            elif a in ("TCP2HeaderToggle", "TCP2ToggleNoKeyword"):
                keep.append("ToggleUI")
            elif a.startswith(("Toggle", "ToggleOff", "Enum", "KeywordEnum", "PowerSlider", "IntRange", "Header",
                               "HDR", "Gamma", "NoScaleOffset", "Normal", "HideInInspector", "PerRendererData",
                               "MainTexture", "MainColor", "Space")) and not a.startswith("TCP2"):
                keep.append(a)
        rest = re.sub(r'\bColor\b\) = \(', 'Color) = (', rest)
        out.append("".join(f"[{a}] " for a in keep) + rest)
    return out


# ---------------------------------------------------------------- Ghép shader
def pick_variants(pass_info):
    """{tổ hợp keyword material (frozenset) → file variant chuẩn nhất}."""
    groups = {}
    for v in pass_info["variants"]:
        if v["stage"] != "Vertex" or not v["file"].endswith(".glsl"):
            continue
        kws = set(v["keywords"])
        mat = frozenset(k for k in kws if k not in SYSTEM_KEYWORDS)
        sysk = kws & SYSTEM_KEYWORDS
        score = next((i for i, pref in enumerate(SYSTEM_PREFERENCE) if sysk == pref), 10 + len(sysk))
        if mat not in groups or score < groups[mat][0]:
            groups[mat] = (score, v["file"])
    return {k: f for k, (s, f) in groups.items()}


def build_shader(folder, name_override=None):
    info = json.load(open(os.path.join(folder, "_passes.json"), encoding="utf-8"))
    lab = open(os.path.join(folder, "_shaderlab.txt"), encoding="utf-8").read()
    name = name_override or info["name"]

    passes_code, uniforms_all, sub_tags, inst_all = [], {}, {}, {}
    for p in info["passes"]:
        lm = p["tags"].get("LIGHTMODE", "").upper()
        if lm in SKIP_LIGHTMODES:
            continue
        variants = pick_variants(p)
        if not variants:
            continue
        sub_tags = p.get("subshader_tags") or sub_tags
        all_mat_kw = sorted(set().union(*variants.keys()))
        instancing = has_instancing(p)
        inst = instanced_props(folder, p) if instancing else {}
        inst_all.update(inst)
        var_kw = {}  # tag → (file, keyword) để ghi chú

        # khử trùng: variant khác keyword nhưng code giống nhau dùng chung
        bodies, combos = {}, []
        for combo, fn in sorted(variants.items(), key=lambda kv: -len(kv[0])):
            glsl = open(os.path.join(folder, fn), encoding="utf-8").read()
            h = hashlib.md5(glsl.split("\n", 2)[2].encode()).hexdigest()[:8]
            if h not in bodies:
                tag = f"v{len(bodies)}"
                try:
                    code, unis = build_variant(glsl, tag, inst, instancing)
                except Exception as e:
                    print(f"    ! bỏ variant {fn}: {e}")
                    continue
                bodies[h] = (tag, code)
                uniforms_all.update(unis)
                var_kw[tag] = (fn, " ".join(sorted(combo)) or "(không keyword)")
            combos.append((combo, bodies[h][0]))
        if not combos:
            continue

        # chọn variant mặc định khi material không khớp tổ hợp nào: tổ hợp rỗng, nếu không có thì nhiều keyword nhất
        default_tag = next((t for c, t in combos if not c), combos[0][1])
        dispatch = []
        first = True
        for combo, tag in combos:
            if not combo:
                continue
            conds = [f"defined({k})" for k in sorted(combo)] + [f"!defined({k})" for k in all_mat_kw if k not in combo]
            dispatch.append(f"{'#if' if first else '#elif'} {' && '.join(conds)}")
            dispatch.append(f"    #define VTAG {tag}\n    #define VTAG_ID {tag[1:]}")
            first = False
        if dispatch:
            dispatch.append("#else")
        dispatch.append(f"    #define VTAG {default_tag}\n    #define VTAG_ID {default_tag[1:]}")
        if len(dispatch) > 1:
            dispatch.append("#endif")

        tags = {k: v for k, v in p["tags"].items() if k.upper() not in ("QUEUE", "RENDERTYPE", "IGNOREPROJECTOR")}
        pl = ["Pass", "{"]
        if p["name"]:
            pl.append(f'    Name "{p["name"]}"')
        if tag_line(tags):
            pl.append("    " + tag_line(tags))
        pl += ["    " + s for s in state_lines(p["state"])]
        pl += ["", "    CGPROGRAM", "    #pragma vertex vert_entry", "    #pragma fragment frag_entry", "    #pragma target 3.0"]
        pl += [f"    #pragma shader_feature_local __ {k}" for k in all_mat_kw]
        if instancing:
            pl.append("    #pragma multi_compile_instancing")
        pl += ["    #include \"UnityCG.cginc\"", "    #include \"UnityLightingCommon.cginc\"", "    __UNIFORMS__", ""]
        pl += ["    " + d.replace("\n", "\n    ") for d in dispatch]
        for _, (tag, code) in bodies.items():
            src_file, kw = var_kw.get(tag, ("?", "?"))
            pl.append(f"    // ---- {tag}: {src_file}  |  keyword material: {kw}")
            pl.append(f"    #if VTAG_ID == {tag[1:]}")
            pl.append("    " + code.replace("\n", "\n    "))
            pl.append("    #endif")
        pl += ["    #define BB_CAT2(a, b) a##b", "    #define BB_CAT(a, b) BB_CAT2(a, b)",
               "    BB_CAT(v2f_, VTAG) vert_entry(BB_CAT(appdata_, VTAG) v) { return BB_CAT(vert_, VTAG)(v); }",
               "    float4 frag_entry(BB_CAT(v2f_, VTAG) i, float vface : VFACE) : SV_Target { return BB_CAT(frag_, VTAG)(i, vface); }",
               "    ENDCG", "}"]
        passes_code.append(pl)

    if not passes_code:
        return None

    # khai báo uniform dùng chung cho mọi pass
    decl = []
    for n, (t, arr) in sorted(uniforms_all.items()):
        decl.append(f"{hlsl_type(t)} {n}{arr};")
    bufs = {}
    for n, (b, t) in sorted(inst_all.items()):
        bufs.setdefault(b, []).append((n, t))
    for b, members in bufs.items():
        decl.append(f"UNITY_INSTANCING_BUFFER_START({b})")
        decl += [f"    UNITY_DEFINE_INSTANCED_PROP({hlsl_type(t)}, {n})" for n, t in members]
        decl.append(f"UNITY_INSTANCING_BUFFER_END({b})")
    uni_block = "\n    ".join(decl)

    out = [f"// Tự động dịch từ GLSL của bản build gốc bằng Tools/ShaderRipper/glsl_to_unity.py – KHÔNG sửa tay.",
           f'Shader "{name}"', "{", "    Properties", "    {"]
    out += ["        " + p for p in properties_block(lab)]
    out += ["    }", "    SubShader", "    {"]
    if sub_tags:
        out.append("        " + tag_line(sub_tags))
    for pl in passes_code:
        text = "\n".join("        " + l for l in pl)
        text = text.replace("__UNIFORMS__", uni_block.replace("\n    ", "\n            "))
        out.append(text)
    out += ["    }", "    Fallback Off" if not info.get("fallback") or "Error" in info.get("fallback", "")
            else f'    Fallback "{info["fallback"]}"', "}"]
    return "\n".join(out)


def main():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            pass
    ap = argparse.ArgumentParser(description="Dịch shader đã đục (GLSL) sang .shader Unity Built-in")
    ap.add_argument("ripdir")
    ap.add_argument("-o", "--out", required=True)
    ap.add_argument("-f", "--filter", action="append", default=[])
    ap.add_argument("--guid-from", help="thư mục Assets của bản AssetRipper để lấy GUID gốc")
    ap.add_argument("--file-map", help="json {tên shader: tên file .shader} để ghi đè đúng file có sẵn")
    args = ap.parse_args()

    guid = {}
    if args.guid_from:
        for root, _, files in os.walk(args.guid_from):
            for fn in files:
                if fn.endswith(".shader"):
                    p = os.path.join(root, fn)
                    first = open(p, encoding="utf-8", errors="ignore").readline()
                    m = re.match(r'Shader "(.*)"', first)
                    if m and os.path.exists(p + ".meta"):
                        g = re.search(r"guid: (\w+)", open(p + ".meta", encoding="utf-8").read())
                        if g:
                            guid[m.group(1)] = g.group(1)
    file_map = json.load(open(args.file_map, encoding="utf-8")) if args.file_map else {}

    os.makedirs(args.out, exist_ok=True)
    for sub in sorted(os.listdir(args.ripdir)):
        folder = os.path.join(args.ripdir, sub)
        if not os.path.isfile(os.path.join(folder, "_passes.json")):
            continue
        name = json.load(open(os.path.join(folder, "_passes.json"), encoding="utf-8"))["name"]
        if args.filter and not any(f.lower() in name.lower() for f in args.filter):
            continue
        try:
            text = build_shader(folder)
        except Exception as e:
            print(f"LỖI {name}: {e}")
            continue
        if text is None:
            print(f"bỏ qua {name} (không có variant GLSL)")
            continue
        fn = file_map.get(name) or re.sub(r'[\\/:*?"<>| ]', "_", name) + ".shader"
        path = os.path.join(args.out, fn)
        open(path, "w", encoding="utf-8", newline="\n").write(text)
        if name in guid and not os.path.exists(path + ".meta"):
            open(path + ".meta", "w", encoding="utf-8", newline="\n").write(
                f"fileFormatVersion: 2\nguid: {guid[name]}\nShaderImporter:\n  externalObjects: {{}}\n  defaultTextures: []\n"
                f"  nonModifiableTextures: []\n  preprocessorOverride: 0\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
        print(f"OK  {name} → {fn}")


if __name__ == "__main__":
    main()
