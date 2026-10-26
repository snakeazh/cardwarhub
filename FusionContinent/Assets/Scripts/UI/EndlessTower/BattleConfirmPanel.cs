using System;
using Cardwar;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.SceneManagement;

namespace FusionContinent
{
	public class BattleConfirmPanelData : UIPanelData
	{
	}
	public partial class BattleConfirmPanel : UIPanel
	{
		private int _nodeId;
		private int _towerId;
		private StageBattleConfig _stageBattleCfg;
		
		private TbStageBattleConfig _stageBattleConfig;
		private TbEndlessTowerMapConfig _endlessTowerMapConfig;
		
		protected void Awake()
		{
			_stageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
			_endlessTowerMapConfig = DataConfigManager.Tables.TbEndlessTowerMapConfig;
		}
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BattleConfirmPanelData ?? new BattleConfirmPanelData();
			// please add init code here
			
			Quit.onClick.AddListener(OnClose);
			NexFloorBtn.onClick.AddListener(NextFloor);
			StaminaBtn.onClick.AddListener(() =>
			{
				if (_stageBattleCfg == null) return;
				StaminaBtn.interactable = false;
				LogTool.Log($"点击开始游戏，关卡ID: {_stageBattleCfg.Id}");
				
				// 设置关卡Id
				Global.StageId.Value = _stageBattleCfg.Id;
				// MainInterfacePanel.Default.PlayGame();
			});
		}

		public void SetData(int towerId, int mapId, int nodeId)
		{
			_nodeId = nodeId;
			_towerId = towerId;
			
			var mapCfg = _endlessTowerMapConfig.GetOrDefault(mapId);
			if (mapCfg == null || mapCfg.NodeList.Count < nodeId)
			{
				FusionContinent.Toast.Show($"无尽塔地图不存在: {mapId}");
				return;
			}

			var nodeCfg = mapCfg.NodeList[nodeId - 1];
			_stageBattleCfg = _stageBattleConfig.GetOrDefault(nodeCfg.StageId);
			if (_stageBattleCfg != null)
			{
				StaminaBtn.interactable = Global.PlayerResource.CheckStamina(_stageBattleCfg.ConsumeStamina);
				StaminaBtn.transform.Find("Text").GetComponent<TMP_Text>().text = $"{_stageBattleCfg.ConsumeStamina}";
			}

			if (nodeCfg.NodeType == MapNodeType.Chest) TitleText.text = "宝箱";
			if (nodeCfg.NodeType == MapNodeType.Event) TitleText.text = "事件";
			if (nodeCfg.NodeType == MapNodeType.Monster) TitleText.text = "怪物";
			if (nodeCfg.NodeType == MapNodeType.NextFloor) TitleText.text = "下一层";
			
			TipText.text = nodeCfg.Desc;
			StaminaBtn.gameObject.SetActive(nodeCfg.NodeType != MapNodeType.NextFloor);
			NexFloorBtn.gameObject.SetActive(nodeCfg.NodeType == MapNodeType.NextFloor);
		}
		
		/**
		 * 进入下一层
		 * 
		 * @return 
		 * @author Zpp
		 * @create 2025/10/30
		 **/
		private async void NextFloor()
		{
			try
			{
				var request = new NextFloorRequest { TowerId = _towerId, NodeId = _nodeId };
				var proto = await WebDataFun.SendProtoPostRequest<NextFloorResponse>(ApiKeys.ApiEndlessTowerNextFloor, request);
				if (proto != null && proto.Tower != null)
				{
					var tower = ProtoMappers.MapPlayerEndlessTower(proto.Tower);
					Global.PlayerEndlessTowers[tower.TowerId] = tower;

					// 播放进入下一层特效
					NextFloorEffect(() =>
					{
						// 刷新数据
						EndlessTowerPanel.Default.SetData();
						OnClose();
					});
				}
			}
			catch (System.Exception ex)
			{
				LogTool.LogError($"请求失败：{ex.Message}");
				FusionContinent.Toast.Show($"操作失败：{ex.Message}");
			}
			finally
			{
			}
		}

		protected override void OnOpen(IUIData uiData = null)
		{
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
		}
		
		protected override void OnClose()
		{
			gameObject.SetActive(false);
		}

		private void NextFloorEffect(Action onFinish)
		{
			onFinish();
		}
		
	}
}
