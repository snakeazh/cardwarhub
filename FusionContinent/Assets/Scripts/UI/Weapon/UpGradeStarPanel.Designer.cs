using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:8b99d87a-1bce-424a-a4b9-48a10d508f1b
	public partial class UpGradeStarPanel
	{
		public const string Name = "UpGradeStarPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public RectTransform CurrentWeapon;
		[SerializeField]
		public RectTransform CurDetail;
		[SerializeField]
		public RectTransform NextDetail;
		[SerializeField]
		public RectTransform Arrow;
		[SerializeField]
		public RectTransform Consume;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress LevelUpBtn;
		[SerializeField]
		public FusionContinent.UI.Components.UIPointerClickAndLongPress ExitBtn;
		
		private UpGradeStarPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			CurrentWeapon = null;
			CurDetail = null;
			NextDetail = null;
			Arrow = null;
			Consume = null;
			LevelUpBtn = null;
			ExitBtn = null;
			
			mData = null;
		}
		
		public UpGradeStarPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		UpGradeStarPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new UpGradeStarPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
