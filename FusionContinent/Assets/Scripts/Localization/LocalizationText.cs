using Config;
using QFramework;
using UnityEngine;
using TMPro;

namespace Assets.Scripts.Localization
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizationText : MonoBehaviour
    {
        [Tooltip("模块名（如：UI、System）")]
        public LocalizationModule Module;
        
        [Tooltip("Key（如：UI_START）")]
        public string Key;
        
        private object[] _args;
        private TextMeshProUGUI _text;

        public TextMeshProUGUI TextUGUI
        {
            get
            {
                return _text;
            }
        }
        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            Refresh();
            LocalizationManager.Instance.OnLanguageChanged += Refresh;
        }

        public void Refresh()
        {
            if (Key == null ||  Key.IsNullOrEmpty())
            {
                return;
            }
            if (LocalizationManager.Instance == null) return;
            if (_text == null) _text = GetComponent<TextMeshProUGUI>();
            
            _text.text = LocalizationManager.Instance.GetText(Module, Key, _args);
            OnRefresh();
        }

        protected virtual void OnRefresh()
        {
            
        }

        public void SetData(LocalizationModule module, string key, params object[] args)
        {
            Key = key;
            _args = args;
            Module = module;
            Refresh();
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= Refresh;
        }
    }
}