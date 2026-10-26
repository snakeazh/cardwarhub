using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:0816c928-e8b9-42ec-89c6-e72cf69ddc00
	public partial class DailyShopList
	{
		public const string Name = "DailyShopList";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI TimeText;
		[SerializeField]
		public RectTransform shopitemparent;
		
		private DailyShopListData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TimeText = null;
			shopitemparent = null;
			
			mData = null;
		}
		
		public DailyShopListData Data
		{
			get
			{
				return mData;
			}
		}
		
		DailyShopListData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new DailyShopListData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
