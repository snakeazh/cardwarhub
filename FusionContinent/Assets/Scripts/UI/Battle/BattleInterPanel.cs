using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class BattleInterPanelData : UIPanelData
	{
	}
	public partial class BattleInterPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BattleInterPanelData ?? new BattleInterPanelData();
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
