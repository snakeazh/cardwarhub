using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class HeroSeatPanelData : UIPanelData
	{
	}
	public partial class HeroSeatPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as HeroSeatPanelData ?? new HeroSeatPanelData();
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
