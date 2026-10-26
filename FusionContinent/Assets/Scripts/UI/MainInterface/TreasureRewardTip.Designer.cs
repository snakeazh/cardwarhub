using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:49ff64eb-7a9b-4e26-b174-309bb40842e4
	public partial class TreasureRewardTip
	{
		public const string Name = "TreasureRewardTip";
		
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
		
		private TreasureRewardTipData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Quit = null;
			TitleImg = null;
			RewardScroll = null;
			CloseAnimation = null;
			OpenAnimation = null;
			
			mData = null;
		}
		
		public TreasureRewardTipData Data
		{
			get
			{
				return mData;
			}
		}
		
		TreasureRewardTipData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new TreasureRewardTipData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
