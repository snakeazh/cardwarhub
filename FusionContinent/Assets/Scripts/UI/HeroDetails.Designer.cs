using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:e47687ae-d76a-4555-bda1-d18f4f182383
	public partial class HeroDetails
	{
		public const string Name = "HeroDetails";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public RectTransform Head;
		[SerializeField]
		public RectTransform Spine;
		[SerializeField]
		public UnityEngine.UI.Button MainAttributes;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text;
		[SerializeField]
		public TMPro.TextMeshProUGUI NumText;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock AttrScrollview;
		
		private HeroDetailsData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			Head = null;
			Spine = null;
			MainAttributes = null;
			Text = null;
			NumText = null;
			AttrScrollview = null;
			
			mData = null;
		}
		
		public HeroDetailsData Data
		{
			get
			{
				return mData;
			}
		}
		
		HeroDetailsData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new HeroDetailsData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
