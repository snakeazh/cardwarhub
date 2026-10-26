using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:40b4ba77-2b39-4307-b6f1-579ba9d4ee05
	public partial class GameFailurePanel
	{
		public const string Name = "GameFailurePanel";
		
		[SerializeField]
		public UnityEngine.UI.Button BackBtn;
		[SerializeField]
		public UnityEngine.UI.Button AgineBtn;
		[SerializeField]
		public UnityEngine.UI.Text RevivalText;
		[SerializeField]
		public UnityEngine.UI.Button RevivalBtn;
		
		private GameFailurePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BackBtn = null;
			AgineBtn = null;
			RevivalText = null;
			RevivalBtn = null;
			
			mData = null;
		}
		
		public GameFailurePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameFailurePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameFailurePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
