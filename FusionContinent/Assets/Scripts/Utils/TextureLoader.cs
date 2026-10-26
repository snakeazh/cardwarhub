using System;
using System.Collections;
using System.Collections.Generic;
using FusionContinent.Base;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Utils
{
    /// <summary>
    /// 统一的 UI 图标加载入口（散图版，不依赖 SpriteAtlas）。
    /// 外部按“分组（目录） + 图标名（文件名）”进行加载：
    /// - group: Res/UIIconTextures 下的子目录（可为空）
    /// - iconName: 图片文件名（建议不带扩展名）
    /// </summary>
    public static class TextureLoader
    {
        // 默认按目录地址加载：UIIconTextures/{group}/{icon}
        // 项目资源系统可能存在不同 address 规则，这里提供多候选尝试与可替换的解析器。
        private const string DefaultFolderPrefix = "";

        /// <summary>
        /// 自定义地址解析（可选）。返回一组候选 address，按顺序尝试。
        /// 若为 null，则使用内置规则（基于 UIIconTextures 目录）。
        /// </summary>
        public static Func<string, string, IEnumerable<string>> AddressResolver;

        private static readonly Dictionary<string, Sprite> sSpriteCache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        private static string Normalize(string s) => string.IsNullOrWhiteSpace(s) ? string.Empty : s.Trim().Replace("\\", "/");

        private static string CacheKey(string group, string iconName)
        {
            return $"{Normalize(group)}_{Normalize(iconName)}";
        }

        /// <summary>
        /// 同步加载具体图标（Sprite）。
        /// </summary>
        public static Sprite LoadIcon(string group, string iconName, bool useCache = true)
        {
            iconName = Normalize(iconName);
            if (string.IsNullOrEmpty(iconName))
            {
                return null;
            }

            group = Normalize(group);
            var key = CacheKey(group, iconName);
            if (useCache && sSpriteCache.TryGetValue(key, out var cached) && cached != null)
            {
                return cached;
            }

            var loader = FusionContinent.Global.ResLoader;
            if (loader == null)
            {
                return null;
            }

            Sprite sprite = null;
            foreach (var address in EnumerateCandidateAddresses(group, iconName))
            {
                if (string.IsNullOrEmpty(address))
                {
                    continue;
                }

                try
                {
                    sprite = loader.LoadSync<Sprite>(address);
                }
                catch
                {
                    sprite = null;
                }

                if (sprite != null)
                {
                    break;
                }
            }

            if (useCache)
            {
                sSpriteCache[key] = sprite;
            }

            return sprite;
        }

        public static bool TryLoadIcon(string group, string iconName, out Sprite sprite)
        {
            sprite = LoadIcon(group, iconName, useCache: true);
            return sprite != null;
        }

        /// <summary>
        /// 直接给 Image 赋值（可选 SetNativeSize）。
        /// </summary>
        public static bool TrySetIcon(Image image, string group, string iconName, bool setNativeSize = false)
        {
            if (image == null)
            {
                return false;
            }

            if (!TryLoadIcon(group, iconName, out var sprite))
            {
                return false;
            }

            image.sprite = sprite;
            if (setNativeSize)
            {
                image.SetNativeSize();
            }
            return true;
        }

        /// <summary>
        /// 走 Mono 托管接口：
        /// - 若 image 所在节点没有 TextureLoaderMono，会自动 AddComponent
        /// - 若已有则复用
        /// 返回 requestId（失败返回 -1）
        /// </summary>
        public static int SetIconByMono(
            Image image,
            string group,
            string iconName,
            bool setNativeSize = false,
            Action<bool> onCompleted = null,
            bool useCache = true)
        {
            if (image == null || image.gameObject == null)
            {
                onCompleted?.Invoke(false);
                return -1;
            }

            var loaderMono = GetOrAddMono(image.gameObject);
            return loaderMono != null
                ? loaderMono.SetIconAsync(image, group, iconName, setNativeSize, onCompleted, useCache)
                : -1;
        }

        /// <summary>
        /// 获取或自动添加 TextureLoaderMono。
        /// </summary>
        public static TextureLoaderMono GetOrAddMono(GameObject host)
        {
            if (host == null)
            {
                return null;
            }

            var mono = host.GetComponent<TextureLoaderMono>();
            if (mono == null)
            {
                mono = host.AddComponent<TextureLoaderMono>();
            }
            return mono;
        }

        /// <summary>
        /// 异步加载（协程）。
        /// 使用方：StartCoroutine(TextureLoader.LoadIconAsync(...))
        /// </summary>
        public static IEnumerator LoadIconAsync(string group, string iconName, Action<Sprite> onLoaded, bool useCache = true)
        {
            onLoaded ??= (_)=> { };

            iconName = Normalize(iconName);
            if (string.IsNullOrEmpty(iconName))
            {
                onLoaded(null);
                yield break;
            }

            group = Normalize(group);

            var key = CacheKey(group, iconName);
            if (useCache && sSpriteCache.TryGetValue(key, out var cached) && cached != null)
            {
                onLoaded(cached);
                yield break;
            }

            var loader = FusionContinent.Global.ResLoader;
            if (loader == null)
            {
                onLoaded(null);
                yield break;
            }

            Sprite sprite = null;
            foreach (var address in EnumerateCandidateAddresses(group, iconName))
            {
                if (string.IsNullOrEmpty(address))
                {
                    continue;
                }

                yield return loader.LoadAsync<Sprite>(address, loaded => sprite = loaded);
                if (sprite != null)
                {
                    break;
                }
            }

            if (useCache)
            {
                sSpriteCache[key] = sprite;
            }

            onLoaded(sprite);
        }

        /// <summary>
        /// 安全异步设置图标：
        /// - 加载完成后会再次检查 image 是否仍然有效
        /// - 可选 requestToken，用于防止复用节点导致的“旧请求覆盖新图标”
        /// </summary>
        public static IEnumerator TrySetIconAsync(
            Image image,
            string group,
            string iconName,
            bool setNativeSize = false,
            Func<int> requestTokenProvider = null,
            int expectedToken = 0,
            Action<bool> onCompleted = null,
            bool useCache = true)
        {
            if (image == null)
            {
                onCompleted?.Invoke(false);
                yield break;
            }

            var imageInstanceId = image.GetInstanceID();
            Sprite loadedSprite = null;

            yield return LoadIconAsync(group, iconName, s => loadedSprite = s, useCache);

            // 1) 节点可能在加载期间销毁
            if (image == null)
            {
                onCompleted?.Invoke(false);
                yield break;
            }

            // 2) 极端情况下对象被销毁重建后引用异常，额外校验实例 ID
            if (image.GetInstanceID() != imageInstanceId)
            {
                onCompleted?.Invoke(false);
                yield break;
            }

            // 3) 可选 token 校验：用于列表复用/快速切换时丢弃旧请求
            if (requestTokenProvider != null && requestTokenProvider() != expectedToken)
            {
                onCompleted?.Invoke(false);
                yield break;
            }

            if (loadedSprite == null)
            {
                onCompleted?.Invoke(false);
                yield break;
            }

            image.sprite = loadedSprite;
            if (setNativeSize)
            {
                image.SetNativeSize();
            }

            onCompleted?.Invoke(true);
        }

        private static IEnumerable<string> EnumerateCandidateAddresses(string group, string iconName)
        {
            if (AddressResolver != null)
            {
                IEnumerable<string> custom = null;
                try
                {
                    custom = AddressResolver(group, iconName);
                }
                catch
                {
                    custom = null;
                }

                if (custom != null)
                {
                    foreach (var a in custom)
                    {
                        yield return a;
                    }
                    yield break;
                }
            }

            // 1) UIIconTextures/{group}/{icon}
            if (string.IsNullOrEmpty(group))
            {
                yield return $"{iconName}";
            }
            else
            {
                yield return $"{group}_{iconName}";
            }

            // 2) 有些 address 会带前缀或不带目录，这里提供额外容错候选
            yield return iconName;
        }

        /// <summary>
        /// 清理本地 Sprite 缓存（不会释放 YooAssetLoader 内部句柄）。
        /// </summary>
        public static void ClearCache()
        {
            sSpriteCache.Clear();
        }
    }
}
