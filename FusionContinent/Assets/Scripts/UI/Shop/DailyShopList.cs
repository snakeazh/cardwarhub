using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class DailyShopListData : UIPanelData
	{
	}
	public partial class DailyShopList : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as DailyShopListData ?? new DailyShopListData();
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
