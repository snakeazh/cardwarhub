using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.Soldier;
using Game.Team;
using UIFramework;
using UIFramework.Services;

namespace Game.CardGroup
{
    /// <summary>
    /// 编队界面 ViewModel：士兵槽位数据、上阵/下阵、保存编队、战力与关卡信息。
    /// 士兵槽位列表保持"源索引"语义；显示槽位与源索引的映射（CardsPosition）由本类持有，
    /// 拖拽换位后由 View 同步进来，保证数据真值源单一。
    /// </summary>
    public class CardGroupPanelViewModel : ViewModel
    {
        /// <summary>士兵拖拽槽位数（与原实现 slotMax=4 一致）</summary>
        public const int SoldierSlotMax = 4;

        /// <summary>士兵槽列表容量（原实现 max=5，第 5 个恒为锁定）</summary>
        private const int SoldierListCapacity = 5;

        #region 属性
        public BindableProperty<string> LevelName { get; } = new BindableProperty<string>("");
        public BindableProperty<string> LevelNameNum { get; } = new BindableProperty<string>("");
        public BindableProperty<int> FightNum { get; } = new BindableProperty<int>(0);
        public BindableProperty<int> SoldierCount { get; } = new BindableProperty<int>(0);
        public int SoldierMaxCount { get; private set; }
        public int CurrentStageId { get; private set; } = -1;
        #endregion

        #region 槽位数据
        /// <summary>士兵槽位卡 Id（源索引语义，0=空、-1=锁定），View 只读</summary>
        public List<int> TeamSoldierIdList { get; } = new List<int>();
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnStartGameCommand { get; private set; }
        public Command OnRecommendCommand { get; private set; }
        #endregion

        #region 事件
        public System.Action OnCloseRequested;
        public System.Action<int> OnStartBattleRequested;
        public System.Action OnOpenRecommendRequested;
        public System.Action OnSoldierListChanged;
        #endregion

        // 槽位映射：mSlotValues[i] = 显示槽位 i 放置的源索引，拖拽换位后由 View 同步
        private int[] mSlotValues = new int[0];

        private int mTalentId;
        private PlayerTeam mCurrentTeam;
        private TbCardBaseConfig mCardBaseConfig;
        private TbSpecialEffectsConfig mSpecialEffectsConfig;

        public override void OnCreate()
        {
            base.OnCreate();
            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            mSpecialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

            OnCloseCommand = new Command(OnClose);
            OnStartGameCommand = new Command(OnStartGame);
            OnRecommendCommand = new Command(OnRecommend);
        }

        /// <summary>
        /// 数据装载入口（OnCreate 后由打开方立即调用）。
        /// </summary>
        public void SetData(int stageId, int talentId = 0)
        {
            CurrentStageId = stageId;
            mTalentId = talentId != 0 ? talentId : Talent.GetUseTalent().TalentId;

            TeamSoldierIdList.Clear();

            mCurrentTeam = GameServiceManager.GetOrCreate<TeamService>().GetTeamByTeamId(mTalentId);

            SoldierMaxCount = Talent.GetPopulation(mTalentId);

            // 士兵槽：容量 SoldierListCapacity，前 SoldierSlotMax 个按人口数解锁
            for (int i = 0; i < SoldierListCapacity; i++)
            {
                TeamSoldierIdList.Add(i < SoldierSlotMax && i < SoldierMaxCount ? 0 : -1);
            }
            InitTeamSoldierSlotData();
            ApplyCardsPositionToSoldierList();

            // 列表已按 CardsPosition 重排，槽位映射重置为恒等（与原实现一致）
            mSlotValues = new int[SoldierSlotMax];
            for (int i = 0; i < mSlotValues.Length; i++)
            {
                mSlotValues[i] = i;
            }

            RefreshLevelName();
            RefreshCounts();
            FightNum.Value = Global.User != null ? Global.User.FightingNum : 0;
        }

        #region 槽位映射

        /// <summary>当前槽位映射拷贝（View 初始化 DragBootstrap 用）</summary>
        public int[] GetSlotValues()
        {
            var result = new int[mSlotValues.Length];
            for (int i = 0; i < mSlotValues.Length; i++)
            {
                result[i] = mSlotValues[i];
            }
            return result;
        }

        /// <summary>拖拽换位成功后由 View 同步最新槽位映射</summary>
        public void SetSoldierSlotMapping(int[] slotValues)
        {
            if (slotValues == null || slotValues.Length == 0) return;

            mSlotValues = new int[slotValues.Length];
            for (int i = 0; i < slotValues.Length; i++)
            {
                mSlotValues[i] = slotValues[i];
            }
        }

        #endregion

        #region 数据装载

        private void InitTeamSoldierSlotData()
        {
            int pos = 0;
            foreach (var cardId in mCurrentTeam.Cards)
            {
                var card = Global.GetCardById(cardId);
                var baseCfg = mCardBaseConfig.GetOrDefault(cardId);
                if (card == null || baseCfg == null) continue;
                if (card.Config.Type == CardType.Soldier && pos < TeamSoldierIdList.Count)
                {
                    TeamSoldierIdList[pos] = cardId;
                    pos++;
                }
            }
        }

        // 按服务器存的 CardsPosition（目标槽位→源索引）重排士兵槽列表
        private void ApplyCardsPositionToSoldierList()
        {
            if (mCurrentTeam == null || mCurrentTeam.CardsPosition == null || mCurrentTeam.CardsPosition.Count == 0)
            {
                return;
            }

            var originSoldierIdList = new List<int>(TeamSoldierIdList);
            var usedSourceIndexes = new HashSet<int>();
            int targetCount = Math.Min(SoldierSlotMax, TeamSoldierIdList.Count);
            int targetIndex = 0;

            for (int i = 0; i < mCurrentTeam.CardsPosition.Count && targetIndex < targetCount; i++)
            {
                int sourceIndex = mCurrentTeam.CardsPosition[i];
                if (sourceIndex < 0 || sourceIndex >= originSoldierIdList.Count || usedSourceIndexes.Contains(sourceIndex))
                {
                    continue;
                }

                TeamSoldierIdList[targetIndex] = originSoldierIdList[sourceIndex];
                usedSourceIndexes.Add(sourceIndex);
                targetIndex++;
            }

            for (int i = 0; i < originSoldierIdList.Count && targetIndex < targetCount; i++)
            {
                if (usedSourceIndexes.Contains(i))
                {
                    continue;
                }

                TeamSoldierIdList[targetIndex] = originSoldierIdList[i];
                targetIndex++;
            }
        }

        private void RefreshLevelName()
        {
            var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.GetOrDefault(CurrentStageId);
            if (stageCfg != null)
            {
                LevelName.Value = stageCfg.StageName;
                LevelNameNum.Value = $"第{stageCfg.ChapterId}章";
            }
            else
            {
                LevelName.Value = "";
                LevelNameNum.Value = "";
            }
        }

        private void RefreshCounts()
        {
            int soldierNum = 0;
            foreach (var id in TeamSoldierIdList)
            {
                if (id > 0) soldierNum++;
            }
            SoldierCount.Value = soldierNum;
        }

        #endregion

        #region 上阵 / 下阵

        /// <summary>
        /// 上阵卡牌（仅士兵槽；技能/奥术卡子面板已下线，其他类型不处理）
        /// </summary>
        public void Deploy(CardType cardType, int cardId, bool needUpdate = true)
        {
            if (cardType != CardType.Soldier) return;

            // 按显示槽位顺序找第一个空槽（取映射后的源索引）
            int findSlotIndex = -1;
            for (int i = 0; i < mSlotValues.Length; i++)
            {
                int sourceIndex = mSlotValues[i];
                if (sourceIndex >= 0 && sourceIndex < TeamSoldierIdList.Count && TeamSoldierIdList[sourceIndex] == 0)
                {
                    findSlotIndex = sourceIndex;
                    break;
                }
            }

            if (findSlotIndex >= 0)
            {
                TeamSoldierIdList[findSlotIndex] = cardId;
                RefreshCounts();
                if (needUpdate && OnSoldierListChanged != null)
                {
                    OnSoldierListChanged.Invoke();
                }
            }
            else
            {
                LogTool.Log($"[CardGroupPanelViewModel] 上阵失败：没有空的士兵槽位, cardId={cardId}");
            }
        }

        /// <summary>
        /// 下阵英雄（仅士兵）
        /// </summary>
        public void ReCallCard(CardType cardType, int cardId)
        {
            if (cardType != CardType.Soldier) return;

            int findSlotIndex = -1;
            for (int i = 0; i < TeamSoldierIdList.Count; i++)
            {
                if (TeamSoldierIdList[i] == cardId)
                {
                    findSlotIndex = i;
                    break;
                }
            }

            if (findSlotIndex >= 0)
            {
                TeamSoldierIdList[findSlotIndex] = 0;
                RefreshCounts();
                if (OnSoldierListChanged != null)
                {
                    OnSoldierListChanged.Invoke();
                }
            }
        }

        /// <summary>
        /// 下阵对应槽位的士兵（传源索引）
        /// </summary>
        public void ReCallCardIndex(CardType cardType, int slotIndex, bool needUpdateUI = true)
        {
            if (cardType != CardType.Soldier) return;

            if (slotIndex >= 0 && slotIndex < TeamSoldierIdList.Count)
            {
                TeamSoldierIdList[slotIndex] = 0;
                RefreshCounts();
                if (needUpdateUI && OnSoldierListChanged != null)
                {
                    OnSoldierListChanged.Invoke();
                }
            }
        }

        /// <summary>
        /// 推荐卡组入口：清空士兵槽位后逐个上阵，最后统一刷新
        /// </summary>
        public void SelectCards(List<int> cards)
        {
            for (int i = 0; i < TeamSoldierIdList.Count; i++)
            {
                if (TeamSoldierIdList[i] > 0)
                {
                    TeamSoldierIdList[i] = 0;
                }
            }

            for (int i = 0; i < cards.Count; i++)
            {
                var baseCfg = mCardBaseConfig.GetOrDefault(cards[i]);
                if (baseCfg == null)
                {
                    continue;
                }

                Deploy(baseCfg.Type, cards[i], false);
            }

            RefreshCounts();
            if (OnSoldierListChanged != null)
            {
                OnSoldierListChanged.Invoke();
            }
        }

        #endregion

        #region 查询

        public int CheckCardInTeam(CardType cardType, int cardId)
        {
            if (cardId <= 0)
            {
                return 0;
            }

            if (cardType == CardType.Soldier)
            {
                return TeamSoldierIdList.Contains(cardId) ? 1 : 0;
            }

            return 0;
        }

        public bool CheckCardSkillSoliderInTeam(int cardId)
        {
            foreach (var soldierId in TeamSoldierIdList)
            {
                var skillCfg = mSpecialEffectsConfig.GetOrDefault(cardId);
                if (skillCfg != null && skillCfg.ActsOnCard == soldierId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 统计当前队伍士兵职业最多项对应的羁绊配置
        /// </summary>
        public BindingConfig GetCurrentBindingConfig(out int activeCount)
        {
            var typeCount = new Dictionary<int, int>();
            foreach (var cardId in TeamSoldierIdList)
            {
                if (cardId <= 0) continue;
                var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(cardId);
                if (soldierCfg == null) continue;
                var carType = (int)soldierCfg.CarType;
                typeCount.TryGetValue(carType, out int count);
                typeCount[carType] = count + 1;
            }

            int bestType = -1;
            int bestCount = 0;
            foreach (var kv in typeCount)
            {
                if (kv.Value > bestCount)
                {
                    bestCount = kv.Value;
                    bestType = kv.Key;
                }
            }

            activeCount = typeCount.Count;

            if (bestType >= 0)
            {
                var configs = DataConfigManager.Tables.TbBindingConfig.DataList;
                foreach (var cfg in configs)
                {
                    if (cfg.Id == bestType)
                    {
                        return cfg;
                    }
                }
            }

            // 兜底：返回表中第一个
            return DataConfigManager.Tables.TbBindingConfig.DataList.Count > 0
                ? DataConfigManager.Tables.TbBindingConfig.DataList[0]
                : null;
        }

        #endregion

        #region 战力与卡组转换

        public List<int> ConvertCards()
        {
            var result = new List<int>();
            for (int i = 0; i < TeamSoldierIdList.Count; i++)
            {
                if (TeamSoldierIdList[i] > 0)
                {
                    result.Add(TeamSoldierIdList[i]);
                }
            }

            foreach (var soldierId in TeamSoldierIdList)
            {
                if (soldierId > 100)
                {
                    var list = GameServiceManager.GetOrCreate<SoldierService>().GetAllSkillsBySoldierId(soldierId);
                    if (list != null && list.Count > 0)
                    {
                        foreach (var item in list)
                        {
                            if (item.IsHave)
                            {
                                result.Add(item.CardId);
                            }
                        }
                    }
                }
            }

            // 奥术卡已去佩戴化：不再并入 AsCardTeam，战斗直接读 Global.PlayerCards（TeamService.BuildBattleTeamsForCurrentTalent）
            return result;
        }

        #endregion

        #region 保存编队

        private async UniTask<bool> SaveTeam()
        {
            try
            {
                var newCards = ConvertCards();
                var newCardsPosition = BuildCardsPosition();


                // 检查必须上阵4个英灵
                int deployedCount = 0;
                foreach (var id in TeamSoldierIdList)
                {
                    if (id > 0) deployedCount++;
                }
                if (deployedCount != 4)
                {
                    throw new Exception("上阵英雄数量必须4个");
                }

                var cards = newCards;

                // 判断阵容是否修改
                var currentSoldierCards = GetSoldierCardIds(mCurrentTeam.Cards);
                if (IsSameIntList(cards, mCurrentTeam.Cards))
                {
                    if (IsSameCardsPosition(newCardsPosition, mCurrentTeam.CardsPosition, TeamSoldierIdList, currentSoldierCards))
                    {
                        return true;
                    }
                }

                mCurrentTeam.Cards = cards;
                mCurrentTeam.CardsPosition = newCardsPosition;
                var teamService = GameServiceManager.GetOrCreate<TeamService>();
                var saveSuccess = await teamService.SaveTeamAsync(mCurrentTeam);
                if (saveSuccess)
                {
                    mCurrentTeam = teamService.GetTeamByTeamId(mTalentId);
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                Toast.Show(ex.Message);
                LogTool.LogError($"保存编队失败：{ex.Message}");
                return false;
            }

            return false;
        }

        private bool CheckSoldierTeamNotEmpty()
        {
            foreach (var cardId in TeamSoldierIdList)
            {
                if (cardId > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private List<int> BuildCardsPosition()
        {
            var cardsPosition = new List<int>(mSlotValues.Length);
            for (int i = 0; i < mSlotValues.Length; i++)
            {
                cardsPosition.Add(mSlotValues[i]);
            }
            return cardsPosition;
        }

        private bool IsSameIntList(IList<int> left, IList<int> right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.Count != right.Count)
            {
                return false;
            }

            var cardCounts = new Dictionary<int, int>();
            foreach (var cardId in left)
            {
                cardCounts.TryGetValue(cardId, out int count);
                cardCounts[cardId] = count + 1;
            }

            foreach (var cardId in right)
            {
                if (!cardCounts.TryGetValue(cardId, out int count))
                {
                    return false;
                }

                if (count == 1)
                {
                    cardCounts.Remove(cardId);
                }
                else
                {
                    cardCounts[cardId] = count - 1;
                }
            }

            return cardCounts.Count == 0;
        }

        private List<int> GetSoldierCardIds(IList<int> cards)
        {
            var result = new List<int>();
            if (cards == null)
            {
                return result;
            }

            foreach (var cardId in cards)
            {
                var cardCfg = mCardBaseConfig.GetOrDefault(cardId);
                if (cardCfg != null && cardCfg.Type == CardType.Soldier)
                {
                    result.Add(cardId);
                }
            }

            return result;
        }

        private bool IsSameCardsPosition(List<int> newCardsPosition, List<int> currentCardsPosition,
            IList<int> newSoldierCards, IList<int> currentSoldierCards)
        {
            if (newCardsPosition == null || currentCardsPosition == null)
            {
                return newCardsPosition == currentCardsPosition;
            }

            if (newCardsPosition.Count == 0)
            {
                return currentCardsPosition.Count == 0;
            }

            for (int i = 0; i < newCardsPosition.Count; i++)
            {
                int currentPosition = i < currentCardsPosition.Count ? currentCardsPosition[i] : i;
                if (newCardsPosition[i] != currentPosition)
                {
                    return false;
                }
            }

            for (int i = newCardsPosition.Count; i < currentCardsPosition.Count; i++)
            {
                if (currentCardsPosition[i] != i)
                {
                    return false;
                }
            }

            for (int i = 0; i < newCardsPosition.Count; i++)
            {
                int newCardId = GetCardIdByPosition(newSoldierCards, newCardsPosition, i);
                int currentCardId = GetCardIdByPosition(currentSoldierCards, currentCardsPosition, i);
                if (newCardId != currentCardId)
                {
                    return false;
                }
            }

            return true;
        }

        private int GetCardIdByPosition(IList<int> soldierCards, IList<int> cardsPosition, int positionIndex)
        {
            if (soldierCards == null || cardsPosition == null)
            {
                return 0;
            }

            int sourceIndex = positionIndex < cardsPosition.Count ? cardsPosition[positionIndex] : positionIndex;
            if (sourceIndex < 0 || sourceIndex >= soldierCards.Count)
            {
                return 0;
            }

            return soldierCards[sourceIndex];
        }

        #endregion

        #region 命令实现

        private async void OnClose()
        {
            // 与原实现一致：无论保存成功与否都关闭
            await SaveTeam();
            if (OnCloseRequested != null)
            {
                OnCloseRequested.Invoke();
            }
        }

        private async void OnStartGame()
        {
            bool result = await SaveTeam();
            if (result && OnStartBattleRequested != null)
            {
                BattleSession.ResetToPve();
                OnStartBattleRequested.Invoke(CurrentStageId);
            }
        }

        private void OnRecommend()
        {
            if (OnOpenRecommendRequested != null)
            {
                OnOpenRecommendRequested.Invoke();
            }
        }

        #endregion
    }
}
