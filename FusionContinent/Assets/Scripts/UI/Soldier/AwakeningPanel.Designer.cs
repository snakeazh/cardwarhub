using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:96131cfb-960e-4cc0-95de-f46f1d958e72
	public partial class AwakeningPanel
	{
		public const string Name = "AwakeningPanel";
		
		[SerializeField]
		public UnityEngine.UI.Button AwakeningBtn;
		[SerializeField]
		public RectTransform AwakeningMaterial;
		[SerializeField]
		public UnityEngine.UI.Button Closebtn;
		[SerializeField]
		public TMPro.TextMeshProUGUI BackGroudDesText;
		[SerializeField]
		public TMPro.TextMeshProUGUI AwakeSkillNameText;
		[SerializeField]
		public TMPro.TextMeshProUGUI AwakeSkillDesText;
		[SerializeField]
		public UnityEngine.UI.Button SkillItem;
		[SerializeField]
		public RectTransform ConsumeItems;
		[SerializeField]
		public TMPro.TextMeshProUGUI IsAwaken;
		
		private AwakeningPanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			AwakeningBtn = null;
			AwakeningMaterial = null;
			Closebtn = null;
			BackGroudDesText = null;
			AwakeSkillNameText = null;
			AwakeSkillDesText = null;
			SkillItem = null;
			ConsumeItems = null;
			IsAwaken = null;
			
			mData = null;
		}
		
		public AwakeningPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		AwakeningPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new AwakeningPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
