using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class SpeakWhellPanelData : UIPanelData
	{
	}
	public partial class SpeakWhellPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SpeakWhellPanelData ?? new SpeakWhellPanelData();
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
