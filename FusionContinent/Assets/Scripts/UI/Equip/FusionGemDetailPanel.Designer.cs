using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:37a77367-45ad-4d44-8169-1d17d608054b
	public partial class FusionGemDetailPanel
	{
		public const string Name = "FusionGemDetailPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button BottomMask;
		[SerializeField]
		public UnityEngine.UI.Button Close;
		[SerializeField]
		public UnityEngine.UI.Image FusionGemQualityFrame;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock PropertyScrollView;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress UnWearBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WearBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress WashBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI GemNameText;
		[SerializeField]
		public TMPro.TextMeshProUGUI GemQualityText;
		[SerializeField]
		public UnityEngine.UI.Image GemIcon;
		[SerializeField]
		public UnityEngine.UI.Image GemIsWear;
		[SerializeField]
		public TMPro.TextMeshProUGUI BattlePoint;
		[SerializeField]
		public UnityEngine.UI.ScrollRect PartList;
		[SerializeField]
		public RectTransform PartContent;
		
		private FusionGemDetailPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BottomMask = null;
			Close = null;
			FusionGemQualityFrame = null;
			PropertyScrollView = null;
			UnWearBtn = null;
			WearBtn = null;
			WashBtn = null;
			GemNameText = null;
			GemQualityText = null;
			GemIcon = null;
			GemIsWear = null;
			BattlePoint = null;
			PartList = null;
			PartContent = null;
			
			mData = null;
		}
		
		public FusionGemDetailPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		FusionGemDetailPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new FusionGemDetailPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
