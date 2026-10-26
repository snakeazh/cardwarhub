using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:bfa0e755-d79b-4e28-8eb5-3c190856d581
	public partial class LevelUpTipPanel
	{
		public const string Name = "LevelUpTipPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button QuitBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI Leveltxt;
		[SerializeField]
		public RectTransform NewFunOpen;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public RectTransform FunObj;
		
		private LevelUpTipPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			QuitBtn = null;
			Leveltxt = null;
			NewFunOpen = null;
			Content = null;
			FunObj = null;
			
			mData = null;
		}
		
		public LevelUpTipPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		LevelUpTipPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LevelUpTipPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
