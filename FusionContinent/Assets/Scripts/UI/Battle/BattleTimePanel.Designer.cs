using System;
using FusionContinent.UI.Components;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:a3a5e9dc-d24a-4914-9302-33123542f871
	public partial class BattleTimePanel
	{
		public const string Name = "BattleTimePanel";
		
		[SerializeField]
		public UIPointerClickAndLongPress SpeedUpBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI speedtxt;
		[SerializeField]
		public UnityEngine.UI.Toggle AutoToggle;
		[SerializeField]
		public UIPointerClickAndLongPress PausedBtn;
		[SerializeField]
		public UnityEngine.UI.Button PlayBtn;
		[SerializeField]
		public UnityEngine.UI.Button SpeedDownBtn;
		[SerializeField]
		public UnityEngine.UI.Button OriginalSpeedBtn;
		[SerializeField]
		public RectTransform StageTop;
		[SerializeField]
		public TMPro.TextMeshProUGUI StageTitle;
		[SerializeField]
		public TMPro.TextMeshProUGUI CityLevelTxt;
		[SerializeField]
		public TMPro.TextMeshProUGUI TotalDamageText;
		
		private BattleTimePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			SpeedUpBtn = null;
			speedtxt = null;
			AutoToggle = null;
			PausedBtn = null;
			PlayBtn = null;
			SpeedDownBtn = null;
			OriginalSpeedBtn = null;
			StageTop = null;
			StageTitle = null;
			CityLevelTxt = null;
			TotalDamageText = null;
			
			mData = null;
		}
		
		public BattleTimePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattleTimePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattleTimePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
