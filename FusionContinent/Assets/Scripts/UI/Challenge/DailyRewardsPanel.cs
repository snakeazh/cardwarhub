using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
	public class DailyRewardsPanelData : UIPanelData
	{
		public string TitleText { get; set; }
		public string TipText { get; set; }
		public string ReceiveBtnText { get; set; }
		
		public List<RewardAndNum> RewardItems { get; set; }
		public bool CanReceive { get; set; }
		public Action ReceiveCallback { get; set; }
	}
	public partial class DailyRewardsPanel : UIPanel
	{
		private DailyRewardsPanelData _data;
		
		protected override void OnInit(IUIData uiData = null)
		{
			// please add init code here
			Quit.onClick.AddListener(OnClose);
			ReceiveBtn.onClick.AddListener(() =>
			{
				if (_data?.ReceiveCallback == null) return;
				_data.ReceiveCallback();
				OnClose();
			});
		}

		public void SetData(DailyRewardsPanelData data)
		{
			_data = data;
			
			TipText.text = data.TipText;
			TitleText.text = data.TitleText;
			
			for (int i = 0; i < data.RewardItems.Count; i++)
			{
				GameObject rewObj = i >= Content.childCount 
					? Global.ResLoader.LoadSync<GameObject>("RewardEntity").Instantiate()
					: Content.GetChild(i).gameObject;
				
				rewObj.transform.SetParent(Content);
				rewObj.LocalScale(Vector3.one);
				var rewardEntity = rewObj.GetComponent<RewardEntity>();
				rewardEntity.SetData(data.RewardItems[i]);
			}

			ReceiveBtn.interactable = data.CanReceive;
			ReceiveBtn.transform.Find("Text").GetComponent<TMP_Text>().text = data.ReceiveBtnText;
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
			gameObject.SetActive(false);
		}
	}
}
