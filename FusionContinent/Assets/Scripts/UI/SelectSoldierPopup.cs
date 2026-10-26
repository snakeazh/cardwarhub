using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Animation;
using FusionContinent.Helper;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.Events;

namespace FusionContinent
{
	public class SelectSoldierPopupData : UIPanelData
	{
	}
	public partial class SelectSoldierPopup : UIPanel
	{
		private int _currentCardId = 0;
		private readonly Dictionary<int, GameObject> _cardObjs = new Dictionary<int, GameObject>();
		
		private TbCardBaseConfig _cardBaseConfig;

		private void Awake()
		{
			_cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
			
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SelectSoldierPopupData ?? new SelectSoldierPopupData();
			// please add init code here
			
			Quit.onClick.RemoveAllListeners();
			Quit.onClick.AddListener(CloseListPopup);
			
			SoldierHead.onClick.RemoveAllListeners();
			SoldierHead.onClick.AddListener(ShowListPopup);
		}

		public void SetData(Action<int> onClick, bool notInitClick = false)
		{
			foreach (var v in Global.PlayerCards.Values)
			{
				var cardId = v.CardId;
				var baseCfg = _cardBaseConfig.GetOrDefault(cardId);
				if (baseCfg is not { Type: CardType.Soldier })
					continue;

				if (!_cardObjs.TryGetValue(cardId, out GameObject cardObj))
				{ 
					//从对象池中提出
					cardObj = Instantiate(Content.GetChild(0).gameObject, Content);
					cardObj.transform.localScale = Vector3.one * 0.75f;
					_cardObjs.Add(cardId, cardObj);
				}
				
				cardObj.SetActive(true);
				
				// 设置卡牌数据
				cardObj.GetComponent<ScrollViewItem>().SetScrollView(SoldierListPopup.Find("CardScroll").GetComponent<ScrollRect>());
				cardObj.GetComponent<SoldierCardGroupSlot>().SetData(new CardSlotData()
				{
					Icon = Global.HeroIconSpriteAtlas.GetSprite(baseCfg.Icon+"LittleIcon"),
					CardId = cardId,
					NameText = baseCfg.Name,
					LevelText = $"{v.Lv}",
					QualityType = QualityType.Ordinary,
					Desc = baseCfg.Desc,
					State = baseCfg.State
				});
				
				Button cardBtn = cardObj.GetComponent<Button>();
				cardBtn.onClick.RemoveAllListeners();
				cardBtn.onClick.AddListener(() =>
				{
					UnlockGuideManager.Send(ModuleFunctionType.Talent, 10);
					onClick(cardId);
					_currentCardId = cardId;
					SoldierListPopup.gameObject.SetActive(false);
					SoldierHead.transform.Find("HeadImg").GetComponent<Image>().sprite =
						Global.HeroHeadIconAtlas.GetSprite(baseCfg.Icon+"Little");
				});
			}
			
			if (!notInitClick && _currentCardId == 0 && _cardObjs.Count > 0)
				_cardObjs.First().Value.GetComponent<Button>().onClick.Invoke();
		}

		public void ShowListPopup()
		{
			SoldierListPopup.gameObject.SetActive(true);
			RunAnimation(SoldierListPopup.Find("OpenAnimation").gameObject, OnAnimationOpen);
		}

		protected override void OnAnimationOpen()
		{
			base.OnAnimationOpen();
			UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent,10,Content.GetChild(2).GetComponent<RectTransform>());

		}

		public void CloseListPopup()
		{
			RunAnimation(SoldierListPopup.Find("CloseAnimation").gameObject, () => 
				SoldierListPopup.gameObject.SetActive(false));
		}

		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target, complete);
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
