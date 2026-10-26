using System;
using FusionContinent.UI.Components;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:19fb88af-897e-466d-97b3-f78cdf875262
	public partial class StageSweepingPanel
	{
		public const string Name = "StageSweepingPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		[SerializeField]
		public RectTransform Spine;
		[SerializeField]
		public RectTransform Head;
		[SerializeField]
		public RectTransform StageName;
		[SerializeField]
		public UnityEngine.UI.Slider SweepingProgress;
		[SerializeField]
		public RectTransform ItemUnit;
		[SerializeField]
		public RectTransform SweepingTime;
		[SerializeField]
		public RectTransform RewardsLayout;
		[SerializeField]
		public RectTransform Body;
		[SerializeField]
		public TMPro.TextMeshProUGUI MaxAFKTextDes;
		[SerializeField]
		public UnityEngine.UI.ScrollRect RewardScroll;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public UIPointerClickAndLongPress RewardEntity;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ReceiveBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI HarvestNum;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress HarvestBtn;
		[SerializeField]
		public UnityEngine.UI.Button MonthlyButton;
		
		private StageSweepingPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Mask = null;
			ExitBtn = null;
			Spine = null;
			Head = null;
			StageName = null;
			SweepingProgress = null;
			ItemUnit = null;
			SweepingTime = null;
			RewardsLayout = null;
			Body = null;
			MaxAFKTextDes = null;
			RewardScroll = null;
			Content = null;
			RewardEntity = null;
			ReceiveBtn = null;
			HarvestNum = null;
			HarvestBtn = null;
			MonthlyButton = null;
			
			mData = null;
		}
		
		public StageSweepingPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		StageSweepingPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new StageSweepingPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
