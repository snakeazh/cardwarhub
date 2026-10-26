using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Helper;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Events;
using Cardwar;
using FusionContinent.Utils;
using Game.CombatPower;
using Game.RedDot;
using PlayerCard = FusionContinent.Model.PlayerCard;
using RewardAndNum = Config.RewardAndNum;

namespace FusionContinent
{
	public class GeneralSkillUpGradePanelData : UIPanelData
	{
	}
	public partial class GeneralSkillUpGradePanel : UIPanel
	{
		[SerializeField] private GeneralCardSlot GeneralCardSlot;

			private PlayerCard _playerSkill;

		// private CardDevelopConfig _devCfg;
		private SpecialEffectsConfig _skillCfg;

		private TbCardBaseConfig _cardBaseConfig;
		// private TbCardDevelopConfig _cardDevelopConfig;
		private TbSpecialEffectsConfig _specialEffectsConfig;

		private void Awake()
		{
			_cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
			// _cardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
			_specialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GeneralSkillUpGradePanelData ?? new GeneralSkillUpGradePanelData();
			Mask.onClick.AddListener(OnClose);
			// 去佩戴：不再支持「上阵」，隐藏上阵按钮（prefab 节点保留，仅运行时隐藏）
			UpLoadBtn.gameObject.SetActive(false);
			// ConfirmUpGradeBtn.onClick.AddListener(ConfirmUpGrade);
		}

		public void SetData(int skillId)
		{
			var cfg = _cardBaseConfig.GetOrDefault(skillId);
			// _devCfg = _cardDevelopConfig.GetOrDefault(skillId);
			_skillCfg = _specialEffectsConfig.GetOrDefault(skillId);
			// if (_devCfg == null || _skillCfg == null) return;
			if (_skillCfg == null) return;

			if (!Global.PlayerCards.TryGetValue(skillId, out _playerSkill))
			{
				_playerSkill = new PlayerCard()
				{
					Lv = 1,
					IsHave = false,
					Config = cfg,
					CardId = cfg.Id,
					ObjData = new CardDataStruct(),
				};
			}

			SetUpGradeBody();
		}

		/**
		 * 设置升级内容
		 *
		 * @return
		 * @author Zpp
		 * @create 2025/12/12
		 **/
		void SetUpGradeBody()
		{
			// if (_devCfg == null || _skillCfg == null) return;
			if (_skillCfg == null) return;

			SetCardSlot(GeneralCardSlot, _playerSkill);

			// var devLevelCfg = _devCfg.LevelPropertyList[_playerSkill.Lv - 1];
			var skillLevelCfg = _skillCfg.LevelPropertyList[_playerSkill.Lv - 1];
			var text = DataConfigManager.Tables.TbGameBattleConst.QualityTypeList[(int)_skillCfg.LevelPropertyList.First().Quality];
			CardLevelText.SetData(LocalizationModule.UITexts,  text);

			// 描述
			var attrText = GetLevelAttrText2(skillLevelCfg.Desc, skillLevelCfg.Attributes);
			DescText.SetData(LocalizationModule.UITexts, _playerSkill.Config.Desc, attrText);

			// 下一等级
			// if (_playerSkill.Lv >= _devCfg.LevelPropertyList.Count)
			// {
			// 	// 满级
			// 	NextLevelText.gameObject.SetActive(false);
			// 	NextLevelDescText.SetData(LocalizationModule.UITexts, "UI_TITLE_NEXT_PHASE_MAX");
			// }
			// else
			// {
			// 	var devNextLevelCfg = _devCfg.LevelPropertyList[_playerSkill.Lv];
			// 	NextLevelText.SetData(LocalizationModule.UITexts, devNextLevelCfg.Name);
			// 	NextLevelDescText.SetData(LocalizationModule.UITexts, devNextLevelCfg.UpGradeEffectsDesc.FirstOrDefault());
			// }

			// 判断下一等级
			// if (!_playerSkill.IsHave || _playerSkill.Lv >= _skillCfg.LevelPropertyList.Count)
			// {
			// 	// 满级
			// 	Consumes.gameObject.SetActive(false);
			// 	ConfirmUpGradeBtn.gameObject.SetActive(false);
			// }
			// else
			// {
				// ConfirmUpGradeBtn.gameObject.SetActive(true);

				// 设置消耗
				// Consumes.gameObject.SetActive(true);
				// SetConsumes(devLevelCfg.ConsumeItems);

				// 升级按钮
				// ConfirmUpGradeBtn.interactable = true;
				// ConfirmUpGradeBtn.GetComponent<LocalRedDot>().SetRed(CheckConsumes() ? 1 : 0);
			// }

		}

		/**
		 * 设置底部内容
		 *
		 * @return
		 * @author Zpp
		 * @create 2025/12/12
		 **/
		// void SetConsumes(List<RewardAndNum> consumeItems, bool isUpPhase = false)
		// {
		// 	// 消耗
		// 	for (int i = 0; i < Consumes.childCount; i++)
		// 	{
		// 		var consumesTran = Consumes.GetChild(i);
		// 		consumesTran.gameObject.SetActive(i < consumeItems.Count);
		// 		if (!consumesTran.gameObject.activeSelf) continue;
		//
		// 		var consumesItem = consumeItems[i];
		// 		consumesTran.GetComponent<RewardEntity>().SetData(consumesItem, RewardStyleType.Consume);
		// 	}
		//
		// }

		// bool CheckConsumes()
		// {
		// 	for (int i = 0; i < Consumes.childCount; i++)
		// 	{
		// 		var consumesTran = Consumes.GetChild(i);
		// 		if (!consumesTran.gameObject.activeSelf) continue;
		// 		if (!consumesTran.GetComponent<RewardEntity>().isEnough)
		// 			return false;
		// 	}
		//
		// 	return true;
		// }

		string GetLevelAttrText2(string descText, List<AttributeStruct> attributes)
		{
			for (int i = 1; i <= attributes.Count; i++)
			{
				var attr = attributes[i - 1];
				var val = attr.ValueType == ValueType.Percentage ? attr.Value * 100 : attr.Value;
				if (attr.Type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage) {
					val = attr.Value * 100;
				}
				descText = descText.Replace("{Attribute" + i + "}", $"{val}");
			}

			return descText;
		}

		/**
		 * 设置卡牌数据
		 *
		 * @return
		 * @author Zpp
		 * @create 2025/12/11
		 **/
		private void SetCardSlot(GeneralCardSlot cardSlot, PlayerCard playerSkill, int num = -1)
		{
			if (playerSkill == null) return;

			// 设置卡数据
			var skillCfg = _specialEffectsConfig.GetOrDefault(playerSkill.CardId);
			var cardData = new GeneralCardSlotData()
			{
				CardId = playerSkill.CardId,
				NameText = playerSkill.Config.Name,
				LevelText = $"{playerSkill.Lv}",
				Desc = playerSkill.Config.Desc,
				State = playerSkill.Config.State,
				IsUnlock = playerSkill.IsHave,
				QualityType = skillCfg.LevelPropertyList[playerSkill.Lv - 1].Quality,
				Num = num >= 0 ? num : playerSkill.Num,
			};
			switch (playerSkill.Config.Type)
			{
				case CardType.GeneralSkill:
					cardData.IconName = playerSkill.Config.Icon;
					cardData.IconScale = Vector3.one * 1.1f;
					break;

				default:
					cardData.Icon = Global.CardIconSpriteAtlas.GetSpriteExt(playerSkill.Config.Icon);
					break;
			}

			cardSlot.SetData(cardData);
		}

		// void ConfirmUpGrade()
		// {
		// 	if (!CheckConsumes())
		// 	{
		// 		FusionContinent.Toast.Show(LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_TIPS_TALENT_ITEM_INSUFFICIENT"));
		// 		return;
		// 	}

			// var title = LocalizationManager.Instance.GetText(LocalizationModule.UITexts,
			// 	UpGrade.gameObject.activeSelf ? "UI_BTN_UP_GRADE" : "UI_BTN_ADVANCED");
			//
			// var devLevelCfg = _devCfg.LevelPropertyList[_playerSkill.Lv - 1];
			// var confirmMessage = LocalizationManager.Instance.GetText(LocalizationModule.UITexts,
			// 	"UI_TIPS_WHETHER_TO_CONSUME", Items.GetRewardsStr(devLevelCfg.ConsumeItems), title);
			// confirmMessage += $"\n {devLevelCfg.Name} >> <color=#AE57B0> {_devCfg.LevelPropertyList[_playerSkill.Lv].Name}</color>";
			// GameMainPanel.Default.ShowConfirm(
		 //        LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_TITLE_CONFIRM_PARAM", title)
			// 	, confirmMessage, SkillUpgradeServer);
		// }

		/**
		 * 升级接口
		 *
		 * @return
		 * @author Zpp
		 * @create 2025/12/12
		 **/
		private async void SkillUpgradeServer()
	    {
		    if (_playerSkill == null) return;

	        try
	        {
	            CombatPowerChangeHelper.CaptureBaseline();

	            var request = new Cardwar.LevelUpCardRequest { CardId = _playerSkill.CardId, CardLv = _playerSkill.Lv + 1 };
	            var proto = await WebDataFun.SendProtoPostRequest<Cardwar.LevelUpCardResponse>(ApiKeys.ApiCardLevelUp, request);
	            if (proto != null && proto.Card != null)
	            {
	                _playerSkill = new PlayerCard(FusionContinent.Utils.ProtoMappers.MapPlayerCard(proto.Card));
	                Global.PlayerCards[_playerSkill.CardId] = _playerSkill;

	                // 同步奖励
	                Items.Update(proto.Items);

	                // 刷新显示
	                if (UpGrade.gameObject.activeSelf) SetUpGradeBody();

	                // 升级特效
	                SkillUpgradeEffect01.gameObject.SetActive(true);
	                DOVirtual.DelayedCall(2f, () => SkillUpgradeEffect01.gameObject.SetActive(false));

	                // 奖励

	                // 计算玩家当前战力（放到最后）
	                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();

	                // 刷新父级列表
	                Global.Publisher.Publish(MessagePublisher.REFRESH_SOLDIER_ARCANE_LIST, null);

	                SoundManager.Instance.PlayVoice(SoundType.CardSkillUp);
	                // 完成引导
	                //GuideFlow.Instance.SetStepComplete(GuideType.CardSkillUpgrade);
	            }
	        }
	        catch (System.Exception ex)
	        {
	            LogTool.LogError($"升级失败：{ex.Message}");
	            FusionContinent.Toast.Show(ex.Message);
	        }
	    }


		protected override void OnShow()
		{
		}

		protected override void OnHide()
		{
		}

		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target, complete);
		}

		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}

		protected override void OnClose()
		{
			RunAnimation(this.GetAnimationCloseNode, () =>
			{
				UIKit.ClosePanel(this);
			});
		}
	}
}
