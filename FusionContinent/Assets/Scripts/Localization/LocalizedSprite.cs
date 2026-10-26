using Config;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.Localization
{
    public class LocalizedSprite : MonoBehaviour
    {
        [Tooltip("模块名（如：UI、System）")]
        public LocalizationModule Module;
        
        [Tooltip("ResourceKey（如：UI_START）")]
        public string ResourceKey;

        private Image _img;

        void Awake()
        {
            _img = GetComponent<Image>();
            LocalizationManager.Instance.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            // WebGL 不支持同步加载，统一走异步接口
            StartCoroutine(LocalizationManager.Instance.LoadResourceAsync<Sprite>(Module, ResourceKey, OnSpriteLoaded));
        }

        private void OnSpriteLoaded(Sprite sp)
        {
            if (sp != null)
                _img.sprite = sp;
        }

        private void OnDestroy()
        {
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
        }
    }
}