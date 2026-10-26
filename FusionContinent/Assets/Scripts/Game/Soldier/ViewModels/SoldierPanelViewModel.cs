using System;
using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using UIFramework.Services;
using Config;
using System.Linq;
using FusionContinent;
using FusionContinent.Model;
using Cysharp.Threading.Tasks;

namespace Game.Soldier
{
    /// <summary>
    /// 士兵/英灵界面 ViewModel
    /// </summary>
    public class SoldierPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> CurrentSoldierIdx { get; } = new BindableProperty<int>(0);
        // 供外部记录/恢复“当前要打开的界面值”（具体打开逻辑由调用方处理）
        public BindableProperty<int> CurrentOpenPanelValue { get; } = new BindableProperty<int>(-1);
        public int LastOpenPanelValue { get; private set; } = -1;
        public BindableProperty<PlayerCard> CurrentCard { get; } = new BindableProperty<PlayerCard>();
        public BindableProperty<List<PlayerCard>> SoldierList { get; } = new BindableProperty<List<PlayerCard>>();
        public BindableProperty<List<PlayerCard>> SkillList { get; } = new BindableProperty<List<PlayerCard>>();
        public BindableProperty<List<PlayerCard>> GeneralSkillList { get; } = new BindableProperty<List<PlayerCard>>();
        public BindableProperty<bool> CanChangeLeft { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> CanChangeRight { get; } = new BindableProperty<bool>(false);
        public BindableProperty<bool> IsUpgradeBtnInteractable { get; } = new BindableProperty<bool>(false);
        public BindableProperty<int> CurrentSkinId { get; } = new BindableProperty<int>(0);
        public BindableProperty<CardType> CurrentSkillFilter { get; } = new BindableProperty<CardType>(CardType.Skill);
        // [已注释] 消耗品多选功能：不再由玩家手动勾选，改为升级时默认消耗全部所需道具
        // public BindableProperty<Dictionary<int, int>> ConsumeItems { get; } = new BindableProperty<Dictionary<int, int>>();
        public BindableProperty<List<QualityType>> GeneralSkillQualityFilters { get; } = new BindableProperty<List<QualityType>>(new List<QualityType>());
        
        // 静态字段 - 所有士兵共用同一份消耗品勾选记录
        // 关闭界面（ViewModel销毁）后自动清空
        // [已注释] 消耗品多选功能：全局静态字典，所有士兵共用勾选状态
        // private static Dictionary<int, int> sGlobalConsumeItems = new Dictionary<int, int>();
        #endregion

        #region 命令
        public Command<int> OnChangeSoldierCommand { get; private set; }
        public Command<int> OnSelectSkinCommand { get; private set; }
        public Command OnUpgradeCommand { get; private set; }
        public Command OnAwakeningCommand { get; private set; }
        public Command OnDevelopCommand { get; private set; }
        public Command OnEquipCommand { get; private set; }
        public Command OnSkinCommand { get; private set; }
        public Command OnFusionGemCommand { get; private set; }
        public Command OnMainAttributesCommand { get; private set; }
        public Command OnBaseAttributesCommand { get; private set; }
        public Command<CardType> OnChangeSkillFilterCommand { get; private set; }
        // [已注释] 消耗品多选功能
        // public Command<int> OnToggleConsumeItemCommand { get; private set; }
        public Command<List<QualityType>> OnChangeGeneralSkillQualityFilterCommand { get; private set; }
        public Command<QualityType> OnToggleGeneralSkillQualityFilterCommand { get; private set; }
        #endregion

        #region 依赖
        private SoldierService mSoldierService;
        #endregion

        #region 配置数据
        private TbCardBaseConfig mCardBaseConfig;
        private TbCardDevelopConfig mCardDevelopConfig;
        private TbSoldierBattleConfig mSoldierBattleConfig;
        private TbSpecialEffectsConfig mSpecialEffectsConfig;
        private TbSkinConfig mSkinConfig;
        public Dictionary<int, List<PlayerCard>> mSoldierSkillList = new Dictionary<int, List<PlayerCard>>();
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mSoldierService = GameServiceManager.Get<SoldierService>();
            
            // 加载配置
            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            mCardDevelopConfig = DataConfigManager.Tables.TbCardDevelopConfig;
            mSoldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;
            mSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
            mSkinConfig = DataConfigManager.Tables.TbSkinConfig;
            
            // 初始化命令
            OnChangeSoldierCommand = new Command<int>(OnChangeSoldier);
            OnSelectSkinCommand = new Command<int>(OnSelectSkin);
            OnUpgradeCommand = new Command(OnUpgrade, CanUpgrade);
            OnAwakeningCommand = new Command(OnAwakening);
            OnDevelopCommand = new Command(OnDevelop);
            OnEquipCommand = new Command(OnEquip);
            OnSkinCommand = new Command(OnSkin);
            OnFusionGemCommand = new Command(OnFusionGem);
            OnMainAttributesCommand = new Command(OnMainAttributes);
            OnBaseAttributesCommand = new Command(OnBaseAttributes);
            OnChangeSkillFilterCommand = new Command<CardType>(OnChangeSkillFilter);
            // [已注释] 消耗品多选功能
            // OnToggleConsumeItemCommand = new Command<int>(OnToggleConsumeItem);
            OnChangeGeneralSkillQualityFilterCommand = new Command<List<QualityType>>(OnChangeGeneralSkillQualityFilter);
            OnToggleGeneralSkillQualityFilterCommand = new Command<QualityType>(OnToggleGeneralSkillQualityFilter);

            // 初始化数据
            InitSoldierList();
            // 使用静态字段作为所有士兵共用的勾选记录
            // [已注释] 消耗品多选功能：不再使用全局勾选记录
            // ConsumeItems.Value = sGlobalConsumeItems;
            GeneralSkillQualityFilters.Value = new List<QualityType>();
            
            // 订阅数据变化事件
            SubscribeDataChangeEvents();
            
            LogTool.Log("[SoldierPanelViewModel] 创建完成");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            // 取消订阅数据变化事件
            UnsubscribeDataChangeEvents();
            
            // 清空静态消耗品勾选记录（关闭界面后不保存）
            // [已注释] 消耗品多选功能
            // sGlobalConsumeItems.Clear();
            
            LogTool.Log("[SoldierPanelViewModel] 销毁");
        }

        #region 数据变化事件订阅

        // 全局事件处理器引用

        private Action<object> mCardSkinChangedHandler;
        private Action<object> mRefreshSkillListHandler;
        private Action<object> mRefreshArcaneListHandler;

        private void SubscribeDataChangeEvents()
        {
            // 订阅皮肤变更事件
            mCardSkinChangedHandler = OnCardSkinChangedHandler;
            Global.Publisher.Subscribe(MessagePublisher.CARD_SKIN_CHANGED, mCardSkinChangedHandler);
            
            // 订阅刷新技能列表事件
            mRefreshSkillListHandler = OnRefreshSkillListHandler;
            Global.Publisher.Subscribe(MessagePublisher.REFRESH_SOLDIER_SKILL_LIST, mRefreshSkillListHandler);
            
            // 订阅刷新奥术卡列表事件
            mRefreshArcaneListHandler = OnRefreshArcaneListHandler;
            Global.Publisher.Subscribe(MessagePublisher.REFRESH_SOLDIER_ARCANE_LIST, mRefreshArcaneListHandler);
        }

        private void UnsubscribeDataChangeEvents()
        {
            // 取消所有事件订阅
            
            if (mCardSkinChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CARD_SKIN_CHANGED, mCardSkinChangedHandler);
                mCardSkinChangedHandler = null;
            }
            
            if (mRefreshSkillListHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.REFRESH_SOLDIER_SKILL_LIST, mRefreshSkillListHandler);
                mRefreshSkillListHandler = null;
            }
            
            if (mRefreshArcaneListHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.REFRESH_SOLDIER_ARCANE_LIST, mRefreshArcaneListHandler);
                mRefreshArcaneListHandler = null;
            }
        }


        
        /// <summary>
        /// 皮肤变更事件处理器
        /// </summary>
        private void OnCardSkinChangedHandler(object data)
        {
            if (data is int skinId)
            {
                LogTool.Log($"[SoldierPanelViewModel] 收到皮肤变更事件: SkinId={skinId}");
                OnCardSkinChanged?.Invoke(skinId);
            }
        }
        
        /// <summary>
        /// 刷新技能列表事件处理器
        /// </summary>
        private void OnRefreshSkillListHandler(object data)
        {
            LogTool.Log("[SoldierPanelViewModel] 收到刷新技能列表事件");
            // 刷新技能列表数据
            RefreshSkillListData();
            // 通知界面刷新
            OnSkillDataChanged?.Invoke();
        }
        
        /// <summary>
        /// 刷新奥术卡列表事件处理器
        /// </summary>
        private void OnRefreshArcaneListHandler(object data)
        {
            LogTool.Log("[SoldierPanelViewModel] 收到刷新奥术卡列表事件");
            // 刷新技能列表数据（包含奥术卡）
            RefreshSkillListData();
            // 通知界面刷新
            OnSkillDataChanged?.Invoke();
        }

        /// <summary>
        /// 刷新士兵列表数据（从 Global.PlayerCards 重新获取）
        /// </summary>
        private void RefreshSoldierListData()
        {
            if (SoldierList.Value == null) return;
            
            var updatedList = new List<PlayerCard>();
            foreach (var soldier in SoldierList.Value)
            {
                // 从 Global.PlayerCards 获取最新数据
                if (Global.PlayerCards.TryGetValue(soldier.CardId, out var latestCard))
                {
                    updatedList.Add(latestCard);
                }
                else
                {
                    updatedList.Add(soldier);
                }
            }
            
            SoldierList.Value = updatedList;
            
            // 更新当前选中士兵
            if (CurrentSoldierIdx.Value >= 0 && CurrentSoldierIdx.Value < updatedList.Count)
            {
                CurrentCard.Value = updatedList[CurrentSoldierIdx.Value];
            }
        }

        /// <summary>
        /// 刷新技能列表数据（从 Global.PlayerCards 重新获取）
        /// </summary>
        private void RefreshSkillListData()
        {
            // 刷新 mSoldierSkillList 中的数据
            foreach (var kvp in mSoldierSkillList)
            {
                var soldierId = kvp.Key;
                var skillList = kvp.Value;
                
                for (int i = 0; i < skillList.Count; i++)
                {
                    var skill = skillList[i];
                    if (Global.PlayerCards.TryGetValue(skill.CardId, out var latestCard))
                    {
                        skillList[i] = latestCard;
                    }
                }
            }
            
            // 触发技能列表更新
            UpdateSkillList();
        }

        #endregion

        #region 初始化

        private void InitSoldierList()
        {
            var soldierList = new List<PlayerCard>();
            
            foreach (var cfg in mCardBaseConfig.DataList)
            {
                var cardInfo = Global.GetCardById(cfg.Id, new PlayerCard()
                {
                    Lv = 1,
                    IsHave = false,
                    Config = cfg,
                    CardId = cfg.Id,
                    ObjData = new CardDataStruct(),
                });

                switch (cfg.Type)
                {
                    case CardType.Soldier:
                        soldierList.Add(cardInfo);
                        break;

                    case CardType.Skill:
                    case CardType.SkillPassive:
                    case CardType.GeneralSkill:
                        var skillCfg = mSpecialEffectsConfig.GetOrDefault(cfg.Id);
                        if (skillCfg != null)
                        {
                            if (!mSoldierSkillList.ContainsKey(skillCfg.ActsOnCard))
                            {
                                mSoldierSkillList[skillCfg.ActsOnCard] = new List<PlayerCard>();
                            }
                            mSoldierSkillList[skillCfg.ActsOnCard].Add(cardInfo);
                        }
                        break;
                }
            }

            SoldierList.Value = soldierList;
            
            // 设置默认选中
            if (soldierList.Count > 0)
            {
                var firstHaveIdx = GetFirstHaveSoldierIdx(soldierList);
                CurrentSoldierIdx.Value = firstHaveIdx;
                CurrentCard.Value = soldierList[firstHaveIdx];
                UpdateCanChangeButtons();

                // 初始化升级按钮状态
                var developCfg = mCardDevelopConfig.GetOrDefault(CurrentCard.Value.CardId);
                IsUpgradeBtnInteractable.Value = CheckCanUpgrade(CurrentCard.Value, developCfg);

                LogTool.Log($"[SoldierPanelViewModel] InitSoldierList: 士兵总数={soldierList.Count}, 默认选中索引={firstHaveIdx}, 选中士兵ID={soldierList[firstHaveIdx].CardId}");
            }
            else
            {
                LogTool.LogWarning("[SoldierPanelViewModel] InitSoldierList: 没有士兵数据");
            }
        }

        private int GetFirstHaveSoldierIdx(List<PlayerCard> soldierList)
        {
            for (int i = 0; i < soldierList.Count; i++)
            {
                if (soldierList[i].IsHave)
                {
                    return i;
                }
            }
            return 0; // 如果没有拥有的士兵，默认返回第一个
        }

        #endregion

        #region 命令实现

        private void OnChangeSoldier(int direction)
        {
            if (SoldierList.Value == null || SoldierList.Value.Count == 0) return;

            int newIdx = CurrentSoldierIdx.Value;
            if (direction == 0) // 左
            {
                if (newIdx <= 0) return;
                newIdx--;
            }
            else // 右
            {
                if (newIdx + 1 >= SoldierList.Value.Count) return;
                newIdx++;
            }

            CurrentSoldierIdx.Value = newIdx;
            CurrentCard.Value = SoldierList.Value[newIdx];
            CurrentSkinId.Value = CurrentCard.Value.ObjData.Skin;
            // [已注释] 消耗品多选功能：不再依赖勾选状态判断升级按钮
            // 重新检查升级按钮状态（检查是否满级 + 道具是否足够）
            var developCfg = mCardDevelopConfig.GetOrDefault(CurrentCard.Value.CardId);
            bool canUpgrade = CheckCanUpgrade(CurrentCard.Value, developCfg);
            IsUpgradeBtnInteractable.Value = canUpgrade;
            
            UpdateCanChangeButtons();
            OnSoldierChanged?.Invoke();
        }

        private void OnSelectSkin(int skinId)
        {
            CurrentSkinId.Value = skinId;
            OnSkinChanged?.Invoke(skinId);
        }

        private async void OnUpgrade()
        {
            if (CurrentCard.Value == null) return;

            var cardId = CurrentCard.Value.CardId;

            // [已修改] 从配置表获取当前等级升级所需的所有消耗品，默认全部消耗
            var developCfg = mCardDevelopConfig.GetOrDefault(cardId);
            var consumeItems = new Dictionary<int, int>();
            if (developCfg != null && CurrentCard.Value.Lv <= developCfg.LevelPropertyList.Count)
            {
                var lvCfg = developCfg.LevelPropertyList[CurrentCard.Value.Lv - 1];
                foreach (var item in lvCfg.ConsumeItems)
                {
                    consumeItems[item.Id] = item.Num;
                }
            }

            var result = await mSoldierService.UpgradeSoldierAsync(cardId, consumeItems);
            if (result.Success)
            {
                // 更新当前卡牌数据
                CurrentCard.Value = Global.PlayerCards[cardId];
                SoldierList.Value[CurrentSoldierIdx.Value] = CurrentCard.Value;

                // 重新检查升级按钮状态
                var newDevelopCfg = mCardDevelopConfig.GetOrDefault(CurrentCard.Value.CardId);
                IsUpgradeBtnInteractable.Value = CheckCanUpgrade(CurrentCard.Value, newDevelopCfg);
                UnlockGuideManager.Send(ModuleFunctionType.SoldierAtlas,3);
                OnUpgradeSuccess?.Invoke();
            }
            else
            {
                OnUpgradeFailed?.Invoke(result.ErrorMessage);
            }
        }

        private bool CanUpgrade()
        {
            if (CurrentCard.Value == null) return false;

            var developCfg = mCardDevelopConfig.GetOrDefault(CurrentCard.Value.CardId);
            if (developCfg == null) return false;

            return CheckCanUpgrade(CurrentCard.Value, developCfg);
        }

        /// <summary>
        /// 检查是否可以升级（未满级 + 消耗品足够）
        /// </summary>
        private bool CheckCanUpgrade(PlayerCard card, CardDevelopConfig developCfg)
        {
            if (card == null || developCfg == null) return false;

            // 检查是否满级
            if (card.Lv >= developCfg.LevelPropertyList.Last().Level)
                return false;

            // 检查当前等级消耗品是否足够
            if (card.Lv > developCfg.LevelPropertyList.Count) return false;
            var lvCfg = developCfg.LevelPropertyList[card.Lv - 1];
            return Items.CheckEnough(lvCfg.ConsumeItems);
        }

        private void OnAwakening()
        {
            OnAwakeningRequested?.Invoke(CurrentCard.Value.CardId, CurrentCard.Value.Lv);
        }

        private void OnDevelop()
        {
            OnDevelopRequested?.Invoke(CurrentCard.Value.CardId);
        }

        private void OnEquip()
        {
            OnEquipRequested?.Invoke(CurrentCard.Value.CardId);
        }

        private void OnSkin()
        {
            OnSkinRequested?.Invoke(CurrentCard.Value.CardId);
        }

        private void OnFusionGem()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.FusionGemSlot, true))
            {
                return;
            }
            OnFusionGemRequested?.Invoke(CurrentCard.Value.CardId);
        }

        private void OnMainAttributes()
        {
            OnMainAttributesRequested?.Invoke(CurrentCard.Value.CardId);
        }

        private void OnBaseAttributes()
        {
            OnBaseAttributesRequested?.Invoke(CurrentCard.Value.CardId);
        }

        private void OnChangeSkillFilter(CardType cardType)
        {
            CurrentSkillFilter.Value = cardType;
            UpdateSkillList();
        }

        // [已注释] 消耗品多选功能：不再需要玩家手动勾选道具
        // private void OnToggleConsumeItem(int itemId)
        // {
        //     if (ConsumeItems.Value.ContainsKey(itemId))
        //     {
        //         ConsumeItems.Value.Remove(itemId);
        //     }
        //     else
        //     {
        //         ConsumeItems.Value[itemId] = -1;
        //     }
        //
        //     // 通知变更
        //     ConsumeItems.Value = new Dictionary<int, int>(ConsumeItems.Value);
        //
        //     // 更新升级按钮状态
        //     var developCfg = mCardDevelopConfig.GetOrDefault(CurrentCard.Value.CardId);
        //     bool canUpgrade = ConsumeItems.Value.Count > 0 &&
        //                      CurrentCard.Value.Lv < developCfg.LevelPropertyList.Last().Level;
        //     IsUpgradeBtnInteractable.Value = canUpgrade;
        // }

        private void OnChangeGeneralSkillQualityFilter(List<QualityType> qualityFilters)
        {
            GeneralSkillQualityFilters.Value = qualityFilters;
            UpdateGeneralSkillList();
        }

        private void OnToggleGeneralSkillQualityFilter(QualityType qualityType)
        {
            // 单选逻辑：如果点击已选中的品质，则取消选择；否则选择新的品质
            if (GeneralSkillQualityFilters.Value.Count == 1 && GeneralSkillQualityFilters.Value[0] == qualityType)
            {
                // 点击已选中的品质，取消选择（显示全部）
                GeneralSkillQualityFilters.Value = new List<QualityType>();
            }
            else
            {
                // 选择新的品质（单选）
                GeneralSkillQualityFilters.Value = new List<QualityType> { qualityType };
            }
            UpdateGeneralSkillList();
        }

        private void UpdateGeneralSkillList()
        {
            // 获取所有通用技能（soldierId = 0 表示通用技能）
            if (!mSoldierSkillList.TryGetValue(0, out var generalList))
            {
                LogTool.Log($"[SoldierPanelViewModel] UpdateGeneralSkillList: 没有找到通用技能列表");
                GeneralSkillList.Value = new List<PlayerCard>();
                return;
            }


            // 筛选通用技能
            var filteredList = new List<PlayerCard>();
            foreach (var skill in generalList)
            {
                // 只处理通用技能类型
                if (skill.Config.Type != CardType.GeneralSkill)
                    continue;

                // 品质筛选
                if (GeneralSkillQualityFilters.Value.Count > 0)
                {
                    var skillCfg = mSpecialEffectsConfig.GetOrDefault(skill.CardId);
                    if (skillCfg == null || skillCfg.LevelPropertyList.Count == 0)
                    {
                        LogTool.Log($"[SoldierPanelViewModel] 技能 {skill.CardId} 配置为空或等级列表为空");
                        continue;
                    }

                    // 获取当前等级的品质（等级从1开始，数组从0开始）
                    int levelIndex = Mathf.Clamp(skill.Lv - 1, 0, skillCfg.LevelPropertyList.Count - 1);
                    var quality = skillCfg.LevelPropertyList[levelIndex].Quality;
                    
                    LogTool.Log($"[SoldierPanelViewModel] 技能 {skill.CardId}, 等级 {skill.Lv}, 品质 {quality}");

                    if (!GeneralSkillQualityFilters.Value.Contains(quality))
                    {
                        continue;
                    }
                }
                
                filteredList.Add(skill);
            }

            GeneralSkillList.Value = filteredList;
        }

        #endregion

        #region 公共方法

        public void Initialize(int soldierId)
        {
            if (SoldierList.Value == null || SoldierList.Value.Count == 0) return;

            var idx = SoldierList.Value.FindIndex(s => s.CardId == soldierId);
            if (idx >= 0)
            {
                CurrentSoldierIdx.Value = idx;
                CurrentCard.Value = SoldierList.Value[idx];
                CurrentSkinId.Value = CurrentCard.Value.ObjData.Skin;
                UpdateCanChangeButtons();

                // 初始化升级按钮状态
                var developCfg = mCardDevelopConfig.GetOrDefault(CurrentCard.Value.CardId);
                IsUpgradeBtnInteractable.Value = CheckCanUpgrade(CurrentCard.Value, developCfg);

                LogTool.Log($"[SoldierPanelViewModel] Initialize: 指定士兵ID={soldierId}, 索引={idx}");
            }
            else
            {
                LogTool.LogWarning($"[SoldierPanelViewModel] Initialize: 未找到士兵ID={soldierId}, 使用默认选中");
            }
        }

        public void UpdateSkillList()
        {
            if (CurrentCard.Value == null) return;

            // 更新普通技能列表（当前士兵专属）
            if (mSoldierSkillList.TryGetValue(CurrentCard.Value.CardId, out var skillList))
            {
                var filteredList = new List<PlayerCard>();
                foreach (var skill in skillList)
                {
                    if (skill.Config.Type == CardType.Skill || skill.Config.Type == CardType.SkillPassive)
                    {
                        filteredList.Add(skill);
                    }
                }
                SkillList.Value = filteredList;
            }
            else
            {
                SkillList.Value = new List<PlayerCard>();
            }

            // 更新通用技能列表（奥术卡）
            UpdateGeneralSkillList();
        }

        public void SetOpenPanelValue(int panelValue, bool syncLast = true)
        {
            CurrentOpenPanelValue.Value = panelValue;
            if (syncLast)
            {
                LastOpenPanelValue = panelValue;
            }
        }

        public int GetOpenPanelValue()
        {
            return CurrentOpenPanelValue.Value;
        }

        public int GetLastOpenPanelValue()
        {
            return LastOpenPanelValue;
        }

        public void ClearOpenPanelValue(bool clearLast = false)
        {
            CurrentOpenPanelValue.Value = -1;
            if (clearLast)
            {
                LastOpenPanelValue = -1;
            }
        }

        public SoldierBattleConfig GetSoldierConfig(int cardId)
        {
            return mSoldierBattleConfig.GetOrDefault(cardId);
        }

        public CardDevelopConfig GetDevelopConfig(int cardId)
        {
            return mCardDevelopConfig.GetOrDefault(cardId);
        }

        public SkinConfig GetSkinConfig(int skinId)
        {
            return mSkinConfig.GetOrDefault(skinId);
        }

        public List<PlayerCard> GetSkillList(int soldierId)
        {
            if (mSoldierSkillList.TryGetValue(soldierId, out var list))
            {
                return list;
            }
            return new List<PlayerCard>();
        }

        public bool CheckCardConsumeRedDot(PlayerCard card)
        {
            return mSoldierService.CheckCardConsumeRedDot(card);
        }

        public long GetCardProgress(PlayerCard card)
        {
            return mSoldierService.GetCardProgress(card);
        }

        public bool CanAwakening(PlayerCard card, out int awakeningLevel)
        {
            return mSoldierService.CanAwakening(card, out awakeningLevel);
        }

        /// <summary>
        /// 检查觉醒材料是否足够
        /// </summary>
        public bool HasEnoughAwakeningMaterials(PlayerCard card)
        {
            return mSoldierService.HasEnoughAwakeningMaterials(card);
        }

        #endregion

        #region 私有方法

        private void UpdateCanChangeButtons()
        {
            CanChangeLeft.Value = CurrentSoldierIdx.Value > 0;
            CanChangeRight.Value = CurrentSoldierIdx.Value + 1 < SoldierList.Value.Count;
        }

        private bool CheckSkillTypeFilter(CardType type)
        {
            switch (CurrentSkillFilter.Value)
            {
                case CardType.Skill:
                    return type == CardType.Skill || type == CardType.SkillPassive;
                case CardType.GeneralSkill:
                    return type == CardType.GeneralSkill;
                default:
                    return true;
            }
        }

        #endregion

        #region 事件

        public System.Action OnSoldierChanged;
        public System.Action<int> OnSkinChanged;
        public System.Action OnUpgradeSuccess;
        public System.Action<string> OnUpgradeFailed;
        public System.Action<int, int> OnAwakeningRequested;
        public System.Action<int> OnDevelopRequested;
        public System.Action<int> OnEquipRequested;
        public System.Action<int> OnSkinRequested;
        public System.Action<int> OnFusionGemRequested;
        public System.Action<int> OnMainAttributesRequested;
        public System.Action<int> OnBaseAttributesRequested;
        public System.Action OnSkillDataChanged; // 技能数据变化时触发，用于刷新显示
        public System.Action<int> OnCardSkinChanged; // 皮肤变更事件，用于刷新显示

        #endregion
    }
}
