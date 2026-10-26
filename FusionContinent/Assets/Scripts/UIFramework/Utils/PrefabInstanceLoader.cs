using System;
using UnityEngine;
using Spine.Unity;

namespace UIFramework
{
    /// <summary>
    /// 通用Prefab实例加载器（异步加载 + 替换旧实例 + 防并发覆盖）
    /// 约定：资源地址由调用方传入，交给 UIManager.AssetLoader 加载。
    /// </summary>
    public sealed class PrefabInstanceLoader
    {
        public sealed class Handle
        {
            public GameObject Instance { get; private set; }
            public string Address { get; private set; }

            private int mVersion;

            public void Clear()
            {
                mVersion++;
                Address = null;
                if (Instance != null)
                {
                    UnityEngine.Object.Destroy(Instance);
                    Instance = null;
                }
            }

            public void LoadAndReplace(
                string address,
                Transform parent,
                bool normalizeRect = true,
                bool applyCanvasSortingToSkeletonAnimation = true,
                int orderInLayerOffset = 1,
                Action<GameObject> onInstantiated = null)
            {
                if (string.IsNullOrEmpty(address))
                {
                    Clear();
                    return;
                }

                // 相同地址且实例仍存在：无需重复加载
                if (address == Address && Instance != null)
                {
                    onInstantiated?.Invoke(Instance);
                    return;
                }

                mVersion++;
                var version = mVersion;
                Address = address;

                // 先清旧实例（避免闪烁与重复叠加）
                if (Instance != null)
                {
                    UnityEngine.Object.Destroy(Instance);
                    Instance = null;
                }

                if (parent == null)
                {
                    return;
                }

                UIManager.AssetLoader.LoadAssetAsync<GameObject>(address, prefab =>
                {
                    if (version != mVersion)
                    {
                        return; // 已有更新请求，丢弃过期回调
                    }

                    if (prefab == null)
                    {
                        LogTool.LogError($"[PrefabInstanceLoader] 加载失败: {address}");
                        return;
                    }

                    if (parent == null)
                    {
                        return;
                    }

                    Instance = UnityEngine.Object.Instantiate(prefab, parent, false);
                    Instance.name = $"{prefab.name}@{address}";

                    if (normalizeRect)
                    {
                        var rt = Instance.transform as RectTransform;
                        if (rt != null)
                        {
                            rt.anchorMin = Vector2.zero;
                            rt.anchorMax = Vector2.one;
                            rt.offsetMin = Vector2.zero;
                            rt.offsetMax = Vector2.zero;
                            rt.localScale = Vector3.one;
                            rt.localPosition = Vector3.zero;
                        }
                    }

                    if (applyCanvasSortingToSkeletonAnimation)
                    {
                        ApplySortingFromParentCanvasToSkeletonAnimation(Instance, parent, orderInLayerOffset);
                    }

                    onInstantiated?.Invoke(Instance);
                });
            }

            private static void ApplySortingFromParentCanvasToSkeletonAnimation(GameObject instance, Transform parent, int orderInLayerOffset)
            {
                if (instance == null || parent == null)
                {
                    return;
                }

                // 只在存在 SkeletonAnimation 时才处理（UI里常见是Spine完整prefab）
                var skeletonAnim = instance.GetComponentInChildren<SkeletonAnimation>(true);
                if (skeletonAnim == null)
                {
                    return;
                }

                var canvas = parent.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    return;
                }

                var targetLayerId = canvas.sortingLayerID;
                var targetOrder = canvas.sortingOrder + orderInLayerOffset;

                // SkeletonAnimation 通常由 MeshRenderer/Renderer 输出
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    if (r == null) continue;
                    r.sortingLayerID = targetLayerId;
                    r.sortingOrder = targetOrder;
                }
            }
        }
    }
}

