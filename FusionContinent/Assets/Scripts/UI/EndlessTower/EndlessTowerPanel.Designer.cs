using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:3ba63f4d-69ac-49bb-922f-6b8c8c1f1507
	public partial class EndlessTowerPanel
	{
		public const string Name = "EndlessTowerPanel";
		
		[SerializeField]
		public UnityEngine.UI.Image MapBg;
		[SerializeField]
		public TMPro.TextMeshProUGUI FloorText;
		[SerializeField]
		public UnityEngine.UI.Button RuleBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI CountdownText;
		[SerializeField]
		public UnityEngine.UI.Button RankingBtn;
		[SerializeField]
		public UnityEngine.UI.Button DailyBtn;
		[SerializeField]
		public UnityEngine.RectTransform Body;
		[SerializeField]
		public RectTransform NodeList;
		[SerializeField]
		public Animator IconAnim;
		[SerializeField]
		public UnityEngine.UI.Image TreasureChestIcon;
		[SerializeField]
		public RectTransform Progress;
		[SerializeField]
		public UnityEngine.UI.Image ProgressFull;
		[SerializeField]
		public TMPro.TextMeshProUGUI ProgressText;
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
		public UnityEngine.UI.Button BackBtn;
		
		private EndlessTowerPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			MapBg = null;
			FloorText = null;
			RuleBtn = null;
			CountdownText = null;
			RankingBtn = null;
			DailyBtn = null;
			Body = null;
			NodeList = null;
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
			BackBtn = null;
			
			mData = null;
		}
		
		public EndlessTowerPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		EndlessTowerPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new EndlessTowerPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
