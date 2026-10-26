using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:8f546f4e-abd4-4ea2-86a2-a7a0f31c3461
	public partial class PlayerMainPanel
	{
		public const string Name = "PlayerMainPanel";
		
		[SerializeField]
		public RectTransform Change;
		
		private PlayerMainPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Change = null;
			
			mData = null;
		}
		
		public PlayerMainPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		PlayerMainPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new PlayerMainPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
