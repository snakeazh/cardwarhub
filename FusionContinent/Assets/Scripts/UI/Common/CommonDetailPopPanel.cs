using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class CommonDetailPopPanelData : UIPanelData
	{
	}
	public partial class CommonDetailPopPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as CommonDetailPopPanelData ?? new CommonDetailPopPanelData();
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
