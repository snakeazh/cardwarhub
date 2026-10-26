using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using UIFramework.Services;
using Config;
using FusionContinent;

namespace Game.Login
{
    /// <summary>
    /// 语言切换面板 ViewModel
    /// </summary>
    public class LanguageChangeViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> SelectedLanguageIndex { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<LanguageConfig>> LanguageList { get; } = new BindableProperty<List<LanguageConfig>>(new List<LanguageConfig>());
        #endregion

        #region 命令
        public Command<int> OnSelectLanguageCommand { get; private set; }
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 依赖
        private LocalizationService mLocalizationService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 获取Service
            mLocalizationService = GameServiceManager.Get<LocalizationService>();

            // 初始化命令
            OnSelectLanguageCommand = new Command<int>(OnSelectLanguage);
            OnCloseCommand = new Command(OnClose);

            // 加载语言列表
            LoadLanguageList();

            // 加载上次选择的语言
            LoadLastSelectedLanguage();

            LogTool.Log("[LanguageChangeViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[LanguageChangeViewModel] 销毁");
        }

        private void LoadLanguageList()
        {
            // 从配置表加载语言列表
            var langList = DataConfigManager.Tables.TbLanguageConfig.DataList;
            var availableLanguages = new List<LanguageConfig>();
            
            foreach (var cfg in langList)
            {
                if (cfg.State == StateType.Close) continue;
                availableLanguages.Add(cfg);
            }
            
            LanguageList.Value = availableLanguages;
            LogTool.Log($"[LanguageChangeViewModel] 加载了 {availableLanguages.Count} 种语言");
        }

        private void LoadLastSelectedLanguage()
        {
            int lastIndex = PlayerPrefs.GetInt("lastSelectedLanguageIndex", 0);
            SelectedLanguageIndex.Value = lastIndex;
            LogTool.Log($"[LanguageChangeViewModel] 上次选择的语言索引: {lastIndex}");
        }

        #region 命令实现

        private void OnSelectLanguage(int index)
        {
            if (index < 0 || index >= LanguageList.Value.Count)
            {
                LogTool.LogWarning($"[LanguageChangeViewModel] 无效的语言索引: {index}");
                return;
            }

            SelectedLanguageIndex.Value = index;
            
            // 切换语言
            var languageType = (LanguageType)index;
            mLocalizationService?.SetLanguage(languageType);
            
            // 保存选择
            PlayerPrefs.SetInt("lastSelectedLanguageIndex", index);
            PlayerPrefs.Save();
            
            LogTool.Log($"[LanguageChangeViewModel] 切换到语言: {LanguageList.Value[index].Name}");
            
            // 触发语言切换事件
            OnLanguageChanged?.Invoke(index);
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        #endregion

        #region 公共方法

        public LanguageConfig GetLanguageConfig(int index)
        {
            if (index >= 0 && index < LanguageList.Value.Count)
            {
                return LanguageList.Value[index];
            }
            return null;
        }

        #endregion

        #region 事件

        public System.Action<int> OnLanguageChanged;
        public System.Action OnClosePanel;

        #endregion
    }

}
