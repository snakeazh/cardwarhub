using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class MonsterPreViewPanelData : UIPanelData
	{
	}
	public partial class MonsterPreViewPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MonsterPreViewPanelData ?? new MonsterPreViewPanelData();
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
