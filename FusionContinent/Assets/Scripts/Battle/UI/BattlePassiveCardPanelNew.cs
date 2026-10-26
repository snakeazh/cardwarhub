using Game.Battle;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Config;
using FusionContinent.Helper;
using FusionContinent.Model;

namespace FusionContinent
{
    [PrefabPath("BattlePassiveCardPanel")]
    /// <summary>
    /// 战斗被动卡牌子界面 - 新框架版本
    /// 管理被动技能卡牌的显示和详情
    /// 作为 BattlePanelNew 的子界面，不参与 UIManager 堆栈管理
    /// 使用与 GameLandUI 相同的数据结构
    /// </summary>
    public class BattlePassiveCardPanelNew : UIPanel<BattlePassiveCardViewModel>
    {
        /// <summary>
        /// 作为子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        // 引用 BattlePassiveCardPanel 的 UI 组件
        private BattlePassiveCardPanel mOldPanel;

        // 缓存卡牌对象
        private Dictionary<int, GameObject> _passiveCards = new Dictionary<int, GameObject>();
        
        // 事件处理器
        private System.Action<int> _cardSelectedHandler;
        private System.Action _detailClosedHandler;

        void Awake()
        {
            // 获取 BattlePassiveCardPanel 引用
            mOldPanel = GetComponent<BattlePassiveCardPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[BattlePassiveCardPanelNew] BattlePassiveCardPanel 组件未找到");
            }
        }

        protected override void OnBindViewModel(BattlePassiveCardViewModel vm)
        {
            if (mOldPanel == null) return;

            // 初始化被动卡牌
            InitializePassiveCards(vm);

            // 绑定事件
            BindEvents(vm);

            LogTool.Log("[BattlePassiveCardPanelNew] 绑定完成");
        }

        protected override void OnUnbindViewModel(BattlePassiveCardViewModel vm)
        {
            UnbindEvents(vm);
        }

        /// <summary>
        /// 初始化被动卡牌
        /// </summary>
        private void InitializePassiveCards(BattlePassiveCardViewModel vm)
        {
            // 清除旧卡牌
            ClearPassiveCards();

            foreach (var cardId in vm.PassiveCardIds.Value)
            {
                CreatePassiveCard(vm, cardId);
            }
        }

        /// <summary>
        /// 清除被动卡牌
        /// </summary>
        private void ClearPassiveCards()
        {
            if (mOldPanel?.PassiveCardObj == null) return;

            var parent = mOldPanel.PassiveCardObj.transform.parent;
            if (parent == null) return;

            // 保留第一个（模板），删除其他
            for (int i = parent.childCount - 1; i >= 1; i--)
            {
                Destroy(parent.GetChild(i).gameObject);
            }

            _passiveCards.Clear();
        }

        /// <summary>
        /// 创建被动卡牌 - 使用与 GameLandUI 相同的数据结构
        /// </summary>
        private void CreatePassiveCard(BattlePassiveCardViewModel vm, int cardId)
        {
            if (mOldPanel?.PassiveCardObj == null) return;

            // 获取数据 - 与 GameLandUI 一致
            var playerCard = vm.GetPlayerCard(cardId);
            if (playerCard == null) return;

            var cardCfg = vm.GetCardConfig(cardId);
            var skillCfg = vm.GetSkillConfig(cardId);
            if (cardCfg == null || skillCfg == null) return;

            var skillLevelCfg = vm.GetSkillLevelConfig(cardId, playerCard.Lv);
            if (skillLevelCfg == null) return;

            var parent = mOldPanel.PassiveCardObj.transform.parent;
            if (parent == null) return;

            // 实例化卡牌
            GameObject cardObj = Instantiate(mOldPanel.PassiveCardObj.gameObject, parent);
            cardObj.transform.localScale = Vector3.one;
            cardObj.SetActive(true);

            // 缓存
            _passiveCards[cardId] = cardObj;

            // 更新UI - 与 GameLandUI 一致
            UpdatePassiveCardUI(cardObj, cardCfg, skillLevelCfg);

            // 绑定点击事件
            BindCardClickEvent(vm, cardObj, cardId, cardCfg, skillCfg, playerCard, skillLevelCfg);
        }

        /// <summary>
        /// 更新被动卡牌UI - 与 GameLandUI 一致
        /// </summary>
        private void UpdatePassiveCardUI(GameObject cardObj, CardBaseConfig cardCfg, SpecialLevelPropertyList skillLevelCfg)
        {
            // 更新边框 - 与 GameLandUI 第132行一致
            var bg = cardObj.transform.Find("Bg")?.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = Global.GeneralSkillFrameIconSpriteAtlas.GetSprite(skillLevelCfg.Quality.ToString());
            }

            // 更新图标 - 与 GameLandUI 第133行一致
            var icon = cardObj.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && !string.IsNullOrEmpty(cardCfg.Icon))
            {
                SpriteHelper.TryLoadPaintAsync(cardCfg.Icon, icon, groupName: "GeneralSkill");
            }
        }

        /// <summary>
        /// 绑定卡牌点击事件 - 与 GameLandUI 一致
        /// </summary>
        private void BindCardClickEvent(BattlePassiveCardViewModel vm, GameObject cardObj, int cardId, 
            CardBaseConfig cardCfg, SpecialEffectsConfig skillCfg, PlayerCard playerCard, SpecialLevelPropertyList skillLevelCfg)
        {
            var button = cardObj.GetComponent<Button>();
            if (button == null) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                // 触发引导 - 与 GameLandUI 第139行一致
                Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.PressArcaneShowDetails);
                
                // 显示详情
                ShowDetailPanel(cardId, cardCfg, playerCard, skillLevelCfg);
            });
        }

        /// <summary>
        /// 显示详情面板 - 与 GameLandUI 一致
        /// </summary>
        private void ShowDetailPanel(int cardId, CardBaseConfig cardCfg, PlayerCard playerCard, SpecialLevelPropertyList skillLevelCfg)
        {
            if (mOldPanel?.PassiveCardDetail == null) return;

            var detailPanel = mOldPanel.PassiveCardDetail;
            detailPanel.gameObject.SetActive(true);

            // 设置位置 - 与 GameLandUI 第143-146行一致
            if (_passiveCards.TryGetValue(cardId, out var cardObj))
            {
                detailPanel.transform.position = new Vector3(
                    detailPanel.transform.position.x,
                    cardObj.transform.position.y,
                    detailPanel.transform.position.z
                );
                detailPanel.anchoredPosition = new Vector2(35, detailPanel.anchoredPosition.y - 100f);
            }

            // 绑定关闭按钮 - 与 GameLandUI 第148-153行一致
            var quitBtn = detailPanel.Find("Quit")?.GetComponent<Button>();
            if (quitBtn != null)
            {
                quitBtn.onClick.RemoveAllListeners();
                quitBtn.onClick.AddListener(() =>
                {
                    detailPanel.gameObject.SetActive(false);
                });
            }

            // 设置卡牌槽 - 使用 GeneralCardSlot
            var cardSlotTran = detailPanel.Find("GeneralCardSlot");
            if (cardSlotTran != null)
            {
                cardSlotTran.localScale = Vector3.one * 0.6f;
                var generalCardSlot = cardSlotTran.GetComponent<GeneralCardSlot>();
                if (generalCardSlot != null)
                {
                    generalCardSlot.SetData(BuildGeneralCardSlotData(playerCard, cardCfg, skillLevelCfg));
                }
            }

            // 设置面板名称 - 与 GameLandUI 第164行一致
            var panelName = detailPanel.Find("CardName")?.GetComponent<TMP_Text>();
            if (panelName != null) panelName.text = cardCfg.Name;

            // 设置描述 - 与 GameLandUI 第165行一致
            var descText = detailPanel.Find("Desc/DescText")?.GetComponent<TMP_Text>();
            if (descText != null) descText.text = cardCfg.Desc;

            // 设置背景 - 与 GameLandUI 第166行一致
            var panelBg = detailPanel.Find("Bg")?.GetComponent<Image>();
            if (panelBg != null)
            {
                SpriteHelper.TryLoadTextureAsync($"{skillLevelCfg.Quality}ArcaneCardFrame", panelBg);
            }
        }

        private GeneralCardSlotData BuildGeneralCardSlotData(PlayerCard playerCard, CardBaseConfig cardCfg, SpecialLevelPropertyList skillLevelCfg)
        {
            return new GeneralCardSlotData
            {
                CardId = playerCard.CardId,
                NameText = cardCfg.Name,
                LevelText = $"{playerCard.Lv}",
                Desc = cardCfg.Desc,
                State = cardCfg.State,
                IsUnlock = true,
                QualityType = skillLevelCfg.Quality,
                IconName = cardCfg.Icon,
                IconScale = Vector3.one * 1.1f,
            };
        }

        /// <summary>
        /// 绑定事件
        /// </summary>
        private void BindEvents(BattlePassiveCardViewModel vm)
        {
            _cardSelectedHandler = (cardId) =>
            {
                // 详情面板已在点击事件中显示
            };
            vm.OnCardSelected += _cardSelectedHandler;

            _detailClosedHandler = () =>
            {
                if (mOldPanel?.PassiveCardDetail != null)
                {
                    mOldPanel.PassiveCardDetail.gameObject.SetActive(false);
                }
            };
            vm.OnDetailClosed += _detailClosedHandler;
        }

        /// <summary>
        /// 解绑事件
        /// </summary>
        private void UnbindEvents(BattlePassiveCardViewModel vm)
        {
            if (_cardSelectedHandler != null)
            {
                vm.OnCardSelected -= _cardSelectedHandler;
                _cardSelectedHandler = null;
            }

            if (_detailClosedHandler != null)
            {
                vm.OnDetailClosed -= _detailClosedHandler;
                _detailClosedHandler = null;
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            gameObject.SetActive(true);
        }

        protected override void OnShow()
        {
            gameObject.SetActive(true);
        }

        protected override void OnHide()
        {
            gameObject.SetActive(false);
        }

        protected override void OnClose()
        {
            base.OnClose();
            _passiveCards.Clear();
        }
    }
}
