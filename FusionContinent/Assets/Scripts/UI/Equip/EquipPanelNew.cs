using Game.Equip;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using QFramework;
using UIFramework;
using Config;
using FusionContinent.Model;
using FusionContinent;
using FusionContinent.Helper;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using FusionContinent.Animation;

namespace FusionContinent
{
    /// <summary>
    /// 装备界面 - 新框架版本 (MVVM)
    /// 复用原 EquipPanel 的 UI 组件
    /// </summary>
    [UIFramework.PrefabPath("EquipPanel")]
    public class EquipPanelNew : UIFramework.UIPanel<EquipPanelViewModel>
    {
        public override PanelStackMode StackMode
        {
            get => PanelStackMode.Unmanaged;
        }

        // 引用原 Panel，复用其 UI 组件
        private EquipPanel mOldPanel;
        
        #region 动画相关组件引用

        private RectTransform mCloseAnimation;
        private RectTransform mOpenAnimation;

        #endregion

        #region 事件处理器引用（用于解绑）

        private UnityAction mExitBtnHandler;
        private UIPoolScope UIPoolScope;
        private UnityAction<int> mEquipFilterHandler;
        #endregion

        void Awake()
        {
            // 获取原 Panel 引用
            mOldPanel = GetComponent<EquipPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[EquipPanelNew] 找不到 EquipPanel 组件！");
                return;
            }

            UIPoolScope = mOldPanel.GetComponent<UIPoolScope>();
            // 获取动画组件引用
            mCloseAnimation = mOldPanel.CloseAnimation;
            mOpenAnimation = mOldPanel.OpenAnimation;

            LogTool.Log("[EquipPanelNew] 已关联 EquipPanel");
        }

        protected override void OnBindViewModel(EquipPanelViewModel vm)
        {
            LogTool.Log("[EquipPanelNew] OnBindViewModel 开始绑定");
            if (mOldPanel == null)
            {
                LogTool.LogError("[EquipPanelNew] mOldPanel 为空！");
                return;
            }

            // 绑定属性
            BindProperties(vm);
            LogTool.Log("[EquipPanelNew] 属性绑定完成");

            // 初始化品质筛选下拉框
            InitEquipFilter(vm);
            LogTool.Log("[EquipPanelNew] 品质筛选初始化完成");

            // 绑定 ViewModel 事件
            vm.OnCloseRequested += OnCloseRequested;
            vm.OnUpgradeRequested += OnUpgradeRequested;
            vm.OnOpenEquipFragmentRequested += OnOpenEquipFragmentRequested;
            LogTool.Log("[EquipPanelNew] 事件绑定完成");

            // 初始化按钮
            InitButtons(vm);
            LogTool.Log("[EquipPanelNew] 按钮初始化完成");
        }

        protected override void OnUnbindViewModel(EquipPanelViewModel vm)
        {
            // 取消事件订阅
            vm.OnCloseRequested -= OnCloseRequested;
            vm.OnUpgradeRequested -= OnUpgradeRequested;
            vm.OnOpenEquipFragmentRequested -= OnOpenEquipFragmentRequested;

            // 移除按钮事件监听
            if (mOldPanel != null)
            {
                if (mExitBtnHandler != null)
                {
                    if (mOldPanel.ExitBg != null)
                        mOldPanel.ExitBg.onClick.RemoveListener(mExitBtnHandler);
                    if (mOldPanel.ExitBtn != null)
                        mOldPanel.ExitBtn.onClick.RemoveListener(mExitBtnHandler);
                    mExitBtnHandler = null;
                }

                // 移除品质筛选下拉框监听
                if (mEquipFilterHandler != null && mOldPanel.EquipFilter != null)
                {
                    mOldPanel.EquipFilter.onValueChanged.RemoveListener(mEquipFilterHandler);
                    mEquipFilterHandler = null;
                }
            }
        }

        #region 属性绑定

        private void BindProperties(EquipPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 士兵名称 - ManType 组件已移除
            // vm.SoldierName.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.ManType != null)
            //     {
            //         var nameText = mOldPanel.ManType.transform.Find("SoldierName")?.GetComponent<TMP_Text>();
            //         if (nameText != null) nameText.text = newVal;
            //     }
            // };

            // 士兵类型名称 - ManType 组件已移除
            // vm.SoldierTypeName.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.ManType != null)
            //     {
            //         var typeText = mOldPanel.ManType.transform.Find("NameText")?.GetComponent<TMP_Text>();
            //         if (typeText != null) typeText.text = newVal;
            //     }
            // };

            // 士兵类型图标 - ManType 组件已移除
            // vm.SoldierTypeIcon.OnValueChanged += (oldVal, newVal) =>
            // {
            //     if (mOldPanel.ManType != null)
            //     {
            //         var icon = mOldPanel.ManType.transform.Find("Icon")?.GetComponent<Image>();
            //         if (icon != null) icon.sprite = newVal;
            //     }
            // };

            // 战力 - Fight 组件已移除，跳过战力绑定
            // vm.SoldierFightingNum.OnValueChanged += (oldVal, newVal) =>
            // {
            //     SetFightText(newVal);
            // };

            // 已佩戴装备列表变化
            vm.WearEquipList.OnValueChanged += (oldVal, newVal) =>
            {
                InitWearEquipSlot(vm);
            };

            // 装备列表变化
            vm.EquipList.OnValueChanged += (oldVal, newVal) =>
            {
                InitEquipScroll(vm);
            };
        }

        #endregion

        #region 初始化按钮

        private void InitButtons(EquipPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            // 返回按钮 - 添加关闭动画
            mExitBtnHandler = () =>
            {
                vm.OnCloseCommand.Execute();
                // 播放关闭动画
                var closeNode = GetAnimationCloseNode;
                if (closeNode != null)
                {
                    RunAnimation(closeNode, () => CloseSelf());
                }
            };

            if (mOldPanel.ExitBg != null)
            {
                mOldPanel.ExitBg.onClick.RemoveListener(mExitBtnHandler);
                mOldPanel.ExitBg.onClick.AddListener(mExitBtnHandler);
            }

            if (mOldPanel.ExitBtn != null)
            {
                mOldPanel.ExitBtn.onClick.RemoveListener(mExitBtnHandler);
                mOldPanel.ExitBtn.onClick.AddListener(mExitBtnHandler);
            }

            // 升级按钮 - 已移除 UpGradeBtn 组件，相关逻辑在 EquipPanelNew 中处理
            // if (mOldPanel.UpGradeBtn != null)
            // {
            //     mOldPanel.UpGradeBtn.onClick.RemoveAllListeners();
            //     mOldPanel.UpGradeBtn.onClick.AddListener(() => vm.OnUpgradeCommand.Execute());
            // }
        }

        #endregion

        #region UI 设置

        /// <summary>
        /// 初始化装备品质筛选下拉框
        /// </summary>
        private void InitEquipFilter(EquipPanelViewModel vm)
        {
            if (mOldPanel?.EquipFilter == null)
            {
                LogTool.LogWarning("[EquipPanelNew] EquipFilter 为空，跳过品质筛选初始化");
                return;
            }

            var dropdown = mOldPanel.EquipFilter;
            
            // 构建选项列表（第一个选项为"全部"，对应 QualityType.Default）
            // 其他选项按 QualityType 枚举顺序添加
            var options = new List<TMP_Dropdown.OptionData>();
            options.Add(new TMP_Dropdown.OptionData("全部"));
            
            // 从枚举中获取品质名称（跳过 Default）
            var qualityTypeNames = new[] { "普通", "稀有", "史诗", "传说", "终极", "神话" };
            for (int i = 0; i < qualityTypeNames.Length; i++)
            {
                options.Add(new TMP_Dropdown.OptionData(qualityTypeNames[i]));
            }
            
            dropdown.options = options;
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();

            // 绑定下拉框变化事件
            mEquipFilterHandler = (int index) =>
            {
                // 将索引转换为品质类型（0=Default全部，1+=具体品质）
                var qualityType = (QualityType)index;
                vm.OnFilterQualityCommand.Execute(qualityType);
            };
            
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(mEquipFilterHandler);
        }

        /// <summary>
        /// 设置战力文本（已停用，Fight 组件已移除）
        /// </summary>
        private void SetFightText(int fightingNum)
        {
            // Fight 组件已移除，跳过战力显示
            // if (mOldPanel?.Fight == null) return;
            // var fightText = mOldPanel.Fight.transform.Find("Text")?.GetComponent<TMP_Text>();
            // if (fightText != null)
            // {
            //     fightText.text = fightingNum.ToString();
            // }
        }

        /// <summary>
        /// 加载士兵 Spine（参考 SoldierPanelNew.SetSkin 实现）
        /// </summary>
        private void LoadSoldierSpine(int soldierId)
        {
            // if (mOldPanel?.Spine == null) return;

            var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(soldierId);
            if (soldierCfg == null) return;

            // 获取当前穿戴的皮肤
            var playerSoldier = Global.GetCardById(soldierId);
            int skinId = playerSoldier?.ObjData?.Skin ?? 0;

            var spineName = soldierCfg.Spine;

            // 如果有皮肤，使用皮肤的 Spine
            if (skinId != 0)
            {
                var skinCfg = DataConfigManager.Tables.TbSkinConfig.GetOrDefault(skinId);
                if (skinCfg != null && !string.IsNullOrEmpty(skinCfg.Spine))
                {
                    spineName = skinCfg.Spine;
                }
            }

            // 清除旧的 Spine
            // for (int i = 0; i < mOldPanel.Spine.childCount; i++)
            //     Destroy(mOldPanel.Spine.GetChild(i).gameObject);

            // 加载新的 Spine
            // if (!string.IsNullOrEmpty(spineName))
            // {
            //     var soliderSpine = Global.ResLoader.LoadSync<GameObject>(spineName).Instantiate().transform;
            //     soliderSpine.SetParent(mOldPanel.Spine);
            //     soliderSpine.localScale = Vector3.one;
            //     soliderSpine.localPosition = Vector3.zero;
            //     mOldPanel.Spine.localScale = soldierCfg.SpineScale > 0 ? Vector3.one * soldierCfg.SpineScale : Vector3.one;
            //     
            //     // 确保 Spine 对象激活
            //     soliderSpine.gameObject.SetActive(true);
            //     
            //     // 播放 Spine 动画
            //     var skeletonGraphic = soliderSpine.GetComponent<SkeletonGraphic>();
            //     if (skeletonGraphic != null)
            //     {
            //         // 初始化 SkeletonGraphic
            //         skeletonGraphic.Initialize(false);
            //         // 播放待机动画
            //         // skeletonGraphic.AnimationState.SetAnimation(0, "idle01", true);
            //         skeletonGraphic.LateUpdate();
            //     }
            // }
        }

        // 已佩戴部位槽位缓存（与原界面 _wearPartSlots 一致）
        private readonly Dictionary<EquipPartType, EquipCardSlotData> _wearPartSlots = new Dictionary<EquipPartType, EquipCardSlotData>();

        /// <summary>
        /// 初始化已佩戴装备槽位（与原界面 InitWearEquipSlot 逻辑一致）
        /// </summary>
        private void InitWearEquipSlot(EquipPanelViewModel vm)
        {
            if (mOldPanel?.WearEquips == null)
            {
                LogTool.LogWarning("[EquipPanelNew] WearEquips 为空，跳过已佩戴装备初始化");
                return;
            }

            LogTool.Log($"[EquipPanelNew] InitWearEquipSlot: count={vm.WearEquipList.Value.Count}");
            _wearPartSlots.Clear();
            var partNameList = DataConfigManager.Tables.TbGameBattleConst.EquipPartTypeList;
            var wearEquipList = vm.WearEquipList.Value;

            for (int i = 0; i < mOldPanel.WearEquips.childCount; i++)
            {
                var partType = (EquipPartType)i;
                var slotObj = mOldPanel.WearEquips.GetChild(i).gameObject;
                
                // 设置部位名称
                var partText = slotObj.transform.Find("Text")?.GetComponent<TMP_Text>();
                if (partText != null) partText.text = partNameList[i];

                // 隐藏默认装备图标
                var rewardEntity = slotObj.transform.Find("RewardEntity")?.gameObject;
                if (rewardEntity != null) rewardEntity.SetActive(false);

                // 创建槽位数据
                var slotData = new EquipCardSlotData()
                {
                    SlotObj = slotObj,
                    PartType = partType,
                };

                // 融合石卡槽
                var gemSlot = slotObj.transform.Find("GemSlot");
                if (gemSlot != null)
                {
                    slotData.LockFusionGem = !ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot, false);
                    var lockObj = gemSlot.Find("Lock");
                    if (lockObj != null) lockObj.gameObject.SetActive(slotData.LockFusionGem);
                }

                // 显示已佩戴装备
                var equip = wearEquipList.FirstOrDefault(v => v.PartType == partType);
                if (equip != null)
                {
                    slotData.EquipOnlyId = equip.Id;
                    var equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
                    
                    if (rewardEntity != null)
                    {
                        rewardEntity.SetActive(true);
                        var rewardEntityComponent = rewardEntity.GetComponent<RewardEntity>();
                        if (rewardEntityComponent != null)
                        {
                            rewardEntityComponent.SetData(new RewardEntityData()
                            {
                                EquipLevel = equipCfg != null ? equipCfg.EquipLevel : 0,
                                Quality = equip.Quality,
                                IconSprite = equipCfg != null ? Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon) : null,
                                StyleType = RewardStyleType.Equip,
                                IsEffect = false,
                                OnClick = () =>
                                {
                                    GlobalFunc.OpenEquipDetailsPanel(equip.Id, VM.SoldierId.Value);
                                }
                            });
                        }
                    }
                }
                
                _wearPartSlots.Add(partType, slotData);
            }
        }

 

        // 装备槽位缓存（与原界面一致）
        private readonly Dictionary<int, GameObject> _equipSlots = new Dictionary<int, GameObject>();

        /// <summary>
        /// 初始化装备列表（与原界面 InitEquipSlot 逻辑一致）
        /// </summary>
        private void InitEquipScroll(EquipPanelViewModel vm)
        {
            if (mOldPanel?.EquipCardScroll == null) return;

            var cardScroll = mOldPanel.EquipCardScroll.Find("Viewport/Content");
            if (cardScroll == null) return;

            // 隐藏所有子对象
            for (int i = 0; i < cardScroll.childCount; i++)
                cardScroll.GetChild(i).gameObject.SetActive(false);

            var equipList = vm.EquipList.Value;

            foreach (var equip in equipList)
            {
                var equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(equip.EquipId);
                if (equipCfg == null) continue;

                // 获取或创建槽位对象
                if (!_equipSlots.TryGetValue(equip.Id, out var slotObj))
                {
                    slotObj =  UIPoolScope.Spawn("RewardEntity",cardScroll);
                    // var rewardPool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity");
                    // var template = cardScroll.Find("RewardEntity");
                    _equipSlots.Add(equip.Id, slotObj);
                }

                // 显示/隐藏（已佩戴的装备不显示在列表中）
                slotObj.SetActive(equip.WearerId == 0);

                if (slotObj.activeSelf)
                {
                    // 设置装备数据
                    var rewardEntity = slotObj.GetComponent<RewardEntity>();
                    rewardEntity.SetData(new RewardEntityData()
                    {
                        EquipLevel = equipCfg.EquipLevel,
                        Quality = equip.Quality,
                        IconSprite = Global.EquipIconSpriteAtlas.GetSprite(equipCfg.Icon),
                        StyleType = RewardStyleType.Equip,
                        SoldierId = equip.WearerId,
                        IsEffect = false,
                        OnClick = () =>
                        {
                            GlobalFunc.OpenEquipDetailsPanel(equip.Id, VM.SoldierId.Value);
                        }
                    });
                }
            }
        }

        #endregion

        #region 事件处理

        private void OnCloseRequested()
        {
            // UIFramework.UIManager.ClosePanel(this);
        }

        private void OnUpgradeRequested(int soldierId)
        {
            UnlockGuideManager.Send(ModuleFunctionType.EquipPhase, 3);
            UnlockGuideManager.Send(ModuleFunctionType.EquipEntry, 3);
            
            if (!GameMainPanel.Default.CheckModuleFunction(ModuleFunctionType.EquipPhase)) return;
            
            // 使用新的 UIFramework 打开装备升级界面
            var vm = new Game.Equip.EquipUpGradePanelViewModel();
            vm.OnCreate();
            if (!vm.Initialize(soldierId))
            {
                Toast.Show(EquipUpGradePanelViewModel.NoWearEquipMessage);
                return;
            }
            
            UIFramework.UIManager.OpenPanelAsync<EquipUpGradePanelNew, Game.Equip.EquipUpGradePanelViewModel>(vm,UIFramework.UILevel.Panel);
        }

        private void OnOpenEquipFragmentRequested(int soldierId)
        {
            // 使用新的 UIFramework 打开装备分解界面
            var vm = new Game.Equip.EquipFragmentPanelViewModel();
            vm.OnCreate();
            vm.Initialize(soldierId);
            
            UIFramework.UIManager.OpenPanelAsync<EquipFragmentPanelNew, Game.Equip.EquipFragmentPanelViewModel>(vm,UIFramework.UILevel.PopUI);
        }

        /// <summary>
        /// 检查装备是否可以升级（用于红点显示）
        /// 装备使用 Phase（阶段）而不是 Level（等级）
        /// </summary>
        private bool CheckEquipCanUpgrade(PlayerEquip playerEquip)
        {
            if (playerEquip == null) return false;
            
            // 装备使用 Phase（阶段），最大阶段为 Config.PhaseList.Count
            // if (playerEquip.Phase >= playerEquip.Config.PhaseList.Count)
            //     return false;
            
            // 检查材料是否足够
            // 这里简化处理，实际应该检查升级所需材料
            // 返回 true 表示可以升级（显示红点）
            return true;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 设置数据（供外部调用）
        /// </summary>
        public void SetData(int soldierId)
        {
            if (VM != null)
            {
                VM.Initialize(soldierId);
                
                // 初始化后刷新 ManType 显示 - ManType 组件已移除
                // RefreshManTypeDisplay(VM);
            }
        }

        #endregion

        #region 生命周期

        protected override void OnOpen(UIFramework.IUIData uiData = null)
        {
            LogTool.Log($"[EquipPanelNew] OnOpen: VM={VM != null}, SoldierId={VM?.SoldierId.Value}");

            // 播放打开动画
            var openNode = GetAnimationOpenNode;
            if (openNode != null)
            {
                RunAnimation(openNode, null);
            }

            // 初始化显示
            if (VM != null && VM.SoldierId.Value > 0)
            {
                // 刷新数据
                VM.RefreshData();

                // 初始化列表显示
                InitWearEquipSlot(VM);
                InitEquipScroll(VM);
            }
        }

        public override void Show()
        {
            base.Show();

            // 播放打开动画
            var openNode = GetAnimationOpenNode;
            if (openNode != null)
            {
                RunAnimation(openNode, null);
            }
        }

        public override void Hide()
        {
            var closeNode = GetAnimationCloseNode;
            if (closeNode != null)
            {
                RunAnimation(closeNode, HidePanelInternal);
                return;
            }

            HidePanelInternal();
        }

        /// <summary>
        /// 刷新士兵信息显示（ManType节点）- ManType 组件已移除，此方法停用
        /// </summary>
        // private void RefreshManTypeDisplay(EquipPanelViewModel vm)
        // {
        //     if (mOldPanel?.ManType == null) return;
        //
        //     // 士兵名称
        //     var nameText = mOldPanel.ManType.transform.Find("SoldierName")?.GetComponent<TMP_Text>();
        //     if (nameText != null) nameText.text = vm.SoldierName.Value;
        //
        //     // 士兵类型名称
        //     var typeText = mOldPanel.ManType.transform.Find("NameText")?.GetComponent<TMP_Text>();
        //     if (typeText != null) typeText.text = vm.SoldierTypeName.Value;
        //
        //     // 士兵类型图标
        //     var icon = mOldPanel.ManType.transform.Find("Icon")?.GetComponent<Image>();
        //     if (icon != null) icon.sprite = vm.SoldierTypeIcon.Value;
        //
        //     // 战力 - Fight 组件已移除，跳过
        //     // SetFightText(vm.SoldierFightingNum.Value);
        // }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }

        #endregion

        #region 动画相关

        protected GameObject GetAnimationCloseNode
        {
            get => mCloseAnimation?.gameObject;
        }

        protected GameObject GetAnimationOpenNode
        {
            get => mOpenAnimation?.gameObject;
        }

        protected void RunAnimation(GameObject target, UnityAction complete)
        {
            if (target == null)
            {
                complete?.Invoke();
                return;
            }

            var animations = target.GetComponents<UIAnimation>();
            if (animations == null || animations.Length == 0)
            {
                complete?.Invoke();
                return;
            }

            int completedCount = 0;
            int totalCount = animations.Length;

            void OnComplete()
            {
                completedCount++;
                if (completedCount >= totalCount)
                {
                    complete?.Invoke();
                }
            }

            foreach (var anim in animations)
            {
                anim.Play(OnComplete);
            }
        }

        private void HidePanelInternal()
        {
            base.Hide();
        }

        #endregion
    }
}
