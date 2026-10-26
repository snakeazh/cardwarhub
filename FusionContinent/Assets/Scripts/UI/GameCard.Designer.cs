using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:898b63af-98cb-435b-b5f9-5315c54311ab
	public partial class GameCard
	{
		public const string Name = "GameCard";
		
		[SerializeField]
		public FusionContinent.AttackRangeDetector AttackRange;
		[SerializeField]
		public UnityEngine.UI.Image Icon;
		[SerializeField]
		public UnityEngine.RectTransform Frame;
		[SerializeField]
		public UnityEngine.UI.Image Attack;
		[SerializeField]
		public UnityEngine.UI.Text AttackText;
		[SerializeField]
		public UnityEngine.UI.Image HP;
		[SerializeField]
		public UnityEngine.UI.Text HpText;
		[SerializeField]
		public UnityEngine.UI.Text LevelText;
		[SerializeField]
		public UnityEngine.UI.Text NameText;
		[SerializeField]
		public UnityEngine.UI.Image ProgressBar;
		[SerializeField]
		public UnityEngine.UI.Image Progress;
		[SerializeField]
		public UnityEngine.UI.Image ExpBar;
		[SerializeField]
		public UnityEngine.UI.Image Exp;
		[SerializeField]
		public UnityEngine.RectTransform LevelUp;
		[SerializeField]
		public UnityEngine.UI.Image Broken;
		
		private GameCardData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			AttackRange = null;
			Icon = null;
			Frame = null;
			Attack = null;
			AttackText = null;
			HP = null;
			HpText = null;
			LevelText = null;
			NameText = null;
			ProgressBar = null;
			Progress = null;
			ExpBar = null;
			Exp = null;
			LevelUp = null;
			Broken = null;
			
			mData = null;
		}
		
		public GameCardData Data
		{
			get
			{
				return mData;
			}
		}
		
		GameCardData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GameCardData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
