using Config;
using FusionContinent.Model;
using Game.Challenge;
using QFramework;
using ScrollView;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using IUIData = UIFramework.IUIData;
using Game.RedDot;

namespace FusionContinent
{
	[PrefabPath("AchievementPanel")]
	public class AchievementPanelNew : UIPanel<AchievementPanelViewModel>
	{
		private AchievementPanel mOldPanel;

		void Awake()
		{
			mOldPanel = GetComponent<AchievementPanel>();
			if (mOldPanel == null)
			{
				LogTool.LogError("[AchievementPanelNew] 找不到 AchievementPanel 组件");
			}
		}

		protected override void OnBindViewModel(AchievementPanelViewModel vm)
		{
			if (mOldPanel == null) return;

			mOldPanel.ReturnBtn.onClick.AddListener(() => vm.OnReturnCommand.Execute());
			mOldPanel.DisplayScrollView.SetUpdateFunc(OnUpdateChapter);
			mOldPanel.ActivityLevelScrollView.SetUpdateFunc(OnUpdateActivityChapter);

			vm.CurrentLevelName.OnValueChanged += OnCurrentLevelNameChanged;
			vm.OnClosePanel += CloseSelf;
			vm.OnRefreshList += RefreshLists;
		}

		protected override void OnUnbindViewModel(AchievementPanelViewModel vm)
		{
			if (mOldPanel == null) return;

			mOldPanel.ReturnBtn.onClick.RemoveAllListeners();
			mOldPanel.DisplayScrollView.SetUpdateFunc(null);
			mOldPanel.ActivityLevelScrollView.SetUpdateFunc(null);

			vm.CurrentLevelName.OnValueChanged -= OnCurrentLevelNameChanged;
			vm.OnClosePanel -= CloseSelf;
			vm.OnRefreshList -= RefreshLists;
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			RefreshPanel();
		}

		protected override void OnShow()
		{
			RefreshPanel();
		}

		protected override void OnHide()
		{
		}

		private void RefreshPanel()
		{
			if (VM == null || mOldPanel == null) return;
			VM.RefreshAllData();
			mOldPanel.CurrentLevelText.SetData(LocalizationModule.UITexts, VM.CurrentLevelName.Value);
			RefreshLists();
		}

		private void RefreshLists()
		{
			if (VM == null || mOldPanel == null) return;

			mOldPanel.DisplayScrollView.ShowList(VM.DisplayAllAchievements.Value.Count);
			mOldPanel.DisplayScrollView.ScrollToIndex(0, ScrollAlignment.Start, false);
			mOldPanel.DisplayScrollView.PlayScaleAnimationDOTween();

			mOldPanel.ActivityLevelScrollView.ShowList(VM.DisplayAllActiveAchievements.Value.Count);
			mOldPanel.ActivityLevelScrollView.ScrollToIndex(0, ScrollAlignment.Start, false);
			mOldPanel.ActivityLevelScrollView.PlayScaleAnimationDOTween();
		}

		private void OnCurrentLevelNameChanged(string oldValue, string newValue)
		{
			if (mOldPanel == null) return;
			mOldPanel.CurrentLevelText.SetData(LocalizationModule.UITexts, newValue);
		}

		private void OnUpdateChapter(int dataIndex, ScrollItem scrollItem)
		{
			if (VM == null) return;
			var itemState = VM.BuildChapterItemState(dataIndex);
			var target = scrollItem.TweenTarget;

			target.Find("Name").GetComponent<TMPro.TextMeshProUGUI>().text = itemState.Title;
			var progressImage = target.Find("ProgressBar/CurrentProgress").GetComponent<Image>();
			progressImage.fillAmount = itemState.ProgressFill;

			target.Find("TreasureChest").GetComponent<TreasureChest>().SetData(new TreasureChestData
			{
				Icon = itemState.ChestIcon,
				StyleType = TreasureChestStyleType.Reward,
				CanReceive = itemState.CanReceive,
				Rewards = itemState.Rewards,
				OnClick = () => VM.OnReceiveTaskRewardCommand.Execute(itemState.TaskId)
			});
		}

		private void OnUpdateActivityChapter(int dataIndex, ScrollItem scrollItem)
		{
			if (VM == null) return;
			var itemState = VM.BuildActivityItemState(dataIndex);
			var target = scrollItem.TweenTarget;

			target.Find("ProgressText").GetComponent<TMPro.TextMeshProUGUI>().text = itemState.ProgressText;
			var progressImage = target.Find("ProgressBar/CurrentProgress").GetComponent<Image>();
			progressImage.fillAmount = itemState.ProgressFill;

			target.Find("TreasureChest").GetComponent<TreasureChest>().SetData(new TreasureChestData
			{
				Icon = itemState.ChestIcon,
				StyleType = TreasureChestStyleType.Reward,
				CanReceive = itemState.CanReceive,
				Rewards = itemState.Rewards,
				OnClick = () => VM.OnReceiveActiveRewardCommand.Execute(itemState.ActiveNum)
			});
		}

	}
}
