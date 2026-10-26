using UIFramework.Services;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent.Guide;
using FusionContinent.Model;
using Manager;
using ValueType = Config.ValueType;

namespace Game.Services
{
    /// <summary>
    /// 流派选卡服务
    /// 管理卡池、随机卡牌逻辑和流派卡添加
    /// </summary>
    public class SchoolChooseCardService : GameServiceBase
    {
        #region 属性
        /// <summary>
        /// 单次选择卡数量
        /// </summary>
        public int SingleChooseCount { get; set; } = 3;
        #endregion

        #region 依赖
        private TbSchoolConfig _schoolConfig;
        private TbCityLevelConfig _cityLevelConfig;
        private TbSchoolCardConfig _schoolCardConfig;
        private TbGameBattleConst _gameBattleConst;
        #endregion

        #region 私有字段

        private Dictionary<QualityType, Color32> _qualityTypeColors = new Dictionary<QualityType, Color32>();

        private List<string> _categoryTypeNameList = new List<string>();

        private int _openCount;
        private int _refreshCount;
        private int _lastUserLv;
        private int _guideRecommendOpenIndex;
        private int? _currentOpenGuideRecommendedCardId;
        private CityLevelList _currentCityLevelCfg;
        private System.Random _rand = new System.Random();
        private readonly HashSet<int> _selectedCardIds = new HashSet<int>();
        #endregion

        #region 事件
        public System.Action<int, List<int>> OnSchoolCardFlyRequested;
        public System.Action OnChooseOver;
        public System.Action<int, int> OnGoldChanged;
        public System.Action<int, int> OnRefreshCardRequested; // layers, cardId
        #endregion

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();

            _schoolConfig = DataConfigManager.Tables.TbSchoolConfig;
            _cityLevelConfig = DataConfigManager.Tables.TbCityLevelConfig;
            _schoolCardConfig = DataConfigManager.Tables.TbSchoolCardConfig;
            _gameBattleConst = DataConfigManager.Tables.TbGameBattleConst;
            _categoryTypeNameList = DataConfigManager.Tables.TbGameBattleConst.SchoolCategoryTypeNameList;

            var qualityTypeColorList = DataConfigManager.Tables.TbGameConst.ChooseQualityTypeColors;
            foreach (var v in qualityTypeColorList)
            {
                _qualityTypeColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }

            LogTool.Log("[SchoolChooseCardService] 初始化完成");
        }

        public override void Reset()
        {
            _openCount = 0;
            _refreshCount = 0;
            _lastUserLv = 0;
            _selectedCardIds.Clear();
            _currentOpenGuideRecommendedCardId = null;
            if (!IsGuideRunning())
            {
                _guideRecommendOpenIndex = 0;
            }
        }

        /// <summary>
        /// 初始化单次选择
        /// </summary>
        public void InitializeSingleChoose(int openCount)
        {
            Reset();
            _openCount = openCount;

            var difficultyCfg = _cityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            var currentCityLevel = Mathf.Max(Global.CityLevel.Value, 1);
            _currentCityLevelCfg = difficultyCfg?.LevelList?.FirstOrDefault(cfg => cfg.Level == currentCityLevel);
        }

        /// <summary>
        /// 设置数据（用于界面打开）
        /// </summary>
        public void SetData(int openCount)
        {
            InitializeSingleChoose(openCount);
        }

        /// <summary>
        /// 随机获取流派三选一卡
        /// </summary>
        public List<int> RandomSchoolChooseCards(int cardCount = 1, HashSet<int> excludedCardIds = null)
        {
            if (_currentCityLevelCfg == null) return null;

            int totalWeight = 0;
            List<QualityWeightStruct> totalPool = new List<QualityWeightStruct>();
            foreach (var v in GetQualityWeightList())
            {
                totalPool.Add(v);
                totalWeight += v.Weight;
            }

            var schoolCardList = new List<int>();
            if (totalWeight <= 0 || SchoolManager.Instance._qualityPoolSchoolCards.Count == 0) return schoolCardList;

            int time = 1000;
            while (schoolCardList.Count < cardCount && time > 0)
            {
                time--;
                int randomValue = _rand.Next(0, totalWeight + 1);
                int accumulatedWeight = 0;

                foreach (var card in totalPool)
                {
                    accumulatedWeight += card.Weight;
                    if (randomValue < accumulatedWeight)
                    {
                        var candidateCards = GetCandidateCardsForWeight(card.Quality, card.Category, excludedCardIds);
                        if (candidateCards.Count > 0)
                        {
                            var schoolCardId = candidateCards[_rand.Next(0, candidateCards.Count)];
                            if (schoolCardList.Contains(schoolCardId))
                                continue;
                            schoolCardList.Add(schoolCardId);
                        }
                        break;
                    }
                }
            }
            return schoolCardList;
        }

        /// <summary>
        /// 生成三选一卡牌列表；新手引导期间按打开次数注入 NewGuideBuilding 推荐卡（每次一张）。
        /// </summary>
        public bool IsGuideRecommendedCard(int cardId)
        {
            return _currentOpenGuideRecommendedCardId.HasValue &&
                   _currentOpenGuideRecommendedCardId.Value == cardId;
        }

        public List<int> GenerateChooseCardIds(int cardCount)
        {
            int? guideCardId = TryConsumeGuideRecommendCardId();
            _currentOpenGuideRecommendedCardId = guideCardId;
            int randomCount = guideCardId.HasValue ? cardCount - 1 : cardCount;

            HashSet<int> excluded = null;
            if (guideCardId.HasValue)
            {
                excluded = new HashSet<int> { guideCardId.Value };
            }

            var result = RandomSchoolChooseCards(randomCount, excluded) ?? new List<int>();
            if (!guideCardId.HasValue)
            {
                return result;
            }

            int insertIndex = result.Count > 0 ? _rand.Next(0, result.Count + 1) : 0;
            result.Insert(insertIndex, guideCardId.Value);
            return result;
        }

        private bool IsGuideRunning()
        {
            return GamePlayManager.Default != null && GamePlayManager.Default.GuideIsRunning();
        }

        private int? TryConsumeGuideRecommendCardId()
        {
            if (!IsGuideRunning())
            {
                return null;
            }

            var guideList = _gameBattleConst?.NewGuideBuilding;
            if (guideList == null || guideList.Count == 0 || _guideRecommendOpenIndex >= guideList.Count)
            {
                return null;
            }

            int cardId = guideList[_guideRecommendOpenIndex];
            _guideRecommendOpenIndex++;
            return cardId;
        }

        /// <summary>
        /// 获取品质权重列表
        /// </summary>
        public List<QualityWeightStruct> GetQualityWeightList()
        {
            return _currentCityLevelCfg?.QualityWeight ?? new List<QualityWeightStruct>();
        }

        /// <summary>
        /// 获取品质类型颜色
        /// </summary>
        public Color32 GetQualityColor(QualityType qualityType)
        {
            return _qualityTypeColors.GetValueOrDefault(qualityType, Color.white);
        }

        /// <summary>
        /// 获取类别名称
        /// </summary>
        public string GetCategoryTypeName(int index)
        {
            if (index >= 0 && index < _categoryTypeNameList.Count)
                return _categoryTypeNameList[index];
            return "";
        }

        /// <summary>
        /// 选择卡牌（处理添加逻辑）
        /// </summary>
        public void ChooseCard(int cardId, List<int> schools, bool isCategorySkill)
        {
            var schoolCardCfg = _schoolCardConfig.GetOrDefault(cardId);
            if (schoolCardCfg == null) return;

            var schoolCard = SchoolManager.Instance.AddSchoolCard(schoolCardCfg.Id);

            if (schoolCard.CardStar >= schoolCardCfg.StarList.Count)
            {
                var qualityPoolKey = (schoolCardCfg.Quality, schoolCardCfg.Category);
                if (schoolCardCfg.RepetitiveSelection != 1 &&
                    SchoolManager.Instance._qualityPoolSchoolCards.TryGetValue(qualityPoolKey, out var pool))
                {
                    pool.Remove(cardId);
                }
            }

            if (schoolCardCfg.RepetitiveSelection != 1 && IsCardAtMaxStar(cardId, schoolCardCfg))
            {
                var qualityPoolKey = (schoolCardCfg.Quality, schoolCardCfg.Category);
                if (SchoolManager.Instance._qualityPoolSchoolCards.TryGetValue(qualityPoolKey, out var pool))
                {
                    pool.Remove(cardId);
                }
            }

            if (!isCategorySkill)
            {
                OnSchoolCardFlyRequested?.Invoke(1, schools);
            }

            _selectedCardIds.Add(cardId);
            Game.Battle.BattleRuleBattleEconomySupport.RegisterBoughtEffectCard(cardId);
            LogTool.Log($"[SchoolChooseCardService] 选择卡牌: {cardId}");
            // 注意：ChooseOver 不在这里调用，由 UI 层在动画结束后决定是否结束
        }

        /// <summary>
        /// 刷新单张卡牌
        /// </summary>
        public int RefreshSingleCard(int layers, IEnumerable<int> currentShownCardIds = null)
        {
            var excludedCardIds = new HashSet<int>(_selectedCardIds);
            if (currentShownCardIds != null)
            {
                foreach (var id in currentShownCardIds)
                {
                    excludedCardIds.Add(id);
                }
            }

            int cardId = GetRandomCardIdExcluding(excludedCardIds);
            if (cardId > 0)
            {
                var schoolCardCfg = _schoolCardConfig.GetOrDefault(cardId);
                if (schoolCardCfg != null)
                {
                    int goldCost = _refreshCount == 0 ? 1 : 3 * _refreshCount;
                    Global.GameGold.Value -= goldCost;
                    OnGoldChanged?.Invoke(goldCost, Global.GameGold.Value);
                    ++_refreshCount;
                    OnRefreshCardRequested?.Invoke(layers, cardId);
                    LogTool.Log($"[SchoolChooseCardService] 刷新卡牌: {cardId}, 消耗金币: {goldCost}");
                    return cardId;
                }
            }
            return 0;
        }

        private int GetRandomCardIdExcluding(HashSet<int> excludedCardIds)
        {
            if (_currentCityLevelCfg == null) return 0;

            int totalWeight = 0;
            var totalPool = new List<QualityWeightStruct>();
            foreach (var v in GetQualityWeightList())
            {
                totalPool.Add(v);
                totalWeight += v.Weight;
            }

            if (totalWeight <= 0 || SchoolManager.Instance._qualityPoolSchoolCards.Count == 0)
            {
                return 0;
            }

            int attempts = 1000;
            while (attempts-- > 0)
            {
                int randomValue = _rand.Next(0, totalWeight + 1);
                int accumulatedWeight = 0;
                foreach (var card in totalPool)
                {
                    accumulatedWeight += card.Weight;
                    if (randomValue >= accumulatedWeight) continue;

                    var candidateCards = GetCandidateCardsForWeight(card.Quality, card.Category, excludedCardIds);
                    if (candidateCards.Count > 0)
                    {
                        return candidateCards[_rand.Next(0, candidateCards.Count)];
                    }

                    break;
                }
            }

            return 0;
        }

        /// <summary>
        /// 检查刷新金币是否足够
        /// </summary>
        public (bool canAfford, int cost) CheckRefreshCost()
        {
            int cost = _refreshCount == 0 ? 1 : 3 * _refreshCount;
            return (Global.GameGold.Value > cost, cost);
        }

        /// <summary>
        /// 获取刷新金币成本
        /// </summary>
        public int GetRefreshCost()
        {
            return _refreshCount == 0 ? 1 : 3 * _refreshCount;
        }

        private bool CanCardAppearInChoosePool(int cardId)
        {
            var cfg = _schoolCardConfig.GetOrDefault(cardId);
            if (cfg == null)
            {
                return false;
            }

            if (cfg.IsAddHero > 0 && !IsHeroCurrentlyOnField(cfg.IsAddHero))
            {
                return false;
            }

            if (cfg.RepetitiveSelection != 1 && IsCardAtMaxStar(cardId, cfg))
            {
                return false;
            }

            return true;
        }

        private bool IsHeroCurrentlyOnField(int heroCardId)
        {
            var gamePlayManager = GamePlayManager.Default;
            if (gamePlayManager == null)
            {
                return false;
            }
            if (gamePlayManager.villageGameCards == null)
            {
                return false;
            }

            foreach (var soldier in gamePlayManager.villageGameCards)
            {
                if (soldier == null || soldier.IsDie()) continue;
                if (soldier.cardId == heroCardId) return true;
            }

            return false;
        }

        private bool IsCardAtMaxStar(int cardId, SchoolCardConfig schoolCardCfg = null)
        {
            schoolCardCfg ??= _schoolCardConfig.GetOrDefault(cardId);
            if (schoolCardCfg == null || schoolCardCfg.StarList == null || schoolCardCfg.StarList.Count == 0)
            {
                return false;
            }

            var schoolCard = SchoolManager.Instance.GetSchoolCard(cardId);
            return schoolCard.CardStar >= schoolCardCfg.StarList.Count;
        }

        private List<int> GetCandidateCardsForWeight(QualityType quality, SchoolCategoryType category,
            HashSet<int> excludedCardIds = null)
        {
            var candidateIds = new HashSet<int>();
            foreach (var pool in SchoolManager.Instance._qualityPoolSchoolCards)
            {
                if (pool.Key.Item2 != category) continue;
                foreach (var id in pool.Value)
                {
                    candidateIds.Add(id);
                }
            }

            return candidateIds
                .Where(id => excludedCardIds == null || !excludedCardIds.Contains(id))
                .Where(CanCardAppearInChoosePool)
                .Where(id => IsCardDisplayQualityMatch(id, quality))
                .ToList();
        }

        private bool IsCardDisplayQualityMatch(int cardId, QualityType targetQuality)
        {
            var cfg = _schoolCardConfig.GetOrDefault(cardId);
            if (cfg == null || cfg.StarList == null || cfg.StarList.Count == 0)
            {
                return cfg != null && cfg.Quality == targetQuality;
            }

            var schoolCard = SchoolManager.Instance.GetSchoolCard(cardId);
            var nextStar = Mathf.Clamp(schoolCard.CardStar + 1, 1, cfg.StarList.Count);
            var displayStarCfg = cfg.StarList[nextStar - 1];
            return displayStarCfg.Quality == targetQuality;
        }

        /// <summary>
        /// 选择结束
        /// </summary>
        public void ChooseOver()
        {
            _openCount = 999;
            if (!GuideManager.Instance.IsRunningSpe())
            {
                GamePlayManager.Default?.StartGame();
            }
            
            OnChooseOver?.Invoke();
            LogTool.Log("[SchoolChooseCardService] 选择结束");
        }

        /// <summary>
        /// 获取卡牌配置
        /// </summary>
        public SchoolCardConfig GetSchoolCardConfig(int cardId)
        {
            return _schoolCardConfig.GetOrDefault(cardId);
        }

        /// <summary>
        /// 获取流派配置
        /// </summary>
        public SchoolConfig GetSchoolConfig(int schoolId)
        {
            return _schoolConfig.GetOrDefault(schoolId);
        }

        /// <summary>
        /// 获取当前打开次数
        /// </summary>
        public int GetOpenCount() => _openCount;

        /// <summary>
        /// 增加打开次数
        /// </summary>
        public int IncrementOpenCount()
        {
            _openCount++;
            return _openCount;
        }

        /// <summary>
        /// 添加特殊卡牌（不显示界面）
        /// </summary>
        public void AddSpecialSchoolChooseCards(int cardCount = 1)
        {
            var addCards = RandomSchoolChooseCards(cardCount);
            for (int i = 0; i < addCards.Count; i++)
            {
                SchoolManager.Instance.AddSchoolCard(addCards[i]);
            }
        }
    }
}
