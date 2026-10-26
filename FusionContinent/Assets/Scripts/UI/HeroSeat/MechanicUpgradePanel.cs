using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class MechanicUpgradePanelData : UIPanelData
	{
	}
	public partial class MechanicUpgradePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MechanicUpgradePanelData ?? new MechanicUpgradePanelData();
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
