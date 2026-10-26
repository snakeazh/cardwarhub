using System.Collections.Generic;
using System.Linq;
using FusionContinent.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;

namespace FusionContinent
{
	public class FusionGemFragmentPanelData : UIPanelData
	{
	}
	public partial class FusionGemFragmentPanel : UIPanel
	{
		private int _soldierId;
		private readonly List<int> _optionFragmentList = new List<int>();
		private readonly Dictionary<int, GameObject> _cardSlots = new Dictionary<int, GameObject>();
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as FusionGemFragmentPanelData ?? new FusionGemFragmentPanelData();
			// please add init code here
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			Quit.onClick.AddListener(OnClose);
			FragmentBtn.onClick.AddListener(() =>
			{
				GameMainPanel.Default.ShowConfirm("确认分解", "", FragmentFusionGem);
			});
		}
		
		
		public void SetData(int soldierId)
		{
			_soldierId = soldierId;
			_optionFragmentList.Clear();
			
			InitSlot();
			RefreshFragmentRewards();
		}
		
		private void InitSlot()
		{
			var cardScroll = FusionGemList.transform.Find("Viewport/Content");
			for (int i = 0; i < cardScroll.childCount; i++)
				cardScroll.GetChild(i).gameObject.SetActive(false);
			
			// var orderByDescList = Global.PlayerFusionGems.Values.Where(v => v.WearerId == 0).OrderByDescending(x => x.Quality).ToList();
			// foreach (var gem in orderByDescList)
			// {
			// 	if (!_cardSlots.TryGetValue(gem.Id, out var slotObj))
			// 	{
			// 		slotObj = Instantiate(cardScroll.Find("RewardEntityCheckbox").gameObject, cardScroll);
			// 		_cardSlots.Add(gem.Id, slotObj);
			// 	}
			//
			// 	slotObj.SetActive(true);
			// 	slotObj.transform.GetComponent<RewardEntityCheckbox>().SetData(new RewardEntityData()
			// 	{
			// 		Num = 1,
			// 		Quality = gem.Quality,
			// 		IconSprite = Global.FusionGemIconSpriteAtlas.GetSprite(gem.Config.Icon),
			// 	}, (id) =>
			// 	{
			// 		if (!_optionFragmentList.Remove(gem.Id)) _optionFragmentList.Add(gem.Id);
			// 		FragmentBtn.interactable = _optionFragmentList.Count > 0;
			// 		RefreshFragmentRewards();
			// 	}, 1);
			// }

			FragmentBtn.interactable = _optionFragmentList.Count > 0;
		}

		private void RefreshFragmentRewards()
		{
			var optionItems = new Dictionary<int, int>();
			foreach (var id in _optionFragmentList)
			{
				// var gem = Global.PlayerFusionGems[id];
				// var lvCfg = gem.GetLevelCfg();
				// foreach (var v in lvCfg.ConvertFragment)
				// {
				// 	optionItems.TryAdd(v.Id, 0);
				// 	optionItems[v.Id] += v.Num;
				// }
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
		
		
		private async void FragmentFusionGem()
		{
			if (_optionFragmentList.Count <= 0) return;
			FragmentBtn.interactable = false;
            
			try
			{
				// var response = await WebDataFun.SendPostRequest(ApiKeys.ApiFusionGemFragment, new Dictionary<string, string>
				// {
				// 	{"OptionList", JsonConvert.SerializeObject(_optionFragmentList)}
				// });
    //             
				// var result = JsonConvert.DeserializeObject<FusionGemResult>(response);
				// foreach (var id in result.DelIds)
				// {
				// 	// Global.PlayerFusionGems.Remove(id);
				// }
    //             
				// // 同步奖励
				// Items.Update(result.Items);
    //             
				// // 刷新数据
				// SetData(_soldierId);
				// EquipPanel.Default.SetData(_soldierId);
    //             
				// // 奖励
				// RewardTips rewardTips = UIKit.OpenPanel<RewardTips>(UILevel.PopUI);
				// rewardTips.SetData(result.Rewards);
    //             
				// // 重新计算战力
				// UserModel.SumFightingNum();
				// CombatTipController.Instance.SetCombat();
			}
			catch (System.Exception ex)
			{
				LogTool.LogError($"请求失败：{ex.Message}");
				FusionContinent.Toast.Show($"操作失败：{ex.Message}", 5f);
			}
			finally
			{
				FragmentBtn.interactable = true;
			}
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
