using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:28b1b8f1-9c68-4136-864e-4c62571151db
	public partial class SoldierPanel
	{
		public const string Name = "SoldierPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public RectTransform Soldier;
		[SerializeField]
		public UnityEngine.UI.Image BigIcon;
		[SerializeField]
		public TMPro.TextMeshProUGUI SoldierName;
		[SerializeField]
		public TMPro.TextMeshProUGUI SoldierName2;
		[SerializeField]
		public UnityEngine.UI.Image MainAttrTypeIcon;
		[SerializeField]
		public RectTransform Spine;
		[SerializeField]
		public UnityEngine.UI.Image TypeIcon;
		[SerializeField]
		public RectTransform GuideNode;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelText;
		[SerializeField]
		public RectTransform SkillModule;
		[SerializeField]
		public RectTransform Skill;
		[SerializeField]
		public UnityEngine.UI.Button MainAttributes;
		[SerializeField]
		public RectTransform UpGrade;
		[SerializeField]
		public RectTransform Consumes;
		[SerializeField]
		public UnityEngine.UI.Button UpGradeBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI UpgradeTips;
		[SerializeField]
		public RectTransform ExpProgressBar;
		[SerializeField]
		public UnityEngine.UI.Button PlayerCoreAttr;
		[SerializeField]
		public TMPro.TextMeshProUGUI IsFullLevel;
		[SerializeField]
		public RectTransform BringBattleSkillSlot;
		[SerializeField]
		public Transform CharacterUpgradeEffect01;
		[SerializeField]
		public RectTransform CharacterUpgradeEffect02;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public UnityEngine.UI.Button SkinBtn;
		[SerializeField]
		public UnityEngine.UI.Button AwakeBtn;
		[SerializeField]
		public UnityEngine.UI.Button SoldierBtn;
		[SerializeField]
		public UnityEngine.UI.Button EquipBtn;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		
		private SoldierPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			Soldier = null;
			BigIcon = null;
			SoldierName = null;
			SoldierName2 = null;
			MainAttrTypeIcon = null;
			Spine = null;
			TypeIcon = null;
			GuideNode = null;
			LevelText = null;
			SkillModule = null;
			Skill = null;
			MainAttributes = null;
			UpGrade = null;
			Consumes = null;
			UpGradeBtn = null;
			UpgradeTips = null;
			ExpProgressBar = null;
			PlayerCoreAttr = null;
			IsFullLevel = null;
			BringBattleSkillSlot = null;
			CharacterUpgradeEffect01 = null;
			CharacterUpgradeEffect02 = null;
			CloseBtn = null;
			SkinBtn = null;
			AwakeBtn = null;
			SoldierBtn = null;
			EquipBtn = null;
			CloseAnimation = null;
			OpenAnimation = null;
			
			mData = null;
		}
		
		public SoldierPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SoldierPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SoldierPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
