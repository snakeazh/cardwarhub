using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:8a536e95-5da4-4ba2-9226-254499b62fa6
	public partial class GuidePanel2
	{
		public const string Name = "GuidePanel2";
		
		[SerializeField]
		public RectTransform Finger;
		[SerializeField]
		public UnityEngine.RectTransform Maskabled;
		[SerializeField]
		public UnityEngine.RectTransform Maskabled2;
		[SerializeField]
		public UnityEngine.UI.Image Mask;
		[SerializeField]
		public RectTransform MaskCard;
		[SerializeField]
		public RectTransform NextBtn;
		[SerializeField]
		public RectTransform Dialoge;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text;
		[SerializeField]
		public UnityEngine.RectTransform Arrow1;
		[SerializeField]
		public UnityEngine.RectTransform Arrow2;
		[SerializeField]
		public RectTransform GuideTipsMessage1;
		[SerializeField]
		public RectTransform GuideTipsMessage2;
		
		private GuidePanel2Data mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Finger = null;
			Maskabled = null;
			Maskabled2 = null;
			Mask = null;
			MaskCard = null;
			NextBtn = null;
			Dialoge = null;
			Text = null;
			Arrow1 = null;
			Arrow2 = null;
			GuideTipsMessage1 = null;
			GuideTipsMessage2 = null;
			
			mData = null;
		}
		
		public GuidePanel2Data Data
		{
			get
			{
				return mData;
			}
		}
		
		GuidePanel2Data mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GuidePanel2Data());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
