using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class BattlePanelData : UIPanelData
	{
	}
	public partial class BattlePanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BattlePanelData ?? new BattlePanelData();
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
