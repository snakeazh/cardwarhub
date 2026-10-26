using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:bd96cb61-0692-4fa5-ad41-a2e2ad39bfbd
	public partial class GameGemPanel
	{
		public const string Name = "GameGemPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI TitleText;
		[SerializeField]
		public UnityEngine.UI.Image Fill;
		[SerializeField]
		public UnityEngine.UI.Image CurrentGemImage;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text;
		[SerializeField]
		public RectTransform Gem;
		[SerializeField]
		public Transform GemGameManager;
		[SerializeField]
		public RectTransform Board;
		[SerializeField]
		public UnityEngine.UI.Image StartGame;
		[SerializeField]
		public TMPro.TextMeshProUGUI NumText;
		[SerializeField]
		public UnityEngine.UI.Button StartGameButton;
		
		private GameGemPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseBtn = null;
			TitleText = null;
			Fill = null;
			CurrentGemImage = null;
			Text = null;
			Gem = null;
			GemGameManager = null;
			Board = null;
			StartGame = null;
			NumText = null;
			StartGameButton = null;
			
			mData = null;
		}
		
		public GameGemPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameGemPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameGemPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
