using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:fa445343-6c7d-477c-9953-43863c9147ff
	public partial class RewardTips
	{
		public const string Name = "RewardTips";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress CloseBtn;
		[SerializeField]
		public RectTransform Reward;
		[SerializeField]
		public RectTransform Title;
		[SerializeField]
		public ScrollView.UIVerticalScrollView RewardScrollView;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public TMPro.TextMeshProUGUI TipText;
		
		private RewardTipsData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			CloseBtn = null;
			Reward = null;
			Title = null;
			RewardScrollView = null;
			Content = null;
			TipText = null;
			
			mData = null;
		}
		
		public RewardTipsData Data
		{
			get
			{
				return mData;
			}
		}
		
		RewardTipsData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new RewardTipsData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
