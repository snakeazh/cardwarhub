using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:d4638a1c-a7b1-4d92-be1d-e949f7e81469
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
		public UnityEngine.UI.Image detailsTitlebg;
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
		
		private TalentDetailsData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			QuitPanel = null;
			talentdetailsicon = null;
			detailsTitlebg = null;
			talentdetailsname = null;
			talentdetails = null;
			expend = null;
			activitybtn = null;
			tip = null;
			
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
