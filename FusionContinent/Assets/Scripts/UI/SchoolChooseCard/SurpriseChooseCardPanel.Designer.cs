using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:bd93b7ba-a14e-4cb0-8076-0a35b76beee8
	public partial class SurpriseChooseCardPanel
	{
		public const string Name = "SurpriseChooseCardPanel";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI TitleText;
		[SerializeField]
		public RectTransform QualityList;
		[SerializeField]
		public UnityEngine.UI.Text CountdownText;
		[SerializeField]
		public RectTransform ChooseList;
		
		private SurpriseChooseCardPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			TitleText = null;
			QualityList = null;
			CountdownText = null;
			ChooseList = null;
			
			mData = null;
		}
		
		public SurpriseChooseCardPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		SurpriseChooseCardPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new SurpriseChooseCardPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
