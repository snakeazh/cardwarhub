using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:6848330a-d3f5-4bed-b941-316fa2408f54
	public partial class GeneralSkillPanel
	{
		public const string Name = "GeneralSkillPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button QuestBtn;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock GeneralSkillScroll;
		[SerializeField]
		public UnityEngine.RectTransform ChooseGeneralCard;
		[SerializeField]
		public RectTransform GeneralContent;
		[SerializeField]
		public RectTransform ArcResonance;
		[SerializeField]
		public RectTransform GeneralSkillFilter;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress DecomposeButton;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress CombineBtn;
		[SerializeField]
		public TMPro.TMP_Dropdown Dropdown;
		[SerializeField]
		public UICommonBtn ResonanceBtn;
		
		private GeneralSkillPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			QuestBtn = null;
			GeneralSkillScroll = null;
			ChooseGeneralCard = null;
			GeneralContent = null;
			ArcResonance = null;
			GeneralSkillFilter = null;
			DecomposeButton = null;
			CombineBtn = null;
			Dropdown = null;
			ResonanceBtn = null;
			
			mData = null;
		}
		
		public GeneralSkillPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GeneralSkillPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GeneralSkillPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
