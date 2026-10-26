using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class StationPanelData : UIPanelData
	{
	}
	public partial class StationPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as StationPanelData ?? new StationPanelData();
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
