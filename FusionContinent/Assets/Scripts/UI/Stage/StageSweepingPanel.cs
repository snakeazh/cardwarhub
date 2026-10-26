using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Model;
using FusionContinent.Utils;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Events;
using UIManager = UIFramework.UIManager;
using Game.RedDot;

namespace FusionContinent
{
	public class StageSweepingPanelData : UIPanelData
	{
		public int currentOpenId;
	}
	public partial class StageSweepingPanel : UIPanel
	{
		private float _afkTime;// 挂机时长
		
		private TbItemConfig _itemConfig;
		private TbRewardGroupConfig _rewardGroupConfig;
		private TbStageBattleConfig _stageBattleConfig;
		private TbChapterStageConfig _chapterStageConfig;
		private TbSoldierBattleConfig _soldierBattleConfig;

		private int _guideStageId;
		private int _afkTimeProduce;
		private int _afkTimeLimitMax;
		private int _dailyStageSweepingNum;
		private bool _isClosing;
		private bool _runDailyPopupOnHide = true;
		private UnityAction _onPanelHidden;

		protected void Awake()
		{
			_itemConfig = DataConfigManager.Tables.TbItemConfig;
			_rewardGroupConfig = DataConfigManager.Tables.TbRewardGroupConfig;
			_stageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
			_chapterStageConfig = DataConfigManager.Tables.TbChapterStageConfig;
			_soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;

			_guideStageId = DataConfigManager.Tables.TbPlayerConst.GuidesStageId;
			_afkTimeProduce = DataConfigManager.Tables.TbGameBattleConst.AfkTimeProduce;
			_afkTimeLimitMax = DataConfigManager.Tables.TbGameBattleConst.AfkTimeLimitMax;
			_dailyStageSweepingNum = DataConfigManager.Tables.TbGameBattleConst.DailyStageSweepingNum;
		}

		// private int currentOpenId;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as StageSweepingPanelData ?? new StageSweepingPanelData();
			Mask.onClick.AddListener(RequestClosePanel);
			ExitBtn.onClick.AddListener(RequestClosePanel);
			ReceiveBtn.SetRedType(RedNodeType.AFKPageClaimBtn);
			ReceiveBtn.onClick.AddListener(ReceiveAfkRewards);
			HarvestBtn.onClick.AddListener(HarvestSweepingRewards);

		}

		private void RequestClosePanel()
		{
			_runDailyPopupOnHide = true;
			PlayCloseAnimation(HidePanelInternal);
		}

		private void PlayCloseAnimation(UnityAction onComplete)
		{
			if (_isClosing) return;

			_onPanelHidden = () =>
			{
				var callback = onComplete;
				onComplete = null;
				callback?.Invoke();
			};
			Hide();
		}

		private void SetData()
		{
			var stageCfg = _stageBattleConfig.GetOrDefault(mData.currentOpenId);
			StageName.Find("Text").GetComponent<TMP_Text>().text = stageCfg.StageName;
			
			// 挂机时长
			_afkTime = BattleModel.GetAfkDuration();
			if (_afkTime > _afkTimeLimitMax) _afkTime = _afkTimeLimitMax;
			// Sweeping.Find("SweepingDesc").GetComponent<TMP_Text>().text = $"最长累计{_afkTimeLimitMax / 3600}小时，关卡越高，收益越大";
			MaxAFKTextDes.GetComponent<TMP_Text>().text = $"最大挂机{_afkTimeLimitMax / 3600}小时";
			SetSweepingTime();
			
			// 扫荡次数
			var sweepingNum = GetSweepingNum();
			var sweepingNumText = sweepingNum < 0 ? "无限" : sweepingNum.ToString();
			HarvestBtn.transform.Find("HarvestNum").GetComponent<TMP_Text>().text = $"快速收获次数：{sweepingNumText}次";
			HarvestBtn.interactable = sweepingNum != 0;
			
			// 挂机奖励
			ShowAfkRewards();
		}

		private void SetSweepingTime()
		{
			var produceNum = Mathf.FloorToInt(_afkTime / _afkTimeProduce);
			// SweepingProgressBar.Find("Progress").GetComponent<Image>().fillAmount = Mathf.Clamp01(_afkTime / _afkTimeLimitMax);
			// SweepingProgressBar.Find("Progress/Text").GetComponent<TMP_Text>().text = $"{TimeTool.FormatDurationWithYm((int)_afkTime)}";
			SweepingProgress.GetComponent<Slider>().value = Mathf.Clamp01(_afkTime / _afkTimeLimitMax);
			SweepingTime.GetChild(0).GetComponent<TMP_Text>().text = $"{TimeTool.FormatDurationWithYm((int)_afkTime)}";
			ReceiveBtn.interactable = produceNum > 0;
		}

		private void ShowAfkRewards()
		{
			var stageCfg = _stageBattleConfig.GetOrDefault(Global.PlayerBattle.ClearStageId);
			if (stageCfg == null || stageCfg.Id == _guideStageId) return;

			var chapterCfg = _chapterStageConfig.GetOrDefault(stageCfg.ChapterId);
			if (chapterCfg.AfkRewards.Count == 0 && chapterCfg.AfkRewardGroups.Count == 0) return;
			
			// 遍历奖励
			var produceNum = Mathf.FloorToInt(_afkTime / _afkTimeProduce);
			var rewards = RewardModel.ToRewardItems(chapterCfg.AfkRewards, produceNum);
			// for (int i = 0; i < SweepingRewards.childCount; i++)
			// {
			// 	var rewObj = SweepingRewards.GetChild(i).gameObject;
			// 	rewObj.SetActive(chapterCfg.AfkRewards.Count > i);
			// 	if (!rewObj.activeSelf) continue; 
			// 	
			// 	var itemCfg = _itemConfig.GetOrDefault(chapterCfg.AfkRewards[i].Id);
			// 	rewObj.transform.Find("Icon").GetComponent<Image>().sprite = Global.ItemIconSpriteAtlas.GetSprite(itemCfg.Icon);
			// 	rewObj.transform.Find("Num").GetComponent<TMP_Text>().text = $"{chapterCfg.AfkRewards[i].Num}/{TimeTool.FormatDurationWithName(_afkTimeProduce)}";
			// }

			foreach (var group in chapterCfg.AfkRewardGroups)
			{
				var groupCfg = _rewardGroupConfig.GetOrDefault(group.Id);
				if (groupCfg == null) continue;

				rewards.AddRange(groupCfg.RewardGroupList.Select(rew => new RewardItem(rew.ItemId, rew.ItemNum * produceNum)));
			}
			
			var listScroll = RewardScroll.transform.Find("Viewport/Content");
			// 所有奖励（含金币）都作为普通节点显示，列表索引与 Content 子节点一一对应。
			for (int r = 0; r < rewards.Count; r++)
			{
				RewardItem item = rewards[r];
				GameObject rewObj = r >= listScroll.childCount
					? Instantiate(RewardEntity.gameObject, listScroll, false)
					: listScroll.GetChild(r).gameObject;

				rewObj.gameObject.SetActive(produceNum > 0);
				rewObj.GetComponent<RewardEntity>().SetData(item);
			}
			// 清理本次未使用到的旧节点，避免重复打开面板时残留上一轮的数据
			for (int i = rewards.Count; i < listScroll.childCount; i++)
			{
				listScroll.GetChild(i).gameObject.SetActive(false);
			}
		}

		private int GetSweepingNum()
		{
			// 月卡未过期，无限扫荡次数
			if (!UserCreatePlayerActivity.CheckMonthCardExpired())
			{
				return -1;
			}

			var sweepingNum = _dailyStageSweepingNum - Global.PlayerBattle.DailyStageSweepingNum;
			return sweepingNum < 0 ? 0 : sweepingNum;
		}

		/**
		 * 领取挂机奖励
		 */
		private async void ReceiveAfkRewards()
		{
			// try
			// {
			// 	var formData = new Dictionary<string, string>{};
			// 	var response = await WebDataFun.SendPostRequest(ApiKeys.ApiBattleChapterAfkRewards, formData);
			// 	if (!string.IsNullOrEmpty(response))
			// 	{
			// 		var result = JsonConvert.DeserializeObject<BattleResult>(response);
			// 		// 更新数据
			// 		Global.PlayerBattle = result.Battle;
			//
			// 		// 同步奖励
			// 		Items.Update(result.Items);
			//
			// 		// 奖励
			// 		RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
			// 		rewardTips.SetData(result.Rewards);
			// 		
			// 		DOTween.To(v => { }, 0, 0, 0.2f).onComplete += () =>
			// 		{
			// 			SetData();
			// 			// 刷新父级红点
			// 			MainInterfacePanel.Default.RefreshRedPoint();
			// 		};
			// 		ClosePanel();
			// 	}
			// }
			// catch (System.Exception ex)
			// {
			// 	LogTool.LogError($"领取奖励失败：{ex.Message}");
			// 	FusionContinent.Toast.Show(ex.Message);
			// }
		}

		/**
		 * 收取扫荡奖励
		 */
		private async void HarvestSweepingRewards()
		{
			try
			{

				BattleModel.Sweep(Global.PlayerBattle.ClearStageId, () =>
				{
					DOTween.To(v => { }, 0, 0, 0.2f).onComplete += SetData;
				});

			}
			catch (System.Exception ex)
			{
				LogTool.LogError($"领取奖励失败：{ex.Message}");
				FusionContinent.Toast.Show(ex.Message);
			}
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			_isClosing = false;
			_runDailyPopupOnHide = true;
			_onPanelHidden = null;
			SetData();
			var openNode = GetAnimationOpenNode;
			if (openNode != null)
			{
				RunAnimation(openNode, null);
			}
		}


		protected void Update()
		{
			if (_afkTime >= 0f && _afkTime < _afkTimeLimitMax)
			{
				_afkTime += Time.deltaTime;
				SetSweepingTime();
			}
		}

		public override void Hide()
		{
			if (_isClosing)
			{
				base.Hide();
				return;
			}

			var closeNode = GetAnimationCloseNode;
			if (closeNode != null)
			{
				_isClosing = true;
				RunAnimation(closeNode, HidePanelInternal);
				return;
			}

			HidePanelInternal();
		}

		
		protected override void OnHide()
		{
			_isClosing = false;
			if (_runDailyPopupOnHide)
			{

			}

			var callback = _onPanelHidden;
			_onPanelHidden = null;
			callback?.Invoke();
		}
		
		protected override void OnClose()
		{
		}

		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target,complete);
		}

		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}

		private void HidePanelInternal()
		{
			var listScroll = RewardScroll.transform.Find("Viewport/Content");
			for (int r = 0; r < listScroll.childCount; r++)
			{
				listScroll.GetChild(r).gameObject.SetActive(false);
			}

			base.Hide();
		}

		private void ClosePanel()
		{
			HidePanelInternal();
		}
	}
}
