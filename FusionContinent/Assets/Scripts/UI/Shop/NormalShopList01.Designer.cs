using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:b1c0f60c-854c-4fd6-94b0-17bea9687014
	public partial class NormalShopList01
	{
		public const string Name = "NormalShopList01";
		
		[SerializeField]
		public RectTransform shopitemparent;
		[SerializeField]
		public RectTransform shopitemTitleparent;
		
		private NormalShopList01Data mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			shopitemparent = null;
			shopitemTitleparent = null;
			
			mData = null;
		}
		
		public NormalShopList01Data Data
		{
			get
			{
				return mData;
			}
		}
		
		NormalShopList01Data mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new NormalShopList01Data());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
