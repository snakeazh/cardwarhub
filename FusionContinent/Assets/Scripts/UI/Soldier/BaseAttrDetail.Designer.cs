using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:eec4aef1-f1bc-44ec-a0ff-eb81803ce65b
	public partial class BaseAttrDetail
	{
		public const string Name = "BaseAttrDetail";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI AttrValue1;
		[SerializeField]
		public TMPro.TextMeshProUGUI AttrValue2;
		
		private BaseAttrDetailData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			ExitBtn = null;
			AttrValue1 = null;
			AttrValue2 = null;
			
			mData = null;
		}
		
		public BaseAttrDetailData Data
		{
			get
			{
				return mData;
			}
		}
		
		BaseAttrDetailData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BaseAttrDetailData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
