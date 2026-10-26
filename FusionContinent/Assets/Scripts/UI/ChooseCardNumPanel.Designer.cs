using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:ba375943-acb8-45bf-90e2-9fbbfc031413
	public partial class ChooseCardNumPanel
	{
		public const string Name = "ChooseCardNumPanel";
		
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress Mask;
		[SerializeField]
		public TMPro.TextMeshProUGUI NumText;
		[SerializeField]
		public UnityEngine.UI.Slider Slider;
		[SerializeField]
		public UnityEngine.UI.Button ConfirmButton;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		
		private ChooseCardNumPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			NumText = null;
			Slider = null;
			ConfirmButton = null;
			ExitBtn = null;
			
			mData = null;
		}
		
		public ChooseCardNumPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ChooseCardNumPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ChooseCardNumPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
