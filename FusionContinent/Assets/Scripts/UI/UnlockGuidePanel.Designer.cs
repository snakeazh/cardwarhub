using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:84830651-2bf3-4075-891d-61a974dcf4da
	public partial class UnlockGuidePanel
	{
		public const string Name = "UnlockGuidePanel";
		
		[SerializeField]
		public RectTransform Finger;
		[SerializeField]
		public UnityEngine.RectTransform Maskabled;
		[SerializeField]
		public UnityEngine.RectTransform Maskabled2;
		[SerializeField]
		public UnityEngine.UI.Image Mask;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress NextBtn;
		[SerializeField]
		public UnityEngine.RectTransform Arrow1;
		[SerializeField]
		public UnityEngine.RectTransform Arrow2;
		[SerializeField]
		public UnityEngine.UI.Button SkipBtn;
		[SerializeField]
		public RectTransform Dialoge;
		[SerializeField]
		public TMPro.TextMeshProUGUI ContentText;
		
		private UnlockGuidePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Finger = null;
			Maskabled = null;
			Maskabled2 = null;
			Mask = null;
			NextBtn = null;
			Arrow1 = null;
			Arrow2 = null;
			SkipBtn = null;
			Dialoge = null;
			ContentText = null;
			
			mData = null;
		}
		
		public UnlockGuidePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UnlockGuidePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UnlockGuidePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
