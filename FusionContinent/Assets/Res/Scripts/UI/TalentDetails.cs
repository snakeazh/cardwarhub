using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class TalentDetailsData : UIPanelData
	{
	}
	public partial class TalentDetails : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as TalentDetailsData ?? new TalentDetailsData();
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
