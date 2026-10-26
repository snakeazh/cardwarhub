using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:09d30ece-8298-423c-81ac-6d68258aebc9
	public partial class FusionGemPanel
	{
		public const string Name = "FusionGemPanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button ExitBg;
		[SerializeField]
		public RectTransform WearGem;
		[SerializeField]
		public UnityEngine.RectTransform Body;
		[SerializeField]
		public UnityEngine.RectTransform GemCardScroll;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		
		private FusionGemPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			ExitBg = null;
			WearGem = null;
			Body = null;
			GemCardScroll = null;
			ExitBtn = null;
			
			mData = null;
		}
		
		public FusionGemPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		FusionGemPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new FusionGemPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
