using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:8ab94962-7e4e-4b06-9987-9a793048b7fb
	public partial class TeamSkillCardSlot
	{
		public const string Name = "TeamSkillCardSlot";
		
		[SerializeField]
		public UnityEngine.UI.Image Bg;
		[SerializeField]
		public UnityEngine.UI.Image SkillCardIcon;
		[SerializeField]
		public UnityEngine.UI.Image Locked;
		
		private TeamSkillCardSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Bg = null;
			SkillCardIcon = null;
			Locked = null;
			
			mData = null;
		}
		
		public TeamSkillCardSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		TeamSkillCardSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new TeamSkillCardSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
