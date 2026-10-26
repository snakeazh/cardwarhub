using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:5ea83502-c955-4db6-9545-6c79a3908ee5
	public partial class SkillUpGradePanel
	{
		public const string Name = "SkillUpGradePanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button QuitBtn;
		[SerializeField]
		public RectTransform UpGrade;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText CardLevelText;
		[SerializeField]
		public FusionContinent.CardSlot CardSlot;
		[SerializeField]
		public TMPro.TextMeshProUGUI SkillName;
		[SerializeField]
		public Assets.Scripts.Localization.LocalizationText DescText;
		[SerializeField]
		public UnityEngine.UI.ScrollRect SkillAttributeScroll;
		[SerializeField]
		public UnityEngine.RectTransform LevelDetail;
		[SerializeField]
		public RectTransform Consumes;
		[SerializeField]
		public UnityEngine.UI.Button ConfirmUpGradeBtn;
		[SerializeField]
		public Transform SkillUpgradeEffect01;
		
		private SkillUpGradePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			QuitBtn = null;
			UpGrade = null;
			CardLevelText = null;
			CardSlot = null;
			SkillName = null;
			DescText = null;
			SkillAttributeScroll = null;
			LevelDetail = null;
			Consumes = null;
			ConfirmUpGradeBtn = null;
			SkillUpgradeEffect01 = null;
			
			mData = null;
		}
		
		public SkillUpGradePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SkillUpGradePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SkillUpGradePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
