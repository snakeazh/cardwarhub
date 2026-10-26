using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:dbeb3c06-c33b-4d9e-9d5e-1b0f89067c76
	public partial class SpeakWhellPanel
	{
		public const string Name = "SpeakWhellPanel";
		
		[SerializeField]
		public FusionContinent.Manager.RollingNumberManager CombatNum;
		[SerializeField]
		public TMPro.TextMeshProUGUI NameText;
		[SerializeField]
		public TMPro.TextMeshProUGUI EffectText;
		[SerializeField]
		public UnityEngine.UI.Image PlayerIcon;
		[SerializeField]
		public RectTransform WearEquipList;
		[SerializeField]
		public UnityEngine.UI.Button TalentBtn;
		[SerializeField]
		public UnityEngine.UI.Button SkinBtn;
		[SerializeField]
		public UnityEngine.UI.Button ArtifactBtn;
		[SerializeField]
		public UnityEngine.UI.Button HeroSeatBtn;
		[SerializeField]
		public UnityEngine.UI.Button AttributeBtn;
		[SerializeField]
		public UnityEngine.UI.Button EditNameBtn;
		[SerializeField]
		public RectTransform WearEquips;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress EquipUpGradeBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress EquipDecomposition;
		[SerializeField]
		public UnityEngine.UI.Toggle EquipBtn;
		[SerializeField]
		public UnityEngine.UI.Toggle FusionGemBtn;
		[SerializeField]
		public UnityEngine.UI.Toggle MaterialBtn;
		[SerializeField]
		public UnityEngine.UI.ScrollRect MaterialScrollView;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock FusionGemScrollView;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock EquipCardScrollView;
		
		private SpeakWhellPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CombatNum = null;
			NameText = null;
			EffectText = null;
			PlayerIcon = null;
			WearEquipList = null;
			TalentBtn = null;
			SkinBtn = null;
			ArtifactBtn = null;
			HeroSeatBtn = null;
			AttributeBtn = null;
			EditNameBtn = null;
			WearEquips = null;
			EquipUpGradeBtn = null;
			EquipDecomposition = null;
			EquipBtn = null;
			FusionGemBtn = null;
			MaterialBtn = null;
			MaterialScrollView = null;
			FusionGemScrollView = null;
			EquipCardScrollView = null;
			
			mData = null;
		}
		
		public SpeakWhellPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SpeakWhellPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SpeakWhellPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
