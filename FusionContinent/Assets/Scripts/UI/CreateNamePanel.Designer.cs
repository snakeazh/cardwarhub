using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:76d2bede-c51e-4de9-b2a6-47045d65a788
	public partial class CreateNamePanel
	{
		public const string Name = "CreateNamePanel";
		
		[SerializeField]
		public UnityEngine.UI.Button Mask;
		[SerializeField]
		public TMPro.TMP_InputField Inputnametxt;
		[SerializeField]
		public UnityEngine.UI.Button CleanNameBtn;
		[SerializeField]
		public UnityEngine.UI.Button ConfirmBtn;
		
		private CreateNamePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			Mask = null;
			Inputnametxt = null;
			CleanNameBtn = null;
			ConfirmBtn = null;
			
			mData = null;
		}
		
		public CreateNamePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		CreateNamePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CreateNamePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
