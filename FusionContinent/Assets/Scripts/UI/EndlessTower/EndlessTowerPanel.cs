using System.Collections.Generic;
using Cardwar;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using PlayerEndlessTower = FusionContinent.Model.PlayerEndlessTower;

namespace FusionContinent
{
	public class EndlessTowerPanelData : UIPanelData
	{
	}
	public partial class EndlessTowerPanel : UIPanel
	{
		public static EndlessTowerPanel Default;
		
		private PlayerEndlessTower _tower;
		private readonly Dictionary<int, GameObject> _mapNodes = new Dictionary<int, GameObject>();
		private EndlessTowerConfig _endlessTowerCfg;
		private EndlessTowerMapConfig _towerMapCfg;

		private TbStageBattleConfig _stageBattleConfig;
		private TbEndlessTowerConfig _endlessTowerConfig;
		private TbEndlessTowerMapConfig _endlessTowerMapConfig;
		
		protected void Awake()
		{
			Default = this;
			_stageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
			_endlessTowerConfig = DataConfigManager.Tables.TbEndlessTowerConfig;
			_endlessTowerMapConfig = DataConfigManager.Tables.TbEndlessTowerMapConfig;
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as EndlessTowerPanelData ?? new EndlessTowerPanelData();
			// please add init code here
			BackBtn.onClick.AddListener(OnClose);
			RuleBtn.onClick.AddListener(() =>
			{
				UIKit.OpenPanel<RulesPopupPanel>().SetData($"{_endlessTowerCfg.Name}规则", _endlessTowerCfg.RulesDesc);
			});
			RankingBtn.onClick.AddListener(() =>
			{
				// UIKit.OpenPanel<ChallengeRankingPanel>().SetData(_endlessTowerCfg.RankRewardList, StageType.EndlessTower);
			});
			DailyBtn.onClick.AddListener(() =>
			{
				var canReceive = _tower.DailyRewardList.Count == 0;
				UIKit.OpenPanel<DailyRewardsPanel>().SetData(new DailyRewardsPanelData()
				{
					TitleText = "每日挑战奖励",
					TipText = "每日进行一次挑战可领取",
					CanReceive = _tower.DailyChallengeNum > 0 && canReceive,
					RewardItems = _endlessTowerCfg.DailyChallengeRewards,
					ReceiveBtnText = canReceive ? "领取" : "已领取",
					ReceiveCallback = ReceiveDailyRewards,
				});
			});

			foreach (var cfg in _endlessTowerConfig.DataList)
			{
				if (cfg.Status == ActivityStatusType.Open)
				{
					_endlessTowerCfg = cfg;
					break;
				}
			}
		}

		public void SetData()
		{
			if (_endlessTowerCfg == null)
			{
				FusionContinent.Toast.Show("暂未开放");
				return;
			}

			if (!Global.PlayerEndlessTowers.TryGetValue(_endlessTowerCfg.Id, out _tower))
			{
				return;
			}

			FloorText.text = $"第{_tower.Floor}层";

			_towerMapCfg = _endlessTowerMapConfig.GetOrDefault(_tower.MapId);
			if (_towerMapCfg == null)
			{
				FusionContinent.Toast.Show("地图不存在");
				return;
			}

			// 设置地图
			MapBg.sprite = Global.EndlessTowerIconSpriteAtlas.GetSpriteExt(_towerMapCfg.MapRes);
			for (int i = 0; i < NodeList.childCount; i++)
				NodeList.GetChild(i).gameObject.SetActive(false);
			
			foreach (var v in _towerMapCfg.NodeList)
			{
				if (!_mapNodes.TryGetValue(v.NodeId, out GameObject slotObj))
				{
					slotObj = Instantiate(NodeList.GetChild(0).gameObject, NodeList);
					_mapNodes.Add(v.NodeId, slotObj);
				}
				
				slotObj.SetActive(true);
				slotObj.transform.localPosition = new Vector2(v.NodePos.X, v.NodePos.Y);
				
				// 节点类型
				slotObj.transform.Find("Question").gameObject.SetActive(v.NodeType == MapNodeType.Event);
				slotObj.transform.Find("BossHead").gameObject.SetActive(v.NodeType == MapNodeType.Monster);
				slotObj.transform.Find("NextFloor").gameObject.SetActive(v.NodeType == MapNodeType.NextFloor);
				slotObj.transform.Find("TreasureChest").gameObject.SetActive(v.NodeType == MapNodeType.Chest);

				// 点击事件
				var slotBtn = slotObj.GetComponent<Button>();
				slotBtn.onClick.RemoveAllListeners();
				slotBtn.onClick.AddListener(() =>
				{
					LogTool.Log($"点击节点: {v.NodeId}");
					if (!CheckPreNode(v.NodeId))
					{
						FusionContinent.Toast.Show("请先解锁前置节点");
						return;
					}

					UIKit.OpenPanel<BattleConfirmPanel>().SetData(_tower.TowerId, _towerMapCfg.Id, v.NodeId);
				});

				// 节点状态
				var nodeState = GetNodeState(v.NodeId);
				slotBtn.interactable = nodeState == 0;
				
				// 可挑战节点
				var canChallenge = slotBtn.interactable && CheckPreNode(v.NodeId);
				slotObj.GetComponent<Animator>().enabled = canChallenge;// 动态缩放效果
				
				// 宝箱处理
				if (v.NodeType == MapNodeType.Chest)
				{
					slotBtn.interactable = nodeState < 2;
					slotObj.transform.Find("TreasureChest").GetComponent<TreasureChest>().SetData(new TreasureChestData()
					{
						Icon = nodeState == 2 ? "SilverOpen" : "Silver",
						Rewards = v.Rewards,
						StyleType = TreasureChestStyleType.Reward,
						CanReceive = nodeState == 1,
						IsOnClick = nodeState == 1,
						IsLongOnClick = true,
						OnClick = () =>
						{
							ReceiveChestReward(v.NodeId);
							LogTool.Log($"点击领取宝箱奖励：{v.NodeId}");
						}
					});
				}

				// 设置透明度
				foreach (var img in slotObj.GetComponentsInChildren<Image>())
				{
					var c = img.color;
					c.a = slotBtn.interactable ? 1f : 0.6f;
					img.color = c;
				}
			}
			
			// 每日奖励红点
			DailyBtn.transform.Find("RedPoint").gameObject.SetActive(_tower.DailyChallengeNum > 0 && _tower.DailyRewardList.Count == 0);
		}

		/**
		 * 获取节点状态
		 */
		private int GetNodeState(int nodeId)
		{
			return _tower.ClearNodeList.GetValueOrDefault(nodeId, 0);
		}

		/**
		 * 检查前置节点
		 */
		private bool CheckPreNode(int nodeId, int state = 0)
		{
			var isUnlock = false;
			var preNodes = _towerMapCfg.NodeList[nodeId - 1].PreNone;
			if (preNodes.Count == 0) return true;

			foreach (var preId in preNodes)
			{
				if (GetNodeState(preId) > state)
				{
					isUnlock = true;
					break;
				}
			}
			
			return isUnlock;
		}
		
		private async void ReceiveChestReward(int nodeId)
		{
			try
			{
				var request = new ReceiveTowerChestRewardRequest { TowerId = _tower.TowerId, NodeId = nodeId };
				var proto = await WebDataFun.SendProtoPostRequest<ReceiveTowerChestRewardResponse>(ApiKeys.ApiEndlessTowerReceiveChestReward, request);
				if (proto != null && proto.Tower != null)
				{
					_tower = ProtoMappers.MapPlayerEndlessTower(proto.Tower);
					Global.PlayerEndlessTowers[_tower.TowerId] = _tower;

					// 同步奖励
					Items.Update(proto.Items);

					var rewardsDict = new Dictionary<string, int>();
					if (proto.Rewards != null)
					{
						foreach (var kv in proto.Rewards)
						{
							rewardsDict[kv.Key.ToString()] = (int)kv.Value;
						}
					}

					// 刷新数据
					SetData();

					// 奖励
					// RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
					// rewardTips.SetData(rewardsDict);
				}
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
		
		
		/**
         * 领取每日奖励
         *
         * @return
         * @author Zpp
         * @create 2025/10/30
         **/
		private async void ReceiveDailyRewards()
		{
			if (_tower == null) return;

			try
			{
				var request = new ReceiveDailyRewardRequest { TowerId = _tower.TowerId };
				var proto = await WebDataFun.SendProtoPostRequest<ReceiveDailyRewardResponse>(ApiKeys.ApiEndlessTowerReceiveDailyReward, request);
				if (proto != null && proto.Tower != null)
				{
					_tower = ProtoMappers.MapPlayerEndlessTower(proto.Tower);
					Global.PlayerEndlessTowers[_tower.TowerId] = _tower;

					// 同步奖励
					Items.Update(proto.Items);

					var rewardsDict = new Dictionary<string, int>();
					if (proto.Rewards != null)
					{
						foreach (var kv in proto.Rewards)
						{
							rewardsDict[kv.Key.ToString()] = (int)kv.Value;
						}
					}

					// 奖励
					// RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
					// rewardTips.SetData(rewardsDict);

					// 刷新数据
					SetData();
				}
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

		protected override void OnOpen(IUIData uiData = null)
		{
			SetData();
			Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, false);
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			gameObject.SetActive(false);
			Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
		}
	}
}
