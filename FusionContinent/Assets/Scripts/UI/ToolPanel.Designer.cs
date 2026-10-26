using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:f0be5af1-89a4-4414-90d8-6bf9ece42a08
	public partial class ToolPanel
	{
		public const string Name = "ToolPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button CloseBtn;
		[SerializeField]
		public RectTransform ItemContent;
		[SerializeField]
		public UnityEngine.UI.Dropdown ItemDropdown;
		[SerializeField]
		public TMPro.TMP_InputField ItemSearchInput;
		[SerializeField]
		public UnityEngine.UI.Dropdown ItemTypeDropdown;
		[SerializeField]
		public UnityEngine.UI.InputField ItemsInput;
		[SerializeField]
		public UnityEngine.UI.Text ItemsText;
		[SerializeField]
		public UnityEngine.UI.Button SendItemBtn;
		[SerializeField]
		public UnityEngine.UI.Button SendItemAllBtn;
		[SerializeField]
		public RectTransform GameContent;
		[SerializeField]
		public UnityEngine.UI.Dropdown AttrDropdown;
		[SerializeField]
		public UnityEngine.UI.InputField AttrInput;
		[SerializeField]
		public UnityEngine.UI.Button AddAttrBtn;
		[SerializeField]
		public UnityEngine.UI.Button GamePauseBtn;
		[SerializeField]
		public UnityEngine.UI.Button FullScreenHarmBtn;
		[SerializeField]
		public UnityEngine.UI.Button GameSuccessBtn;
		[SerializeField]
		public RectTransform SchoolContent;
		[SerializeField]
		public UnityEngine.UI.Dropdown SchoolDropdown;
		[SerializeField]
		public UnityEngine.UI.InputField SchoolInput;
		[SerializeField]
		public UnityEngine.UI.Text SchoolsText;
		[SerializeField]
		public UnityEngine.UI.Button AddSchoolBtn;
		
		private ToolPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			CloseBtn = null;
			ItemContent = null;
			ItemDropdown = null;
			ItemSearchInput = null;
			ItemTypeDropdown = null;
			ItemsInput = null;
			ItemsText = null;
			SendItemBtn = null;
			SendItemAllBtn = null;
			GameContent = null;
			AttrDropdown = null;
			AttrInput = null;
			AddAttrBtn = null;
			GamePauseBtn = null;
			FullScreenHarmBtn = null;
			GameSuccessBtn = null;
			SchoolContent = null;
			SchoolDropdown = null;
			SchoolInput = null;
			SchoolsText = null;
			AddSchoolBtn = null;
			
			mData = null;
		}
		
		public ToolPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		ToolPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new ToolPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
