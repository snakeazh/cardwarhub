using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:9923c61a-e441-4107-bc8c-576d93af8a74
	public partial class ShopItem2
	{
		public const string Name = "ShopItem2";
		
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
		public TMPro.TextMeshProUGUI DiscountText;
		[SerializeField]
		public UnityEngine.UI.Button consumebtn;
		[SerializeField]
		public UnityEngine.UI.Image consumeicon;
		[SerializeField]
		public TMPro.TextMeshProUGUI consumenum;
		[SerializeField]
		public UnityEngine.UI.Button consumeBtnDisC;
		[SerializeField]
		public UnityEngine.UI.Image DisConsumeIcon;
		[SerializeField]
		public TMPro.TextMeshProUGUI DisConsumeNum;
		[SerializeField]
		public TMPro.TextMeshProUGUI consumeNumBefore;
		[SerializeField]
		public UnityEngine.UI.Image shopitembgmask;
		[SerializeField]
		public TMPro.TextMeshProUGUI ShopNums;
		
		private ShopItem2Data mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			shopitembg = null;
			shopnametxt = null;
			shopiconbg = null;
			shopicon = null;
			shopnum = null;
			buylimitnum = null;
			DiscountText = null;
			consumebtn = null;
			consumeicon = null;
			consumenum = null;
			consumeBtnDisC = null;
			DisConsumeIcon = null;
			DisConsumeNum = null;
			consumeNumBefore = null;
			shopitembgmask = null;
			ShopNums = null;
			
			mData = null;
		}
		
		public ShopItem2Data Data
		{
			get
			{
				return mData;
			}
		}
		
		ShopItem2Data mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ShopItem2Data());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
