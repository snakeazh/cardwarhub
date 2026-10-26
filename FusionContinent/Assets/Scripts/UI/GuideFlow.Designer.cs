using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:6f0d3e90-1e51-472f-a5d7-26dd093b50ce
	public partial class GuideFlow
	{
		public const string Name = "GuideFlow";
		
		[SerializeField]
		public UnityEngine.UI.Button PanelBtn;
		[SerializeField]
		public UnityEngine.UI.Button PointerBtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text;
		[SerializeField]
		public RectTransform GuildDetail;
		
		private GuideFlowData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			PanelBtn = null;
			PointerBtn = null;
			Text = null;
			GuildDetail = null;
			
			mData = null;
		}
		
		public GuideFlowData Data
		{
			get
			{
				return mData;
			}
		}
		
		GuideFlowData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GuideFlowData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
