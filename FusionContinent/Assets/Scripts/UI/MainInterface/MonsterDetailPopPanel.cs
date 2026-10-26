using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class MonsterDetailPopPanelData : UIPanelData
	{
	}
	public partial class MonsterDetailPopPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MonsterDetailPopPanelData ?? new MonsterDetailPopPanelData();
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
