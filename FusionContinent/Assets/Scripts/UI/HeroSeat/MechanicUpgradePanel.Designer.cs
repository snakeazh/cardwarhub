using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:e5de4679-923b-4389-984e-377190f3acef
	public partial class MechanicUpgradePanel
	{
		public const string Name = "MechanicUpgradePanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public UnityEngine.RectTransform Bg;
		[SerializeField]
		public RectTransform ConsumeList;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress UpGradeBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress UnWearBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WearBtn;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI MechanicName;
		[SerializeField]
		public TMPro.TextMeshProUGUI MechanicDetail;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelText;
		[SerializeField]
		public UnityEngine.UI.Slider LevelSlider;
		[SerializeField]
		public RectTransform LevelList;
		[SerializeField]
		public TMPro.TextMeshProUGUI MainAttrName;
		[SerializeField]
		public TMPro.TextMeshProUGUI CurNum;
		[SerializeField]
		public TMPro.TextMeshProUGUI NextNum;
		[SerializeField]
		public UnityEngine.UI.ScrollRect MechanicEffSCView;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress SeatEntity;
		
		private MechanicUpgradePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Mask = null;
			Bg = null;
			ConsumeList = null;
			UpGradeBtn = null;
			UnWearBtn = null;
			WearBtn = null;
			CloseBtn = null;
			MechanicName = null;
			MechanicDetail = null;
			LevelText = null;
			LevelSlider = null;
			LevelList = null;
			MainAttrName = null;
			CurNum = null;
			NextNum = null;
			MechanicEffSCView = null;
			SeatEntity = null;
			
			mData = null;
		}
		
		public MechanicUpgradePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		MechanicUpgradePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MechanicUpgradePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
