using System;
using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;
using UnityEngine;

namespace Game.Setting
{
    /// <summary>
    /// 设置面板 ViewModel（UIFramework MVVM）
    /// </summary>
    public class SettingPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<bool> IsMusicOn { get; } = new BindableProperty<bool>(true);
        public BindableProperty<bool> IsSoundOn { get; } = new BindableProperty<bool>(true);

        public BindableProperty<string> UserName { get; } = new BindableProperty<string>("");
        public BindableProperty<string> UserIdText { get; } = new BindableProperty<string>("");
        public BindableProperty<string> ScoreText { get; } = new BindableProperty<string>("");
        public BindableProperty<string> StageNameText { get; } = new BindableProperty<string>("");
        public BindableProperty<int> AvatarId { get; } = new BindableProperty<int>(UserData.DefaultAvatarId);

        public BindableProperty<List<string>> LanguageOptions { get; } = new BindableProperty<List<string>>(new List<string>());
        public BindableProperty<int> SelectedLanguageIndex { get; } = new BindableProperty<int>(0);
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnToggleMusicCommand { get; private set; }
        public Command OnToggleSoundCommand { get; private set; }
        public Command OnShowFeedbackCommand { get; private set; }
        public Command OnLogoutCommand { get; private set; }
        public Command OnOpenGiftCodeCommand { get; private set; }
        public Command<int> OnSelectLanguageCommand { get; private set; }
        public Command OnOpenCommand { get; private set; }
        public Command OnCopyUidCommand { get; private set; }
        #endregion

        private int mToolClickCount;

        public override void OnCreate()
        {
            base.OnCreate();

            OnCloseCommand = new Command(OnClose);
            OnToggleMusicCommand = new Command(OnToggleMusic);
            OnToggleSoundCommand = new Command(OnToggleSound);
            OnShowFeedbackCommand = new Command(() => OnShowFeedbackRequested?.Invoke());
            OnLogoutCommand = new Command(OnLogout);
            OnOpenGiftCodeCommand = new Command(() => OnOpenGiftCodeRequested?.Invoke());
            OnSelectLanguageCommand = new Command<int>(OnSelectLanguage);
            OnOpenCommand = new Command(RefreshUserInfo);
            OnCopyUidCommand = new Command(OnCopyUid);

            IsMusicOn.Value = Global.Music.Value;
            IsSoundOn.Value = Global.Sound.Value;

            BuildLanguageOptions();
            SelectedLanguageIndex.Value = (int)LocalizationManager.Instance.CurrentLanguage;

            LogTool.Log("[SettingPanelViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            LogTool.Log("[SettingPanelViewModel] 销毁");
        }

        public void TryOpenToolPanel()
        {
            if (Global.Debug.Value != 1) return;

            mToolClickCount++;
            if (mToolClickCount >= 8)
            {
                mToolClickCount = 0;
                OnOpenToolPanelRequested?.Invoke();
            }
        }

        private void RefreshUserInfo()
        {
            UserName.Value = Global.User.UserName;
            UserIdText.Value = "UID:" + Global.User.UserId;
            ScoreText.Value = Global.PlayerBattle.Score.ToString();
            StageNameText.Value = $"{Global.GetMaxClearStageName()}";
        }

        private void BuildLanguageOptions()
        {
            var options = new List<string>();
            var langList = DataConfigManager.Tables.TbLanguageConfig.DataList;
            foreach (var cfg in langList)
            {
                if (cfg.State == StateType.Close) continue;
                options.Add(cfg.Name);
            }
            LanguageOptions.Value = options;
        }

        private void OnToggleMusic()
        {
            IsMusicOn.Value = !IsMusicOn.Value;
            Global.Music.Value = IsMusicOn.Value;
        }

        private void OnToggleSound()
        {
            IsSoundOn.Value = !IsSoundOn.Value;
            Global.Sound.Value = IsSoundOn.Value;
            OnSoundVolumeRequested?.Invoke(IsSoundOn.Value);
        }

        private void OnSelectLanguage(int idx)
        {
            SelectedLanguageIndex.Value = idx;
            LocalizationManager.Instance.SetLanguage((LanguageType)idx);
        }

        private void OnLogout()
        {
            UserModel.LogOut();
            OnClosePanel?.Invoke();
        }

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        private void OnCopyUid()
        {
            // 纯数字 UID 写入系统剪贴板（不加 "UID:" 前缀，便于玩家直接用于搜索/添加好友）
            GUIUtility.systemCopyBuffer = Global.User.UserId.ToString();
            OnCopyUidCompleted?.Invoke();
        }

        #region 事件
        public Action OnClosePanel;
        public Action OnShowFeedbackRequested;
        public Action OnOpenGiftCodeRequested;
        public Action<bool> OnSoundVolumeRequested;
        public Action OnOpenToolPanelRequested;
        public Action OnCopyUidCompleted;
        #endregion
    }
}

