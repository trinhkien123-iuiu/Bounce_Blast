"""
ui_extract.py – tách UI gốc (canvas trong scene / prefab của bản AssetRipper) thành prefab dùng được trong project.

Làm gì:
  1. Đọc YAML scene/prefab gốc, lấy nguyên cây GameObject theo tên (giữ RectTransform, Image, TMP, Button, Animator...).
  2. Script trong DLL gốc (UnityEngine.UI.dll, Unity.TextMeshPro.dll) được ánh xạ sang script của package trong project
     bằng fileID = MD4("s\\0\\0\\0" + namespace + class) (cách Unity định danh script nằm trong DLL).
  3. Script .cs của game: nếu project có file cùng GUID (VD MirroredImage.cs) thì giữ, không thì bỏ component đó
     (KPFramework UIText/UIButton, IAP, liveops... – logic sẽ viết lại bằng code của mình).
  4. Tham chiếu tới object nằm ngoài cây → null. Asset (sprite, texture, material, font, animator...) chưa có trong project
     thì chép từ bản rip sang Assets/_Project/Art/UI/Menu/Extra/ (đổi bằng --assets-out) (giữ GUID), đệ quy theo dependency.

Cách dùng:
  python ui_extract.py --src <scene.unity|prefab> --name "GameCanvas" --out Assets/_Project/UI/Prefabs/GameCanvas.prefab
                       [--path "MenuManager/GameCanvas"] [--project D:/GitHub/Bounce_Blast] [--rip D:/toolGame/game/ExportedProject]
"""
import argparse
import glob
import os
import re
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from md4 import script_file_id  # noqa: E402

HEADER = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"
REF = re.compile(r"\{fileID: (-?\d+)(?:, guid: ([0-9a-f]{32}), type: (\d+))?\}")
CLASS_DECL = re.compile(r"^\s*(?:public|internal)?\s*(?:sealed\s+|abstract\s+|partial\s+)*class\s+(\w+)", re.M)
NAMESPACE_DECL = re.compile(r"^\s*namespace\s+([\w.]+)", re.M)


# ---------------------------------------------------------------- YAML
def load_docs(path):
    text = open(path, encoding="utf-8").read()
    docs = []
    for part in re.split(r"\n(?=--- !u!)", text)[1:]:
        m = re.match(r"--- !u!(\d+) &(-?\d+)( stripped)?", part)
        docs.append({"cls": int(m.group(1)), "id": m.group(2), "stripped": bool(m.group(3)), "text": part})
    return docs


def tree(docs):
    by_id = {d["id"]: d for d in docs}
    go_name, go_comps, tr_of_go, father, children = {}, {}, {}, {}, {}
    for d in docs:
        t = d["text"]
        if d["cls"] == 1:
            m = re.search(r"m_Name: (.*)", t)
            go_name[d["id"]] = m.group(1).strip() if m else ""
            go_comps[d["id"]] = re.findall(r"component: \{fileID: (-?\d+)\}", t)
        elif d["cls"] in (4, 224):
            g = re.search(r"m_GameObject: \{fileID: (-?\d+)\}", t).group(1)
            tr_of_go[g] = d["id"]
            father[d["id"]] = re.search(r"m_Father: \{fileID: (-?\d+)\}", t).group(1)
            block = t.split("m_Children:")[1].split("m_Father")[0] if "m_Children:" in t else ""
            children[d["id"]] = re.findall(r"\{fileID: (-?\d+)\}", block)
    go_of_tr = {v: k for k, v in tr_of_go.items()}
    return by_id, go_name, go_comps, tr_of_go, go_of_tr, father, children


def find_root(info, name, path=None):
    by_id, go_name, go_comps, tr_of_go, go_of_tr, father, children = info

    def full_path(t):
        p = []
        while t in go_of_tr:
            p.append(go_name[go_of_tr[t]])
            t = father.get(t, "0")
        return "/".join(reversed(p))

    hits = [t for t, g in go_of_tr.items() if go_name[g] == name and (path is None or full_path(t).endswith(path))]
    if not hits:
        raise SystemExit(f"Không tìm thấy '{name}' (path={path})")
    if len(hits) > 1:
        print(f"  ! có {len(hits)} object tên '{name}', lấy cái đầu: {full_path(hits[0])}")
    return hits[0]


def collect_subtree(info, root_tr, skip=()):
    """Mọi object dưới root_tr; bỏ nguyên nhánh nếu tên object nằm trong `skip` (VD tính năng chưa làm: Shop, Leaderboards)."""
    by_id, go_name, go_comps, tr_of_go, go_of_tr, father, children = info
    ids, stack = [], [root_tr]
    while stack:
        t = stack.pop()
        g = go_of_tr[t]
        if t != root_tr and go_name[g] in skip:
            continue
        ids.append(g)
        ids.extend(go_comps[g])
        stack.extend(children.get(t, []))
    return ids


# ---------------------------------------------------------------- Ánh xạ script
def package_script_map(project):
    """{fileID DLL (MD4)} → guid .cs trong package (ugui, textmeshpro)."""
    result = {}
    roots = glob.glob(os.path.join(project, "Library", "PackageCache", "com.unity.ugui@*")) + \
        glob.glob(os.path.join(project, "Library", "PackageCache", "com.unity.textmeshpro@*"))
    for root in roots:
        for cs in glob.glob(os.path.join(root, "**", "*.cs"), recursive=True):
            if os.sep + "Editor" + os.sep in cs or "/Editor/" in cs.replace("\\", "/") or not os.path.exists(cs + ".meta"):
                continue
            src = open(cs, encoding="utf-8", errors="ignore").read()
            ns = NAMESPACE_DECL.search(src)
            namespace = ns.group(1) if ns else ""
            guid = re.search(r"guid: (\w+)", open(cs + ".meta", encoding="utf-8").read()).group(1)
            base = os.path.splitext(os.path.basename(cs))[0]
            for cls in CLASS_DECL.findall(src):
                if cls == base:  # Unity chỉ coi class trùng tên file là MonoScript của file đó
                    result[script_file_id(namespace, cls)] = guid
    return result


def project_guids(project):
    guids = {}
    for base in ("Assets", "Packages", os.path.join("Library", "PackageCache")):
        for meta in glob.glob(os.path.join(project, base, "**", "*.meta"), recursive=True):
            try:
                g = re.search(r"guid: (\w+)", open(meta, encoding="utf-8", errors="ignore").read(400))
            except OSError:
                continue
            if g:
                guids[g.group(1)] = meta[:-5]
    return guids


def rip_guids(rip):
    out = {}
    for meta in glob.glob(os.path.join(rip, "Assets", "**", "*.meta"), recursive=True):
        g = re.search(r"guid: (\w+)", open(meta, encoding="utf-8", errors="ignore").read(400))
        if g:
            out[g.group(1)] = meta[:-5]
    return out


# ---------------------------------------------------------------- Chép asset thiếu
COPY_EXT = {".asset", ".png", ".jpg", ".tga", ".mat", ".controller", ".anim", ".ttf", ".otf", ".prefab", ".psd", ".exr"}


def copy_missing(guid, proj, rip, dest_root, copied, depth=0):
    if guid in proj or guid in copied or guid not in rip or depth > 6:
        return
    src = rip[guid]
    ext = os.path.splitext(src)[1].lower()
    if ext not in COPY_EXT or os.path.isdir(src):
        return
    rel = os.path.relpath(src, os.path.join(os.path.dirname(dest_root), "..", "..")) if False else os.path.basename(src)
    sub = {".png": "Texture2D", ".jpg": "Texture2D", ".tga": "Texture2D", ".exr": "Texture2D", ".psd": "Texture2D",
           ".mat": "Material", ".controller": "AnimatorController", ".anim": "AnimationClip", ".ttf": "Font", ".otf": "Font",
           ".prefab": "Prefab", ".asset": "Asset"}[ext]
    dst_dir = os.path.join(dest_root, sub)
    os.makedirs(dst_dir, exist_ok=True)
    dst = os.path.join(dst_dir, rel)
    if os.path.exists(dst):  # trùng tên khác GUID → thêm hậu tố
        stem, e = os.path.splitext(rel)
        dst = os.path.join(dst_dir, f"{stem}_{guid[:6]}{e}")
    shutil.copyfile(src, dst)
    shutil.copyfile(src + ".meta", dst + ".meta")
    copied[guid] = dst
    if ext in (".asset", ".mat", ".controller", ".anim", ".prefab"):
        text = open(src, encoding="utf-8", errors="ignore").read()
        for _, g, _ in REF.findall(text):
            if g:
                copy_missing(g, proj, rip, dest_root, copied, depth + 1)


# ---------------------------------------------------------------- Main
def extract(args):
    docs = load_docs(args.src)
    info = tree(docs)
    by_id, go_name, go_comps, tr_of_go, go_of_tr, father, children = info
    root_tr = find_root(info, args.name, args.path)
    keep_ids = collect_subtree(info, root_tr, set(args.skip))
    keep = set(keep_ids)

    pkg_map = package_script_map(args.project)
    proj = project_guids(args.project)
    rip = rip_guids(args.rip)

    removed, stats_kept, stats_removed = set(), {}, {}
    for i in keep_ids:
        d = by_id.get(i)
        if d is None or d["cls"] != 114:
            continue
        m = re.search(r"m_Script: \{fileID: (-?\d+), guid: (\w+), type: (\d)\}", d["text"])
        if not m:
            removed.add(i)
            continue
        fid, guid = int(m.group(1)), m.group(2)
        name = os.path.basename(rip.get(guid, guid))
        if fid in pkg_map and guid not in proj:
            d["text"] = d["text"].replace(m.group(0), f"m_Script: {{fileID: 11500000, guid: {pkg_map[fid]}, type: 3}}")
            stats_kept[name] = stats_kept.get(name, 0) + 1
        elif guid in proj:
            stats_kept[name] = stats_kept.get(name, 0) + 1
        else:
            removed.add(i)
            stats_removed[name] = stats_removed.get(name, 0) + 1

    out_docs = []
    for i in keep_ids:
        if i in removed or i not in by_id:
            continue
        d = by_id[i]
        text = d["text"]
        if d["cls"] == 1:
            text = re.sub(r"  - component: \{fileID: (-?\d+)\}\n",
                          lambda m: "" if m.group(1) in removed else m.group(0), text)
        if i == root_tr:
            text = re.sub(r"m_Father: \{fileID: -?\d+\}", "m_Father: {fileID: 0}", text)

        def fix_ref(m):
            fid, guid = m.group(1), m.group(2)
            if guid:
                copy_missing(guid, proj, rip, args.assets_out, copied)
                return m.group(0)
            if fid == "0" or (fid in keep and fid not in removed):
                return m.group(0)
            return "{fileID: 0}"
        text = REF.sub(fix_ref, text)
        out_docs.append(text)

    os.makedirs(os.path.dirname(args.out), exist_ok=True)
    with open(args.out, "w", encoding="utf-8", newline="\n") as f:
        f.write(HEADER + "\n".join(t.rstrip("\n") for t in out_docs) + "\n")

    print(f"OK {args.name} → {args.out}: {len(out_docs)} object")
    print("  giữ script : " + ", ".join(f"{k}×{v}" for k, v in sorted(stats_kept.items(), key=lambda x: -x[1])))
    print("  bỏ script  : " + ", ".join(f"{k}×{v}" for k, v in sorted(stats_removed.items(), key=lambda x: -x[1])[:12]))


copied = {}


def main():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            pass
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", required=True)
    ap.add_argument("--name", required=True)
    ap.add_argument("--path", default=None, help="đuôi đường dẫn để phân biệt object trùng tên")
    ap.add_argument("--out", required=True)
    ap.add_argument("--skip", nargs="*", default=[], help="tên các object (cả nhánh) bỏ qua, VD \"Shop Panel\"")
    ap.add_argument("--project", default=r"D:/GitHub/Bounce_Blast")
    ap.add_argument("--rip", default=r"D:/toolGame/game/ExportedProject")
    ap.add_argument("--assets-out", default=r"D:/GitHub/Bounce_Blast/Assets/_Project/Art/UI/Menu/Extra")
    args = ap.parse_args()
    extract(args)
    if copied:
        print(f"  chép thêm {len(copied)} asset vào {args.assets_out}")


if __name__ == "__main__":
    main()
