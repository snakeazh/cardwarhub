using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class ChangeHeadPanelData : UIPanelData
	{
	}
	public partial class ChangeHeadPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ChangeHeadPanelData ?? new ChangeHeadPanelData();
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
