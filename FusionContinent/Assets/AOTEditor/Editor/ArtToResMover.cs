using UnityEditor;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR

public static class ArtToResMover
{
    [MenuItem("Tools/Move Art assets with AssetBundleName to Res")] 
    public static void Move()
    {
        var artRoot = "Assets/Sprites";
        var resRoot = "Assets/Res/UITextures";
        Move(artRoot, resRoot);
    }
    [MenuItem("Tools/Move Art Effect assets with AssetBundleName to Res")] 
    public static void Move2()
    {
        var artRoot = "Assets/Art/Effect";
        var resRoot = "Assets/Res/Effect";
        Move(artRoot, resRoot);
    }
    public static void Move(string artRoot, string resRoot)
    {
        if (!AssetDatabase.IsValidFolder(artRoot))
        {
            Debug.Log("Art folder not found: " + artRoot);
            return;
        }
        if (!AssetDatabase.IsValidFolder(resRoot))
        {
            AssetDatabase.CreateFolder("Assets", "Res");
        }

        var guids = AssetDatabase.FindAssets(string.Empty, new[] { artRoot });
        if (guids == null || guids.Length == 0)
        {
            Debug.Log("No assets under Art.");
            return;
        }

        int moved = 0;
        int skipped = 0;
        foreach (var g in guids)
        {
            var srcPath = AssetDatabase.GUIDToAssetPath(g);
            var importer = AssetImporter.GetAtPath(srcPath);
            if (importer == null) { skipped++; continue; }
            var abName = importer.assetBundleName;
            if (string.IsNullOrEmpty(abName)) { skipped++; continue; }

            string relative;
            if (srcPath.StartsWith(artRoot + "/"))
            {
                relative = srcPath.Substring(artRoot.Length + 1);
            }
            else
            {
                relative = Path.GetFileName(srcPath);
            }
            var dstPath = resRoot + "/" + relative;
            var dstDirRel = Path.GetDirectoryName(dstPath).Replace('\\','/');
            var absDir = Path.Combine(Application.dataPath, dstDirRel.Substring("Assets/".Length));
            if (!Directory.Exists(absDir)) Directory.CreateDirectory(absDir);

            var err = AssetDatabase.MoveAsset(srcPath, dstPath);
            if (string.IsNullOrEmpty(err))
            {
                moved++;
            }
            else
            {
                Debug.LogWarning("Skip move: " + srcPath + " -> " + dstPath + " | " + err);
                skipped++;
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("Moved: " + moved + ", Skipped: " + skipped);
    }
}

#endif
