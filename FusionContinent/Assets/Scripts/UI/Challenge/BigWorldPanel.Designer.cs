using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:697674db-4598-4234-b36a-120bb4e5a9e2
	public partial class BigWorldPanel
	{
		public const string Name = "BigWorldPanel";
		
		[SerializeField]
		public RectTransform TweenTarget;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress EndlessTowerBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress MonsterNotesBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress StationBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress RankingBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ChallengeBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress GameLobbyBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ArenaBtn;
		
		private BigWorldPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TweenTarget = null;
			EndlessTowerBtn = null;
			MonsterNotesBtn = null;
			StationBtn = null;
			RankingBtn = null;
			ChallengeBtn = null;
			GameLobbyBtn = null;
			ArenaBtn = null;
			
			mData = null;
		}
		
		public BigWorldPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BigWorldPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BigWorldPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
