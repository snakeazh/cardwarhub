using System;
using FusionContinent.UI.Components;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:86250cfa-2c0e-4968-aedc-638b9d29d7b1
	public partial class BattleInterPanel
	{
		public const string Name = "BattleInterPanel";
		
		[SerializeField]
		public UnityEngine.UI.Image GoldBar;
		[SerializeField]
		public Transform CoinGainEffect01;
		[SerializeField]
		public Transform CoinGainEffect02;
		[SerializeField]
		public TMPro.TextMeshProUGUI GoldText;
		[SerializeField]
		public UIPointerClickAndLongPress BuySkillBtn;
		[SerializeField]
		public UnityEngine.UI.Button BuySchoolBtn;
		[SerializeField]
		public UIPointerClickAndLongPress BuyExpBtn;
		[SerializeField]
		public RectTransform Combo;
		[SerializeField]
		public UnityEngine.Animator SkillShowSpine;
		[SerializeField]
		public Transform AnnaSpineEffect02;
		[SerializeField]
		public RectTransform SkillShowContent;
		[SerializeField]
		public TMPro.TextMeshProUGUI TimerNum;
		[SerializeField]
		public GameObject BuySkillUseGameObject;
		[SerializeField]
		public GameObject BuyExpUseGameObject;
		[SerializeField]
		public GameObject BuySkillNoUseGameObject;
		[SerializeField]
		public GameObject BuyExpNoUseGameObject;

		
		private BattleInterPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			GoldBar = null;
			CoinGainEffect01 = null;
			CoinGainEffect02 = null;
			GoldText = null;
			BuySkillBtn = null;
			BuySchoolBtn = null;
			BuyExpBtn = null;
			Combo = null;
			SkillShowSpine = null;
			AnnaSpineEffect02 = null;
			SkillShowContent = null;
			TimerNum = null;
			
			mData = null;
		}
		
		public BattleInterPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattleInterPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattleInterPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
