using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:08fd14e7-4c13-4607-ab7b-b42d3eaf1724
	public partial class MonsterAtlasDetailPanel
	{
		public const string Name = "MonsterAtlasDetailPanel";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public UnityEngine.UI.Image MonsterLineArt;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public RectTransform MonsterSpine;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText MonsterNameText;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText DescText;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText AbilityDescText;
		[SerializeField]
		public ScrollView.UIVerticalScrollViewLock AttrScrollview;
		[SerializeField]
		public UnityEngine.UI.ScrollRect RewardScrollView;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public UnityEngine.UI.Button ReceiveBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI IsReceived;
		
		private MonsterAtlasDetailPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			Mask = null;
			MonsterLineArt = null;
			CloseBtn = null;
			MonsterSpine = null;
			MonsterNameText = null;
			DescText = null;
			AbilityDescText = null;
			AttrScrollview = null;
			RewardScrollView = null;
			Content = null;
			ReceiveBtn = null;
			IsReceived = null;
			
			mData = null;
		}
		
		public MonsterAtlasDetailPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		MonsterAtlasDetailPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MonsterAtlasDetailPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
