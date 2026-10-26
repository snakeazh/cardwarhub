using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:f2edf7cd-c077-419b-b82c-59443d6ff4e3
	public partial class SoldierCardGroupSlot
	{
		public const string Name = "SoldierCardGroupSlot";
		
		[SerializeField]
		public UnityEngine.UI.Image Icon;
		
		private SoldierCardGroupSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Icon = null;
			
			mData = null;
		}
		
		public SoldierCardGroupSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		SoldierCardGroupSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SoldierCardGroupSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
