using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:4bdd517d-ffd4-4da4-bac8-0d079d61a9d9
	public partial class ChallengeBossPanel
	{
		public const string Name = "ChallengeBossPanel";
		
		[SerializeField]
		public UnityEngine.UI.Image Title;
		[SerializeField]
		public UnityEngine.UI.Button Rules;
		[SerializeField]
		public UnityEngine.UI.Button RankingBtn;
		[SerializeField]
		public UnityEngine.UI.Button DailyBtn;
		[SerializeField]
		public UnityEngine.UI.Button ShopBtn;
		[SerializeField]
		public RectTransform Dragon;
		[SerializeField]
		public RectTransform Body;
		[SerializeField]
		public UnityEngine.UI.Image Accumulateddamage;
		[SerializeField]
		public UnityEngine.UI.Image BrownProgressBar;
		[SerializeField]
		public UnityEngine.UI.Image YellowProgressBar;
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
		public RectTransform RewardTip;
		[SerializeField]
		public UnityEngine.RectTransform RewardScroll;
		[SerializeField]
		public UnityEngine.UI.Button ChallengeBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI ChallengeNum;
		[SerializeField]
		public TMPro.TextMeshProUGUI MaxValue;
		[SerializeField]
		public RectTransform ShopPanel;
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public RectTransform shopitemparent;
		[SerializeField]
		public UnityEngine.UI.Image shopitembg;
		[SerializeField]
		public TMPro.TextMeshProUGUI shopnametxt;
		[SerializeField]
		public UnityEngine.UI.Image shopiconbg;
		[SerializeField]
		public UnityEngine.UI.Image shopicon;
		[SerializeField]
		public TMPro.TextMeshProUGUI shopnum;
		[SerializeField]
		public UnityEngine.UI.Button consumebtn;
		[SerializeField]
		public UnityEngine.UI.Image consumeicon;
		[SerializeField]
		public TMPro.TextMeshProUGUI consumenum;
		[SerializeField]
		public UnityEngine.UI.Image shopitembgmask;
		[SerializeField]
		public RectTransform shopitemTitleparent;
		[SerializeField]
		public UnityEngine.UI.Button ReturnBtn;
		
		private ChallengeBossPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Title = null;
			Rules = null;
			RankingBtn = null;
			DailyBtn = null;
			ShopBtn = null;
			Dragon = null;
			Body = null;
			Accumulateddamage = null;
			BrownProgressBar = null;
			YellowProgressBar = null;
			IconAnim = null;
			TreasureChestIcon = null;
			Progress = null;
			ProgressFull = null;
			ProgressText = null;
			TreasureChestTip = null;
			TreasureChestTipIcon = null;
			NextTreasureChestTipIcon = null;
			Tip = null;
			RewardTip = null;
			RewardScroll = null;
			ChallengeBtn = null;
			ChallengeNum = null;
			MaxValue = null;
			ShopPanel = null;
			Quit = null;
			shopitemparent = null;
			shopitembg = null;
			shopnametxt = null;
			shopiconbg = null;
			shopicon = null;
			shopnum = null;
			consumebtn = null;
			consumeicon = null;
			consumenum = null;
			shopitembgmask = null;
			shopitemTitleparent = null;
			ReturnBtn = null;
			
			mData = null;
		}
		
		public ChallengeBossPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ChallengeBossPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ChallengeBossPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
