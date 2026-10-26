using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Model;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using ScrollView;

namespace FusionContinent
{
	public class GeneralCardResolvePanelData : UIPanelData
	{
		public HashSet<int> SelectedCardIds = new HashSet<int>();
	}
	public partial class GeneralCardResolvePanel : UIPanel
	{
		private QualityType? _currentFilter = null;
		private TbSpecialEffectsConfig _specialEffectsConfig;
		private List<PlayerCard> _filteredCardList = new List<PlayerCard>();
		private TMPro.TMP_Dropdown _cardFilterDropdown;
		
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as GeneralCardResolvePanelData ?? new GeneralCardResolvePanelData();

			_specialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

			InitCardFilterDropdown();
		}

		/// <summary>
		/// 初始化卡牌筛选下拉框
		/// </summary>
		private void InitCardFilterDropdown()
		{
			if (CardFilter == null) return;

			_cardFilterDropdown = CardFilter.GetComponent<TMPro.TMP_Dropdown>();
			if (_cardFilterDropdown == null) return;

			_cardFilterDropdown.ClearOptions();

			var options = new List<TMPro.TMP_Dropdown.OptionData>();

			// 添加"全部"选项
			options.Add(new TMPro.TMP_Dropdown.OptionData("全部"));

			// 添加各品质选项
			options.Add(new TMPro.TMP_Dropdown.OptionData("普通"));
			options.Add(new TMPro.TMP_Dropdown.OptionData("稀有"));
			options.Add(new TMPro.TMP_Dropdown.OptionData("史诗"));
			options.Add(new TMPro.TMP_Dropdown.OptionData("传说"));
			options.Add(new TMPro.TMP_Dropdown.OptionData("终极"));
			options.Add(new TMPro.TMP_Dropdown.OptionData("神话"));

			_cardFilterDropdown.AddOptions(options);
			_cardFilterDropdown.value = 0;

			_cardFilterDropdown.onValueChanged.RemoveAllListeners();
			_cardFilterDropdown.onValueChanged.AddListener(OnFilterChanged);
		}
		
		private TbCardDevelopConfig _cardDevelopConfig;

		/// <summary>
		/// 执行分解
		/// </summary>
		private void OnFilterChanged(int index)
		{
			_currentFilter = index == 0 ? null : (QualityType)(index);
			RefreshCardList();
		}
		
		protected override void OnOpen(IUIData uiData = null)
		{
			_currentFilter = null;
			mData.SelectedCardIds.Clear();
			
			if (_cardFilterDropdown != null)
			{
				_cardFilterDropdown.value = 0;
			}
			
			if (GeneralCardScroll != null)
			{
				GeneralCardScroll.SetUpdateFunc(OnUpdateCardItem);
			}
			
			RefreshCardList();
		}
		
		private void RefreshCardList()
		{
			var allCards = Global.PlayerCards.Values
				.Where(card => card.Config.Type == CardType.GeneralSkill)
				.ToList();
			
			if (_currentFilter.HasValue)
			{
				_filteredCardList.Clear();
				foreach (var card in allCards)
				{
					var skillCfg = _specialEffectsConfig.GetOrDefault(card.CardId);
					if (skillCfg != null && skillCfg.LevelPropertyList.Count > 0)
					{
						var quality = skillCfg.LevelPropertyList[0].Quality;
						if (quality == _currentFilter.Value)
						{
							_filteredCardList.Add(card);
						}
					}
				}
			}
			else
			{
				_filteredCardList = allCards;
			}
			
			if (GeneralCardScroll != null)
			{
				GeneralCardScroll.ScrollToIndex(0, ScrollView.ScrollAlignment.Start, false);
				GeneralCardScroll.ShowList(_filteredCardList.Count);
			}
		}
		
		private void OnUpdateCardItem(int dataIndex, ScrollItem scrollItem)
		{
			if (dataIndex < 0 || dataIndex >= _filteredCardList.Count)
			{
				scrollItem.gameObject.SetActive(false);
				return;
			}
			
			var playerCard = _filteredCardList[dataIndex];
			var config = playerCard.Config;
			var skillCfg = _specialEffectsConfig.GetOrDefault(playerCard.CardId);
			var quality = QualityType.Default;
			if (skillCfg != null && skillCfg.LevelPropertyList.Count > 0)
			{
				quality = skillCfg.LevelPropertyList[0].Quality;
			}
			
			var isSelected = mData.SelectedCardIds.Contains(playerCard.CardId);
			
			var slotData = new GeneralCardSlotData
			{
				CardId = playerCard.CardId,
				NameText = config.Name,
				QualityType = quality,
				IconName = config.Icon,
				IconScale = Vector3.one * 1.1f,
				IsSelected = isSelected,
				// OnSelectChanged = OnCardSelectChanged
			};
			var slot = scrollItem.GetComponent<GeneralCardSlot>();
			var bagScrollItem = scrollItem as BagScrollItem;
			bagScrollItem.OnSpawn();
			bagScrollItem.OnClick = () =>
			{
				OnCardSelectChanged(playerCard.CardId,mData.SelectedCardIds.Contains(playerCard.CardId));
				// slot.SetSelected(mData.SelectedCardIds.Contains(playerCard.CardId));
			};
			if (slot != null)
			{
				slot.SetData(slotData);
			
			}
			
			
		}
		
		private void OnCardSelectChanged(int cardId, bool isSelected)
		{
			if (isSelected)
			{
				mData.SelectedCardIds.Add(cardId);
			}
			else
			{
				mData.SelectedCardIds.Remove(cardId);
			}
		}
		
		protected override void OnShow()
		{
		}
		
		protected override void OnHide()
		{
			mData.SelectedCardIds.Clear();
		}
		
		protected override void OnClose()
		{
		}
	}
}
