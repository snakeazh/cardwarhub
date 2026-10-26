
namespace FusionContinent.Pool
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using QFramework;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class UIItemPool : Pool<RectTransform>,IScrollPool<RectTransform>
    {
         private readonly RectTransform mPrefab;
        private readonly Transform mParent;
        private readonly bool mWorldPositionStays;
        private List<RectTransform> allObjects = new List<RectTransform>(10);
        public int MaxCount
        {
            get => mMaxCount;
            set => mMaxCount = Math.Max(0, value);
        }

        public UIItemPool(RectTransform prefab, Transform parent, int maxCount = 120, bool worldPositionStays = false)
        {
            mPrefab = prefab;
            mParent = parent;
            mWorldPositionStays = worldPositionStays;
            mMaxCount = Math.Max(0, maxCount);

            SetFactoryMethod(() =>
            {
                var go = mPrefab ? UnityEngine.Object.Instantiate(mPrefab, mParent, mWorldPositionStays) : new GameObject("PooledGameObject").AddComponent<RectTransform>();
                return go;
            });
        }

        public int CachedCount => mCacheStack.Count;

        public bool PrewarmOne()
        {
            if (mCacheStack.Count >= mMaxCount) return false;
            var go = mFactory.Create();
            return Recycle(go);
        }
        
        public override RectTransform Allocate()
        {
            var go = base.Allocate();
            if (mParent && go.transform.parent != mParent)
            {
                go.transform.SetParent(mParent, mWorldPositionStays);
            }
            // go.SetActive(true);
            allObjects.Add(go);
            
        
            return go;
        }

        public override bool Recycle(RectTransform obj)
        {
            if (!obj) return false;
            obj.gameObject.SetActive(false);
            if (mParent)
            {
                obj.transform.SetParent(mParent, mWorldPositionStays);
            }
            if (mCacheStack.Count < mMaxCount)
            {
                mCacheStack.Push(obj);
                return true;
            }
            UnityEngine.Object.Destroy(obj.gameObject);
            return false;
        }
        

        public void Purge()
        {
            this.ReturnAll();
        }

        // 清空并销毁池内对象
        public void DisposeAll()
        {
            while (mCacheStack.Count > 0)
            {
                var obj = mCacheStack.Pop();
                if (obj) UnityEngine.Object.Destroy(obj);
            }
        }
        
        public RectTransform Get()
        {
            RectTransform obj = Allocate();

            obj.gameObject.SetActive(true);
            return obj;
        }
        
        public RectTransform Get(Transform parent)
        {
            var obj = Get();

            obj.transform.SetParent(parent);
            obj.localScale = Vector3.one;
            // obj.localPosition = Vector3.zero;
            return obj;
        }

        void IScrollPool<RectTransform>.Recycle(RectTransform item)
        {
            Recycle(item);
        }

        public bool Return(RectTransform obj)
        {
            return Recycle(obj);
        }
        private bool IsObjectValid(RectTransform obj)
        {
            // Unity 重载的 == 运算符能正确检测销毁对象
            if (obj == null) return false;
        
            // 额外检查场景有效性（防止跨场景对象）
            return obj.gameObject.scene.IsValid(); 
        }
        public void ReturnAll()
        {
            for (int i = allObjects.Count - 1; i >= 0; i--)
            {
                RectTransform obj = allObjects[i];
            
                // 严格对象有效性检查
                if (!IsObjectValid(obj))
                {
                    allObjects.RemoveAt(i);
                    continue;
                }
                
                // 标准回收流程
                obj.gameObject.SetActive(false);
                Return(obj); // 会进入队列二次检查
            }
        }
    }

    public interface IScrollPool<T>
    {
        void Recycle(RectTransform item);
    }
}