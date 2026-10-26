using System;
using Config;
using Game.RedDot;
using Game.Soldier;
using UIFramework;
using UnityEngine;
using TMPro;
using FusionContinent;
using FusionContinent.Model;
using ScrollView;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("GeneralSkillPanel")]
    public class GeneralSkillPanelNew : UIPanel<GeneralSkillPanelViewModel>
    {
        private GeneralSkillPanel mOldPanel;
        private System.Action<string, string> _cardCountHandler;
        private System.Action<int> _openSkillUpgradeHandler;
        private UnityEngine.Events.UnityAction _decomposeButtonHandler;
        private UnityEngine.Events.UnityAction _combineButtonHandler;
        private System.Action _onOpenResolvePanelHandler;
        private System.Action _onOpenConbinePanelHandler;

        private UnityEngine.Events.UnityAction<int> _dropdownHandler;
        private UnityEngine.Events.UnityAction _questBtnHandler;

        private static readonly string[] QualityNames = { "全部", "普通", "稀有", "史诗", "传说", "终极", "神话" };

        void Awake()
        {
            mOldPanel = GetComponent<GeneralSkillPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GeneralSkillPanelNew] 找不到 GeneralSkillPanel 组件");
                return;
            }

            mOldPanel.enabled = false;

            // 去共鸣/去佩戴：共鸣显示区、装备槽区、共鸣入口按钮仅运行时隐藏（prefab 节点保留）
            mOldPanel.ArcResonance.gameObject.SetActive(false);
            mOldPanel.ChooseGeneralCard.gameObject.SetActive(false);
            mOldPanel.GeneralContent.gameObject.SetActive(false);
            mOldPanel.ResonanceBtn.gameObject.SetActive(false);
        }

        protected override void OnBindViewModel(GeneralSkillPanelViewModel vm)
        {
            _cardCountHandler = (oldValue, newValue) =>
            {
                if (mOldPanel.GeneralSkillFilter != null)
                {
                    var cardNumText = mOldPanel.GeneralSkillFilter.Find("CardNum/Text");
                    if (cardNumText != null)
                    {
                        cardNumText.GetComponent<TMP_Text>().text = newValue;
                    }
                }
            };
            vm.CardCountText.OnValueChanged += _cardCountHandler;

            vm.InitGeneralSkillList();

            BindQualityDropdown(vm);

            _openSkillUpgradeHandler = (skillId) =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.GeneralSkill, 2);
                var panel = QFramework.UIKit.OpenPanel<GeneralSkillUpGradePanel>(QFramework.UILevel.PopUI);
                panel.SetData(skillId);
            };
            vm.OnOpenSkillUpgradePanel += _openSkillUpgradeHandler;

            _onOpenResolvePanelHandler = () =>
            {
                OpenBottomBar(Game.Soldier.BottomBarViewModel.TabType.Resolve);
            };
            vm.OnOpenResolvePanel += _onOpenResolvePanelHandler;

            _onOpenConbinePanelHandler = () =>
            {
                OpenBottomBar(Game.Soldier.BottomBarViewModel.TabType.Conbine);
            };
            vm.OnOpenConbinePanel += _onOpenConbinePanelHandler;

            BindDecomposeButton(vm);
            BindCombineButton(vm);
            BindQuestBtn();
        }

        protected override void OnUnbindViewModel(GeneralSkillPanelViewModel vm)
        {
            if (_cardCountHandler != null)
            {
                vm.CardCountText.OnValueChanged -= _cardCountHandler;
                _cardCountHandler = null;
            }

            if (_openSkillUpgradeHandler != null)
            {
                vm.OnOpenSkillUpgradePanel -= _openSkillUpgradeHandler;
                _openSkillUpgradeHandler = null;
            }

            if (_onOpenResolvePanelHandler != null)
            {
                vm.OnOpenResolvePanel -= _onOpenResolvePanelHandler;
                _onOpenResolvePanelHandler = null;
            }

            if (_onOpenConbinePanelHandler != null)
            {
                vm.OnOpenConbinePanel -= _onOpenConbinePanelHandler;
                _onOpenConbinePanelHandler = null;
            }

            UnbindDecomposeButton();
            UnbindCombineButton();
            UnbindQuestBtn();

            UnbindQualityDropdown();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            RefreshSkillScroll(VM);
            LogTool.Log("[GeneralSkillPanelNew] OnOpen");
        }

        protected override void OnShow()
        {
            if (VM == null)
            {
                return;
            }

            // OnShow 时从 Global 重新同步一遍最新卡牌数据（例如分解/获得后返回该界面）
            VM.InitGeneralSkillList();
            RefreshSkillScroll(VM);
            LogTool.Log("[GeneralSkillPanelNew] OnShow");
        }

        protected override void OnHide()
        {
            LogTool.Log("[GeneralSkillPanelNew] OnHide");
        }

        /// <summary>
        /// 绑定品质筛选 Dropdown
        /// </summary>
        private void BindQualityDropdown(GeneralSkillPanelViewModel vm)
        {
            if (mOldPanel.Dropdown == null) return;

            mOldPanel.Dropdown.ClearOptions();
            var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (var name in QualityNames)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }
            mOldPanel.Dropdown.AddOptions(options);
            mOldPanel.Dropdown.value = 0;
            mOldPanel.Dropdown.RefreshShownValue();

            _dropdownHandler = (index) =>
            {
                LogTool.Log($"[GeneralSkillPanelNew] Dropdown 选择: index={index}, qualityType={(QualityType)index}");
                if (index == 0)
                {
                    vm.ClearFiltersCommand.Execute();
                }
                else
                {
                    var qualityType = (QualityType)index;
                    vm.ToggleQualityFilterCommand.Execute(qualityType);
                }
                RefreshSkillScroll(vm);
                LogTool.Log($"[GeneralSkillPanelNew] 筛选后列表数量: {vm.GetFilteredSkillList().Count}");
            };
            mOldPanel.Dropdown.onValueChanged.AddListener(_dropdownHandler);
        }

        /// <summary>
        /// 绑定分解按钮
        /// </summary>
        private void BindDecomposeButton(GeneralSkillPanelViewModel vm)
        {
            if (mOldPanel.DecomposeButton == null) return;

            _decomposeButtonHandler = () =>
            {
                vm.NotifyOpenResolvePanel();
            };
            mOldPanel.DecomposeButton.onClick.AddListener(_decomposeButtonHandler);
        }

        /// <summary>
        /// 绑定合成按钮
        /// </summary>
        private void BindCombineButton(GeneralSkillPanelViewModel vm)
        {
            if (mOldPanel.CombineBtn == null) return;

            _combineButtonHandler = () =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.ArcCardDecompose, 2);
                vm.NotifyOpenConbinePanel();
            };
            mOldPanel.CombineBtn.onClick.AddListener(_combineButtonHandler);
            mOldPanel.CombineBtn.BindRedDot(RedNodeType.ArcCardCombine);

            if (UnlockGuideManager.Instance != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardDecompose, 2, mOldPanel.CombineBtn.GetComponent<RectTransform>());
            }
        }

        /// <summary>
        /// 解绑分解按钮
        /// </summary>
        private void UnbindDecomposeButton()
        {
            if (mOldPanel.DecomposeButton == null) return;

            if (_decomposeButtonHandler != null)
            {
                mOldPanel.DecomposeButton.onClick.RemoveListener(_decomposeButtonHandler);
                _decomposeButtonHandler = null;
            }
        }

        /// <summary>
        /// 解绑合成按钮
        /// </summary>
        private void UnbindCombineButton()
        {
            if (mOldPanel.CombineBtn == null) return;

            if (_combineButtonHandler != null)
            {
                mOldPanel.CombineBtn.onClick.RemoveListener(_combineButtonHandler);
                _combineButtonHandler = null;
            }
        }

        /// <summary>
        /// 绑定 QuestBtn 打开 TipPanel
        /// </summary>
        private void BindQuestBtn()
        {
            if (mOldPanel.QuestBtn == null) return;

            _questBtnHandler = () =>
            {
                var tipVm = new TipPanelViewModel();
                tipVm.OnCreate();
                tipVm.Title = "奥术卡配置限制";
                tipVm.Detail = "普通奥术卡：6张，稀有奥术卡：4张 史诗奥术卡：2张 传说奥术卡：1张";
                UIFramework.UIManager.OpenPanelAsync<TipPanelNew, TipPanelViewModel>(tipVm, UIFramework.UILevel.PopUI).Forget();
            };
            mOldPanel.QuestBtn.onClick.AddListener(_questBtnHandler);
        }

        /// <summary>
        /// 解绑 QuestBtn
        /// </summary>
        private void UnbindQuestBtn()
        {
            if (mOldPanel.QuestBtn == null) return;

            if (_questBtnHandler != null)
            {
                mOldPanel.QuestBtn.onClick.RemoveListener(_questBtnHandler);
                _questBtnHandler = null;
            }
        }

        /// <summary>
        /// 解绑品质筛选 Dropdown
        /// </summary>
        private void UnbindQualityDropdown()
        {
            if (mOldPanel.Dropdown == null) return;

            if (_dropdownHandler != null)
            {
                mOldPanel.Dropdown.onValueChanged.RemoveListener(_dropdownHandler);
                _dropdownHandler = null;
            }
        }

        /// <summary>
        /// 刷新技能滚动列表
        /// </summary>
        private void RefreshSkillScroll(GeneralSkillPanelViewModel vm)
        {
            if (mOldPanel.GeneralSkillScroll == null) return;

            var skillList = vm.GetFilteredSkillList();

            mOldPanel.GeneralSkillScroll.ScrollToIndex(0, ScrollView.ScrollAlignment.Start, false);
            mOldPanel.GeneralSkillScroll.SetUpdateFunc((dataIndex, scrollItem) => OnUpdateGeneralCardItem(dataIndex, scrollItem, vm, skillList));
            mOldPanel.GeneralSkillScroll.ShowList(skillList.Count);
            // mOldPanel.GeneralSkillScroll.PlayScaleAnimationDOTween();
        }

        private GeneralCardSlotData BuildGeneralCardSlotData(PlayerCard playerSkill, SpecialEffectsConfig skillCfg, int num = -1)
        {
            return new GeneralCardSlotData
            {
                CardId = playerSkill.CardId,
                NameText = playerSkill.Config.Name,
                LevelText = $"{playerSkill.Lv}",
                Desc = playerSkill.Config.Desc,
                State = playerSkill.Config.State,
                IsUnlock = playerSkill.IsHave,
                QualityType = skillCfg.LevelPropertyList[playerSkill.Lv - 1].Quality,
                CheckRed = true,
                Num = num,
                IconName = playerSkill.Config.Icon,
                IconScale = Vector3.one * 1.1f,
            };
        }

        /// <summary>
        /// ScrollView 每项更新回调
        /// </summary>
        private void OnUpdateGeneralCardItem(int dataIndex, ScrollItem scrollItem, GeneralSkillPanelViewModel vm, System.Collections.Generic.List<PlayerCard> skillList)
        {
            if (dataIndex < 0 || dataIndex >= skillList.Count)
            {
                scrollItem.gameObject.SetActive(false);
                return;
            }

            var bagScrollItem = (BagScrollItem)scrollItem;
            var playerSkill = Global.GetCardById(skillList[dataIndex].CardId, skillList[dataIndex]);
            if (playerSkill == null)
            {
                bagScrollItem.gameObject.SetActive(false);
                return;
            }

            var skillCfg = vm.GetSkillConfig(playerSkill.CardId);
            if (skillCfg == null || skillCfg.LevelPropertyList.Count <= 0
                || playerSkill.Lv <= 0 || playerSkill.Lv > skillCfg.LevelPropertyList.Count)
            {
                LogTool.LogWarning($"[GeneralSkillPanelNew] 奥术卡配置不完整: cardId={playerSkill.CardId}, lv={playerSkill.Lv}");
                bagScrollItem.gameObject.SetActive(false);
                return;
            }

            bagScrollItem.gameObject.SetActive(true);

            // 去佩戴后奥术卡 Num 恒 1，已拥有直接展示数量
            int displayCount = playerSkill.IsHave ? playerSkill.Num : -1;
            var cardData = BuildGeneralCardSlotData(playerSkill, skillCfg, displayCount);
            bagScrollItem.GetComponent<GeneralCardSlot>().SetData(cardData);

            int skillId = skillCfg.Id;
            bagScrollItem.OnClick = () =>
            {
                SelectGeneralSkillCard(vm, skillId);
            };

            bagScrollItem.transform.SetLocalRedCount(vm.CheckCardConsumeRedDot(playerSkill));
            if (dataIndex == 0 && UnlockGuideManager.Instance != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.GeneralSkill, 2, bagScrollItem.GetComponent<RectTransform>());
            }
        }

        private void SelectGeneralSkillCard(GeneralSkillPanelViewModel vm, int skillId)
        {
            vm.NotifyOpenSkillUpgradePanel(skillId);
        }

        /// <summary>
        /// 打开底栏容器面板（聚合合成/分解），按指定初始 Tab 显示对应子面板。
        /// </summary>
        private void OpenBottomBar(Game.Soldier.BottomBarViewModel.TabType initialTab)
        {
            var bottomVm = new Game.Soldier.BottomBarViewModel();
            bottomVm.OnCreate();
            bottomVm.InitialTab = initialTab;
            UIFramework.UIManager.OpenPanelAsync<BottomBarNew, Game.Soldier.BottomBarViewModel>(
                bottomVm,
                UIFramework.UILevel.Panel);
        }
    }
}
