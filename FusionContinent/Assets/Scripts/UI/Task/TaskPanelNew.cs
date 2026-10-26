using Assets.Scripts.Localization;
using Config;
using FusionContinent.Animation;
using FusionContinent.Model;
using Game.Task;
using QFramework;
using ScrollView;
using TMPro;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;
using Game.RedDot;

namespace FusionContinent
{
    [PrefabPath("TaskPanel")]
    public class TaskPanelNew : UIPanel<TaskPanelViewModel>
    {
        private TaskPanel mOldPanel;
        private readonly System.Collections.Generic.Dictionary<Button, UnityAction> mTabHandlers = new System.Collections.Generic.Dictionary<Button, UnityAction>();
        private readonly System.Collections.Generic.Dictionary<Button, UnityAction> mTaskHandlers = new System.Collections.Generic.Dictionary<Button, UnityAction>();
        private UnityAction mCloseBtnHandler;
        private UnityAction mQuitBgHandler;
        private System.Action mRefreshViewHandler;
        private System.Action mPlayListAnimationHandler;
        private bool mIsClosing;

        void Awake()
        {
            mOldPanel = GetComponent<TaskPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[TaskPanelNew] 找不到 TaskPanel 组件");
            }
        }

        protected override void OnBindViewModel(TaskPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mCloseBtnHandler = () => vm.OnCloseCommand.Execute();
            mQuitBgHandler = () => vm.OnCloseCommand.Execute();
            mRefreshViewHandler = RefreshView;
            mPlayListAnimationHandler = PlayListAnimation;

            mOldPanel.CloseBtn.onClick.AddListener(mCloseBtnHandler);
            mOldPanel.QuitBg.onClick.AddListener(mQuitBgHandler);
            mOldPanel.TaskScrollView.SetUpdateFunc(OnUpdateItem);

            vm.OnClosePanel += ClosePanelWithAnimation;
            vm.OnRefreshView += mRefreshViewHandler;
            vm.OnPlayListAnimation += mPlayListAnimationHandler;
            vm.OnShowErrorTip += ShowErrorTip;
            vm.OnPublishTaskRedPoint += PublishTaskRedPoint;

            InitTabs(vm);
        }

        protected override void OnUnbindViewModel(TaskPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mCloseBtnHandler != null)
            {
                mOldPanel.CloseBtn.onClick.RemoveListener(mCloseBtnHandler);
                mCloseBtnHandler = null;
            }

            if (mQuitBgHandler != null)
            {
                mOldPanel.QuitBg.onClick.RemoveListener(mQuitBgHandler);
                mQuitBgHandler = null;
            }

            foreach (var entry in mTabHandlers)
            {
                if (entry.Key != null)
                {
                    entry.Key.onClick.RemoveListener(entry.Value);
                }
            }
            mTabHandlers.Clear();

            foreach (var entry in mTaskHandlers)
            {
                if (entry.Key != null)
                {
                    entry.Key.onClick.RemoveListener(entry.Value);
                }
            }
            mTaskHandlers.Clear();

            mOldPanel.TaskScrollView.SetUpdateFunc(null);

            if (mRefreshViewHandler != null)
            {
                vm.OnRefreshView -= mRefreshViewHandler;
                mRefreshViewHandler = null;
            }

            if (mPlayListAnimationHandler != null)
            {
                vm.OnPlayListAnimation -= mPlayListAnimationHandler;
                mPlayListAnimationHandler = null;
            }

            vm.OnClosePanel -= ClosePanelWithAnimation;
            vm.OnShowErrorTip -= ShowErrorTip;
            vm.OnPublishTaskRedPoint -= PublishTaskRedPoint;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM == null || mOldPanel == null) return;

            mIsClosing = false;
            mOldPanel.TaskScrollView.ShowList(0);
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, false);
            RefreshView();
            PlayOpenAnimation();
        }

        protected override void OnShow()
        {
            if (VM == null || mOldPanel == null) return;

            mIsClosing = false;
            RefreshView();
        }

        protected override void OnHide()
        {
            if (mOldPanel == null) return;

            mIsClosing = false;
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, true);
            for (int i = 0; i < mOldPanel.TaskContent.childCount; i++)
            {
                mOldPanel.TaskContent.GetChild(i).gameObject.SetActive(false);
            }
        }

        private void InitTabs(TaskPanelViewModel vm)
        {
            foreach (var entry in mTabHandlers)
            {
                if (entry.Key != null)
                {
                    entry.Key.onClick.RemoveListener(entry.Value);
                }
            }
            mTabHandlers.Clear();

            var taskTypes = vm.VisibleTaskTypes.Value;
            for (int i = 0; i < mOldPanel.ChangeBtn.childCount; i++)
            {
                var btn = mOldPanel.ChangeBtn.GetChild(i).GetComponent<Button>();
                var isActive = i < taskTypes.Count;
                btn.gameObject.SetActive(isActive);
                if (!isActive) continue;

                var taskTypeCfg = taskTypes[i];
                var icon = btn.transform.Find("Image");
                if (icon != null)
                {
                    icon.localScale = Vector3.zero;
                }

                var text = btn.transform.Find("Text")?.GetComponent<LocalizationText>();
                if (text != null)
                {
                    text.SetData(LocalizationModule.UITexts, taskTypeCfg.Name);
                }

                var handler = new UnityAction(() => vm.OnSelectTaskTypeCommand.Execute(taskTypeCfg.Type));
                btn.onClick.AddListener(handler);
                mTabHandlers[btn] = handler;
            }
        }

        private void RefreshView()
        {
            if (VM == null || mOldPanel == null) return;

            mOldPanel.ActivityNum.text = $"{VM.ActivityProgress.Value}";
            mOldPanel.ActivitySlider.value = VM.ActivityFillAmount.Value;
            UpdateTabSelected(VM.CurrentTaskType.Value);
            RefreshChests();
            RefreshTaskList();
        }

        private void UpdateTabSelected(TaskType taskType)
        {
            for (int i = 0; i < mOldPanel.ChangeBtn.childCount; i++)
            {
                var icon = mOldPanel.ChangeBtn.GetChild(i).Find("Image");
                if (icon != null)
                {
                    icon.localScale = Vector3.zero;
                }
            }

            var taskTypes = VM.VisibleTaskTypes.Value;
            for (int i = 0; i < taskTypes.Count && i < mOldPanel.ChangeBtn.childCount; i++)
            {
                if (taskTypes[i].Type != taskType) continue;
                var icon = mOldPanel.ChangeBtn.GetChild(i).Find("Image");
                if (icon != null)
                {
                    icon.localScale = Vector3.one;
                }
                break;
            }
        }

        private void RefreshChests()
        {
            for (int i = 0; i < mOldPanel.Chest.childCount; i++)
            {
                var chestObj = mOldPanel.Chest.GetChild(i).gameObject;
                var state = VM.BuildChestState(i);
                var hasRewards = state.Rewards != null && state.Rewards.Count > 0;
                chestObj.SetActive(hasRewards);
                if (!hasRewards) continue;

                chestObj.transform.Find("ActivityNum").GetComponent<TMP_Text>().text = state.DisplayText;
                chestObj.transform.Find("TreasureChest").GetComponent<TreasureChest>().SetData(new TreasureChestData
                {
                    Icon = state.Icon,
                    StyleType = TreasureChestStyleType.Reward,
                    CanReceive = state.CanReceive,
                    Rewards = state.Rewards,
                    OnClick = () =>
                    {
                        if (!state.CanReceive)
                        {
                            if (state.IsReceived)
                            {
                                FusionContinent.Toast.Show("已领取");
                            }
                            return;
                        }

                        VM.OnReceiveChestRewardCommand.Execute(state.ActiveNum);
                    }
                });
            }
        }

        private void RefreshTaskList()
        {
            foreach (var entry in mTaskHandlers)
            {
                if (entry.Key != null)
                {
                    entry.Key.onClick.RemoveListener(entry.Value);
                }
            }
            mTaskHandlers.Clear();

            var tasks = VM.DisplayedTasks.Value;
            mOldPanel.TaskScrollView.ShowList(tasks.Count);
        }

        private void OnUpdateItem(int dataIndex, ScrollItem scrollItem)
        {
            if (VM == null) return;

            var task = VM.GetDisplayedTask(dataIndex);
            if (task == null) return;

            var taskObj = scrollItem.TweenTarget;
            // taskObj.transform.Find("TaskTitle").GetComponent<TMP_Text>().text = task.TaskCfg.Desc;
            // taskObj.transform.Find("TaskNum").GetComponent<TMP_Text>().text = $"{task.Progress}/{task.TaskCfg.TargetNum}";
            var taskSlider = taskObj.transform.Find("Slider")?.GetComponent<Slider>();
            if (taskSlider != null)
            {
                // taskSlider.value = task.TaskCfg.TargetNum > 0 ? Mathf.Clamp01((float)task.Progress / task.TaskCfg.TargetNum) : 0f;
            }
            taskObj.transform.Find("ReceivedText").gameObject.SetActive(task.State == TaskState.Received);
            taskObj.transform.Find("TaskBtn/Text").GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts, VM.GetTaskButtonLocalizationKey(task));

            var rewards = taskObj.transform.Find("Rewards");
            for (int i = 0; i < rewards.childCount; i++)
            {
                var rewardObj = rewards.GetChild(i).gameObject;
                // rewardObj.SetActive(task.TaskCfg.Rewards.Count > i);
                if (!rewardObj.activeSelf) continue;

                // var item = task.TaskCfg.Rewards[i];
                // rewardObj.GetComponent<RewardEntity>().SetData(item.Id, item.Num);
            }

            var taskBtn = taskObj.transform.Find("TaskBtn").GetComponent<Button>();
            taskBtn.gameObject.SetActive(task.State != TaskState.Received);

            if (mTaskHandlers.TryGetValue(taskBtn, out var oldHandler))
            {
                taskBtn.onClick.RemoveListener(oldHandler);
                mTaskHandlers.Remove(taskBtn);
            }

            UnityAction handler = null;
            switch (task.State)
            {
                case TaskState.Complete:
                    handler = () => VM.OnReceiveTaskRewardCommand.Execute(task.Id);
                    break;
                case TaskState.Progress:
                    handler = () => { };
                    break;
            }

            if (handler != null)
            {
                taskBtn.onClick.AddListener(handler);
                mTaskHandlers[taskBtn] = handler;
            }
        }

        private void PlayListAnimation()
        {
            if (mOldPanel?.TaskScrollView == null) return;
            mOldPanel.TaskScrollView.PlayScaleAnimationDOTween();
        }

        private void PlayOpenAnimation()
        {
            if (mOldPanel?.OpenAnimation == null) return;
            UIAnimationUtils.PlayAnimations(mOldPanel.OpenAnimation.gameObject);
        }

        private void ClosePanelWithAnimation()
        {
            if (mIsClosing) return;
            mIsClosing = true;

            if (mOldPanel?.CloseAnimation == null)
            {
                CloseSelf();
                return;
            }

            UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () => CloseSelf());
        }

        private void ShowErrorTip(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            FusionContinent.Toast.Show(message);
        }

        private void PublishTaskRedPoint()
        {
            Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.TaskPageDayBtn, null);
        }
    }
}
