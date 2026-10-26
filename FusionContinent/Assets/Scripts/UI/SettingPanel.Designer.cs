using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:f0048d07-ee3b-4f46-a13d-2bb538c520fe
	public partial class SettingPanel
	{
		public const string Name = "SettingPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public PlayerHead PlayerHead;
		[SerializeField]
		public TMPro.TextMeshProUGUI UserName;
		[SerializeField]
		public TMPro.TextMeshProUGUI UserId;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ChanNameBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress CopyUIDBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI ScoreNum;
		[SerializeField]
		public TMPro.TextMeshProUGUI StageName;
		[SerializeField]
		public UnityEngine.UI.Button MusicBtn;
		[SerializeField]
		public UnityEngine.UI.Button SoundBtn;
		[SerializeField]
		public TMPro.TMP_Dropdown LanguageDropdown;
		[SerializeField]
		public UnityEngine.UI.Button FeedbackBtn;
		[SerializeField]
		public UnityEngine.UI.Button LogOutBtn;
		[SerializeField]
		public UnityEngine.UI.Button GiftCodeBtn;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		
		private SettingPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Quit = null;
			PlayerHead = null;
			UserName = null;
			UserId = null;
			ChanNameBtn = null;
			CopyUIDBtn = null;
			ScoreNum = null;
			StageName = null;
			MusicBtn = null;
			SoundBtn = null;
			LanguageDropdown = null;
			FeedbackBtn = null;
			LogOutBtn = null;
			GiftCodeBtn = null;
			CloseBtn = null;
			
			mData = null;
		}
		
		public SettingPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SettingPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SettingPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
