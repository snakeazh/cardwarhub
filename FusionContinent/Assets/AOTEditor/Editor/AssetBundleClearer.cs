#if UNITY_EDITOR


using UnityEditor;
using UnityEngine;

public static class AssetBundleClearer
{
    [MenuItem("Tools/AssetBundle/Clear All AssetBundle Names")]
    public static void ClearAllAssetBundleNames()
    {
        var names = AssetDatabase.GetAllAssetBundleNames();
        if (names.Length == 0)
        {
            Debug.Log("No AssetBundle names found.");
            return;
        }

        int count = 0;
        foreach (var name in names)
        {
            var paths = AssetDatabase.GetAssetPathsFromAssetBundle(name);
            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path);
                if (importer != null && !string.IsNullOrEmpty(importer.assetBundleName))
                {
                    importer.assetBundleName = null;
                    count++;
                }
            }
        }

        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
        Debug.Log($"Cleared AssetBundle names from {count} assets.");
    }
}
#endif