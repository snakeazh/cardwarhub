using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:1c3ec0db-0430-492e-8659-aceeac11d9c7
	public partial class GameCardTips
	{
		public const string Name = "GameCardTips";
		
		[SerializeField]
		public UnityEngine.UI.Image BG;
		[SerializeField]
		public UnityEngine.UI.Text CardName;
		[SerializeField]
		public UnityEngine.UI.Text Description;
		
		private GameCardTipsData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BG = null;
			CardName = null;
			Description = null;
			
			mData = null;
		}
		
		public GameCardTipsData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameCardTipsData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameCardTipsData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
