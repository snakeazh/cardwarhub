using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:16c240f1-6dcd-48e8-9aea-73acb965fb14
	public partial class GeneralCardResonancePanel
	{
		public const string Name = "GeneralCardResonancePanel";
		
		[SerializeField]
		public RectTransform EnergyToggleGroup;
		[SerializeField]
		public UnityEngine.UI.Image CurrentSelectEnergyIcon;
		[SerializeField]
		public RectTransform CardToggleGroup;
		[SerializeField]
		public RectTransform EnergyItem;
		[SerializeField]
		public RectTransform EnergyItemNextLevel;
		[SerializeField]
		public RectTransform ResonancePointList;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelUpSucProbabilityText;
		[SerializeField]
		public RectTransform DetailInfo;
		[SerializeField]
		public TMPro.TextMeshProUGUI MainDesc;
		[SerializeField]
		public RectTransform ConsumeItemList;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress UpGradeBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ExitBtn;
		
		private GeneralCardResonancePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			EnergyToggleGroup = null;
			CurrentSelectEnergyIcon = null;
			CardToggleGroup = null;
			EnergyItem = null;
			EnergyItemNextLevel = null;
			ResonancePointList = null;
			LevelUpSucProbabilityText = null;
			DetailInfo = null;
			MainDesc = null;
			ConsumeItemList = null;
			UpGradeBtn = null;
			ExitBtn = null;
			
			mData = null;
		}
		
		public GeneralCardResonancePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GeneralCardResonancePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GeneralCardResonancePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
