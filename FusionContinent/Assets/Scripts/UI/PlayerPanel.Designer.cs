using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:42049e01-8dc7-4811-b374-ae338b7ac411
	public partial class PlayerPanel
	{
		public const string Name = "PlayerPanel";
		
		private PlayerPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public PlayerPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		PlayerPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new PlayerPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
