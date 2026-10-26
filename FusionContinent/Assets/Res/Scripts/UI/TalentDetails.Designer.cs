using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:13dd7e12-7b26-4d0c-a829-682cbd2df059
	public partial class TalentDetails
	{
		public const string Name = "TalentDetails";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button QuitPanel;
		[SerializeField]
		public UnityEngine.UI.Image talentdetailsicon;
		[SerializeField]
		public TMPro.TextMeshProUGUI talentdetails;
		[SerializeField]
		public UnityEngine.UI.Image detailsTitlebg;
		[SerializeField]
		public TMPro.TextMeshProUGUI talentdetailsname;
		[SerializeField]
		public RectTransform expend;
		[SerializeField]
		public UnityEngine.UI.Button activitybtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI tip;
		[SerializeField]
		public UnityEngine.UI.Image IsLearn;
		
		private TalentDetailsData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			QuitPanel = null;
			talentdetailsicon = null;
			talentdetails = null;
			detailsTitlebg = null;
			talentdetailsname = null;
			expend = null;
			activitybtn = null;
			tip = null;
			IsLearn = null;
			
			mData = null;
		}
		
		public TalentDetailsData Data
		{
			get
			{
				return mData;
			}
		}
		
		TalentDetailsData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new TalentDetailsData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
