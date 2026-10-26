using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:1bb2ca52-445a-445e-abc3-a42291af8341
	public partial class ConfirmDialogPanel
	{
		public const string Name = "ConfirmDialogPanel";
		
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Mask;
		[SerializeField]
		public RectTransform BasePlate;
		[SerializeField]
		public TMPro.TextMeshProUGUI TitleText;
		[SerializeField]
		public UnityEngine.UI.Image DetailBox;
		[SerializeField]
		public TMPro.TextMeshProUGUI Message;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress OptionBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress CancelBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ConfirmBtn;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		
		private ConfirmDialogPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			BasePlate = null;
			TitleText = null;
			DetailBox = null;
			Message = null;
			OptionBtn = null;
			CancelBtn = null;
			ConfirmBtn = null;
			CloseAnimation = null;
			OpenAnimation = null;
			
			mData = null;
		}
		
		public ConfirmDialogPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ConfirmDialogPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ConfirmDialogPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
