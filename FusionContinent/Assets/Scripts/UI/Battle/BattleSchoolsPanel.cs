using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class BattleSchoolsPanelData : UIPanelData
	{
	}
	public partial class BattleSchoolsPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BattleSchoolsPanelData ?? new BattleSchoolsPanelData();
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
