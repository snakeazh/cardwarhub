using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:61ef8d00-ed10-452b-9c36-bde66c1aff3c
	public partial class WeaponPanel
	{
		public const string Name = "WeaponPanel";
		
		[SerializeField]
		public UnityEngine.UI.Image ExitBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI WeaponName;
		[SerializeField]
		public RectTransform WeaponStar;
		[SerializeField]
		public TMPro.TextMeshProUGUI WeaponBonus;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WeaponRuleItem;
		[SerializeField]
		public TMPro.TextMeshProUGUI WeaponSkillStateText;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WeaponForgeBtn;
		[SerializeField]
		public ScrollView.UIHorizontalScrollViewLock WeaponSCView;
		[SerializeField]
		public RectTransform WeaponNode;
		[SerializeField]
		public UIConsumeItem Consume;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WearBtn;
		
		private WeaponPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			ExitBtn = null;
			WeaponName = null;
			WeaponStar = null;
			WeaponBonus = null;
			WeaponRuleItem = null;
			WeaponSkillStateText = null;
			WeaponForgeBtn = null;
			WeaponSCView = null;
			WeaponNode = null;
			Consume = null;
			WearBtn = null;
			
			mData = null;
		}
		
		public WeaponPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		WeaponPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new WeaponPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
