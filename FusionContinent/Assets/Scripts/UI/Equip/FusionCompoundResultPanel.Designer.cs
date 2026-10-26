using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:5c799538-22fc-4605-8367-7c885cbfaba1
	public partial class FusionCompoundResultPanel
	{
		public const string Name = "FusionCompoundResultPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public UnityEngine.UI.Image GemItemIcon;
		[SerializeField]
		public RectTransform Success;
		[SerializeField]
		public RectTransform Failure;
		[SerializeField]
		public UnityEngine.UI.Image CurProbability;
		[SerializeField]
		public TMPro.TextMeshProUGUI CurProText;
		
		private FusionCompoundResultPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			GemItemIcon = null;
			Success = null;
			Failure = null;
			CurProbability = null;
			CurProText = null;
			
			mData = null;
		}
		
		public FusionCompoundResultPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		FusionCompoundResultPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new FusionCompoundResultPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
