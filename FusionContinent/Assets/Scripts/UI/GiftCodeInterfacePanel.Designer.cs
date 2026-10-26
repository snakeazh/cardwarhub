using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:08452cbf-317b-4f04-833b-45247bec6a89
	public partial class GiftCodeInterfacePanel
	{
		public const string Name = "GiftCodeInterfacePanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Shadow;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text;
		[SerializeField]
		public TMPro.TMP_InputField CodeField;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ReceiveBtn;
		
		private GiftCodeInterfacePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			Shadow = null;
			Text = null;
			CodeField = null;
			ReceiveBtn = null;
			
			mData = null;
		}
		
		public GiftCodeInterfacePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GiftCodeInterfacePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GiftCodeInterfacePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
