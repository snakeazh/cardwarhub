using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:e1781b71-e99c-4f67-92e2-a6dd68c489e8
	public partial class DevelopRewards
	{
		public const string Name = "DevelopRewards";
		
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock RewardScroll;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		
		private DevelopRewardsData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			RewardScroll = null;
			Content = null;
			CloseAnimation = null;
			OpenAnimation = null;
			
			mData = null;
		}
		
		public DevelopRewardsData Data
		{
			get
			{
				return mData;
			}
		}
		
		DevelopRewardsData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new DevelopRewardsData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
