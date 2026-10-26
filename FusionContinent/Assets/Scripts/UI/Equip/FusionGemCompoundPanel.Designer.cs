using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:4382fce5-08ca-4991-b287-309656f1509f
	public partial class FusionGemCompoundPanel
	{
		public const string Name = "FusionGemCompoundPanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Image FusionIcon;
		[SerializeField]
		public TMPro.TextMeshProUGUI FusionName;
		[SerializeField]
		public RectTransform FusionGemRoot;
		[SerializeField]
		public UnityEngine.UI.Slider GemProgress;
		[SerializeField]
		public TMPro.TextMeshProUGUI GemStartLv;
		[SerializeField]
		public TMPro.TextMeshProUGUI GemEndLv;
		[SerializeField]
		public TMPro.TextMeshProUGUI FusionProbabilityText;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock FusionScrollView;
		[SerializeField]
		public UnityEngine.UI.Image ExitBtn;
		[SerializeField]
		public UnityEngine.UI.Button AdvancedBtn;
		
		private FusionGemCompoundPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			FusionIcon = null;
			FusionName = null;
			FusionGemRoot = null;
			GemProgress = null;
			GemStartLv = null;
			GemEndLv = null;
			FusionProbabilityText = null;
			FusionScrollView = null;
			ExitBtn = null;
			AdvancedBtn = null;
			
			mData = null;
		}
		
		public FusionGemCompoundPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		FusionGemCompoundPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new FusionGemCompoundPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
