using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:d3706029-4880-4bae-a04b-d02db925697c
	public partial class GamePausePanel
	{
		public const string Name = "GamePausePanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI Title;
		[SerializeField]
		public UnityEngine.UI.ScrollRect SchoolCardScroll;
		[SerializeField]
		public UnityEngine.UI.ScrollRect RewardCardScroll;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress SchoolBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress RewardBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress BackBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ContineBtn;
		
		private GamePausePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Title = null;
			SchoolCardScroll = null;
			RewardCardScroll = null;
			SchoolBtn = null;
			RewardBtn = null;
			BackBtn = null;
			ContineBtn = null;
			
			mData = null;
		}
		
		public GamePausePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GamePausePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GamePausePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
