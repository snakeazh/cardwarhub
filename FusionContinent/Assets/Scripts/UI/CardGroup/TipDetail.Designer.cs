using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:107c7021-90f7-4d35-bb4c-4da740d04333
	public partial class TipDetail
	{
		public const string Name = "TipDetail";
		
		[SerializeField]
		public TMPro.TextMeshProUGUI NameText;
		[SerializeField]
		public UnityEngine.UI.ScrollRect CardCountBox;
		[SerializeField]
		public RectTransform Content;
		[SerializeField]
		public UnityEngine.UI.Button ExitBtn;
		
		private TipDetailData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			NameText = null;
			CardCountBox = null;
			Content = null;
			ExitBtn = null;
			
			mData = null;
		}
		
		public TipDetailData Data
		{
			get
			{
				return mData;
			}
		}
		
		TipDetailData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new TipDetailData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
