using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:2a4a627c-244c-4fd0-ab97-fa95e50f67ad
	public partial class LoginAccountPanel
	{
		public const string Name = "LoginAccountPanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Mask;
		[SerializeField]
		public TMPro.TMP_InputField AccountField;
		[SerializeField]
		public TMPro.TMP_InputField PasswordField;
		[SerializeField]
		public UICommonBtn LoginBtn;
		
		private LoginAccountPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			Mask = null;
			AccountField = null;
			PasswordField = null;
			LoginBtn = null;
			
			mData = null;
		}
		
		public LoginAccountPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		LoginAccountPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LoginAccountPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
