using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:0cae4afd-118c-436a-88dd-a1e90e5ec02c
	public partial class SchoolChooseCardPanel
	{
		public const string Name = "SchoolChooseCardPanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI TitleText;
		[SerializeField]
		public RectTransform QualityList;
		[SerializeField]
		public UnityEngine.UI.Text CountdownText;
		[SerializeField]
		public RectTransform ChooseList;
		[SerializeField]
		public UnityEngine.UI.Button GetAllBtn;
		
		private SchoolChooseCardPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TitleText = null;
			QualityList = null;
			CountdownText = null;
			ChooseList = null;
			GetAllBtn = null;
			
			mData = null;
		}
		
		public SchoolChooseCardPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SchoolChooseCardPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SchoolChooseCardPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
