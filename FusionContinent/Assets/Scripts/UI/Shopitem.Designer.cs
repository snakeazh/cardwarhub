using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:b04bc2e7-9873-46cf-926a-ec1121d00641
	public partial class Shopitem
	{
		public const string Name = "Shopitem";
		
		[SerializeField]
		public UnityEngine.UI.Image shopitembg;
		[SerializeField]
		public TMPro.TextMeshProUGUI shopnametxt;
		[SerializeField]
		public UnityEngine.UI.Image shopiconbg;
		[SerializeField]
		public UnityEngine.UI.Image shopicon;
		[SerializeField]
		public TMPro.TextMeshProUGUI shopnum;
		[SerializeField]
		public TMPro.TextMeshProUGUI buylimitnum;
		[SerializeField]
		public UnityEngine.UI.Button consumebtn;
		[SerializeField]
		public UnityEngine.UI.Image consumeicon;
		[SerializeField]
		public TMPro.TextMeshProUGUI consumenum;
		[SerializeField]
		public UnityEngine.UI.Image shopitembgmask;
		[SerializeField]
		public TMPro.TextMeshProUGUI ShopNums;
		
		private ShopitemData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			shopitembg = null;
			shopnametxt = null;
			shopiconbg = null;
			shopicon = null;
			shopnum = null;
			buylimitnum = null;
			consumebtn = null;
			consumeicon = null;
			consumenum = null;
			shopitembgmask = null;
			ShopNums = null;
			
			mData = null;
		}
		
		public ShopitemData Data
		{
			get
			{
				return mData;
			}
		}
		
		ShopitemData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ShopitemData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
