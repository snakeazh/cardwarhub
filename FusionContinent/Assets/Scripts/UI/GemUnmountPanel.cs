using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class GemUnmountPanelData : UIPanelData
	{
	}
	public partial class GemUnmountPanel : UIPanel
	{
		private List<GemUnmountSlot> _gemUnmountSlots = new List<GemUnmountSlot>();
		private int gemNum;

		protected override void OnInit(IUIData uiData = null)
		{
			_gemUnmountSlots.Add(GemInlaySlot1);
			_gemUnmountSlots.Add(GemInlaySlot2);
			_gemUnmountSlots.Add(GemInlaySlot3);
			_gemUnmountSlots.Add(GemInlaySlot4);
			_gemUnmountSlots.Add(GemInlaySlot5);
			_gemUnmountSlots.Add(GemInlaySlot6);
			CloseBtn.onClick.AddListener(() =>
			{
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                UIKit.HidePanel<GemUnmountPanel>();
			});
			mData = uiData as GemUnmountPanelData ?? new GemUnmountPanelData();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			gemNum = 0;
			for (int i = 0; i < Global.GemSlots.Count; i++)
			{
				var slotData = Global.GemSlots[i];
				var gemUnmount = _gemUnmountSlots[i];

				if (slotData != null)
				{
					gemNum++;
				}
				gemUnmount.SetData(slotData,i);
			}

			TitleText.text = $"已镶嵌：{gemNum}/6";
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
