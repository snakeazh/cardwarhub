using System.Collections.Generic;
using System.IO;
using FusionContinent;
using FusionContinent.SpineBake;
using UnityEngine;

namespace FusionContinent.Monster.Animation
{
    /// <summary>
    /// 加载 Assets/BakeMesh/{SkeletonName}_Frame.bytes（及 YooAsset 兼容地址）。
    /// </summary>
    public static class BakeMeshFrameLoader
    {
        public const string BakeMeshFolder = "BakeMesh";
        private const string SpineBakedMeshFolder = "Res/SpineBakedMesh";
        /// <summary>AddressByGroupAndFileName 时的兼容前缀（当前 BakeMesh 组使用 AddressByFileName）。</summary>
        public const string YooAssetGroupPrefix = "SpineBakedMesh_";
        public const string BakeMeshGroupPrefix = "BakeMesh_";

        private static readonly Dictionary<string, SpineMeshFrameData> Cache = new Dictionary<string, SpineMeshFrameData>();
        private static readonly HashSet<string> MissingLogged = new HashSet<string>();

        public static string GetFrameAssetKey(string skeletonName)
        {
            return skeletonName + "_Frame";
        }

        public static bool HasData(string skeletonName)
        {
            return HasValidData(skeletonName);
        }

        public static bool HasValidData(string skeletonName)
        {
            if (string.IsNullOrEmpty(skeletonName))
            {
                return false;
            }

            if (Cache.TryGetValue(skeletonName, out SpineMeshFrameData cached) && IsValidData(cached))
            {
                return true;
            }

            byte[] bytes = TryLoadAssetBytes(skeletonName);
            if (bytes == null || bytes.Length == 0)
            {
                return false;
            }

            SpineMeshFrameData data = SpineMeshFrameData.ReadBinary(bytes);
            return IsValidData(data);
        }

        public static SpineMeshFrameData Load(string skeletonName)
        {
            if (string.IsNullOrEmpty(skeletonName))
            {
                return null;
            }

            if (Cache.TryGetValue(skeletonName, out SpineMeshFrameData cached))
            {
                return cached;
            }

            byte[] bytes = TryLoadAssetBytes(skeletonName);
            if (bytes == null)
            {
                LogMissingOnce(skeletonName);
                return null;
            }

            SpineMeshFrameData data = SpineMeshFrameData.ReadBinary(bytes);
            if (data == null)
            {
                LogTool.LogError("[BakeMeshFrameLoader] 解析失败: " + skeletonName);
                return null;
            }

            data.OptimizeAllActions();
            Cache[skeletonName] = data;
            return data;
        }

        public static void Preload(IEnumerable<string> skeletonNames)
        {
            if (skeletonNames == null)
            {
                return;
            }

            foreach (string skeletonName in skeletonNames)
            {
                Load(skeletonName);
            }
        }

        public static void ClearCache()
        {
            Cache.Clear();
            MissingLogged.Clear();
        }

        private static bool IsValidData(SpineMeshFrameData data)
        {
            if (data == null || data.ActionDic.Count == 0)
            {
                return false;
            }

            foreach (KeyValuePair<string, SpineMeshActionFrames> pair in data.ActionDic)
            {
                SpineMeshActionFrames action = pair.Value;
                if (action.CompactFrames != null && action.CompactFrames.Length > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static byte[] TryLoadAssetBytes(string skeletonName)
        {
            string frameKey = GetFrameAssetKey(skeletonName);

#if UNITY_EDITOR
            byte[] localBytes = TryLoadBytesFromLocalFile(frameKey);
            if (localBytes != null)
            {
                return localBytes;
            }
#endif

            string[] assetKeys =
            {
                frameKey,
                YooAssetGroupPrefix + frameKey,
                BakeMeshGroupPrefix + frameKey
            };

            for (int i = 0; i < assetKeys.Length; i++)
            {
                byte[] bytes = TryLoadBytesFromYooAsset(assetKeys[i]);
                if (bytes != null)
                {
                    return bytes;
                }
            }

#if !UNITY_EDITOR
            return TryLoadBytesFromLocalFile(frameKey);
#else
            return null;
#endif
        }

        private static byte[] TryLoadBytesFromYooAsset(string assetKey)
        {
            if (Global.ResLoader == null)
            {
                return null;
            }

            TextAsset textAsset = Global.ResLoader.TryLoadSync<TextAsset>(assetKey);
            if (textAsset == null)
            {
                return null;
            }

            return textAsset.bytes;
        }

        private static byte[] TryLoadBytesFromLocalFile(string frameKey)
        {
            string fileName = frameKey + ".bytes";
            string dataPath = Application.dataPath;
            string[] relativePaths =
            {
                Path.Combine(BakeMeshFolder, fileName),
                Path.Combine(SpineBakedMeshFolder, fileName)
            };

            for (int i = 0; i < relativePaths.Length; i++)
            {
                string fullPath = Path.Combine(dataPath, relativePaths[i]);
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                LogTool.Log("[BakeMeshFrameLoader] Editor 本地加载: " + relativePaths[i]);
                return File.ReadAllBytes(fullPath);
            }

            return null;
        }

        private static void LogMissingOnce(string skeletonName)
        {
            if (MissingLogged.Add(skeletonName))
            {
                string frameKey = GetFrameAssetKey(skeletonName);
                LogTool.LogWarning(
                    "[BakeMeshFrameLoader] 未找到烘焙资源 " + frameKey
                    + "（YooAsset 地址无效或缺少 .bytes 文件）。怪物 " + skeletonName
                    + " 将回退 Spine。请执行 Tools/Spine/Spine Mesh Bake Window 烘焙，"
                    + "并将资源放入 Assets/Res/SpineBakedMesh 或 Assets/BakeMesh，"
                    + "然后重新 Collect + Build YooAsset。");
            }
        }
    }
}
