using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:ed0e9764-8af5-47cf-9f5b-a645cc0d6e70
	public partial class SkillItem
	{
		public const string Name = "SkillItem";
		
		
		private SkillItemData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public SkillItemData Data
		{
			get
			{
				return mData;
			}
		}
		
		SkillItemData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SkillItemData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
