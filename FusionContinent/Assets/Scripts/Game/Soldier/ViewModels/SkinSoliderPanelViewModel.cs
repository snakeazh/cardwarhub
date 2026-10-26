using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;
using TMPro;
using UnityEngine.UI;

namespace Game.Soldier
{
    /// <summary>
    /// 士兵皮肤选择界面 ViewModel
    /// </summary>
    public class SkinSoliderPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> CardId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> CurrentSkinId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> GlobalSkinId { get; } = new BindableProperty<int>(0);
        public BindableProperty<List<SkinConfig>> SkinConfigs { get; } = new BindableProperty<List<SkinConfig>>();
        public BindableProperty<SkinConfig> SelectedSkin { get; } = new BindableProperty<SkinConfig>();
        public BindableProperty<bool> CanChoose { get; } = new BindableProperty<bool>(false);
        public BindableProperty<string> ChooseBtnText { get; } = new BindableProperty<string>("穿戴");
        #endregion

        #region 命令
        public Command<int> OnSelectSkinCommand { get; private set; }
        public Command OnChooseSkinCommand { get; private set; }
        public Command OnCloseCommand { get; private set; }
        #endregion

        #region 依赖
        private TbSkinConfig mSkinConfig;
        #endregion

        #region 事件
        public System.Action<int> OnSkinChanged;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mSkinConfig = DataConfigManager.Tables.TbSkinConfig;
            
            // 初始化命令
            OnSelectSkinCommand = new Command<int>(OnSelectSkin);
            OnChooseSkinCommand = new Command(OnChooseSkin, CanExecuteChoose);
            OnCloseCommand = new Command(OnClose);
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        public void Initialize(int cardId)
        {
            CardId.Value = cardId;
            
            // 获取该士兵的所有皮肤
            var skinConfigs = mSkinConfig.DataList.Where(t => t.ActsOnCard == cardId).ToList();
            SkinConfigs.Value = skinConfigs;
            
            if (skinConfigs.Count == 0)
            {
                // 没有皮肤
                OnCloseRequested?.Invoke();
                return;
            }
            
            // 获取当前穿戴的皮肤
            if (Global.PlayerCards.ContainsKey(cardId) && Global.PlayerCards[cardId]?.ObjData != null)
            {
                GlobalSkinId.Value = Global.PlayerCards[cardId].ObjData.Skin;
            }
            
            // 如果没有皮肤，默认选择第一个
            if (GlobalSkinId.Value == 0 && skinConfigs.Count > 0)
            {
                // 自动穿戴第一个皮肤
                var firstSkin = skinConfigs[0];
                Global.PlayerCards[cardId].SetSkin(firstSkin.Id, () =>
                {
                    GlobalSkinId.Value = Global.PlayerCards[cardId].ObjData.Skin;
                    SelectSkin(firstSkin.Id);
                });
            }
            else
            {
                // 选择当前穿戴的皮肤
                SelectSkin(GlobalSkinId.Value);
            }
        }

        private void OnSelectSkin(int skinId)
        {
            SelectSkin(skinId);
        }

        private void SelectSkin(int skinId)
        {
            var skinCfg = mSkinConfig.GetOrDefault(skinId);
            if (skinCfg == null) return;
            
            SelectedSkin.Value = skinCfg;
            CurrentSkinId.Value = skinId;
            
            // 更新按钮状态
            UpdateChooseBtnState();
        }

        private void UpdateChooseBtnState()
        {
            var skinCfg = SelectedSkin.Value;
            if (skinCfg == null) return;
            
            // 检查是否已穿戴
            if (CurrentSkinId.Value == GlobalSkinId.Value)
            {
                CanChoose.Value = false;
                ChooseBtnText.Value = "已穿戴";
                return;
            }
            
            // 检查是否解锁
            if (skinCfg.UnlockItemId.HasValue && 
                Global.GetItemNumById(skinCfg.UnlockItemId.Value) == 0 && 
                GlobalSkinId.Value != 0 &&
                skinCfg.Id != SkinConfigs.Value[0].Id)
            {
                CanChoose.Value = false;
                ChooseBtnText.Value = "暂未解锁";
                return;
            }
            
            CanChoose.Value = true;
            ChooseBtnText.Value = "穿戴";
        }

        private bool CanExecuteChoose()
        {
            return CanChoose.Value;
        }

        private void OnChooseSkin()
        {
            if (CurrentSkinId.Value == 0) return;
            
            Global.PlayerCards[CardId.Value].SetSkin(CurrentSkinId.Value, () =>
            {
                GlobalSkinId.Value = Global.PlayerCards[CardId.Value].ObjData.Skin;
                CanChoose.Value = false;
                ChooseBtnText.Value = "已穿戴";
                
                // 触发皮肤变更事件
                OnSkinChanged?.Invoke(CurrentSkinId.Value);
            });
        }

        private void OnClose()
        {
            OnCloseRequested?.Invoke();
        }

        public System.Action OnCloseRequested;
    }
}
