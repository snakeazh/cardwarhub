using Game.Soldier;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using FusionContinent.Model;
using ScrollView;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Animation;
using Game.RedDot;
using Game.Soldier.RedDot;

namespace FusionContinent
{
    /// <summary>
    /// 养成奖励界面 - 新框架版本 (MVVM)
    /// 复用原 DevelopRewards 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("DevelopRewards")]
    public class DevelopRewardsNew : UIFramework.UIPanel<DevelopRewardsViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private DevelopRewards mOldPanel;

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<DevelopRewards>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[DevelopRewardsNew] 找不到 DevelopRewards 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            LogTool.Log("[DevelopRewardsNew] 已关联 DevelopRewards");
        }

        protected override void OnBindViewModel(DevelopRewardsViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);
            
            // 绑定 ViewModel 事件
            vm.OnCloseRequested += OnCloseRequested;
            vm.OnRewardReceived += OnRewardReceived;
            vm.OnReceiveFailed += OnReceiveFailed;

            // 初始化按钮
            InitButtons(vm);
            
            // 设置滚动视图
            SetupScrollView(vm);
        }

        protected override void OnUnbindViewModel(DevelopRewardsViewModel vm)
        {
            // 取消事件订阅
            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnRewardReceived -= OnRewardReceived;
            vm.OnReceiveFailed -= OnReceiveFailed;
            
            // 清理滚动视图
            if (mOldPanel?.RewardScroll != null)
            {
                mOldPanel.RewardScroll.SetUpdateFunc(null);
            }
        }

        #region 属性绑定

        private void BindProperties(DevelopRewardsViewModel vm)
        {
            if (mOldPanel == null) return;

            // 奖励列表变化
            vm.DevelopRewardList.OnValueChanged += (oldVal, newVal) =>
            {
                if (newVal != null)
                {
                    RefreshRewardList();
                }
            };
        }

        #endregion

        #region 初始化

        private void InitButtons(DevelopRewardsViewModel vm)
        {
            if (mOldPanel == null) return;

            // 关闭按钮
            var quitBtn = mOldPanel.transform.Find("Quit")?.GetComponent<Button>();
            if (quitBtn != null)
            {
                quitBtn.onClick.RemoveAllListeners();
                quitBtn.onClick.AddListener(() =>
                {
                    UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () =>
                    {
                        vm.OnCloseCommand.Execute();
                    });
                });
            }
        }

        private void SetupScrollView(DevelopRewardsViewModel vm)
        {
            if (mOldPanel?.RewardScroll == null) return;

            // 设置更新函数
            mOldPanel.RewardScroll.SetUpdateFunc((dataIndex, scrollItem) =>
            {
                OnUpdateRewardItem(dataIndex, scrollItem as ScrollItem, vm);
            });
        }

        #endregion

        #region 奖励项更新

        private void OnUpdateRewardItem(int dataIndex, ScrollItem scrollItem, DevelopRewardsViewModel vm)
        {
            if (scrollItem == null || vm.DevelopRewardList.Value == null) return;
            if (dataIndex >= vm.DevelopRewardList.Value.Count) return;

            var rewObj = scrollItem.TweenTarget.gameObject;
            var devCfg = vm.DevelopRewardList.Value[dataIndex];
            var cardInfo = vm.CardInfo.Value;

            rewObj.SetActive(true);
            rewObj.transform.localScale = Vector3.one;

            // 设置奖励物品
            var rewardItems = rewObj.transform.Find("RewardItems");
            for (int i = 0; i < rewardItems.childCount; i++)
            {
                var itemUnit = rewardItems.GetChild(i);
                itemUnit.gameObject.SetActive(devCfg.TakeRewards.Count > i);
                if (!itemUnit.gameObject.activeSelf) break;

                var itemAndNum = devCfg.TakeRewards[i];
                itemUnit.GetComponent<RewardEntity>().SetData(itemAndNum.Id, itemAndNum.Num, default, false);
            }

            // 设置文本
            rewObj.transform.Find("TitleText")?.GetComponent<TMP_Text>()?.SetText(
                devCfg.UpGradeRewardDes.Count > 0 ? devCfg.UpGradeRewardDes.First() : ""
            );
            rewObj.transform.Find("DescText")?.GetComponent<TMP_Text>()?.SetText($"Lv {devCfg.Level}");

            // 设置按钮状态
            var receiveBtn = rewObj.transform.Find("ReceiveBtn")?.GetComponent<Button>();
            if (receiveBtn != null)
            {
                receiveBtn.interactable = true;
                receiveBtn.onClick.RemoveAllListeners();
                receiveBtn.onClick.AddListener(() =>
                {
                    vm.OnReceiveRewardCommand.Execute(devCfg.Level);
                });

                receiveBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>()?.SetText("领取");

                // 检查状态
                var status = vm.CheckRewardStatus(devCfg.Level);
                
                // 已领取
                rewObj.transform.Find("ReceivedText")?.gameObject.SetActive(status == Config.RewardStatus.Received);
                receiveBtn.gameObject.SetActive(status != Config.RewardStatus.Received);
                receiveBtn.interactable = status == Config.RewardStatus.CanReceive;

                if (status == Config.RewardStatus.Default)
                {
                    receiveBtn.transform.Find("Text")?.GetComponent<TextMeshProUGUI>()?.SetText("待升级");
                }
            }
        }

        #endregion

        #region UI 刷新

        private void RefreshRewardList()
        {
            if (mOldPanel?.RewardScroll == null || VM?.DevelopRewardList.Value == null) return;

            mOldPanel.RewardScroll.ShowList(VM.DevelopRewardList.Value.Count);
        }

        private void PlayOpenAnimation()
        {
            if (mOldPanel?.OpenAnimation == null) return;

            UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject, () =>
            {
                RefreshRewardList();
                ScrollToTop();
            });
        }

        private void ScrollToTop()
        {
            if (mOldPanel?.RewardScroll == null) return;

            mOldPanel.RewardScroll.ScrollToIndex(0, ScrollAlignment.Start, false);
            mOldPanel.RewardScroll.PlayScaleAnimationDOTween();
        }

        #endregion

        #region 事件处理

        private void OnCloseRequested()
        {
            CloseSelf();
        }

        private void OnRewardReceived(int level, List<RewardAndNum> rewards)
        {
            // 显示奖励提示
            GlobalFunc.ShowRewardTips(rewards);

            // 发布红点更新事件
            LocalRedDotScopeBase.GetActive<SoldierPanelRedDotScope>(SoldierPanelRedDotScope.ScopeName)?.NotifyDevelopChanged();
        }

        private void OnReceiveFailed(string errorMessage)
        {
            FusionContinent.Toast.Show(errorMessage);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetData(int cardId)
        {
            if (VM != null)
            {
                VM.Initialize(cardId);
                PlayOpenAnimation();
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            // 播放打开动画
            PlayOpenAnimation();
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        #endregion
    }
}
