using Game.Player;
using Game.Equip;
using UnityEngine;
using UIFramework;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Model;
using FusionContinent.UI.Components;
using Game.Login;
using Game.Equip.RedDot;
using Game.RedDot;
using Game.RedDot.View;
using Game.Soldier;
using ScrollView;
using TMPro;
using UIFramework.Services;

namespace FusionContinent
{
    [PrefabPath("SpeakWhellPanel")]
    public class SpeakWhellPanelNew : UIPanel<SpeakWhellPanelViewModel>
    {
        private SpeakWhellPanel mOldPanel;
        private ToggleGroup mToggleGroup;

        private UnityEngine.Events.UnityAction<bool> mMaterialToggleHandler;
        private UnityEngine.Events.UnityAction<bool> mFusionGemToggleHandler;
        private UnityEngine.Events.UnityAction<bool> mEquipToggleHandler;

        private UIScrollView mMaterialListScrollView;

        private UIScrollView mFusionGemListScrollView;

        private UIScrollView mEquipListScrollView;

        private EquipSlotDisplay[] mEquipSlotDisplays;

        readonly SpeakWhellPanelRedDotScope _redDotScope = new SpeakWhellPanelRedDotScope();
        System.Action<object> _redChangeHandler;
        bool _redDotActive;

        private TbItemConfig mItemConfig;
        private TbGameBattleConst mTbBattleConst;

        private RectTransform mPlayerFigureSpine;
        private string mLastAppliedSpeakerSpineAddress;
        private readonly PrefabInstanceLoader.Handle mSpeakerSpineHandle = new PrefabInstanceLoader.Handle();

        private UnityEngine.Events.UnityAction mTalentBtnClickHandler;
        private System.Action<int, int> mFightPowerChangedHandler;
        private UnityEngine.Events.UnityAction mSkinBtnClickHandler;
        private UnityEngine.Events.UnityAction mEditNameBtnClickHandler;
        private UnityEngine.Events.UnityAction mEquipUpGradeBtnClickHandler;
        private UnityEngine.Events.UnityAction mArtifactBtnClickHandler;
        private UnityEngine.Events.UnityAction mHeroSeatBtnClickHandler;

        private System.Action<string, string> mSpeakerNameChangedHandler;
        private System.Action<string, string> mSpeakerSpineAddressChangedHandler;
        private System.Action<object> mFusionGemWearChangedHandler;

        void Awake()
        {
            mOldPanel = GetComponent<SpeakWhellPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] 找不到 SpeakWhellPanel 组件");
            }

            CachePlayerFigureSpine();
        }

        private void CachePlayerFigureSpine()
        {
            Transform spine = transform.Find("TweenTarget/PlayerFigureSpine");
            if (spine == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] 找不到 TweenTarget/PlayerFigureSpine 节点");
                return;
            }

            mPlayerFigureSpine = spine as RectTransform;
            if (mPlayerFigureSpine == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] PlayerFigureSpine 不是 RectTransform");
            }
        }

        protected override void OnBindViewModel(SpeakWhellPanelViewModel vm)
        {
            if (mOldPanel == null)
            {
                return;
            }

            mItemConfig = DataConfigManager.Tables.TbItemConfig;
            mTbBattleConst = DataConfigManager.Tables.TbGameBattleConst;

            BindMaterialScrollView();
            BindFusionGemScrollView();
            BindEquipScrollView();

            BindToggleGroup();
            BindToggleEvents();
            BindViewModelStates(vm);
            ApplySelectedTab(vm.SelectedTab.Value, force: true);

            BindButtons(vm);

            BindFightPower(vm);

            // 融合石穿卸后刷新装备槽位上的 Gem 图标，并刷新融合石列表红点
            mFusionGemWearChangedHandler = _ =>
            {
                RefreshSpeakerWearEquipSlots();
                RefreshBagFusionGemRedDots();
            };
            Global.Publisher.Subscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mFusionGemWearChangedHandler);
            // Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_NAME, OnUserNameChanged);

            ActivateRedDot();
        }

        protected override void OnUnbindViewModel(SpeakWhellPanelViewModel vm)
        {
            DeactivateRedDot();

            UnbindViewModelStates(vm);
            UnbindFightPower(vm);
            UnbindButtons();
            UnbindToggleEvents();

            if (mFusionGemWearChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.FUSION_GEM_WEAR_CHANGED, mFusionGemWearChangedHandler);
                mFusionGemWearChangedHandler = null;
            }

            // Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_USER_NAME, OnUserNameChanged);
            ReleaseMaterialScrollView();
            ReleaseFusionGemScrollView();
            ReleaseEquipScrollView();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            this.mOldPanel.FusionGemBtn.interactable = ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot,false); 
            this.mOldPanel.FusionGemBtn.transform.Find("Lock")?.gameObject.SetActive(!ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot,false));
            // 注意：首次打开只会走 OnOpen，不会走 OnShow，所以初始化放这里
            ApplySelectedTab(VM != null ? VM.SelectedTab.Value : SpeakWhellTab.Equip, force: true);
            RefreshWearFusionGemSlots();
            // 先开局部树（VM 在 OnCreate 时可能已有穿戴数据），再刷列表，避免 ShowEquipList 时树为空
            SyncRedDotScope();
            VM?.RefreshSpeakerEquipLists();
            SyncRedDotScope();
            RefreshAndShowCurrentTab(forceRefresh: false);
            BindGuideNodes();
            RefreshSpeakerEquipUpGradeRedPoint();
            RefreshSpeakerSpine();
            // 首开时 ScrollView 常未完成 Layout（Viewport 高度为 0），item 要到后续帧才创建；下一帧再刷箭头
            RefreshEquipArrowsAfterLayout().Forget();
        }


        void BindGuideNodes()
        {
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.FusionGemSlot,2,mOldPanel.FusionGemBtn.GetComponent<RectTransform>());
        }

        protected override void OnShow()
        {
            // 显示时再次保证选中与显隐正确（某些情况下OnBind先于节点激活/或外部改了Toggle状态）
            ApplySelectedTab(VM != null ? VM.SelectedTab.Value : SpeakWhellTab.Equip, force: true);

            // OnShow 时刷新战力：若值有变化，会触发绑定回调并 Play 到新战力
            VM?.RefreshFightPower();
            RefreshWearFusionGemSlots();
            SyncRedDotScope();
            VM?.RefreshSpeakerEquipLists();
            SyncRedDotScope();
            RefreshAndShowCurrentTab(forceRefresh: false);
            BindGuideNodes();
            RefreshSpeakerEquipUpGradeRedPoint();
            RefreshSpeakerSpine();
            RefreshEquipArrowsAfterLayout().Forget();
        }

        private void RefreshSpeakerSpine()
        {
            if (VM == null)
            {
                return;
            }

            VM.RefreshSpeakerPresentation(force: true);
            ApplySpeakerPresentation(force: true);
        }

        private void OnSpeakerSpineAddressChanged(string oldValue, string newValue)
        {
            ApplySpeakerPresentation(force: false);
        }

        async UniTaskVoid RefreshEquipArrowsAfterLayout()
        {
            // 等到下一帧 LateUpdate 之后：ScrollView.Start / Viewport 尺寸已就绪，可见 item 已创建
            await UniTask.NextFrame();
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            if (!this)
            {
                return;
            }

            if (VM == null)
            {
                return;
            }

            if (!_redDotScope.IsOpen)
            {
                SyncRedDotScope();
            }

            Canvas.ForceUpdateCanvases();

            if (VM.SelectedTab.Value == SpeakWhellTab.Equip)
            {
                ShowEquipList();
                RefreshBagEquipArrowDots();
            }
            else if (VM.SelectedTab.Value == SpeakWhellTab.FusionGem)
            {
                ShowFusionGemList();
                RefreshBagFusionGemRedDots();
            }
        }

        void ActivateRedDot()
        {
            if (_redDotActive)
            {
                return;
            }

            _redDotActive = true;
            _redChangeHandler = OnEquipRedDotDataChanged;
            EquipRedDotRefreshEvents.Subscribe(_redChangeHandler);
        
        }

        void DeactivateRedDot()
        {
            if (!_redDotActive)
            {
                return;
            }

            _redDotActive = false;

            if (_redChangeHandler != null)
            {
                EquipRedDotRefreshEvents.Unsubscribe(_redChangeHandler);
                _redChangeHandler = null;
            }

            _redDotScope.Close();
        }

        void SyncRedDotScope()
        {
            if (VM == null || mTbBattleConst == null)
            {
                return;
            }

            int partCount = mTbBattleConst.EquipPartTypeList?.Count ?? 0;
            _redDotScope.Sync(VM.GetSpeakerEquipWearerId(), VM.SpeakerWearPartSlots.Value, partCount);
        }

        void OnEquipRedDotDataChanged(object _)
        {
            if (!_redDotActive)
            {
                return;
            }

            SyncRedDotScope();
            RefreshSpeakerEquipUpGradeRedPoint();
            RefreshWearEquipSlotRedDots();
            RefreshBagEquipArrowDots();
            RefreshBagFusionGemRedDots();
        }

        void RefreshWearEquipSlotRedDots()
        {
            if (!_redDotScope.IsOpen)
            {
                return;
            }

            _redDotScope.MarkAllDirty();
            RedDotService.Instance.FlushNow();
        }

        /// <summary>
        /// 装备页可见背包装备重新 Watch + MarkDirty，保证操作后箭头仍按战力比较显示。
        /// </summary>
        void RefreshBagEquipArrowDots()
        {
            if (VM == null || VM.SelectedTab.Value != SpeakWhellTab.Equip)
            {
                return;
            }

            if (!_redDotScope.IsOpen || mOldPanel?.EquipCardScrollView == null)
            {
                return;
            }

            LocalRedDotView[] views = mOldPanel.EquipCardScrollView.GetComponentsInChildren<LocalRedDotView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                views[i].RefreshWatch();
            }

            _redDotScope.MarkAllDirty();
            RedDotService.Instance.FlushNow();
        }

        /// <summary>
        /// 融合石页可见背包石重新 Watch + MarkDirty。
        /// </summary>
        void RefreshBagFusionGemRedDots()
        {
            if (VM == null || VM.SelectedTab.Value != SpeakWhellTab.FusionGem)
            {
                return;
            }

            if (!_redDotScope.IsOpen || mOldPanel?.FusionGemScrollView == null)
            {
                return;
            }

            LocalRedDotView[] views = mOldPanel.FusionGemScrollView.GetComponentsInChildren<LocalRedDotView>(true);
            for (int i = 0; i < views.Length; i++)
            {
                views[i].RefreshWatch();
            }

            _redDotScope.MarkAllDirty();
            RedDotService.Instance.FlushNow();
        }

        private void RefreshSpeakerEquipUpGradeRedPoint()
        {
            EquipGlobalRedDotRefresh.PublishSpeakerEntry();
        }

        protected override void OnHide()
        {
            ReleaseMaterialScrollView();
            ReleaseFusionGemScrollView();
            ReleaseEquipScrollView();
            mSpeakerSpineHandle.Clear();
            mLastAppliedSpeakerSpineAddress = null;
        }

        private void BindFightPower(SpeakWhellPanelViewModel vm)
        {
            if (mOldPanel?.CombatNum == null)
            {
                LogTool.LogWarning("[SpeakWhellPanelNew] CombatNum 为空，无法显示战力");
                return;
            }

            // 打开时先 Init 当前战力
            vm.RefreshFightPower();
            mOldPanel.CombatNum.Init(vm.FightPowerValue.Value);

            // 战力变更时 Play
            mFightPowerChangedHandler = (oldVal, newVal) =>
            {
                mOldPanel?.CombatNum?.Play(newVal);
            };
            vm.FightPowerValue.OnValueChanged += mFightPowerChangedHandler;
        }

        private void UnbindFightPower(SpeakWhellPanelViewModel vm)
        {
            if (mFightPowerChangedHandler != null)
            {
                vm.FightPowerValue.OnValueChanged -= mFightPowerChangedHandler;
                mFightPowerChangedHandler = null;
            }
        }

        private void BindButtons(SpeakWhellPanelViewModel vm)
        {
            if (mOldPanel?.TalentBtn != null)
            {
                mOldPanel.TalentBtn.SetRedType(RedNodeType.TalentUpgradeBtn, RedDotDisplay.ArrowPointPrefab);
                mTalentBtnClickHandler = () => vm.OnTalentCommand?.Execute();
                mOldPanel.TalentBtn.onClick.AddListener(mTalentBtnClickHandler);
            }

            if (mOldPanel?.SkinBtn != null)
            {
                mOldPanel.SkinBtn.SetRedType(RedNodeType.SpeakerSkinActivateBtn);
                mSkinBtnClickHandler = () => vm.OnSkinCommand?.Execute();
                mOldPanel.SkinBtn.onClick.AddListener(mSkinBtnClickHandler);
            }

            if (mOldPanel?.EditNameBtn != null)
            {
                mEditNameBtnClickHandler = OnEditNameBtnClicked;
                mOldPanel.EditNameBtn.onClick.AddListener(mEditNameBtnClickHandler);
            }

            if (mOldPanel?.EquipUpGradeBtn != null)
            {
                mOldPanel.EquipUpGradeBtn.SetRedType(RedNodeType.SpeakerEquipUpGradeBtn);
                mEquipUpGradeBtnClickHandler = () => vm.OnEquipUpGradeCommand?.Execute();
                mOldPanel.EquipUpGradeBtn.onClick.AddListener(mEquipUpGradeBtnClickHandler);
            }

            if (mOldPanel?.ArtifactBtn != null)
            {
                mOldPanel.ArtifactBtn.SetRedType(RedNodeType.SpeakerWeaponBtn);
                mArtifactBtnClickHandler = OnArtifactBtnClicked;
                mOldPanel.ArtifactBtn.onClick.AddListener(mArtifactBtnClickHandler);
            }

            if (mOldPanel?.HeroSeatBtn != null)
            {
                mOldPanel.HeroSeatBtn.SetRedType(RedNodeType.SpeakerHeroSeatBtn);
                mHeroSeatBtnClickHandler = OnHeroSeatBtnClicked;
                mOldPanel.HeroSeatBtn.onClick.AddListener(mHeroSeatBtnClickHandler);
            }

            mOldPanel.EquipDecomposition.onClick.AddListener(OnEquipDecompositionBtnClicked);
        }

        private void UnbindButtons()
        {
            if (mOldPanel?.TalentBtn != null && mTalentBtnClickHandler != null)
            {
                mOldPanel.TalentBtn.onClick.RemoveListener(mTalentBtnClickHandler);
                mTalentBtnClickHandler = null;
            }

            if (mOldPanel?.SkinBtn != null && mSkinBtnClickHandler != null)
            {
                mOldPanel.SkinBtn.onClick.RemoveListener(mSkinBtnClickHandler);
                mSkinBtnClickHandler = null;
            }

            if (mOldPanel?.EditNameBtn != null && mEditNameBtnClickHandler != null)
            {
                mOldPanel.EditNameBtn.onClick.RemoveListener(mEditNameBtnClickHandler);
                mEditNameBtnClickHandler = null;
            }

            if (mOldPanel?.EquipUpGradeBtn != null && mEquipUpGradeBtnClickHandler != null)
            {
                mOldPanel.EquipUpGradeBtn.onClick.RemoveListener(mEquipUpGradeBtnClickHandler);
                mEquipUpGradeBtnClickHandler = null;
            }

            if (mOldPanel?.ArtifactBtn != null && mArtifactBtnClickHandler != null)
            {
                mOldPanel.ArtifactBtn.onClick.RemoveListener(mArtifactBtnClickHandler);
                mArtifactBtnClickHandler = null;
            }

            if (mOldPanel?.HeroSeatBtn != null && mHeroSeatBtnClickHandler != null)
            {
                mOldPanel.HeroSeatBtn.onClick.RemoveListener(mHeroSeatBtnClickHandler);
                mHeroSeatBtnClickHandler = null;
            }

            mOldPanel.EquipDecomposition.onClick.RemoveListener(OnEquipDecompositionBtnClicked);
        }

        private void OnEditNameBtnClicked()
        {
            var vm = new CreateNamePanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<CreateNamePanelNew, CreateNamePanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnEquipDecompositionBtnClicked()
        {
            var vm = new EquipFragmentPanelViewModel();
            vm.OnCreate();
            vm.Initialize(VM.GetSpeakerEquipWearerId());
            UIFramework.UIManager.OpenPanelAsync<EquipFragmentPanelNew, EquipFragmentPanelViewModel>(
                vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnArtifactBtnClicked()
        {
            var vm = new WeaponPanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<WeaponPanelNew, WeaponPanelViewModel>(vm, UIFramework.UILevel.Panel).Forget();
        }

        private void OnHeroSeatBtnClicked()
        {
            var vm = new Game.HeroSeat.HeroSeatPanelViewModel();
            vm.OnCreate();
            UIFramework.UIManager.OpenPanelAsync<HeroSeatPanelNew, Game.HeroSeat.HeroSeatPanelViewModel>(vm, UIFramework.UILevel.Panel).Forget();
        }

        private void BindViewModelStates(SpeakWhellPanelViewModel vm)
        {
            vm.SelectedTab.OnValueChanged += OnSelectedTabVmChanged;

            // SpeakerName
            mSpeakerNameChangedHandler = (_, newVal) =>
            {
                if (mOldPanel?.NameText != null)
                {
                    // mOldPanel.NameText.text = newVal ?? string.Empty;
                }
            };
            vm.SpeakerName.OnValueChanged += mSpeakerNameChangedHandler;

            // 与 SpeakWheelSkinPanel 相同：按当前穿戴皮肤 ShowSpine 加载 Speaker_{key}
            mSpeakerSpineAddressChangedHandler = OnSpeakerSpineAddressChanged;
            vm.SpeakerSpineAddress.OnValueChanged += mSpeakerSpineAddressChangedHandler;

            // 先同步一次（避免首次打开时不触发 OnValueChanged）
            if (mOldPanel?.NameText != null)
            {
                mOldPanel.NameText.text = Global.User.UserName;
            }
            ApplySpeakerPresentation(force: true);

            vm.WearFusionGemSlotList.OnValueChanged += OnWearFusionGemSlotListVmChanged;
            RefreshWearFusionGemSlots();

            vm.EquipDisplayed.OnValueChanged += OnEquipDisplayedVmChanged;
            vm.SpeakerWearPartSlots.OnValueChanged += OnSpeakerWearPartSlotsVmChanged;
            RefreshSpeakerWearEquipSlots();
        }

        private void OnUserNameChanged(object userName)
        {
            mOldPanel.NameText.text = (string)userName;
        }

        private void OnSelectedTabVmChanged(SpeakWhellTab oldTab, SpeakWhellTab newTab)
        {
            ApplySelectedTab(newTab, force: true);
            RefreshAndShowCurrentTab(forceRefresh: true);
        }

        private void OnWearFusionGemSlotListVmChanged(List<PlayerFusionGem> oldList, List<PlayerFusionGem> newList)
        {
            RefreshWearFusionGemSlots();
            RefreshSpeakerWearEquipSlots();
            if (VM != null && VM.SelectedTab.Value == SpeakWhellTab.FusionGem)
            {
                ShowFusionGemList();
            }
        }

        private void OnEquipDisplayedVmChanged(List<PlayerEquip> oldList, List<PlayerEquip> newList)
        {
            if (VM != null && VM.SelectedTab.Value == SpeakWhellTab.Equip)
            {
                ShowEquipList();
            }
        }

        private void OnSpeakerWearPartSlotsVmChanged(List<PlayerEquip> oldList, List<PlayerEquip> newList)
        {
            SyncRedDotScope();
            RefreshSpeakerWearEquipSlots();
            RefreshWearEquipSlotRedDots();
            RefreshBagEquipArrowDots();
            RefreshBagFusionGemRedDots();
        }

        private void UnbindViewModelStates(SpeakWhellPanelViewModel vm)
        {
            vm.SelectedTab.OnValueChanged -= OnSelectedTabVmChanged;

            if (mSpeakerNameChangedHandler != null)
            {
                vm.SpeakerName.OnValueChanged -= mSpeakerNameChangedHandler;
                mSpeakerNameChangedHandler = null;
            }

            if (mSpeakerSpineAddressChangedHandler != null)
            {
                vm.SpeakerSpineAddress.OnValueChanged -= mSpeakerSpineAddressChangedHandler;
                mSpeakerSpineAddressChangedHandler = null;
            }

            mSpeakerSpineHandle.Clear();
            mLastAppliedSpeakerSpineAddress = null;

            vm.WearFusionGemSlotList.OnValueChanged -= OnWearFusionGemSlotListVmChanged;

            vm.EquipDisplayed.OnValueChanged -= OnEquipDisplayedVmChanged;
            vm.SpeakerWearPartSlots.OnValueChanged -= OnSpeakerWearPartSlotsVmChanged;
        }

        private void RefreshWearFusionGemSlots()
        {
            if (mOldPanel?.WearEquipList == null || VM == null)
            {
                return;
            }

            // 只显示前6个槽位
            const int slotCount = 6;
            var slots = VM.WearFusionGemSlotList?.Value;

            for (int i = 0; i < slotCount; i++)
            {
                if (i >= mOldPanel.WearEquipList.childCount)
                {
                    break;
                }

                var slotNode = mOldPanel.WearEquipList.GetChild(i);
                // if (slotNode == null) continue;
                slotNode.gameObject.SetActive(true);
                var entity = slotNode.GetComponentInChildren<GemStoneEntity>(true);
                // entity.gameObject.SetActive(false);
                var gem = (slots != null && i < slots.Count) ? slots[i] : null;
                if (gem == null)
                {
                    entity.gameObject.SetActive(false);
                    continue;
                }
                else
                {
                    entity.gameObject.SetActive(true);
                }
                

                entity.SetData(new GemStoneData
                {
                    GemId = gem.GemId,
                    Equip = false, // 槽位列表本身就是已装备的融合石
                    Level = gem.Level,
                });
            }
        }

        private void ApplySpeakerPresentation(bool force)
        {
            if (mPlayerFigureSpine == null)
            {
                CachePlayerFigureSpine();
            }

            if (mPlayerFigureSpine == null)
            {
                return;
            }

            string address = VM.SpeakerSpineAddress.Value;
            if (string.IsNullOrEmpty(address))
            {
                mSpeakerSpineHandle.Clear();
                mLastAppliedSpeakerSpineAddress = null;
                return;
            }

            if (!force && address == mLastAppliedSpeakerSpineAddress)
            {
                return;
            }

            // 与 SpeakWheelSkinPanelNew.RefreshSpine 一致：Speaker_{ShowSpine}
            mSpeakerSpineHandle.LoadAndReplace(address, mPlayerFigureSpine, normalizeRect: true);
            mLastAppliedSpeakerSpineAddress = address;
        }

        private void RefreshAndShowCurrentTab(bool forceRefresh)
        {
            if (VM == null)
            {
                return;
            }

            switch (VM.SelectedTab.Value)
            {
                case SpeakWhellTab.Material:
                    if (forceRefresh)
                    {
                        VM.RefreshMaterialDisplayedCards();
                    }
                    ReleaseFusionGemScrollView();
                    ReleaseEquipScrollView();
                    ShowMaterialList();
                    break;
                case SpeakWhellTab.FusionGem:
                    ReleaseMaterialScrollView();
                    ReleaseEquipScrollView();
                    if (forceRefresh)
                    {
                        VM.RefreshFusionGemDisplayed();
                    }
                    ShowFusionGemList();
                    break;
                case SpeakWhellTab.Equip:
                    ReleaseMaterialScrollView();
                    ReleaseFusionGemScrollView();
                    if (forceRefresh)
                    {
                        VM.RefreshSpeakerEquipLists();
                    }
                    ShowEquipList();
                    break;
            }
        }

        private void BindToggleGroup()
        {
            if (mOldPanel.MaterialBtn == null || mOldPanel.FusionGemBtn == null || mOldPanel.EquipBtn == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] Toggle引用为空：MaterialBtn、FusionGemBtn 或 EquipBtn 未绑定");
                return;
            }

            // 优先挂在 Toggle 的父节点上，避免额外创建对象
            var groupNode = mOldPanel.MaterialBtn.transform.parent != null
                ? mOldPanel.MaterialBtn.transform.parent.gameObject
                : mOldPanel.MaterialBtn.gameObject;

            mToggleGroup = groupNode.GetComponent<ToggleGroup>();
            if (mToggleGroup == null)
            {
                mToggleGroup = groupNode.AddComponent<ToggleGroup>();
            }

            mToggleGroup.allowSwitchOff = false;
            mOldPanel.MaterialBtn.group = mToggleGroup;
            mOldPanel.FusionGemBtn.group = mToggleGroup;
            mOldPanel.EquipBtn.group = mToggleGroup;
        }

        private void BindToggleEvents()
        {
            if (mOldPanel.MaterialBtn == null || mOldPanel.FusionGemBtn == null || mOldPanel.EquipBtn == null)
            {
                return;
            }

            mMaterialToggleHandler = isOn =>
            {
                if (isOn && VM != null)
                {
                    VM.SelectedTab.Value = SpeakWhellTab.Material;
                }
            };
            mOldPanel.MaterialBtn.onValueChanged.AddListener(mMaterialToggleHandler);

            mFusionGemToggleHandler = isOn =>
            {
                if (isOn && VM != null)
                {
                    VM.SelectedTab.Value = SpeakWhellTab.FusionGem;
                }
            };
            mOldPanel.FusionGemBtn.onValueChanged.AddListener(mFusionGemToggleHandler);

            mEquipToggleHandler = isOn =>
            {
                if (isOn && VM != null)
                {
                    VM.SelectedTab.Value = SpeakWhellTab.Equip;
                }
            };
            mOldPanel.EquipBtn.onValueChanged.AddListener(mEquipToggleHandler);

            // 装备页签：背包存在更优装备时显示箭头（非红点），聚合到导航栏 NavBarSpeaker
            mOldPanel.EquipBtn.transform.BindRedDot(
                RedNodeType.SpeakerEquipReplaceRedDot,
                RedDotDisplay.ArrowPointPrefab);

            // 融合石页签：可替换或可突破时显示箭头，聚合到导航栏 NavBarSpeaker
            mOldPanel.FusionGemBtn.transform.BindRedDot(
                RedNodeType.SpeakerFusionGemBtn,
                RedDotDisplay.ArrowPointPrefab);
        }

        private void UnbindToggleEvents()
        {
            if (mOldPanel == null)
            {
                return;
            }

            if (mOldPanel.MaterialBtn != null && mMaterialToggleHandler != null)
            {
                mOldPanel.MaterialBtn.onValueChanged.RemoveListener(mMaterialToggleHandler);
                mMaterialToggleHandler = null;
            }

            if (mOldPanel.FusionGemBtn != null && mFusionGemToggleHandler != null)
            {
                mOldPanel.FusionGemBtn.onValueChanged.RemoveListener(mFusionGemToggleHandler);
                mFusionGemToggleHandler = null;
            }

            if (mOldPanel.EquipBtn != null && mEquipToggleHandler != null)
            {
                mOldPanel.EquipBtn.onValueChanged.RemoveListener(mEquipToggleHandler);
                mEquipToggleHandler = null;
            }
        }

        private void ApplySelectedTab(SpeakWhellTab tab, bool force = false)
        {
            if (mOldPanel == null)
            {
                return;
            }

            var isMaterial = tab == SpeakWhellTab.Material;
            var isFusion = tab == SpeakWhellTab.FusionGem;
            var isEquip = tab == SpeakWhellTab.Equip;

            if (mOldPanel.MaterialBtn != null && mOldPanel.FusionGemBtn != null && mOldPanel.EquipBtn != null)
            {
                if (force)
                {
                    mOldPanel.MaterialBtn.SetIsOnWithoutNotify(isMaterial);
                    mOldPanel.FusionGemBtn.SetIsOnWithoutNotify(isFusion);
                    mOldPanel.EquipBtn.SetIsOnWithoutNotify(isEquip);
                }
                else
                {
                    if (mOldPanel.MaterialBtn.isOn != isMaterial)
                    {
                        mOldPanel.MaterialBtn.SetIsOnWithoutNotify(isMaterial);
                    }

                    if (mOldPanel.FusionGemBtn.isOn != isFusion)
                    {
                        mOldPanel.FusionGemBtn.SetIsOnWithoutNotify(isFusion);
                    }

                    if (mOldPanel.EquipBtn.isOn != isEquip)
                    {
                        mOldPanel.EquipBtn.SetIsOnWithoutNotify(isEquip);
                    }
                }
            }

            if (mOldPanel.MaterialScrollView != null)
            {
                mOldPanel.MaterialScrollView.gameObject.SetActive(isMaterial);
            }

            if (mOldPanel.FusionGemScrollView != null)
            {
                mOldPanel.FusionGemScrollView.gameObject.SetActive(isFusion);
            }



            if (mOldPanel.EquipCardScrollView != null)
            {
                mOldPanel.EquipCardScrollView.gameObject.SetActive(isEquip);
            }
        }

        private void BindMaterialScrollView()
        {
            if (mOldPanel?.MaterialScrollView == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] MaterialScrollView 未绑定");
                return;
            }

            // SpeakWhellPanel.prefab 上同节点挂了 UIVerticalScrollViewLock（实现 UIScrollView）
            mMaterialListScrollView = mOldPanel.MaterialScrollView.GetComponent<UIVerticalScrollViewLock>();
            if (mMaterialListScrollView == null)
            {
                // 兜底：如果未来改成其他实现，仍按 UIScrollView 抽象走
                mMaterialListScrollView = mOldPanel.MaterialScrollView.GetComponent<UIScrollView>();
            }

            if (mMaterialListScrollView == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] MaterialScrollView 缺少 UIScrollView 组件（UIVerticalScrollViewLock）");
                return;
            }

            // mMaterialListScrollView.SetPrefabFactory(CreateMaterialItemNode);
            // mMaterialListScrollView.SetReleaseAction(ReleaseMaterialItemNode);
            mMaterialListScrollView.SetUpdateFunc(OnUpdateMaterialItem);
            mMaterialListScrollView.SetUpdateSelectFunc(SelectMaterialItem);
        }

        private void ReleaseMaterialScrollView()
        {
            if (mMaterialListScrollView != null)
            {
                mMaterialListScrollView.ReleaseScrollView();
            }
        }

        private void BindFusionGemScrollView()
        {
            if (mOldPanel?.FusionGemScrollView == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] FusionGemScrollView 未绑定");
                return;
            }

            mFusionGemListScrollView = mOldPanel.FusionGemScrollView.GetComponent<UIVerticalScrollViewLock>();
            if (mFusionGemListScrollView == null)
            {
                mFusionGemListScrollView = mOldPanel.FusionGemScrollView.GetComponent<UIScrollView>();
            }

            if (mFusionGemListScrollView == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] FusionGemScrollView 缺少 UIScrollView 组件（UIVerticalScrollViewLock）");
                return;
            }

            // mFusionGemListScrollView.SetPrefabFactory(CreateFusionGemItemNode);
            // mFusionGemListScrollView.SetReleaseAction(ReleaseMaterialItemNode);
            mFusionGemListScrollView.SetUpdateFunc(OnUpdateFusionGemItem);
            mFusionGemListScrollView.SetUpdateSelectFunc(SelectFusionGemItem);
        }

        private void ReleaseFusionGemScrollView()
        {
            if (mFusionGemListScrollView != null)
            {
                mFusionGemListScrollView.ReleaseScrollView();
            }
        }

        private void BindEquipScrollView()
        {
            if (mOldPanel?.EquipCardScrollView == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] EquipCardScrollView 未绑定");
                return;
            }

            mEquipListScrollView = mOldPanel.EquipCardScrollView.GetComponent<UIVerticalScrollViewLock>();
            if (mEquipListScrollView == null)
            {
                mEquipListScrollView = mOldPanel.EquipCardScrollView.GetComponent<UIScrollView>();
            }

            if (mEquipListScrollView == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] EquipCardScrollView 缺少 UIScrollView 组件（UIVerticalScrollViewLock）");
                return;
            }
            
            // mEquipListScrollView.SetPrefabFactory(CreateMaterialItemNode);
            // mEquipListScrollView.SetReleaseAction(ReleaseMaterialItemNode);
            mEquipListScrollView.SetUpdateFunc(OnUpdateEquipItem);
            mEquipListScrollView.SetUpdateSelectFunc(SelectEquipItem);
        }

        private void ReleaseEquipScrollView()
        {
            if (mEquipListScrollView != null)
            {
                mEquipListScrollView.ReleaseScrollView();
            }
        }

        private void ShowEquipList()
        {
            if (mEquipListScrollView == null)
            {
                return;
            }

            var list = VM?.EquipDisplayed?.Value;
            var count = list != null ? list.Count : 0;
            if (VM != null)
            {
                VM.SelectedEquipIndex.Value = count > 0 ? 0 : -1;
            }

            mEquipListScrollView.ShowList(0);
            mEquipListScrollView.ShowList(count);
            mEquipListScrollView.ScrollToIndex(0, ScrollAlignment.Start, false);
            mEquipListScrollView.RefreshSelect();

            // Bind 时新建 Bag 节点 count 默认为 0，必须立刻 Evaluate，否则首次打开箭头不亮
            if (_redDotScope.IsOpen)
            {
                _redDotScope.MarkAllDirty();
                RedDotService.Instance.FlushNow();
            }
        }

        private void RefreshSpeakerWearEquipSlots()
        {
            if (mOldPanel?.WearEquips == null || VM == null)
            {
                return;
            }

            CollectEquipSlotDisplays();

            var slots = VM.SpeakerWearPartSlots.Value;
            var wearerId = VM.GetSpeakerEquipWearerId();
            bool registeredEquipPhaseStep2 = false;

            for (int i = 0; i < mEquipSlotDisplays.Length; i++)
            {
                var display = mEquipSlotDisplays[i];
                display.SetPartType((EquipPartType)i);
                var equip = slots != null && i < slots.Count ? slots[i] : null;
                if (equip != null)
                {
                    var equipOnlyId = equip.Id;
                    display.SetEquip(equip, wearerId, false, () =>
                    {
                        UnlockGuideManager.Send(ModuleFunctionType.EquipPhase, 2);
                        GlobalFunc.OpenEquipDetailsPanel(equipOnlyId, wearerId);
                    });

                    if (!registeredEquipPhaseStep2 && UnlockGuideManager.Instance != null)
                    {
                        UnlockGuideManager.Instance.SetUnlockTypeStep(
                            ModuleFunctionType.EquipPhase,
                            2,
                            display.GetComponent<RectTransform>());
                        registeredEquipPhaseStep2 = true;
                    }
                }
                else
                {
                    display.SetEquip(null, wearerId);
                }

                ClearWearEquipSlotArrow(display.transform);
            }

            if (!registeredEquipPhaseStep2
                && UnlockGuideManager.Instance != null
                && UnlockGuideManager.Instance.CheckUnlockGuideRunning(ModuleFunctionType.EquipPhase))
            {
                UnlockGuideManager.Instance.SetErrorTypeStep(ModuleFunctionType.EquipPhase, 2);
            }

            RefreshWearEquipSlotRedDots();
        }

        /// <summary>
        /// WearEquips 已穿戴槽不显示可替换箭头（仅背包装备列表保留箭头）。
        /// </summary>
        void ClearWearEquipSlotArrow(Transform slotRoot)
        {
            if (slotRoot == null)
            {
                return;
            }

            LocalRedDotView view = slotRoot.GetComponent<LocalRedDotView>();
            if (view != null)
            {
                Destroy(view);
            }

            for (int i = slotRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = slotRoot.GetChild(i);
                string childName = child.name;
                if (childName == RedDotDisplay.ArrowPointPrefab
                    || childName == RedDotDisplay.ArrowPointPrefab + "(Clone)"
                    || childName.StartsWith(RedDotDisplay.ArrowPointPrefab))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        void BindBagEquipRedDot(Transform itemRoot, int equipId)
        {
            if (itemRoot == null)
            {
                return;
            }

            // item 在滚动列表里复用，先清掉旧 LocalRedDotView 再重新绑定当前装备 id
            if (!itemRoot.GetComponent<LocalRedDotView>())
            {
                itemRoot.gameObject.AddComponent<LocalRedDotView>();
            }
            itemRoot.BindLocalRedDot(
                _redDotScope,
                SpeakWhellPanelRedDotScope.Paths.BagEquip(equipId),
                RedDotDisplay.ArrowPointPrefab);
            itemRoot.GetComponent<LocalRedDotView>()?.RefreshWatch();
        }

        void BindBagFusionGemRedDot(Transform itemRoot, int gemId)
        {
            if (itemRoot == null)
            {
                return;
            }

            if (!itemRoot.GetComponent<LocalRedDotView>())
            {
                itemRoot.gameObject.AddComponent<LocalRedDotView>();
            }

            itemRoot.BindLocalRedDot(
                _redDotScope,
                SpeakWhellPanelRedDotScope.Paths.BagFusionGem(gemId),
                RedDotDisplay.RedPointPrefab);
            itemRoot.GetComponent<LocalRedDotView>()?.RefreshWatch();
        }

        private void CollectEquipSlotDisplays()
        {
            if (mEquipSlotDisplays != null)
            {
                return;
            }

            var wearEquips = mOldPanel.WearEquips;
            var list = new List<EquipSlotDisplay>(wearEquips.childCount);
            for (int i = 0; i < wearEquips.childCount; i++)
            {
                var display = wearEquips.GetChild(i).GetComponent<EquipSlotDisplay>();
                if (display != null)
                {
                    list.Add(display);
                }
            }

            mEquipSlotDisplays = list.ToArray();
        }

        private void ShowFusionGemList()
        {
            if (mFusionGemListScrollView == null)
            {
                return;
            }
            UnlockGuideManager.Send(ModuleFunctionType.FusionGemSlot,2);
            var list = VM?.FusionGemDisplayed?.Value;
            var count = list != null ? list.Count : 0;
            if (VM != null) VM.SelectedFusionGemIndex.Value = count > 0 ? 0 : -1;
         
            mFusionGemListScrollView.ShowList(0);
            mFusionGemListScrollView.ShowList(count);
            mFusionGemListScrollView.ScrollToIndex(0, ScrollAlignment.Start, false);
            mFusionGemListScrollView.RefreshSelect();

            if (_redDotScope.IsOpen)
            {
                _redDotScope.MarkAllDirty();
                RedDotService.Instance.FlushNow();
            }
        }

        private void ShowMaterialList()
        {
            if (mMaterialListScrollView == null)
            {
                return;
            }

            var list = VM?.MaterialDisplayedCards?.Value;
            var count = list != null ? list.Count : 0;
            if (VM != null) VM.SelectedMaterialIndex.Value = count > 0 ? 0 : -1;

            // 先清空，避免首次打开时出现旧数据残留
            mMaterialListScrollView.ShowList(0);
            mMaterialListScrollView.ShowList(count);
            mMaterialListScrollView.ScrollToIndex(0, ScrollAlignment.Start, false);
            mMaterialListScrollView.RefreshSelect();
        }

        private void SelectMaterialItem(int dataIndex, ScrollItem scrollItem)
        {
            if (VM != null)
            {
                VM.SelectedMaterialIndex.Value = dataIndex;
            }
            var bagScrollItem = scrollItem as BagScrollItem;
            var item = bagScrollItem != null ? bagScrollItem.TweenTarget : null;
            if (item == null) return;

            var frame = item.transform.Find("Frame");
            if (frame != null)
            {
                // frame.gameObject.SetActive(VM != null && VM.SelectedMaterialIndex.Value == dataIndex);
            }
        }

        private GameObject CreateMaterialItemNode(RectTransform parent)
        {
            var itemPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("BackPackItem");
            var item = itemPool?.Get(parent)?.GetComponent<ScrollItem>();
            if (item == null)
            {
                return null;
            }

            item.OnSpawn();
            return item.gameObject;
        }

        private void ReleaseMaterialItemNode(GameObject obj)
        {
            if (obj == null) return;

            var rewardEntity = obj.transform.Find("backobj/RewardEntity");
            if (rewardEntity != null)
            {
                var rewardPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity");
                rewardPool?.Return(rewardEntity.gameObject);
            }

            obj.GetComponent<ScrollItem>()?.OnRelease();

            var itemPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("BackPackItem");
            itemPool?.Return(obj);
        }

        private GameObject CreateFusionGemItemNode(RectTransform parent)
        {
            // 复用背包Item样式：BackPackItem + backobj/RewardEntity
            var itemPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("BackPackItem");
            var item = itemPool?.Get(parent)?.GetComponent<ScrollItem>();
            if (item == null)
            {
                return null;
            }

            item.OnSpawn();
            return item.gameObject;
        }

        public void OpenAttrs()
        {
            var rows = new List<Game.Soldier.BaseAttrRowData>();

            // foreach (var attrType in sCoreAttrTypes)
            // {
            //     FormatAttrNameValue(attrType, out var name, out var value);
            //     rows.Add(new Game.Soldier.BaseAttrRowData
            //     {
            //         AttrName = name,
            //         AttrValue = value
            //     });
            // }

            var vm = new BaseAttrDetailViewModel();
            vm.OnCreate();
            vm.Rows.AddRange(rows);

            UIFramework.UIManager.OpenPanelAsync<BaseAttrDetailNew, BaseAttrDetailViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        private void OnUpdateMaterialItem(int dataIndex, ScrollItem scrollItem)
        {
            var list = VM?.MaterialDisplayedCards?.Value;
            if (list == null || dataIndex < 0 || dataIndex >= list.Count)
            {
                return;
            }

            var bagScrollItem = scrollItem as BagScrollItem;
            if (bagScrollItem == null) return;

            // MaterialScrollView 的 ItemPrefab 本身就是 RewardEntity（根节点上挂了 BagScrollItem），
            // 没有 backobj/RewardEntity 子节点，直接在节点本身上填充数据
            var rewardEntity = bagScrollItem.GetComponent<RewardEntity>();
            if (rewardEntity == null) return;

            var data = list[dataIndex];
            rewardEntity.SetData(
                data.cardid,
                data.cardnumber,
                RewardStyleType.Default,
                false);

            bagScrollItem.OnClick = () =>
            {
                var cfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(data.cardid);
                var entityData = new RewardEntityData()
                {
                    Num = 0,
                    ItemId = cfg.Id,
                    Icon = cfg.Icon,
                    Name = cfg.Name,
                    Desc = cfg.Desc,
                    Access = cfg.Access,
                    Quality = cfg.Quality,
                    StyleType = RewardStyleType.Default,
                    IsEffect = false,
                };
				ItemShowPanelViewModel vm = new ItemShowPanelViewModel();
				vm.OnCreate();
				vm.Data = entityData;
				UIManager.OpenPanelAsync<ItemShowPanelNew, ItemShowPanelViewModel>(vm, UILevel.PopUI).Forget();
                // 如需“点击即选中”，可在这里设置 VM.SelectedMaterialIndex 并 RefreshSelect()
            };
        }

        private void SelectFusionGemItem(int dataIndex, ScrollItem scrollItem)
        {
            if (VM != null)
            {
                VM.SelectedFusionGemIndex.Value = dataIndex;
            }
            var bagScrollItem = scrollItem as BagScrollItem;
            var item = bagScrollItem != null ? bagScrollItem.TweenTarget : null;
            if (item == null) return;

            var frame = item.transform.Find("Frame");
            if (frame != null)
            {
                // frame.gameObject.SetActive(VM != null && VM.SelectedFusionGemIndex.Value == dataIndex);
            }
        }

        private void OnUpdateFusionGemItem(int dataIndex, ScrollItem scrollItem)
        {
            var list = VM?.FusionGemDisplayed?.Value;
            if (list == null || dataIndex < 0 || dataIndex >= list.Count)
            {
                return;
            }

            var bagScrollItem = scrollItem as BagScrollItem;
            if (bagScrollItem == null) return;

            var item = bagScrollItem.TweenTarget;
            if (item == null) return;

            var gem = list[dataIndex];
            var cfg = gem?.Config;
            if (cfg == null) return;

            // RewardEntity 填充
            // var rewardEntity = item.transform.Find("backobj")?.Find("RewardEntity");
            // var rewardObj = rewardEntity != null
            //     ? rewardEntity.gameObject
            //     : CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity")?.Get();
            var equiped = GlobalFunc.IsFusionGemEquipped(gem);
            bagScrollItem.GetComponent<GemStoneEntity>().SetData(new GemStoneData()
            {
                GemId = gem.GemId,
                // Equip 判断基于 PlayerFusionGem.WearerId
                Equip = equiped,
                Level = gem.Level,
            });

            bagScrollItem.OnClick = () =>
            {
                
                UnlockGuideManager.Send(ModuleFunctionType.FusionGemSlot,3);
                
                var detailPanel = UIFramework.UIManager.GetPanel<FusionGemDetailPanelNew>();
                if (detailPanel != null)
                {
                    detailPanel.SetData(gem.Id);
                    return;
                }

                var vm = new FusionGemDetailPanelViewModel();
                vm.OnCreate();
                // 这里传入的是玩家融合石唯一Id（对应 Global.PlayerFusionGems 的 key：v.Id）
                vm.Initialize(gem.Id);
                UIFramework.UIManager
                    .OpenPanelAsync<FusionGemDetailPanelNew, FusionGemDetailPanelViewModel>(vm, UIFramework.UILevel.PopUI)
                    .Forget();
            };
            if (dataIndex == 0)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.FusionGemSlot,3,bagScrollItem.GetComponent<RectTransform>());
            }

            BindBagFusionGemRedDot(bagScrollItem.transform, gem.Id);
        }

        private void SelectEquipItem(int dataIndex, ScrollItem scrollItem)
        {
            if (VM != null)
            {
                VM.SelectedEquipIndex.Value = dataIndex;
            }
        }

        private void OnUpdateEquipItem(int dataIndex, ScrollItem scrollItem)
        {
            var list = VM?.EquipDisplayed?.Value;
            if (list == null || dataIndex < 0 || dataIndex >= list.Count)
            {
                return;
            }

            var bagScrollItem = scrollItem as BagScrollItem;
            if (bagScrollItem == null)
            {
                return;
            }

            var item = bagScrollItem.TweenTarget;
            if (item == null)
            {
                return;
            }

            var equip = list[dataIndex];
            // 已穿戴的装备也会复用 cell：仍要刷新箭头（Evaluate 对 WearerId!=0 返回 0，避免残留）
            if (equip.WearerId != 0)
            {
                BindBagEquipRedDot(bagScrollItem.transform, equip.Id);
                return;
            }

            var equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null)
            {
                return;
            }

            var itemCfg = GameServiceManager.Get<EquipService>().GetSoldierEquipItemByEquipId(equip.EquipId);
            if (itemCfg == null)
            {
                LogTool.LogError("[SpeakWhellPanelNew] 找不到装备对应道具，equipId=" + equip.EquipId);
                return;
            }

            var rewardEntity = item.transform.Find("backobj")?.Find("RewardEntity");
            var rewardObj = rewardEntity != null
                ? rewardEntity.gameObject
                : CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity")?.Get();

            if (rewardObj == null)
            {
                return;
            }

            rewardObj.name = "RewardEntity";
            rewardObj.SetActive(true);
            var backObj = item.transform.Find("backobj");
            if (backObj != null)
            {
                rewardObj.transform.SetParent(backObj);
                rewardObj.transform.localScale = Vector3.one;
                rewardObj.transform.localPosition = Vector3.zero;
            }

            rewardObj.GetComponent<RewardEntity>().SetData(new RewardEntityData
            {
                ItemId = itemCfg.Id,
                EquipLevel = equipCfg.EquipLevel,
                Quality = equip.Quality,
                IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
                StyleType = RewardStyleType.Equip,
                IsEffect = false,
            });

            var wearerId = VM != null ? VM.GetSpeakerEquipWearerId() : DataConfigManager.Tables.TbPlayerConst.GlobalUserWearId;
            var equipOnlyId = equip.Id;
            bagScrollItem.OnClick = () =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.Equip,2);
                GlobalFunc.OpenEquipDetailsPanel(equipOnlyId, wearerId);
            };
            if (dataIndex == 0 && UnlockGuideManager.Instance != null)
            {
                UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Equip,2,bagScrollItem.GetComponent<RectTransform>());
            }

            // 背包 item：该装备比当前穿戴的同部位装备战力更高时亮局部红点
            // 绑到 BackPackItem(Clone) 根节点（bagScrollItem.transform，150×199 正常尺寸）。
            // 不能挂 TweenTarget（尺寸 0）或 backobj（会被 RewardEntity 反复 SetParent 干扰）。
            BindBagEquipRedDot(bagScrollItem.transform, equip.Id);
        }
    }
}
