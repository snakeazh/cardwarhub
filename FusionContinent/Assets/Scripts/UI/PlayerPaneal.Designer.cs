using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:dbfd4595-da36-4b53-8867-e85ed89bb381
	public partial class PlayerPaneal
	{
		public const string Name = "PlayerPaneal";
		
		
		private PlayerPanealData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public PlayerPanealData Data
		{
			get
			{
				return mData;
			}
		}
		
		PlayerPanealData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new PlayerPanealData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
