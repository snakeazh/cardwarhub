using UnityEngine.UI;
using UnityEngine;
using QFramework;

namespace FusionContinent
{
	public class ShopItem2Data : UIPanelData
	{
	}
	public partial class ShopItem2 : UIPanel
	{
		public int shopItemID;

		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as ShopItem2Data ?? new ShopItem2Data();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		public void SetShopItemId(int id)
		{
			shopItemID = id;
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
