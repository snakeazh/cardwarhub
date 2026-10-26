using Game.UI;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FusionContinent.Animation;
using FusionContinent.Sounds;
using Manager;
using System.Collections;
using System.Collections.Generic;
using ScrollView;
using UIFramework.Services;

namespace FusionContinent
{
    [PrefabPath("RewardTips")]
    /// <summary>
    /// 奖励提示面板 - 新框架版本 (MVVM)
    /// 复用原 RewardTips 的 UI 组件
    /// </summary>    
    public class RewardTipsNew : UIPanel<RewardTipsViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private RewardTips mOldPanel;
        private int mSingleLineMaxCount = 5;
        private float mAdditionalHeight = 10f;

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<RewardTips>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[RewardTipsNew] 找不到 RewardTips 组件！请确保预制体上同时挂载了两个脚本");
                return;
            }

            LogTool.Log("[RewardTipsNew] 已关联 RewardTips");
        }

        protected override void OnBindViewModel(RewardTipsViewModel vm)
        {
            // 绑定属性
            BindProperties(vm);
            
            // 绑定按钮
            BindButtons(vm);
        }

        private void BindProperties(RewardTipsViewModel vm)
        {
            // 奖励列表变化绑定
            vm.RewardItems.OnValueChanged += OnRewardItemsChanged;
        }

        private void BindButtons(RewardTipsViewModel vm)
        {
            mOldPanel.CloseBtn.onClick.AddListener(OnCloseButtonClicked);
        }

        protected override void OnUnbindViewModel(RewardTipsViewModel vm)
        {
            // 解绑属性
            vm.RewardItems.OnValueChanged -= OnRewardItemsChanged;

            // 解绑按钮
            mOldPanel.CloseBtn.onClick.RemoveListener(OnCloseButtonClicked);
        }

        private void OnRewardItemsChanged(List<FusionContinent.Model.RewardItem> oldVal, List<FusionContinent.Model.RewardItem> newVal)
        {
            RefreshView(VM, true);
        }

        private void OnCloseButtonClicked()
        {
            PlayCloseAnimation();
        }

        #region 动画处理

        private void PlayOpenAnimation()
        {
            // 播放打开动画
            UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject, OnAnimationOpenComplete);
        }

        private void PlayCloseAnimation()
        {
            // 播放关闭动画
            UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, OnAnimationCloseComplete);
        }

        private void OnAnimationOpenComplete()
        {
            // 动画打开完成，刷新ScrollView
            var rewards = GetRewardItems();
            if (rewards == null) return;

            mOldPanel.RewardScrollView.ShowList(rewards.Count);
            mOldPanel.RewardScrollView.ScrollToIndex(0, ScrollAlignment.Start, false);
            mOldPanel.RewardScrollView.PlayScaleAnimationDOTween();

            PlayGetSound(rewards.Count);
        }

        private void OnAnimationCloseComplete()
        {
            // 动画关闭完成，关闭面板
            var service = GameServiceManager.Get<RewardTipsService>();
            if (service == null)
            {
                LogTool.LogError("[RewardTipsNew] 找不到 RewardTipsService，无法关闭奖励提示界面");
                return;
            }

            service.CloseRewardTips();
        }

        private void PlayGetSound(int count = 1)
        {
            StartCoroutine(DelayPlayGetSound(count));
        }

        private IEnumerator DelayPlayGetSound(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                SoundManager.Instance.PlayVoice(SoundType.GetReward);
                yield return new WaitForSeconds(0.15f);
            }
        }

        #endregion

        #region 布局刷新

        private void RefreshView(RewardTipsViewModel vm, bool playAnimation)
        {
            // RefreshTitleAndTip(vm);
            RefreshRewards(playAnimation);
        }

        private void RefreshTitleAndTip(RewardTipsViewModel vm)
        {
            var titleTextTransform = mOldPanel.Title.Find("Text");
            if (titleTextTransform == null)
            {
                LogTool.LogError("[RewardTipsNew] Title 缺少 Text 节点");
                return;
            }

            var titleText = titleTextTransform.GetComponent<TMP_Text>();
            if (titleText == null)
            {
                LogTool.LogError("[RewardTipsNew] Title/Text 缺少 TMP_Text 组件");
                return;
            }

            titleText.text = vm.Title;
            mOldPanel.TipText.text = vm.TipText;
            mOldPanel.TipText.gameObject.SetActive(vm.IsTipTextVisible);
        }

        private void RefreshRewards(bool playAnimation)
        {
            RefreshRewardLayout();

            if (playAnimation)
            {
                PlayOpenAnimation();
            }
        }

        private void RefreshRewardLayout()
        {
            var rewards = GetRewardItems();
            if (rewards == null) return;

            var size = CalculateDisplayedSize(rewards.Count);
            mOldPanel.RewardScrollView.GetComponent<RectTransform>().sizeDelta = size;
            mOldPanel.Reward.sizeDelta = new Vector2(mOldPanel.Reward.sizeDelta.x, size.y + 380f);
            mOldPanel.RewardScrollView.ShowList(0);
        }

        private List<FusionContinent.Model.RewardItem> GetRewardItems()
        {
            var rewards = VM.RewardItems.Value;
            if (rewards == null)
            {
                LogTool.LogError("[RewardTipsNew] VM.RewardItems 为 null");
                return null;
            }

            if (rewards.Count == 0)
            {
                LogTool.LogError("[RewardTipsNew] VM.RewardItems 为空");
                return null;
            }

            return rewards;
        }

        private Vector2 CalculateDisplayedSize(int count)
        {
            if (count <= 0)
            {
                LogTool.LogError("[RewardTipsNew] 奖励数量必须大于 0");
                return Vector2.zero;
            }

            var line = Mathf.CeilToInt(count * 1f / mSingleLineMaxCount);
            var itemSize = mOldPanel.RewardScrollView.ItemSize;
            var itemSpacing = mOldPanel.RewardScrollView.ItemSpacing;

            if (count <= mSingleLineMaxCount)
            {
                return new Vector2(
                    itemSpacing.x * (count - 1) + itemSize.x * count,
                    itemSpacing.y * (line - 1) + itemSize.y * line);
            }

            if (count <= mSingleLineMaxCount * 2)
            {
                return new Vector2(
                    itemSpacing.x * (mSingleLineMaxCount - 1) + itemSize.x * mSingleLineMaxCount,
                    itemSpacing.y * (line - 1) + itemSize.y * line + mAdditionalHeight);
            }

            return new Vector2(
                itemSpacing.x * (mSingleLineMaxCount - 1) + itemSize.x * mSingleLineMaxCount,
                itemSpacing.y * 2f + itemSize.y * 2.5f + mAdditionalHeight);
        }

        #endregion

        #region ScrollView刷新

        /// <summary>
        /// 刷新奖励项显示
        /// </summary>
        private void RefreshReward(int dataIndex, ScrollItem scrollItem)
        {
            var rewards = GetRewardItems();
            if (rewards == null) return;

            if (dataIndex < 0 || dataIndex >= rewards.Count)
            {
                LogTool.LogError($"[RewardTipsNew] 奖励索引越界: {dataIndex}");
                return;
            }

            var data = rewards[dataIndex];
            var rootRect = scrollItem.transform as RectTransform;
            var rewObj = rootRect.Find("RewardEntity");
            
            if (rewObj == null)
            {
                LogTool.LogError("[RewardTipsNew] ScrollItem 缺少 RewardEntity 节点");
                return;
            }
            
            rewObj.localScale = Vector3.one;
            var rewardEntity = rewObj.GetComponent<RewardEntity>();
            if (rewardEntity == null)
            {
                LogTool.LogError("[RewardTipsNew] RewardEntity 节点缺少 RewardEntity 组件");
                return;
            }

            rewardEntity.SetData(data, RewardStyleType.Default, dataIndex < 10);
        }

        #endregion

        protected override void OnOpen(IUIData uiData = null)
        {
            // 面板打开时的逻辑
            mOldPanel.RewardScrollView.SetUpdateFunc(RefreshReward);
        }

        protected override void OnShow()
        {
            // 面板显示时的逻辑
        }

        protected override void OnHide()
        {
            // 面板隐藏时的逻辑
        }

        protected override void OnClose()
        {
            // 面板关闭时的逻辑
            // 重置状态
            mOldPanel.TipText.gameObject.SetActive(false);
        }
    }
}
