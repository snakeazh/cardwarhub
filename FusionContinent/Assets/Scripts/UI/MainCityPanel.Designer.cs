using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:2b240042-d8bf-4bac-969c-4fa422ecb13e
	public partial class MainCityPanel
	{
		public const string Name = "MainCityPanel";
		
		
		private MainCityPanelData mPrivateData = null;
		[SerializeField]
		public UnityEngine.Transform Change;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public MainCityPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		MainCityPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MainCityPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
