using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Manager;
using FusionContinent.Model;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;
using Spine.Unity;
using UnityEngine.Events;
using Cardwar;
using FusionContinent.Utils;
using Game.RedDot;

namespace FusionContinent
{
	public class MonsterAtlasDetailPanelData : UIPanelData
	{
	}
	public partial class MonsterAtlasDetailPanel : UIPanel
	{
		
		private int _monsterId;
		private List<AttributeDataView> _attributeList;
		
		private TbCardBaseConfig _cardConfig;
		private TbMonsterBattleConfig _monsterConfig;
		private TbAttributeConfig _attributeConfig;
        
		private void Awake()
		{
			_cardConfig = DataConfigManager.Tables.TbCardBaseConfig;
			_monsterConfig = DataConfigManager.Tables.TbMonsterBattleConfig;
			_attributeConfig = DataConfigManager.Tables.TbAttributeConfig;
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MonsterAtlasDetailPanelData ?? new MonsterAtlasDetailPanelData();
			CloseBtn.onClick.AddListener(() =>
			{
				RunAnimation(this.GetAnimationCloseNode, () =>
				{
					UIKit.ClosePanel(this);
				});
			});
			
			AttrScrollview.SetUpdateFunc(OnUpdateAttr);
			ReceiveBtn.onClick.AddListener(ReceiveRewardServer);
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
		public void SetData(int monsterId)
		{
			_monsterId = monsterId;
			var cardCfg = _cardConfig.GetOrDefault(monsterId);
			var monsterCfg = _monsterConfig.GetOrDefault(monsterId);
			if (monsterCfg == null || cardCfg == null)
			{
				return;
			}

			// MonsterIcon.sprite = Global.MonsterIconSpriteAtlas.GetSpriteExt(cardCfg.Icon);
			// MonsterIcon.SetNativeSize();
			var spineName = monsterCfg.Spine;
			var spineScale = monsterCfg.SpineScale;
			if (spineName != "")
			{
				var soliderSpine = Global.ResLoader.LoadSync<GameObject>(spineName).Instantiate().transform;
				soliderSpine.SetParent(MonsterSpine);
				soliderSpine.localScale = Vector3.one;
				soliderSpine.localPosition = Vector3.zero;
				soliderSpine.GetChild(0).localPosition = Vector3.zero;
				soliderSpine.GetChild(0).Layer("UI");
				soliderSpine.GetChild(0).localScale = Vector3.one * 80;
				soliderSpine.GetChild(0).GetComponent<MeshRenderer>().sortingLayerName = "UI";
				MonsterSpine.localScale = spineScale > 0 ? Vector3.one * spineScale : Vector3.one;
			}

			var monsterName = monsterCfg.Type.ToString();
			if (monsterName.IsNotNull())
			{
				MonsterLineArt.sprite = Global.MonsterLineArtSpriteAtlas.GetSpriteExt(monsterName+"LineArt");
				MonsterLineArt.SetNativeSize();
				MonsterLineArt.transform.localScale = Vector3.one * 0.6f;
				var lineArtRect = MonsterLineArt.rectTransform;
				lineArtRect.anchorMin = new Vector2(1, 0);
				lineArtRect.anchorMax = new Vector2(1, 0);
				lineArtRect.pivot = new Vector2(1, 0);
				lineArtRect.anchoredPosition = Vector2.zero;
			}
			
			AbilityDescText.SetData(LocalizationModule.UITexts, monsterCfg.AbilityDesc);
			MonsterNameText.SetData(LocalizationModule.UITexts, cardCfg.Name);
			DescText.SetData(LocalizationModule.UITexts, cardCfg.Desc);

			for (int i = 0; i < Content.childCount; i++)
				Content.GetChild(i).gameObject.SetActive(false);
			
			for (int i = 0; i < monsterCfg.AtlasRewards.Count; i++)
			{
				var rewObj = i >= Content.childCount
					? Instantiate(Content.GetChild(0).gameObject, Content)
					: Content.GetChild(i).gameObject;
				
				rewObj.SetActive(true);
				rewObj.GetComponent<RewardEntity>().SetData(monsterCfg.AtlasRewards[i]);
			}
			
			// 属性
			SetAttribute(monsterCfg.LevelPropertyList.First().Attributes);
			
			var atlasStatus = Global.PlayerAtlas.GetMonsterAtlasStatus(monsterCfg.Id);
			ReceiveBtn.interactable = atlasStatus == RewardStatus.CanReceive && monsterCfg.AtlasRewards.Count > 0;
			ReceiveBtn.transform.Find("Text").GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts, 
				atlasStatus == RewardStatus.CanReceive ? "UI_BTN_REWARD_CLAIM" : "UI_BTN_REWARD_CLAIMED");
		}
		

		private void SetAttribute(List<AttributeStruct> attributes)
		{
			_attributeList = AttributeManager.GeAttributeDataViews(new AttributeViewParam()
			{
				Attrs = attributes
			});
			
			if (_attributeList.Count == 0) return;
			AttrScrollview.ShowList(_attributeList.Count);
			AttrScrollview.ScrollToIndex(0);
			AttrScrollview.PlayScaleAnimationDOTween();
		}


		private void OnUpdateAttr(int dataIndex, ScrollItem scrollItem)
		{
			var attr = _attributeList[dataIndex];
			var attrCfg = _attributeConfig.GetOrDefault(attr.Type);

			// 设置数据
			var attrTran = scrollItem.transform.Find("TweenTarget");
			attrTran.Find("AttrText").GetComponent<LocalizationText>().SetData(LocalizationModule.UITexts, attrCfg.Name, $": {attr.Value}{attr.SuffixSymbol}");
		}


		private async void ReceiveRewardServer()
		{
			try
			{
				var request = new ReceiveMonsterRewardRequest { MonsterId = _monsterId };
				var proto = await WebDataFun.SendProtoPostRequest<ReceiveMonsterRewardResponse>(ApiKeys.ApiAtlasReceiveMonsterReward, request);
				if (proto != null)
				{
					// 更新数据
					Global.PlayerAtlas = ProtoMappers.MapPlayerAtlas(proto.PlayerAtlas);
					SetData(_monsterId);
					
					// 同步奖励
					Items.Update(proto.Items);
					
					// 奖励
					var rewardsDict = new Dictionary<string, int>();
					if (proto.Rewards != null)
					{
						foreach (var kv in proto.Rewards)
						{
							rewardsDict[kv.Key.ToString()] = (int)kv.Value;
						}
					}
					// RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
					// rewardTips.SetData(rewardsDict);
					
					// 通知图鉴列表刷新解锁/红点状态（取代旧的 MonsterAtlasPanel.Default.RefreshCardScrollview() 单例调用）
					Global.Publisher.Publish(MessagePublisher.MONSTER_ATLAS_STATUS_CHANGED, null);
					Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE+RedNodeType.MonsterAtlasBtn, null);
				}
			}
			catch (System.Exception ex)
			{
				LogTool.LogError($"领取奖励失败：{ex.Message}");
				FusionContinent.Toast.Show($"领取奖励失败：{ex.Message}");
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
