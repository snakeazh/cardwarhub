using Game.Soldier;
using Config;
using QFramework;
using UIFramework;
using UnityEngine;
using ScrollView;
using UnityEngine.Events;
using System.Collections.Generic;
using FusionContinent.Model;

namespace FusionContinent
{
    [PrefabPath("GeneralCardResolvePanel")]
    public class GeneralCardResolvePanelNew : UIPanel<GeneralCardResolvePanelViewModel>
    {
        // 作为 BottomBar 的子面板，不参与 UIManager 全局栈管理
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        private GeneralCardResolvePanel mOldPanel;

        private System.Action<int, int> _filterIndexHandler;
        private System.Action<System.Collections.Generic.List<FusionContinent.Model.PlayerCard>,
            System.Collections.Generic.List<FusionContinent.Model.PlayerCard>> _listChangedHandler;
        private System.Action<List<RewardItem>, List<RewardItem>> _convertedItemsHandler;

        private UnityAction<int> _dropdownHandler;
        private UnityAction _resolveBtnHandler;
        private System.Action _resolveSucceededHandler;

        private const string GetItemsGroup = "GeneralCardResolvePanelNew_GetItems";

        void Awake()
        {
            mOldPanel = GetComponent<GeneralCardResolvePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[GeneralCardResolvePanelNew] 找不到 GeneralCardResolvePanel 组件");
            }
        }

        protected override void OnBindViewModel(GeneralCardResolvePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            _filterIndexHandler = (o, n) =>
            {
                if (mOldPanel.CardFilter != null && mOldPanel.CardFilter.value != n)
                {
                    mOldPanel.CardFilter.value = n;
                    mOldPanel.CardFilter.RefreshShownValue();
                }
            };
            vm.FilterIndex.OnValueChanged += _filterIndexHandler;

            _listChangedHandler = (o, n) =>
            {
                RefreshScroll(vm);
            };
            vm.FilteredCards.OnValueChanged += _listChangedHandler;

            _convertedItemsHandler = (o, n) => RefreshGetItems(n);
            vm.ConvertedItems.OnValueChanged += _convertedItemsHandler;

            if (mOldPanel.GeneralCardScroll != null)
            {
                mOldPanel.GeneralCardScroll.SetUpdateFunc((dataIndex, item) => OnUpdateCardItem(vm, dataIndex, item));
            }
            
            if (mOldPanel.GeneralCardScroll != null)
            {
                mOldPanel.GeneralCardScroll.SetUpdateSelectFunc((dataIndex, item) => OnUpdateSelectCardItem(vm, dataIndex, item));
            }

            BindDropdown(vm);
            BindButtons(vm);

            _resolveSucceededHandler = () =>
            {
                RefreshScroll(vm);
                if (mOldPanel != null && mOldPanel.GeneralCardScroll != null)
                {
                    mOldPanel.GeneralCardScroll.RefreshSelect();
                }
                RefreshGetItems(vm.ConvertedItems.Value);
            };
            vm.OnResolveSucceeded += _resolveSucceededHandler;

            // 首次同步一次道具列表
            RefreshGetItems(vm.ConvertedItems.Value);
        }

        protected override void OnUnbindViewModel(GeneralCardResolvePanelViewModel vm)
        {
            if (_filterIndexHandler != null)
            {
                vm.FilterIndex.OnValueChanged -= _filterIndexHandler;
                _filterIndexHandler = null;
            }

            if (_listChangedHandler != null)
            {
                vm.FilteredCards.OnValueChanged -= _listChangedHandler;
                _listChangedHandler = null;
            }

            if (_resolveSucceededHandler != null)
            {
                vm.OnResolveSucceeded -= _resolveSucceededHandler;
                _resolveSucceededHandler = null;
            }

            if (_convertedItemsHandler != null)
            {
                vm.ConvertedItems.OnValueChanged -= _convertedItemsHandler;
                _convertedItemsHandler = null;
            }

            UnbindDropdown();
            UnbindButtons();

            PoolScope.ReleaseGroup(GetItemsGroup);
        }

        protected override void OnOpen(UIFramework.IUIData uiData)
        {
            VM.ResetOnOpen();
            RefreshScroll(VM);
            if (mOldPanel != null && mOldPanel.GeneralCardScroll != null)
            {
                mOldPanel.GeneralCardScroll.RefreshSelect();
            }
            RefreshGetItems(VM.ConvertedItems.Value);
        }

        private void RefreshScroll(GeneralCardResolvePanelViewModel vm)
        {
            if (mOldPanel == null || mOldPanel.GeneralCardScroll == null) return;

            var list = vm.FilteredCards.Value;
            var count = list == null ? 0 : list.Count;

            mOldPanel.GeneralCardScroll.ScrollToIndex(0, ScrollAlignment.Start, false);
            mOldPanel.GeneralCardScroll.ShowList(count);
        }

        private void RefreshGetItems(List<RewardItem> items)
        {
            if (mOldPanel == null || mOldPanel.GetItems == null) return;

            PoolScope.ReleaseGroup(GetItemsGroup);

            if (items == null || items.Count == 0) return;

            var parent = mOldPanel.GetItems;
            var rewardPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity");
            if (rewardPool == null)
            {
                LogTool.LogError("[GeneralCardResolvePanelNew] RewardEntity 对象池获取失败");
                return;
            }

            foreach (var item in items)
            {
                if (item == null) continue;

                var obj = rewardPool.Get(parent);
                if (obj == null) continue;

                PoolScope.Track(rewardPool, obj, GetItemsGroup);

                var rewardEntity = obj.GetComponent<RewardEntity>();
                if (rewardEntity != null)
                {
                    rewardEntity.SetData(item);
                }

                obj.transform.SetParent(parent);
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = Vector3.zero;
            }
        }

        private void OnUpdateCardItem(GeneralCardResolvePanelViewModel vm, int dataIndex, ScrollItem scrollItem)
        {
            var playerCard = vm.GetCardAt(dataIndex);
            if (playerCard == null)
            {
                scrollItem.gameObject.SetActive(false);
                return;
            }

            var config = playerCard.Config;
            var skillCfg = DataConfigManager.Tables.TbSpecialEffectsConfig.GetOrDefault(playerCard.CardId);
            var quality = QualityType.Default;
            if (skillCfg != null && skillCfg.LevelPropertyList != null && skillCfg.LevelPropertyList.Count > 0)
            {
                quality = skillCfg.LevelPropertyList[0].Quality;
            }

            var isSelected = vm.IsSelected(playerCard.CardId);

            var slotData = new GeneralCardSlotData
            {
                CardId = playerCard.CardId,
                NameText = config.Name,
                QualityType = quality,
                IconName = config.Icon,
                IconScale = Vector3.one * 1.1f,
                IsSelected = isSelected,
                SelectedCount = vm.GetSelectedCount(playerCard.CardId),
                // OnSelectChanged = vm.SetSelected
                Num = playerCard.Num,
            };
            BagScrollItem bagScrollItem = scrollItem as BagScrollItem;
            bagScrollItem.OnSpawn();
            var slot = scrollItem.GetComponent<GeneralCardSlot>();
            if (slot != null)
            {
                slot.SetData(slotData);
            }
            bagScrollItem.OnClick = () =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.ArcCardDecompose, 4);
                GlobalFunc.OpenChooseNum(playerCard.Num, (selectCount) =>
                {
                    vm.SetSelected(playerCard.CardId, selectCount);
                    if (mOldPanel != null && mOldPanel.GeneralCardScroll != null)
                    {
                        mOldPanel.GeneralCardScroll.RefreshSelect();
                    }
                });

            };

            if (dataIndex == 0 && UnlockGuideManager.Instance != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardDecompose, 4, scrollItem.GetComponent<RectTransform>());
            }
     
        }
        
        private void OnUpdateSelectCardItem(GeneralCardResolvePanelViewModel vm, int dataIndex, ScrollItem scrollItem)
        {
            var playerCard = vm.GetCardAt(dataIndex);
            if (playerCard == null)
            {
                scrollItem.gameObject.SetActive(false);
                return;
            }
            var isSelected = vm.IsSelected(playerCard.CardId);

            var slot = scrollItem.GetComponent<GeneralCardSlot>();
            if (slot != null)
            {
                slot.SetSelected(isSelected,vm.GetSelectedCount(playerCard.CardId));
            }
        }

        private void BindDropdown(GeneralCardResolvePanelViewModel vm)
        {
            if (mOldPanel.CardFilter == null) return;

            _dropdownHandler = (index) => vm.ChangeFilterCommand.Execute(index);
            mOldPanel.CardFilter.onValueChanged.AddListener(_dropdownHandler);

            // 同步一次
            mOldPanel.CardFilter.value = vm.FilterIndex.Value;
            mOldPanel.CardFilter.RefreshShownValue();
        }

        private void UnbindDropdown()
        {
            if (mOldPanel == null || mOldPanel.CardFilter == null) return;
            if (_dropdownHandler != null)
            {
                mOldPanel.CardFilter.onValueChanged.RemoveListener(_dropdownHandler);
                _dropdownHandler = null;
            }
        }

        private void BindButtons(GeneralCardResolvePanelViewModel vm)
        {
            if (mOldPanel.ResolveBtn != null)
            {
                _resolveBtnHandler = () =>
                {
                    UnlockGuideManager.Send(ModuleFunctionType.ArcCardDecompose, 6);
                    vm.ResolveCommand.Execute();
                };
                mOldPanel.ResolveBtn.onClick.AddListener(_resolveBtnHandler);

                if (UnlockGuideManager.Instance != null)
                {
                    UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardDecompose, 6, mOldPanel.ResolveBtn.GetComponent<RectTransform>());
                }
            }
        }

        public override void Show()
        {
            base.Show();
        }

        private void UnbindButtons()
        {
            if (mOldPanel == null) return;

            if (_resolveBtnHandler != null && mOldPanel.ResolveBtn != null)
            {
                mOldPanel.ResolveBtn.onClick.RemoveListener(_resolveBtnHandler);
                _resolveBtnHandler = null;
            }
        }
    }
}

