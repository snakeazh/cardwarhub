using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using Manager;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.SceneManagement;

namespace FusionContinent
{
	public class CreateNamePanelData : UIPanelData
	{
	}
	public partial class CreateNamePanel : UIPanel
	{

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as CreateNamePanelData ?? new CreateNamePanelData();
			// please add init code here
			
			// // Startgamebtn.onClick.AddListener(PayGame);
			// Randomnamebtn.onClick.AddListener(RandomUserName);
			//
			// Inputnametxt.onValueChanged.AddListener((string input) =>
			// {
			// 	Inputnametxt.text = SensitiveCharacterTool.Filter(input);
			// 	var maxLength = 10;
			// 	if (input.Length > maxLength)
			// 	{
			// 		FusionContinent.Toast.Show("名称不能超过10个字符"); 
			// 	}
			// });
			// Global.Player.FigureData = _playerConst.InitManFigures;//默认为男性装扮
			// RandomUserName();
		}
		
		private void OnEnable()
		{

		}
		
		private void RandomUserName()
		{
			Inputnametxt.text = Utils.RandomTool.RandomUserName(0);
		}
		
		
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
		}
	}
}
