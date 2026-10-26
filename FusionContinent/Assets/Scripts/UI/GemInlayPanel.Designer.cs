using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:968f17c4-1347-4459-8537-8bab9ef0e064
	public partial class GemInlayPanel
	{
		public const string Name = "GemInlayPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Bg;
		[SerializeField]
		public UnityEngine.UI.Text TitleText;
		[SerializeField]
		public GemInlaySlot GemInlaySlot1;
		[SerializeField]
		public GemInlaySlot GemInlaySlot2;
		[SerializeField]
		public GemInlaySlot GemInlaySlot3;
		[SerializeField]
		public GemInlaySlot GemInlaySlot4;
		[SerializeField]
		public GemInlaySlot GemInlaySlot5;
		[SerializeField]
		public GemInlaySlot GemInlaySlot6;
		[SerializeField]
		public UnityEngine.UI.Text GemText;
		[SerializeField]
		public UnityEngine.UI.Image GemIcon;
		[SerializeField]
		public UnityEngine.UI.Button InlayBtn;
		[SerializeField]
		public UnityEngine.UI.Text ReplaceText;
		
		private GemInlayPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Bg = null;
			TitleText = null;
			GemInlaySlot1 = null;
			GemInlaySlot2 = null;
			GemInlaySlot3 = null;
			GemInlaySlot4 = null;
			GemInlaySlot5 = null;
			GemInlaySlot6 = null;
			GemText = null;
			GemIcon = null;
			InlayBtn = null;
			ReplaceText = null;
			
			mData = null;
		}
		
		public GemInlayPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GemInlayPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GemInlayPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
