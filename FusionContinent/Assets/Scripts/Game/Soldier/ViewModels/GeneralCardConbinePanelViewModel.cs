using System;
using System.Collections.Generic;
using UIFramework;
using Config;
using UIFramework.Services;

namespace Game.Soldier
{
    public class GeneralCardConbinePanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<QualityType> SelectedQuality { get; } = new BindableProperty<QualityType>(QualityType.Default);
        #endregion

        #region 命令
        public Command ConbineCommand { get; private set; }
        public Command QuestionCommand { get; private set; }
        #endregion
        public override Game.UserTool.UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.ArcCardPage);
        }
        #region 事件
        public Action OnRequestConbine;
        public Action OnRequestQuestion;
        public Action<int> OnConbineSucceeded;
        public Action OnConbineFailed;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            ConbineCommand = new Command(RequestConbine);
            QuestionCommand = new Command(RequestQuestion);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            OnRequestConbine = null;
            OnRequestQuestion = null;
            OnConbineSucceeded = null;
            OnConbineFailed = null;
        }

        public void ResetOnOpen()
        {
            if (SelectedQuality.Value == QualityType.Default)
            {
                SelectedQuality.Value = QualityType.Ultimate;
            }
        }

        public void SetSelectedQuality(QualityType qualityType)
        {
            SelectedQuality.Value = qualityType;
        }

        private async void RequestConbine()
        {
            OnRequestConbine?.Invoke();

            var result = await GameServiceManager.Get<SoldierService>().CompositionGeneralSkillCard(SelectedQuality.Value);
            if (result.Success)
            {
                LogTool.E($"合成成功{result.CardId}");
                OnConbineSucceeded?.Invoke(result.CardId);
            }
            else
            {
                OnConbineFailed?.Invoke();
            }
        }

        private void RequestQuestion()
        {
            OnRequestQuestion?.Invoke();
        }
    }
}
