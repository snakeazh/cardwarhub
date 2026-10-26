using Game.Soldier;
using Game.Soldier.RedDot;
using Game.RedDot;
using Game.RedDot.Rules;
using Game.RedDot.View;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using System.Collections.Generic;
using System.Linq;
using FusionContinent.Model;
using ScrollView;
using IUIData = UIFramework.IUIData;

namespace FusionContinent
{
    [UIFramework.PrefabPath("SoldierMainPanel")]
    public class SoldierMainPanelNew : UIFramework.UIPanel<SoldierMainPanelViewModel>
    {

        private SoldierMainPanel mOldPanel;

        // 士兵列表红点 scope：未拥有→Activate；已拥有→OwnedHint（升级/养成/技能/觉醒）。
        // 与 SoldierPanelNew 共享同名 scope，引用计数管理生命周期。
        private readonly SoldierPanelRedDotScope _redDotScope = new SoldierPanelRedDotScope();

        private static readonly string[] CareerFilterNames = { "全部", "射手", "战士", "法师", "刺客", "辅助" };

        private System.Action<List<PlayerCard>, List<PlayerCard>> _filteredSoldierListHandler;
        private System.Action<string, string> _cardNumTextHandler;
        private System.Action<string, string> _damageLevelTextHandler;
        private UnityEngine.Events.UnityAction<int> _dropdownHandler;
        private UnityEngine.Events.UnityAction _detailBtnHandler;

        void Awake()
        {
            mOldPanel = GetComponent<SoldierMainPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[SoldierMainPanelNew] 找不到 SoldierMainPanel 组件");
            }
        }

        protected override void OnBindViewModel(SoldierMainPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 激活士兵列表红点 scope（Card/{cardId}/Upgrade 驱动每个 SoldierSlot）
            _redDotScope.OpenShared(SoldierRedDotHelper.GetSoldierCardIds());

            _filteredSoldierListHandler = (o, n) => UpdateSoldierCardScroll(vm, n);
            vm.FilteredSoldierList.OnValueChanged += _filteredSoldierListHandler;

            _cardNumTextHandler = (o, n) =>
            {
                if (mOldPanel.CardNum != null)
                {
                    var cardNumText = mOldPanel.CardNum.transform.Find("Text");
                    if (cardNumText != null)
                        cardNumText.GetComponent<TMP_Text>().text = n;
                }
            };
            vm.CardNumText.OnValueChanged += _cardNumTextHandler;

            _damageLevelTextHandler = (_, n) =>
            {
                if (mOldPanel?.DamageLevelText != null)
                    mOldPanel.DamageLevelText.text = n;
            };
            vm.DamageLevelText.OnValueChanged += _damageLevelTextHandler;
            // 订阅后同步初始值（避免 OnCreate 中已设置的值丢失）
            if (mOldPanel?.DamageLevelText != null)
                mOldPanel.DamageLevelText.text = vm.DamageLevelText.Value;

            BindDropdown(vm);
            BindDetailBtn();
        }

        protected override void OnUnbindViewModel(SoldierMainPanelViewModel vm)
        {
            if (_filteredSoldierListHandler != null)
            {
                vm.FilteredSoldierList.OnValueChanged -= _filteredSoldierListHandler;
                _filteredSoldierListHandler = null;
            }

            if (_cardNumTextHandler != null)
            {
                vm.CardNumText.OnValueChanged -= _cardNumTextHandler;
                _cardNumTextHandler = null;
            }

            if (_damageLevelTextHandler != null)
            {
                vm.DamageLevelText.OnValueChanged -= _damageLevelTextHandler;
                _damageLevelTextHandler = null;
            }

            if (_dropdownHandler != null && mOldPanel?.SoldierCardDropdown != null)
            {
                mOldPanel.SoldierCardDropdown.onValueChanged.RemoveListener(_dropdownHandler);
                _dropdownHandler = null;
            }

            if (_detailBtnHandler != null && mOldPanel?.DetailBtn != null)
            {
                mOldPanel.DetailBtn.onClick.RemoveListener(_detailBtnHandler);
                _detailBtnHandler = null;
            }

            // 关闭士兵列表红点 scope（引用计数归零才真正销毁）
            _redDotScope.CloseShared();
        }

        private void BindDetailBtn()
        {
            if (mOldPanel?.DetailBtn == null) return;

            _detailBtnHandler = () =>
            {
                GlobalFunc.OpenCommonDetailPopPanel(
                    mOldPanel.DetailBtn.transform,
                    "技能伤害增幅",
                    "解锁任意英灵或升级英灵，均可以获得技能伤害增幅");
            };
            mOldPanel.DetailBtn.onClick.AddListener(_detailBtnHandler);
        }

        private void BindDropdown(SoldierMainPanelViewModel vm)
        {
            if (mOldPanel?.SoldierCardDropdown == null) return;

            var dropdown = mOldPanel.SoldierCardDropdown;

            dropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (var name in CareerFilterNames)
            {
                options.Add(new TMP_Dropdown.OptionData(name));
            }
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();

            _dropdownHandler = (index) =>
            {
                vm.OnFilterClickedCommand.Execute(index);
            };
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(_dropdownHandler);
        }

        private void UpdateSoldierCardScroll(SoldierMainPanelViewModel vm, List<PlayerCard> filteredList)
        {
            if (mOldPanel?.SoldierCardScroll == null) return;

            mOldPanel.SoldierCardScroll.ScrollToIndex(0, ScrollAlignment.Start, false);
            mOldPanel.SoldierCardScroll.SetUpdateFunc((dataIndex, scrollItem) =>
            {
                if (dataIndex < 0 || dataIndex >= filteredList.Count)
                {
                    scrollItem.gameObject.SetActive(false);
                    return;
                }

                var bagScrollItem = (BagScrollItem)scrollItem;
                var playerSoldier = filteredList[dataIndex];
                bagScrollItem.gameObject.SetActive(true);
                bagScrollItem.GetComponent<SoldierSlot>().SetData(playerSoldier.CardId);

                // 该士兵可升级/可解锁时，在 SoldierSlot/NameBg/RedPoint 显示局部红点
                BindSoldierSlotRedDot(bagScrollItem.transform, playerSoldier.CardId);

                if (dataIndex == 0 && UnlockGuideManager.Instance != null)
                {
                    var firstSoldierRt = bagScrollItem.GetComponent<RectTransform>();
                    UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.SoldierAtlas, 2, firstSoldierRt);
                    UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.SoldierSkillAtlas, 2, firstSoldierRt);
                }
            });
            mOldPanel.SoldierCardScroll.ShowList(filteredList.Count);
            mOldPanel.SoldierCardScroll.PlayScaleAnimationDOTween();
        }

        /// <summary>
        /// 给 SoldierSlot 列表项绑定红点：已拥有绑可升级，未拥有绑可解锁。
        /// 显示节点：SoldierSlot/TweenTarget/NameBg/RedPoint（默认隐藏，count&gt;0 时由 LocalRedDotView 打开）。
        /// </summary>
        void BindSoldierSlotRedDot(Transform slotRoot, int cardId)
        {
            if (slotRoot == null || !_redDotScope.IsOpen)
            {
                return;
            }

            Transform nameBg = slotRoot.Find("TweenTarget/NameBg");
            if (nameBg == null)
            {
                LogTool.LogError("[SoldierMainPanelNew] 找不到 SoldierSlot/TweenTarget/NameBg 节点");
                return;
            }

            Transform redPoint = nameBg.Find("RedPoint");
            if (redPoint == null)
            {
                LogTool.LogError("[SoldierMainPanelNew] 找不到 SoldierSlot/TweenTarget/NameBg/RedPoint 节点");
                return;
            }

            // 默认不显示；有红点数据时再由 RedDotDisplay 打开
            redPoint.gameObject.SetActive(false);

            // 清掉旧绑定：slotRoot 上历史 LocalRedDotView 只卸组件；其它误挂的动态节点可销毁
            LocalRedDotView rootView = slotRoot.GetComponent<LocalRedDotView>();
            if (rootView != null)
            {
                Destroy(rootView);
            }

            LocalRedDotView[] childViews = slotRoot.GetComponentsInChildren<LocalRedDotView>(true);
            for (int i = 0; i < childViews.Length; i++)
            {
                LocalRedDotView view = childViews[i];
                if (view.transform == nameBg)
                {
                    continue;
                }

                if (view.transform != slotRoot)
                {
                    Destroy(view.gameObject);
                }
            }

            PlayerCard card = Global.GetCardById(cardId);
            bool owned = card != null && card.IsHave;
            string path = owned
                ? SoldierPanelRedDotScope.Paths.CardOwnedHint(cardId)
                : SoldierPanelRedDotScope.Paths.CardActivate(cardId);

            nameBg.BindLocalRedDot(_redDotScope, path);
            LocalRedDotView nameBgView = nameBg.GetComponent<LocalRedDotView>();
            if (nameBgView != null)
            {
                nameBgView.RefreshWatch();
            }
        }

        public void RefreshSoldierList()
        {
            if (VM != null)
            {
                VM.RefreshSoldierList();
            }
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM != null && mOldPanel != null)
            {
                UpdateSoldierCardScroll(VM, VM.FilteredSoldierList.Value);
            }
        }
        protected override void OnShow()
        {
            RefreshSoldierList();
        }
        protected override void OnHide() { }
    }
}
