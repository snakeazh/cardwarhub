using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:2a0dc8d4-bb3c-4e62-b83c-1fedd5db127c
	public partial class MainCityStrengthenTips
	{
		public const string Name = "MainCityStrengthenTips";
		
		[SerializeField]
		public UnityEngine.UI.Button Bg;
		[SerializeField]
		public UnityEngine.UI.Image MainCityIcon;
		[SerializeField]
		public UnityEngine.UI.Text LastMaxHpText;
		[SerializeField]
		public UnityEngine.UI.Text NowMaxHpText;
		[SerializeField]
		public UnityEngine.UI.Text LastDefenseText;
		[SerializeField]
		public UnityEngine.UI.Text NowDefenseText;
		[SerializeField]
		public UnityEngine.UI.Text LastMaxPopulationText;
		[SerializeField]
		public UnityEngine.UI.Text NowMaxPopulationText;
		[SerializeField]
		public UnityEngine.UI.Text LastRetaliationText;
		[SerializeField]
		public UnityEngine.UI.Text NowRetaliationText;
		[SerializeField]
		public UnityEngine.UI.Text LastBuildingTimeText;
		[SerializeField]
		public UnityEngine.UI.Text NowBuildingTimeText;
		
		private MainCityStrengthenTipsData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Bg = null;
			MainCityIcon = null;
			LastMaxHpText = null;
			NowMaxHpText = null;
			LastDefenseText = null;
			NowDefenseText = null;
			LastMaxPopulationText = null;
			NowMaxPopulationText = null;
			LastRetaliationText = null;
			NowRetaliationText = null;
			LastBuildingTimeText = null;
			NowBuildingTimeText = null;
			
			mData = null;
		}
		
		public MainCityStrengthenTipsData Data
		{
			get
			{
				return mData;
			}
		}
		
		MainCityStrengthenTipsData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MainCityStrengthenTipsData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
