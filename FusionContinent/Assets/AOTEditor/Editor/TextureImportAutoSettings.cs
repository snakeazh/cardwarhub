using UnityEditor;
using UnityEngine;
using System.Linq;

public class TextureImportAutoSettings : AssetPostprocessor
{
    static void ConfigureImporter(TextureImporter importer, string assetPath)
    {
        if (importer == null) return;
        var p = assetPath.ToLowerInvariant();
        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        // importer.
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.isReadable = false;
        importer.sRGBTexture = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Repeat;
        var androidDefault = new TextureImporterPlatformSettings { name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_4x4, compressionQuality = 100 };
        importer.SetPlatformTextureSettings(androidDefault);
        // var iosDefault = new TextureImporterPlatformSettings { name = "iPhone", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.AutomaticCompressed, compressionQuality = 100 };
        // importer.SetPlatformTextureSettings(iosDefault);
    }

    // void OnPreprocessTexture()
    // {
    //     var importer = assetImporter as TextureImporter;
    //     ConfigureImporter(importer, assetPath);
    // }

    [MenuItem("Assets/Apply Texture Auto Settings", false)]
    static void ApplyTextureSettingsToSelectedFolders()
    {
        var guids = Selection.assetGUIDs;
        if (guids == null || guids.Length == 0) return;
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var folders = paths.Where(p => AssetDatabase.IsValidFolder(p)).ToList();
        var textures = paths.Where(p => p.EndsWith(".png") || p.EndsWith(".jpg") || p.EndsWith(".jpeg") || p.EndsWith(".tga") || p.EndsWith(".psd") || p.EndsWith(".bmp")).ToList();
        foreach (var f in folders)
        {
            var found = AssetDatabase.FindAssets("t:Texture", new[] { f });
            foreach (var guid in found)
            {
                var ap = AssetDatabase.GUIDToAssetPath(guid);
                if (!textures.Contains(ap)) textures.Add(ap);
            }
        }
        if (textures.Count == 0) return;
        try
        {
            AssetDatabase.StartAssetEditing();
            int total = textures.Count;
            for (int i = 0; i < total; i++)
            {
                var ap = textures[i];
                EditorUtility.DisplayProgressBar("Apply Texture Settings", ap, (float)(i + 1) / total);
                var importer = AssetImporter.GetAtPath(ap) as TextureImporter;
                if (importer == null) continue;
                ConfigureImporter(importer, ap);
                importer.SaveAndReimport();
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            EditorUtility.ClearProgressBar();
        }
    }

    [MenuItem("Assets/Apply Spine Texture Auto Settings", true)]
    static bool ValidateApplyTextureSettingsToSelectedFolders()
    {
        var guids = Selection.assetGUIDs;
        return guids != null && guids.Length > 0;
    }
}
