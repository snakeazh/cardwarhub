using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:6b4cf33e-410f-4443-8833-0514318cb7cb
	public partial class TalentPanel
	{
		public const string Name = "TalentPanel";
		
		[SerializeField]
		public RectTransform Talent;
		[SerializeField]
		public UnityEngine.UI.Image TalentIcon1;
		[SerializeField]
		public LevelSliderController SkillShowScroll;
		[SerializeField]
		public UnityEngine.UI.Button CardAtlasBtn;
		[SerializeField]
		public UnityEngine.UI.Button RefBtn;
		[SerializeField]
		public UnityEngine.UI.Button GoBattleBtn;
		[SerializeField]
		public TalentIntro TalentIntro;
		[SerializeField]
		public Talentshow Talentshow;
		[SerializeField]
		public UnityEngine.UI.Button QuitPanel;
		[SerializeField]
		public UnityEngine.UI.Image talentdetailsicon;
		[SerializeField]
		public UnityEngine.UI.Image detailsIconbg;
		[SerializeField]
		public TMPro.TextMeshProUGUI talentdetailsname;
		[SerializeField]
		public TMPro.TextMeshProUGUI talentdetails;
		[SerializeField]
		public RectTransform expend;
		[SerializeField]
		public UnityEngine.UI.Button activitybtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI tip;
		
		private TalentPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Talent = null;
			TalentIcon1 = null;
			SkillShowScroll = null;
			CardAtlasBtn = null;
			RefBtn = null;
			GoBattleBtn = null;
			TalentIntro = null;
			Talentshow = null;
			QuitPanel = null;
			talentdetailsicon = null;
			detailsIconbg = null;
			talentdetailsname = null;
			talentdetails = null;
			expend = null;
			activitybtn = null;
			tip = null;
			
			mData = null;
		}
		
		public TalentPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		TalentPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new TalentPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
