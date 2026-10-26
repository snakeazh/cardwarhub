using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:7be69820-ee97-472d-8c67-bcb823e61dcc
	public partial class CancelAccountPanel
	{
		public const string Name = "CancelAccountPanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Mask;
		[SerializeField]
		public TMPro.TextMeshProUGUI CancelAccountText;
		[SerializeField]
		public UICommonBtn CancelBtn;
		
		private CancelAccountPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			Mask = null;
			CancelAccountText = null;
			CancelBtn = null;
			
			mData = null;
		}
		
		public CancelAccountPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		CancelAccountPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CancelAccountPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
