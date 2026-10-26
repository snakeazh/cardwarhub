using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:4851be99-6ca1-4dea-8a10-267655b1fe45
	public partial class LoginPanel01
	{
		public const string Name = "LoginPanel01";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI VersionNum;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress EnterGameBtn;
		[SerializeField]
		public UnityEngine.UI.Toggle Toggle;
		[SerializeField]
		public FusionContinent.UI.DropDownBase ServiceDropdown;
		[SerializeField]
		public TMPro.TextMeshProUGUI SelectServerLabel;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress LanguageChangeBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress AccountChangeBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress CancelPanelBtn;
		
		private LoginPanel01Data mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			VersionNum = null;
			EnterGameBtn = null;
			Toggle = null;
			ServiceDropdown = null;
			SelectServerLabel = null;
			LanguageChangeBtn = null;
			AccountChangeBtn = null;
			CancelPanelBtn = null;
			
			mData = null;
		}
		
		public LoginPanel01Data Data
		{
			get
			{
				return mData;
			}
		}
		
		LoginPanel01Data mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LoginPanel01Data());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
