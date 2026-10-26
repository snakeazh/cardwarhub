using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:10fd1362-2389-4611-8f97-9c993b18fc27
	public partial class EquipFragmentPanel
	{
		public const string Name = "EquipFragmentPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public RectTransform Body;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock EquipList;
		[SerializeField]
		public UnityEngine.RectTransform RewardScroll;
		[SerializeField]
		public FusionContinent.UI.DropDownBase PartDropdown;
		[SerializeField]
		public FusionContinent.UI.DropDownBase QualityDropdown;
		[SerializeField]
		public UnityEngine.UI.Button FragmentBtn;
		
		private EquipFragmentPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			Body = null;
			EquipList = null;
			RewardScroll = null;
			PartDropdown = null;
			QualityDropdown = null;
			FragmentBtn = null;
			
			mData = null;
		}
		
		public EquipFragmentPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		EquipFragmentPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new EquipFragmentPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
