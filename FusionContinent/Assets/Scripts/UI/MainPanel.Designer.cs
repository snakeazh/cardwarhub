using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:b69eab37-9c1b-4010-aa12-a154892175ae
	public partial class MainPanel
	{
		public const string Name = "MainPanel";
		
		[SerializeField]
		public UnityEngine.RectTransform ChooseMenu;
		[SerializeField]
		public UnityEngine.UI.Button ContinueGameBtn;
		[SerializeField]
		public UnityEngine.UI.Button NewGameBtn;
		[SerializeField]
		public UnityEngine.UI.Button CardEncyclopediaBtn;
		[SerializeField]
		public UnityEngine.UI.Button SettingsBtn;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		[SerializeField]
		public UnityEngine.UI.Button DifficultyBtn;
		[SerializeField]
		public UnityEngine.UI.Text DifficultyText;
		
		private MainPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			ChooseMenu = null;
			ContinueGameBtn = null;
			NewGameBtn = null;
			CardEncyclopediaBtn = null;
			SettingsBtn = null;
			ExitBtn = null;
			DifficultyBtn = null;
			DifficultyText = null;
			
			mData = null;
		}
		
		public MainPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		MainPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MainPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
