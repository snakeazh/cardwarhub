using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public static class SpriteAtlasFolderSyncTool
{
    private const string GameUiAtlasDefine = "GAME_UIATLAS";
    private const string SpritesRoot = "Assets/Textures/Sprites";
    private const string StaticAtlasOutputRoot = "Assets/Textures/Atlas/Static";
    private const string DynamicAtlasOutputRoot = "Assets/Textures/Atlas/Dynamic";
    private const string DynamicSourcePrefix = "Dyn_";
    private const string GlobalSpriteAtlasAutoFilePath = "Assets/Scripts/Global.SpriteAtlas.Auto.cs";

    private enum AtlasKind
    {
        Auto,
        Static,
        Dynamic
    }

    [MenuItem("Assets/Sprite Atlas/Create Or Update Atlas (Selected Sprite Folder)", false, 2001)]
    private static void CreateOrUpdateSelectedFolderAtlas()
    {
        var folders = GetSelectedSpriteFolders();
        if (folders.Count == 0)
        {
            Debug.LogWarning($"未选中 {SpritesRoot} 下的子文件夹，无法创建或更新图集。");
            EditorUtility.DisplayDialog("Sprite Atlas", $"请在 Project 视图选中 {SpritesRoot} 下的子文件夹。", "确定");
            return;
        }

        CreateOrUpdateForFolders(folders, AtlasKind.Auto);
    }

    [MenuItem("Assets/Sprite Atlas/Create Or Update Atlas (Selected Sprite Folder)", true)]
    private static bool ValidateCreateOrUpdateSelectedFolderAtlas()
    {
        return GetSelectedSpriteFolders().Count > 0;
    }

    [MenuItem("Assets/Sprite Atlas/Create Or Update Static Atlas (Selected Sprite Folder)", false, 2002)]
    private static void CreateOrUpdateSelectedFolderStaticAtlas()
    {
        var folders = GetSelectedSpriteFolders();
        if (folders.Count == 0)
        {
            Debug.LogWarning($"未选中 {SpritesRoot} 下的子文件夹，无法创建或更新静态图集。");
            EditorUtility.DisplayDialog("Sprite Atlas", $"请在 Project 视图选中 {SpritesRoot} 下的子文件夹。", "确定");
            return;
        }

        CreateOrUpdateForFolders(folders, AtlasKind.Static);
    }

    [MenuItem("Assets/Sprite Atlas/Create Or Update Static Atlas (Selected Sprite Folder)", true)]
    private static bool ValidateCreateOrUpdateSelectedFolderStaticAtlas()
    {
        return GetSelectedSpriteFolders().Count > 0;
    }

    [MenuItem("Assets/Sprite Atlas/Create Or Update Dynamic Atlas (Selected Sprite Folder)", false, 2003)]
    private static void CreateOrUpdateSelectedFolderDynamicAtlas()
    {
        var folders = GetSelectedSpriteFolders();
        if (folders.Count == 0)
        {
            Debug.LogWarning($"未选中 {SpritesRoot} 下的子文件夹，无法创建或更新动态图集。");
            EditorUtility.DisplayDialog("Sprite Atlas", $"请在 Project 视图选中 {SpritesRoot} 下的子文件夹。", "确定");
            return;
        }

        CreateOrUpdateForFolders(folders, AtlasKind.Dynamic);
    }

    [MenuItem("Assets/Sprite Atlas/Create Or Update Dynamic Atlas (Selected Sprite Folder)", true)]
    private static bool ValidateCreateOrUpdateSelectedFolderDynamicAtlas()
    {
        return GetSelectedSpriteFolders().Count > 0;
    }

    [MenuItem("Tools/Sprite Atlas/Create Or Update Atlases Under Textures/Sprites %#x")]
    private static void CreateOrUpdateAllSpriteFolderAtlases()
    {
        if (!AssetDatabase.IsValidFolder(SpritesRoot))
        {
            Debug.LogError($"目录不存在: {SpritesRoot}");
            EditorUtility.DisplayDialog("Sprite Atlas", $"目录不存在:\n{SpritesRoot}", "确定");
            return;
        }

        var folderAssetsPaths = CollectTargetFoldersUnderRoot();

        if (folderAssetsPaths.Count == 0)
        {
            Debug.LogWarning($"{SpritesRoot} 下没有子文件夹。");
            EditorUtility.DisplayDialog("Sprite Atlas", $"{SpritesRoot} 下没有子文件夹。", "确定");
            return;
        }

        CreateOrUpdateForFolders(folderAssetsPaths, AtlasKind.Auto);
    }

    [MenuItem("Tools/Sprite Atlas/Toggle GAME_UIATLAS Define", false, 100)]
    private static void ToggleGameUiAtlasDefine()
    {
        var currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        if (currentGroup == BuildTargetGroup.Unknown)
        {
            Debug.LogWarning("当前 BuildTargetGroup 为 Unknown，无法切换 GAME_UIATLAS 宏定义。");
            return;
        }

        var defineSymbols = GetDefineSymbols(currentGroup);
        var symbolSet = new HashSet<string>();
        if (!string.IsNullOrEmpty(defineSymbols))
        {
            var symbols = defineSymbols.Split(';');
            for (var i = 0; i < symbols.Length; i++)
            {
                var symbol = symbols[i].Trim();
                if (!string.IsNullOrEmpty(symbol))
                {
                    symbolSet.Add(symbol);
                }
            }
        }

        var removed = symbolSet.Remove(GameUiAtlasDefine);
        if (!removed)
        {
            symbolSet.Add(GameUiAtlasDefine);
        }

        var builder = new StringBuilder();
        foreach (var symbol in symbolSet)
        {
            if (builder.Length > 0)
            {
                builder.Append(';');
            }

            builder.Append(symbol);
        }

        SetDefineSymbols(currentGroup, builder.ToString());
        Debug.Log(removed
            ? $"已移除宏定义: {GameUiAtlasDefine} ({currentGroup})"
            : $"已添加宏定义: {GameUiAtlasDefine} ({currentGroup})");
    }

    private static void CreateOrUpdateForFolders(List<string> folderPaths, AtlasKind forcedKind)
    {
        EnsureFolderExists(StaticAtlasOutputRoot);
        EnsureFolderExists(DynamicAtlasOutputRoot);

        var created = 0;
        var updated = 0;
        var changedAtlases = new List<SpriteAtlas>();
        var changedAtlasNames = new HashSet<string>();

        foreach (var folderPath in folderPaths)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                continue;
            }

            var folderName = Path.GetFileName(folderPath);
            var atlasKind = ResolveAtlasKind(folderPath, forcedKind);
            var atlasName = GetAtlasFileName(folderName, atlasKind);
            var atlasOutputRoot = GetAtlasOutputRoot(atlasKind);
            var atlasPath = $"{atlasOutputRoot}/{atlasName}.spriteatlas";
            var folderObj = AssetDatabase.LoadAssetAtPath<Object>(folderPath);
            if (folderObj == null)
            {
                continue;
            }

            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            if (atlas == null)
            {
                atlas = new SpriteAtlas();
                AssetDatabase.CreateAsset(atlas, atlasPath);
                created++;
            }
            else
            {
                updated++;
            }

            ResetPackables(atlas);
            SpriteAtlasExtensions.Add(atlas, new[] { folderObj });
            ApplyAtlasKindSettings(atlas, atlasKind);
            EditorUtility.SetDirty(atlas);
            changedAtlases.Add(atlas);
            changedAtlasNames.Add(atlasName);
            Debug.Log($"已同步{GetAtlasKindName(atlasKind)}图集: {atlasPath} -> {folderPath}");
        }

        if (changedAtlases.Count > 0)
        {
            PackAtlases(changedAtlases);
            SyncGlobalSpriteAtlasDeclarations(changedAtlasNames);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var message = $"完成，创建 {created} 个，更新 {updated} 个。";
        Debug.Log(message);
        EditorUtility.DisplayDialog("Sprite Atlas", message, "确定");
    }

    private static void PackAtlases(List<SpriteAtlas> atlases)
    {
        try
        {
            SpriteAtlasUtility.PackAtlases(atlases.ToArray(), EditorUserBuildSettings.activeBuildTarget, false);
            Debug.Log($"图集 Pack 完成，数量: {atlases.Count}，目标平台: {EditorUserBuildSettings.activeBuildTarget}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"图集 Pack 失败: {ex}");
        }
    }

    private static void SyncGlobalSpriteAtlasDeclarations(HashSet<string> atlasNames)
    {
        if (atlasNames == null || atlasNames.Count == 0)
        {
            return;
        }

        EnsureGlobalSpriteAtlasAutoFileExists();
        var fullPath = ToFullPath(GlobalSpriteAtlasAutoFilePath);

        var lines = new List<string>(File.ReadAllLines(fullPath));
        if (lines.Count == 0)
        {
            return;
        }

        var insertIndex = FindClassClosingBraceLine(lines);
        if (insertIndex < 0)
        {
            Debug.LogWarning($"无法定位类结束位置，跳过自动写入: {GlobalSpriteAtlasAutoFilePath}");
            return;
        }

        var sortedNames = new List<string>(atlasNames);
        sortedNames.Sort(System.StringComparer.Ordinal);

        var toInsert = new List<string>();
        foreach (var atlasName in sortedNames)
        {
            if (string.IsNullOrWhiteSpace(atlasName))
            {
                continue;
            }

            var attrToken = $"[SpriteAtlasTarget(\"{atlasName}\")]";
            if (ContainsText(lines, attrToken))
            {
                continue;
            }

            var fieldName = BuildAtlasFieldName(atlasName);
            var fieldLine = $"        public static SpriteAtlas {fieldName};";
            if (ContainsText(lines, fieldLine))
            {
                continue;
            }

            toInsert.Add($"        [SpriteAtlasTarget(\"{atlasName}\")]");
            toInsert.Add(fieldLine);
            toInsert.Add(string.Empty);
        }

        if (toInsert.Count == 0)
        {
            return;
        }

        lines.Insert(insertIndex, string.Empty);
        lines.InsertRange(insertIndex + 1, toInsert);
        File.WriteAllLines(fullPath, lines);
        AssetDatabase.ImportAsset(GlobalSpriteAtlasAutoFilePath);
        Debug.Log($"已自动更新图集字段声明: {GlobalSpriteAtlasAutoFilePath}");
    }

    private static List<string> GetSelectedSpriteFolders()
    {
        var folders = new List<string>();
        var folderSet = new HashSet<string>();
        var assetsRoot = $"{SpritesRoot}/";
        var selectedGuids = Selection.assetGUIDs;
        foreach (var guid in selectedGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetDatabase.IsValidFolder(path))
            {
                continue;
            }

            if (!path.StartsWith(assetsRoot))
            {
                continue;
            }

            if (path == SpritesRoot)
            {
                CollectSpriteFoldersRecursive(path, folders, folderSet, includeRoot: false);
                continue;
            }

            CollectSpriteFoldersRecursive(path, folders, folderSet, includeRoot: true);
        }

        return folders;
    }

    private static void ResetPackables(SpriteAtlas atlas)
    {
        var oldPackables = SpriteAtlasExtensions.GetPackables(atlas);
        if (oldPackables != null && oldPackables.Length > 0)
        {
            SpriteAtlasExtensions.Remove(atlas, oldPackables);
        }
    }

    private static List<string> CollectTargetFoldersUnderRoot()
    {
        var result = new List<string>();
        var folderSet = new HashSet<string>();
        CollectSpriteFoldersRecursive(SpritesRoot, result, folderSet, includeRoot: false);
        return result;
    }

    private static AtlasKind ResolveAtlasKind(string folderPath, AtlasKind forcedKind)
    {
        if (forcedKind != AtlasKind.Auto)
        {
            return forcedKind;
        }

        var folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(folderName) && folderName.StartsWith("Dyn_"))
        {
            return AtlasKind.Dynamic;
        }

        return AtlasKind.Static;
    }

    private static void CollectSpriteFoldersRecursive(string folderPath, List<string> result, HashSet<string> resultSet, bool includeRoot)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        if (includeRoot && HasSpriteSourceInFolder(folderPath) && resultSet.Add(folderPath))
        {
            result.Add(folderPath);
        }

        var subFolders = AssetDatabase.GetSubFolders(folderPath);
        foreach (var subFolder in subFolders)
        {
            CollectSpriteFoldersRecursive(subFolder, result, resultSet, includeRoot: true);
        }
    }

    private static bool HasSpriteSourceInFolder(string folderPath)
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                continue;
            }

            var parentPath = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            if (string.Equals(parentPath, folderPath, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetAtlasKindName(AtlasKind kind)
    {
        return kind == AtlasKind.Dynamic ? "动态" : "静态";
    }

    private static void ApplyAtlasKindSettings(SpriteAtlas atlas, AtlasKind kind)
    {
        atlas.SetIncludeInBuild(true);
        ApplyCommonAtlasSettings(atlas);
    }

    private static void ApplyCommonAtlasSettings(SpriteAtlas atlas)
    {
        var packing = atlas.GetPackingSettings();
        packing.enableRotation = false;
        packing.enableAlphaDilation = true;
        packing.padding = 8;
        atlas.SetPackingSettings(packing);

        var texture = atlas.GetTextureSettings();
        texture.readable = false;
        texture.generateMipMaps = false;
        texture.sRGB = true;
        texture.filterMode = FilterMode.Bilinear;
        atlas.SetTextureSettings(texture);

        var android = atlas.GetPlatformSettings("Android");
        android.overridden = true;
        android.maxTextureSize = 2048;
        android.format = TextureImporterFormat.ASTC_6x6;
        android.textureCompression = TextureImporterCompression.Compressed;
        android.compressionQuality = 50;
        atlas.SetPlatformSettings(android);
    }

    private static string GetAtlasFileName(string folderName, AtlasKind kind)
    {
        var safeFolderName = string.IsNullOrEmpty(folderName) ? "Unnamed" : folderName;
        if (kind == AtlasKind.Dynamic && safeFolderName.StartsWith(DynamicSourcePrefix))
        {
            safeFolderName = safeFolderName.Substring(DynamicSourcePrefix.Length);
        }

        return safeFolderName;
    }

    private static string GetAtlasOutputRoot(AtlasKind kind)
    {
        return kind == AtlasKind.Dynamic ? DynamicAtlasOutputRoot : StaticAtlasOutputRoot;
    }

    private static int FindClassClosingBraceLine(List<string> lines)
    {
        var closingIndices = new List<int>();
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim() == "}")
            {
                closingIndices.Add(i);
            }
        }

        if (closingIndices.Count < 2)
        {
            return -1;
        }

        return closingIndices[closingIndices.Count - 2];
    }

    private static bool ContainsText(List<string> lines, string token)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains(token))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildAtlasFieldName(string atlasName)
    {
        var sb = new StringBuilder(atlasName.Length + 8);
        var makeUpper = true;

        foreach (var c in atlasName)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(makeUpper ? char.ToUpperInvariant(c) : c);
                makeUpper = false;
            }
            else
            {
                makeUpper = true;
            }
        }

        var normalized = sb.Length == 0 ? "Unnamed" : sb.ToString();
        if (char.IsDigit(normalized[0]))
        {
            normalized = "_" + normalized;
        }

        return normalized + "Atlas";
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        var parts = folderPath.Split('/');
        if (parts.Length == 0)
        {
            return;
        }

        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private static void EnsureGlobalSpriteAtlasAutoFileExists()
    {
        var fullPath = ToFullPath(GlobalSpriteAtlasAutoFilePath);
        if (File.Exists(fullPath))
        {
            return;
        }

        var dirPath = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        var content = new[]
        {
            "using UnityEngine;",
            "using UnityEngine.U2D;",
            "",
            "namespace FusionContinent",
            "{",
            "    // Auto-generated sprite atlas references. Do not edit manually.",
            "    public partial class Global : MonoBehaviour",
            "    {",
            "    }",
            "}"
        };
        File.WriteAllLines(fullPath, content);
        AssetDatabase.ImportAsset(GlobalSpriteAtlasAutoFilePath);
    }

    private static string GetDefineSymbols(BuildTargetGroup targetGroup)
    {
#if UNITY_2021_2_OR_NEWER
        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        return PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
        return PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
#endif
    }

    private static void SetDefineSymbols(BuildTargetGroup targetGroup, string symbols)
    {
#if UNITY_2021_2_OR_NEWER
        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);
        PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, symbols);
#else
        PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symbols);
#endif
    }

    private static string ToAssetPath(string absolutePath)
    {
        var normalized = absolutePath.Replace("\\", "/");
        var dataPath = Application.dataPath.Replace("\\", "/");
        if (normalized.StartsWith(dataPath))
        {
            return "Assets" + normalized.Substring(dataPath.Length);
        }

        return normalized;
    }

    private static string ToFullPath(string assetPath)
    {
        var normalized = assetPath.Replace("\\", "/");
        var dataPath = Application.dataPath.Replace("\\", "/");
        if (!normalized.StartsWith("Assets"))
        {
            return normalized;
        }

        return dataPath + normalized.Substring("Assets".Length);
    }
}
