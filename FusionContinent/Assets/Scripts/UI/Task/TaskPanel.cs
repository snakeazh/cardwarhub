using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using FusionContinent.Animation;
using FusionContinent.Model;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using TMPro;
using UnityEngine.Events;
using Object = System.Object;
using Game.RedDot;

namespace FusionContinent
{
	public class TaskPanelData : UIPanelData
	{
	}
	public partial class TaskPanel : UIPanel
	{
		private TaskType _currentTaskType;
		private Dictionary<int, GameObject> _taskObjs = new Dictionary<int, GameObject>();
		private Dictionary<TaskType, Button> _buttonMap = new Dictionary<TaskType, Button>();
		
		private TbTaskTypeConfig _tbTaskTypeConfig;

		private void Awake()
		{
			_tbTaskTypeConfig = DataConfigManager.Tables.TbTaskTypeConfig;
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as TaskPanelData ?? new TaskPanelData();
			CloseBtn.onClick.AddListener(OnClose);
			QuitBg.onClick.AddListener(OnClose);
			
			// 页签切换
			var taskTypeList = _tbTaskTypeConfig.DataList.Where(v => v.ShowPanel == ShowPanelType.DailyTask).ToList();
			for (int i = 0; i < ChangeBtn.childCount; i++)
			{
				int idx = i;
				Button changeBtn = ChangeBtn.GetChild(idx).GetComponent<Button>();
				changeBtn.gameObject.SetActive(taskTypeList.Count > idx);
				if (!changeBtn.gameObject.activeSelf) continue;
				
				var taskTypeCfg = taskTypeList[idx];
				changeBtn.transform.Find("Image").localScale = Vector3.zero;
				changeBtn.transform.Find("Text").GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts, taskTypeCfg.Name);
				changeBtn.onClick.AddListener(() =>
				{
					for (int j = 1; j < ChangeBtn.childCount; j++)
					{
						if (idx != j)
						{
							ChangeBtn.GetChild(j).Find("Image").localScale = Vector3.zero;
						}
					}

					changeBtn.transform.Find("Image").localScale = Vector3.one;
					SetData(taskTypeCfg.Type);
					this.TaskScrollView.PlayScaleAnimationDOTween();
				});
				
				// if (idx == 0) changeBtn.onClick.Invoke();
				_buttonMap.Add(taskTypeCfg.Type, changeBtn);
			}
			
			this.TaskScrollView.SetUpdateFunc(OnUpdateItem);
		}

		private void OnUpdateItem(int dataIndex, ScrollItem scrollItem)
		{
			var v = Global.PlayerTasks[this._displayedTasks[dataIndex].Id];// 重新读取数据
			var taskObj = scrollItem.TweenTarget;
			// taskObj.transform.Find("TaskTitle").GetComponent<TMP_Text>().text = v.TaskCfg.Desc;
			// taskObj.transform.Find("TaskNum").GetComponent<TMP_Text>().text = $"{v.Progress}/{v.TaskCfg.TargetNum}";
			taskObj.transform.Find("ReceivedText").gameObject.SetActive(v.State == TaskState.Received);
			taskObj.transform.Find("TaskBtn/Text").GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts, 
				v.State == TaskState.Progress ? "UI_BTN_GO_TO" : "UI_REW_NOT_CLAIMED");
            
			// 奖励
			var rewards = taskObj.transform.Find("Rewards");
			for (int i = 0; i < rewards.childCount; i++)
			{
				var rewObj = rewards.GetChild(i).gameObject;
				// rewObj.SetActive(v.TaskCfg.Rewards.Count > i);
				if (!rewObj.activeSelf) continue;

				// var item = v.TaskCfg.Rewards[i];
				// rewObj.GetComponent<RewardEntity>().SetData(item.Id, item.Num);
			}
				
			// 点击事件
			var taskBtn = taskObj.transform.Find("TaskBtn").GetComponent<Button>();
			taskBtn.gameObject.SetActive(v.State != TaskState.Received);
			taskBtn.onClick.RemoveAllListeners();
			taskBtn.onClick.AddListener(() =>
			{
				switch (v.State)
				{
					case TaskState.Progress:
						// LogTool.Log($"点击前往: {v.Id}");
						break;
						
					case TaskState.Complete:
						// LogTool.Log($"点击领取: {v.Id}");
						ReceiveTaskReward(v.Id);
						break;
						
					default:
						// LogTool.Log($"已领取: {v.Id}");
						break;
				}
			});
		}
		PrioritySorter<PlayerTask> _prioritySorter = new PrioritySorter<PlayerTask>();
		private List<PlayerTask> _displayedTasks = new List<PlayerTask>();
		
		private void SetData(TaskType type)
		{
			_currentTaskType = type;
			
			// 活跃度
			var typeCfg = _tbTaskTypeConfig.GetOrDefault(type);
			if (typeCfg != null && Global.PlayerTaskActives.TryGetValue(type, out var active))
			{
				ActivityNum.text = $"{active.Progress}";
				GreenBur.fillAmount = Mathf.Clamp01((float)active.Progress / typeCfg.ActivityList.Last().ActiveNum);

				var activityList = typeCfg.ActivityList;
				for (int i = 0; i < Chest.childCount; i++)
				{
					var chestObj = Chest.GetChild(i).gameObject;
					chestObj.SetActive(activityList.Count > i);
					if (!chestObj.activeSelf) continue;

					var activeCfg = activityList[i];
					var isReceiveChest = active.ReceiveRews != null && active.ReceiveRews.Contains(activeCfg.ActiveNum);
					chestObj.transform.Find("ActivityNum").GetComponent<TMP_Text>().text = activeCfg.ActiveNum.ToString();
					
					// 宝箱点击事件
					var canReceive = !isReceiveChest && active.Progress >= activeCfg.ActiveNum;
					chestObj.transform.Find("TreasureChest").GetComponent<TreasureChest>().SetData(new TreasureChestData()
					{
						Icon = isReceiveChest ? $"{activeCfg.ChestIcon}Open" : activeCfg.ChestIcon,
						StyleType = TreasureChestStyleType.Reward,
						CanReceive = canReceive,
						Rewards = activeCfg.Rewards,
						OnClick = () =>
						{
							if (!canReceive) return;
							if (isReceiveChest)
							{
								FusionContinent.Toast.Show("已领取");
								return;
							}

							LogTool.Log($"点击领取宝箱：{activeCfg.ActiveNum}");
							ReceiveTaskChestReward(type, activeCfg.ActiveNum);
						}
					});
				}
			}

			// 任务列表
			var taskList = Global.PlayerTasks.Values.Where(v => v.Type == type).ToList();
	
			_prioritySorter.SetData(taskList);
			_prioritySorter.SetScoreCalculator(Calculator);
			_displayedTasks = _prioritySorter.GetSortedList();
			this.TaskScrollView.ShowList(_displayedTasks.Count);
		}

		private int Calculator(PlayerTask arg)
		{
			var v = Global.PlayerTasks[arg.Id];
			int stateScore = arg.Id;
			switch (v.State)
			{
				case TaskState.Progress:
					stateScore += 1000;
					break;
						
				case TaskState.Complete:
					stateScore += 10000;
					break;
				default:
					stateScore += 100;
					// LogTool.Log($"已领取: {v.Id}");
					break;
			}
			return stateScore;
		}


		private async void ReceiveTaskReward(int taskId)
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

				SetData(_currentTaskType);

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
		
		private async void ReceiveTaskChestReward(TaskType type, int activeNum)
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

				SetData(_currentTaskType);

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
		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target,complete);
		}

		protected override void OnAnimationOpen()
		{
			base.OnAnimationOpen();
			if (_buttonMap.TryGetValue(TaskType.Daily, out var btn))
			{
				btn.onClick.Invoke();
			}
		}

		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}
		protected override void OnOpen(IUIData uiData = null)
		{
			this.TaskScrollView.ShowList(0);
			Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, false);
		}

		protected override void OnClose()
		{
			RunAnimation(this.GetAnimationCloseNode, () =>
			{
				UIKit.ClosePanel(this);
			});
		}

		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
			gameObject.SetActive(false);
			Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, true);
			for (int i = 0; i < TaskContent.childCount; i++)
				TaskContent.GetChild(i).gameObject.SetActive(false);
		}
	}
}
