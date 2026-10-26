using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:4c1ed3e4-434a-421d-aad1-7f1719ff4f47
	public partial class MonsterEntryDetail
	{
		public const string Name = "MonsterEntryDetail";
		
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public UnityEngine.UI.ScrollRect CardCountBox;
		
		private MonsterEntryDetailData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Quit = null;
			CardCountBox = null;
			
			mData = null;
		}
		
		public MonsterEntryDetailData Data
		{
			get
			{
				return mData;
			}
		}
		
		MonsterEntryDetailData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new MonsterEntryDetailData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
