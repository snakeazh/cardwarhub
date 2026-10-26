using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class SpeakWheelSkinPanelData : UIPanelData
	{
	}
	public partial class SpeakWheelSkinPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SpeakWheelSkinPanelData ?? new SpeakWheelSkinPanelData();
			// please add init code here
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
