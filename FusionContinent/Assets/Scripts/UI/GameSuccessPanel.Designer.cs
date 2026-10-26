using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:e9a023f5-78eb-41ed-bce7-d0b22cd4dcea
	public partial class GameSuccessPanel
	{
		public const string Name = "GameSuccessPanel";
		
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public UnityEngine.UI.Button BackBtn;
		[SerializeField]
		public UnityEngine.UI.Button NextBtn;
		
		private GameSuccessPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Content = null;
			BackBtn = null;
			NextBtn = null;
			
			mData = null;
		}
		
		public GameSuccessPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameSuccessPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameSuccessPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
