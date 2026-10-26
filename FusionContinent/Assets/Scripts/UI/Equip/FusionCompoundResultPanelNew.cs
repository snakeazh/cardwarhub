using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Model;
using Game.Equip;
using TMPro;
using UnityEngine;
using UIFramework;
using UIFramework.Services;

namespace FusionContinent
{
    /// <summary>
    /// 融合石合成结果面板 - MVVM架构
    /// 复用原 FusionCompoundResultPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("FusionCompoundResultPanel")]
    public class FusionCompoundResultPanelNew : UIPanel<FusionCompoundResultPanelViewModel>
    {
        private FusionCompoundResultPanel mOldPanel;

        #region 生命周期

        void Awake()
        {
            mOldPanel = GetComponent<FusionCompoundResultPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[FusionCompoundResultPanelNew] 找不到 FusionCompoundResultPanel 组件！");
                return;
            }

            LogTool.Log("[FusionCompoundResultPanelNew] 已关联 FusionCompoundResultPanel");
        }

        protected override void OnBindViewModel(FusionCompoundResultPanelViewModel vm)
        {
            mOldPanel.Mask.onClick.AddListener(OnMaskClicked);

            vm.OnCloseRequested += OnCloseRequested;
            vm.OnPresentationRefreshRequested += OnPresentationRefreshRequested;
            vm.MainGemId.OnValueChanged += OnResultDataChanged;
            vm.SuccessProbability.OnValueChanged += OnSuccessProbabilityChanged;
            vm.IsSuccess.OnValueChanged += OnResultSuccessChanged;
        }

        protected override void OnUnbindViewModel(FusionCompoundResultPanelViewModel vm)
        {
            mOldPanel.Mask.onClick.RemoveListener(OnMaskClicked);

            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnPresentationRefreshRequested -= OnPresentationRefreshRequested;
            vm.MainGemId.OnValueChanged -= OnResultDataChanged;
            vm.SuccessProbability.OnValueChanged -= OnSuccessProbabilityChanged;
            vm.IsSuccess.OnValueChanged -= OnResultSuccessChanged;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            RefreshPresentation(VM);
        }

        public override void Show()
        {
            base.Show();
        }

        protected override void OnClose()
        {
        }

        #endregion

        #region 按钮

        private void OnMaskClicked()
        {
            VM.OnCloseCommand.Execute();
        }

        #endregion

        #region 事件处理

        private void OnCloseRequested()
        {
            CloseSelf();
        }

        private void OnPresentationRefreshRequested()
        {
            RefreshPresentation(VM);
        }

        private void OnResultDataChanged(int oldVal, int newVal)
        {
            RefreshPresentation(VM);
        }

        private void OnResultSuccessChanged(bool oldVal, bool newVal)
        {
            RefreshPresentation(VM);
        }

        private void OnSuccessProbabilityChanged(float oldVal, float newVal)
        {
            RefreshPresentation(VM);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置合成结果数据
        /// </summary>
        /// <param name="mainGemId">主宝石流水 id</param>
        /// <param name="isSuccess">是否合成成功</param>
        public void SetData(int mainGemId, bool isSuccess)
        {
            VM.Initialize(mainGemId, isSuccess);
        }

        #endregion

        #region 展示刷新

        private void RefreshPresentation(FusionCompoundResultPanelViewModel vm)
        {
            var isSuccess = vm.IsSuccess.Value;
            mOldPanel.Success.gameObject.SetActive(isSuccess);
            mOldPanel.Failure.gameObject.SetActive(!isSuccess);

            var probability = Mathf.Clamp01(vm.SuccessProbability.Value);
            // var percent = Mathf.RoundToInt(probability * 100f);
            // mOldPanel.CurProbability.fillAmount = probability;
            // mOldPanel.CurProText.text = $"高能值:{percent}%";

            
            var gem = Global.PlayerFusionGems[vm.MainGemId.Value];
            mOldPanel.CurProText.text = $"高能值:{Mathf.FloorToInt(gem.Progress*100)}%";
     
            var sprite = Global.FusionGemIconSpriteAtlas.GetSprite(gem.Config.Icon);
            mOldPanel.GemItemIcon.sprite = sprite;
            mOldPanel.GemItemIcon.enabled = sprite != null;

            if (!isSuccess)
            {
                this.mOldPanel.Failure.Find("Cur").Find("Phase").GetComponent<TextMeshProUGUI>().text = 
                    GameServiceManager.GetOrCreate<FusionGemService>().GetGemLevelText(gem.Level);
                var rows = GetLevelRows(gem,gem.Level);
                this.mOldPanel.Failure.Find("Cur").Find("Value").GetComponent<LocalizationText>().
                    SetData(LocalizationModule.UITexts,rows[0].NameKey, rows[0].ValueText);
            }
            else
            {
                this.mOldPanel.Success.Find("Cur").Find("Phase").GetComponent<TextMeshProUGUI>().text = 
                    GameServiceManager.GetOrCreate<FusionGemService>().GetGemLevelText(gem.Level-1);
                this.mOldPanel.Success.Find("Next").Find("Phase").GetComponent<TextMeshProUGUI>().text = 
                    GameServiceManager.GetOrCreate<FusionGemService>().GetGemLevelText(gem.Level);
                
                
                var rowCurrent = GetLevelRows(gem,gem.Level-1);
                this.mOldPanel.Success.Find("Cur").Find("Value").GetComponent<LocalizationText>().
                    SetData(LocalizationModule.UITexts,rowCurrent[0].NameKey, rowCurrent[0].ValueText);
                
                
                var rows = GetLevelRows(gem,gem.Level);
                this.mOldPanel.Success.Find("Next").Find("Value").GetComponent<LocalizationText>().
                    SetData(LocalizationModule.UITexts,rows[0].NameKey, rows[0].ValueText);
            }
           
            // mOldPanel.CurValue.SetData(LocalizationModule.UITexts,rows[0].NameKey, rows[0].ValueText);

        }

        public List<DisplayAttributeRow> GetLevelRows(PlayerFusionGem gem,int level)
        {
            var attributes = gem.GetLevelCfg(level).Attributes;
            List<DisplayAttributeRow> rows = new List<DisplayAttributeRow>();
            if (attributes != null && attributes.Count > 0)
            {
                rows.AddRange(GlobalFunc.GetAttributeRows(gem.GetLevelCfg(level).Attributes));
            }
            var mainAtts = gem.GetLevelCfg(level).MainAttributes;
            if (mainAtts != null && mainAtts.Count > 0)
            {
                rows.AddRange(GlobalFunc.GetMainAttributeRows(gem.GetLevelCfg(level).MainAttributes));
            }

            return rows;
        }

        #endregion
    }
}
