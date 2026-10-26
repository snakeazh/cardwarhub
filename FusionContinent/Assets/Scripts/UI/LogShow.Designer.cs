using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	// Generate Id:99ad57f8-0e71-4a64-95b9-7d50f8102fed
	public partial class LogShow
	{
		public const string Name = "LogShow";
		//
		[SerializeField]
		public UnityEngine.UI.Text LogShowText;
		//
		private LogShowData mPrivateData = null;
		protected override void ClearUIComponents()
		{
			LogShowText = null;
			
			mData = null;
		}
		
		public LogShowData Data
		{
			get
			{
				return mData;
			}
		}
		
		LogShowData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new LogShowData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
