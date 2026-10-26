using Config;
using UnityEngine;

namespace Assets.Scripts.Localization
{
    public class LocalizedPrefab : MonoBehaviour
    {
        [Tooltip("模块名（如：Prefab）")]
        public LocalizationModule Module;
        
        [Tooltip("ResourceKey（如：PREFAB_BANNER）")]
        public string ResourceKey;


        private GameObject _instance;

        void Start()
        {
            LocalizationManager.Instance.OnLanguageChanged += Rebuild;
            Rebuild();
        }

        private void Rebuild()
        {
            if (_instance != null)
                Destroy(_instance);

            // WebGL 不支持同步加载，统一走异步接口
            StartCoroutine(LocalizationManager.Instance.LoadResourceAsync<GameObject>(Module, ResourceKey, OnPrefabLoaded));
        }

        private void OnPrefabLoaded(GameObject prefab)
        {
            if (prefab != null)
                _instance = Instantiate(prefab, transform);
        }

        private void OnDestroy()
        {
            LocalizationManager.Instance.OnLanguageChanged -= Rebuild;
        }
    }
}