using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class FusionGemPanelData : UIPanelData
	{
	}
	public partial class FusionGemPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as FusionGemPanelData ?? new FusionGemPanelData();
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
