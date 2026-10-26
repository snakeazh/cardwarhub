using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:3a613c9d-8685-4854-904b-fa5015dba25e
	public partial class BattlePassiveCardPanel
	{
		public const string Name = "BattlePassiveCardPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button PassiveCardObj;
		[SerializeField]
		public RectTransform PassiveCardDetail;
		[SerializeField]
		public UnityEngine.RectTransform LevelUp1;
		[SerializeField]
		public UnityEngine.UI.Text LevelUpLast;
		[SerializeField]
		public UnityEngine.UI.Text LevelUpNew;
		
		private BattlePassiveCardPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			PassiveCardObj = null;
			PassiveCardDetail = null;
			LevelUp1 = null;
			LevelUpLast = null;
			LevelUpNew = null;
			
			mData = null;
		}
		
		public BattlePassiveCardPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattlePassiveCardPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattlePassiveCardPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
