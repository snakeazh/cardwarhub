using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:9acfd0ec-46db-46e8-9e9a-04a22580e94a
	public partial class SoldierMainPanel
	{
		public const string Name = "SoldierMainPanel";
		
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock SoldierCardScroll;
		[SerializeField]
		public RectTransform SoldierCardFilter;
		[SerializeField]
		public UnityEngine.UI.Image CardNum;
		[SerializeField]
		public TMPro.TMP_Dropdown SoldierCardDropdown;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI DamageLevelText;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress DetailBtn;
		
		private SoldierMainPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			SoldierCardScroll = null;
			SoldierCardFilter = null;
			CardNum = null;
			SoldierCardDropdown = null;
			CloseBtn = null;
			DamageLevelText = null;
			DetailBtn = null;
			
			mData = null;
		}
		
		public SoldierMainPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SoldierMainPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SoldierMainPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
