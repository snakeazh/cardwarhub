using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:fdb0bfcd-bb66-4037-af72-e1894deb454a
	public partial class FusionCompoundConfirmPanel
	{
		public const string Name = "FusionCompoundConfirmPanel";
		
		[SerializeField]
		public UnityEngine.UI.Image Exit;
		[SerializeField]
		public TMPro.TextMeshProUGUI TotalProgressText;
		[SerializeField]
		public TMPro.TextMeshProUGUI ProbabilityText;
		[SerializeField]
		public UnityEngine.UI.Button CompoundBtn;
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		
		private FusionCompoundConfirmPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Exit = null;
			TotalProgressText = null;
			ProbabilityText = null;
			CompoundBtn = null;
			CloseBtn = null;
			
			mData = null;
		}
		
		public FusionCompoundConfirmPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		FusionCompoundConfirmPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new FusionCompoundConfirmPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
