using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:8e927ca9-3ee9-4202-8e08-2901244cd8a4
	public partial class GameMainPanel
	{
		public const string Name = "GameMainPanel";
		
		[SerializeField]
		public UnityEngine.RectTransform Bottom;
		
		private GameMainPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Bottom = null;
			
			mData = null;
		}
		
		public GameMainPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameMainPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameMainPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
