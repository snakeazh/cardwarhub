using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:c7c6eb3a-8a95-4521-93d9-2f630986f2d3
	public partial class MainAttrDetail
	{
		public const string Name = "MainAttrDetail";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI AttrName;
		[SerializeField]
		public TMPro.TextMeshProUGUI AttrValue;
		
		private MainAttrDetailData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			ExitBtn = null;
			AttrName = null;
			AttrValue = null;
			
			mData = null;
		}
		
		public MainAttrDetailData Data
		{
			get
			{
				return mData;
			}
		}
		
		MainAttrDetailData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MainAttrDetailData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
