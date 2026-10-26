using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:aea1a25a-f2a3-4730-89ea-c248da22916f
	public partial class MainInterfacePanel
	{
		public const string Name = "MainInterfacePanel";
		
		[SerializeField]
		public RectTransform OnSelectAnimation;
		[SerializeField]
		public RectTransform ChangeStageAnimation;
		[SerializeField]
		public RectTransform PlayerQueue;
		[SerializeField]
		public UnityEngine.RectTransform SwipeArea;
		[SerializeField]
		public RectTransform LeftList;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress FirstRechargeBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ActivityBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress SingInBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress PassCheck;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress MonthCardBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress FundCashback;
		[SerializeField]
		public UnityEngine.RectTransform RightListBg;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress RankingBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress SettingBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress BulletinBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress FriendBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress MailBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress TaskBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress BackpackBtn;
		[SerializeField]
		public UnityEngine.RectTransform StageInfo;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText StageChapterNameText;
		[SerializeField]
		public TMPro.TextMeshProUGUI StageNameText;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress HelpBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress LevelDetailBtn;
		[SerializeField]
		public RectTransform StageDifficulty;
		[SerializeField]
		public UnityEngine.RectTransform StageChests;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress StageLeftBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress StageRightBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress PlayGameBtn;
		[SerializeField]
		public RectTransform StartText;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress StageAfkBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI AfkText;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText AfkProgressText;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ChatPanel;
		[SerializeField]
		public RectTransform ChatDetail1;
		[SerializeField]
		public RectTransform ChatDetail2;
		
		private MainInterfacePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OnSelectAnimation = null;
			ChangeStageAnimation = null;
			PlayerQueue = null;
			SwipeArea = null;
			LeftList = null;
			FirstRechargeBtn = null;
			ActivityBtn = null;
			SingInBtn = null;
			PassCheck = null;
			MonthCardBtn = null;
			FundCashback = null;
			RightListBg = null;
			RankingBtn = null;
			SettingBtn = null;
			BulletinBtn = null;
			FriendBtn = null;
			MailBtn = null;
			TaskBtn = null;
			BackpackBtn = null;
			StageInfo = null;
			StageChapterNameText = null;
			StageNameText = null;
			HelpBtn = null;
			LevelDetailBtn = null;
			StageDifficulty = null;
			StageChests = null;
			StageLeftBtn = null;
			StageRightBtn = null;
			PlayGameBtn = null;
			StartText = null;
			StageAfkBtn = null;
			AfkText = null;
			AfkProgressText = null;
			ChatPanel = null;
			ChatDetail1 = null;
			ChatDetail2 = null;
			
			mData = null;
		}
		
		public MainInterfacePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		MainInterfacePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MainInterfacePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
