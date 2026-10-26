using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:dc183c6d-619e-4e0a-b9e8-ea14b0e16c0d
	public partial class DailyRewardsPanel
	{
		public const string Name = "DailyRewardsPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public TMPro.TextMeshProUGUI TitleText;
		[SerializeField]
		public TMPro.TextMeshProUGUI TipText;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public UnityEngine.UI.Button ReceiveBtn;
		
		private DailyRewardsPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Quit = null;
			TitleText = null;
			TipText = null;
			Content = null;
			ReceiveBtn = null;
			
			mData = null;
		}
		
		public DailyRewardsPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		DailyRewardsPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new DailyRewardsPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
