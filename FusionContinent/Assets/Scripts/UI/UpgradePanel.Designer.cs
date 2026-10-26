using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:35467582-5a49-4ffc-82b7-116d2db5e56a
	public partial class UpgradePanel
	{
		public const string Name = "UpgradePanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelText;
		[SerializeField]
		public RectTransform UnlockList;
		[SerializeField]
		public UnityEngine.RectTransform List;
		
		private UpgradePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			ExitBtn = null;
			LevelText = null;
			UnlockList = null;
			List = null;
			
			mData = null;
		}
		
		public UpgradePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UpgradePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UpgradePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
