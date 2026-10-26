using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:c771c8a2-56c9-4976-a432-bf516d584e10
	public partial class StationPanel
	{
		public const string Name = "StationPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		[SerializeField]
		public RectTransform Body;
		[SerializeField]
		public UnityEngine.UI.ScrollRect RewardScroll;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public RectTransform NextStationEntity;
		[SerializeField]
		public RewardEntity RewardEntity;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ReceiveBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI HarvestNum;
		
		private StationPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Mask = null;
			ExitBtn = null;
			Body = null;
			RewardScroll = null;
			Content = null;
			NextStationEntity = null;
			RewardEntity = null;
			ReceiveBtn = null;
			HarvestNum = null;
			
			mData = null;
		}
		
		public StationPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		StationPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new StationPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
