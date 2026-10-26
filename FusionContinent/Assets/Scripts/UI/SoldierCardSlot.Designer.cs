using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:ebdd244b-53f8-4d2d-8c72-94061b549240
	public partial class SoldierCardSlot
	{
		public const string Name = "SoldierCardSlot";
		
		
		private SoldierCardSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public SoldierCardSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		SoldierCardSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SoldierCardSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
