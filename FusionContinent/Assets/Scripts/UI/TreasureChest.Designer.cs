using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:d750c89d-f022-4530-a90e-93a340b8aeb4
	public partial class TreasureChest
	{
		public const string Name = "TreasureChest";
		
		[SerializeField]
		public TreasureChestAnimation IconAnim;
		[SerializeField]
		public UnityEngine.UI.Image TreasureChestIcon;
		[SerializeField]
		public RectTransform Progress;
		[SerializeField]
		public UnityEngine.UI.Image ProgressFull;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText ProgressText;
		[SerializeField]
		public RectTransform TreasureChestTip;
		[SerializeField]
		public UnityEngine.UI.Image TreasureChestTipIcon;
		[SerializeField]
		public UnityEngine.UI.Image NextTreasureChestTipIcon;
		[SerializeField]
		public TMPro.TextMeshProUGUI Tip;
		
		private TreasureChestData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			IconAnim = null;
			TreasureChestIcon = null;
			Progress = null;
			ProgressFull = null;
			ProgressText = null;
			TreasureChestTip = null;
			TreasureChestTipIcon = null;
			NextTreasureChestTipIcon = null;
			Tip = null;
			
			mData = null;
		}
		
		public TreasureChestData Data
		{
			get
			{
				return mData;
			}
		}
		
		TreasureChestData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new TreasureChestData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
