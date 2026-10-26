using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:4a968315-035a-418a-97f1-dc87562eb3b4
	public partial class SelectSoldierPopup
	{
		public const string Name = "SelectSoldierPopup";
		
		[SerializeField]
		public UnityEngine.UI.Button SoldierHead;
		[SerializeField]
		public RectTransform SoldierListPopup;
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public UnityEngine.UI.Image Icon;
		
		private SelectSoldierPopupData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			SoldierHead = null;
			SoldierListPopup = null;
			Quit = null;
			Content = null;
			Icon = null;
			
			mData = null;
		}
		
		public SelectSoldierPopupData Data
		{
			get
			{
				return mData;
			}
		}
		
		SelectSoldierPopupData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SelectSoldierPopupData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
