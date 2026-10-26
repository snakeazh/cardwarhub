using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:1999c139-d5c5-4085-a6bb-a3f97b62c252
	public partial class BossComing
	{
		public const string Name = "BossComing";
		
		
		private BossComingData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public BossComingData Data
		{
			get
			{
				return mData;
			}
		}
		
		BossComingData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BossComingData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
