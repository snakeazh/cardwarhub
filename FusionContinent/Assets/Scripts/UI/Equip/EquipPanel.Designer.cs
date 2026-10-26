using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:4a7248fd-8e12-4eae-ac67-c33396c84efd
	public partial class EquipPanel
	{
		public const string Name = "EquipPanel";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button ExitBg;
		[SerializeField]
		public RectTransform WearEquips;
		[SerializeField]
		public UnityEngine.RectTransform Body;
		[SerializeField]
		public UnityEngine.RectTransform EquipCardScroll;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		[SerializeField]
		public TMPro.TMP_Dropdown EquipFilter;
		
		private EquipPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			ExitBg = null;
			WearEquips = null;
			Body = null;
			EquipCardScroll = null;
			ExitBtn = null;
			EquipFilter = null;
			
			mData = null;
		}
		
		public EquipPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		EquipPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new EquipPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
