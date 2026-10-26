using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Base
{
    /// <summary>
    /// UI 贴图（Sprite）异步加载管理器：传入完整资源地址（如 UITextures_XXX），
    /// 统一排队后在 Update 中轮询发起 YooAsset 异步加载，完成后自动设置到 Image。
    /// - 同名地址只加载一次并缓存，并发请求共享同一个加载
    /// - 同一 Image 请求新图时旧请求自动失效，ScrollView 复用不会串图
    /// - 加载完成的 handle 留在 Global.ResLoader 缓存中，WebGL 下后续 LoadSync 同名资源可直接命中
    /// </summary>
    public class AsyncTextureManager : MonoBehaviour
    {
        private const int MaxConcurrentLoads = 8;

        private static AsyncTextureManager sInstance;
        public static AsyncTextureManager Instance
        {
            get
            {
                if (sInstance == null)
                {
                    var go = new GameObject(nameof(AsyncTextureManager));
                    DontDestroyOnLoad(go);
                    sInstance = go.AddComponent<AsyncTextureManager>();
                }
                return sInstance;
            }
        }

        private class SetTarget
        {
            public Image Img;
            public int ImgId;
            public bool IsNative;
            public Action<bool> OnCompleted;
        }

        private class LoadEntry
        {
            public readonly List<SetTarget> Targets = new List<SetTarget>();
        }

        private readonly Dictionary<string, Sprite> mCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, LoadEntry> mLoading = new Dictionary<string, LoadEntry>();
        private readonly Queue<string> mPendingQueue = new Queue<string>();
        private readonly Dictionary<int, string> mLatestAddressByImage = new Dictionary<int, string>();
        private int mRunningCount;

        /// <summary>
        /// 异步加载 address 并设置到 img。已缓存时同步设置；未缓存则排队加载，完成后自动设置。
        /// 地址无效（未打进包）或 img 为空时返回 false，不发起加载，调用方可继续尝试其它候选。
        /// </summary>
        public bool SetSprite(Image img, string address, bool isNative = false, Action<bool> onCompleted = null)
        {
            if (img == null || string.IsNullOrEmpty(address))
            {
                return false;
            }
            if (Global.ResLoader == null || !Global.ResLoader.CheckLocationValid(address))
            {
                LogTool.LogError($"[AsyncTextureManager] 资源地址无效: {address}");
                return false;
            }

            var imgId = img.GetInstanceID();
            string latest;
            if (!mLatestAddressByImage.TryGetValue(imgId, out latest) || latest != address)
            {
                // 该 Image 被复用展示新图，先清掉旧图，避免加载期间显示上一张
                img.sprite = null;
            }
            mLatestAddressByImage[imgId] = address;

            var cached = GetCached(address);
            if (cached != null)
            {
                ApplySprite(img, cached, isNative);
                if (onCompleted != null)
                {
                    onCompleted.Invoke(true);
                }
                return true;
            }

            var target = new SetTarget
            {
                Img = img,
                ImgId = imgId,
                IsNative = isNative,
                OnCompleted = onCompleted,
            };
            LoadEntry entry;
            if (mLoading.TryGetValue(address, out entry))
            {
                entry.Targets.Add(target);
                return true;
            }

            entry = new LoadEntry();
            entry.Targets.Add(target);
            mLoading.Add(address, entry);
            mPendingQueue.Enqueue(address);
            return true;
        }

        /// <summary>使该 Image 上未完成的异步请求失效（例如槽位复用后改用图集直设 sprite）。</summary>
        public void InvalidateImage(Image img)
        {
            if (img == null)
            {
                return;
            }
            mLatestAddressByImage.Remove(img.GetInstanceID());
        }

        /// <summary>取已加载完成的缓存，未就绪返回 null（不触发加载）。</summary>
        public Sprite GetCached(string address)
        {
            Sprite sprite;
            if (mCache.TryGetValue(address, out sprite) && sprite != null)
            {
                return sprite;
            }
            // 可能已被其它流程（如启动预载）加载进 ResLoader
            if (Global.ResLoader != null && Global.ResLoader.TryGetLoaded(address, out sprite))
            {
                mCache[address] = sprite;
                return sprite;
            }
            return null;
        }

        private void Update()
        {
            while (mRunningCount < MaxConcurrentLoads && mPendingQueue.Count > 0)
            {
                mRunningCount++;
                StartCoroutine(RunLoad(mPendingQueue.Dequeue()));
            }
        }

        private IEnumerator RunLoad(string address)
        {
            Sprite sprite = null;
            yield return Global.ResLoader.LoadAsync<Sprite>(address, s => sprite = s);

            mRunningCount--;
            var entry = mLoading[address];
            mLoading.Remove(address);

            if (sprite == null)
            {
                LogTool.LogError($"[AsyncTextureManager] 加载失败: {address}");
            }
            else
            {
                mCache[address] = sprite;
            }

            for (int i = 0; i < entry.Targets.Count; i++)
            {
                CompleteTarget(entry.Targets[i], address, sprite);
            }
        }

        private void CompleteTarget(SetTarget target, string address, Sprite sprite)
        {
            var success = false;
            if (sprite != null && target.Img != null && IsLatestRequest(target.ImgId, address))
            {
                ApplySprite(target.Img, sprite, target.IsNative);
                success = true;
            }
            if (target.OnCompleted != null)
            {
                target.OnCompleted.Invoke(success);
            }
        }

        private bool IsLatestRequest(int imgId, string address)
        {
            string latest;
            return mLatestAddressByImage.TryGetValue(imgId, out latest) && latest == address;
        }

        private void ApplySprite(Image img, Sprite sprite, bool isNative)
        {
            img.sprite = sprite;
            if (isNative)
            {
                img.SetNativeSize();
            }
        }
    }
}
