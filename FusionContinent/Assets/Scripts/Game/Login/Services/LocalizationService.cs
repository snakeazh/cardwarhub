using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.Localization;
using Config;

namespace Game.Login
{
    /// <summary>
    /// 本地化服务 - 处理语言切换
    /// </summary>
    public class LocalizationService : GameServiceBase
    {
        public LanguageType CurrentLanguage { get; private set; }

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            
            // 初始化当前语言
            CurrentLanguage = LocalizationManager.Instance.CurrentLanguage;
            
            LogTool.Log($"[LocalizationService] 初始化完成，当前语言: {CurrentLanguage}");
        }

        /// <summary>
        /// 设置语言
        /// </summary>
        public void SetLanguage(LanguageType languageType)
        {
            if (CurrentLanguage == languageType) return;
            
            LocalizationManager.Instance.SetLanguage(languageType);
            CurrentLanguage = languageType;
            
            LogTool.Log($"[LocalizationService] 语言已切换为: {languageType}");
            
            // 触发全局语言切换事件
            OnLanguageChanged?.Invoke(languageType);
        }

        /// <summary>
        /// 获取当前语言
        /// </summary>
        public LanguageType GetCurrentLanguage()
        {
            return CurrentLanguage;
        }

        #region 事件

        public event System.Action<LanguageType> OnLanguageChanged;

        #endregion
    }
}
