using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Animation;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Events;

namespace FusionContinent
{
	public class CardGroupRecommendData : UIPanelData
	{
	}
	public partial class CardGroupRecommend : UIPanel
	{
		private CardGroupRecomConfig _recommendCfg;
		private readonly Dictionary<int, Button> _buttonMap = new Dictionary<int, Button>();

		private TbCardBaseConfig _cardBaseConfig;
		private TbCardGroupRecomConfig _cardGroupRecommendConfig;

		protected void Awake()
		{
			_cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
			_cardGroupRecommendConfig = DataConfigManager.Tables.TbCardGroupRecomConfig;
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as CardGroupRecommendData ?? new CardGroupRecommendData();
			// please add init code here
			Quit.onClick.AddListener(() =>
			{
				RunAnimation(GetAnimationCloseNode, () =>
				{
					UIKit.ClosePanel(this);
				});
			});
			ApplyBtn.onClick.AddListener(ApplyRecommend);

			// 清理动态创建的按钮，保留模板
			var templateBtn = LeftBtn.Find("Button")?.gameObject;
			for (int i = LeftBtn.childCount - 1; i >= 0; i--)
			{
				var child = LeftBtn.GetChild(i).gameObject;
				if (child != templateBtn)
					Destroy(child);
			}
			_buttonMap.Clear();

			for (int i = 0; i < _cardGroupRecommendConfig.DataList.Count; i++)
			{
				GameObject btnObj = Instantiate(templateBtn, LeftBtn, false);
				btnObj.SetActive(true);
				btnObj.name = $"Button_{i}";
				var recommendCfg = _cardGroupRecommendConfig.DataList[i];
				btnObj.transform.Find("Text").GetComponent<TMP_Text>().text = recommendCfg.Name;

				// 点击事件
				btnObj.GetComponent<Button>().onClick.AddListener(() =>
				{
					for (int j = 0; j < LeftBtn.childCount; j++)
					{
						var select = LeftBtn.GetChild(j).Find("Select");
						if (select != null) select.localScale = Vector3.zero;
					}

					var selectSelf = btnObj.transform.Find("Select");
					if (selectSelf != null) selectSelf.localScale = Vector3.one;

					_recommendCfg = recommendCfg;
					SetData();
				});

				_buttonMap.Add(recommendCfg.Id, btnObj.GetComponent<Button>());
			}

			// 隐藏模板按钮
			if (templateBtn != null) templateBtn.SetActive(false);

			if (_buttonMap.Count > 0)
				ChangePage(_buttonMap.First().Key);
		}

		protected override void OnAnimationOpen()
		{
			base.OnAnimationOpen();
			UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.SetCardGroup,2,ApplyBtn.GetComponent<RectTransform>());
		}

		private void SetData()
		{
			if (_recommendCfg == null) return;

			DescText.text = _recommendCfg.Desc;

			// 流派
			for (int i = 0; i < SchoolList.childCount; i++)
			{
				GameObject cardObj = SchoolList.GetChild(i).gameObject;
				cardObj.SetActive(_recommendCfg.Schools.Count > i);
				if (!cardObj.activeSelf) continue;

				var schoolCfg = DataConfigManager.Tables.TbSchoolConfig.GetOrDefault(_recommendCfg.Schools[i]);
				var schoolCountCfg = schoolCfg.CardCountList.First();
				cardObj.transform.Find("Icon").GetComponent<Image>().sprite = Global.SchoolIconAtlas.GetSprite(schoolCountCfg.Icon);
				cardObj.transform.Find("Iconbg").GetComponent<Image>().sprite = Global.SchoolIconAtlas.GetSprite(schoolCountCfg.Quality.ToString()); // 使用匹配项的属性
				cardObj.transform.Find("CardCount").gameObject.SetActive(false);
			}

			// 角色
			var soldierCards = _recommendCfg.Cards.Where(v => _cardBaseConfig.GetOrDefault(v)?.Type == CardType.Soldier).ToList();
			for (int i = 0; i < SoldierList.childCount; i++)
			{
				GameObject cardObj = SoldierList.GetChild(i).gameObject;
				cardObj.SetActive(soldierCards.Count > i);
				if (!cardObj.activeSelf) continue;

				// 设置卡牌数据
				var baseCfg = _cardBaseConfig.GetOrDefault(soldierCards[i]);
				cardObj.GetComponent<SoldierCardSlot>().SetData(new CardSlotData(){
					Icon = Global.CardIconSpriteAtlas.GetSpriteExt(baseCfg.Icon),
					CardId = baseCfg.Id,
					NameText = baseCfg.Name,
					LevelText = $"{Global.GetCardById(baseCfg.Id)?.Lv}",
					QualityType = QualityType.Ordinary,
					Desc = baseCfg.Desc,
				});
				cardObj.GetComponent<ScrollViewItem>().SetScrollView(ScrollView.GetComponent<ScrollRect>());
			}

			// 技能 - 使用独立索引避免跳过角色卡后索引错位
			for (int i = 0; i < SkillList.childCount; i++)
				SkillList.GetChild(i).gameObject.SetActive(false);

			int skillIndex = 0;
			for (int i = 0; i < _recommendCfg.Cards.Count; i++)
			{
				var cardId = _recommendCfg.Cards[i];
				if (soldierCards.Contains(cardId)) continue;

				GameObject cardObj = skillIndex >= SkillList.childCount
					? Instantiate(SkillList.GetChild(0).gameObject, SkillList, true).gameObject
					: SkillList.GetChild(skillIndex).gameObject;
				cardObj.SetActive(true);

				// 设置卡牌数据
				var baseCfg = _cardBaseConfig.GetOrDefault(cardId);
				cardObj.GetComponent<CardSlot>().SetData(new CardSlotData(){
					Icon = Global.CardIconSpriteAtlas.GetSpriteExt(baseCfg.Icon),
					CardId = baseCfg.Id,
					NameText = baseCfg.Name,
					LevelText = $"{Global.GetCardById(baseCfg.Id)?.Lv}",
					QualityType = QualityType.Ordinary,
					Desc = baseCfg.Desc,
				});
				cardObj.GetComponent<ScrollViewItem>().SetScrollView(ScrollView.GetComponent<ScrollRect>());

				skillIndex++;
			}

			// 引导 - 应用按钮
			// GuideFlow.Instance.SetStep(GuideType.ConfigureTheDeck, 3, ApplyBtn.transform);
		}

		private void ChangePage(int id)
		{
			if (_buttonMap.TryGetValue(id, out var btn))
				btn.onClick.Invoke();
		}
		protected override void RunAnimation(GameObject target, UnityAction complete)
		{
			UIAnimationUtils.PlayAnimations(target,complete);
		}

		protected override GameObject GetAnimationCloseNode
		{
			get => this.CloseAnimation.gameObject;
		}

		protected override GameObject GetAnimationOpenNode
		{
			get => this.OpenAnimation.gameObject;
		}
		private void ApplyRecommend()
		{
			UnlockGuideManager.Send(ModuleFunctionType.SetCardGroup,2);
			RunAnimation(GetAnimationCloseNode, () =>
			{
				UIFramework.UIManager.GetPanel<CardGroupPanelNew>().SelectCards(_recommendCfg.Cards);
				// GuideFlow.Instance.SetStep(GuideType.ConfigureTheDeck, 4, CardGroupPanel2.Default.GeneralSkillTag.transform);
				UIKit.ClosePanel(this);
			});
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			// 引导 - 选择推荐配置
			// var stepCfg = GuideFlow.Instance.GetCurrentStepCfg(GuideType.SetRecommendCardGroup,3);
			// if (stepCfg != null && stepCfg.SkipPath.Count > 0)
			// {
			// 	var paramId = int.Parse(stepCfg.SkipPath.First().Param);
			// 	if (_buttonMap.TryGetValue(paramId, out var btn))
			// 	{
			// 		//GuideFlow.Instance.SetStep(GuideType.SetRecommendCardGroup, 3, btn.transform);
			// 	}
			// }
		}

		protected override void OnShow()
		{

		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			// 清理按钮监听器
			foreach (var kvp in _buttonMap)
			{
				if (kvp.Value != null)
					kvp.Value.onClick.RemoveAllListeners();
			}
			_buttonMap.Clear();
			_recommendCfg = null;
			Quit.onClick.RemoveAllListeners();
			ApplyBtn.onClick.RemoveAllListeners();
			gameObject.SetActive(false);
		}
	}
}
