using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:7974f256-5623-4980-967c-dc8d8a780e44
	public partial class GeneralCardSlot
	{
		public const string Name = "GeneralCardSlot";
		
		[SerializeField]
		public UnityEngine.UI.Button CardQualityBg;
		[SerializeField]
		public UnityEngine.UI.Image CardIcon;
		[SerializeField]
		public UnityEngine.UI.Button FrameStyleImg;
		[SerializeField]
		public TMPro.TextMeshProUGUI CardNameText;
		[SerializeField]
		public TMPro.TextMeshProUGUI NumText;
		[SerializeField]
		public UnityEngine.UI.Image Lock;
		[SerializeField]
		public UnityEngine.UI.Image Progress;
		[SerializeField]
		public TMPro.TextMeshProUGUI ProgressNumText;
		[SerializeField]
		public UnityEngine.UI.Image IsSelected;
		[SerializeField]
		public TMPro.TextMeshProUGUI SelectCount;
		[SerializeField]
		public UnityEngine.UI.Image IsGear;
		
		private GeneralCardSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CardQualityBg = null;
			CardIcon = null;
			FrameStyleImg = null;
			CardNameText = null;
			NumText = null;
			Lock = null;
			Progress = null;
			ProgressNumText = null;
			IsSelected = null;
			SelectCount = null;
			IsGear = null;

			mData = null;
		}
		
		public GeneralCardSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		GeneralCardSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GeneralCardSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
