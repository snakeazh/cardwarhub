using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor.U2D;
using System.Collections.Generic;
using System.Linq;

public static class SpriteAtlasConflictScanner
{
    [MenuItem("Assets/Scan Sprite Atlas Conflicts", false)]
    private static void ScanConflicts()
    {
        var atlasGuids = AssetDatabase.FindAssets("t:SpriteAtlas");
        if (atlasGuids == null || atlasGuids.Length == 0)
        {
            Debug.Log("No SpriteAtlas assets found.");
            return;
        }

        var atlases = new List<SpriteAtlas>();
        foreach (var g in atlasGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas != null) atlases.Add(atlas);
        }

        var textureToAtlases = new Dictionary<string, HashSet<string>>();

        foreach (var atlas in atlases)
        {
            var atlasKey = GetAtlasGroupKey(atlas);
            var packables = SpriteAtlasExtensions.GetPackables(atlas);
            foreach (var obj in packables)
            {
                var objPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(objPath)) continue;

                if (AssetDatabase.IsValidFolder(objPath))
                {
                    var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { objPath });
                    foreach (var tg in guids)
                    {
                        var tPath = AssetDatabase.GUIDToAssetPath(tg);
                        AddMap(textureToAtlases, tPath, atlasKey);
                    }
                }
                else if (obj is Texture2D)
                {
                    AddMap(textureToAtlases, objPath, atlasKey);
                }
                else if (obj is Sprite sp)
                {
                    var t = sp.texture;
                    var tPath = t != null ? AssetDatabase.GetAssetPath(t) : null;
                    if (!string.IsNullOrEmpty(tPath)) AddMap(textureToAtlases, tPath, atlasKey);
                }
                else
                {
                    var main = AssetDatabase.LoadMainAssetAtPath(objPath) as Texture2D;
                    if (main != null) AddMap(textureToAtlases, objPath, atlasKey);
                }
            }
        }

        var conflicts = textureToAtlases
            .Where(kv => kv.Value.Count > 1)
            .OrderByDescending(kv => kv.Value.Count)
            .ToList();

        if (conflicts.Count == 0)
        {
            Debug.Log("No sprite atlas conflicts detected.");
            return;
        }

        var msg = $"Conflicts: {conflicts.Count}\n";
        int show = Mathf.Min(100, conflicts.Count);
        for (int i = 0; i < show; i++)
        {
            var kv = conflicts[i];
            msg += kv.Key + " -> " + string.Join(", ", kv.Value) + "\n";
        }
        Debug.Log(msg);
    }

    private static string GetAtlasGroupKey(SpriteAtlas atlas)
    {
        return atlas != null ? atlas.name : "";
    }

    private static void AddMap(Dictionary<string, HashSet<string>> map, string texturePath, string atlasKey)
    {
        if (string.IsNullOrEmpty(texturePath) || string.IsNullOrEmpty(atlasKey)) return;
        if (!map.TryGetValue(texturePath, out var set))
        {
            set = new HashSet<string>();
            map[texturePath] = set;
        }
        set.Add(atlasKey);
    }
    
    
    [MenuItem("Assets/Scan Selected Sprite Atlases Conflicts", false)]
    private static void ScanSelectedConflicts()
    {
        var selected = Selection.objects;
        if (selected == null || selected.Length == 0)
        {
            Debug.Log("No selection.");
            return;
        }

        var atlasList = new List<SpriteAtlas>();
        foreach (var obj in selected)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path)) continue;
            if (AssetDatabase.IsValidFolder(path))
            {
                var guids = AssetDatabase.FindAssets("t:SpriteAtlas", new[] { path });
                foreach (var g in guids)
                {
                    var ap = AssetDatabase.GUIDToAssetPath(g);
                    var a = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(ap);
                    if (a != null) atlasList.Add(a);
                }
            }
            else
            {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
                if (atlas != null) atlasList.Add(atlas);
            }
        }

        if (atlasList.Count == 0)
        {
            Debug.Log("No SpriteAtlas found in selection.");
            return;
        }

        var textureToAtlases = new Dictionary<string, HashSet<string>>();
        foreach (var atlas in atlasList)
        {
            var atlasKey = GetAtlasGroupKey(atlas);
            var packables = SpriteAtlasExtensions.GetPackables(atlas);
            foreach (var obj in packables)
            {
                var objPath = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(objPath)) continue;
                if (AssetDatabase.IsValidFolder(objPath))
                {
                    var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { objPath });
                    foreach (var tg in guids)
                    {
                        var tPath = AssetDatabase.GUIDToAssetPath(tg);
                        AddMap(textureToAtlases, tPath, atlasKey);
                    }
                }
                else if (obj is Texture2D)
                {
                    AddMap(textureToAtlases, objPath, atlasKey);
                }
                else if (obj is Sprite sp)
                {
                    var t = sp.texture;
                    var tPath = t != null ? AssetDatabase.GetAssetPath(t) : null;
                    if (!string.IsNullOrEmpty(tPath)) AddMap(textureToAtlases, tPath, atlasKey);
                }
                else
                {
                    var main = AssetDatabase.LoadMainAssetAtPath(objPath) as Texture2D;
                    if (main != null) AddMap(textureToAtlases, objPath, atlasKey);
                }
            }
        }

        var conflicts = textureToAtlases
            .Where(kv => kv.Value.Count > 1)
            .OrderByDescending(kv => kv.Value.Count)
            .ToList();

        if (conflicts.Count == 0)
        {
            Debug.Log("No conflicts in selected atlases.");
            return;
        }

        var msg = $"Selected Conflicts: {conflicts.Count}\n";
        int show = Mathf.Min(100, conflicts.Count);
        for (int i = 0; i < show; i++)
        {
            var kv = conflicts[i];
            msg += kv.Key + " -> " + string.Join(", ", kv.Value) + "\n";
        }
        Debug.Log(msg);
    }
}
