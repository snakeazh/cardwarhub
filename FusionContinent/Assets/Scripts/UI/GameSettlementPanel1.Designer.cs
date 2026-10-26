using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:2dc78ac8-2c5d-4ed0-8352-ab3c17411863
	public partial class GameSettlementPanel1
	{
		public const string Name = "GameSettlementPanel1";
		
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText Title;
		[SerializeField]
		public UnityEngine.UI.Image BgImg;
		[SerializeField]
		public RectTransform StageBg;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText StageName;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress AgineBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress DoubleRewardBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress BackBtn;
		[SerializeField]
		public RectTransform HeadBox;
		[SerializeField]
		public UnityEngine.UI.Image ExpBoxBg;
		[SerializeField]
		public UnityEngine.RectTransform ExpStatistics;
		[SerializeField]
		public UnityEngine.UI.Image StatisticsBoxBg;
		[SerializeField]
		public UnityEngine.RectTransform Statistics;
		[SerializeField]
		public UnityEngine.UI.Image RewardBoxBg;
		[SerializeField]
		public UnityEngine.RectTransform Rewards;
		
		private GameSettlementPanel1Data mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Title = null;
			BgImg = null;
			StageBg = null;
			StageName = null;
			AgineBtn = null;
			DoubleRewardBtn = null;
			BackBtn = null;
			HeadBox = null;
			ExpBoxBg = null;
			ExpStatistics = null;
			StatisticsBoxBg = null;
			Statistics = null;
			RewardBoxBg = null;
			Rewards = null;
			
			mData = null;
		}
		
		public GameSettlementPanel1Data Data
		{
			get
			{
				return mData;
			}
		}
		
		GameSettlementPanel1Data mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameSettlementPanel1Data());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
