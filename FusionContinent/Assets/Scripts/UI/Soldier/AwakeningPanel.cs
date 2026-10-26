using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Helper;
using Game.CombatPower;
using FusionContinent.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class AwakeningPanelData : UIPanelData
	{
	}
	public partial class AwakeningPanel : UIPanel
	{
		private TbCardDevelopConfig _cardDevelopConfig;
		private TbSkinConfig _skinConfig;
		private TbItemConfig _itemConfig;
		private TbCardBaseConfig _cardBase;

		public static Action<int> AwakeningAction;

		private int skinid;
		private int _cardId;
		private int _cardLv;
		
		private void Awake()
		{
			_cardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
			_skinConfig = DataConfigManager.Tables.TbSkinConfig;
			_itemConfig = DataConfigManager.Tables.TbItemConfig;
			_cardBase = DataConfigManager.Tables.TbCardBaseConfig;
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as AwakeningPanelData ?? new AwakeningPanelData();
			// please add init code here
		}
		
		/// <summary>
		/// 暂未使用。回收 AwakeningMaterial 下的所有奖励对象
		/// </summary>
		private void RecycleMaterials()
		{
			for (int i = AwakeningMaterial.transform.childCount - 1; i >= 0; i--)
			{
				var child = AwakeningMaterial.transform.GetChild(i).gameObject;
				CommonItemPoolManager.Instance.GetGameObjectPool("RewardEntity").Return(child);
			}
		}
		
		public void SetAwakening(int cardId, int lv)
		{
			this.gameObject.SetActive(true);
			_cardId = cardId;
			_cardLv = lv;

			// 先清空旧的材料
			RecycleMaterials();

			var levelPropertyList = _cardDevelopConfig.GetOrDefault(cardId)?.LevelPropertyList;
			if (levelPropertyList == null || levelPropertyList.Count == 0)
			{
				// 这里补上 skinid 赋值
				AssignLatestSkinId(levelPropertyList);
				SetAsAwakened();
				return;
			}

			// 找到最近的觉醒点（>= 当前等级 且有奖励）
			var awakeningLevel = levelPropertyList
				.Where(x => x.Level >= lv && x.AwakeningRewards.Any())
				.OrderByDescending(x => x.Level)
				.FirstOrDefault();

			if (awakeningLevel == null || awakeningLevel.AwakeningRewards.Count < 2)
			{
				this.gameObject.SetActive(false);
				FusionContinent.Toast.Show("暂无觉醒");
				return;
			}

			ProcessAwakening(awakeningLevel, lv);
		}
		
		/// <summary>
        /// 找到最近一次 ≤ 当前等级的觉醒点，并赋值 skinid
        /// </summary>
        private void AssignLatestSkinId(List<DevelopLevelPropertyList> levelPropertyList)
        {
            if (skinid != 0 || levelPropertyList == null) return;

            var lastAwakening = levelPropertyList
                .Where(x => x.Level <= _cardLv && x.AwakeningRewards.Any())
                .OrderByDescending(x => x.Level)
                .FirstOrDefault();

            if (lastAwakening != null)
            {
                var skinConfigTemp = _skinConfig.GetOrDefault(
                    _itemConfig.GetOrDefault(lastAwakening.AwakeningRewards[0].Id).EffectValue
                );
                if (skinConfigTemp != null)
                {
                    skinid = skinConfigTemp.Id;
                }
            }
        }

        private void SetAsAwakened()
        {
            // 清理材料
            RecycleMaterials();

            // Tips.text = "英灵已觉醒";
            // CardParent.gameObject.SetActive(false);
            // AwakenText.gameObject.SetActive(false);
            // 如果 skinid 没有赋值，就找到最近一次觉醒奖励的皮肤
            if (skinid == 0)
            {
                var levelPropertyList = _cardDevelopConfig.GetOrDefault(_cardId)?.LevelPropertyList;
                if (levelPropertyList != null)
                {
                    // 找到 <= 当前等级的最近觉醒点
                    var lastAwakening = levelPropertyList
                        .Where(x => x.Level <= _cardLv && x.AwakeningRewards.Any())
                        .OrderByDescending(x => x.Level)
                        .FirstOrDefault();

                    if (lastAwakening != null)
                    {
                        var skinConfigTemp = _skinConfig.GetOrDefault(
                            _itemConfig.GetOrDefault(lastAwakening.AwakeningRewards[0].Id).EffectValue
                        );
                        if (skinConfigTemp != null)
                        {
                            skinid = skinConfigTemp.Id;
                        }
                    }
                }
            }
            //
            // SkinConfig skinConfig = _skinConfig.GetOrDefault(skinid);
            // for (int i = SoliderSpine.transform.childCount - 1; i >= 1; i--)
            // {
            //     Destroy(SoliderSpine.transform.GetChild(i).gameObject);
            // }
            //
            // if (skinConfig != null)
            // {
            //     SoliderSpine.transform.Find("Icon").localScale = Vector3.one;
            //     SoliderSpine.transform.Find("Icon").GetComponent<Image>().sprite =
            //         Global.CardIconSpriteAtlas.GetSpriteExt(skinConfig.Icon);
            // }
            //
            // if (skinConfig != null && !string.IsNullOrEmpty(skinConfig.IllustrationSpine))
            // {
            //     SoliderSpine.transform.Find("Icon").localScale = Vector3.zero;
            //     //先清除SoliderSpine.transform下的原有预制体
            //
            //     GameObject skin = Global.ResLoader.LoadSync<GameObject>(skinConfig.IllustrationSpine).Instantiate();
            //     skin.transform.SetParent(SoliderSpine.transform);
            //     skin.transform.localScale = Vector3.one * skinConfig.IllustrationSpineScale;
            //     skin.transform.localPosition = Vector3.zero;
            // }

            AwakeningBtn.gameObject.SetActive(false);
        }

        private void ProcessAwakening(DevelopLevelPropertyList awakeningLevel, int level)
        {
            if (awakeningLevel.AwakeningRewards.Count < 2) return;

            // 皮肤
            var skinItemCfg = _itemConfig.GetOrDefault(awakeningLevel.AwakeningRewards[0].Id);
            var skinConfig = _skinConfig.GetOrDefault(skinItemCfg.EffectValue);

            // 技能
            var skillItemCfg = _itemConfig.GetOrDefault(awakeningLevel.AwakeningRewards[1].Id);
            var skillConfig = _cardBase.GetOrDefault(skillItemCfg.EffectValue);

            if (skinConfig == null || skillConfig == null) return;

            // if (skinConfig.Icon != null)
            // {
            //     SoliderSpine.transform.Find("Icon").localScale = Vector3.one;
            //     // SoliderSpine.transform.Find("Icon").GetComponent<Image>().sprite = Global.CardIconSpriteAtlas.GetSpriteExt(skinConfig.Icon);
            //     SpriteHelper.TryLoadTexture(skinConfig.Icon, SoliderSpine.transform.Find("Icon").GetComponent<Image>());
            // }
            //
            // if (skinConfig.IllustrationSpine != "")
            // {
            //     SoliderSpine.transform.Find("Icon").localScale = Vector3.zero;
            //     GameObject skin =Global.ResLoader.LoadSync<GameObject>(skinConfig.IllustrationSpine).Instantiate();
            //     skin.transform.SetParent(SoliderSpine.transform);
            //     skin.transform.localScale = Vector3.one * skinConfig.IllustrationSpineScale;
            //     skin.transform.localPosition = Vector3.zero;
            // }

            AwakeningBtn.gameObject.SetActive(true);
            // Tips.text = "英灵满级后可进行觉醒";
            // CardParent.gameObject.SetActive(true);
            // AwakenText.gameObject.SetActive(true);

            bool isBuy = true;

            // 生成消耗材料
            for (int i = 0; i < awakeningLevel.ConsumeItems.Count; i++)
            {
                var rewardObj = CommonItemPoolManager.Instance.GetGameObjectPool("RewardEntity").Get();
                if (rewardObj == null) return;

                rewardObj.transform.SetParent(AwakeningMaterial.transform, false);
                bool isReward = rewardObj.GetComponent<RewardEntity>()
                    .SetData(awakeningLevel.ConsumeItems[i].Id,
                        awakeningLevel.ConsumeItems[i].Num,
                        RewardStyleType.Consume);

                if (!isReward) isBuy = false;
            }

            // 设置卡牌展示
            var cardData = new CardSlotData()
            {
                Icon = Global.CardIconSpriteAtlas.GetSpriteExt(skillConfig.Icon),
                CardId = skillConfig.Id,
                NameText = skillConfig.Name,
                QualityType = QualityType.Ordinary,
                Desc = skillConfig.Desc,
            };
            // var cards = CardParent.transform.Find("CardSlot");
            // cards.GetComponent<CardSlot>().SetData(cardData);
            // cards.GetComponent<Button>().onClick.RemoveAllListeners();
            // cards.GetComponent<Button>().onClick.AddListener(() =>
            // {
            //     // 使用MVVM架构打开技能升级面板
            //     var skillUpgradeVm = new Game.Skill.SkillUpGradePanelViewModel();
            //     skillUpgradeVm.OnCreate();
            //     skillUpgradeVm.SetData(skillConfig.Id);
            //     UIFramework.UIManager.OpenPanelAsync<SkillUpGradePanelNew, Game.Skill.SkillUpGradePanelViewModel>(skillUpgradeVm, UIFramework.UILevel.PopUI).Forget();
            // });
            AwakeningBtn.interactable = true;
            // 未达到觉醒等级，禁用按钮
            if (level != awakeningLevel.Level)
            {
                AwakeningBtn.interactable = false;
                return;
            }

            // 重设按钮事件
            AwakeningBtn.onClick.RemoveAllListeners();
            AwakeningBtn.onClick.AddListener(() =>
            {
                if (!isBuy)
                {
	                FusionContinent.Toast.Show("材料或等级不够", 1.5f);
                    return;
                }

                SoldierAwakeningServer();
            });
        }
        
        private async void SoldierAwakeningServer()
        {
	        try
	        {
		        CombatPowerChangeHelper.CaptureBaseline();

		        var request = new Cardwar.LevelUpCardRequest { CardId = _cardId, CardLv = _cardLv + 1 };
		        var proto = await WebDataFun.SendProtoPostRequest<Cardwar.LevelUpCardResponse>(ApiKeys.ApiCardLevelUp, request);
		        if (proto != null && proto.Card != null)
		        {
			        var localCard = FusionContinent.Utils.ProtoMappers.MapPlayerCard(proto.Card);
			        Global.PlayerCards[localCard.CardId] = localCard;

			        // 同步奖励
			        Items.Update(proto.Items);

			        // 刷新显示
			        skinid = Global.PlayerCards[localCard.CardId].ObjData.Skin;
			        AwakeningAction?.Invoke(skinid);
			        SetAsAwakened();

			        CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();

			        // 完成引导
			        //GuideFlow.Instance.SetStepComplete(GuideType.HeroAwakening);
		        }
	        }
	        catch (System.Exception ex)
	        {
		        LogTool.LogError($"升级失败：{ex.Message}");
		        FusionContinent.Toast.Show(ex.Message);
	        }
        }
		
		protected override void OnOpen(IUIData uiData = null)
		{
			Closebtn.onClick.RemoveAllListeners();
			Closebtn.onClick.AddListener(() => { UIKit.ClosePanel(this); });
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			RecycleMaterials();
		}
	}
}
