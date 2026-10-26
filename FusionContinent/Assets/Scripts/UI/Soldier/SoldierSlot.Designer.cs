using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:063f2a25-2296-42a0-9506-8ef29e5d3544
	public partial class SoldierSlot
	{
		public const string Name = "SoldierSlot";
		
		[SerializeField]
		public UnityEngine.UI.Button HeadMask;
		[SerializeField]
		public UnityEngine.UI.Image SoldierHeadBg;
		[SerializeField]
		public UnityEngine.UI.Image SoldierHeadIcon;
		[SerializeField]
		public UnityEngine.UI.Image Mask;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text;
		[SerializeField]
		public UnityEngine.UI.Image TypeIcon;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ActiveFrame;
		
		private SoldierSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			HeadMask = null;
			SoldierHeadBg = null;
			SoldierHeadIcon = null;
			Mask = null;
			Text = null;
			TypeIcon = null;
			ActiveFrame = null;
			
			mData = null;
		}
		
		public SoldierSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		SoldierSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SoldierSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
