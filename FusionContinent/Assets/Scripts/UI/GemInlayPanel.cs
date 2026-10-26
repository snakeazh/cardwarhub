using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.U2D;

namespace FusionContinent
{
	public class GemInlayPanelData : UIPanelData
	{
	}
	public partial class GemInlayPanel : UIPanel
	{
		private List<GemInlaySlot> gemInlaySlots = new List<GemInlaySlot>();
		private int gemNum;
		private MainCityPanel _mainCityPanel;
		protected override void OnInit(IUIData uiData = null)
		{
			gemInlaySlots.Add(GemInlaySlot1);
			gemInlaySlots.Add(GemInlaySlot2);
			gemInlaySlots.Add(GemInlaySlot3);
			gemInlaySlots.Add(GemInlaySlot4);
			gemInlaySlots.Add(GemInlaySlot5);
			gemInlaySlots.Add(GemInlaySlot6);
			mData = uiData as GemInlayPanelData ?? new GemInlayPanelData();
			Bg.onClick.AddListener(() =>
			{
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                UIKit.HidePanel<GemInlayPanel>();
			});
			InlayBtn.onClick.AddListener(() =>
			{
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                AddGem();
			});
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			RefreshShow();
		}

		public void RefreshShow()
		{
			gemNum = 0;
			for (int i = 0; i < Global.GemSlots.Count; i++)
			{
				var slotData = Global.GemSlots[i];
				var gemInlay = gemInlaySlots[i];

				if (slotData != null)
				{
					gemNum++;
				}
				gemInlay.SetData(slotData,i);
			}

			TitleText.text = $"已镶嵌：{gemNum}/6";
			if (gemNum >= 6)
			{
				ReplaceText.gameObject.SetActive(true);
				InlayBtn.gameObject.SetActive(false);
			}
			else
			{
				ReplaceText.gameObject.SetActive(false);
				InlayBtn.gameObject.SetActive(true);
			}
			_mainCityPanel = UIKit.GetPanel<MainCityPanel>();

		}
		
		private void AddGem()
		{

			// 找到第一个空槽位
			int emptySlotIndex = Global.GemSlots.FindIndex(slot => slot == null);

			if (emptySlotIndex == -1)
			{
				LogTool.LogWarning("没有空的槽位可供镶嵌！");
				return;
			}
			
			RefreshShow();
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
