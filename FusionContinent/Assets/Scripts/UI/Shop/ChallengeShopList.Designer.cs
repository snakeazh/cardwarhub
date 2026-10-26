using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:ee0514b7-167f-46b9-866d-7ee4fbe8887a
	public partial class ChallengeShopList
	{
		public const string Name = "ChallengeShopList";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public TMPro.TextMeshProUGUI TitltText;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock ShopListScroll;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Exit;
		
		private ChallengeShopListData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			TitltText = null;
			ShopListScroll = null;
			Exit = null;
			
			mData = null;
		}
		
		public ChallengeShopListData Data
		{
			get
			{
				return mData;
			}
		}
		
		ChallengeShopListData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ChallengeShopListData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
