using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class GeneralCardResonancePanelData : UIPanelData
	{
	}
	public partial class GeneralCardResonancePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GeneralCardResonancePanelData ?? new GeneralCardResonancePanelData();
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
