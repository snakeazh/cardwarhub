using UnityEngine;

namespace ScrollView
{
    public class ScrollItem : MonoBehaviour
    {
        object cacheData = null;
        public object Data
        {
            get { return cacheData; }
            set { cacheData = value; }
        }
        public RectTransform TweenTarget;
        public virtual void SetData<T>(T data) where T : new()
        {
            
        }

        public virtual void RefreshSelect<T>(T data) where T : new()
        {
            OnRefreshSelect(data);
        }

        protected virtual void OnRefreshSelect<T>(T data) where T : new()
        {
            
        }

        public virtual void OnSpawn()
        {
            
        }
        
        public virtual void OnRelease()
        {
            
        }
    }
}