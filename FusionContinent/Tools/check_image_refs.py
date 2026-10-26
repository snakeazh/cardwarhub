#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
图片引用检查工具 - 检查指定文件夹下的图片是否被 Unity 资源通过 GUID 引用。

仅检测 GUID 引用链: 读取每张图片 .meta 文件里的 guid, 然后在整个 Unity 工程的
YAML 文本资源 (.prefab/.mat/.spriteatlasv2/.anim/.controller/.asset 等) 里搜索该 guid。
命中 = 已引用; 未命中 = 未引用 (候选删除项)。

使用方式:
    # 检查 (干跑, 默认)
    python check_image_refs.py Assets/Sprites/Icon/ItemIcon

    # 显式指定 Unity 工程根 (默认自动推断为 FusionContinent/)
    python check_image_refs.py <图片文件夹> --unity-root FusionContinent

    # 同时检查多个文件夹
    python check_image_refs.py Assets/Sprites/Icon/ItemIcon Assets/Sprites/InWar

    # 干跑确认无误后, 真正删除未引用的图片 (含 .meta)
    python check_image_refs.py <图片文件夹> --delete

    # 强制删除, 不弹确认
    python check_image_refs.py <图片文件夹> --delete --yes

    # 关闭递归 / 自定义排除目录 / 不生成 JSON 报告
    python check_image_refs.py <文件夹> --no-recursive
    python check_image_refs.py <文件夹> --exclude Library Temp --no-report

说明:
    - 路径既可相对 (相对于当前工作目录) 也可绝对; 内部统一转绝对路径处理。
    - --unity-root 同样支持相对/绝对, 默认推断为离目标文件夹最近、向上首个含 Assets 子目录的目录。
"""

import argparse
import datetime
import json
import os
import re
import sys


# 图片扩展名 (小写, 含点)
IMAGE_EXTS = {
    ".png", ".jpg", ".jpeg", ".tif", ".tiff", ".tga",
    ".psd", ".bmp", ".exr", ".iff", ".pict", ".webp",
}

# 需要扫描 guid 的 Unity 文本资源扩展名 (小写, 含点)
# 这些都是 YAML/文本格式, 可能含 "guid: <32hex>" 引用
REF_SCAN_EXTS = {
    ".prefab", ".mat", ".spriteatlas", ".spriteatlasv2",
    ".anim", ".controller", ".asset", ".font", ".guiskin",
    ".flare", ".rendertexture", ".preset", ".shader",
    ".shadergraph", ".compute", ".cginc", ".hlsl", ".glslinc",
    ".physicmaterial", ".physicsmaterial2d",
}

# 默认排除的目录名 (匹配任意层级, 仅按名字)
DEFAULT_EXCLUDE_DIRS = {
    # Unity 生成/缓存
    "library", "temp", "obj", "bundles", "yoo", "hybridclrdata",
    "logs", "usersettings", ".git", ".vs", ".idea",
    "build", "buildreports", "release", "debug",
    # 常见第三方插件目录
    "plugins", "samples", "qframework",
    "spine examples", "spine-unity", "enhancedscroller v2",
    "demostreamingassets",
}

# 预编译 guid 提取正则; Unity meta / YAML 里都是 "guid: <32位十六进制>"
GUID_RE = re.compile(rb'guid:\s*([0-9a-fA-F]{32})')

# 进度打印间隔
PROGRESS_EVERY = 2000


def to_abspath(p):
    """相对路径转绝对路径 (相对于 cwd), 规范化分隔符。"""
    if not os.path.isabs(p):
        p = os.path.join(os.getcwd(), p)
    return os.path.normpath(p)


def infer_unity_root(target_dir):
    """
    从目标文件夹向上找, 返回首个含 'Assets' 子目录的祖先目录;
    找不到则回退到目标目录本身 (后续扫描就以它为根, 范围会窄但能跑)。
    """
    d = target_dir
    last = d
    while True:
        if os.path.isdir(os.path.join(d, "Assets")):
            return d
        parent = os.path.dirname(d)
        if parent == d:  # 到达盘根
            break
        d = parent
    return last


def is_excluded(path_parts, exclude_set):
    """路径任一层级目录名落在 exclude_set (已小写) 即视为排除。"""
    for part in path_parts:
        if part.lower() in exclude_set:
            return True
    return False


def walk_files(root, recursive, exclude_set):
    """遍历 root 下文件; recursive=False 时只看顶层。"""
    if not recursive:
        for name in os.listdir(root):
            full = os.path.join(root, name)
            if os.path.isfile(full):
                yield full
        return
    for dirpath, dirnames, filenames in os.walk(root):
        # 原地裁剪 dirnames, 避免下钻被排除目录 (按目录名)
        # 注意: 这里按单层目录名过滤; 完整路径层级排除在下面 file 处理再做一次
        pruned = []
        for d in dirnames:
            if d.lower() in exclude_set:
                continue
            pruned.append(d)
        dirnames[:] = pruned
        for fn in filenames:
            yield os.path.join(dirpath, fn)


def collect_images(target_dirs, recursive, exclude_set, quiet):
    """
    收集目标文件夹下的图片。
    返回:
        images: [{path, meta_path, guid}], guid 可能为 None
        missing_meta: [图片路径]
    """
    images = []
    missing_meta = []
    seen = set()

    for target in target_dirs:
        if not os.path.isdir(target):
            print("[ERROR] 目标文件夹不存在: %s" % target, file=sys.stderr)
            sys.exit(2)
        if not quiet:
            print("[INFO] 收集图片: %s (recursive=%s)" % (target, recursive))
        for full in walk_files(target, recursive, exclude_set):
            ext = os.path.splitext(full)[1].lower()
            if ext not in IMAGE_EXTS:
                continue
            # 跳过 .meta 自身 (它不是图片)
            # 排除层级检查 (双重保险)
            rel = os.path.relpath(full, target)
            # 跳过被排除目录里的文件 (虽然 walk 已裁剪, 再校验一次更稳)
            np = full
            if is_excluded(np.split(os.sep), exclude_set):
                continue
            ap = os.path.normpath(full)
            if ap in seen:
                continue
            seen.add(ap)
            meta_path = ap + ".meta"
            guid = None
            if os.path.isfile(meta_path):
                guid = read_guid_from_meta(meta_path)
                if guid is None:
                    # meta 存在但读不到 guid, 视作缺 meta 同等处理并提示
                    missing_meta.append(ap)
                else:
                    images.append({"path": ap, "meta_path": meta_path, "guid": guid})
            else:
                missing_meta.append(ap)

    return images, missing_meta


def read_guid_from_meta(meta_path):
    """从 .meta 文件读 guid; 读不到返回 None。以二进制读, 避免编码问题。"""
    try:
        with open(meta_path, "rb") as f:
            # meta 很小, 整体读
            data = f.read(4096)
        m = GUID_RE.search(data)
        if m:
            return m.group(1).decode("ascii")
    except OSError:
        pass
    return None


def scan_references(unity_root, exclude_set, target_guid_set, quiet):
    """
    扫描整个 Unity 工程的可引用资源, 建立 {guid: [引用文件...]}。
    只记录命中 target_guid_set 的 guid, 节省内存。
    返回 dict: {guid: [abs ref paths]}
    """
    if not os.path.isdir(unity_root):
        print("[ERROR] Unity 工程根不存在: %s" % unity_root, file=sys.stderr)
        sys.exit(2)

    refs = {g: [] for g in target_guid_set}
    scanned = 0
    if not quiet:
        print("[INFO] 扫描 Unity 工程引用: %s" % unity_root)

    for full in walk_files(unity_root, True, exclude_set):
        ext = os.path.splitext(full)[1].lower()
        if ext not in REF_SCAN_EXTS:
            continue
        scanned += 1
        if not quiet and scanned % PROGRESS_EVERY == 0:
            print("[INFO]   已扫描 %d 个资源文件..." % scanned)
        try:
            with open(full, "rb") as f:
                data = f.read()
        except OSError:
            continue
        # 一次扫描全文, 提取所有 guid; 仅记录在目标集合里的
        # 用 set 去重同一文件内多次引用
        found = set(m.group(1) for m in GUID_RE.finditer(data))
        if not found:
            continue
        found_str = {g.decode("ascii") for g in found}
        hit = found_str & target_guid_set
        if hit:
            for g in hit:
                refs.setdefault(g, []).append(full)

    if not quiet:
        print("[INFO]   扫描完成, 共扫描 %d 个资源文件" % scanned)
    return refs


def make_report(images, refs, missing_meta, unity_root):
    """构造每张图的结果条目。"""
    items = []
    for img in images:
        g = img["guid"]
        ref_list = refs.get(g, [])
        items.append({
            "path": img["path"],
            "rel_path": os.path.relpath(img["path"], unity_root),
            "guid": g,
            "referenced": len(ref_list) > 0,
            "refs": ref_list,
        })
    return items


def print_summary(items, missing_meta, unity_root, quiet):
    total = len(items)
    referenced = sum(1 for it in items if it["referenced"])
    unreferenced = total - referenced
    print("")
    print("================ 检查结果 ================")
    print("Unity 工程根 : %s" % unity_root)
    print("目标图片总数 : %d" % total)
    print("  已引用     : %d" % referenced)
    print("  未引用     : %d" % unreferenced)
    print("缺 .meta    : %d" % len(missing_meta))
    print("==========================================")

    if missing_meta:
        print("\n[!] 以下图片缺少 .meta (无法检测 GUID, 跳过):")
        for p in missing_meta:
            try:
                show = os.path.relpath(p, unity_root)
            except ValueError:
                show = p
            print("    " + show)

    if unreferenced:
        print("\n[-] 未引用图片清单 (候选删除项):")
        for it in items:
            if not it["referenced"]:
                print("    " + it["rel_path"])
    else:
        print("\n[OK] 所有目标图片均被引用。")


def write_json_report(items, missing_meta, unity_root, report_path):
    report = {
        "generated_at": datetime.datetime.now().isoformat(timespec="seconds"),
        "unity_root": unity_root,
        "summary": {
            "total": len(items),
            "referenced": sum(1 for it in items if it["referenced"]),
            "unreferenced": sum(1 for it in items if not it["referenced"]),
            "missing_meta": len(missing_meta),
        },
        "images": items,
        "missing_meta": missing_meta,
    }
    try:
        with open(report_path, "w", encoding="utf-8") as f:
            json.dump(report, f, ensure_ascii=False, indent=2)
        print("\n[INFO] JSON 报告已写入: %s" % report_path)
    except OSError as e:
        print("[WARN] 写 JSON 报告失败: %s" % e, file=sys.stderr)


def delete_unreferenced(items, assume_yes, quiet):
    """删除未引用图片及其 .meta; 返回 (成功数, 失败数)。"""
    targets = [it for it in items if not it["referenced"]]
    if not targets:
        print("\n[INFO] 没有需要删除的未引用图片。")
        return 0, 0

    print("\n即将删除以下 %d 个未引用图片 (含 .meta):" % len(targets))
    for it in targets:
        print("    " + it["path"])

    if not assume_yes:
        try:
            ans = input("\n确认删除? 输入 y 继续, 其它取消: ").strip().lower()
        except EOFError:
            ans = ""
        if ans != "y":
            print("[INFO] 已取消, 未删除任何文件。")
            return 0, 0

    ok = 0
    fail = 0
    for it in targets:
        # 先删 .meta 再删图片 (顺序无关紧要, 但 meta 删失败不应阻止图片删除)
        for p in (it["path"] + ".meta", it["path"]):
            if not os.path.isfile(p):
                continue
            try:
                os.remove(p)
                ok += 1
                if not quiet:
                    print("[DEL] %s" % p)
            except OSError as e:
                fail += 1
                print("[FAIL] %s -> %s" % (p, e), file=sys.stderr)
    print("\n[INFO] 删除完成: 成功 %d, 失败 %d" % (ok, fail))
    return ok, fail


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="检查指定文件夹下的图片是否被 Unity 资源通过 GUID 引用。",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="示例:\n"
               "  python check_image_refs.py Assets/Sprites/Icon/ItemIcon\n"
               "  python check_image_refs.py <文件夹> --delete --yes\n",
    )
    parser.add_argument("targets", nargs="+",
                        help="要检查的图片文件夹 (可多个, 相对/绝对均可)")
    parser.add_argument("--unity-root", default=None,
                        help="Unity 工程根目录 (含 Assets/ 的目录); 默认自动推断")
    parser.add_argument("--no-recursive", action="store_true",
                        help="不递归子文件夹, 仅看顶层")
    parser.add_argument("--exclude", nargs="*", default=None,
                        help="覆盖默认排除目录名列表 (区分大小写不敏感)")
    parser.add_argument("--no-report", action="store_true",
                        help="不生成 JSON 报告文件")
    parser.add_argument("--delete", action="store_true",
                        help="删除未引用的图片 (含 .meta); 默认为干跑不删")
    parser.add_argument("--yes", action="store_true",
                        help="删除模式下跳过确认")
    parser.add_argument("--quiet", action="store_true",
                        help="减少进度输出")
    args = parser.parse_args(argv)

    # 解析目标文件夹
    target_dirs = [to_abspath(t) for t in args.targets]

    # 排除目录集合
    exclude_set = set(d.lower() for d in args.exclude) if args.exclude is not None \
        else set(DEFAULT_EXCLUDE_DIRS)

    # 推断 Unity 根
    unity_root = args.unity_root
    if unity_root:
        unity_root = to_abspath(unity_root)
    else:
        unity_root = infer_unity_root(target_dirs[0])
    if not os.path.isdir(unity_root):
        print("[ERROR] Unity 工程根不存在: %s" % unity_root, file=sys.stderr)
        sys.exit(2)

    # 1. 收集图片
    images, missing_meta = collect_images(
        target_dirs, recursive=not args.no_recursive,
        exclude_set=exclude_set, quiet=args.quiet,
    )
    if not images:
        print("[WARN] 未找到任何图片 (with .meta), 检查路径/排除项是否正确。")
        if not args.no_report:
            # 仍写一份空报告便于排查
            ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
            report_path = os.path.join(
                os.path.dirname(os.path.abspath(__file__)),
                "check_image_refs_report_%s.json" % ts,
            )
            write_json_report([], missing_meta, unity_root, report_path)
        return 0

    if not args.quiet:
        print("[INFO] 共找到 %d 张图片 (含 .meta), 缺 meta %d 张" %
              (len(images), len(missing_meta)))

    # 2. 扫描全工程引用
    target_guid_set = set(it["guid"] for it in images)
    refs = scan_references(
        unity_root, exclude_set, target_guid_set, quiet=args.quiet,
    )

    # 3. 汇总 & 输出
    items = make_report(images, refs, missing_meta, unity_root)
    print_summary(items, missing_meta, unity_root, args.quiet)

    # 4. JSON 报告
    if not args.no_report:
        ts = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        report_path = os.path.join(
            os.path.dirname(os.path.abspath(__file__)),
            "check_image_refs_report_%s.json" % ts,
        )
        write_json_report(items, missing_meta, unity_root, report_path)

    # 5. 删除模式
    if args.delete:
        ok, fail = delete_unreferenced(items, assume_yes=args.yes, quiet=args.quiet)
        if fail > 0:
            sys.exit(3)

    return 0


if __name__ == "__main__":
    sys.exit(main())
