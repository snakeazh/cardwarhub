using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:c63354dc-e07d-4f0f-bc14-31c353eec2ec
	public partial class MonsterAtlasPanel
	{
		public const string Name = "MonsterAtlasPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock CardScrollview;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle NormalToggle;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle EliteToggle;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle BossToggle;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI Tip;
		
		private MonsterAtlasPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Mask = null;
			CardScrollview = null;
			NormalToggle = null;
			EliteToggle = null;
			BossToggle = null;
			CloseBtn = null;
			Tip = null;
			
			mData = null;
		}
		
		public MonsterAtlasPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		MonsterAtlasPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MonsterAtlasPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
