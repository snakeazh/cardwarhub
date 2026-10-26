using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:0f9d8803-8235-400a-9bec-83a89e923287
	public partial class ChestRollPanel
	{
		public const string Name = "ChestRollPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public RectTransform Content;
		
		private ChestRollPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Quit = null;
			Content = null;
			
			mData = null;
		}
		
		public ChestRollPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ChestRollPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ChestRollPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
