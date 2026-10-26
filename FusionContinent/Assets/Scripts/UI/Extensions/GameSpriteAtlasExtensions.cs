using System;
using System.Collections.Generic;
using System.IO;
using FusionContinent;
using FusionContinent.Helper;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;


// 为 SpriteAtlas 的 GetSprite 提供扩展重载，支持：
// - 替补图集（fallback atlas）
// - 占位符精灵（fallback sprite）
// - 名称清洗（去扩展名、替换空格等）
// - 全局默认占位符与名称重映射
public static class GameSpriteAtlasExtensions
{
    private static readonly Dictionary<SpriteAtlas, List<SpriteAtlas>> sAtlasFallbacks = new();
    private static readonly List<SpriteAtlas> sGlobalFallbackAtlases = new();
    private static readonly Dictionary<string, string> sNameRemap = new(StringComparer.OrdinalIgnoreCase);
    private static Sprite sGlobalDefaultSprite;

    // 自动索引：为每个图集建立名称映射（规范名 -> 实际名），用于自动重映射
    private static readonly Dictionary<SpriteAtlas, Dictionary<string, string>> sAtlasNameIndex = new();
    private static readonly Dictionary<SpriteAtlas, Func<string, string>> sAtlasKeyNormalizers = new();
    private static Func<string, string> sGlobalKeyNormalizer = DefaultNormalizeKey;
    private static bool sAutoIndexOnAdd = true;

    // 注册：为某个图集设置替补图集
    public static void SetFallbackAtlas(SpriteAtlas atlas, SpriteAtlas fallbackAtlas)
    {
        // 兼容旧 API：等价于 AddFallbackAtlas
        AddFallbackAtlas(atlas, fallbackAtlas);
    }

    // 新增：为某个图集追加一个替补图集（可重复调用形成链）
    public static void AddFallbackAtlas(SpriteAtlas atlas, SpriteAtlas fallbackAtlas)
    {
        if (!atlas || !fallbackAtlas) return;
        if (!sAtlasFallbacks.TryGetValue(atlas, out var list))
        {
            list = new List<SpriteAtlas>();
            sAtlasFallbacks[atlas] = list;
        }
        if (!list.Contains(fallbackAtlas)) list.Add(fallbackAtlas);

        if (sAutoIndexOnAdd) TryIndexAtlas(fallbackAtlas);
    }

    // 新增：一次性设置多个替补图集（覆盖旧链）
    public static void SetFallbackAtlases(SpriteAtlas atlas, params SpriteAtlas[] fallbacks)
    {
        if (!atlas) return;
        if (fallbacks == null) fallbacks = Array.Empty<SpriteAtlas>();
        var list = new List<SpriteAtlas>();
        foreach (var fb in fallbacks)
        {
            if (!fb) continue;
            if (!list.Contains(fb)) list.Add(fb);
            if (sAutoIndexOnAdd) TryIndexAtlas(fb);
        }
        sAtlasFallbacks[atlas] = list;
    }

    // 新增：清除某图集的替补链
    public static void ClearFallbackAtlases(SpriteAtlas atlas)
    {
        if (!atlas) return;
        sAtlasFallbacks.Remove(atlas);
    }

    // 新增：全局替补链（所有图集都会尝试）
    public static void AddGlobalFallbackAtlas(SpriteAtlas atlas)
    {
        if (!atlas) return;
        if (!sGlobalFallbackAtlases.Contains(atlas)) sGlobalFallbackAtlases.Add(atlas);
        if (sAutoIndexOnAdd) TryIndexAtlas(atlas);
    }

    public static void SetGlobalFallbackAtlases(params SpriteAtlas[] atlases)
    {
        sGlobalFallbackAtlases.Clear();
        if (atlases == null) return;
        foreach (var a in atlases)
        {
            if (!a) continue;
            if (!sGlobalFallbackAtlases.Contains(a)) sGlobalFallbackAtlases.Add(a);
            if (sAutoIndexOnAdd) TryIndexAtlas(a);
        }
    }

    public static void ClearGlobalFallbackAtlases()
    {
        sGlobalFallbackAtlases.Clear();
    }

    // 注册：设置全局默认占位符精灵
    public static void SetGlobalDefaultSprite(Sprite defaultSprite)
    {
        sGlobalDefaultSprite = defaultSprite;
    }

    // 设置全局/指定图集的名称规范化函数
    public static void SetGlobalKeyNormalizer(Func<string, string> normalizer)
    {
        sGlobalKeyNormalizer = normalizer ?? DefaultNormalizeKey;
    }

    public static void SetAtlasKeyNormalizer(SpriteAtlas atlas, Func<string, string> normalizer)
    {
        if (!atlas) return;
        if (normalizer == null)
        {
            if (sAtlasKeyNormalizers.ContainsKey(atlas)) sAtlasKeyNormalizers.Remove(atlas);
            return;
        }
        sAtlasKeyNormalizers[atlas] = normalizer;
    }

    // 控制是否在添加替补图集时自动建立索引
    public static void SetAutoIndexOnAdd(bool enabled)
    {
        sAutoIndexOnAdd = enabled;
    }

    // 注册：名称重映射（例如请求名 -> 实际名）
    public static void SetNameRemap(string requestedName, string actualName)
    {
        if (string.IsNullOrEmpty(requestedName) || string.IsNullOrEmpty(actualName)) return;
        sNameRemap[requestedName] = actualName;
    }

    // 重载一：指定占位符精灵
    public static Sprite GetSpriteExt(this SpriteAtlas atlas, string name)
    {
        return GetSpriteInternal(atlas, name, null, null);
    }

    // 重载二：指定替补图集
    public static Sprite GetSpriteFbAtlas(this SpriteAtlas atlas, string name, SpriteAtlas fallbackAtlas)
    {
        return GetSpriteInternal(atlas, name, new[] { fallbackAtlas }, null);
    }

    // 重载：传入多个替补图集（按顺序尝试）
    public static Sprite GetSprite(this SpriteAtlas atlas, string name, params SpriteAtlas[] fallbackAtlases)
    {
        return GetSpriteInternal(atlas, name, fallbackAtlases, null);
    }

    // 重载三：同时指定替补图集与占位符精灵
    public static Sprite GetSprite(this SpriteAtlas atlas, string name, SpriteAtlas fallbackAtlas, Sprite fallback)
    {
        return GetSpriteInternal(atlas, name, new[] { fallbackAtlas }, fallback);
    }

    // 常用：获取或使用全局占位符
    public static Sprite GetSpriteOrFallback(this SpriteAtlas atlas, string name)
    {
        return GetSpriteInternal(atlas, name, null, sGlobalDefaultSprite);
    }

    private static Sprite GetSpriteInternal(SpriteAtlas atlas, string name, IEnumerable<SpriteAtlas> explicitFallbackAtlases, Sprite explicitFallbackSprite)
    {
        if (!atlas) return explicitFallbackSprite ? explicitFallbackSprite : sGlobalDefaultSprite;

        // 1) 尝试原名
        var sprite = atlas.GetSprite(name);
        if (sprite) return sprite;

        // 2) 名称重映射
        if (sNameRemap.TryGetValue(name ?? string.Empty, out var mapped))
        {
            sprite = atlas.GetSprite(mapped);
            if (sprite) return sprite;
        }

        // 3) 重映射：手动映射/索引映射（规范名 -> 实际名）
        var key = NormalizeKey(name, atlas);
        if (TryGetIndexedName(atlas, key, out var indexedName))
        {
            sprite = atlas.GetSprite(indexedName);
            if (sprite) return sprite;
        }

        // 4) 替补图集链（优先显式传入，其次注册的链，最后全局链）
        var chain = new List<SpriteAtlas>();
        if (explicitFallbackAtlases != null)
        {
            foreach (var a in explicitFallbackAtlases)
            {
                if (!a) continue;
                if (!chain.Contains(a) && a != atlas) chain.Add(a);
            }
        }
        if (sAtlasFallbacks.TryGetValue(atlas, out var regList) && regList != null)
        {
            foreach (var a in regList)
            {
                if (!a) continue;
                sprite = a.GetSprite(name);
                if (sprite) return sprite;
            }
        }
        
        return explicitFallbackSprite ? explicitFallbackSprite : sGlobalDefaultSprite;
    }

    // 已移除名称变体尝试，统一用手动重映射与索引重映射

    // 规范化键：避免大小写/扩展名/空格/连字符等差异
    private static string DefaultNormalizeKey(string name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        var n = name.Trim();
        var noExt = Path.GetFileNameWithoutExtension(n);
        var normalized = noExt.Replace(" ", "_")
                               .Replace('-', '_')
                               .ToLowerInvariant();
        return normalized;
    }

    private static string NormalizeKey(string name, SpriteAtlas atlas)
    {
        var normalizer = atlas != null && sAtlasKeyNormalizers.TryGetValue(atlas, out var fn) ? fn : sGlobalKeyNormalizer;
        return normalizer?.Invoke(name) ?? DefaultNormalizeKey(name);
    }

    private static bool TryGetIndexedName(SpriteAtlas atlas, string key, out string indexedName)
    {
        indexedName = null;
        if (!atlas || string.IsNullOrEmpty(key)) return false;
        if (sAtlasNameIndex.TryGetValue(atlas, out var dict) && dict != null)
        {
            return dict.TryGetValue(key, out indexedName);
        }
        return false;
    }

    // 建立索引：读取图集中所有精灵名，生成规范键到实际名的映射
    private static void TryIndexAtlas(SpriteAtlas atlas)
    {
        if (!atlas) return;
        if (!sAtlasNameIndex.TryGetValue(atlas, out var dict) || dict == null)
        {
            dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            sAtlasNameIndex[atlas] = dict;
        }
        try
        {
            Sprite[] list = new Sprite[atlas.spriteCount];
            
            
            // Sprite[] list;
            // // Unity 2020+ 提供 List 重载
            // atlas.GetSprites(list);
            foreach (var sp in list)
            {
                var key = NormalizeKey(sp.name, atlas);
                if (!dict.ContainsKey(key)) dict[key] = sp.name;
            }
        }
        catch { /* 忽略：仅运行时索引，不做编辑器兼容回退 */ }
    }
}
