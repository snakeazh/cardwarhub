using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:90b1c7f6-69de-4555-9ba1-fa7b4ee5d556
	public partial class SkinSoliderPanel
	{
		public const string Name = "SkinSoliderPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button shadow;
		[SerializeField]
		public UnityEngine.UI.Image icon;
		[SerializeField]
		public UnityEngine.UI.Button ChooeseBtn;
		[SerializeField]
		public UnityEngine.UI.Button closebtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI SkinName;
		[SerializeField]
		public RectTransform AttributeScrollContent;
		[SerializeField]
		public RectTransform SelectBtnParent;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		
		private SkinSoliderPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			shadow = null;
			icon = null;
			ChooeseBtn = null;
			closebtn = null;
			SkinName = null;
			AttributeScrollContent = null;
			SelectBtnParent = null;
			CloseAnimation = null;
			OpenAnimation = null;
			
			mData = null;
		}
		
		public SkinSoliderPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SkinSoliderPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SkinSoliderPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
