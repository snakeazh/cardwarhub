using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:89cf4ef3-13bd-4796-9923-038e636c6b44
	public partial class GeneralCardConbinePanel
	{
		public const string Name = "GeneralCardConbinePanel";
		
		[SerializeField]
		public RectTransform TopAnimationLeft;
		[SerializeField]
		public RectTransform TopAnimationRight;
		[SerializeField]
		public RectTransform CombineBefore;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public UnityEngine.UI.Button QuestionBtn;
		[SerializeField]
		public UnityEngine.UI.Button ConbineBtn;
		[SerializeField]
		public RectTransform CardsContent;
		[SerializeField]
		public UnityEngine.RectTransform Gem;
		[SerializeField]
		public RectTransform CombineAfter;
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public TMPro.TextMeshProUGUI NameText;
		
		private GeneralCardConbinePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TopAnimationLeft = null;
			TopAnimationRight = null;
			CombineBefore = null;
			CloseBtn = null;
			QuestionBtn = null;
			ConbineBtn = null;
			CardsContent = null;
			Gem = null;
			CombineAfter = null;
			Mask = null;
			NameText = null;
			
			mData = null;
		}
		
		public GeneralCardConbinePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		GeneralCardConbinePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new GeneralCardConbinePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
