using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:1780f835-8c54-4a3e-8575-41ca2d1f809a
	public partial class CardGroupPanel
	{
		public const string Name = "CardGroupPanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelNameText;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelNameTextNum;
		[SerializeField]
		public RectTransform CardConfigPanel;
		[SerializeField]
		public RectTransform CardLayout;
		[SerializeField]
		public UnityEngine.UI.Button ClickArea;
		[SerializeField]
		public UnityEngine.RectTransform ChooseHero;
		[SerializeField]
		public RectTransform SoliderContent;
		[SerializeField]
		public UnityEngine.UI.Button SynergyBg;
		[SerializeField]
		public UnityEngine.RectTransform ChooseSkillCard;
		[SerializeField]
		public RectTransform SkillContent;
		[SerializeField]
		public UnityEngine.RectTransform ChooseGeneralCard;
		[SerializeField]
		public RectTransform GeneralContent;
		[SerializeField]
		public FusionContinent.Manager.RollingNumberManager PowerNumText;
		[SerializeField]
		public UnityEngine.UI.Button RecommandCardBtn;
		[SerializeField]
		public BattleInfoPanel BattleInfoPanel;
		[SerializeField]
		public UnityEngine.UI.Toggle BattleBtn;
		[SerializeField]
		public UnityEngine.UI.Toggle CardBtn;
		[SerializeField]
		public ChooseHeroPanel ChooseHeroPanel;
		[SerializeField]
		public ChooseSkillPanel ChooseSkillCardPanel;
		[SerializeField]
		public ChooseGeneralSkillPanel ChooseGeneralCardPanel;
		[SerializeField]
		public UnityEngine.UI.Button ReturnBtn;
		[SerializeField]
		public UnityEngine.UI.Button StartGameBtn;
		[SerializeField]
		public RectTransform TipDetail;
		[SerializeField]
		public TMPro.TMP_Dropdown Dropdown;
		[SerializeField]
		public RectTransform SpineCache;
		
		private CardGroupPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			LevelNameText = null;
			LevelNameTextNum = null;
			CardConfigPanel = null;
			CardLayout = null;
			ClickArea = null;
			ChooseHero = null;
			SoliderContent = null;
			SynergyBg = null;
			ChooseSkillCard = null;
			SkillContent = null;
			ChooseGeneralCard = null;
			GeneralContent = null;
			PowerNumText = null;
			RecommandCardBtn = null;
			BattleInfoPanel = null;
			BattleBtn = null;
			CardBtn = null;
			ChooseHeroPanel = null;
			ChooseSkillCardPanel = null;
			ChooseGeneralCardPanel = null;
			ReturnBtn = null;
			StartGameBtn = null;
			TipDetail = null;
			Dropdown = null;
			SpineCache = null;
			
			mData = null;
		}
		
		public CardGroupPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		CardGroupPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CardGroupPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
