using System;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;

namespace FusionContinent
{
	// Generate Id:11eca5fa-0e0f-4006-b25c-ac617e8248ed
	public partial class CreatePlayerPanel
	{
		public const string Name = "CreatePlayerPanel";
		
		
		private CreatePlayerPanelData mPrivateData = null;
		[SerializeField]
		private GameObject Playerboy;
		[SerializeField]
		private GameObject Playergirl;

		[SerializeField] private Button sexboybtn;
		[SerializeField] private Button sexgirlbtn;
		[SerializeField] private TMP_InputField  playernameinput;
		[SerializeField] private Button  RandomNamebtn;
		[SerializeField] private Button startgamebtn;
		[SerializeField] private Button confirmbtn;
		[SerializeField] private Transform Hair_accessoriesbtns;
		[SerializeField] private GameObject goods;
		[SerializeField] private Transform expressionbtns;
		[SerializeField] private Transform Hairbtns;
		[SerializeField] private Transform bodybtns;
		
		protected override void ClearUIComponents()
		{
			
			mData = null;
		}
		
		public CreatePlayerPanelData Data
		{
			get
			{
				return mData;
			}
		}
		
		CreatePlayerPanelData mData
		{
			get
			{
				return mPrivateData ?? (mPrivateData = new CreatePlayerPanelData());
			}
			set
			{
				mUIData = value;
				mPrivateData = value;
			}
		}
	}
}
