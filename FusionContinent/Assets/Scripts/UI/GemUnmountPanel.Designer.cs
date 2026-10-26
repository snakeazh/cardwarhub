using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:ca704804-3e24-40f7-a5f7-d940d1660b8f
	public partial class GemUnmountPanel
	{
		public const string Name = "GemUnmountPanel";
		
		[SerializeField]
		public UnityEngine.UI.Text TitleText;
		[SerializeField]
		public GemUnmountSlot GemInlaySlot1;
		[SerializeField]
		public GemUnmountSlot GemInlaySlot2;
		[SerializeField]
		public GemUnmountSlot GemInlaySlot3;
		[SerializeField]
		public GemUnmountSlot GemInlaySlot4;
		[SerializeField]
		public GemUnmountSlot GemInlaySlot5;
		[SerializeField]
		public GemUnmountSlot GemInlaySlot6;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		
		private GemUnmountPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TitleText = null;
			GemInlaySlot1 = null;
			GemInlaySlot2 = null;
			GemInlaySlot3 = null;
			GemInlaySlot4 = null;
			GemInlaySlot5 = null;
			GemInlaySlot6 = null;
			CloseBtn = null;
			
			mData = null;
		}
		
		public GemUnmountPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GemUnmountPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GemUnmountPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
