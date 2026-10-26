using System.Collections.Generic;
using FusionContinent.Monster.Animation;
using FusionContinent.SpineBake;

namespace FusionContinent.SpineBake
{
    /// <summary>
    /// 兼容旧调用，转发到 <see cref="BakeMeshFrameLoader"/>。
    /// </summary>
    public static class SpineMeshFrameLoader
    {
        public const string YooAssetGroupPrefix = BakeMeshFrameLoader.YooAssetGroupPrefix;

        public static string GetFrameAssetKey(string spineAssetKey)
        {
            return BakeMeshFrameLoader.GetFrameAssetKey(spineAssetKey);
        }

        public static void Preload(IEnumerable<string> spineAssetKeys)
        {
            BakeMeshFrameLoader.Preload(spineAssetKeys);
        }

        public static SpineMeshFrameData Load(string spineAssetKey)
        {
            return BakeMeshFrameLoader.Load(spineAssetKey);
        }

        public static void ClearCache()
        {
            BakeMeshFrameLoader.ClearCache();
        }
    }
}
