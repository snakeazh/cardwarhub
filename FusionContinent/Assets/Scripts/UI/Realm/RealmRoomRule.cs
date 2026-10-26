using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class RealmRoomRuleData : UIPanelData
	{
	}
	public partial class RealmRoomRule : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as RealmRoomRuleData ?? new RealmRoomRuleData();
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
