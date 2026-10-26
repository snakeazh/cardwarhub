using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using Newtonsoft.Json;
using UnityEngine;
using QFramework;

namespace FusionContinent
{
	public class EquipFragmentPanelData : UIPanelData
	{
	}
	public partial class EquipFragmentPanel : UIPanel
	{
		private int _soldierId;
		private readonly List<int> _optionFragmentList = new List<int>();
		private readonly Dictionary<int, GameObject> _equipSlots = new Dictionary<int, GameObject>();
		
		private TbEquipEntryConfig _entryConfig;
		private TbCardBaseConfig _cardBaseConfig;
		private TbEquipConfig _equipCardConfig;
		private TbSoldierBattleConfig _soldierBattleConfig;
		
		protected void Awake()
		{
			_entryConfig =  DataConfigManager.Tables.TbEquipEntryConfig;
			_cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
			_equipCardConfig = DataConfigManager.Tables.TbEquipConfig;
			_soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
		}
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as EquipFragmentPanelData ?? new EquipFragmentPanelData();
			// please add init code here
			Mask.onClick.AddListener(OnClose);
			FragmentBtn.onClick.AddListener(() =>
			{
				// GameMainPanel.Default.ShowConfirm("确认分解", "", FragmentEquip);
			});
		}

		public void SetData(int soldierId)
		{
			_soldierId = soldierId;
			_optionFragmentList.Clear();
			
			InitEquipSlot();
		}

		private void InitEquipSlot()
		{
			var cardScroll = EquipList.transform.Find("Viewport/Content");
			for (int i = 0; i < cardScroll.childCount; i++)
				cardScroll.GetChild(i).gameObject.SetActive(false);
			
			var equipOrderByDescList = Global.PlayerEquips.Values.Where(v => v.WearerId == 0).OrderByDescending(x => x.Quality).ToList();
			foreach (var equip in equipOrderByDescList)
			{
				var equipCfg = _equipCardConfig.GetOrDefault(equip.EquipId);
				if (!_equipSlots.TryGetValue(equip.Id, out var slotObj))
				{
					slotObj = Instantiate(cardScroll.Find("RewardEntityCheckbox").gameObject, cardScroll);
					_equipSlots.Add(equip.Id, slotObj);
				}

				slotObj.SetActive(true);
				slotObj.transform.GetComponent<RewardEntityCheckbox>().SetData(new RewardEntityData()
				{
					Num = 1,
					Quality = equipCfg.Quality,
					IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
				}, (id) =>
				{
					if (!_optionFragmentList.Remove(equip.Id)) _optionFragmentList.Add(equip.Id);
					FragmentBtn.interactable = _optionFragmentList.Count > 0;
					RefreshFragmentRewards();
				}, 1);
			}

			FragmentBtn.interactable = _optionFragmentList.Count > 0;
		}

		private void RefreshFragmentRewards()
		{
			var optionItems = new Dictionary<int, int>();
			foreach (var eid in _optionFragmentList)
			{
				var equip = Global.PlayerEquips[eid];
				var phaseCfg = _equipCardConfig.GetOrDefault(equip.EquipId);
				foreach (var v in phaseCfg.FragmentItems)
				{
					optionItems.TryAdd(v.Id, 0);
					optionItems[v.Id] += RandomTool.RandomNumRange(v.RandomNumRange);
				}
				
				// 词条分解返还
				foreach (var entryId in equip.Entrys)
				{
					var entryCfg = _entryConfig.GetOrDefault(entryId);
					if (entryCfg == null) continue;
					
					// foreach (var v in entryCfg.FragmentItems)
					// {
					// 	optionItems.TryAdd(v.Id, 0);
					// 	optionItems[v.Id] += RandomTool.RandomNumRange(v.RandomNumRange);
					// }
				}
			}
			
			var cardScroll = RewardScroll.transform.Find("Viewport/Content");
			for (int i = 0; i < cardScroll.childCount; i++)
				cardScroll.GetChild(i).gameObject.SetActive(false);
			
			var idx = 0;
			foreach (var item in optionItems)
			{
				GameObject rewObj = idx >= cardScroll.childCount 
					? Instantiate(cardScroll.Find("RewardEntity").gameObject, cardScroll, true) 
					: cardScroll.GetChild(idx).gameObject;
				
				rewObj.SetActive(true);
				rewObj.GetComponent<RewardEntity>().SetData(item.Key, item.Value);
				idx++;
				
			}
		}
  //
		// private async void FragmentEquip()
  //       {
  //           if (_optionFragmentList.Count <= 0) return;
  //           FragmentBtn.interactable = false;
  //           
  //           try
  //           {
  //               var response = await WebDataFun.SendPostRequest(ApiKeys.ApiEquipFragment, new Dictionary<string, string>
  //               {
  //                   {"OptionList", JsonConvert.SerializeObject(_optionFragmentList)}
  //               });
  //               
  //               var result = JsonConvert.DeserializeObject<FragmentEquipResult>(response);
  //               foreach (var eid in result.FragmentIds)
  //               {
  //                   Global.PlayerEquips.Remove(eid);
  //               }
  //               
  //               // 同步奖励
  //               Items.Update(result.Items);
  //               
  //               // 刷新数据
  //               SetData(_soldierId);
  //               EquipPanel.Default.SetData(_soldierId);
  //               
  //               // 奖励
  //               RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
  //               rewardTips.SetData(result.Rewards);
  //               
  //               // 重新计算战力
  //               UserModel.SumFightingNum();
  //               CombatTipController.Instance.SetCombat();
  //           }
  //           catch (System.Exception ex)
  //           {
  //               LogTool.LogError($"请求失败：{ex.Message}");
  //               FusionContinent.Toast.Show($"操作失败：{ex.Message}", 5f);
  //           }
  //           finally
  //           {
  //               FragmentBtn.interactable = true;
  //           }
  //       }
		//
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
	}
}
