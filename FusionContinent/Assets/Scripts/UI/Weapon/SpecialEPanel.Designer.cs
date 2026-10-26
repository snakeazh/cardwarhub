using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:0e4ef6c1-9166-4745-b9ef-db5c61d7b6e2
	public partial class SpecialEPanel
	{
		public const string Name = "SpecialEPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WeaponRuleItem;
		[SerializeField]
		public TMPro.TextMeshProUGUI SkillName;
		[SerializeField]
		public UnityEngine.UI.ScrollRect EffectScrollView;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ExitBtn;
		
		private SpecialEPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			WeaponRuleItem = null;
			SkillName = null;
			EffectScrollView = null;
			ExitBtn = null;
			
			mData = null;
		}
		
		public SpecialEPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SpecialEPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SpecialEPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
