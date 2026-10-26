using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class SpecialEPanelData : UIPanelData
	{
		public int ConfigId;
	}
	public partial class SpecialEPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SpecialEPanelData ?? new SpecialEPanelData();
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
