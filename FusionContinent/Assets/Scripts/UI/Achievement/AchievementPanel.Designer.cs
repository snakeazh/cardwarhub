using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:49fb90f7-8f07-4e60-9da8-ce31a926874b
	public partial class AchievementPanel
	{
		public const string Name = "AchievementPanel";
		
		[SerializeField]
		public ScrollView.UIHorizontalScrollViewLock ActivityLevelScrollView;
		[SerializeField]
		public Animator IconAnim;
		[SerializeField]
		public UnityEngine.UI.Image TreasureChestIcon;
		[SerializeField]
		public RectTransform Progress;
		[SerializeField]
		public UnityEngine.UI.Image ProgressFull;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText ProgressText;
		[SerializeField]
		public RectTransform TreasureChestTip;
		[SerializeField]
		public UnityEngine.UI.Image TreasureChestTipIcon;
		[SerializeField]
		public UnityEngine.UI.Image NextTreasureChestTipIcon;
		[SerializeField]
		public TMPro.TextMeshProUGUI Tip;
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public UnityEngine.UI.Image TitleImg;
		[SerializeField]
		public UnityEngine.RectTransform RewardScroll;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText CurrentLevelText;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock DisplayScrollView;
		[SerializeField]
		public UnityEngine.UI.Button ReturnBtn;
		
		private AchievementPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			ActivityLevelScrollView = null;
			IconAnim = null;
			TreasureChestIcon = null;
			Progress = null;
			ProgressFull = null;
			ProgressText = null;
			TreasureChestTip = null;
			TreasureChestTipIcon = null;
			NextTreasureChestTipIcon = null;
			Tip = null;
			Quit = null;
			TitleImg = null;
			RewardScroll = null;
			CloseAnimation = null;
			OpenAnimation = null;
			CurrentLevelText = null;
			DisplayScrollView = null;
			ReturnBtn = null;
			
			mData = null;
		}
		
		public AchievementPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		AchievementPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new AchievementPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
