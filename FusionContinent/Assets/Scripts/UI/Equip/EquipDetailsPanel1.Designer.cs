using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:94d6b849-0552-4a93-9806-18e9a068a938
	public partial class EquipDetailsPanel1
	{
		public const string Name = "EquipDetailsPanel1";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Mask;
		[SerializeField]
		public UnityEngine.UI.Image EquipStyleBg;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress RewardEntity;
		[SerializeField]
		public TMPro.TextMeshProUGUI EquipNameTxt;
		[SerializeField]
		public TMPro.TextMeshProUGUI QualityTxt;
		[SerializeField]
		public TMPro.TextMeshProUGUI NeedLevelTxt;
		[SerializeField]
		public TMPro.TextMeshProUGUI CombatPowerTxt;
		[SerializeField]
		public UnityEngine.UI.Image IsWearImg;
		[SerializeField]
		public UnityEngine.UI.ToggleGroup Toggle;
		[SerializeField]
		public RectTransform EquipAttrPanel;
		[SerializeField]
		public TMPro.TextMeshProUGUI BaseAttrText;
		[SerializeField]
		public UnityEngine.UI.Image FusionGemPanel;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WearBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress UpGradeBtn;
		
		private EquipDetailsPanel1Data mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			Mask = null;
			EquipStyleBg = null;
			RewardEntity = null;
			EquipNameTxt = null;
			QualityTxt = null;
			NeedLevelTxt = null;
			CombatPowerTxt = null;
			IsWearImg = null;
			Toggle = null;
			EquipAttrPanel = null;
			BaseAttrText = null;
			FusionGemPanel = null;
			WearBtn = null;
			UpGradeBtn = null;
			
			mData = null;
		}
		
		public EquipDetailsPanel1Data Data
		{
			get
			{
				return mData;
			}
		}
		
		EquipDetailsPanel1Data mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new EquipDetailsPanel1Data());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
