using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:fc30dba4-5209-4037-8203-782a34fa5c69
	public partial class FusionGemFragmentPanel
	{
		public const string Name = "FusionGemFragmentPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public RectTransform Body;
		[SerializeField]
		public UnityEngine.RectTransform RewardScroll;
		[SerializeField]
		public UnityEngine.UI.Dropdown QualityDropdown;
		[SerializeField]
		public UnityEngine.UI.ScrollRect FusionGemList;
		[SerializeField]
		public UnityEngine.UI.Image Bg;
		[SerializeField]
		public UnityEngine.UI.Image Frame;
		[SerializeField]
		public UnityEngine.UI.Image Icon;
		[SerializeField]
		public TMPro.TextMeshProUGUI Num;
		[SerializeField]
		public RectTransform Effect;
		[SerializeField]
		public UnityEngine.UI.Image FrameOut;
		[SerializeField]
		public UnityEngine.UI.Image Checkbox;
		[SerializeField]
		public UnityEngine.UI.Image Checkbox2;
		[SerializeField]
		public UnityEngine.UI.Button FragmentBtn;
		
		private FusionGemFragmentPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Quit = null;
			Body = null;
			RewardScroll = null;
			QualityDropdown = null;
			FusionGemList = null;
			Bg = null;
			Frame = null;
			Icon = null;
			Num = null;
			Effect = null;
			FrameOut = null;
			Checkbox = null;
			Checkbox2 = null;
			FragmentBtn = null;
			
			mData = null;
		}
		
		public FusionGemFragmentPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		FusionGemFragmentPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new FusionGemFragmentPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
