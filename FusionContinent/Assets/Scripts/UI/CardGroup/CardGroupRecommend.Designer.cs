using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:36dbd5cd-c6fd-401b-b92b-58a2cf7fcefb
	public partial class CardGroupRecommend
	{
		public const string Name = "CardGroupRecommend";
		
		[SerializeField]
		public RectTransform CloseAnimation;
		[SerializeField]
		public RectTransform OpenAnimation;
		[SerializeField]
		public UnityEngine.UI.Button Quit;
		[SerializeField]
		public RectTransform Body;
		[SerializeField]
		public RectTransform LeftBtn;
		[SerializeField]
		public UnityEngine.RectTransform ScrollView;
		[SerializeField]
		public TMPro.TextMeshProUGUI DescText;
		[SerializeField]
		public RectTransform SchoolList;
		[SerializeField]
		public RectTransform Soldiers;
		[SerializeField]
		public RectTransform SoldierList;
		[SerializeField]
		public RectTransform Skills;
		[SerializeField]
		public RectTransform SkillList;
		[SerializeField]
		public UnityEngine.UI.Image Progress;
		[SerializeField]
		public TMPro.TextMeshProUGUI Text;
		[SerializeField]
		public RectTransform Fool;
		[SerializeField]
		public UnityEngine.UI.Button ApplyBtn;
		
		private CardGroupRecommendData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseAnimation = null;
			OpenAnimation = null;
			Quit = null;
			Body = null;
			LeftBtn = null;
			ScrollView = null;
			DescText = null;
			SchoolList = null;
			Soldiers = null;
			SoldierList = null;
			Skills = null;
			SkillList = null;
			Progress = null;
			Text = null;
			Fool = null;
			ApplyBtn = null;
			
			mData = null;
		}
		
		public CardGroupRecommendData Data
		{
			get
			{
				return mData;
			}
		}
		
		CardGroupRecommendData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CardGroupRecommendData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
