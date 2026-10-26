using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:0d3e20e8-c8ed-424c-aa15-16ebedf3443e
	public partial class RotaryTablePanel
	{
		public const string Name = "RotaryTablePanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText NameText;
		[SerializeField]
		public TMPro.TextMeshProUGUI ScoreText;
		[SerializeField]
		public Transform CoinGainEffect01;
		[SerializeField]
		public UnityEngine.UI.Button DrawBtn;
		[SerializeField]
		public RectTransform SpriteParent;
		[SerializeField]
		public RectTransform HaloParent;
		
		private RotaryTablePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Quit = null;
			NameText = null;
			ScoreText = null;
			CoinGainEffect01 = null;
			DrawBtn = null;
			SpriteParent = null;
			HaloParent = null;
			
			mData = null;
		}
		
		public RotaryTablePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		RotaryTablePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new RotaryTablePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
