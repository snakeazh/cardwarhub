using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class WeaponPanelData : UIPanelData
	{
	}
	public partial class WeaponPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as WeaponPanelData ?? new WeaponPanelData();
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
