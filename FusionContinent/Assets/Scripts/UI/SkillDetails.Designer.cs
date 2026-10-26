using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:964cbb62-555b-402d-8485-dfbc33e83149
	public partial class SkillDetails
	{
		public const string Name = "SkillDetails";
		
		[SerializeField]
		public RectTransform Head;
		[SerializeField]
		public RectTransform Spine;
		[SerializeField]
		public UnityEngine.UI.ScrollRect AttributeScroll;
		[SerializeField]
		public RectTransform SchoolStars;
		[SerializeField]
		public UnityEngine.UI.ScrollRect StarList;
		
		private SkillDetailsData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Head = null;
			Spine = null;
			AttributeScroll = null;
			SchoolStars = null;
			StarList = null;
			
			mData = null;
		}
		
		public SkillDetailsData Data
		{
			get
			{
				return mData;
			}
		}
		
		SkillDetailsData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SkillDetailsData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
