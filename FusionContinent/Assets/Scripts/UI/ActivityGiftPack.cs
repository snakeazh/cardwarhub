using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Localization;
using Config;
using Game.RedDot;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class ActivityGiftPackData : UIPanelData
	{
		public string TitleText;
		public List<RewardAndNum> Rewards = new List<RewardAndNum>();
		public List<RewardAndNum> FreeRewards = new List<RewardAndNum>();
		public List<RewardAndNum> BuyConsumes = new List<RewardAndNum>();
		public bool IsPurchased; // 是否已购买
		public Action OnClick;
	}
	public partial class ActivityGiftPack : UIPanel
	{
		private ActivityGiftPackData _data;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ActivityGiftPackData ?? new ActivityGiftPackData();
			// please add init code here
		}

		public void SetData(ActivityGiftPackData data)
		{
			_data = data;
			var isEnough = true;
			
			TitleText.SetData(LocalizationModule.UITexts, data.TitleText);
			FreeText.gameObject.SetActive(data.BuyConsumes.Count == 0 || data.IsPurchased);
			FreeText.SetData(LocalizationModule.UITexts, data.IsPurchased ? "UI_BTN_PURCHASED" : "UI_BTN_FREE");
			if (data.BuyConsumes.Count == 0 && !data.IsPurchased)
			{
				RedDot.SetLocalRedCount(1);
			}
			else
			{
				RedDot.SetLocalRedCount(0);
			}
			RewardEntityNotDetail.gameObject.SetActive(data.BuyConsumes.Count > 0 && !data.IsPurchased);
			if (RewardEntityNotDetail.gameObject.activeSelf) 
				isEnough = RewardEntityNotDetail.SetData(data.BuyConsumes.First(), RewardStyleType.Consume);

			for (int i = 0; i < Content.childCount; i++)
				Content.GetChild(i).gameObject.SetActive(false);

			for (int i = 0; i < data.Rewards.Count; i++)
			{
				var rewTran = i >= Content.childCount
					? Instantiate(Content.GetChild(0), Content)
					: Content.GetChild(i);
				
				rewTran.gameObject.SetActive(true);
				rewTran.GetComponent<RewardEntity>().SetData(data.Rewards[i]);
			}
			
			for (int i = 0; i < data.FreeRewards.Count; i++)
			{
				var rewTran = i >= Content.childCount
					? Instantiate(Content.GetChild(0), Content)
					: Content.GetChild(i);
				
				rewTran.gameObject.SetActive(true);
				rewTran.GetComponent<RewardEntity>().SetData(data.FreeRewards[i]);
			}
			
			BuyBtn.onClick.RemoveAllListeners();
			BuyBtn.interactable = !data.IsPurchased;
			BuyBtn.onClick.AddListener(() =>
			{
				if (data.OnClick == null) return;
				if (!isEnough) {
					FusionContinent.Toast.Show(
					LocalizationManager.Instance.GetText(LocalizationModule.SystemTexts, "SYS_ERR_ITEM_NOT_ENOUGH"));
					return;
				}
				
				data.OnClick();
			});
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
