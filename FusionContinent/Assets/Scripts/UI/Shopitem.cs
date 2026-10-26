using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class ShopitemData : UIPanelData
	{
	}
	public partial class Shopitem : UIPanel
	{
		public Transform shopItemBuyLImitIcon;
		
		public int shopitemid;
		
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ShopitemData ?? new ShopitemData();
			// please add init code here
		}

		public void SetShopitemid(int shopitemid)
		{
			this.shopitemid = shopitemid;
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
