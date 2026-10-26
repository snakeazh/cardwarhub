using Config;
using UnityEngine;

namespace Assets.Scripts.Localization
{
    public class LocalizedAudio : MonoBehaviour
    {
        [Tooltip("模块名（如：Audio）")]
        public LocalizationModule Module;
        
        [Tooltip("ResourceKey（如：AUDIO_CLICK）")]
        public string ResourceKey;
        
        private AudioSource _source;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            LocalizationManager.Instance.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            // WebGL 不支持同步加载，统一走异步接口
            StartCoroutine(LocalizationManager.Instance.LoadResourceAsync<AudioClip>(Module, ResourceKey, OnClipLoaded));
        }

        private void OnClipLoaded(AudioClip clip)
        {
            if (clip != null)
                _source.clip = clip;
        }

        private void OnDestroy()
        {
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
        }
    }
}