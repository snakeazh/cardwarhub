using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:17a587bd-24a5-475f-a0a7-270ff05cce63
	public partial class SoldierGetPanel
	{
		public const string Name = "SoldierGetPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button BgButton;
		[SerializeField]
		public UnityEngine.UI.Image SoldierImg;
		[SerializeField]
		public RectTransform SpinePos;
		[SerializeField]
		public TMPro.TextMeshProUGUI SoldierName;
		[SerializeField]
		public TMPro.TextMeshProUGUI SoldierName2;
		[SerializeField]
		public UnityEngine.UI.Image MainAttrTypeIcon;
		
		private SoldierGetPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			BgButton = null;
			SoldierImg = null;
			SpinePos = null;
			SoldierName = null;
			SoldierName2 = null;
			MainAttrTypeIcon = null;
			
			mData = null;
		}
		
		public SoldierGetPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SoldierGetPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SoldierGetPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
