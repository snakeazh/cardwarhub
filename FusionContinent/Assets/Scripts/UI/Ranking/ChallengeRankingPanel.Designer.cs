using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:a7c060d4-bcc2-4a9f-b235-cbc8b6fc2fd6
	public partial class ChallengeRankingPanel
	{
		public const string Name = "ChallengeRankingPanel";
		
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock RankSCView;
		[SerializeField]
		public UnityEngine.RectTransform NotInRank;
		[SerializeField]
		public TMPro.TextMeshProUGUI RankNum;
		[SerializeField]
		public PlayerHead PlayerHead;
		[SerializeField]
		public TMPro.TextMeshProUGUI PlayerName;
		[SerializeField]
		public TMPro.TextMeshProUGUI DetailData;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ExitBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggleGroup ToggleGroup;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle FightPowerRanking;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle StageRanking;
		[SerializeField]
		public FusionContinent.UI.Components.UISelectableToggle RealmRanking;
		
		private ChallengeRankingPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			RankSCView = null;
			NotInRank = null;
			RankNum = null;
			PlayerHead = null;
			PlayerName = null;
			DetailData = null;
			ExitBtn = null;
			ToggleGroup = null;
			FightPowerRanking = null;
			StageRanking = null;
			RealmRanking = null;
			
			mData = null;
		}
		
		public ChallengeRankingPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ChallengeRankingPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ChallengeRankingPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
