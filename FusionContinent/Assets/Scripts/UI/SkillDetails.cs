using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using DG.Tweening;
using FusionContinent.Model;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
	public class SkillDetailsData : UIPanelData
	{
	}
	public partial class SkillDetails : UIPanel
	{
		private TbSchoolCardConfig _schoolConfig;
		private TbCardBaseConfig _cardBaseConfig;

		private GameObject _cardObj;
		
		private void Awake()
		{
			if (_schoolConfig != null) return;

			_schoolConfig = DataConfigManager.Tables.TbSchoolCardConfig;
			_cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
		}
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SkillDetailsData ?? new SkillDetailsData();
			// please add init code here
		}
		
		public void SetData(int cardId, bool isNormalAttack = false)
		{
			Awake();
			
			var cardCfg = _cardBaseConfig.GetOrDefault(cardId);
			var cardInfo = Global.GetCardById(cardId);
			if (isNormalAttack)
			{
				cardInfo = new PlayerCard
				{
					Lv = 1
				};
			}

			if (cardCfg == null || cardInfo == null) return;

			gameObject.SetActive(true);

			Transform cardSlot = Head.Find("CardSlot");
			Spine.gameObject.SetActive(false);
			cardSlot.gameObject.SetActive(false);
			// 普通攻击显示英雄Spine
			var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(cardCfg.Id);
			var quality = skillCfg
				.LevelPropertyList[cardInfo.Lv-1].Quality;
			

			if (skillCfg != null && isNormalAttack)
			{
				// Spine
                for (int i = 0; i < Spine.childCount; i++)
                	Destroy(Spine.GetChild(i).gameObject);

                Spine.gameObject.SetActive(true);
                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(skillCfg.ActsOnCard);
                if (soldierCfg != null && soldierCfg.Spine != "")
                {
                	var soliderSpine = Global.ResLoader.LoadSync<GameObject>(soldierCfg.Spine).Instantiate().transform;
                	soliderSpine.SetParent(Spine);
                	soliderSpine.localScale = Vector3.one;
                	soliderSpine.localPosition = Vector3.zero;
                	Spine.localScale = soldierCfg.SpineScale > 0 ? Vector3.one * soldierCfg.SpineScale : Vector3.one;
                }
			}
			else
			{
				cardSlot.gameObject.SetActive(true);
				var existingCardInfo = cardSlot.Find("CardInfo");
				if (existingCardInfo != null)
				{
					_cardObj = existingCardInfo.gameObject;
				}
				else if (GameLand.Default != null && GameLand.Default.cardInfoObj != null)
				{
					_cardObj = Instantiate(GameLand.Default.cardInfoObj.gameObject, cardSlot, true);
				}
				else
				{
					LogTool.LogError("[SkillDetails] 无法创建技能卡预览：cardSlot 下无 CardInfo，且 GameLand.cardInfoObj 未绑定");
					return;
				}
				_cardObj.SetActive(true);
				_cardObj.name = "CardInfo";
				_cardObj.transform.position = cardSlot.position;
				_cardObj.LocalScale(Vector3.one);
			
				// 设置信息
				var canvas = _cardObj.transform.Find("Canvas");
				canvas.gameObject.layer = LayerMask.NameToLayer("UI");
				canvas.GetComponent<Canvas>().sortingOrder = 901;
				UpdateCardInfoQuality(quality,_cardObj.gameObject);
				canvas.Find("night").localScale = Vector3.zero;
				canvas.Find("cooling").localScale = Vector3.zero;
				canvas.Find("CardName").GetComponent<TMP_Text>().text = $"{cardCfg.Name}";
				canvas.Find("Painting").GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(cardCfg.Icon);
				canvas.Find("PaintingBg").GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(cardCfg.Icon);
				canvas.Find("Level/LevelText").GetComponent<Text>().text = $"Lv {cardInfo.Lv}";
				Head.Find("DescText").GetComponent<TMP_Text>().text = cardCfg.Desc;
			}

			// 星级列表
			// SetSkillStarList(cardId);
			
            // 技能属性
            SetSkillAttributes(cardId);
		}

		public void UpdateCardInfoQuality(QualityType qualityType,GameObject cardObj)
		{
			SkillCardUiHelper.ApplyQualityFrame(cardObj.transform, qualityType);
			var bgIcon = Global.CardStyleIconSpriteAtlas.GetSprite(qualityType.ToString());
			if (bgIcon == null)
			{
				bgIcon = Global.CardStyleIconSpriteAtlas.GetSprite(QualityType.Ordinary.ToString());
			}
			cardObj.transform.Find("Canvas/Bg").GetComponent<Image>().sprite =
				bgIcon;
			if (qualityType != QualityType.Default)
			{
				var effectParent = cardObj.transform.Find("Canvas/Effect");
				var effectName = $"SkillCard{qualityType.ToString()}01";
				if (effectParent.Find(effectName) == null)
				{
					for (int i = 0; i < effectParent.childCount; i++)
						effectParent.GetChild(i).gameObject.SetActive(false);
                            
					var pool = CommonItemPoolManager.Instance.GetGameObjectPool(effectName);
					if (pool != null)
					{
						GameObject skillEffect = pool.Get();
						skillEffect.name = effectName;
						skillEffect.gameObject.SetActive(true);
						skillEffect.transform.SetParent(effectParent);
						skillEffect.transform.localScale = Vector3.one * 100;
						skillEffect.transform.position = cardObj.transform.position;
					}
				}
				else
				{
					effectParent.Find(effectName).gameObject.SetActive(true);
				}
			}
		}
		/**
         * 技能星级列表
         */
        private void SetSkillStarList(int schoolId)
        {
            var schoolCfg = _schoolConfig.GetOrDefault(schoolId);
            SchoolStars.gameObject.SetActive(schoolCfg != null);
            if (!SchoolStars.gameObject.activeSelf) return;
            
            // 星级列表
            var schoolCard = SchoolManager.Instance.GetSchoolCard(schoolId);
            var starListScroll = StarList.transform.Find("Viewport/Content");
            var starCard = starListScroll.Find("Card");
            for (int i = 0; i < schoolCfg.StarList.Count; i++)
            {
                Transform starObj = i >= starListScroll.childCount 
                    ? Instantiate(starCard, starListScroll, true).transform 
                    : starListScroll.GetChild(i);

                var lvCfg = schoolCfg.StarList[i];
                starObj.Find("Desc").GetComponent<TMP_Text>().text = $"{lvCfg.Desc2}";
                
                // 当前星级 - 亮星
                var isCurrentStar = schoolCard != null && schoolCard.CardStar >= lvCfg.Star;

                var starList = starObj.Find("StarList");
                for (int j = 0; j < lvCfg.Star; j++)
                {
	                Transform starChild = j >= starList.childCount 
		                ? Instantiate(starList.GetChild(0), starList, true).transform 
		                : starList.GetChild(j);
	                starChild.Find("Icon/Shine").gameObject.SetActive(isCurrentStar);
	                starChild.Find("Icon/Shine").GetComponent<Image>().color = new Color(255f, 255f, 255f, 1f);
                }
                
                starObj.Find("Desc").GetComponent<TMP_Text>().color = isCurrentStar ? Color.yellow : Color.white;
                starObj.Find("Bg/Select").gameObject.SetActive(isCurrentStar);
            }
        }
		
		/**
		 * 技能属性
		 **/
		private void SetSkillAttributes(int skillId)
		{
			for (int i = 0; i < AttributeScroll.content.childCount; i++)
				AttributeScroll.content.GetChild(i).gameObject.SetActive(false);
            
			var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(skillId);
			if (skillCfg == null || skillCfg.ShowAttributeTypes.Count == 0)
			{
				return;
			}

			// 技能属性
			var soldierData = GamePlayManager.Default.GetVillageGameCard(skillCfg.ActsOnCard);
			var skillData = soldierData.GetSkillById(skillId);
			if (skillData == null) return;

			// 属性信息
			for (int i = 0; i < skillCfg.ShowAttributeTypes.Count; i++)
			{
				var attrType = skillCfg.ShowAttributeTypes[i];
				var attrTran = AttributeScroll.content.GetChild(i);

				foreach (var attr in skillData.Attributes.GetAll())
				{
					if (attr.Key != attrType) continue;
					
					attrTran.gameObject.SetActive(true);
					var finalValue = attr.Value.FinalValue;
					var valSymbol = ": ";
					attrTran.Find("AttrText").GetComponent<TMP_Text>().text = 
						LocalizationManager.Instance.GetText(LocalizationModule.UITexts, attr.Value.GetAttrName(), $"{valSymbol}{finalValue}");
					break;
				}
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
