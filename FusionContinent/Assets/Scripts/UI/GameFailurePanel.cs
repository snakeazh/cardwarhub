using FusionContinent.Model;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;

namespace FusionContinent
{
	public class GameFailurePanelData : UIPanelData
	{
	}
	public partial class GameFailurePanel : UIPanel
	{
		private int gameReviveCount = 0;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GameFailurePanelData ?? new GameFailurePanelData();
			gameReviveCount = DataConfigManager.Tables.TbGameBattleConst.GameReviveCount;
			if (Global.moon.Value)
			{
				gameReviveCount = DataConfigManager.Tables.TbGameBattleConst.GameReviveCountMonthly;
			}
			
			BackBtn.onClick.AddListener(() =>
            {
				GamePlayManager.Default.BackHome();
				UIKit.ClosePanel<GameFailurePanel>();
			});
			AgineBtn.onClick.AddListener(() =>
			{
				AgineBtn.interactable = false; // 禁用按钮
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
				GamePlayManager.Default.RestartGame(() =>
				{
					UIKit.ClosePanel<GameFailurePanel>();
					SceneLoader.Load("GamePlay");
				}, () =>
				{
					if (AgineBtn != null) AgineBtn.interactable = true; // 恢复按钮
				});
			});
			RevivalBtn.onClick.AddListener(() =>
			{
				if (Global.GameReviveCount.Value >= gameReviveCount)
				{
					FusionContinent.Toast.Show("复活次数已达上限");
					return;
				}

				Global.GameReviveCount.Value++;
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                //Global.GameGold.Value = 0;
                UIKit.ClosePanel<GameFailurePanel>();
				// GameLand.Default.wall.InitData();
				GamePlayManager.Default.ReviveSoldierCard();
                GamePlayManager.Default.StartGame();
			});
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			var canReviveCount = Global.GameReviveCount.Value > 0 ? gameReviveCount - Global.GameReviveCount.Value : gameReviveCount;
			RevivalText.text = $"可复活次数：{canReviveCount}";
			RevivalBtn.interactable = canReviveCount > 0;
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
