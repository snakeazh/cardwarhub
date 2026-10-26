using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class ShopItemData : UIPanelData
	{
	}
	public partial class ShopItem : UIPanel
	{
		public int shopItemID;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ShopItemData ?? new ShopItemData();
			// please add init code here
		}

		public void SetShopItemId(int id)
		{
			shopItemID = id;
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
