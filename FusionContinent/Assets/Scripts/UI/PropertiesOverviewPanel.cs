using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class PropertiesOverviewPanelData : UIPanelData
	{
	}
	public partial class PropertiesOverviewPanel : UIPanel
	{
		private List<GameObject> slotObj = new List<GameObject>();
		private List<Text> slotTexts = new List<Text>();
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as PropertiesOverviewPanelData ?? new PropertiesOverviewPanelData();
			slotObj.Add(GemInlaySlot1.gameObject);
			slotObj.Add(GemInlaySlot2.gameObject);
			slotObj.Add(GemInlaySlot3.gameObject);
			slotObj.Add(GemInlaySlot4.gameObject);
			slotObj.Add(GemInlaySlot5.gameObject);
			slotObj.Add(GemInlaySlot6.gameObject);
			slotTexts.Add(Slot1Text);
			slotTexts.Add(Slot2Text);
			slotTexts.Add(Slot3Text);
			slotTexts.Add(Slot4Text);
			slotTexts.Add(Slot5Text);
			slotTexts.Add(Slot6Text);
			CloseBtn.onClick.AddListener(() =>
			{
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                UIKit.HidePanel<PropertiesOverviewPanel>();
			});
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			CityLordText.text = $"{DataConfigManager.Tables.TbMainCityBattleConfig.GetOrDefault(Global.CitySkill).Desc}";
			for (int i = 0; i < Global.GemSlots.Count; i++)
			{
				if (Global.GemSlots[i] != null)
				{
					
				}
				else
				{
					slotTexts[i].text = $"";
				}
			}
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
