using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:d0e5ce39-711e-4e70-89d4-afeb528537b7
	public partial class SpeakWheelSkinPanel
	{
		public const string Name = "SpeakWheelSkinPanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI PlayerName;
		[SerializeField]
		public RectTransform SkinStar;
		[SerializeField]
		public RectTransform AttributeScrollContent;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ForgeBtn;
		[SerializeField]
		public RectTransform SkinRoot;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WeaponRuleItem;
		[SerializeField]
		public TMPro.TextMeshProUGUI SkinSkillStateText;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock PlayerScrollView;
		[SerializeField]
		public UnityEngine.UI.Image ExitBtn;
		[SerializeField]
		public UnityEngine.UI.Image AwakeBtn;
		[SerializeField]
		public RectTransform WeaponNode;
		[SerializeField]
		public UIConsumeItem Consume;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WearBtn;
		
		private SpeakWheelSkinPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			PlayerName = null;
			SkinStar = null;
			AttributeScrollContent = null;
			ForgeBtn = null;
			SkinRoot = null;
			WeaponRuleItem = null;
			SkinSkillStateText = null;
			PlayerScrollView = null;
			ExitBtn = null;
			AwakeBtn = null;
			WeaponNode = null;
			Consume = null;
			WearBtn = null;
			
			mData = null;
		}
		
		public SpeakWheelSkinPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SpeakWheelSkinPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SpeakWheelSkinPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
