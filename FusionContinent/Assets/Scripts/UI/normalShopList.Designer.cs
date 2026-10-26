using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:5200a51c-f3eb-4c6b-8c94-881a476c7564
	public partial class normalShopList
	{
		public const string Name = "normalShopList";
		
		[SerializeField]
		public RectTransform shopitemparent;
		
		private normalShopListData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			shopitemparent = null;
			
			mData = null;
		}
		
		public normalShopListData Data
		{
			get
			{
				return mData;
			}
		}
		
		normalShopListData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new normalShopListData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
