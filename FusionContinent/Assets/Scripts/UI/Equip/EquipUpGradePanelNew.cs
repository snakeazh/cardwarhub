using Game.Equip;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using QFramework;
using UIFramework;
using Config;
using Game.CombatPower;
using FusionContinent;
using FusionContinent.Manager;
using FusionContinent.Sounds;
using FusionContinent.UI.Components;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using Assets.Scripts.Localization;
using FusionContinent.Model;
using Game.Equip.RedDot;
using Manager;
using Game.RedDot;
using Game.RedDot.View;

namespace FusionContinent
{
    /// <summary>
    /// 装备升级/洗炼界面 - 新框架版本 (MVVM)
    /// 复用原 EquipUpGradePanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("EquipUpGradePanel")]
    public class EquipUpGradePanelNew : UIFramework.UIPanel<EquipUpGradePanelViewModel>
    {
        // 引用原 Panel，复用其 UI 组件
        private EquipUpGradePanel mOldPanel;

        // 装备槽位缓存
        private readonly Dictionary<int, GameObject> _wearEquipSlots = new Dictionary<int, GameObject>();

        // 引导目标
        private GameObject _guideAttr;

        // Refine/LevelUp 互斥页签（来自 Foot/ToggleGroup）
        private UISelectableToggle _refineToggle;
        private UISelectableToggle _levelUpToggle;

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<EquipUpGradePanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[EquipUpGradePanelNew] 找不到 EquipUpGradePanel 组件！");
                return;
            }
            mOldPanel.enabled = false;

            LogTool.Log("[EquipUpGradePanelNew] 已关联 EquipUpGradePanel");
        }

        protected override void OnBindViewModel(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 绑定属性
            BindProperties(vm);
            
            // 绑定 ViewModel 事件
            BindEvents(vm);

            // 初始化按钮
            InitButtons(vm);

            // OnValueChanged 不会在订阅时重放当前值
            vm.ActivateRedDot();
            SyncBoundPropertyValues(vm);
        }

        protected override void OnUnbindViewModel(EquipUpGradePanelViewModel vm)
        {
            vm?.DeactivateRedDot();

            // 取消属性监听
            UnbindProperties(vm);
            
            // 取消事件订阅
            UnbindEvents(vm);
            
            // 移除按钮监听
            RemoveButtonListeners();
        }

        #region 属性绑定

        private void BindProperties(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            vm.CurrentPage.OnValueChanged += OnPageChanged;
            vm.WearEquipList.OnValueChanged += OnWearEquipListChanged;
            vm.CurrentEquip.OnValueChanged += OnCurrentEquipPropertyChanged;
            vm.EquipName.OnValueChanged += OnEquipNameChanged;
            vm.CurrentPhase.OnValueChanged += OnCurrentPhaseChanged;
            vm.NextPhase.OnValueChanged += OnNextPhaseChanged;
            vm.IsMaxPhase.OnValueChanged += OnIsMaxPhaseChanged;
            vm.CanUpPhase.OnValueChanged += OnCanUpPhaseChanged;
            vm.UpPhaseButtonText.OnValueChanged += OnUpPhaseButtonTextChanged;
            vm.QualityTipText.OnValueChanged += OnQualityTipTextChanged;
            vm.SelectedEquipId.OnValueChanged += OnSelectedEquipIdChanged;
            vm.OperationError.OnValueChanged += OnOperationErrorChanged;
            vm.CanRefreshEntry.OnValueChanged += OnCanRefreshEntryChanged;
            vm.CanUseRefreshEntry.OnValueChanged += OnCanUseRefreshEntryChanged;
            vm.SelectedLockedEffectIds.OnValueChanged += OnSelectedLockedEffectIdsChanged;
        }

        private void UnbindProperties(EquipUpGradePanelViewModel vm)
        {
            if (vm == null) return;

            vm.CurrentPage.OnValueChanged -= OnPageChanged;
            vm.WearEquipList.OnValueChanged -= OnWearEquipListChanged;
            vm.CurrentEquip.OnValueChanged -= OnCurrentEquipPropertyChanged;
            vm.EquipName.OnValueChanged -= OnEquipNameChanged;
            vm.CurrentPhase.OnValueChanged -= OnCurrentPhaseChanged;
            vm.NextPhase.OnValueChanged -= OnNextPhaseChanged;
            vm.IsMaxPhase.OnValueChanged -= OnIsMaxPhaseChanged;
            vm.CanUpPhase.OnValueChanged -= OnCanUpPhaseChanged;
            vm.UpPhaseButtonText.OnValueChanged -= OnUpPhaseButtonTextChanged;
            vm.QualityTipText.OnValueChanged -= OnQualityTipTextChanged;
            vm.SelectedEquipId.OnValueChanged -= OnSelectedEquipIdChanged;
            vm.OperationError.OnValueChanged -= OnOperationErrorChanged;
            vm.CanRefreshEntry.OnValueChanged -= OnCanRefreshEntryChanged;
            vm.CanUseRefreshEntry.OnValueChanged -= OnCanUseRefreshEntryChanged;
            vm.SelectedLockedEffectIds.OnValueChanged -= OnSelectedLockedEffectIdsChanged;
        }

        private void OnWearEquipListChanged(List<PlayerEquip> oldVal, List<PlayerEquip> newVal)
        {
            InitWearEquipSlot(VM);
            VM?.RefreshLocalRedDots();
        }

        private void OnCurrentEquipPropertyChanged(PlayerEquip oldVal, PlayerEquip newVal)
        {
            OnCurrentEquipChanged(VM);
        }

        private void OnEquipNameChanged(string oldVal, string newVal)
        {
            if (mOldPanel.NameText != null)
                mOldPanel.NameText.text = newVal;
        }

        private void OnCurrentPhaseChanged(int oldVal, int newVal)
        {
            OnForgeLevelChanged(VM);
        }

        private void OnNextPhaseChanged(int oldVal, int newVal)
        {
            OnForgeLevelChanged(VM);
        }

        private void OnIsMaxPhaseChanged(bool oldVal, bool newVal)
        {
            UpdateMaxPhaseDisplay(VM);
            UpdateExtraAttributes(VM);
        }

        private void OnCanUpPhaseChanged(bool oldVal, bool newVal)
        {
            if (mOldPanel.UpGradeBtn != null)
                mOldPanel.UpGradeBtn.interactable = newVal;
        }

        private void OnUpPhaseButtonTextChanged(string oldVal, string newVal)
        {
            if (mOldPanel.UpGradeBtn == null) return;

            var text = mOldPanel.UpGradeBtn.transform.Find("Text")?.GetComponent<TMP_Text>();
            if (text != null)
                text.text = newVal;
        }

        private void OnQualityTipTextChanged(string oldVal, string newVal)
        {
            if (mOldPanel.UpGrade == null) return;

            var tipText = mOldPanel.UpGrade.Find("TipText")?.GetComponent<TMP_Text>();
            if (tipText != null)
                tipText.text = newVal;
        }

        private void OnSelectedEquipIdChanged(int oldVal, int newVal)
        {
            RefreshEquipSlotChecks(newVal);
        }

        private void OnOperationErrorChanged(string oldVal, string newVal)
        {
            if (string.IsNullOrEmpty(newVal)) return;
            Toast.Error(newVal);
        }

        private void OnCanRefreshEntryChanged(bool oldVal, bool newVal)
        {
            mOldPanel.RefineBtn.interactable = newVal;
        }

        private void OnCanUseRefreshEntryChanged(bool oldVal, bool newVal)
        {
            mOldPanel.ReplaceBtn.interactable = newVal;
        }

        private void OnSelectedLockedEffectIdsChanged(List<int> oldVal, List<int> newVal)
        {
            if (VM == null) return;

            UpdateRefineConsumes(VM);
            RefreshBeforeRefineEffectLocks(VM);
        }

        #endregion

        #region 事件绑定

        private void BindEvents(EquipUpGradePanelViewModel vm)
        {
            vm.OnCloseRequested += OnCloseRequested;
            vm.OnShowSelectSoldier += OnShowSelectSoldier;
            vm.OnDisableGameMainFootBtn += OnDisableGameMainFootBtn;
            vm.OnEnableGameMainFootBtn += OnEnableGameMainFootBtn;
            vm.OnUpPhaseStarted += OnUpPhaseStarted;
            vm.OnUpPhaseEnded += OnUpPhaseEnded;
            vm.OnUpPhaseSuccess += OnUpPhaseSuccess;
            vm.OnRefreshEntryStarted += OnRefreshEntryStarted;
            vm.OnRefreshEntryEnded += OnRefreshEntryEnded;
            vm.OnRefreshEntrySuccess += OnRefreshEntrySuccess;
            vm.OnUseRefreshEntryStarted += OnUseRefreshEntryStarted;
            vm.OnUseRefreshEntryEnded += OnUseRefreshEntryEnded;
            vm.OnUseRefreshEntrySuccess += OnUseRefreshEntrySuccess;
            vm.OnEntryPageOpened += OnEntryPageOpened;
        }

        private void UnbindEvents(EquipUpGradePanelViewModel vm)
        {
            if (vm == null) return;
            
            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnShowSelectSoldier -= OnShowSelectSoldier;
            vm.OnDisableGameMainFootBtn -= OnDisableGameMainFootBtn;
            vm.OnEnableGameMainFootBtn -= OnEnableGameMainFootBtn;
            vm.OnUpPhaseStarted -= OnUpPhaseStarted;
            vm.OnUpPhaseEnded -= OnUpPhaseEnded;
            vm.OnUpPhaseSuccess -= OnUpPhaseSuccess;
            vm.OnRefreshEntryStarted -= OnRefreshEntryStarted;
            vm.OnRefreshEntryEnded -= OnRefreshEntryEnded;
            vm.OnRefreshEntrySuccess -= OnRefreshEntrySuccess;
            vm.OnUseRefreshEntryStarted -= OnUseRefreshEntryStarted;
            vm.OnUseRefreshEntryEnded -= OnUseRefreshEntryEnded;
            vm.OnUseRefreshEntrySuccess -= OnUseRefreshEntrySuccess;
            vm.OnEntryPageOpened -= OnEntryPageOpened;
        }

        #endregion

        #region 按钮初始化

        private void InitButtons(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.GoBackBtn != null)
                mOldPanel.GoBackBtn.onClick.AddListener(OnGoBackButtonClicked);
            
            if (mOldPanel.UpGradeBtn != null)
                mOldPanel.UpGradeBtn.onClick.AddListener(OnUpGradeButtonClicked);
            mOldPanel.RefineBtn.onClick.AddListener(OnRefineButtonClicked);
            mOldPanel.ReplaceBtn.onClick.AddListener(OnReplaceButtonClicked);

            InitToggleTabs();
        }

        private void OnGoBackButtonClicked()
        {
            if (VM != null)
                VM.OnCloseCommand.Execute();
        }

        private void RemoveButtonListeners()
        {
            if (mOldPanel == null) return;

            if (mOldPanel.GoBackBtn != null)
                mOldPanel.GoBackBtn.onClick.RemoveListener(OnGoBackButtonClicked);

            if (mOldPanel.UpGradeBtn != null)
                mOldPanel.UpGradeBtn.onClick.RemoveListener(OnUpGradeButtonClicked);
            mOldPanel.RefineBtn.onClick.RemoveListener(OnRefineButtonClicked);
            mOldPanel.ReplaceBtn.onClick.RemoveListener(OnReplaceButtonClicked);

            if (_refineToggle != null)
                _refineToggle.onValueChanged.RemoveListener(OnRefineToggleValueChanged);
            if (_levelUpToggle != null)
                _levelUpToggle.onValueChanged.RemoveListener(OnLevelUpToggleValueChanged);
        }

        /// <summary>
        /// 初始化 Refine / LevelUp 页签。点击仅更新 ViewModel，面板显隐由 CurrentPage 统一驱动。
        /// </summary>
        private void InitToggleTabs()
        {
            if (mOldPanel == null || mOldPanel.ToggleGroup == null) return;

            var toggleGroup = mOldPanel.ToggleGroup.transform;
            _refineToggle = toggleGroup.Find("RefineToggle")?.GetComponent<UISelectableToggle>();
            _levelUpToggle = toggleGroup.Find("LevelUpToggle")?.GetComponent<UISelectableToggle>();

            if (_refineToggle == null || _levelUpToggle == null)
            {
                LogTool.LogError("[EquipUpGradePanelNew] 找不到 RefineToggle / LevelUpToggle！");
                return;
            }

            _refineToggle.onValueChanged.AddListener(OnRefineToggleValueChanged);
            _levelUpToggle.onValueChanged.AddListener(OnLevelUpToggleValueChanged);

            _levelUpToggle.transform.BindRedDot(RedNodeType.SpeakerEquipForgeRedDot);
            _refineToggle.transform.BindRedDot(RedNodeType.SpeakerEquipRefineRedDot);
        }

        private void OnRefineToggleValueChanged(bool isOn)
        {
            if (!isOn || VM == null) return;

            VM.OnEntryPageCommand.Execute();

            if (VM.CurrentPage.Value != EquipUpGradePanelViewModel.EntryPageIndex)
                SyncToggleState(VM.CurrentPage.Value);
        }

        private void OnLevelUpToggleValueChanged(bool isOn)
        {
            if (!isOn || VM == null) return;
            VM.OnForgePageCommand.Execute();
        }

        /// <summary>
        /// 控制洗炼 / 升级面板的显隐。
        /// </summary>
        /// <param name="showRefine">true=显示洗炼(Refine)，false=显示升级(UpGrade)</param>
        private void SetPageVisible(bool showRefine)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.Refine != null)
                mOldPanel.Refine.gameObject.SetActive(showRefine);
            if (mOldPanel.UpGrade != null)
                mOldPanel.UpGrade.gameObject.SetActive(!showRefine);
        }

        private void SyncToggleState(int pageIndex)
        {
            if (_refineToggle == null || _levelUpToggle == null) return;

            _refineToggle.SetIsOnWithoutNotify(pageIndex == EquipUpGradePanelViewModel.EntryPageIndex);
            _levelUpToggle.SetIsOnWithoutNotify(pageIndex == EquipUpGradePanelViewModel.ForgePageIndex);
        }

        private void OnUpGradeButtonClicked()
        {
            UnlockGuideManager.Send(ModuleFunctionType.EquipPhase, 4);
            if (!VM.TryValidateUpPhaseConsumes(out var message))
            {
                Toast.Show(message);
                return;
            }

            VM.OnUpPhaseCommand.Execute();
        }

        private void OnRefineButtonClicked()
        {
            if (VM == null) return;

            UnlockGuideManager.Send(ModuleFunctionType.EquipEntry, 6);
            if (!VM.TryValidateRefreshEntryConsumes(out var message))
            {
                Toast.Show(message);
                return;
            }

            VM.OnRefreshEntryCommand.Execute();
        }

        private void OnReplaceButtonClicked()
        {
            if (VM == null) return;
            VM.OnUseRefreshEntryCommand.Execute();
        }

        #endregion

        #region UI 更新

        /// <summary>
        /// 将 ViewModel 当前属性值同步到 UI（OnValueChanged 订阅时不会重放初始值）。
        /// </summary>
        private void SyncBoundPropertyValues(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            InitWearEquipSlot(vm);
            OnPageChanged(vm.CurrentPage.Value, vm.CurrentPage.Value);
            vm.RefreshLocalRedDots();

            if (mOldPanel.NameText != null)
                mOldPanel.NameText.text = vm.EquipName.Value;

            if (mOldPanel.UpGradeBtn != null)
            {
                mOldPanel.UpGradeBtn.interactable = vm.CanUpPhase.Value;
                var text = mOldPanel.UpGradeBtn.transform.Find("Text")?.GetComponent<TMP_Text>();
                if (text != null)
                    text.text = vm.UpPhaseButtonText.Value;
            }

            if (mOldPanel.UpGrade != null)
            {
                var tipText = mOldPanel.UpGrade.Find("TipText")?.GetComponent<TMP_Text>();
                if (tipText != null)
                    tipText.text = vm.QualityTipText.Value;
            }

            UpdateMaxPhaseDisplay(vm);
            RefreshEquipSlotChecks(vm.SelectedEquipId.Value);
            mOldPanel.RefineBtn.interactable = vm.CanRefreshEntry.Value;
            mOldPanel.ReplaceBtn.interactable = vm.CanUseRefreshEntry.Value;

            if (vm.CurrentEquip.Value != null)
            {
                OnCurrentEquipChanged(vm);
                OnForgeLevelChanged(vm);
            }
        }

        private void OnPageChanged(int oldPage, int newPage)
        {
            if (mOldPanel == null) return;

            SetPageVisible(newPage == EquipUpGradePanelViewModel.EntryPageIndex);
            SyncToggleState(newPage);

            if (newPage == EquipUpGradePanelViewModel.EntryPageIndex && VM != null)
            {
                UpdateEntryList(VM);
                UpdateEffectList(VM);
                UpdateConsumes(VM);
            }
        }

        private void OnForgeLevelChanged(EquipUpGradePanelViewModel vm)
        {
            UpdateLevelDisplay(vm);
            UpdateConsumes(vm);
            UpdateAttributes(vm);
            UpdateExtraAttributes(vm);
        }

        private void UpdateLevelDisplay(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel?.Level == null) return;

            var levelText = mOldPanel.Level.Find("LevelText")?.GetComponent<TMP_Text>();
            var nextLevelText = mOldPanel.Level.Find("NextLevelText")?.GetComponent<TMP_Text>();

            if (levelText != null)
                levelText.text = $"Lv{vm.CurrentPhase.Value}";
            if (nextLevelText != null)
                nextLevelText.text = $"Lv{vm.NextPhase.Value}";
        }

        private void UpdateMaxPhaseDisplay(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.Level != null)
                mOldPanel.Level.gameObject.SetActive(!vm.IsMaxPhase.Value);
        }

        private void UpdateExtraAttributes(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel?.ExtraAttributes == null) return;

            var activeCfg = vm.GetActiveExtraLevelCfg();
            var nextCfg = vm.IsMaxPhase.Value ? null : vm.GetNextExtraLevelCfg();

            var attribute1 = mOldPanel.ExtraAttributes.Find("Attribute1");
            var attribute2 = mOldPanel.ExtraAttributes.Find("Attribute2");

            BindExtraAttributeRow(attribute1, activeCfg, true);
            BindExtraAttributeRow(attribute2, nextCfg, false);

            mOldPanel.ExtraAttributes.gameObject.SetActive(
                (attribute1 != null && attribute1.gameObject.activeSelf) ||
                (attribute2 != null && attribute2.gameObject.activeSelf));
        }

        private void BindExtraAttributeRow(Transform row, SlotLevelListStruct cfg, bool isUnlocked)
        {
            if (row == null)
            {
                return;
            }

            if (cfg == null)
            {
                row.gameObject.SetActive(false);
                return;
            }

            row.gameObject.SetActive(true);

            var unlockBg = row.Find("UnLockBg");
            if (unlockBg != null)
                unlockBg.gameObject.SetActive(isUnlocked);

            var lockBg = row.Find("LockBg");
            if (lockBg != null)
                lockBg.gameObject.SetActive(!isUnlocked);
            var entryTitle = row.Find("EntryTitle")?.GetComponent<TMP_Text>();
            if (isUnlocked)
            {
                entryTitle.text = "突破";
            }
            else
            {
                if (entryTitle != null)
                    entryTitle.text = $"Lv.{cfg.Level}";
            }

            var entryDesc = row.Find("EntryDesc")?.GetComponent<TMP_Text>();
            if (entryDesc != null)
                entryDesc.text = cfg.ExtraDesc;
        }

        private void OnCurrentEquipChanged(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            var equip = vm.CurrentEquip.Value;
            if (equip == null) return;

            var equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null) return;

            // 更新装备名称
            if (mOldPanel.NameText != null)
            {
                mOldPanel.NameText.text = equipCfg.Name;
                // 根据装备品质修改字体颜色
                mOldPanel.NameText.color = GlobalFunc.GetQualityStyle(equip.Quality).MainColor;
            }

            // 更新装备图标 (EquipIcon)
            if (mOldPanel.EquipIcon != null)
            {
                mOldPanel.EquipIcon.sprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon);
            }

            // 更新装备图标 (Equip RewardEntity)
            if (mOldPanel.Equip != null)
            {
                var rewardEntity = mOldPanel.Equip.GetComponent<RewardEntity>();
                if (rewardEntity != null)
                {
                    rewardEntity.SetData(new RewardEntityData()
                    {
                        EquipLevel = equipCfg.EquipLevel,
                        Quality = equip.Quality,
                        IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
                        StyleType = RewardStyleType.Equip,
                        IsEffect = false,
                        OnClick = () => { }
                    });
                }
            }

            // 更新属性显示
            UpdateAttributes(vm);

            // 更新消耗品显示
            UpdateConsumes(vm);

            // 更新词条显示
            UpdateEntryList(vm);
        }

        private void UpdateAttributes(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel?.Attribute == null) return;

            var currentAttrs = vm.GetCurrentSlotAttributes();
            var nextAttrs = vm.GetNextSlotAttributes();
            var attrConfig = DataConfigManager.Tables.TbAttributeConfig;

            var attrParent = mOldPanel.Attribute.transform;
            for (int i = 0; i < attrParent.childCount; i++)
            {
                var attrObj = attrParent.GetChild(i).gameObject;
                var hasCurrent = currentAttrs.Count > i;
                var hasNext = nextAttrs.Count > i;
                attrObj.SetActive(hasCurrent || hasNext);
                if (!attrObj.activeSelf) continue;

                if (hasCurrent)
                {
                    var currentCfg = currentAttrs[i];
                    var attrText = attrObj.transform.Find("Attr/Text")?.GetComponent<TMP_Text>();
                    if (attrText != null)
                    {
                        attrText.text = $"{currentCfg.Value:F0}";
                    }
                }

                if (hasNext)
                {
                    var nextCfg = nextAttrs[i];
                    var nextAttrText = attrObj.transform.Find("NextAttr/Text")?.GetComponent<TMP_Text>();
                    if (nextAttrText != null)
                    {
                        nextAttrText.text = $"{nextCfg.Value:F0}";
                    }
                }

                if (hasCurrent)
                {
                    var attrName = attrConfig.GetOrDefault(currentAttrs[i].Type).Name;
                    var textName = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, attrName, "");
                    attrObj.transform.Find("AttrName").GetComponent<TMP_Text>().text = textName;
                }
            }
        }

        private void UpdateConsumes(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mOldPanel.UpGrade != null)
            {
                var consumes = vm.GetUpPhaseConsumes();
                RefreshConsumes(mOldPanel.UpGrade.Find("Consumes"), consumes);
            }

            UpdateRefineConsumes(vm);
        }

        private void UpdateRefineConsumes(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel.RefineConsume == null) return;

            var consumeItem = mOldPanel.RefineConsume.GetComponentInChildren<UIConsumeItem>();
            var consumes = vm.GetRefreshEntryConsumes();
            if (consumes == null || consumes.Count == 0)
            {
                mOldPanel.RefineConsume.gameObject.SetActive(false);
                return;
            }

            mOldPanel.RefineConsume.gameObject.SetActive(true);
            if (consumeItem == null)
            {
                LogTool.LogError("[EquipUpGradePanelNew] RefineConsume 上找不到 UIConsumeItem");
                return;
            }

            var consume = consumes[0];
            consumeItem.SetData(consume.Id, (int)consume.Num);
        }

        private void RefreshConsumes(Transform consumes, List<RewardAndNum> consumeItems)
        {
            if (consumes == null) return;

            for (int i = 0; i < consumes.childCount; i++)
            {
                var consumeObj = consumes.GetChild(i).gameObject;
                consumeObj.SetActive(consumeItems.Count > i);
                if (!consumeObj.activeSelf) continue;

                var item = consumeItems[i];
                var rewardEntity = consumeObj.GetComponent<RewardEntity>();
                if (rewardEntity != null)
                {
                    rewardEntity.SetData(item.Id, item.Num, RewardStyleType.Consume);
                }
            }
        }

        private void RefreshConsumes(Transform consumes, List<RewardItem> consumeItems)
        {
            if (consumes == null) return;

            for (int i = 0; i < consumes.childCount; i++)
            {
                var consumeObj = consumes.GetChild(i).gameObject;
                consumeObj.SetActive(consumeItems.Count > i);
                if (!consumeObj.activeSelf) continue;

                var item = consumeItems[i];
                var rewardEntity = consumeObj.GetComponent<RewardEntity>();
                if (rewardEntity != null)
                {
                    rewardEntity.SetData(item.Id, item.Num, RewardStyleType.Consume);
                }
            }
        }

        private void UpdateEntryList(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel == null) return;

            var equip = vm.CurrentEquip.Value;
            if (equip == null) return;

            RefreshRefineEntryList(mOldPanel.BeforeRefine?.transform, equip.Entrys);
            RefreshRefineEntryList(mOldPanel.AfterRefine?.transform, equip.TempEntrys);
            UpdateEffectList(vm);
        }

        /// <summary>
        /// 刷新洗炼前/后特效列表（EffectEntryList 下挂 EquipEffectEntry）
        /// </summary>
        private void UpdateEffectList(EquipUpGradePanelViewModel vm)
        {
            var equip = vm.CurrentEquip.Value;
            if (equip == null) return;

            var lockedIds = vm.SelectedLockedEffectIds.Value ?? new List<int>();
            RefreshRefineEffectList(
                mOldPanel.BeforeRefine?.transform,
                equip.Effects,
                showLock: true,
                lockedIds,
                vm);
            RefreshRefineEffectList(
                mOldPanel.AfterRefine?.transform,
                equip.TempEffects,
                showLock: false,
                lockedIds,
                vm);

            UpdateEffectProbabilityText(vm);
        }

        private void UpdateEffectProbabilityText(EquipUpGradePanelViewModel vm)
        {
            mOldPanel.EffectProbabilityText.text = vm.GetEffectProbabilityText();
        }

        private void RefreshRefineEffectList(
            Transform refineRoot,
            IList<int> effectIds,
            bool showLock,
            IList<int> lockedIds,
            EquipUpGradePanelViewModel vm)
        {
            if (refineRoot == null) return;

            var effectList = refineRoot.Find("EffectEntryList");
            if (effectList == null) return;

            for (int i = 0; i < effectList.childCount; i++)
            {
                var effectObj = effectList.GetChild(i).gameObject;
                var effectUI = effectObj.GetComponent<EquipEffectEntry>();
                var hasEffect = effectIds != null && i < effectIds.Count && effectIds[i] > 0;

                effectObj.SetActive(hasEffect);
                if (!hasEffect || effectUI == null) continue;

                var effectId = effectIds[i];
                var isLocked = showLock && lockedIds != null && lockedIds.Contains(effectId);
                effectUI.SetData(effectId, isLocked, showLock);

                if (!showLock || effectUI.Toggle == null) continue;

                effectUI.Toggle.onValueChanged.RemoveAllListeners();
                var capturedEffectId = effectId;
                effectUI.Toggle.onValueChanged.AddListener(isOn =>
                    OnEffectLockToggleChanged(vm, capturedEffectId, isOn));
            }
        }

        private void RefreshBeforeRefineEffectLocks(EquipUpGradePanelViewModel vm)
        {
            var equip = vm.CurrentEquip.Value;
            if (equip == null) return;

            var lockedIds = vm.SelectedLockedEffectIds.Value ?? new List<int>();
            RefreshRefineEffectList(
                mOldPanel.BeforeRefine?.transform,
                equip.Effects,
                showLock: true,
                lockedIds,
                vm);
        }

        private void OnEffectLockToggleChanged(EquipUpGradePanelViewModel vm, int effectId, bool isLocked)
        {
            vm.SetLockedEffect(effectId, isLocked);
        }

        /// <summary>
        /// 刷新洗炼前/后词条列表（EntryDetail 下挂 EquipEntryUI）
        /// </summary>
        private void RefreshRefineEntryList(Transform refineRoot, IList<int> entryIds)
        {
            if (refineRoot == null) return;

            var entryDetail = refineRoot.Find("EntryDetail");
            if (entryDetail == null) return;

            for (int i = 0; i < entryDetail.childCount; i++)
            {
                var entryObj = entryDetail.GetChild(i).gameObject;
                var entryUI = entryObj.GetComponent<EquipEntryUI>();
                var hasEntry = entryIds != null && i < entryIds.Count && entryIds[i] > 0;

                entryObj.SetActive(hasEntry);
                if (!hasEntry || entryUI == null) continue;

                entryUI.SetData(entryIds[i]);
            }
        }


        /// <summary>
        /// 选中装备变化时更新所有槽位的 FrameCheck 显隐
        /// </summary>
        private void RefreshEquipSlotChecks(int selectedEquipId)
        {
            foreach (var kvp in _wearEquipSlots)
            {
                var slotDisplay = kvp.Value.GetComponent<EquipSlotDisplay>();
                if (slotDisplay != null)
                    slotDisplay.SetSelected(kvp.Key == selectedEquipId);
            }
        }

        /// <summary>
        /// 初始化已佩戴装备槽位
        /// </summary>
        private void InitWearEquipSlot(EquipUpGradePanelViewModel vm)
        {
            if (mOldPanel?.WearEquipScroll == null) return;

            var wearEquipScroll = mOldPanel.WearEquipScroll.Find("Viewport/Content");
            if (wearEquipScroll == null) return;

            var equipList = vm.WearEquipList.Value;
            if (equipList == null || equipList.Count == 0) return;

            var template = wearEquipScroll.Find("EquipSlot");
            if (template != null)
            {
                template.gameObject.SetActive(false);
            }

            var activeEquipIds = new HashSet<int>(equipList.Count);
            foreach (var equip in equipList)
            {
                activeEquipIds.Add(equip.Id);

                if (!_wearEquipSlots.TryGetValue(equip.Id, out GameObject slotObj))
                {
                    if (template == null) continue;

                    slotObj = Instantiate(template.gameObject, wearEquipScroll, true);
                    _wearEquipSlots.Add(equip.Id, slotObj);
                }

                slotObj.SetActive(true);

                BindWearEquipSlotRedDot(slotObj.transform, equip.Id);

                var slotDisplay = slotObj.GetComponent<EquipSlotDisplay>();
                slotDisplay.SetPartType(equip.PartType);

                var equipId = equip.Id;
                var isSelected = vm.SelectedEquipId.Value == equipId;
                slotDisplay.SetEquip(equip, vm.SoldierId.Value, isSelected, () =>
                    vm.OnSelectEquipCommand.Execute(equipId));
            }

            foreach (var kvp in _wearEquipSlots)
            {
                if (!activeEquipIds.Contains(kvp.Key))
                {
                    kvp.Value.SetActive(false);
                }
            }
        }

        void BindWearEquipSlotRedDot(Transform slotRoot, int equipId)
        {
            var childViews = slotRoot.GetComponentsInChildren<LocalRedDotView>(true);
            for (int i = 0; i < childViews.Length; i++)
            {
                LocalRedDotView view = childViews[i];
                if (view.transform != slotRoot)
                {
                    Destroy(view);
                }
            }

            slotRoot.BindLocalRedDot(
                VM.RedDotScope, EquipUpGradePanelRedDotScope.Paths.WearSlot(equipId));

            slotRoot.GetComponent<LocalRedDotView>()?.RefreshWatch();
        }

        #endregion

        #region 事件处理

        private void OnRefreshEntryStarted()
        {
            mOldPanel.RefineBtn.interactable = false;
            mOldPanel.ReplaceBtn.interactable = false;
            CombatPowerChangeHelper.CaptureBaseline();
        }

        private void OnRefreshEntryEnded()
        {
            if (VM == null) return;

            mOldPanel.RefineBtn.interactable = VM.CanRefreshEntry.Value;
            mOldPanel.ReplaceBtn.interactable = VM.CanUseRefreshEntry.Value;
        }

        private void OnUseRefreshEntryStarted()
        {
            mOldPanel.RefineBtn.interactable = false;
            mOldPanel.ReplaceBtn.interactable = false;
            CombatPowerChangeHelper.CaptureBaseline();
        }

        private void OnUseRefreshEntryEnded()
        {
            if (VM == null) return;

            mOldPanel.RefineBtn.interactable = VM.CanRefreshEntry.Value;
            mOldPanel.ReplaceBtn.interactable = VM.CanUseRefreshEntry.Value;
        }

        private void OnCloseRequested()
        {
            CloseSelf();
        }

        private void OnShowSelectSoldier()
        {
            var selectSoldierPopup = UIKit.OpenPanel<SelectSoldierPopup>();
            selectSoldierPopup.SetData((soldierId) =>
            {
                if (VM == null) return;

                if (VM.OnSoldierSelected(soldierId))
                    RefreshUIAfterInit();
                else
                    Toast.Show(EquipUpGradePanelViewModel.NoWearEquipMessage);
            });

            if (selectSoldierPopup != null)
            {
                selectSoldierPopup.transform.SetParent(transform.Find("Canvas"));
                selectSoldierPopup.transform.localScale = Vector3.one;
            }
        }

        private void OnDisableGameMainFootBtn(bool enabled)
        {
            Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, enabled);
        }

        private void OnEnableGameMainFootBtn(bool enabled)
        {
            Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, enabled);
        }

        private void OnUpPhaseStarted()
        {
            if (mOldPanel?.UpGradeBtn != null)
            {
                mOldPanel.UpGradeBtn.interactable = false;
            }

            CombatPowerChangeHelper.CaptureBaseline();
        }

        private void OnUpPhaseEnded()
        {
            if (mOldPanel?.UpGradeBtn != null && VM != null)
            {
                mOldPanel.UpGradeBtn.interactable = VM.CanUpPhase.Value;
            }
        }

        private void OnUpPhaseSuccess()
        {
            PlayUpGradeEffect(() =>
            {
                UpdateConsumes(VM);
                UpdateAttributes(VM);
                UpdateMaxPhaseDisplay(VM);
                UpdateExtraAttributes(VM);
                InitWearEquipSlot(VM);
                VM?.RefreshLocalRedDots();

                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
            });
        }

        private void OnUseRefreshEntrySuccess()
        {
            PlayUpGradeEffect(() =>
            {
                UpdateEntryList(VM);
                UpdateEffectList(VM);
                UpdateRefineConsumes(VM);
                VM?.RefreshLocalRedDots();
                CombatPowerChangeHelper.RecalculateAndShowTipIfIncreased();
            });
        }

        private void OnRefreshEntrySuccess()
        {
            PlayUpGradeEffect(() =>
            {
                UpdateEntryList(VM);
                UpdateEffectList(VM);
                UpdateRefineConsumes(VM);
                VM?.RefreshLocalRedDots();

                GuideFlow.Instance.SetStepComplete(GuideType.EquipRefreshEntry);
            });
        }

        private void OnEntryPageOpened()
        {
            UnlockGuideManager.Send(ModuleFunctionType.EquipEntry, 4);
        }

        #endregion

        #region 特效和引导

        private void PlayUpGradeEffect(System.Action callback = null)
        {
            if (mOldPanel?.ForgingEffectHammer01 == null || mOldPanel?.ForgingEffectUpgrade01 == null)
            {
                callback?.Invoke();
                return;
            }

            mOldPanel.ForgingEffectHammer01.gameObject.SetActive(true);
            DOVirtual.DelayedCall(0.4f, () =>
            {
                // 在特效中间执行回调（刷新数据）
                callback?.Invoke();
                
                SoundManager.Instance.PlayVoice(SoundType.SoundEquipForging);
                mOldPanel.ForgingEffectUpgrade01.gameObject.SetActive(true);

                DOVirtual.DelayedCall(1f, () =>
                {
                    mOldPanel.ForgingEffectHammer01.gameObject.SetActive(false);
                    mOldPanel.ForgingEffectUpgrade01.gameObject.SetActive(false);
                });
            });
        }

        private IEnumerator WaitGuide()
        {
            // 锻造引导第4步：高亮本界面 UpGradeBtn

            if (mOldPanel?.UpGrade != null)
            {
                mOldPanel.UpGrade.gameObject.SetActive(true);
            }

            if (mOldPanel?.Refine != null)
            {
                mOldPanel.Refine.gameObject.SetActive(false);
            }

            yield return null;
            Canvas.ForceUpdateCanvases();
            yield return new WaitForSeconds(0.35f);

            if (UnlockGuideManager.Instance.CheckUnlockGuideRunning(ModuleFunctionType.EquipPhase))
            {
                if (mOldPanel.UpGradeBtn != null)
                {
                    mOldPanel.UpGradeBtn.gameObject.SetActive(true);
                    UnlockGuideManager.Instance.SetUnlockTypeStep(
                        ModuleFunctionType.EquipPhase,
                        4,
                        mOldPanel.UpGradeBtn.GetComponent<RectTransform>());
                }
                else
                {
                    UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.EquipPhase, 4);
                }
            }

            if (UnlockGuideManager.Instance.CheckUnlockGuideRunning(ModuleFunctionType.EquipEntry))
            {
                if (_guideAttr != null)
                {
                    UnlockGuideManager.Instance.SetUnlockTypeStep(
                        ModuleFunctionType.EquipEntry,
                        5,
                        _guideAttr.GetComponent<RectTransform>());
                }
                else
                {
                    UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.EquipEntry, 5);
                }
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（从世界界面打开）
        /// </summary>
        public void SetData()
        {
            if (VM != null)
            {
                VM.InitializeFromWorld();
                // 注意：从世界界面打开时，需要等待士兵选择后才刷新 UI
                // 刷新逻辑在 OnSoldierSelected 回调中处理
            }
        }

        /// <summary>
        /// 设置数据（指定士兵）
        /// </summary>
        public void SetData(int soldierId, int equipId = 0)
        {
            if (VM == null) return;

            if (VM.Initialize(soldierId, equipId))
                RefreshUIAfterInit();
            else
                Toast.Show(EquipUpGradePanelViewModel.NoWearEquipMessage);
        }
        
        /// <summary>
        /// 初始化后刷新 UI
        /// </summary>
        private void RefreshUIAfterInit()
        {
            if (VM == null) return;
            SyncBoundPropertyValues(VM);
        }

        /// <summary>
        /// 切换页面
        /// </summary>
        public void ChangePage(int index)
        {
            if (VM == null) return;

            if (index == EquipUpGradePanelViewModel.ForgePageIndex)
            {
                VM.OnForgePageCommand.Execute();
            }
            else if (index == EquipUpGradePanelViewModel.EntryPageIndex)
            {
                VM.OnEntryPageCommand.Execute();
                SyncToggleState(VM.CurrentPage.Value);
                // 引导 - 选择锻造按钮（与原逻辑一致）
                // GuideFlow.Instance.SetStep(GuideType.EquipUpgradePhase, 2, mOldPanel?.UpGradeBtn?.transform);
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            if (VM != null)
            {
                VM.ChangePage(EquipUpGradePanelViewModel.ForgePageIndex);
                SyncBoundPropertyValues(VM);
            }
            // 启动引导协程
            StartCoroutine(WaitGuide());
        }

        protected override void OnShow()
        {
            // 重置从世界界面打开的标志（与原逻辑一致）
            if (VM != null)
            {
                VM.ResetWorldPanelFlag();
            }
            
            // 启动引导协程
            StartCoroutine(WaitGuide());
        }

        protected override void OnClose()
        {
            VM?.DeactivateRedDot();

            if (VM != null && VM.IsFromWorldPanel.Value)
            {
                OnEnableGameMainFootBtn(true);
            }
        }

        #endregion
    }
}
