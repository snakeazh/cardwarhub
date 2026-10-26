using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:bf26702f-1c19-4653-8f29-f2de382ac72f
	public partial class BattlePanel
	{
		public const string Name = "BattlePanel";
		
		[SerializeField]
		public UnityEngine.UI.Text PopulationLimitText;
		[SerializeField]
		public UnityEngine.UI.Text DateText;
		[SerializeField]
		public UnityEngine.UI.Text DifficultyText;
		[SerializeField]
		public UnityEngine.UI.Button SettingBtn;
		[SerializeField]
		public UnityEngine.UI.Button BackBtn;
		[SerializeField]
		public RectTransform SchoolTipDetail;
		[SerializeField]
		public UnityEngine.UI.Button NextWaveBtn;
		[SerializeField]
		public UnityEngine.UI.Slider Exp;
		[SerializeField]
		public RectTransform SubPanel;
		
		private BattlePanelData mPrivateData = null;
		
		protected override void ClearUIComponents()
		{
			PopulationLimitText = null;
			DateText = null;
			DifficultyText = null;
			SettingBtn = null;
			BackBtn = null;
			SchoolTipDetail = null;
			NextWaveBtn = null;
			Exp = null;
			SubPanel = null;
			
			mData = null;
		}
		
		public BattlePanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		BattlePanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new BattlePanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
