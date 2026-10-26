using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Utils
{
    /// <summary>
    /// 使用 MonoBehaviour 管理图标异步加载请求。
    /// 适合挂在 UI 面板/列表项节点上，在销毁时自动取消所有未完成请求。
    /// </summary>
    public class TextureLoaderMono : MonoBehaviour
    {
        private readonly Dictionary<int, Coroutine> mRunningRequests = new Dictionary<int, Coroutine>();
        private readonly Dictionary<int, int> mLatestRequestByImageId = new Dictionary<int, int>();
        private int mNextRequestId;

        /// <summary>
        /// 发起异步图标加载并设置到目标 Image，返回 requestId。
        /// </summary>
        public int SetIconAsync(
            Image image,
            string group,
            string iconName,
            bool setNativeSize = false,
            Action<bool> onCompleted = null,
            bool useCache = true)
        {
            if (image == null)
            {
                onCompleted?.Invoke(false);
                return -1;
            }

            var imageId = image.GetInstanceID();
            // 同一 Image 发起新请求时，自动取消旧请求，避免列表复用时串图
            if (mLatestRequestByImageId.TryGetValue(imageId, out var oldRequestId))
            {
                CancelRequest(oldRequestId);
            }

            var requestId = ++mNextRequestId;
            var routine = StartCoroutine(RunSetIconRequest(
                requestId,
                imageId,
                image,
                group,
                iconName,
                setNativeSize,
                onCompleted,
                useCache));

            mRunningRequests[requestId] = routine;
            mLatestRequestByImageId[imageId] = requestId;
            return requestId;
        }

        /// <summary>
        /// 取消单个请求。
        /// </summary>
        public bool CancelRequest(int requestId)
        {
            if (!mRunningRequests.TryGetValue(requestId, out var routine))
            {
                return false;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            mRunningRequests.Remove(requestId);
            RemoveImageRequestMappingIfMatch(requestId);
            return true;
        }

        /// <summary>
        /// 取消当前组件发起的所有请求。
        /// </summary>
        public void CancelAllRequests()
        {
            foreach (var pair in mRunningRequests)
            {
                if (pair.Value != null)
                {
                    StopCoroutine(pair.Value);
                }
            }

            mRunningRequests.Clear();
            mLatestRequestByImageId.Clear();
        }

        private IEnumerator RunSetIconRequest(
            int requestId,
            int imageId,
            Image image,
            string group,
            string iconName,
            bool setNativeSize,
            Action<bool> onCompleted,
            bool useCache)
        {
            var completed = false;
            var result = false;

            yield return TextureLoader.TrySetIconAsync(
                image,
                group,
                iconName,
                setNativeSize,
                requestTokenProvider: () => mRunningRequests.ContainsKey(requestId) ? requestId : -1,
                expectedToken: requestId,
                onCompleted: success =>
                {
                    completed = true;
                    result = success;
                },
                useCache: useCache);

            // 可能被 CancelRequest 先移除，这里做幂等删除
            mRunningRequests.Remove(requestId);
            RemoveImageRequestMappingIfMatch(requestId, imageId);

            if (completed)
            {
                onCompleted?.Invoke(result);
            }
            else
            {
                onCompleted?.Invoke(false);
            }
        }

        private void OnDestroy()
        {
            CancelAllRequests();
        }

        private void RemoveImageRequestMappingIfMatch(int requestId, int imageId = 0)
        {
            if (imageId != 0)
            {
                if (mLatestRequestByImageId.TryGetValue(imageId, out var mappedRequestId) && mappedRequestId == requestId)
                {
                    mLatestRequestByImageId.Remove(imageId);
                }
                return;
            }

            var toRemove = 0;
            var found = false;
            foreach (var pair in mLatestRequestByImageId)
            {
                if (pair.Value == requestId)
                {
                    toRemove = pair.Key;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                mLatestRequestByImageId.Remove(toRemove);
            }
        }
    }
}

