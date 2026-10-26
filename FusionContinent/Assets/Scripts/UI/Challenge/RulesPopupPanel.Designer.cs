using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:da6e08ff-e511-41e8-9d06-c1593ff1b83e
	public partial class RulesPopupPanel
	{
		public const string Name = "RulesPopupPanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI TitleText;
		[SerializeField]
		public TMPro.TextMeshProUGUI ContentText;
		[SerializeField]
		public UnityEngine.UI.Button ApplyBtn;
		
		private RulesPopupPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TitleText = null;
			ContentText = null;
			ApplyBtn = null;
			
			mData = null;
		}
		
		public RulesPopupPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		RulesPopupPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new RulesPopupPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
