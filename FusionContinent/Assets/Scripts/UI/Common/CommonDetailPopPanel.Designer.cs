using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:62a2228c-cbe9-4171-a80f-03f4ec8f497a
	public partial class CommonDetailPopPanel
	{
		public const string Name = "CommonDetailPopPanel";
		
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Mask;
		[SerializeField]
		public RectTransform PosNode;
		[SerializeField]
		public TMPro.TextMeshProUGUI Title;
		[SerializeField]
		public TMPro.TextMeshProUGUI Detail;
		
		private CommonDetailPopPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			PosNode = null;
			Title = null;
			Detail = null;
			
			mData = null;
		}
		
		public CommonDetailPopPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		CommonDetailPopPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CommonDetailPopPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
