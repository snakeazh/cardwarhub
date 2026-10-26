using System;
using Config;
using FusionContinent;
using QFramework;
using UnityEngine;
using YooAsset;

namespace Assets.Scripts.Localization
{
    public class LocalizationManager : MonoBehaviour
    {
        public static LocalizationManager Instance { get; private set; }
        
        [Tooltip("语言类型（如：ZH、EN）")]
        public LanguageType CurrentLanguage { get; private set; } = default;
        
        private readonly string _prefKey = "UserLanguage";
        private JsonMultiLanguageProvider _provider;

        public event Action OnLanguageChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _provider = new JsonMultiLanguageProvider();
                
                // 加载缓存语言，没有则默认中文
                var lang = PlayerPrefs.GetString(_prefKey, LanguageType.ZH.ToString());
                CurrentLanguage = (LanguageType)Enum.Parse(typeof(LanguageType), lang);
                Initialize(CurrentLanguage);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(LanguageType language)
        {
            CurrentLanguage = language;
            _provider.LoadLanguage(language);
            OnLanguageChanged?.Invoke();
        }

        public void SetLanguage(LanguageType language)
        {
            if (language == CurrentLanguage) return;
            
            CurrentLanguage = language;
            PlayerPrefs.SetString(_prefKey, CurrentLanguage.ToString());
            PlayerPrefs.Save();
            
            _provider.LoadLanguage(language);
            OnLanguageChanged?.Invoke();
        }

        public string GetText(LocalizationModule module, string key, params object[] args)
        {
            if (_provider == null)
                return $"<Missing Key:{key}>";

            var text = _provider.GetText(module, key, CurrentLanguage);
            if (args is { Length: > 0 })
                text = string.Format(text, args);// 替换参数
            
            return text;
        }
        
        // 资源加载规则（支持 fallback）
        public T LoadResource<T>(LocalizationModule module, string key) where T : UnityEngine.Object
        {
            if (_provider != null)
            {
                var path = _provider.GetResourcePath(module, key, CurrentLanguage);
                if (path != null)
                {
                    return YooAssets.LoadAssetSync<T>(path).GetAssetObject<T>();
                }
            }

            // 最后 fallback：key 直接就是路径
            return Resources.Load<T>(key);
        }

        // 异步版本：WebGL 不支持同步加载，WebGL 下必须使用此接口
        public System.Collections.IEnumerator LoadResourceAsync<T>(LocalizationModule module, string key, Action<T> onLoaded) where T : UnityEngine.Object
        {
            string path = _provider != null ? _provider.GetResourcePath(module, key, CurrentLanguage) : null;
            if (path == null)
            {
                // 最后 fallback：key 直接就是路径
                onLoaded?.Invoke(Resources.Load<T>(key));
                yield break;
            }

            var handle = YooAssets.LoadAssetAsync<T>(path);
            yield return handle;
            onLoaded?.Invoke(handle.GetAssetObject<T>());
        }

        public void HotUpdateModule(LocalizationModule module, string jsonContent)
        {
            _provider.HotUpdateModule(module, jsonContent);
            OnLanguageChanged?.Invoke();
        }

        public void LoadModule(LocalizationModule module)
        {
            _provider.LoadLanguage(CurrentLanguage, module);
            OnLanguageChanged?.Invoke();
        }

    }
}