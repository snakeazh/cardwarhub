using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using YooAsset;
using Game.RedDot;

namespace FusionContinent
{
	public class GuideFlowData : UIPanelData
	{
	}
	public partial class GuideFlow : UIPanel
	{
		public static GuideFlow Instance;

		private GuideFlowConfig _currentCfg;
		private TbGuideFlowConfig _guideFlowConfig;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GuideFlowData ?? new GuideFlowData();
		}

		public bool CanReceive()
		{
			if (_currentCfg == null)
			{
				return false;
			}
			var guildStatus = Global.Player.GetGuideStatusById(_currentCfg.Id);
			if (guildStatus == GuideFlowStatus.GuideFlowReceive)
			{
				return true;
			}

			return false;
		}
		Canvas renderCanvas;
		private void Awake()
		{
			Instance = this;
			_guideFlowConfig = DataConfigManager.Tables.TbGuideFlowConfig;
			renderCanvas = transform.Find("Canvas").GetComponent<Canvas>();
			PanelBtn.onClick.AddListener(() =>
			{
				if (_currentCfg == null) return;
				
				var guildStatus = Global.Player.GetGuideStatusById(_currentCfg.Id);
				switch (guildStatus)
				{
					case GuideFlowStatus.GuideFlowReady:
						UIKit.OpenPanel<GuildDetail>(UILevel.Common,new GuildDetailData()
						{
							FlowConfig = _currentCfg,
						});
						// GuildDetail.gameObject.SetActive(!GuildDetail.gameObject.activeSelf);
						// GuildDetail.Find("NameText").GetComponent<TMP_Text>().text = _currentCfg.Name;
						// GuildDetail.Find("Desc/Text").GetComponent<TMP_Text>().text = _currentCfg.Desc;
						//
						// // 奖励
						// var rewardList = GuildDetail.Find("Rewards/Viewport/Content");
						// var rewParent = rewardList.Find("RewardEntity");
						// for (int r = 0; r < _currentCfg.Rewards.Count; r++)
						// {
						// 	var rewChild = r < rewardList.childCount 
						// 		? rewardList.GetChild(r) 
						// 		: Instantiate(rewParent, rewardList);
						// 	rewChild.gameObject.SetActive(true);
						// 	rewChild.GetComponent<RewardEntity>().SetData(_currentCfg.Rewards[r]);
						// }
				
						// SetPointerParent(GuildDetail.Find("SkipBtn"), new Vector2(0, -20f));
						break;
					
					case GuideFlowStatus.GuideFlowReceive:
						LogTool.Log("领取奖励");
						Global.Player.SetGuideFlowsStatus(_currentCfg.Id, RefreshData);
						break;
					
					case GuideFlowStatus.GuideFlowFinished:
						RefreshData();// 已完成的引導避免出現卡主
						break;
						
					default:
						return;
				}
			});
			
			// GuildDetail.Find("Quit").GetComponent<Button>().onClick.AddListener(GuildDetailClose);
			// GuildDetail.Find("SkipBtn").GetComponent<Button>().onClick.AddListener(GuildSkipPath);
			
			// 订阅消息 - 关闭
			Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, (enabledBtn) => {
				if (PointerBtn == null) return;
				// PointerBtn.transform.gameObject.SetActive((bool)enabledBtn);
			});
			
			// 订阅消息 - 关闭
			Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, (enabledBtn) => {
				//if (PointerBtn == null) return;
				if (_currentCfg != null && _currentCfg.ConditionType == GuideConditionType.PlyerLevel)
				{
					var guildStatus = Global.Player.GetGuideStatusById(_currentCfg.Id);
					if (guildStatus == GuideFlowStatus.GuideFlowReady)
					{
						RefreshData();
					}
				}
				// PointerBtn.transform.gameObject.SetActive((bool)enabledBtn);
			});
			// CHANGE_USER_LEVEL_INFO
		}
		
		private void GuildSkipPath()
		{
			if (_currentCfg == null) return;
			
			// GuildDetailClose();
			var skipPath = _currentCfg.StepList.First().SkipPath.First();
			GameMainPanel.Default.GotoSkipPath(skipPath.Type, skipPath.Param);
		}

		private GameObject FingerRes;
		private GameObject FingerInstance;
		public void SetFinger(GameObject target)
		{
			if (FingerInstance != null)
			{
				return;
			}
			if (FingerRes == null)
			{
				FingerRes = YooAssets.LoadAssetSync<GameObject>("Finger").GetAssetObject<GameObject>();
			}
			FingerInstance = FingerRes.InstantiateWithParent(target.transform);
		}

		public void ClearFinger()
		{
			if (FingerInstance != null)
			{
				GameObject.DestroyImmediate(FingerInstance);
				FingerInstance = null;
			}
		}

		public void RefreshData()
		{
			
			return;
			if (PointerBtn == null) return;
			
			_currentCfg = null;
			transform.localScale = Vector3.zero;
			PointerBtn.transform.gameObject.SetActive(false);
			foreach (var cfg in _guideFlowConfig.DataList)
			{
				if (cfg == null) continue;
				// if (cfg.Type == GuideType.GuideStage) continue;
				// if (cfg.Type == GuideType.GuideStage) continue;
				var guildStatus = Global.Player.GetGuideStatusById(cfg.Id);
				if (guildStatus == GuideFlowStatus.GuideFlowFinished || cfg.AppearLv > Global.User.Level) continue;
				
				// 前置引导
				if (cfg.PreId > 0)
				{
					var preStatus = Global.Player.GetGuideStatusById(cfg.PreId);
					if (preStatus != GuideFlowStatus.GuideFlowFinished) continue;
				}

				_currentCfg = cfg;
				transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
				// if (PointerBtn.transform.parent.Equals(transform))
				// {
				// 	PointerBtn.transform.gameObject.SetActive(false);
				// }
				// PointerBtn.transform.gameObject.SetActive(guildStatus == GuideFlowStatus.GuideFlowReady);
				PanelBtn.transform.Find("Text").GetComponent<TMP_Text>().text = $"{cfg.Name}";

				string conditionStr = "white";
				if ((int)guildStatus <1)
				{
					conditionStr = "red";
				}
				
				PanelBtn.transform.Find("ConditionText").GetComponent<TMP_Text>().text = $"(<color={conditionStr}>{(int)guildStatus}</color>/1)";
				PanelBtn.transform.Find("RewardEntity").GetComponent<RewardEntity>().SetData(_currentCfg.Rewards[0]);
				PanelBtn.transform.Find("RedPoint").gameObject.SetActive(guildStatus == GuideFlowStatus.GuideFlowReceive);
				CheckConditionConfig(_currentCfg);
				break;
			}
			// GuildDetailClose();
		}

		private void CheckConditionConfig(GuideFlowConfig cfg)
		{
			if (cfg == null || cfg.ConditionType == GuideConditionType.None)
			{
				return;
			}
			bool conditionDone = false;
			switch (cfg.ConditionType)
			{
				case GuideConditionType.Stage:
					try
					{
						int conditionStageId = int.Parse(cfg.StepList.First().SkipPath.First().Param);
						if (Global.PlayerStages.TryGetValue(conditionStageId, out var stageInfo))
						{
							if (stageInfo.Status > 0)
							{
								conditionDone = true;
							}
						}
					}
					catch (Exception e)
					{
						LogTool.E(e.Message);
					}
					// Global.StageId
					break;
				case GuideConditionType.PlyerLevel:
					try
					{
						int conditionStageId = int.Parse(cfg.StepList.First().SkipPath.First().Param);
						conditionDone = Global.User.Level >= conditionStageId;
						
					}
					catch (Exception e)
					{
						LogTool.E(e.Message);
					}
					break;
				case GuideConditionType.NumOfSkillUsed:
					conditionDone = CheckHeroSkillCount();
					break;
				case GuideConditionType.BuyTheHero:
					int heroId = int.Parse(cfg.StepList.First().SkipPath.First().Param);
					conditionDone = CheckContainsHero(heroId);
					break;
			}

			if (conditionDone)
			{
				SetStepComplete(cfg.Type);
			}
		}

		bool CheckContainsHero(int heroId)
		{
			if (Global.PlayerCards.TryGetValue(heroId, out var card))
			{
				return true;
			}
			return false;
		}
		
		bool CheckHeroSkillCount()
		{
			Dictionary<int,int>  heroSkillCount = new Dictionary<int, int>();
			// 初始化数据
			foreach (var cfg in DataConfigManager.Tables.TbCardBaseConfig.DataList)
			{
				switch (cfg.Type)
				{
					case CardType.Skill:
					case CardType.SkillPassive:
						var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cfg.Id);
						// _soldierSkillList.TryAdd(skillCfg.ActsOnCard, new List<PlayerCard>());
						// _soldierSkillList[skillCfg.ActsOnCard].Add(cardInfo);

						if (Global.PlayerCards.TryGetValue(cfg.Id, out var playerCard))
						{
							if (playerCard.IsHave)
							{
								if (heroSkillCount.ContainsKey(skillCfg.ActsOnCard))
								{
									heroSkillCount[skillCfg.ActsOnCard]++;
								}
								else
								{
									heroSkillCount.Add(skillCfg.ActsOnCard, 1);
								}
							}

						}
						break;
				}
			}

			foreach (var item in heroSkillCount)
			{
				if (item.Value >= 5)
				{
					return true;
				}
			}
			return false;
		}

		/**
		 * 关闭引导步骤
		 */
		private void CloseStep()
		{
			if (PointerBtn == null) return;
			PointerBtn.transform.SetParent(transform);
			PointerBtn.transform.gameObject.SetActive(false);
			SetDepth(PointerBtn.gameObject,transform.Find("Canvas").GetComponent<Canvas>());
		}

		private const bool EnableFinger = false;
		/**
		 * 设置引导步骤
		 */
		public void SetStep(GuideType type, int stepId, Transform parent, string param = "")
		{
			if (!CheckStep(type)) return;
			if (parent == null)
			{
				PointerBtn.gameObject.SetActive(false);
				return;
			}
			var stepCfg = GetCurrentStepCfg(type, stepId);
			if (param != "" && stepCfg.SkipPath.Count > 0 && stepCfg.SkipPath.First().Param != param || stepCfg.DialogType == GuideDialogType.None) return;
			PointerBtn.gameObject.SetActive(true && EnableFinger);
			lastFlowStep = stepCfg;
			var localPosition = new Vector3(stepCfg.DialogOffset.X, stepCfg.DialogOffset.Y, stepCfg.DialogOffset.Z);
			SetPointerParent(parent, localPosition, stepCfg.DialogScale, false);
		}

		public void CloseStepDone(GuideType type, int stepId)
		{
			if (_currentCfg == null || type != _currentCfg.Type) return;
			if (_currentCfg.Type == type)
			{
				if (lastFlowStep != null && lastFlowStep.StepId == stepId)
				{
					PointerBtn.gameObject.SetActive(false);
					lastFlowStep = null;
				}
			}
		}

		private GuideFlowStepListStruct lastFlowStep;
		
		/**
		 * 设置引导步骤完成
		 */
		public void SetStepComplete(GuideType type)
		{
			if (!CheckStep(type)) return;
			Global.Player.SetGuideFlowsStatus(_currentCfg.Id, CloseStepAndRefresh);// 完成引导
		}

		void CloseStepAndRefresh()
		{
			CloseStep();
			RefreshData();
		}
		
		/**
		 * 设置引导步骤完成检查参数
		 */
		public void SetStepCompleteFirstStep(GuideType type,int stepId,int param)
		{
			if (!CheckStep(type)) return;
			bool stepCheck = false;
			
			if (_currentCfg.StepList.Count >= stepId && _currentCfg.StepList[stepId]!= null && _currentCfg.StepList[stepId].SkipPath.Count > 0)
			{
				if (_currentCfg.StepList[stepId].SkipPath[0].Param.Equals(param.ToString()))
				{
					stepCheck = true;
				}
			}
			if (!stepCheck) return;
			Global.Player.SetGuideFlowsStatus(_currentCfg.Id, CloseStepAndRefresh);// 完成引导
		}
		
		
		private void SetPointerParent(Transform parent, Vector3 localPosition = default, float localScale = 0, bool cover = true)
		{
			if (PointerBtn == null || !parent.gameObject.activeSelf) return;
			
			PointerBtn.transform.SetParent(parent);
			PointerBtn.transform.localPosition = Vector3.one;
			PointerBtn.transform.localScale = localScale > 0 ? new Vector2(localScale, localScale) : Vector2.one;
			SetDepth(PointerBtn.gameObject,parent == transform? renderCanvas:null);
			if (localPosition != default)
			{
				PointerBtn.transform.localPosition = cover ? localPosition : new Vector3(
					PointerBtn.transform.localPosition.x + localPosition.x, 
					PointerBtn.transform.localPosition.y + localPosition.y,
					PointerBtn.transform.localPosition.z + localPosition.z
				);
			}
		}

		void SetDepth(GameObject finger,Canvas target = null)
		{
			var cv =target ? target : finger.GetComponentInParent<Canvas>();
			
			
			var psrs = finger.GetComponentsInChildren<ParticleSystemRenderer>();
			int j = 1;
			for (int i = psrs.Length - 1; i >= 0; i--)
			{
				psrs[i].sortingOrder = cv.sortingOrder  +j++;
			}
		}

		private bool CheckStep(GuideType type)
		{
			if (_currentCfg == null || type != _currentCfg.Type) return false;
			return Global.Player.GetGuideStatusById(_currentCfg.Id) == GuideFlowStatus.GuideFlowReady;
		}
		
		/**
		 * 获取当前步骤配置
		 */
		public GuideFlowStepListStruct GetCurrentStepCfg(GuideType type, int stepId)
		{
			if (_currentCfg == null) return null;
			return _currentCfg.Type != type || _currentCfg.StepList.Count < stepId ? null : _currentCfg.StepList[stepId - 1];
		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}

		private void GuildDetailClose()
		{
			// GuildDetail.gameObject.SetActive(false);
			// 	
			// var rewardList = GuildDetail.Find("Rewards/Viewport/Content");
			// for (int i = 0; i < rewardList.childCount; i++)
			// {
			// 	rewardList.GetChild(i).gameObject.SetActive(false);
			// }
			//
			// SetPointerParent(transform, new Vector2(220f, -50f));
		}
		
		
	}
}
