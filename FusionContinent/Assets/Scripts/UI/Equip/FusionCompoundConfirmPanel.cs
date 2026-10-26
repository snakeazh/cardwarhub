using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class FusionCompoundConfirmPanelData : UIPanelData
	{
	}
	public partial class FusionCompoundConfirmPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as FusionCompoundConfirmPanelData ?? new FusionCompoundConfirmPanelData();
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
