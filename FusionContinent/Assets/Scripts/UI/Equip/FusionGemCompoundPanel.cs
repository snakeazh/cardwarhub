using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class FusionGemCompoundPanelData : UIPanelData
	{
	}
	public partial class FusionGemCompoundPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as FusionGemCompoundPanelData ?? new FusionGemCompoundPanelData();
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
