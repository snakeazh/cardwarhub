using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:cf05b24e-9810-45d8-b952-4975c41e0b3d
	public partial class ShopPanel01
	{
		public const string Name = "ShopPanel01";
		
		[SerializeField]
		public UnityEngine.UI.Image BannerBg;
		[SerializeField]
		public Canvas TitleCanvas;
		[SerializeField]
		public RectTransform Banner;
		[SerializeField]
		public RectTransform Body;
		[SerializeField]
		public RectTransform Foot;
		[SerializeField]
		public RectTransform Change;
		
		private ShopPanel01Data mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BannerBg = null;
			TitleCanvas = null;
			Banner = null;
			Body = null;
			Foot = null;
			Change = null;
			
			mData = null;
		}
		
		public ShopPanel01Data Data
		{
			get
			{
				return mData;
			}
		}
		
		ShopPanel01Data mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ShopPanel01Data());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
