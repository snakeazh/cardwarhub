using System;
using System.Text;
using FusionContinent.Base;
using FusionContinent.Utils;
using QFramework;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace FusionContinent.Helper
{

    public enum EPaintType
    {
        
        Small,
        Medium,
        Large,

    }
    public static class SpriteHelper
    {
        private static StringBuilder _builder;

        /// <summary>
        /// 异步加载立绘/贴图：交给 AsyncTextureManager 统一排队异步加载，完成后自动设置到 img。
        /// 地址无效（未打进包）返回 false，调用方可继续尝试下一张候选图；已缓存的图会同步设置。
        /// </summary>
        public static bool TryLoadPaintAsync(string assetName,Image img,EPaintType ePaintType = EPaintType.Medium,bool isNative = false,string groupName = "UITextures",Action<bool> onCompleted = null)
        {
            if (_builder == null)
            {
                _builder = new StringBuilder();
            }

            _builder.Clear();
            _builder.Append(assetName);
            if (ePaintType == EPaintType.Large)
            {
                _builder.Append("Big");
            }
            else if (ePaintType == EPaintType.Small)
            {
                _builder.Append("Little");
            }

            return TryLoadTextureAsync(_builder.ToString(),img,isNative,groupName,onCompleted);
        }

        /// <summary>
        /// 异步加载散图：同一 Image 请求新图时旧请求自动失效，列表复用不会串图。
        /// </summary>
        public static bool TryLoadTextureAsync(string assetName, Image img, bool isNative = false,string groupName = "UITextures",Action<bool> onCompleted = null)
        {
            if (assetName == "" || img == null) return false;

            return AsyncTextureManager.Instance.SetSprite(img, groupName+"_"+assetName, isNative, onCompleted);
        }

        public static bool TryLoadSpriteByAtlas(SpriteAtlas atlas,string assetName, Image img, bool isNative = false)
        {
            var tex2d = atlas.GetSprite(assetName);
            if (tex2d == null)
            {
                return false;
            }

            if (img.sprite == null)
            {
                return false;
            }
            img.sprite = tex2d;
            if (isNative)
            {
                img.SetNativeSize();
            }

            return true;
        }
    }
}