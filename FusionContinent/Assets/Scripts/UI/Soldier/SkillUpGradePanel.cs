using System;
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
using YooAsset;
using ValueType = Config.ValueType;
using Cardwar;
using Game.CombatPower;
using FusionContinent.Utils;
using PlayerCard = FusionContinent.Model.PlayerCard;
using RewardAndNum = Config.RewardAndNum;
using Game.RedDot;

namespace FusionContinent
{
	public class SkillUpGradePanelData : UIPanelData
	{
	}
	public partial class SkillUpGradePanel : UIPanel
	{

		private PlayerCard _playerSkill;
		private readonly Dictionary<int, LevelUpAnimation.State> mLevelStateCache = new Dictionary<int, LevelUpAnimation.State>();
		
		private CardDevelopConfig _devCfg;
		private SpecialEffectsConfig _skillCfg;
		
		private TbCardBaseConfig _cardBaseConfig;
		private TbCardDevelopConfig _cardDevelopConfig;
		private TbSpecialEffectsConfig _specialEffectsConfig;
		
		private void Awake()
		{
			_cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
			_cardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
			_specialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SkillUpGradePanelData ?? new SkillUpGradePanelData();
			QuitBtn.onClick.AddListener(OnClose);
			ConfirmUpGradeBtn.onClick.AddListener(OnConfirmUpgrade);
		}
		
		public void SetData(int skillId)
		{
			mLevelStateCache.Clear();
			var cfg = _cardBaseConfig.GetOrDefault(skillId);
			_devCfg = _cardDevelopConfig.GetOrDefault(skillId);
			_skillCfg = _specialEffectsConfig.GetOrDefault(skillId);
			if (_devCfg == null || _skillCfg == null) return;

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
			
			// 刷新统一界面
			RefreshUpgradePanel();
		}
		/**
		 * 刷新统一的升级/升阶界面
		 * 
		 * @return 
		 * @author Zpp
		 * @create 2025/12/12
		 **/
		void RefreshUpgradePanel()
		{
			if (_devCfg == null || _skillCfg == null || _playerSkill == null) return;
			
			// 获取配置
			var devLevelCfg = _devCfg.LevelPropertyList[_playerSkill.Lv - 1];
			var skillLevelCfg = _skillCfg.LevelPropertyList[_playerSkill.Lv - 1];
			
			// 设置卡牌信息
			SetCardSlot(CardSlot, _playerSkill);
			CardLevelText.SetData(LocalizationModule.UITexts, devLevelCfg.Name);

			// 刷新升品描述
			RefreshLevelDetailDesc(devLevelCfg);

			// 技能名称
			var skillNameText = SkillName.GetComponent<TMP_Text>();
			if (skillNameText != null && _playerSkill.Config != null)
			{
				skillNameText.text = _playerSkill.Config.Name;
			}

			// 描述
			var attrText = GetLevelAttrText2(skillLevelCfg.Desc, skillLevelCfg.Attributes);
			DescText.SetData(LocalizationModule.UITexts, _playerSkill.Config.Desc, attrText);
			
			// 判断是否满级
			bool isMaxLevel = _playerSkill.Lv >= _devCfg.LevelPropertyList.Count;
			bool isHaveNext = _playerSkill.Lv < _skillCfg.LevelPropertyList.Count;
			
			// 下一等级预览
			// if (isMaxLevel || !_playerSkill.IsHave)
			// {
			// 	// 满级
			// 	NextLevelText.gameObject.SetActive(false);
			// 	NextLevelDescText.SetData(LocalizationModule.UITexts, "UI_TITLE_NEXT_PHASE_MAX");
			// }
			// else
			// {
			// 	var devNextLevelCfg = _devCfg.LevelPropertyList[_playerSkill.Lv];
			// 	var skillNextLevelCfg = _skillCfg.LevelPropertyList[_playerSkill.Lv];
			// 	
			// 	// 判断是否需要进阶
			// 	bool needUpPhase = skillLevelCfg.Quality != skillNextLevelCfg.Quality;
			// 	
			// 	if (needUpPhase)
			// 	{
			// 		// 升阶预览
			// 		NextLevelText.SetData(LocalizationModule.UITexts, devNextLevelCfg.Name);
			// 		NextLevelDescText.SetData(LocalizationModule.UITexts, devNextLevelCfg.UpQualityDesc.FirstOrDefault());
			// 	}
			// 	else
			// 	{
			// 		// 升级预览
			// 		NextLevelText.SetData(LocalizationModule.UITexts, devNextLevelCfg.Name);
			// 		NextLevelDescText.SetData(LocalizationModule.UITexts, devNextLevelCfg.UpGradeEffectsDesc.FirstOrDefault());
			// 	}
			// }
			
			// 设置消耗和按钮状态
			RefreshConsumeAndButton(isMaxLevel, isHaveNext, devLevelCfg);
			
			// 技能属性
			RefreshSkillAttributes(skillLevelCfg, isMaxLevel ? null : _skillCfg.LevelPropertyList[_playerSkill.Lv]);
		}
		
		/**
		 * 刷新技能属性显示
		 * 
		 * @param skillLevelCfg 当前等级配置
		 * @param skillNextLevelCfg 下一等级配置（null表示满级）
		 **/
		void RefreshSkillAttributes(SpecialLevelPropertyList skillLevelCfg, SpecialLevelPropertyList skillNextLevelCfg)
		{
			if (SkillAttributeScroll.content == null) return;
			
			// 获取技能配置
			var damageType = _skillCfg.DamageType;
			var damageParams = _skillCfg.DamageParam;
			
			// 遍历所有属性槽位
			for (int i = 0; i < SkillAttributeScroll.content.childCount; i++)
			{
				var slot = SkillAttributeScroll.content.GetChild(i);
				
				// 只处理前4个槽位（0:攻击力, 1:伤害类型, 2:继承伤害, 3:冷却时间）
				if (i >= 4)
				{
					slot.gameObject.SetActive(false);
					continue;
				}
				
				slot.gameObject.SetActive(true);
				var attrNum = slot.Find("AttrNum");
				var attrDiff = slot.Find("AttrDiff");
				var isDiffBg = slot.Find("IsDiffBg");
				
				// 获取或创建TMP组件
				TMP_Text attrNumText = null;
				TMP_Text attrDiffText = null;
				
				if (attrNum != null)
				{
					attrNumText = attrNum.GetComponent<TMP_Text>();
				}
				if (attrDiff != null)
				{
					attrDiffText = attrDiff.GetComponent<TMP_Text>();
				}
				
				// 获取当前值和下一级值
				float currentVal = 0;
				float nextVal = 0;
				string suffix = "";
				string titleKey = "";
				
				switch (i)
				{
					case 0: // 基础攻击力 
						titleKey = "UI_SKILL_BASE_ATTACK";
						var baseAttackAttr = skillLevelCfg.Attributes.Find(a => a.Type == AttributeType.SkillBasicAttackPower);
						var nextBaseAttackAttr = skillNextLevelCfg?.Attributes.Find(a => a.Type == AttributeType.SkillBasicAttackPower);
						if (baseAttackAttr != null) currentVal = baseAttackAttr.Value;
						if (nextBaseAttackAttr != null) nextVal = nextBaseAttackAttr.Value;
						else if (skillNextLevelCfg != null) nextVal = currentVal;
						break;
						
					case 1: // 伤害类型
						titleKey = "UI_SKILL_DAMAGE_TYPE";
						if (damageType != null && damageType.Count > 0)
						{
							// 伤害类型用枚举值作为ID
							currentVal = (int)damageType[0];
							nextVal = currentVal; // 伤害类型不会变化
						}
						break;
						
					case 2: // 继承伤害 - 从DamageParam中找百分比类型 (ValueType=0)
						titleKey = "UI_SKILL_INHERIT_DAMAGE";
						if (damageParams != null && damageParams.Count > 0)
						{
							var inheritParam = damageParams.FirstOrDefault(p => p.ValueType == ValueType.Percentage);
							if (inheritParam != null)
							{
								currentVal = inheritParam.EffectVal * 100; // 转为百分比
								nextVal = currentVal; // DamageParam是配置级别的，不随等级变化
							}
						}
						suffix = "%";
						break;
						
					case 3: // 冷却时间 - 从Attributes中找 SkillCdTime (Type=111)
						titleKey = "UI_SKILL_COOLDOWN";
						var cdAttr = skillLevelCfg.Attributes.Find(a => a.Type == AttributeType.SkillCdTime);
						var nextCdAttr = skillNextLevelCfg?.Attributes.Find(a => a.Type == AttributeType.SkillCdTime);
						if (cdAttr != null) currentVal = cdAttr.Value;
						if (nextCdAttr != null) nextVal = nextCdAttr.Value;
						else if (skillNextLevelCfg != null) nextVal = currentVal;
						suffix = "s";
						break;
				}
				
				// 计算差值
				float diffVal = nextVal - currentVal;
				bool hasDiff = skillNextLevelCfg != null && Mathf.Abs(diffVal) > 0.001f;
				
				// 冷却时间需要保留小数，其他属性保留整数
				string valFormat = (i == 3) ? "F1" : "F0";
				
				// 设置主数值文本
				if (attrNumText != null)
				{
					if (i == 1) // 伤害类型显示名称
					{
						var damageTypeName = GetDamageTypeName((TakeDamageType)(int)currentVal);
						attrNumText.GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts,damageTypeName);
					}
					else
					{
						attrNumText.text = $"{currentVal.ToString(valFormat)}{suffix}";
						
						// 继承伤害根据伤害类型设置不同颜色和材质
						if (i == 2 && damageType != null && damageType.Count > 0)
						{
							var dmgType = damageType[0];
							Material mat = null;
							if (dmgType == TakeDamageType.PhysicalDamage)
							{
								mat = Global.ResLoader.LoadSync<Material>("ChironGoRoundTC-700BSDFStaticGreen");
								attrNumText.color = new Color(0.533f, 1f, 0.49f); // #88FF7D
							}
							else if (dmgType == TakeDamageType.MagicDamage)
							{
								mat = Global.ResLoader.LoadSync<Material>("ChironGoRoundTC-700BSDFStaticIndigo");
								attrNumText.color = new Color(0.616f, 0.851f, 1f); // #9DD9FF
							}
							if (mat != null)
							{
								attrNumText.fontMaterial = mat;
							}
						}
					}
				}
				
				// 设置差值文本和背景
				if (attrDiff != null)
				{
					attrDiff.gameObject.SetActive(hasDiff);
					if (hasDiff && attrDiffText != null)
					{
						string diffStr = diffVal > 0 ? $"+{diffVal.ToString(valFormat)}" : $"{diffVal.ToString(valFormat)}";
						attrDiffText.text = diffStr;
					}
				}
				
				if (isDiffBg != null)
				{
					isDiffBg.gameObject.SetActive(hasDiff);
				}
			}
		}

		/**
		 * 刷新升品描述
		 *
		 * @param devLevelCfg 当前等级配置
		 **/
		void RefreshLevelDetailDesc(DevelopLevelPropertyList devLevelCfg)
		{
			if (LevelDetail == null || _devCfg == null) return;

			// 收集所有等级中非空的 UpQualityDesc，每个描述项都记录对应的等级
			var allDescs = new List<string>();
			var allLevels = new List<int>();
			foreach (var levelCfg in _devCfg.LevelPropertyList)
			{
				if (levelCfg.UpQualityDesc != null && levelCfg.UpQualityDesc.IsNotNull())
				{
					foreach (var desc in levelCfg.UpQualityDesc.Where(d => !string.IsNullOrEmpty(d)))
					{
						allDescs.Add(desc);
						allLevels.Add(levelCfg.Level);
					}
				}
			}

			int maxCount = Math.Min(LevelDetail.childCount, 4);

			for (int i = 0; i < maxCount; i++)
			{
				var child = LevelDetail.GetChild(i);
				var childLevel = i + 1;
				var levelUpAnim = child.GetComponent<LevelUpAnimation>();
				EnsureLevelDetailRowActive(child, levelUpAnim);

				// 从收集的描述中依次填充
				var descText = child.Find("LevelDescText")?.GetComponent<TMP_Text>();
				var levelText = child.Find("LevelText")?.GetComponent<TMP_Text>();
				if (descText != null)
				{
					descText.text = i < allDescs.Count ? allDescs[i] : string.Empty;
				}
				if (levelText != null)
				{
					levelText.text = i < allLevels.Count ? $"Lv.{allLevels[i]}" : $"Lv.{childLevel}";
				}

				int unlockLevel = i < allLevels.Count ? allLevels[i] : childLevel;
				bool isLocked = _playerSkill.Lv < unlockLevel;
				ApplyLevelDetailAnimator(levelUpAnim, isLocked, unlockLevel);
			}
		}

		static void EnsureLevelDetailRowActive(Transform child, LevelUpAnimation levelUpAnim)
		{
			bool wasActive = child.gameObject.activeSelf;
			child.gameObject.SetActive(true);
			if (!wasActive)
			{
				levelUpAnim.PrepareAnimator();
			}
		}

		void ApplyLevelDetailAnimator(LevelUpAnimation levelUpAnim, bool isLocked, int cacheKey)
		{
			bool hasCachedState = mLevelStateCache.TryGetValue(cacheKey, out var cachedState);
			var playState = LevelUpAnimation.ResolvePlayState(isLocked, hasCachedState, cachedState);
			levelUpAnim.SetAnimationState(playState);
			mLevelStateCache[cacheKey] = LevelUpAnimation.ToCachedState(isLocked);
		}

		/**
		 * 获取伤害类型名称
		 * 
		 * @param type 伤害类型
		 * @return 本地化的伤害类型名称
		 **/
		string GetDamageTypeName(TakeDamageType type)
		{
			switch (type)
			{
				case TakeDamageType.PhysicalDamage:
					return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_PHYSICAL");
				case TakeDamageType.MagicDamage:
					return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_MAGIC");
				case TakeDamageType.MagicDamageThunder:
					return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_THUNDER");
				case TakeDamageType.RealDamage:
					return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_REAL");
				case TakeDamageType.SchoolDamage:
					return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_SCHOOL");
				default:
					return LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_DAMAGE_TYPE_GENERAL");
			}
		}
		
		/**
		 * 刷新消耗和按钮状态
		 * 
		 * @param isMaxLevel 是否满级
		 * @param isHaveNext 是否有下一级
		 * @param devLevelCfg 当前等级配置
		 **/
		void RefreshConsumeAndButton(bool isMaxLevel, bool isHaveNext, DevelopLevelPropertyList devLevelCfg)
		{
			var skillLevelCfg = _skillCfg.LevelPropertyList[_playerSkill.Lv - 1];
			
			// 满级处理
			if (isMaxLevel || !_playerSkill.IsHave || !isHaveNext)
			{
				Consumes.gameObject.SetActive(false);
				ConfirmUpGradeBtn.gameObject.SetActive(false);
				return;
			}
			
			// 有下一级
			ConfirmUpGradeBtn.gameObject.SetActive(true);
			var skillNextLevelCfg = _skillCfg.LevelPropertyList[_playerSkill.Lv];
			var isUpPhase = skillLevelCfg.Quality != skillNextLevelCfg.Quality;
			var isEnough = SetConsumes(devLevelCfg.ConsumeItems);
			
			// 设置消耗（升级时显示，升阶时隐藏）
			Consumes.gameObject.SetActive(!isUpPhase);
			
			// 按钮状态和文字
			var buttonText = ConfirmUpGradeBtn.transform.Find("Text").GetComponent<LocalizationText>();
			var upgradeIcon = ConfirmUpGradeBtn.transform.Find("CanUpgrade").gameObject;
			var redDot = ConfirmUpGradeBtn.GetComponent<LocalRedDot>();
			
			upgradeIcon.SetActive(false);
			if (isUpPhase)
			{
				// 需要进阶 - 按钮显示升阶但可能不可点击
				buttonText.SetData(LocalizationModule.UITexts, "UI_BTN_ADVANCED");
				ConfirmUpGradeBtn.interactable = isEnough;
				redDot.SetRed(isEnough ? 1 : 0);
			}
			else
			{
				// 可以升级
				buttonText.SetData(LocalizationModule.UITexts, "UI_BTN_UP_GRADE");
				upgradeIcon.SetActive(true);
				ConfirmUpGradeBtn.interactable = isEnough;
				redDot.SetRed(isEnough ? 1 : 0);
			}
		}

		/**
		 * 统一的确认升级/升阶方法
		 * 
		 * @return 
		 * @author Zpp
		 * @create 2025/12/12
		 **/
		void OnConfirmUpgrade()
		{
			UnlockGuideManager.Send(ModuleFunctionType.SoldierAtlas, 5);
			
			// 检查材料
			if (!CheckConsumes())
			{
				FusionContinent.Toast.Show(LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_TIPS_TALENT_ITEM_INSUFFICIENT"));
				return;
			}
			
			// 获取配置
			var devLevelCfg = _devCfg.LevelPropertyList[_playerSkill.Lv - 1];
			var skillLevelCfg = _skillCfg.LevelPropertyList[_playerSkill.Lv - 1];
			var isUpPhase = skillLevelCfg.Quality != _skillCfg.LevelPropertyList[_playerSkill.Lv].Quality;
			
			// 标题
			var titleKey = isUpPhase ? "UI_BTN_ADVANCED" : "UI_BTN_UP_GRADE";
			var title = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, titleKey);
			
			// 确认消息
			var confirmMessage = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, 
				"UI_TIPS_WHETHER_TO_CONSUME", Items.GetRewardsStr(devLevelCfg.ConsumeItems), title);
			confirmMessage += $"\n {devLevelCfg.Name} >> <color=#AE57B0> {_devCfg.LevelPropertyList[_playerSkill.Lv].Name}</color>";
			
			GameMainPanel.Default.ShowConfirm(
				LocalizationManager.Instance.GetText(LocalizationModule.UITexts, "UI_TITLE_CONFIRM_PARAM", title)
				, confirmMessage, SkillUpgradeServer);
		}

		/**
		 * 设置底部内容
		 *
		 * @return
		 * @author Zpp
		 * @create 2025/12/12
		 **/
		bool SetConsumes(List<RewardAndNum> consumeItems)
		{
			var isEnough = true;
			
			// 消耗
			for (int i = 0; i < Consumes.childCount; i++)
			{
				var consumesTran = Consumes.GetChild(i);
				consumesTran.gameObject.SetActive(i < consumeItems.Count);
				if (!consumesTran.gameObject.activeSelf) continue;

				var consumesItem = consumeItems[i];
				if (!consumesTran.GetComponent<RewardEntity>().SetData(consumesItem, RewardStyleType.Consume))
				{
					isEnough = false;
				}
			}

			return isEnough;
		}

		bool CheckConsumes()
		{
			for (int i = 0; i < Consumes.childCount; i++)
			{
				var consumesTran = Consumes.GetChild(i);
				if (!consumesTran.gameObject.activeSelf) continue;
				if (!consumesTran.GetComponent<RewardEntity>().isEnough)
					return false;
			}

			return true;
		}

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
		
		string GetLevelAttrText(List<AttributeStruct> attributes)
		{
			var attrText = "";
			foreach (var v in attributes)
			{
				if (attrText != "") attrText += ", ";
				var symbol = v.Value > 0 ? "+" : "";
				var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(v.Type);
				attrText += LocalizationManager.Instance.GetText(LocalizationModule.UITexts, attrCfg.Name, $"{symbol}{v.Value}");
			}
			
			return attrText;
		}

		/**
		 * 设置卡牌数据
		 *
		 * @return
		 * @author Zpp
		 * @create 2025/12/11
		 **/
		private void SetCardSlot(CardSlot cardSlot, PlayerCard playerSkill)
		{
			if (playerSkill == null) return;
			
			// 设置卡数据
			var skillCfg = _specialEffectsConfig.GetOrDefault(playerSkill.CardId);
			var cardData = new CardSlotData()
			{
				CardId = playerSkill.CardId,
				NameText = playerSkill.Config.Name,
				LevelText = $"{playerSkill.Lv}",
				Desc = playerSkill.Config.Desc,
				State = playerSkill.Config.State,
				IsUnlock = playerSkill.IsHave,
				QualityType = skillCfg.LevelPropertyList[playerSkill.Lv - 1].Quality,
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

		protected override void OnOpen(IUIData uiData = null)
		{
			mLevelStateCache.Clear();
			WarmUpLevelDetailAnimators();
		}

		void WarmUpLevelDetailAnimators()
		{
			if (LevelDetail == null) return;

			int count = Math.Min(LevelDetail.childCount, 4);
			for (int i = 0; i < count; i++)
			{
				var child = LevelDetail.GetChild(i);
				var levelUpAnim = child.GetComponent<LevelUpAnimation>();
				EnsureLevelDetailRowActive(child, levelUpAnim);
			}
		}

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

	            var formData = new Dictionary<string, string>
	            {
	                { "CardId", _playerSkill.CardId.ToString() },
	                { "CardLv", (_playerSkill.Lv + 1).ToString() },
	            };
	            var request = new Cardwar.LevelUpCardRequest { CardId = _playerSkill.CardId, CardLv = _playerSkill.Lv + 1 };
	            var proto = await WebDataFun.SendProtoPostRequest<Cardwar.LevelUpCardResponse>(ApiKeys.ApiCardLevelUp, request);
	            if (proto != null && proto.Card != null)
	            {
	                _playerSkill = FusionContinent.Utils.ProtoMappers.MapPlayerCard(proto.Card);
	                Global.PlayerCards[_playerSkill.CardId] = _playerSkill;

	                // 同步奖励
	                Items.Update(proto.Items);
	                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
	                RefreshUpgradePanel();
	                
	                // 升级特效
	                SkillUpgradeEffect01.gameObject.SetActive(true);
	                DOVirtual.DelayedCall(2f, () => SkillUpgradeEffect01.gameObject.SetActive(false));

	                Global.Publisher.Publish(MessagePublisher.REFRESH_SOLDIER_SKILL_LIST, null);

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

		protected override void OnAnimationOpen()
		{
			base.OnAnimationOpen();
			// UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.SoldierAtlas,5,ConfirmUpGradeBtn.GetComponent<RectTransform>());
			
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
