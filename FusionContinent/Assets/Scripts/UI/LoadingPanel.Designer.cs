using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:b5f2cbdb-7912-416b-9fa1-31379bf628da
	public partial class LoadingPanel
	{
		public const string Name = "LoadingPanel";
		
		[SerializeField]
		public UnityEngine.UI.Image Icon;
		[SerializeField]
		public UnityEngine.UI.Slider progressBar;
		[SerializeField]
		public RectTransform Spine;
		[SerializeField]
		public TMPro.TextMeshProUGUI ProgressText;
		[SerializeField]
		public UnityEngine.UI.Image SpineShade;
		[SerializeField]
		public TMPro.TextMeshProUGUI TipText;
		[SerializeField]
		public RectTransform Tips;
		
		private LoadingPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Icon = null;
			progressBar = null;
			Spine = null;
			ProgressText = null;
			SpineShade = null;
			TipText = null;
			Tips = null;
			
			mData = null;
		}
		
		public LoadingPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		LoadingPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LoadingPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
