using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class ChooseCardNumPanelData : UIPanelData
	{
	}
	public partial class ChooseCardNumPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ChooseCardNumPanelData ?? new ChooseCardNumPanelData();
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
