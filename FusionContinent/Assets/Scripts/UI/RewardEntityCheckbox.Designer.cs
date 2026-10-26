using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:11d294cb-7f9c-4481-851f-a8621a050a3c
	public partial class RewardEntityCheckbox
	{
		public const string Name = "RewardEntityCheckbox";
		
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
		
		private RewardEntityCheckboxData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Bg = null;
			Frame = null;
			Icon = null;
			Num = null;
			Effect = null;
			FrameOut = null;
			Checkbox = null;
			Checkbox2 = null;
			
			mData = null;
		}
		
		public RewardEntityCheckboxData Data
		{
			get
			{
				return mData;
			}
		}
		
		RewardEntityCheckboxData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new RewardEntityCheckboxData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
