using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class BottomBarData : UIPanelData
	{
	}
	public partial class BottomBar : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BottomBarData ?? new BottomBarData();
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
