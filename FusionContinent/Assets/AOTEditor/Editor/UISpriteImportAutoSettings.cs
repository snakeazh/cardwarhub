using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
namespace Editor
{
    public class UISpriteImportAutoSettings: AssetPostprocessor
    {
        private void OnPostprocessTexture(Texture2D texture)
        {
            ConfigureImporter(this.assetImporter as TextureImporter, this.assetPath);
        }

        static void ConfigureImporter(TextureImporter importer, string assetPath)
        {
            // var isSprite = assetPath.Contains("Assets/Sprites/");
            // if (!isSprite)
            // {
            //     return;
            // }
            if (importer == null) return;
            var p = assetPath.ToLowerInvariant();
            var setting = importer.GetPlatformTextureSettings("Android");
            if(setting.overridden)return;
            setting.overridden = true;
            importer.SetPlatformTextureSettings(setting);
        }
    }
}
#endif
