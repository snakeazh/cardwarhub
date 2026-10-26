using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:6f929076-9633-4016-ab9b-de451d3e4802
	public partial class GeneralSkillUpGradePanel
	{
		public const string Name = "GeneralSkillUpGradePanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText TitleText;
		[SerializeField]
		public RectTransform UpGrade;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText CardLevelText;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText DescText;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText NextLevelText;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText NextLevelPhase;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText NextLevelDescText;
		[SerializeField]
		public Transform SkillUpgradeEffect01;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress UpLoadBtn;
		
		private GeneralSkillUpGradePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			Mask = null;
			TitleText = null;
			UpGrade = null;
			CardLevelText = null;
			DescText = null;
			NextLevelText = null;
			NextLevelPhase = null;
			NextLevelDescText = null;
			SkillUpgradeEffect01 = null;
			UpLoadBtn = null;
			
			mData = null;
		}
		
		public GeneralSkillUpGradePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GeneralSkillUpGradePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GeneralSkillUpGradePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
