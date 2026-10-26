
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;

namespace FusionContinent
{
	public class SoldierMainPanelData : UIPanelData
	{
	}
	public partial class SoldierMainPanel : UIPanel
	{
		private TbCardBaseConfig _cardBaseConfig;
		private TbSoldierBattleConfig _soldierBattleConfig;
		private readonly List<PlayerCard> _allSoldierList = new List<PlayerCard>();
		private CareerType? _currentFilter;

		private static readonly string[] CareerFilterNames = { "全部", "射手", "战士", "法师", "刺客", "辅助" };

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SoldierMainPanelData ?? new SoldierMainPanelData();

			_cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
			_soldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;

			InitSoldierList();
			InitDropdown();
			SetSoldierList();

			CloseBtn.onClick.AddListener(() => CloseSelf());
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
		}

		private void OnDisable()
		{
			_allSoldierList.Clear();
			_currentFilter = null;
		}

		private void InitSoldierList()
		{
			_allSoldierList.Clear();
			foreach (var cfg in _cardBaseConfig.DataList)
			{
				if (cfg.Type != CardType.Soldier)
					continue;

				var cardInfo = Global.GetCardById(cfg.Id, new PlayerCard()
				{
					Lv = 1,
					IsHave = false,
					Config = cfg,
					CardId = cfg.Id,
					ObjData = new CardDataStruct(),
				});
				_allSoldierList.Add(cardInfo);
			}
		}

		private void InitDropdown()
		{
			if (SoldierCardDropdown == null) return;

			SoldierCardDropdown.ClearOptions();
			var options = new List<TMP_Dropdown.OptionData>();
			foreach (var name in CareerFilterNames)
			{
				options.Add(new TMP_Dropdown.OptionData(name));
			}
			SoldierCardDropdown.AddOptions(options);
			SoldierCardDropdown.value = 0;
			SoldierCardDropdown.RefreshShownValue();

			SoldierCardDropdown.onValueChanged.RemoveAllListeners();
			SoldierCardDropdown.onValueChanged.AddListener(OnDropdownChanged);
		}

		private void OnDropdownChanged(int index)
		{
			if (index == 0)
			{
				_currentFilter = null;
			}
			else
			{
				_currentFilter = (CareerType)(index - 1);
			}

			SetSoldierList();
		}

		private List<PlayerCard> GetFilteredSoldierList()
		{
			if (_currentFilter == null)
				return _allSoldierList;

			return _allSoldierList.Where(soldier =>
			{
				var soldierCfg = _soldierBattleConfig.GetOrDefault(soldier.CardId);
				return soldierCfg != null && soldierCfg.CarType == _currentFilter;
			}).ToList();
		}

		private void SetSoldierList()
		{
			if (SoldierCardScroll == null) return;

			var filteredList = GetFilteredSoldierList();

			var haveCount = filteredList.Count(soldier => soldier.IsHave);
			var totalCount = filteredList.Count;
			var cardNumText = CardNum.transform.Find("Text");
			if (cardNumText != null)
			{
				cardNumText.GetComponent<TMP_Text>().text = $"{haveCount}/{totalCount}";
			}

			SoldierCardScroll.ScrollToIndex(0, ScrollAlignment.Start, false);
			SoldierCardScroll.SetUpdateFunc(OnUpdateCardItem);
			SoldierCardScroll.ShowList(filteredList.Count);
			SoldierCardScroll.PlayScaleAnimationDOTween();
		}

		private void OnUpdateCardItem(int dataIndex, ScrollItem scrollItem)
		{
			var filteredList = GetFilteredSoldierList();
			if (dataIndex < 0 || dataIndex >= filteredList.Count)
			{
				scrollItem.gameObject.SetActive(false);
				return;
			}

			var bagScrollItem = (BagScrollItem)scrollItem;
			var playerSoldier = filteredList[dataIndex];
			bagScrollItem.gameObject.SetActive(true);

			bagScrollItem.GetComponent<SoldierSlot>().SetData(playerSoldier.CardId);
		}

		public void RefreshSoldierList()
		{
			InitSoldierList();
			SetSoldierList();
		}
	}
}
