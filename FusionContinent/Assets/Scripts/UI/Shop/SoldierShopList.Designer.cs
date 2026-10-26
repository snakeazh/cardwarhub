using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:4883ca87-064a-4d1d-849c-3b70ea090569
	public partial class SoldierShopList
	{
		public const string Name = "SoldierShopList";
		
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock ShopListScroll;
		[SerializeField]
		public RectTransform shopitemparent;
		
		private SoldierShopListData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			ShopListScroll = null;
			shopitemparent = null;
			
			mData = null;
		}
		
		public SoldierShopListData Data
		{
			get
			{
				return mData;
			}
		}
		
		SoldierShopListData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SoldierShopListData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
