using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public static class PrefabContentScanner
{
    [MenuItem("Tools/Scan Prefabs For '奥数字' And Digits")]
    public static void Scan()
    {
        var results = new List<string>();
        var guids = AssetDatabase.FindAssets("t:Prefab");
        if (guids == null || guids.Length == 0)
        {
            Debug.Log("No prefabs found.");
            return;
        }

        const string target = "奥数";
        var digitRegex = new Regex("\\d", RegexOptions.Compiled);

        foreach (var g in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(g);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (go == null) continue;
            var root = go.transform;
            var matches = new List<string>();

            var tmps = go.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps)
            {
                var s = t != null ? t.text : null;
                if (string.IsNullOrEmpty(s)) continue;
                if (s.Contains(target))
                {
                    matches.Add(assetPath + " | " + GetTransformPath(root, t.transform) + " | TMP_Text: " + s);
                }
            }

            if (matches.Count > 0)
            {
                results.AddRange(matches);
            }
        }

        if (results.Count == 0)
        {
            Debug.Log("No matching prefabs.");
            return;
        }

        Debug.Log("Matching TMP_Text nodes (" + results.Count + "):\n" + string.Join("\n", results));
    }

    static string GetTransformPath(Transform root, Transform t)
    {
        if (root == null || t == null) return "";
        var parts = new System.Collections.Generic.List<string>();
        var cur = t;
        while (cur != null)
        {
            parts.Insert(0, cur.name);
            cur = cur.parent;
        }
        return string.Join("/", parts);
    }
}
