using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;
using Game.RedDot;

namespace FusionContinent
{
	public class AchievementPanelData : UIPanelData
	{
	}
	public partial class AchievementPanel : UIPanel
	{
		private int currentStageNum = 0;
		private int clearStageId = 0;
		private StageBattleConfig stageCfg;
		private TbStageBattleConfig _stageBattleConfig;
		private readonly Dictionary<(int, int), StageBattleConfig> _stageCfgMap = new Dictionary<(int, int), StageBattleConfig>();
		
		List<int> displayAllAchievements;
		List<TaskActivityListStruct> displayAllActiveAchievements;

		private void Awake()
		{
			_stageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
			foreach (var cfg in _stageBattleConfig.DataList)
			{
				var cfgKey = (cfg.ChapterId, cfg.Difficulty);
				_stageCfgMap.TryAdd(cfgKey, cfg);
			}
		}

		protected override void OnInit(IUIData uiData = null)
		{
			clearStageId = Global.PlayerBattle.ClearStageId;//获得玩家当前通关ID
			stageCfg = _stageBattleConfig.GetOrDefault(clearStageId);
			mData = uiData as AchievementPanelData ?? new AchievementPanelData();
			// please add init code here
			displayAllAchievements = new List<int>();

			var allTask = DataConfigManager.Tables.TbTaskConfig.DataList;
			foreach (var item in allTask)
			{
				if (item.Type == TaskType.ChapterAchievements)
				{
					displayAllAchievements.Add(item.Id);
				}
			}

			var allActivityTask = DataConfigManager.Tables.TbTaskTypeConfig.DataList;
			displayAllActiveAchievements = new List<TaskActivityListStruct>();
			foreach (var item in allActivityTask)
			{
				if (item.Type == TaskType.ChapterAchievements)
				{
					foreach (var itemActivity in item.ActivityList)
					{
						displayAllActiveAchievements.Add(itemActivity);
					}
				}
			}
			
			this.ReturnBtn.onClick.AddListener(() =>
			{
				UIKit.ClosePanel(this);
			});
			this.DisplayScrollView.SetUpdateFunc(OnUpdateChapter);
			this.ActivityLevelScrollView.SetUpdateFunc(OnUpdateActivityChapter);
			
		}

		private void OnUpdateActivityChapter(int dataIndex, ScrollItem scrollItem)
		{
			var target = scrollItem.TweenTarget;
			var data = this.displayAllActiveAchievements[dataIndex];
			Global.PlayerTaskActives.TryGetValue(TaskType.ChapterAchievements, out var active);
			target.Find("ProgressText").GetComponent<TextMeshProUGUI>().text = $"完成第{data.ActiveNum}章";
			var img = target.Find("ProgressBar/CurrentProgress").GetComponent<Image>();
			img.FillAmount(0);
			bool isReceive, canReceive;
			if (active != null)
			{
				isReceive = active.ReceiveRews != null && active.ReceiveRews.Contains(data.ActiveNum);
				canReceive = !isReceive && active.Progress >= data.ActiveNum;
				int step = 5;
				int maxProgressForThisIndex = (dataIndex + 1) * step;
				int minProgressForThisIndex = dataIndex * step;
				// 逻辑判断
				if (active.Progress >= maxProgressForThisIndex)
				{
					// 情况1：完全通过 -> 填满
					// 比如 Progress=42, dataIndex=7(最大值40)，42 >= 40，设为1
					img.fillAmount = 1.0f; 
				}
				else if (active.Progress > minProgressForThisIndex)
				{
					// 情况2：正在当前段 -> 计算比例
					// 比如 Progress=42, dataIndex=8(范围41-45)，进入此逻辑
					// 进度值减去该段的起始值： 42 - 40 = 2
					float currentVal = active.Progress - minProgressForThisIndex;
					img.fillAmount = currentVal / (float)step; // 2 / 5 = 0.4
				}
				else
				{
					// 情况3：未达到（如果需要重置状态的话）
					img.fillAmount = 0f;
				}
			}
			else
			{
				isReceive = false;
				canReceive = false;
			}
			
			target.Find("TreasureChest").GetComponent<TreasureChest>().SetData(new TreasureChestData()
			{
				Icon = isReceive ? $"{data.ChestIcon}Open" : data.ChestIcon,
				StyleType = TreasureChestStyleType.Reward,
				CanReceive = canReceive,
				// CanReceive = false,
				Rewards = data.Rewards,
				OnClick = (() =>
				{
					ReceiveAchievementChestReward(TaskType.ChapterAchievements, data.ActiveNum);
				})
			});


		}

		private void OnUpdateChapter(int dataIndex, ScrollItem scrollItem)
		{
			// scrollItem.
			var target = scrollItem.TweenTarget;
			var dataId = this.displayAllAchievements[dataIndex];
			var data = DataConfigManager.Tables.TbTaskConfig.Get(dataId);
			// target.Find("Name").GetComponent<TextMeshProUGUI>().text = $"{data.Desc}";
			Global.PlayerTasks.TryGetValue(dataId, out var active);
			var isReceive = active.State == TaskState.Received;
			var obj = target.Find("ProgressBar/CurrentProgress").GetComponent<Image>();
			switch (active.Progress)
			{
				case 0:
					obj.fillAmount = 0f;
					break;
				case 1:
					obj.fillAmount = 0.065f;
					break;
				case 2:
					obj.fillAmount = 0.54f;
					break;
				case 3:
					obj.fillAmount = 1f;
					break;
			}
			target.Find("TreasureChest").GetComponent<TreasureChest>().SetData(new TreasureChestData()
			{
				Icon = isReceive ? "CopperOpen" : "Copper",
				StyleType = TreasureChestStyleType.Reward,
				CanReceive = active.State == TaskState.Complete,
				// Rewards = data.Rewards,
				OnClick = (() =>
				{
					ReceiveAchievementReward(data.Id);
				})
			});
		}

		//获得指定关卡简单、普通、困难的通关情况 
		private int GetChapterState(int chapterId)
		{
			int state = 0;
			for (int difficulty = 1; difficulty <= 3; difficulty++)
			{
				if (_stageCfgMap.TryGetValue((chapterId, difficulty), out var stageCfg))
				{
					int targetStageId = stageCfg.Id;
					bool isCleared = false;
					if (Global.PlayerStages.TryGetValue(targetStageId, out var stageInfo))
					{
						if (stageInfo.Status > 0)
						{
							state++;
							isCleared = true;
						}
					}

					// LogTool.LogError($"章节 {chapterId} - 难度 {difficulty} (关卡ID {targetStageId}): {(isCleared ? "已通关" : "未通关")}");
				}
				else
				{
					LogTool.Log($"章节 {chapterId} 没有配置难度 {difficulty}");
				}
			}
			return state;
		}
		
		/// <summary>
		/// 获得玩家总通过关卡数
		/// </summary>
		private int GetCurrentStageChapter()
		{
			clearStageId = Global.PlayerBattle.ClearStageId;
			int currentMaxChapter = stageCfg != null ? stageCfg.ChapterId : 1;
			return currentMaxChapter;
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			Sort();
			currentStageNum = GetCurrentStageChapter();
		}


		void UpdateCurrentStageName()
		{
			//这里写设置当前关卡名
			if (stageCfg != null)
			{
				CurrentLevelText.SetData(LocalizationModule.UITexts,stageCfg.StageName);
			}
			else
			{
				CurrentLevelText.SetData(LocalizationModule.UITexts, "无");
			}

		}

		void SetContentScrollView()
		{
			this.DisplayScrollView.ShowList(this.displayAllAchievements.Count);
			this.DisplayScrollView.ScrollToIndex(0,ScrollAlignment.Start,false);
			this.DisplayScrollView.PlayScaleAnimationDOTween();
		}

		void SetTitleScrollView()
		{
			this.ActivityLevelScrollView.ShowList(this.displayAllActiveAchievements.Count);
			this.ActivityLevelScrollView.ScrollToIndex(0,ScrollAlignment.Start,false);
			this.ActivityLevelScrollView.PlayScaleAnimationDOTween();
		}
		void Sort()
		{
			displayAllAchievements.Sort();
			displayAllActiveAchievements.Sort((a,b) => { return a.ActiveNum .CompareTo(b.ActiveNum); });
		}
		
		protected override void OnShow()
		{
			UpdateCurrentStageName();
			SetContentScrollView();
			SetTitleScrollView();
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}
		
		private async void ReceiveAchievementReward(int taskId)
		{
			try
			{
				var request = new Cardwar.ReceiveTaskRewardRequest { TaskIds = { taskId } };
				var proto = await WebDataFun.SendProtoPostRequest<Cardwar.ReceiveTaskRewardResponse>(ApiKeys.ApiReceiveTaskReward, request);

				if (proto == null)
				{
					return;
				}

				foreach (var kv in proto.Tasks)
				{
					Global.PlayerTasks[kv.Key] = ProtoMappers.MapPlayerTask(kv.Key, kv.Value);
				}

				if (proto.Items != null)
				{
					Items.Update(proto.Items);
				}

				OnShow();

				var rewards = new Dictionary<string, int>();
				foreach (var kv in proto.Rewards)
				{
					rewards[kv.Key.ToString()] = (int)kv.Value;
				}

			

				Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE+RedNodeType.TaskPageDayBtn,null);
			}
			catch (System.Exception ex)
			{
				LogTool.LogError($"请求失败：{ex.Message}");
				FusionContinent.Toast.Show($"操作失败：{ex.Message}");
			}
			finally
			{
			}
		}
		
		private async void ReceiveAchievementChestReward(TaskType type, int activeNum)
		{
			try
			{
				var request = new Cardwar.ReceiveTaskActiveRewardRequest
				{
					TaskType = (int)type,
					ActiveNum = activeNum,
				};
				var proto = await WebDataFun.SendProtoPostRequest<Cardwar.ReceiveTaskActiveRewardResponse>(ApiKeys.ApiReceiveTaskActiveReward, request);

				if (proto == null)
				{
					return;
				}

				foreach (var kv in proto.Tasks)
				{
					Global.PlayerTaskActives[(TaskType)kv.Key] = ProtoMappers.MapPlayerTaskActiveRewards(kv.Key, kv.Value);
				}

				if (proto.Items != null)
				{
					Items.Update(proto.Items);
				}

				OnShow();

				var rewards = new Dictionary<string, int>();
				foreach (var kv in proto.Rewards)
				{
					rewards[kv.Key.ToString()] = (int)kv.Value;
				}

				// RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
				// rewardTips.SetData(rewards);
				// Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE+RedNodeType.TaskPageDayBtn,null);
			}
			catch (System.Exception ex)
			{
				LogTool.LogError($"请求失败：{ex.Message}");
				FusionContinent.Toast.Show($"操作失败：{ex.Message}");
			}
			finally
			{
			}
		}
	}
}
