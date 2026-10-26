using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:43684da3-7208-4587-8b55-f80f2c60c09a
	public partial class BattleSkillsPanel
	{
		public const string Name = "BattleSkillsPanel";
		
		[SerializeField]
		public RectTransform BottomLand;
		[SerializeField]
		public UnityEngine.UI.Image Icon;
		[SerializeField]
		public TMPro.TextMeshProUGUI LevelText;
		[SerializeField]
		public RectTransform Effect;
		[SerializeField]
		public UnityEngine.UI.Button TableBtn;
		[SerializeField]
		public BoxCollider2D CardSlotR;
		[SerializeField]
		public BoxCollider2D CardSlotL;
		[SerializeField]
		public BoxCollider2D CardSlotD;
		[SerializeField]
		public BoxCollider2D CardSlotT;
		[SerializeField]
		public UnityEngine.UI.Button CardInfo;
		[SerializeField]
		public UnityEngine.RectTransform LevelUp1;
		[SerializeField]
		public UnityEngine.UI.Text LevelUpLast;
		[SerializeField]
		public UnityEngine.UI.Text LevelUpNew;
		[SerializeField]
		public Transform BottomClick;
		[SerializeField]
		public Transform CardSlot;
		
		private BattleSkillsPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BottomLand = null;
			Icon = null;
			LevelText = null;
			Effect = null;
			TableBtn = null;
			CardSlotR = null;
			CardSlotL = null;
			CardSlotD = null;
			CardSlotT = null;
			CardInfo = null;
			LevelUp1 = null;
			LevelUpLast = null;
			LevelUpNew = null;
			BottomClick = null;
			CardSlot = null;
			
			mData = null;
		}
		
		public BattleSkillsPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattleSkillsPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattleSkillsPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
