using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:b35de5ee-11bb-4856-be41-66aa11c4a021
	public partial class ChooseCardSlot
	{
		public const string Name = "ChooseCardSlot";
		
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public UnityEngine.UI.Button QuitBtn;
		[SerializeField]
		public RectTransform TweenTarget;
		[SerializeField]
		public UnityEngine.UI.Image Bg;
		[SerializeField]
		public TMPro.TextMeshProUGUI Title;
		[SerializeField]
		public UnityEngine.UI.Image Icon;
		[SerializeField]
		public TMPro.TextMeshProUGUI Description;
		[SerializeField]
		public RectTransform BelongSchool;
		[SerializeField]
		public RectTransform StarList;
		
		private ChooseCardSlotData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			OpenAnimation = null;
			CloseAnimation = null;
			QuitBtn = null;
			TweenTarget = null;
			Bg = null;
			Title = null;
			Icon = null;
			Description = null;
			BelongSchool = null;
			StarList = null;
			
			mData = null;
		}
		
		public ChooseCardSlotData Data
		{
			get
			{
				return mData;
			}
		}
		
		ChooseCardSlotData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ChooseCardSlotData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
