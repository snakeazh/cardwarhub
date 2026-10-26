using System.Collections.Generic;
using Config;
using DG.Tweening;
using Game.RedDot;
using Game.RedDot.View;
using Game.Soldier;
using Game.Soldier.RedDot;
using UIFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace FusionContinent
{
    [PrefabPath("GeneralCardConbinePanel")]
    public class GeneralCardConbinePanelNew : UIPanel<GeneralCardConbinePanelViewModel>
    {
        // 作为 BottomBar 的子面板，不参与 UIManager 全局栈管理
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        private GeneralCardConbinePanel mOldPanel;

        private UnityAction _conbineBtnHandler;
        private UnityAction _questionBtnHandler;
        private System.Action<int> _conbineSucceededHandler;
        private System.Action _conbineFailedHandler;
        private List<QualityType> UIQualityList = new List<QualityType>();

        private readonly List<(Button button, UnityAction handler)> _cardClickBindings =
            new List<(Button, UnityAction)>();

        private readonly GeneralCardConbinePanelRedDotScope mRedDotScope = new GeneralCardConbinePanelRedDotScope();

        private int _selectedQualityIndex = -1;
        void Awake()
        {
            UIQualityList.Add(QualityType.Ultimate);
            UIQualityList.Add(QualityType.Legend);
            UIQualityList.Add(QualityType.Epic);
            UIQualityList.Add(QualityType.Rarity);
            UIQualityList.Add(QualityType.Ordinary);
            mOldPanel = GetComponent<GeneralCardConbinePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GeneralCardConbinePanelNew] 找不到 GeneralCardConbinePanel 组件");
            }
        }

        protected override void OnBindViewModel(GeneralCardConbinePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mRedDotScope.Open();
            BindButtons(vm);
            BindCardsContent(vm);

            _conbineSucceededHandler = OnConbineSucceeded;
            vm.OnConbineSucceeded += _conbineSucceededHandler;

            _conbineFailedHandler = ShowCombineBefore;
            vm.OnConbineFailed += _conbineFailedHandler;
        }

        private void OnConbineSucceeded(int cardId)
        {
            mRedDotScope.NotifyDataChanged();
            RefreshCardsContentSelection(VM);
            ShowCombineAfter(cardId);
        }

        protected override void OnUnbindViewModel(GeneralCardConbinePanelViewModel vm)
        {
            if (_conbineSucceededHandler != null)
            {
                vm.OnConbineSucceeded -= _conbineSucceededHandler;
                _conbineSucceededHandler = null;
            }

            if (_conbineFailedHandler != null)
            {
                vm.OnConbineFailed -= _conbineFailedHandler;
                _conbineFailedHandler = null;
            }

            UnbindButtons();
            UnbindCardsContent(vm);
            mRedDotScope.Close();
        }

        protected override void OnOpen(UIFramework.IUIData uiData)
        {
            VM.ResetOnOpen();
            RefreshCardsContentSelection(VM);
            ShowCombineBefore();
            this.mOldPanel.Gem.DOAnchorPosY(400f, 1f, true).SetEase(Ease.Linear).SetLoops(-1,LoopType.Yoyo);
        }

        private void RefreshCardsContentSelection(GeneralCardConbinePanelViewModel vm)
        {
            if (mOldPanel == null || mOldPanel.CardsContent == null) return;

            // 先全量重置，避免 prefab 默认状态残留
            for (int i = 0; i < mOldPanel.CardsContent.childCount; i++)
            {
                SetCardSelectedVisual(mOldPanel.CardsContent.GetChild(i), false);
                RefreshCardComposeCost(mOldPanel.CardsContent.GetChild(i), i);
            }

            var quality = vm.SelectedQuality.Value;
            var index = UIQualityList.IndexOf(quality);
            if (index < 0) index = 0;

            if (index >= 0 && index < mOldPanel.CardsContent.childCount)
            {
                _selectedQualityIndex = index;
                SetCardSelectedVisual(mOldPanel.CardsContent.GetChild(index), true);
            }
        }

        private void RefreshCardComposeCost(Transform cardRoot, int index)
        {
            if (cardRoot == null) return;
            if (UIQualityList == null || index < 0 || index >= UIQualityList.Count) return;

            var quality = UIQualityList[index];
            var arcCfg = DataConfigManager.Tables.TbArcCardProConfig.GetOrDefault(quality);

            var gem = cardRoot.Find("Gem")?.GetComponent<Image>();
            var numText = cardRoot.Find("Num")?.GetComponent<TMP_Text>();

            if (gem == null && numText == null) return;

            if (arcCfg == null || arcCfg.ComposeRewards == null || arcCfg.ComposeRewards.Count == 0)
            {
                if (numText != null) numText.text = "X 0";
                if (gem != null) gem.sprite = null;
                return;
            }

            // 当前 UI 结构只有一个 Gem + Num，取第一个消耗作为展示
            var cost = arcCfg.ComposeRewards[0];
            if (numText != null)
            {
                numText.text = $"X {cost.Num}";
                var haveNum = Global.GetItemNumById(cost.Id);
                numText.color = haveNum < cost.Num ? Color.red : Color.white;
            }

            if (gem != null)
            {
                var itemCfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(cost.Id);
                gem.sprite = itemCfg != null ? Global.ItemIconSpriteAtlas.GetSpriteExt(itemCfg.Icon) : null;
            }

            BindCardRedDot(cardRoot, quality);
        }

        private void BindCardRedDot(Transform cardRoot, QualityType quality)
        {
            if (!mRedDotScope.IsOpen)
            {
                return;
            }

            ClearChildLocalRedDotViews(cardRoot);
            cardRoot.BindLocalRedDot(mRedDotScope, GeneralCardConbinePanelRedDotScope.Paths.Card(quality));
            LocalRedDotView view = cardRoot.GetComponent<LocalRedDotView>();
            if (view != null)
            {
                view.RefreshWatch();
            }
        }

        private void ClearChildLocalRedDotViews(Transform root)
        {
            LocalRedDotView[] childViews = root.GetComponentsInChildren<LocalRedDotView>(true);
            for (int i = 0; i < childViews.Length; i++)
            {
                LocalRedDotView view = childViews[i];
                if (view.transform != root)
                {
                    Destroy(view.gameObject);
                }
            }
        }

        private void BindButtons(GeneralCardConbinePanelViewModel vm)
        {
            if (mOldPanel.ConbineBtn != null)
            {
                _conbineBtnHandler = () =>
                {
                    var quality = vm.SelectedQuality.Value;
                    var arcCfg = DataConfigManager.Tables.TbArcCardProConfig.GetOrDefault(quality);
                    if (arcCfg == null || arcCfg.ComposeRewards == null || arcCfg.ComposeRewards.Count == 0)
                    {
                        Toast.Show("道具数量不足");
                        return;
                    }

                    foreach (var cost in arcCfg.ComposeRewards)
                    {
                        if (cost == null) continue;
                        var haveNum = Global.GetItemNumById(cost.Id);
                        if (haveNum < cost.Num)
                        {
                            Toast.Show("道具数量不足");
                            return;
                        }
                    }

                    ShowCombining();
                    vm.ConbineCommand.Execute();
                };
                mOldPanel.ConbineBtn.onClick.AddListener(_conbineBtnHandler);
            }

            if (mOldPanel.QuestionBtn != null)
            {
                _questionBtnHandler = () => vm.QuestionCommand.Execute();
                mOldPanel.QuestionBtn.onClick.AddListener(_questionBtnHandler);
            }

            if (mOldPanel.Mask != null)
            {
                mOldPanel.Mask.onClick.RemoveAllListeners();
                mOldPanel.Mask.onClick.AddListener(ShowCombineBefore);
            }
        }

        private void UnbindButtons()
        {
            if (mOldPanel == null) return;

            if (_conbineBtnHandler != null && mOldPanel.ConbineBtn != null)
            {
                mOldPanel.ConbineBtn.onClick.RemoveListener(_conbineBtnHandler);
                _conbineBtnHandler = null;
            }

            if (_questionBtnHandler != null && mOldPanel.QuestionBtn != null)
            {
                mOldPanel.QuestionBtn.onClick.RemoveListener(_questionBtnHandler);
                _questionBtnHandler = null;
            }
        }

        private void BindCardsContent(GeneralCardConbinePanelViewModel vm)
        {
            if (mOldPanel == null || mOldPanel.CardsContent == null) return;

            // 清理旧绑定（避免重复绑定）
            UnbindCardsContent(vm);

            for (int i = 0; i < mOldPanel.CardsContent.childCount; i++)
            {
                var child = mOldPanel.CardsContent.GetChild(i);
                if (child == null) continue;

                int clickIndex = i;
                var button = child.GetComponent<Button>();
                if (button == null) continue;

                UnityAction handler = () => SetSelectedQualityIndex(clickIndex, vm);
                button.onClick.AddListener(handler);

                _cardClickBindings.Add((button, handler));
            }
        }

        private void UnbindCardsContent(GeneralCardConbinePanelViewModel vm)
        {
            for (int i = 0; i < _cardClickBindings.Count; i++)
            {
                var binding = _cardClickBindings[i];
                if (binding.button == null) continue;
                if (binding.handler != null)
                {
                    binding.button.onClick.RemoveListener(binding.handler);
                }
            }

            _cardClickBindings.Clear();
            _selectedQualityIndex = -1;
        }

        private void SetSelectedQualityIndex(int newIndex, GeneralCardConbinePanelViewModel vm)
        {
            if (mOldPanel == null || mOldPanel.CardsContent == null) return;
            if (newIndex < 0 || newIndex >= mOldPanel.CardsContent.childCount) return;

            if (_selectedQualityIndex == newIndex)
            {
                // 仍然保持单选：重复点不取消
                return;
            }

            // 取消旧选中
            if (_selectedQualityIndex >= 0 && _selectedQualityIndex < mOldPanel.CardsContent.childCount)
            {
                SetCardSelectedVisual(mOldPanel.CardsContent.GetChild(_selectedQualityIndex), false);
            }

            // 设置新选中
            _selectedQualityIndex = newIndex;
            SetCardSelectedVisual(mOldPanel.CardsContent.GetChild(_selectedQualityIndex), true);

            // 同步到 VM（用于后续合成逻辑）
            if (UIQualityList != null && _selectedQualityIndex >= 0 && _selectedQualityIndex < UIQualityList.Count)
            {
                vm.SetSelectedQuality(UIQualityList[_selectedQualityIndex]);
            }
        }

        private void SetCardSelectedVisual(Transform cardRoot, bool selected)
        {
            if (cardRoot == null) return;

            var selectedNode = cardRoot.Find("Selected");
            var unSelectedNode = cardRoot.Find("UnSelected");

            if (selectedNode != null) selectedNode.gameObject.SetActive(selected);
            if (unSelectedNode != null) unSelectedNode.gameObject.SetActive(!selected);
        }

        private void ShowCombineBefore()
        {
            if (mOldPanel == null) return;
            RefreshCardsContentSelection(VM);
            if (mOldPanel.CombineBefore != null) mOldPanel.CombineBefore.gameObject.SetActive(true);
            if (mOldPanel.CombineAfter != null) mOldPanel.CombineAfter.gameObject.SetActive(false);
        }

        private void ShowCombining()
        {
            if (mOldPanel == null) return;
            if (mOldPanel.CombineBefore != null) mOldPanel.CombineBefore.gameObject.SetActive(false);
            if (mOldPanel.CombineAfter != null) mOldPanel.CombineAfter.gameObject.SetActive(false);
        }

        private void ShowCombineAfter(int cardId)
        {
            if (mOldPanel == null) return;
            if (mOldPanel.CombineBefore != null) mOldPanel.CombineBefore.gameObject.SetActive(false);
            if (mOldPanel.CombineAfter != null) mOldPanel.CombineAfter.gameObject.SetActive(true);

            // 这里已经拿到服务器回包的 cardId（本次合成产物）
            LogTool.Log($"[GeneralCardConbinePanelNew] CombineAfter cardId={cardId}");
            if (Global.PlayerCards.TryGetValue(cardId, out var playerCard))
            {
                var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(playerCard.CardId);
                var cardData = new GeneralCardSlotData()
                {
                    CardId = playerCard.CardId,
                    NameText = playerCard.Config.Name,
                    LevelText = $"{playerCard.Lv}",
                    Desc = playerCard.Config.Desc,
                    State = playerCard.Config.State,
                    IsUnlock = playerCard.IsHave,
                    IconScale = Vector3.one * 1.1f,
                    QualityType = skillCfg.LevelPropertyList[playerCard.Lv - 1].Quality,
                    // ProgressBar = GetCardProgressData(playerSkill),
                    CheckRed = false,
                };
                cardData.IconName = playerCard.Config.Icon;
                this.mOldPanel.CombineAfter.Find("GeneralCardSlot").GetComponent<GeneralCardSlot>().SetData(cardData);
                // GetComponent<GeneralCardSlot>().SetData(cardData);
                this.mOldPanel.NameText.text = cardData.NameText;
                // this.mOldPanel.
            }

        }
    }
}
