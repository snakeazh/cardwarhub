using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:9e03964d-2f1e-4953-a276-24e16aad89f5
	public partial class BattleSchoolsPanel
	{
		public const string Name = "BattleSchoolsPanel";
		
		[SerializeField]
		public UnityEngine.UI.ScrollRect Schools;
		[SerializeField]
		public RectTransform SchoolTipDetail;
		
		private BattleSchoolsPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Schools = null;
			SchoolTipDetail = null;
			
			mData = null;
		}
		
		public BattleSchoolsPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattleSchoolsPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattleSchoolsPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
