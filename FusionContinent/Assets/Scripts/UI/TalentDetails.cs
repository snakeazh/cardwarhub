using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.Localization;
using Config;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Helper;
using FusionContinent.Model;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.Events;
using Game.CombatPower;
using Game.RedDot;

namespace FusionContinent
{
	public class TalentDetailsData : UIPanelData
	{
	}
	
	
	public partial class TalentDetails : UIPanel
	{
		public Action<bool> TalentUpOver;
		public Transform IsLearn;
		private TbTalentConfig talentConfig;
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as TalentDetailsData ?? new TalentDetailsData();
			// please add init code here
		}
		
		private Queue<GameObject> _rewardEntitys = new Queue<GameObject>();

		private void Awake()
		{
			talentConfig= DataConfigManager.Tables.TbTalentConfig;
		}
		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target,complete);
		}

		protected override void OnAnimationOpen()
		{
			base.OnAnimationOpen();
			UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent,4,activitybtn.GetComponent<RectTransform>());
			UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent,6,activitybtn.GetComponent<RectTransform>());
		}

		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}
		private bool isExtraUpgrade = false;
		public void SetTalentDetails(int talentId,TalentListStruct talentDetails,bool isExtra,int studyId)
		{
			isExtraUpgrade =  isExtra;
			ReleasePool();
			_rewardEntitys.Clear();
			this.gameObject.SetActive(true);
			RunAnimation(GetAnimationOpenNode, OnAnimationOpen);
			// detailsIconbg.sprite = Global.TalentIconAtlas.GetSprite(talentConfig.GetOrDefault(talentId).Icon);
			bool isBuy=true;
			if (!isExtra)
			{
				SpriteHelper.TryLoadTextureAsync(talentDetails.Icon,talentdetailsicon, false, "UITalentIcon");

				// talentdetailsicon.sprite=Global.TalentIconAtlas.GetSprite(talentDetails.Icon);
				talentdetailsname.text = talentDetails.Name;
				talentdetails.text = talentDetails.Desc;
				for (int i = 0; i < talentDetails.ConsumeItems.Count; i++)
				{
					var rewardObj = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity").Get();
					if (rewardObj == null) return;
					_rewardEntitys.Enqueue(rewardObj);
					rewardObj.transform.SetParent(expend);
					rewardObj.transform.localScale = Vector3.one * 1f;
					rewardObj.transform.localPosition = new Vector3(0f, 0f, 0f);
					bool isreward= rewardObj.GetComponent<RewardEntity>().SetData(talentDetails.ConsumeItems[i].Id,
						talentDetails.ConsumeItems[i].Num,RewardStyleType.Consume);
					if (isreward == false) isBuy = false;
				}
			}
			else
			{
				SpriteHelper.TryLoadTextureAsync(talentDetails.ExtraIcon,talentdetailsicon, false, "UITalentIcon");

				// talentdetailsicon.sprite=Global.TalentIconAtlas.GetSprite(talentDetails.ExtraIcon);
				talentdetailsname.text = talentDetails.ExtraName;
				talentdetails.text = talentDetails.ExtraDesc;
				for (int i = 0; i < talentDetails.ExtraConsumeItems.Count; i++)
				{
					var rewardObj = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity").Get();
					if (rewardObj == null) return;

					_rewardEntitys.Enqueue(rewardObj);
					rewardObj.transform.SetParent(expend);
					rewardObj.transform.localScale = Vector3.one * 1f;
					rewardObj.transform.localPosition = new Vector3(0f, 0f, 0f);
					bool isreward=rewardObj.GetComponent<RewardEntity>().SetData(talentDetails.ExtraConsumeItems[i].Id,
						talentDetails.ExtraConsumeItems[i].Num,RewardStyleType.Consume);
					if (isreward == false) isBuy = false;
				}
			}

			switch (studyId)
			{
				case 0:
				{
					activitybtn.gameObject.SetActive(true);
					tip.gameObject.SetActive(false);
					activitybtn.interactable = isBuy;
					// 引导 - 点击激活天赋
					// GuideFlow.Instance.SetStep(GuideType.SetTalent, 3, activitybtn.transform);
					// 引导 - 点击特殊天赋
					// GuideFlow.Instance.SetStep(GuideType.SetSpecialTalent, 3, activitybtn.transform);
					IsLearn.gameObject.SetActive(false);

					activitybtn.onClick.RemoveAllListeners();
					activitybtn.onClick.AddListener(() =>
					{
					
						if (isExtra)
						{
							UnlockGuideManager.Send(ModuleFunctionType.Talent,6);
							//Global.PlayerTalents[talentId].ExtraLv=talentDetails.Level;
							UpGradeTalentPostAsync(talentId, 1);
						}
						else
						{
							UnlockGuideManager.Send(ModuleFunctionType.Talent,4);
							UpGradeTalentPostAsync(talentId, 0);
							//Global.PlayerTalents[talentId].Lv=talentDetails.Level;
						}
						// 计算玩家当前战力（放到最后）
						RunAnimation(GetAnimationCloseNode, OnAnimationClose);

					});
					break;
				}
				case 1:
				{
					activitybtn.gameObject.SetActive(false);
					tip.gameObject.SetActive(true);
					IsLearn.gameObject.SetActive(false);
					//请激活上一级天赋
					tip.GetOrAddComponent<LocalizationText>().SetData(LocalizationModule.UITexts,"UI_TIPS_TALENT_NORMAL");
					break;
				}
				case 4:
				{
					activitybtn.gameObject.SetActive(false);
					tip.gameObject.SetActive(true);
					IsLearn.gameObject.SetActive(false);
					// tip.text = "请先学习上一个特殊天赋";
					tip.GetOrAddComponent<LocalizationText>().SetData(LocalizationModule.UITexts,"UI_TIPS_TALENT_NORMAL_SPECIAL");
					break;
				}
				case 2:
				{
					activitybtn.gameObject.SetActive(false);
					tip.gameObject.SetActive(true);
					IsLearn.gameObject.SetActive(false);
					// tip.text = "货币不足";
					tip.GetOrAddComponent<LocalizationText>().SetData(LocalizationModule.UITexts,"UI_TIPS_TALENT_ITEM_INSUFFICIENT");
					break;
				}
				case 3:
				{
					activitybtn.gameObject.SetActive(false);
					tip.gameObject.SetActive(false);
					IsLearn.gameObject.SetActive(true);
					break;
				}
			}
		}

		public override void Hide()
		{
			ReleasePool();
			base.Hide();
		}

		protected override void OnAnimationClose()
		{
			base.OnAnimationClose();
			// UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent,6,null);
			// UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent,4,null);

		}

		private void Start()
		{
			QuitPanel.onClick.AddListener(() =>
			{
				RunAnimation(GetAnimationCloseNode, OnAnimationClose);
				// GetComponent<UIBottomPopup>().Hide();

			});
		}

		private void ReleasePool()
		{
			var pool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity");
			while (_rewardEntitys.Count > 0)
			{
				pool.Recycle(_rewardEntitys.Dequeue());
			}
		}

		private async Task UpGradeTalentPostAsync(int id, int upgradeExtra)
		{
			try {
				CombatPowerChangeHelper.CaptureBaseline();

				var request = new Cardwar.UpGradeTalentRequest { Id = id, UpgradeExtra = upgradeExtra };
				var proto = await WebDataFun.SendProtoPostRequest<Cardwar.UpGradeTalentResponse>(ApiKeys.ApiUpGradeTalent, request);

				if (proto == null || proto.Talent == null)
				{
					throw new Exception("服务器返回空响应");
				}

				// 更新本地数据
				Global.PlayerTalents[proto.Talent.TalentId] = ProtoMappers.MapPlayerTalent(proto.Talent);

				// 同步奖励
				Items.Update(proto.Items);
				TalentUpOver.Invoke(isExtraUpgrade);
				if (!isExtraUpgrade)
				{
					//GuideFlow.Instance.SetStepComplete(GuideType.SetNormalTalent);
				}
				else
				{
					//GuideFlow.Instance.SetStepComplete(GuideType.SetSpecialTalent);
				}
				CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
				Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE+RedNodeType.TalentUpgradeBtn,null);
				// 完成引导
				// GuideFlow.Instance.SetStepComplete(result.Talent.ExtraLv > 0
				// 	? GuideType.SetSpecialTalent // 完成额外引导
				// 	: GuideType.SetTalent);

			} catch (Exception ex) { 
				LogTool.LogError($"{ex.Message}");
				FusionContinent.Toast.Show(ex.Message);
			}
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
	}
}
