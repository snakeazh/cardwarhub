using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:a715ed93-34a9-4627-a1d2-12b374fcbfb6
	public partial class EquipUpGradePanel
	{
		public const string Name = "EquipUpGradePanel";
		
		[SerializeField]
		public RectTransform Equip;
		[SerializeField]
		public UnityEngine.UI.Image EquipIcon;
		[SerializeField]
		public TMPro.TextMeshProUGUI NameText;
		[SerializeField]
		public Transform ForgingEffectHammer01;
		[SerializeField]
		public Transform ForgingEffectUpgrade01;
		[SerializeField]
		public UnityEngine.RectTransform Body;
		[SerializeField]
		public RectTransform UpGrade;
		[SerializeField]
		public RectTransform Level;
		[SerializeField]
		public UnityEngine.UI.Image Attribute;
		[SerializeField]
		public UnityEngine.UI.Button UpGradeBtn;
		[SerializeField]
		public RectTransform ExtraAttributes;
		[SerializeField]
		public RectTransform Refine;
		[SerializeField]
		public TMPro.TextMeshProUGUI EffectProbabilityText;
		[SerializeField]
		public UnityEngine.UI.Image BeforeRefine;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text;
		[SerializeField]
		public UnityEngine.UI.Image AfterRefine;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ReplaceBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress RefineBtn;
		[SerializeField]
		public RectTransform RefineConsume;
		[SerializeField]
		public UnityEngine.RectTransform WearEquipScroll;
		[SerializeField]
		public UnityEngine.UI.Button GoBackBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggleGroup ToggleGroup;
		
		private EquipUpGradePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Equip = null;
			EquipIcon = null;
			NameText = null;
			ForgingEffectHammer01 = null;
			ForgingEffectUpgrade01 = null;
			Body = null;
			UpGrade = null;
			Level = null;
			Attribute = null;
			UpGradeBtn = null;
			ExtraAttributes = null;
			Refine = null;
			EffectProbabilityText = null;
			BeforeRefine = null;
			Text = null;
			AfterRefine = null;
			ReplaceBtn = null;
			RefineBtn = null;
			RefineConsume = null;
			WearEquipScroll = null;
			GoBackBtn = null;
			ToggleGroup = null;
			
			mData = null;
		}
		
		public EquipUpGradePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		EquipUpGradePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new EquipUpGradePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
