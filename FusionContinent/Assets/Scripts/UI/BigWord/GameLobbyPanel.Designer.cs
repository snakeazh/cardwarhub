using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:b9649ead-a1b1-4462-821c-136c0ab84e16
	public partial class GameLobbyPanel
	{
		public const string Name = "GameLobbyPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress GoldDig;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress JewelMaze;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress SecretAdventure;
		
		private GameLobbyPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			GoldDig = null;
			JewelMaze = null;
			SecretAdventure = null;
			
			mData = null;
		}
		
		public GameLobbyPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameLobbyPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameLobbyPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
