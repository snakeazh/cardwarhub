using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// 图片引用检查工具 —— 检查指定文件夹下的图片是否被 Unity 资源引用。
///
/// 工作原理（仅检测 GUID 引用链）：
///   1) 收集目标文件夹下的图片（Texture2D / Sprite）及其 GUID；
///   2) 扫描全工程所有可引用资源（Prefab / Material / SpriteAtlas / AnimationController /
///      AnimationClip / .asset 等）的 YAML 文本，提取其中出现的 guid；
///   3) 另把 SpriteAtlas 的 packables（文件夹/单图）里包含的图片也视为“被引用”
///      （图集按名取图，prefab 里不一定出现 guid）；
///   4) 凡 guid 未出现在任何引用方 → 判为未引用（候选删除项）。
///
/// 用法：
///   - 菜单 Tools/图片引用检查/检查选中文件夹  —— 检查选中的文件夹/资源
///   - 菜单 Tools/图片引用检查/扫描整个工程    —— 扫描整个 Assets
///   - 或在 Project 窗口右键选中文件夹 → Assets/检查图片引用
///   - 选中未引用结果条目可 Ping；删除按钮二次确认后才会真删（含 .meta）。
///
/// 注意：本项目立绘 / 大量 icon 是运行时通过图集按“名字”加载
/// （GetSprite("名字")，名字来自配置表 Icon 字段），这类图片不会被 GUID 引用。
/// 工具已尽量把 SpriteAtlas packables 内的图算作“被引用”，但仍可能有遗漏，
/// 删除前请务必人工复核。
/// </summary>
public class ImageReferenceChecker : EditorWindow
{
    // 需要扫描 guid 的资源扩展名（小写，含点）。
    // 这些都是 YAML/文本格式，可能含 "guid: <32hex>" 引用。
    private static readonly HashSet<string> RefScanExts = new HashSet<string>
    {
        ".prefab", ".mat", ".spriteatlas", ".spriteatlasv2",
        ".anim", ".controller", ".asset", ".font", ".guiskin",
        ".flare", ".rendertexture", ".preset", ".shader",
        ".shadergraph", ".compute", ".cginc", ".hlsl", ".glslinc",
        ".physicmaterial", ".physicsmaterial2d",
    };

    // 单次扫描进度上报间隔
    private const int ProgressEvery = 500;

    // ========== 运行期状态 ==========
    private List<ImageEntry> _results = new List<ImageEntry>();
    private Vector2 _leftScroll;
    private Vector2 _rightScroll;

    // 多选：当前选中的行索引集合；_lastClicked 用于右侧详情显示
    private readonly HashSet<int> _selectedSet = new HashSet<int>();
    private int _lastClicked = -1;

    // 列表过滤模式：默认显示全部（含已引用 + 未引用）
    private enum FilterMode { All, Referenced, Unreferenced }
    private FilterMode _filter = FilterMode.All;

    private string _lastScanScope = "";

    // 列表行样式（从 "OL MenuItem" clone，直接改 normal.textColor 才能真正变成绿色）
    private GUIStyle _rowStyleReferenced;
    private GUIStyle _rowStyleUnreferenced;
    // 行背景：选中 / 未选中（避免每行每帧 new GUIStyle 造成 GC）
    private GUIStyle _rowBgSelected;
    private GUIStyle _rowBgNormal;

    // 缩略图缓存：图片路径 -> 预览纹理（Unity 自动缓存，这里只缓存引用）
    private readonly Dictionary<string, Texture2D> _previewCache = new Dictionary<string, Texture2D>();
    private const float PreviewSize = 40f;

    // 缓存：guid -> 引用方路径列表（用于右侧详情）
    private readonly Dictionary<string, List<string>> _refIndex =
        new Dictionary<string, List<string>>();

    /// <summary>一条图片检查结果。</summary>
    private class ImageEntry
    {
        public string Path;          // Assets/ 相对路径
        public string Guid;
        public bool Referenced;
        public bool MissingMeta;     // 理论上 AssetPathToGUID 不会缺，保留以防异常
    }

    // ----------------------------------------------------------------
    //  菜单入口
    // ----------------------------------------------------------------

    [MenuItem("Tools/图片引用检查/检查选中文件夹")]
    private static void OpenWithSelection()
    {
        var win = GetWindow<ImageReferenceChecker>(false, "图片引用检查工具");
        win.ScanSelection();
    }

    [MenuItem("Tools/图片引用检查/扫描整个工程")]
    private static void OpenWholeProject()
    {
        var win = GetWindow<ImageReferenceChecker>(false, "图片引用检查工具");
        win.ScanPaths(new[] { "Assets" }, "整个工程");
    }

    // 选中文件夹时，右键菜单也可用（MenuItem 验证）
    [MenuItem("Assets/检查图片引用")]
    private static void ContextMenuCheck()
    {
        var win = GetWindow<ImageReferenceChecker>(false, "图片引用检查工具");
        win.ScanSelection();
    }

    // ----------------------------------------------------------------
    //  扫描入口
    // ----------------------------------------------------------------

    private void ScanSelection()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog("图片引用检查",
                "未选择任何资源。\n请在 Project 窗口选中一个文件夹（或多张图片）后再执行。", "确定");
            return;
        }

        // 把选中的文件夹展开为“需要检查图片的根路径列表”
        var roots = new List<string>();
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (AssetDatabase.IsValidFolder(path)) roots.Add(path);
            else if (IsImagePath(path)) roots.Add(path);
        }

        if (roots.Count == 0)
        {
            EditorUtility.DisplayDialog("图片引用检查",
                "选中的不是文件夹或图片。\n请选中文件夹或图片资源。", "确定");
            return;
        }

        ScanPaths(roots, "Selection");
    }

    /// <summary>
    /// 核心：扫描 roots 下的图片，并反查全工程引用。
    /// </summary>
    private void ScanPaths(IList<string> roots, string scopeLabel)
    {
        _results.Clear();
        _refIndex.Clear();
        _previewCache.Clear();
        _selectedSet.Clear();
        _lastClicked = -1;

        try
        {
            // 1) 收集目标图片
            EditorUtility.DisplayProgressBar("图片引用检查", "收集目标图片...", 0.0f);
            var imagePaths = CollectImagePaths(roots);
            if (imagePaths.Count == 0)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("图片引用检查",
                    "在指定范围内未找到任何图片。", "确定");
                return;
            }

            // 目标 guid 集合（用于扫描阶段只记录命中的引用，省内存）
            var targetGuids = new HashSet<string>();
            foreach (var p in imagePaths)
            {
                var g = AssetDatabase.AssetPathToGUID(p);
                targetGuids.Add(g);
            }

            // 2) 扫描全工程引用方，构建反向索引
            EditorUtility.DisplayProgressBar("图片引用检查", "构建反向引用索引...", 0.1f);
            if (!BuildReferenceIndex(targetGuids))
            {
                // 用户取消了扫描，直接中止（进度条与提示已在内部处理）
                return;
            }

            // 3) 把 SpriteAtlas packables 内的图也标为被引用（图集按名取图的兜底）
            EditorUtility.DisplayProgressBar("图片引用检查", "处理 SpriteAtlas packables...", 0.85f);
            MarkAtlasPackableImages(imagePaths);

            // 4) 汇总结果
            EditorUtility.DisplayProgressBar("图片引用检查", "汇总结果...", 0.95f);
            foreach (var p in imagePaths)
            {
                var g = AssetDatabase.AssetPathToGUID(p);
                _results.Add(new ImageEntry
                {
                    Path = p,
                    Guid = g,
                    Referenced = _refIndex.ContainsKey(g),
                    MissingMeta = string.IsNullOrEmpty(g) || g == "00000000000000000000000000000000",
                });
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        _lastScanScope = scopeLabel;
        var unreferenced = _results.Count(r => !r.Referenced);
        Debug.Log($"[ImageReferenceChecker] 扫描完成（{scopeLabel}）：共 {_results.Count} 张图片，" +
                  $"已引用 {_results.Count - unreferenced}，未引用 {unreferenced}。");
        Repaint();
    }

    // ----------------------------------------------------------------
    //  收集 / 索引
    // ----------------------------------------------------------------

    /// <summary>收集 roots 下所有图片路径（roots 元素可以是文件夹或单张图）。</summary>
    private static List<string> CollectImagePaths(IList<string> roots)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();
        var folderRoots = roots.Where(AssetDatabase.IsValidFolder).ToArray();
        var fileRoots = roots.Where(p => !AssetDatabase.IsValidFolder(p) && IsImagePath(p)).ToArray();

        // 文件夹：用 AssetDatabase.FindAssets 取图（递归，且只取 Texture2D）
        if (folderRoots.Length > 0)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", folderRoots);
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (!string.IsNullOrEmpty(p) && IsImagePath(p) && seen.Add(p))
                    result.Add(p);
            }
        }
        // 单张图
        foreach (var p in fileRoots)
        {
            if (seen.Add(p)) result.Add(p);
        }
        return result;
    }

    /// <summary>
    /// 扫描 Assets 下所有可引用资源（YAML 文本），把其中出现的、且属于 targetGuids
    /// 的 guid 记入 _refIndex：guid -> [引用方路径...]。返回 false 表示用户取消了扫描。
    /// </summary>
    private bool BuildReferenceIndex(HashSet<string> targetGuids)
    {
        // 扫描所有“可能引用别人”的资源类型。这里用一组 filter 一次性查全。
        // 注意：FindAssets 的 t: 过滤不覆盖所有上述扩展名（比如 .cginc/.hlsl 没有 Type），
        // 所以分两步：先按类型查主要资源，再按扩展名补扫 .asset/.mat/.controller 等也已被覆盖。
        var refFilters = new[]
        {
            "t:Prefab",
            "t:Material",
            "t:SpriteAtlas",
            "t:AnimationClip",
            "t:AnimatorController",
            "t:ScriptableObject",
            "t:Shader",
            "t:ComputeShader",
        };

        var refFiles = new HashSet<string>();
        foreach (var filter in refFilters)
        {
            var guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                if (!string.IsNullOrEmpty(p) && refFiles.Add(p)) { }
            }
        }

        // 再按扩展名补扫一次（覆盖 FindAssets 类型未覆盖的文本资源，如 .cginc/.hlsl/.flare 等）
        // 这里不展开实现，因为本项目的“图片被引用”主要来自 prefab/mat/spriteatlas/asset，
        // 已被上面类型覆盖；如有特殊需要可在此扩展。

        var total = refFiles.Count;
        var i = 0;
        var cancelled = false;
        foreach (var path in refFiles)
        {
            if (++i % ProgressEvery == 0)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                        "图片引用检查",
                        $"扫描引用方资源... {i}/{total}",
                        0.1f + 0.75f * i / Mathf.Max(1, total)))
                {
                    cancelled = true;
                    break;
                }
            }

            ScanFileForGuids(path, targetGuids, _refIndex);
        }

        if (cancelled)
        {
            _refIndex.Clear();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("图片引用检查", "已取消扫描。", "确定");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 读取一个文本资源文件，提取其中出现的 guid（仅记录命中的目标）。
    /// 资源文件可能较大，按行读减少峰值内存；guid 总是 32 位连续十六进制。
    /// </summary>
    private static void ScanFileForGuids(string assetPath, HashSet<string> targetGuids,
                                         Dictionary<string, List<string>> index)
    {
        // 仅处理已知扩展名（跳过 .cs/.txt/.json 等不会出现 guid 引用的）
        var ext = Path.GetExtension(assetPath)?.ToLowerInvariant();
        if (ext == null || !RefScanExts.Contains(ext)) return;

        string absPath;
        try
        {
            absPath = Path.GetFullPath(assetPath);
        }
        catch
        {
            return;
        }
        if (!File.Exists(absPath)) return;

        string text;
        try
        {
            text = File.ReadAllText(absPath);
        }
        catch
        {
            return;
        }

        // Unity YAML 里引用形如：guid: <32hex>
        var hitInThisFile = new HashSet<string>();
        int idx = 0;
        while ((idx = text.IndexOf("guid:", idx, System.StringComparison.Ordinal)) >= 0)
        {
            var start = idx + "guid:".Length;
            // 跳过空白
            while (start < text.Length && (text[start] == ' ' || text[start] == '\t')) start++;
            var end = start;
            while (end < text.Length && end - start < 32 &&
                   IsHexChar(text[end])) end++;
            if (end - start == 32)
            {
                var g = text.Substring(start, 32);
                if (targetGuids.Contains(g)) hitInThisFile.Add(g);
            }
            idx = end;
        }

        if (hitInThisFile.Count == 0) return;
        foreach (var g in hitInThisFile)
        {
            if (!index.TryGetValue(g, out var list))
            {
                list = new List<string>();
                index[g] = list;
            }
            list.Add(assetPath);
        }
    }

    private static bool IsHexChar(char c)
        => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    /// <summary>
    /// SpriteAtlas 的 packables 可能是文件夹或单图；这些图按名被代码取用，
    /// 在 prefab 里不一定出现 guid。把它们也标为被引用。
    /// </summary>
    private void MarkAtlasPackableImages(List<string> imagePaths)
    {
        var pathToGuid = new Dictionary<string, string>(imagePaths.Count);
        foreach (var p in imagePaths)
            pathToGuid[p] = AssetDatabase.AssetPathToGUID(p);

        var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { "Assets" });
        foreach (var g in atlasGuids)
        {
            var atlasPath = AssetDatabase.GUIDToAssetPath(g);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null) continue;

            IEnumerable<Object> packables;
            try
            {
                packables = SpriteAtlasExtensions.GetPackables(atlas);
            }
            catch
            {
                continue;
            }
            if (packables == null) continue;

            foreach (var obj in packables)
            {
                if (obj == null) continue;
                var p = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(p)) continue;

                if (AssetDatabase.IsValidFolder(p))
                {
                    // 文件夹：把其下所有图片（只要在目标集合里）标为被引用
                    var inner = AssetDatabase.FindAssets("t:Texture2D", new[] { p });
                    foreach (var ig in inner)
                    {
                        var ip = AssetDatabase.GUIDToAssetPath(ig);
                        if (pathToGuid.TryGetValue(ip, out var imageGuid))
                            RegisterReference(imageGuid, atlasPath);
                    }
                }
                else if (IsImagePath(p) && pathToGuid.TryGetValue(p, out var imageGuid))
                {
                    RegisterReference(imageGuid, atlasPath);
                }
            }
        }
    }

    private void RegisterReference(string guid, string refAssetPath)
    {
        if (string.IsNullOrEmpty(guid)) return;
        if (!_refIndex.TryGetValue(guid, out var list))
        {
            list = new List<string>();
            _refIndex[guid] = list;
        }
        if (!list.Contains(refAssetPath)) list.Add(refAssetPath);
    }

    private static bool IsImagePath(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) return false;
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".tif" ||
               ext == ".tiff" || ext == ".tga" || ext == ".psd" || ext == ".bmp" ||
               ext == ".exr" || ext == ".iff" || ext == ".pict" || ext == ".webp";
    }

    /// <summary>
    /// 懒加载行样式：从 "OL MenuItem" clone 后直接改 normal.textColor。
    /// 必须直接改 textColor —— 用 GUI.color 去乘菜单样式的固定文字色，在深色皮肤下
    /// 会变成灰白，看不出绿色。
    /// </summary>
    private void EnsureRowStyles()
    {
        if (_rowStyleReferenced != null && _rowStyleUnreferenced != null &&
            _rowBgSelected != null && _rowBgNormal != null) return;

        // 已引用：明亮绿色
        _rowStyleReferenced = new GUIStyle("OL MenuItem")
        {
            normal = { textColor = new Color(0.20f, 0.95f, 0.30f) },
            hover = { textColor = new Color(0.30f, 1.00f, 0.40f) },
            active = { textColor = new Color(0.20f, 0.95f, 0.30f) },
            onNormal = { textColor = new Color(0.20f, 0.95f, 0.30f) },
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            richText = false,
            fontSize = 12,
        };

        // 未引用：橙色
        _rowStyleUnreferenced = new GUIStyle("OL MenuItem")
        {
            normal = { textColor = new Color(1.0f, 0.62f, 0.20f) },
            hover = { textColor = new Color(1.0f, 0.72f, 0.30f) },
            active = { textColor = new Color(1.0f, 0.62f, 0.20f) },
            onNormal = { textColor = new Color(1.0f, 0.62f, 0.20f) },
            alignment = TextAnchor.MiddleLeft,
            wordWrap = false,
            fontSize = 12,
        };

        // 行背景（做成“点击按钮”样式：所有状态背景一致，避免按下时闪烁）
        // 选中：高亮蓝条；未选中：普通 Box 灰底。
        _rowBgSelected = MakeFlatButtonStyle("LO HorizontalEntry");
        _rowBgNormal = MakeFlatButtonStyle("Box");
    }

    /// <summary>
    /// 从 baseStyle 克隆一个“扁平按钮”：所有状态（normal/hover/active/onNormal...）
    /// 的背景都用 baseStyle 的 normal 背景，这样作为按钮使用时按下/悬停不会变色。
    /// </summary>
    private static GUIStyle MakeFlatButtonStyle(string baseStyle)
    {
        var src = new GUIStyle(baseStyle);
        var bg = src.normal.background;
        var s = new GUIStyle(baseStyle);
        s.normal.background = bg;
        s.hover.background = bg;
        s.active.background = bg;
        s.onNormal.background = bg;
        s.onHover.background = bg;
        s.onActive.background = bg;
        s.focused.background = bg;
        s.onFocused.background = bg;
        return s;
    }

    /// <summary>
    /// 取一张图片的预览纹理（带缓存）。AssetPreview 在后台异步生成，首次可能为空，
    /// 下一帧重绘时通常就有；缓存避免重复 GetAssetPreview 调用。
    /// </summary>
    private Texture2D GetPreview(string assetPath)
    {
        if (_previewCache.TryGetValue(assetPath, out var cached)) return cached;

        var tex = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
        // 若后台尚未生成，按 mini 缩略图兜底（mini 是同步的）
        if (tex == null)
        {
            tex = AssetDatabase.GetCachedIcon(assetPath) as Texture2D;
        }
        if (tex != null) _previewCache[assetPath] = tex;
        // 即便为 null 也缓存，避免每帧反复 LoadAssetAtPath；用户滚动后重绘会重试
        else _previewCache[assetPath] = null;
        return tex;
    }

    /// <summary>
    /// 获取某张图片被哪些资源引用，返回这些资源的名称（去掉路径和扩展名）。
    /// 例如 "Assets/Res/UIPrefab/Soldier/SoldierPanel.prefab" → "SoldierPanel"。
    /// 多个用 ", " 连接；过长截断（完整列表见右侧详情）。
    /// </summary>
    private string GetRefNames(string guid)
    {
        if (!_refIndex.TryGetValue(guid, out var refs) || refs.Count == 0)
            return "";

        var names = refs
            .Select(p => Path.GetFileNameWithoutExtension(p))
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .Take(5);

        var joined = string.Join(", ", names);
        if (refs.Count > 5)
            joined += $" 等{refs.Count}处";
        return joined;
    }

    // ----------------------------------------------------------------
    //  GUI
    // ----------------------------------------------------------------

    private void OnGUI()
    {
        DrawToolbar();

        if (_results.Count == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "尚未扫描。\n\n" +
                "用法：\n" +
                "  1) 在 Project 窗口选中一个（或多个）图片文件夹；\n" +
                "  2) 菜单 Tools/图片引用检查/检查选中文件夹；\n" +
                "  或直接点下面的按钮扫描整个工程。",
                MessageType.Info);
            if (GUILayout.Button("扫描整个工程 (Assets)", GUILayout.Height(28)))
            {
                ScanPaths(new[] { "Assets" }, "整个工程");
            }
            return;
        }

        DrawResults();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("扫描范围: " + (string.IsNullOrEmpty(_lastScanScope) ? "-" : _lastScanScope),
                GUILayout.Width(220));

            var unreferenced = _results.Count(r => !r.Referenced);
            GUILayout.Label($"总计 {_results.Count} | 已引用 {_results.Count - unreferenced} | 未引用 {unreferenced}",
                GUILayout.Width(300));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("重扫选中", EditorStyles.toolbarButton))
            {
                ScanSelection();
            }
            if (GUILayout.Button("扫全工程", EditorStyles.toolbarButton))
            {
                ScanPaths(new[] { "Assets" }, "整个工程");
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label("显示:", GUILayout.Width(40));
            _filter = (FilterMode)GUILayout.Toolbar((int)_filter,
                new[] { "全部", "仅已引用", "仅未引用" }, GUILayout.Width(240));

            GUILayout.Space(10);
            if (GUILayout.Button("全选(当前视图)", GUILayout.Width(110)))
            {
                SelectAllVisible();
            }
            if (GUILayout.Button("全不选", GUILayout.Width(60)))
            {
                _selectedSet.Clear();
                _lastClicked = -1;
                Repaint();
            }

            GUILayout.FlexibleSpace();

            // 批量删除已选
            var selectedCount = _selectedSet.Count;
            using (new EditorGUI.DisabledScope(selectedCount == 0))
            {
                if (GUILayout.Button($"删除已选 ({selectedCount})", GUILayout.Width(120)))
                {
                    DeleteSelected();
                }
            }

            GUILayout.Space(8);
            if (GUILayout.Button("导出 CSV"))
            {
                ExportCsv();
            }
            if (GUILayout.Button("导出未引用列表", GUILayout.Width(120)))
            {
                ExportUnreferencedList();
            }
        }
    }

    /// <summary>选中当前过滤模式下所有可见的行。</summary>
    private void SelectAllVisible()
    {
        _selectedSet.Clear();
        for (var i = 0; i < _results.Count; i++)
        {
            var it = _results[i];
            if (_filter == FilterMode.Referenced && !it.Referenced) continue;
            if (_filter == FilterMode.Unreferenced && it.Referenced) continue;
            _selectedSet.Add(i);
        }
        Repaint();
    }

    /// <summary>批量删除所有选中的图片（含 .meta），二次确认。</summary>
    private void DeleteSelected()
    {
        if (_selectedSet.Count == 0) return;

        // 按索引降序，避免删除时索引错位
        var indices = _selectedSet.ToList();
        indices.Sort();
        indices.Reverse();

        var preview = string.Join("\n", indices
            .Where(i => i >= 0 && i < _results.Count)
            .Select(i => _results[i].Path)
            .Take(20));
        var more = indices.Count > 20 ? $"\n... 等共 {indices.Count} 项" : "";

        if (!EditorUtility.DisplayDialog("确认批量删除",
                $"将删除以下 {indices.Count} 张图片（含 .meta）：\n\n{preview}{more}\n\n该操作不可撤销，确定继续？",
                "删除", "取消"))
        {
            return;
        }

        DeleteAssetsAndRebuildSelection(indices);
    }

    /// <summary>
    /// 删除给定索引（降序传入或内部处理）对应的图片，并重建选中集合 / lastClicked。
    /// 删除会导致 _results 索引整体前移，必须重算。
    /// </summary>
    private void DeleteAssetsAndRebuildSelection(List<int> indices)
    {
        if (indices == null || indices.Count == 0) return;

        // 去重 + 降序，保证从后往前删不会错位
        var sorted = new HashSet<int>(indices).ToList();
        sorted.Sort();
        sorted.Reverse();

        var okCount = 0;
        var failCount = 0;
        foreach (var idx in sorted)
        {
            if (idx < 0 || idx >= _results.Count) continue;
            var path = _results[idx].Path;
            if (AssetDatabase.DeleteAsset(path)) okCount++;
            else failCount++;
            _results.RemoveAt(idx);
            _previewCache.Remove(path);
        }
        AssetDatabase.Refresh();

        // 重建选中集合：原索引已失效，清空更安全
        _selectedSet.Clear();
        _lastClicked = -1;

        Debug.Log($"[ImageReferenceChecker] 批量删除完成：成功 {okCount}，失败 {failCount}。");
        if (failCount > 0)
        {
            EditorUtility.DisplayDialog("批量删除", $"成功 {okCount}，失败 {failCount}（详见控制台）。", "确定");
        }
        Repaint();
    }

    private void DrawResults()
    {
        EnsureRowStyles();

        using (new EditorGUILayout.HorizontalScope())
        {
            // 左侧列表
            using (var left = new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.5f)))
            {
                _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
                for (var i = 0; i < _results.Count; i++)
                {
                    var it = _results[i];

                    // 过滤
                    if (_filter == FilterMode.Referenced && !it.Referenced) continue;
                    if (_filter == FilterMode.Unreferenced && it.Referenced) continue;

                    // 文本：[OK]/[--] 文件名 + （已引用时）引用它的预制体名
                    var label = it.Referenced
                        ? $"[OK] {Path.GetFileName(it.Path)}  →  {GetRefNames(it.Guid)}"
                        : $"[--] {Path.GetFileName(it.Path)}";

                    // 整行用一个 Button 接管点击（Button 在被点击的当帧返回 true，
                    // 且不会像 Toggle 那样吞掉 MouseDown 导致我们的修饰键判断失效）。
                    var rowStyle = _selectedSet.Contains(i) ? _rowBgSelected : _rowBgNormal;
                    if (GUILayout.Button(GUIContent.none, rowStyle,
                            GUILayout.Height(PreviewSize + 4), GUILayout.ExpandWidth(true)))
                    {
                        var ctrl = Event.current.control || Event.current.command;
                        var shift = Event.current.shift;

                        if (shift && _lastClicked >= 0 && _lastClicked < _results.Count)
                        {
                            // 范围选择：从上次点击到当前
                            var from = Mathf.Min(_lastClicked, i);
                            var to = Mathf.Max(_lastClicked, i);
                            _selectedSet.Clear();
                            for (var k = from; k <= to; k++) _selectedSet.Add(k);
                        }
                        else if (ctrl)
                        {
                            // Ctrl/Cmd：切换该行
                            if (!_selectedSet.Add(i)) _selectedSet.Remove(i);
                        }
                        else
                        {
                            // 普通点击：仅选该行
                            _selectedSet.Clear();
                            _selectedSet.Add(i);
                        }
                        _lastClicked = i;
                        Repaint();
                    }

                    // Button 已在上面绘制（占位），用 GetLastRect 取它的 rect，
                    // 把缩略图和文字画在它上面（同一帧 Repaint 叠加绘制）。
                    var rowRect = GUILayoutUtility.GetLastRect();
                    {
                        var pad = 4f;
                        // 缩略图
                        var preview = GetPreview(it.Path);
                        var previewRect = new Rect(
                            rowRect.x + pad,
                            rowRect.y + (rowRect.height - PreviewSize) * 0.5f,
                            PreviewSize, PreviewSize);
                        if (preview != null)
                        {
                            GUI.DrawTexture(previewRect, preview, ScaleMode.ScaleToFit);
                        }
                        else
                        {
                            GUI.Label(previewRect, "?");
                        }

                        // 文本
                        var style = it.Referenced ? _rowStyleReferenced : _rowStyleUnreferenced;
                        var textRect = new Rect(
                            previewRect.xMax + 6,
                            rowRect.y,
                            rowRect.xMax - (previewRect.xMax + 6) - pad,
                            rowRect.height);
                        GUI.Label(textRect, label, style);
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            // 右侧详情
            using (new EditorGUILayout.VerticalScope())
            {
                _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
                DrawDetail();
                EditorGUILayout.EndScrollView();
            }
        }
    }

    private void DrawDetail()
    {
        if (_lastClicked < 0 || _lastClicked >= _results.Count)
        {
            EditorGUILayout.HelpBox("在左侧选择一张图片查看详情。", MessageType.Info);
            return;
        }

        var it = _results[_lastClicked];
        EditorGUILayout.LabelField("图片", it.Path);
        EditorGUILayout.LabelField("GUID", it.Guid);
        EditorGUILayout.LabelField("状态", it.Referenced ? "已引用" : "未引用");

        // 预览大图
        EditorGUILayout.Space();
        var bigPreview = AssetPreview.GetAssetPreview(AssetDatabase.LoadAssetAtPath<Object>(it.Path));
        if (bigPreview != null)
        {
            var r = GUILayoutUtility.GetRect(128, 128, GUILayout.Width(128), GUILayout.Height(128));
            GUI.DrawTexture(r, bigPreview, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUILayout.LabelField("(预览生成中...)");
        }

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Ping (定位)", GUILayout.Width(120)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(it.Path);
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }
            if (GUILayout.Button("复制路径", GUILayout.Width(120)))
            {
                GUIUtility.systemCopyBuffer = it.Path;
            }
        }

        if (!it.Referenced)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "该图片未被任何 Prefab / Material / SpriteAtlas / .asset 通过 GUID 引用。\n" +
                "注意：运行时通过图集按名字加载的图不会被 GUID 引用，删除前请人工确认。",
                MessageType.Warning);
            if (GUILayout.Button("删除这张图 (含 .meta)", GUILayout.Width(180)))
            {
                if (EditorUtility.DisplayDialog("确认删除",
                        $"将删除：\n{it.Path}\n以及它的 .meta\n\n该操作不可撤销，确定继续？",
                        "删除", "取消"))
                {
                    DeleteAssetsAndRebuildSelection(new List<int> { _lastClicked });
                }
            }
        }
        else
        {
            // 列出引用方
            if (_refIndex.TryGetValue(it.Guid, out var refs) && refs.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"被以下 {refs.Count} 处引用：", EditorStyles.boldLabel);
                foreach (var r in refs)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(r);
                        if (GUILayout.Button("Ping", GUILayout.Width(60)))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<Object>(r);
                            if (obj != null) EditorGUIUtility.PingObject(obj);
                        }
                    }
                }
            }
        }
    }

    // ----------------------------------------------------------------
    //  导出
    // ----------------------------------------------------------------

    private void ExportCsv()
    {
        if (_results.Count == 0) return;
        var dir = Path.Combine("Builds", "Reports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"image_reference_report_{System.DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("path,guid,referenced,ref_count,refs");
        foreach (var it in _results)
        {
            var refs = _refIndex.TryGetValue(it.Guid, out var list) ? list : new List<string>();
            var refsJoined = string.Join(" | ", refs);
            sb.AppendLine($"{it.Path},{it.Guid},{it.Referenced},{refs.Count},{refsJoined}");
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[ImageReferenceChecker] CSV 报告已保存：{path}");
        EditorUtility.DisplayDialog("图片引用检查", $"CSV 报告已保存：\n{path}", "确定");
    }

    private void ExportUnreferencedList()
    {
        var unreferenced = _results.Where(r => !r.Referenced).ToList();
        if (unreferenced.Count == 0)
        {
            EditorUtility.DisplayDialog("图片引用检查", "没有未引用的图片。", "确定");
            return;
        }
        var dir = Path.Combine("Builds", "Reports");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"unreferenced_images_{System.DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllLines(path, unreferenced.Select(r => r.Path));
        Debug.Log($"[ImageReferenceChecker] 未引用清单已保存：{path}（共 {unreferenced.Count} 条）");
        EditorUtility.DisplayDialog("图片引用检查",
            $"未引用清单已保存：\n{path}\n（共 {unreferenced.Count} 条）", "确定");
    }
}
