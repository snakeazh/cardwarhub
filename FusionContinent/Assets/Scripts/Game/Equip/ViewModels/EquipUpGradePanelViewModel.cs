using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.Equip.RedDot;
using Game.RedDot;
using Game.UserTool;
using Newtonsoft.Json;
using UIFramework.Services;

namespace Game.Equip
{
    /// <summary>
    /// 装备升级/洗炼界面 ViewModel
    /// </summary>
    public class EquipUpGradePanelViewModel : ViewModel
    {
        #region 属性
        // 当前选中的士兵和装备
        public BindableProperty<int> SoldierId { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> SelectedEquipId { get; } = new BindableProperty<int>(0);
        
        // 已佩戴装备列表
        public BindableProperty<List<PlayerEquip>> WearEquipList { get; } = new BindableProperty<List<PlayerEquip>>();
        
        // 当前选中的装备数据
        public BindableProperty<PlayerEquip> CurrentEquip { get; } = new BindableProperty<PlayerEquip>();
        
        // 页面状态
        public const int ForgePageIndex = 0;
        public const int EntryPageIndex = 1;
        public BindableProperty<int> CurrentPage { get; } = new BindableProperty<int>(ForgePageIndex);
        
        // 锻造相关
        public BindableProperty<int> CurrentPhase { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> NextPhase { get; } = new BindableProperty<int>(0);
        public BindableProperty<bool> IsMaxPhase { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> CanUpPhase { get; } = new BindableProperty<bool>(false);
        public BindableProperty<string> UpPhaseButtonText { get; } = new BindableProperty<string>("锻造");
        public BindableProperty<string> QualityTipText { get; } = new BindableProperty<string>("");
        
        // 洗炼相关（锁定的特效词条 id，对应 RefreshEntryEquipRequest.locked_effects）
        public BindableProperty<List<int>> SelectedLockedEffectIds { get; } = new BindableProperty<List<int>>();
        public BindableProperty<bool> CanRefreshEntry { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> CanUseRefreshEntry { get; } = new BindableProperty<bool>(false);
        
        // 装备名称
        public BindableProperty<string> EquipName { get; } = new BindableProperty<string>("");
        
        // 是否从世界界面打开
        public BindableProperty<bool> IsFromWorldPanel { get; } = new BindableProperty<bool>(false);

        // 异步操作错误（View 绑定后展示 Toast）
        public BindableProperty<string> OperationError { get; } = new BindableProperty<string>(string.Empty);
        #endregion

        public override UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(Config.PageType.PlayerPage);
        }

        public const string NoWearEquipMessage = "角色未佩戴装备";
        public const string CannotRefreshEntryMessage = "当前装备无法进行洗练";

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnForgePageCommand { get; private set; }
        public Command OnEntryPageCommand { get; private set; }
        public Command OnUpPhaseCommand { get; private set; }
        public Command OnRefreshEntryCommand { get; private set; }
        public Command OnUseRefreshEntryCommand { get; private set; }
        public Command<int> OnSelectEquipCommand { get; private set; }
        #endregion

        #region 依赖
        private EquipService mEquipService;
        #endregion

        #region 配置数据
        private TbEquipConfig mEquipCardConfig;
        private TbGameBattleConst mGameBattleConst;
        #endregion

        #region 状态保护
        private bool _isSetDataRunning = false;
        private bool _suppressRedDotCascade;
        #endregion

        #region 红点

        readonly EquipUpGradePanelRedDotScope _redDotScope = new EquipUpGradePanelRedDotScope();
        System.Action<object> _redChangeHandler;
        bool _redDotActive;

        public EquipUpGradePanelRedDotScope RedDotScope => _redDotScope;

        public void ActivateRedDot()
        {
            if (_redDotActive)
            {
                return;
            }

            _redDotActive = true;
            _redChangeHandler = OnRedDotExternalChange;
            EquipRedDotRefreshEvents.Subscribe(_redChangeHandler);

            WearEquipList.OnValueChanged += OnWearEquipListRedDotChanged;
            CurrentPage.OnValueChanged += OnCurrentPageRedDotChanged;
            SelectedEquipId.OnValueChanged += OnSelectedEquipRedDotChanged;
            SelectedLockedEffectIds.OnValueChanged += OnLockedEffectsRedDotChanged;

            SyncRedDotState();
        }

        public void DeactivateRedDot()
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

            WearEquipList.OnValueChanged -= OnWearEquipListRedDotChanged;
            CurrentPage.OnValueChanged -= OnCurrentPageRedDotChanged;
            SelectedEquipId.OnValueChanged -= OnSelectedEquipRedDotChanged;
            SelectedLockedEffectIds.OnValueChanged -= OnLockedEffectsRedDotChanged;

            _redDotScope.Close();
        }

        public void RefreshLocalRedDots()
        {
            if (!_redDotScope.IsOpen)
            {
                return;
            }

            _redDotScope.MarkAllDirty();
            RedDotService.Instance.FlushNow();
        }

        void SyncRedDotState()
        {
            var equipList = WearEquipList.Value;
            if (equipList == null || equipList.Count == 0)
            {
                if (_redDotScope.IsOpen)
                {
                    _redDotScope.Close();
                }

                return;
            }

            _redDotScope.Sync(SoldierId.Value, equipList, CurrentPage.Value);
            SyncRedDotRefineContext();
        }

        void SyncRedDotRefineContext()
        {
            int selectedEquipId = SelectedEquipId.Value;
            int lockedCount = SelectedLockedEffectIds.Value?.Count ?? 0;
            _redDotScope.SetRefineContext(selectedEquipId, lockedCount);
        }

        void RefreshAllRedDots()
        {
            PublishGlobalRedDotRefresh();
            RefreshLocalRedDots();
        }

        void PublishGlobalRedDotRefresh()
        {
            EquipGlobalRedDotRefresh.PublishAll();
            RedDotService.Instance.FlushNow();
        }

        void OnRedDotExternalChange(object _)
        {
            if (_suppressRedDotCascade)
            {
                return;
            }

            RefreshAllRedDots();
        }

        void OnWearEquipListRedDotChanged(List<PlayerEquip> oldVal, List<PlayerEquip> newVal)
        {
            SyncRedDotState();
            if (!_suppressRedDotCascade)
            {
                RefreshAllRedDots();
            }
        }

        void OnCurrentPageRedDotChanged(int oldPage, int newPage)
        {
            _redDotScope.SetCurrentPage(newPage);
            if (!_suppressRedDotCascade)
            {
                RefreshLocalRedDots();
            }
        }

        void OnSelectedEquipRedDotChanged(int oldVal, int newVal)
        {
            SyncRedDotRefineContext();
            if (!_suppressRedDotCascade)
            {
                RefreshAllRedDots();
            }
        }

        void OnLockedEffectsRedDotChanged(List<int> oldVal, List<int> newVal)
        {
            SyncRedDotRefineContext();
            if (!_suppressRedDotCascade)
            {
                RefreshAllRedDots();
            }
        }

        void ApplyEquipMutation(System.Action mutate)
        {
            _suppressRedDotCascade = true;
            try
            {
                mutate();
            }
            finally
            {
                _suppressRedDotCascade = false;
                SyncRedDotState();
                RefreshAllRedDots();
            }
        }

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mEquipService = GameServiceManager.Get<EquipService>();
            
            // 加载配置
            mEquipCardConfig = DataConfigManager.Tables.TbEquipConfig;
            mGameBattleConst = DataConfigManager.Tables.TbGameBattleConst;
            
            // 初始化命令
            OnCloseCommand = new Command(OnClose);
            OnForgePageCommand = new Command(OnForgePage);
            OnEntryPageCommand = new Command(OnEntryPage);
            OnUpPhaseCommand = new Command(OnUpPhase, () => CanUpPhase.Value);
            OnRefreshEntryCommand = new Command(OnRefreshEntry, () => CanRefreshEntry.Value);
            OnUseRefreshEntryCommand = new Command(OnUseRefreshEntry, () => CanUseRefreshEntry.Value);
            OnSelectEquipCommand = new Command<int>(OnSelectEquip);
            
            SelectedLockedEffectIds.Value = new List<int>();
            SelectedLockedEffectIds.OnValueChanged += OnSelectedLockedEffectIdsChanged;
        }

        private void OnSelectedLockedEffectIdsChanged(List<int> oldVal, List<int> newVal)
        {
            UpdateCanRefreshEntry();
        }

        /// <summary>
        /// 初始化数据 - 从世界界面打开
        /// </summary>
        public void InitializeFromWorld()
        {
            IsFromWorldPanel.Value = true;
            OnShowSelectSoldier?.Invoke();
        }

        /// <summary>
        /// 初始化数据 - 直接指定士兵
        /// </summary>
        /// <returns>是否成功选中装备</returns>
        public bool Initialize(int soldierId, int equipId = 0)
        {
            if (_isSetDataRunning) return false;
            _isSetDataRunning = true;
            
            try
            {
                SoldierId.Value = soldierId;
                IsFromWorldPanel.Value = false;
                
                RefreshWearEquipList();
                
                if (WearEquipList.Value.Count == 0)
                {
                    OnCloseRequested?.Invoke();
                    return false;
                }
                
                var targetEquipId = equipId == 0 ? WearEquipList.Value.First().Id : equipId;
                SelectEquip(targetEquipId);
                return true;
            }
            finally
            {
                _isSetDataRunning = false;
            }
        }

        /// <summary>
        /// 士兵选择回调
        /// </summary>
        /// <returns>是否成功选中装备</returns>
        public bool OnSoldierSelected(int soldierId)
        {
            SoldierId.Value = soldierId;
            RefreshWearEquipList();
            
            if (WearEquipList.Value.Count == 0)
            {
                OnCloseRequested?.Invoke();
                return false;
            }
            
            SelectEquip(WearEquipList.Value.First().Id);
            OnDisableGameMainFootBtn?.Invoke(true);
            return true;
        }

        /// <summary>
        /// 刷新已佩戴装备列表
        /// </summary>
        private void RefreshWearEquipList()
        {
            var equips = new List<PlayerEquip>(Global.PlayerEquips.Values);
            var list = equips
                .Where(v => v.WearerId == SoldierId.Value)
                .ToList();
            WearEquipList.Value = list;
        }

        /// <summary>
        /// 选择装备
        /// </summary>
        private void OnSelectEquip(int equipId)
        {
            if (SelectedEquipId.Value == equipId) return;

            if (CurrentPage.Value == EntryPageIndex && !CanEquipRefreshEntry(equipId))
            {
                ShowOperationError(CannotRefreshEntryMessage);
                return;
            }

            SelectEquip(equipId);
        }

        private void SelectEquip(int equipId)
        {
            SelectedEquipId.Value = equipId;
            SelectedLockedEffectIds.Value = new List<int>();
            
            if (Global.PlayerEquips.TryGetValue(equipId, out var equip))
            {
                CurrentEquip.Value = equip;
                RefreshForgeData();
                RefreshEntryData();
            }
        }

        /// <summary>
        /// 刷新锻造数据（部位槽位等级）
        /// </summary>
        private void RefreshForgeData()
        {
            var equip = CurrentEquip.Value;
            if (equip == null) return;

            var equipCfg = mEquipCardConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null) return;

            var slot = GetSlotForEquip(equip);

            EquipName.Value = equipCfg.Name;
            CurrentPhase.Value = slot.Level;
            NextPhase.Value = slot.Level + 1;

            var levelCfg = slot.GetLevelCfg();

            IsMaxPhase.Value = levelCfg == null || levelCfg.UpPhaseConsumes.Count == 0;
            CanUpPhase.Value = !IsMaxPhase.Value && CheckConsumes(levelCfg.UpPhaseConsumes);
            UpPhaseButtonText.Value = "锻造";
            QualityTipText.Value = "";
        }

        private PlayerEquipSlot GetSlotForEquip(PlayerEquip equip)
        {
            return mEquipService.GetEquipSlot(equip.PartType);
        }

        /// <summary>
        /// 刷新洗炼相关状态
        /// </summary>
        private void RefreshEntryData()
        {
            UpdateCanRefreshEntry();
            UpdateCanUseRefreshEntry();
        }

        /// <summary>
        /// 洗炼按钮：仅根据 RefreshEntryConsume + LockEffectRefreshEntryConsume * 锁定数量 判断道具是否足够
        /// </summary>
        private void UpdateCanRefreshEntry()
        {
            var equip = CurrentEquip.Value;
            if (equip == null)
            {
                CanRefreshEntry.Value = false;
                OnRefreshEntryCommand?.RaiseCanExecuteChanged();
                return;
            }

            var equipCfg = mEquipCardConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null || equipCfg.EntryNum <= 0)
            {
                CanRefreshEntry.Value = false;
                OnRefreshEntryCommand?.RaiseCanExecuteChanged();
                return;
            }

            var lockedCount = SelectedLockedEffectIds.Value?.Count ?? 0;
            CanRefreshEntry.Value = CheckRefreshEntryConsumes(equipCfg, lockedCount);
            OnRefreshEntryCommand?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 替换按钮：是否存在待确认的洗炼预览词条
        /// </summary>
        private void UpdateCanUseRefreshEntry()
        {
            var equip = CurrentEquip.Value;
            CanUseRefreshEntry.Value = equip != null &&
                                        equip.TempEntrys != null &&
                                        equip.TempEntrys.Count > 0;
            OnUseRefreshEntryCommand?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// 设置特效词条锁定状态（对应 RefreshEntryEquipRequest.locked_effects）
        /// </summary>
        public void SetLockedEffect(int effectId, bool isLocked)
        {
            var lockedEffectIds = new List<int>(SelectedLockedEffectIds.Value ?? new List<int>());

            if (isLocked)
            {
                if (!lockedEffectIds.Contains(effectId))
                {
                    lockedEffectIds.Add(effectId);
                }
            }
            else
            {
                lockedEffectIds.Remove(effectId);
            }

            SelectedLockedEffectIds.Value = lockedEffectIds;
        }

        /// <summary>
        /// 打开锻造确认框前校验：刷新消耗后判断是否可锻造
        /// </summary>
        public bool TryValidateUpPhaseConsumes(out string message)
        {
            RefreshForgeData();
            if (CanUpPhase.Value)
            {
                message = null;
                return true;
            }

            message = IsMaxPhase.Value ? "已达最大锻造等级" : "道具不足";
            return false;
        }

        /// <summary>
        /// 洗炼前校验：刷新消耗后判断是否可洗炼
        /// </summary>
        public bool TryValidateRefreshEntryConsumes(out string message)
        {
            if (!CanEquipRefreshEntry())
            {
                message = CannotRefreshEntryMessage;
                return false;
            }

            UpdateCanRefreshEntry();
            if (CanRefreshEntry.Value)
            {
                message = null;
                return true;
            }

            message = "道具不足";
            return false;
        }

        /// <summary>
        /// 当前装备配置是否支持洗练（EntryNum &gt; 0）
        /// </summary>
        private bool CanEquipRefreshEntry(int equipId)
        {
            if (!Global.PlayerEquips.TryGetValue(equipId, out var equip))
                return false;

            var equipCfg = mEquipCardConfig.GetOrDefault(equip.EquipId);
            return equipCfg != null && equipCfg.EntryNum > 0;
        }

        private bool CanEquipRefreshEntry()
        {
            var equip = CurrentEquip.Value;
            return equip != null && CanEquipRefreshEntry(equip.Id);
        }

        private void ShowOperationError(string message)
        {
            OperationError.SetWithoutNotify(string.Empty);
            OperationError.Value = message;
        }

        /// <summary>
        /// 检查洗炼消耗：RefreshEntryConsume + LockEffectRefreshEntryConsume * lockedCount
        /// </summary>
        private bool CheckRefreshEntryConsumes(EquipConfig equipCfg, int lockedCount)
        {
            foreach (var consume in BuildRefreshEntryConsumes(equipCfg, lockedCount))
            {
                if (Global.GetItemNumById(consume.Id) < consume.Num)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 洗炼消耗 = RefreshEntryConsume + LockEffectRefreshEntryConsume * lockedCount
        /// </summary>
        private static List<RewardItem> BuildRefreshEntryConsumes(EquipConfig equipCfg, int lockedCount)
        {
            var consumeMap = new Dictionary<int, long>();

            foreach (var item in equipCfg.RefreshEntryConsume)
            {
                if (item.Num == 0) continue;
                consumeMap[item.Id] = consumeMap.GetValueOrDefault(item.Id) + item.Num;
            }

            if (lockedCount > 0)
            {
                foreach (var item in equipCfg.LockEffectRefreshEntryConsume)
                {
                    if (item.Num == 0) continue;
                    consumeMap[item.Id] = consumeMap.GetValueOrDefault(item.Id) + item.Num * lockedCount;
                }
            }

            var consumes = new List<RewardItem>(consumeMap.Count);
            foreach (var kvp in consumeMap)
            {
                consumes.Add(new RewardItem(kvp.Key, kvp.Value));
            }

            return consumes;
        }

        /// <summary>
        /// 检查消耗品是否足够
        /// </summary>
        private bool CheckConsumes(List<RewardAndNum> consumes, int redouble = 1)
        {
            foreach (var item in consumes)
            {
                if (item.Num == 0) continue;
                var itemNum = Global.GetItemNumById(item.Id);
                if (itemNum < item.Num * redouble) return false;
            }
            return true;
        }

        /// <summary>
        /// 检查装备部位槽位是否有红点（可升级）
        /// </summary>
        public bool CheckEquipRedPoint(PlayerEquip equip)
        {
            int lockedCount = equip.Id == SelectedEquipId.Value
                ? SelectedLockedEffectIds.Value?.Count ?? 0
                : 0;
            return EquipForgeRedDotHelper.CheckWearEquipSlotRedPoint(
                equip,
                EquipWearSlotRedDotContext.ByPage(
                    CurrentPage.Value, SelectedEquipId.Value, lockedCount));
        }

        /// <summary>
        /// 获取当前槽位等级属性
        /// </summary>
        public List<AttributeStruct> GetCurrentSlotAttributes()
        {
            var equip = CurrentEquip.Value;
            if (equip == null) return new List<AttributeStruct>();
            return GetSlotForEquip(equip).GetDisplayAttributes();
        }

        /// <summary>
        /// 获取下一级槽位等级属性
        /// </summary>
        public List<AttributeStruct> GetNextSlotAttributes()
        {
            var equip = CurrentEquip.Value;
            if (equip == null) return new List<AttributeStruct>();
            var slot = GetSlotForEquip(equip);
            return slot.GetDisplayAttributes(slot.Level + 1);
        }

        public SlotLevelListStruct GetActiveExtraLevelCfg()
        {
            var equip = CurrentEquip.Value;
            if (equip == null) return null;
            return GetSlotForEquip(equip).GetActiveExtraLevelCfg();
        }

        public SlotLevelListStruct GetNextExtraLevelCfg()
        {
            var equip = CurrentEquip.Value;
            if (equip == null) return null;
            return GetSlotForEquip(equip).GetNextExtraLevelCfg();
        }

        /// <summary>
        /// 获取消耗品列表
        /// </summary>
        public List<RewardAndNum> GetUpPhaseConsumes()
        {
            var equip = CurrentEquip.Value;
            if (equip == null) return new List<RewardAndNum>();
            var levelCfg = GetSlotForEquip(equip).GetLevelCfg();
            if (levelCfg == null) return new List<RewardAndNum>();
            return levelCfg.UpPhaseConsumes;
        }

        /// <summary>
        /// 获取洗炼消耗品列表
        /// </summary>
        public List<RewardItem> GetRefreshEntryConsumes()
        {
            var equip = CurrentEquip.Value;
            if (equip == null) return new List<RewardItem>();
            var equipCfg = mEquipCardConfig.GetOrDefault(equip.EquipId);
            if (equipCfg == null) return new List<RewardItem>();

            var lockedCount = SelectedLockedEffectIds.Value?.Count ?? 0;
            return BuildRefreshEntryConsumes(equipCfg, lockedCount);
        }

        /// <summary>
        /// 洗炼时出现特效的概率描述（来自 PlayerConst.ProbabilityOfEquipOccurrence）
        /// </summary>
        public string GetEffectProbabilityText()
        {
            var percent = (int)(DataConfigManager.Tables.TbPlayerConst.ProbabilityOfEquipOccurrence * 100f + 0.5f);
            return $"出现的特效概率为：{percent}%";
        }

        /// <summary>
        /// 获取词条品质类型列表
        /// </summary>
        public List<string> GetQualityTypeList()
        {
            return mGameBattleConst.QualityTypeList;
        }

        /// <summary>
        /// 切换页面
        /// </summary>
        public void ChangePage(int pageIndex)
        {
            if (pageIndex == EntryPageIndex && !CanEquipRefreshEntry())
            {
                ShowOperationError(CannotRefreshEntryMessage);
                return;
            }

            CurrentPage.Value = pageIndex;
            if (pageIndex == EntryPageIndex)
            {
                OnEntryPageOpened?.Invoke();
            }
        }

        private void OnForgePage()
        {
            CurrentPage.Value = ForgePageIndex;
        }

        private void OnEntryPage()
        {
            if (!CanEquipRefreshEntry())
            {
                ShowOperationError(CannotRefreshEntryMessage);
                return;
            }

            CurrentPage.Value = EntryPageIndex;
            OnEntryPageOpened?.Invoke();
        }

        /// <summary>
        /// 升级装备槽位
        /// </summary>
        private async void OnUpPhase()
        {
            if (CurrentEquip.Value == null) return;

            try
            {
                OnUpPhaseStarted?.Invoke();

                var partType = CurrentEquip.Value.PartType;
                var result = await mEquipService.UpgradeEquipSlotAsync(partType);

                if (result != null)
                {
                    ApplyEquipMutation(() =>
                    {
                        Global.PlayerEquipSlots[partType] = result.EquipSlot;
                        Items.Update(result.Items);
                        RefreshWearEquipList();
                        SelectEquip(SelectedEquipId.Value);
                        OnUpPhaseSuccess?.Invoke();
                    });
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipUpGradePanelViewModel] 槽位升级失败: {ex.Message}");
                OperationError.Value = $"操作失败：{ex.Message}";
            }
            finally
            {
                OnUpPhaseEnded?.Invoke();
            }
        }

        /// <summary>
        /// 洗炼词条
        /// </summary>
        private async void OnRefreshEntry()
        {
            if (CurrentEquip.Value == null) return;
            
            try
            {
                OnRefreshEntryStarted?.Invoke();
                var lockedEffects = new List<int>(SelectedLockedEffectIds.Value ?? new List<int>());

                var result = await mEquipService.RefreshEntryAsync(SelectedEquipId.Value, lockedEffects);

                if (result != null && result.Equip != null)
                {
                    ApplyEquipMutation(() =>
                    {
                        Global.PlayerEquips[result.Equip.Id] = new PlayerEquip(result.Equip);
                        Items.Update(result.Items);
                        RefreshWearEquipList();
                        SelectEquip(SelectedEquipId.Value);
                        OnRefreshEntrySuccess?.Invoke();
                    });
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipUpGradePanelViewModel] 词条洗炼失败: {ex.Message}");
                OperationError.Value = $"操作失败：{ex.Message}";
            }
            finally
            {
                OnRefreshEntryEnded?.Invoke();
            }
        }

        /// <summary>
        /// 应用洗炼结果（将 TempEntrys 替换为正式词条）
        /// </summary>
        private async void OnUseRefreshEntry()
        {
            var equip = CurrentEquip.Value;
            if (equip == null || equip.TempEntrys == null || equip.TempEntrys.Count == 0) return;

            try
            {
                OnUseRefreshEntryStarted?.Invoke();

                var result = await mEquipService.UseRefreshEntryAsync(SelectedEquipId.Value);
                if (result?.Equip != null)
                {
                    ApplyEquipMutation(() =>
                    {
                        Global.PlayerEquips[result.Equip.Id] = new PlayerEquip(result.Equip);
                        RefreshWearEquipList();
                        SelectEquip(SelectedEquipId.Value);
                        OnUseRefreshEntrySuccess?.Invoke();
                    });
                }
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"[EquipUpGradePanelViewModel] 应用洗炼词条失败: {ex.Message}");
                OperationError.Value = $"操作失败：{ex.Message}";
            }
            finally
            {
                OnUseRefreshEntryEnded?.Invoke();
            }
        }

        private void OnClose()
        {
            if (IsFromWorldPanel.Value)
            {
                OnEnableGameMainFootBtn?.Invoke(true);
            }
            OnCloseRequested?.Invoke();
        }

        /// <summary>
        /// 重置世界界面标志（OnShow时调用）
        /// </summary>
        public void ResetWorldPanelFlag()
        {
            IsFromWorldPanel.Value = false;
        }

        #region 事件
        public System.Action OnCloseRequested;

        public System.Action OnShowSelectSoldier;
        public System.Action<bool> OnDisableGameMainFootBtn;
        public System.Action<bool> OnEnableGameMainFootBtn;
        public System.Action OnEntryPageOpened;
        
        // 锻造事件
        public System.Action OnUpPhaseStarted;
        public System.Action OnUpPhaseEnded;
        public System.Action OnUpPhaseSuccess;
        
        // 洗炼事件
        public System.Action OnRefreshEntryStarted;
        public System.Action OnRefreshEntryEnded;
        public System.Action OnRefreshEntrySuccess;

        // 应用洗炼结果
        public System.Action OnUseRefreshEntryStarted;
        public System.Action OnUseRefreshEntryEnded;
        public System.Action OnUseRefreshEntrySuccess;
        #endregion
    }
}
