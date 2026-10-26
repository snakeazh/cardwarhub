using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:0a8bf427-3b76-4aef-ac55-6d4c1bed5060
	public partial class ShopItem
	{
		public const string Name = "ShopItem";
		
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
		public TMPro.TextMeshProUGUI consumeNumBefore;
		[SerializeField]
		public TMPro.TextMeshProUGUI DisConsumeNum;
		[SerializeField]
		public UnityEngine.UI.Image shopitembgmask;
		[SerializeField]
		public TMPro.TextMeshProUGUI ShopNums;
		
		private ShopItemData mPrivateData = null;
		
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
			consumeNumBefore = null;
			DisConsumeNum = null;
			shopitembgmask = null;
			ShopNums = null;
			
			mData = null;
		}
		
		public ShopItemData Data
		{
			get
			{
				return mData;
			}
		}
		
		ShopItemData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ShopItemData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
