using System.Collections.Generic;
using FusionContinent.Animation;
using FusionContinent.Model;
using Game.RedDot;
using Game.Station;
using TMPro;
using UIFramework;
using UnityEngine;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    [PrefabPath("StationPanel")]
    public class StationPanelNew : UIPanel<StationPanelViewModel>
    {
        private StationPanel mOldPanel;
        private bool mIsClosing;

        void Awake()
        {
            mOldPanel = GetComponent<StationPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[StationPanelNew] 找不到 StationPanel 组件");
            }
        }

        protected override void OnBindViewModel(StationPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.Mask.onClick.AddListener(OnCloseBtnClicked);
            mOldPanel.ExitBtn.onClick.AddListener(OnCloseBtnClicked);
            mOldPanel.ReceiveBtn.onClick.AddListener(OnReceiveBtnClicked);
            mOldPanel.ReceiveBtn.BindRedDot(RedNodeType.StationReceiveBtn);

            vm.RewardItems.OnValueChanged += OnRewardItemsChanged;
            vm.StoredNumText.OnValueChanged += OnStoredNumTextChanged;
            vm.CanClaim.OnValueChanged += OnCanClaimChanged;
            vm.NextTakeTimestamp.OnValueChanged += OnNextTakeTimestampChanged;
            vm.NextTakeStartTimestamp.OnValueChanged += OnNextTakeStartTimestampChanged;
            vm.NextTakeReward.OnValueChanged += OnNextTakeRewardChanged;
            vm.OnCloseRequested += OnCloseRequested;

            // 数据可能在绑定前已加载完成，先按当前值刷新一次
            RefreshRewardList(vm.RewardItems.Value);
            mOldPanel.HarvestNum.text = vm.StoredNumText.Value;
            mOldPanel.ReceiveBtn.interactable = vm.CanClaim.Value;
            SetTakeTimeText(vm.TakeTimeText.Value);
            RefreshNextTakeTime(vm.NextTakeTimestamp.Value);
            RefreshCountdownFill(vm.NextTakeStartTimestamp.Value, vm.NextTakeTimestamp.Value);
            RefreshNextTakeReward(vm.NextTakeReward.Value);
        }

        protected override void OnUnbindViewModel(StationPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mOldPanel.Mask.onClick.RemoveListener(OnCloseBtnClicked);
            mOldPanel.ExitBtn.onClick.RemoveListener(OnCloseBtnClicked);
            mOldPanel.ReceiveBtn.onClick.RemoveListener(OnReceiveBtnClicked);

            vm.RewardItems.OnValueChanged -= OnRewardItemsChanged;
            vm.StoredNumText.OnValueChanged -= OnStoredNumTextChanged;
            vm.CanClaim.OnValueChanged -= OnCanClaimChanged;
            vm.NextTakeTimestamp.OnValueChanged -= OnNextTakeTimestampChanged;
            vm.NextTakeStartTimestamp.OnValueChanged -= OnNextTakeStartTimestampChanged;
            vm.NextTakeReward.OnValueChanged -= OnNextTakeRewardChanged;
            vm.OnCloseRequested -= OnCloseRequested;
        }

        private void OnCloseBtnClicked()
        {
            VM.OnCloseCommand.Execute();
        }

        private void OnReceiveBtnClicked()
        {
            VM.OnReceiveCommand.Execute();
        }

        private void OnRewardItemsChanged(List<RewardItem> oldItems, List<RewardItem> newItems)
        {
            RefreshRewardList(newItems);
        }

        private void OnStoredNumTextChanged(string oldText, string newText)
        {
            mOldPanel.HarvestNum.text = newText;
        }

        private void OnCanClaimChanged(bool oldValue, bool newValue)
        {
            mOldPanel.ReceiveBtn.interactable = newValue;
        }

        private void OnNextTakeTimestampChanged(long oldValue, long newValue)
        {
            RefreshNextTakeTime(newValue);
            RefreshCountdownFill(VM.NextTakeStartTimestamp.Value, newValue);
        }

        private void OnNextTakeStartTimestampChanged(long oldValue, long newValue)
        {
            RefreshCountdownFill(newValue, VM.NextTakeTimestamp.Value);
        }

        private void RefreshNextTakeTime(long timestamp)
        {
            var timeTextTran = mOldPanel.Content.Find("NextStationEntity/TimeRecord/TimeText");
            if (timeTextTran == null)
            {
                LogTool.LogError("[StationPanelNew] 找不到 NextStationEntity/TimeRecord/TimeText 节点");
                return;
            }

            timeTextTran.GetComponent<TimeRecordBind>().SetData(timestamp);
        }

        private void RefreshCountdownFill(long startTs, long endTs)
        {
            var fillTran = mOldPanel.Content.Find("NextStationEntity/TimeRecord/CountdownImg");
            if (fillTran == null)
            {
                LogTool.LogError("[StationPanelNew] 找不到 NextStationEntity/TimeRecord/CountdownImg 节点");
                return;
            }

            fillTran.GetComponent<CountdownFillBind>().SetData(startTs, endTs);
        }

        private void OnNextTakeRewardChanged(RewardItem oldValue, RewardItem newValue)
        {
            RefreshNextTakeReward(newValue);
        }

        private void RefreshNextTakeReward(RewardItem reward)
        {
            var rewardTran = mOldPanel.Content.Find("NextStationEntity/RewardEntity");
            if (rewardTran == null)
            {
                LogTool.LogError("[StationPanelNew] 找不到 NextStationEntity/RewardEntity 节点");
                return;
            }

            rewardTran.GetComponent<RewardEntity>().SetData(reward);
        }

        private void OnCloseRequested()
        {
            if (mIsClosing) return;
            mIsClosing = true;
            UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, CloseSelf);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (mOldPanel == null) return;

            mIsClosing = false;
            UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject);
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        private void SetTakeTimeText(string text)
        {
            // StageName 字段在 Prefab 上未绑定，改用 TweenTarget/Tip 节点显示"每天X时可领取体力"
            var textTran = mOldPanel.transform.Find("TweenTarget/Tip");
            if (textTran == null)
            {
                LogTool.LogError("[StationPanelNew] 找不到 TweenTarget/Tip 节点");
                return;
            }

            textTran.GetComponent<TMP_Text>().text = text;
        }

        private void RefreshRewardList(List<RewardItem> items)
        {
            var content = mOldPanel.Content;
            // Content 第一个子节点是 NextStationEntity（下一次领取时间），奖励项从索引 1 开始
            for (int i = 0; i < items.Count; i++)
            {
                int childIndex = i + 1;
                GameObject rewObj = childIndex >= content.childCount
                    ? Instantiate(mOldPanel.RewardEntity.gameObject, content, false)
                    : content.GetChild(childIndex).gameObject;

                rewObj.SetActive(true);
                rewObj.GetComponent<RewardEntity>().SetData(items[i]);
            }

            // 清理本次未使用到的旧节点，避免残留上一轮的数据
            for (int i = items.Count + 1; i < content.childCount; i++)
            {
                content.GetChild(i).gameObject.SetActive(false);
            }
        }
    }
}
