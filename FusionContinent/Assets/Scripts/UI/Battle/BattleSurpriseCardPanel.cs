using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class BattleSurpriseCardPanelData : UIPanelData
	{
	}
	public partial class BattleSurpriseCardPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BattleSurpriseCardPanelData ?? new BattleSurpriseCardPanelData();
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
