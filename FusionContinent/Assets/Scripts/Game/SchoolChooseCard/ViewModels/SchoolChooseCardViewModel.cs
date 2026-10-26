using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using Manager;
using UIFramework.Services;
using ValueType = Config.ValueType;

namespace Game.SchoolChooseCard
{
    /// <summary>
    /// 流派选卡 ViewModel
    /// 管理卡牌选择界面的状态和逻辑
    /// </summary>
    public class SchoolChooseCardViewModel : ViewModel
    {
        #region 属性

        /// <summary>
        /// 打开计数（在调用 OpenPanelAsync 前设置）
        /// </summary>
        public int OpenCount { get; set; }

        /// <summary>
        /// 标题文本
        /// </summary>
        public BindableProperty<string> TitleText { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 金币数量
        /// </summary>
        public BindableProperty<int> GoldCount { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 倒计时文本
        /// </summary>
        public BindableProperty<string> CountdownText { get; } = new BindableProperty<string>("60");

        /// <summary>
        /// 是否暂停倒计时
        /// </summary>
        public BindableProperty<bool> IsPaused { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 品质权重列表数据（序列化传输到View）
        /// </summary>
        public BindableProperty<List<QualityWeightData>> QualityWeights { get; } =
            new BindableProperty<List<QualityWeightData>>(new List<QualityWeightData>());

        /// <summary>
        /// 当前卡牌列表（供View显示）
        /// </summary>
        public BindableProperty<List<CardDisplayData>> CardList { get; } =
            new BindableProperty<List<CardDisplayData>>(new List<CardDisplayData>());

        #endregion

        #region 命令

        /// <summary>
        /// 选择全部命令
        /// </summary>
        public Command OnGetAllCommand { get; private set; }

        /// <summary>
        /// 选择卡牌命令
        /// </summary>
        public Command<CardDisplayData> OnChooseCardCommand { get; private set; }

        /// <summary>
        /// 刷新卡牌命令
        /// </summary>
        public Command<int> OnRefreshCardCommand { get; private set; }

        #endregion

        #region 配置数据

        private TbGameBattleConst _gameBattleConst;
        private const float COUNTDOWN_TIME = 60f;

        #endregion

        #region 事件

        public System.Action<int, List<int>> OnSchoolCardFlyRequested;
        public System.Action<int> OnCountdownAutoChooseRequested;

        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            _gameBattleConst = DataConfigManager.Tables.TbGameBattleConst;

            OnGetAllCommand = new Command(OnGetAll);
            OnChooseCardCommand = new Command<CardDisplayData>(OnChooseCard);
            OnRefreshCardCommand = new Command<int>(OnRefreshCard);

            RegisterGlobalListeners();

            LogTool.Log("[SchoolChooseCardViewModel] 创建完成");
        }

        /// <summary>
        /// 注册全局数据监听
        /// </summary>
        private void RegisterGlobalListeners()
        {
            Global.GameGold.RegisterWithInitValue(gold =>
            {
                GoldCount.Value = gold;
            });
        }

        /// <summary>
        /// 初始化界面数据
        /// </summary>
        public void Initialize(int openCount)
        {
            var service = GameServiceManager.Get<Game.Services.SchoolChooseCardService>();
            service.Reset();
            service.InitializeSingleChoose(openCount);

            GoldCount.Value = Global.GameGold.Value;

            GenerateCardList();
            StartCountdown();
            
            var qualityList = service.GetQualityWeightList();
            var weights = new List<QualityWeightData>();

            foreach (var q in qualityList)
            {
                weights.Add(new QualityWeightData
                {
                    Quality = q.Quality,
                    Weight = q.Weight,
                    CategoryName = service.GetCategoryTypeName((int)q.Category),
                    Color = service.GetQualityColor(q.Quality)
                });
            }

            QualityWeights.Value = weights;

            if (qualityList.Count > 0)
            {
                var firstQuality = qualityList[0];
                TitleText.Value = $"请选择{service.GetCategoryTypeName((int)firstQuality.Category)}";
            }
        }

        /// <summary>
        /// 生成卡牌列表
        /// </summary>
        private void GenerateCardList()
        {
            var service = GameServiceManager.Get<Game.Services.SchoolChooseCardService>();
            var cards = new List<CardDisplayData>();
            service.IncrementOpenCount();
            var schoolCardList = service.GenerateChooseCardIds(service.SingleChooseCount);

            if (schoolCardList == null || schoolCardList.Count == 0) return;

            for (int i = 0; i < schoolCardList.Count; i++)
            {
                int cardId = schoolCardList[i];
                var cfg = service.GetSchoolCardConfig(cardId);
                if (cfg == null) continue;

                var data = CreateCardDisplayData(cardId, cfg, service, i + 1);
                data.IsRecommended = service.IsGuideRecommendedCard(cardId);
                cards.Add(data);
            }

            CardList.Value = cards;
        }

        /// <summary>
        /// 创建卡牌显示数据
        /// </summary>
        internal CardDisplayData CreateCardDisplayData(int cardId, SchoolCardConfig cfg,
            Game.Services.SchoolChooseCardService service, int slotIndex, int? existingStar = null)
        {
            var schoolCard = SchoolManager.Instance.GetSchoolCard(cardId);
            bool isCategorySkill = cfg.Category == SchoolCategoryType.Skill;
            var maxStar = cfg.StarList.Count;
            var currentStar = (existingStar.HasValue && existingStar.Value > 0)
                ? existingStar.Value
                : schoolCard.CardStar;
            var cardStar = isCategorySkill
                ? Mathf.Max(cfg.StarList.FirstOrDefault()?.AddStar ?? 1, 1)
                : (maxStar > 0 ? Mathf.Clamp(currentStar + 1, 1, maxStar) : 0);
            var starCfg = cfg.StarList.Count > 0
                ? cfg.StarList[Mathf.Clamp(cardStar, 1, cfg.StarList.Count) - 1]
                : null;

            string desc = starCfg?.Desc1 ?? "";
            for (int i = 1; i <= (starCfg?.Attributes.Count ?? 0); i++)
            {
                var attr = starCfg.Attributes[i - 1];
                var val = attr.ValueType == ValueType.Percentage ? attr.Value * 100 : attr.Value;
                if (attr.Type is AttributeType.SoldierCriticalRate or AttributeType.SoldierCriticalDamage)
                {
                    val = attr.Value * 100;
                }
                desc = desc.Replace("{Attribute" + i + "}", $"{val}");
            }

            if (starCfg?.Param1 > 0) desc = desc.Replace("{Param1}", $"{starCfg.Param1}");
            if (starCfg?.Param2 > 0) desc = desc.Replace("{Param2}", $"{starCfg.Param2}");

            var schoolIds = cfg.Schools.Count > 0 ? cfg.Schools : new List<int>();
            var quality = starCfg != null ? starCfg.Quality : cfg.Quality;
            var displayName = string.IsNullOrEmpty(starCfg?.Name) ? cfg.Name : starCfg.Name;
            var displayIcon = string.IsNullOrEmpty(starCfg?.Icon) ? cfg.Icon : starCfg.Icon;

            return new CardDisplayData
            {
                CardId = cardId,
                Name = displayName,
                Category = cfg.Category,
                CategoryName = service.GetCategoryTypeName((int)cfg.Category),
                Icon = displayIcon,
                Quality = quality,
                QualityColor = service.GetQualityColor(quality),
                Schools = schoolIds,
                SchoolIcons = schoolIds.Select(id =>
                {
                    var schoolCfg = service.GetSchoolConfig(id);
                    return schoolCfg?.Icon2 ?? "";
                }).ToList(),
                Star = cardStar,
                Description = desc,
                SlotIndex = slotIndex,
                MaxStar = maxStar,
                IsCategorySkill = isCategorySkill
            };
        }

        #region 命令实现

        /// <summary>
        /// 全部选择
        /// </summary>
        private void OnGetAll()
        {
            var cards = CardList.Value;
            foreach (var card in cards)
            {
                if (card != null)
                {
                    OnChooseCard(card);
                }
            }
        }

        /// <summary>
        /// 选择卡牌
        /// </summary>
        private void OnChooseCard(CardDisplayData data)
        {
            if (data == null) return;

            var service = GameServiceManager.Get<Game.Services.SchoolChooseCardService>();

            service.ChooseCard(data.CardId, data.Schools, data.IsCategorySkill);

            LogTool.Log($"[SchoolChooseCardViewModel] 选择卡牌: {data.Name}");
        }

        /// <summary>
        /// 刷新卡牌
        /// </summary>
        private void OnRefreshCard(int slotIndex)
        {
            var service = GameServiceManager.Get<Game.Services.SchoolChooseCardService>();
            var (canAfford, cost) = service.CheckRefreshCost();
            if (!canAfford) return;

            var newCardIds = service.RandomSchoolChooseCards(1);
            if (newCardIds == null || newCardIds.Count == 0) return;

            var newCfg = service.GetSchoolCardConfig(newCardIds[0]);
            if (newCfg == null) return;

            int refreshIndex = CardList.Value.Count > 0 ? slotIndex : 0;
            service.RefreshSingleCard(refreshIndex);

            var updatedCards = new List<CardDisplayData>(CardList.Value);
            updatedCards[slotIndex] = CreateCardDisplayData(newCardIds[0], newCfg, service, slotIndex);
            CardList.Value = updatedCards;
        }

        #endregion

        #region 倒计时

        private float _countdownTime;
        private bool _countdownActive;

        /// <summary>
        /// 开始倒计时
        /// </summary>
        public void StartCountdown()
        {
            _countdownTime = COUNTDOWN_TIME;
            _countdownActive = true;
            IsPaused.Value = false;
        }

        /// <summary>
        /// 停止倒计时
        /// </summary>
        public void StopCountdown()
        {
            _countdownActive = false;
            IsPaused.Value = true;
        }

        /// <summary>
        /// 每帧更新（由Panel调用）
        /// </summary>
        public void OnUpdate(float deltaTime)
        {
            if (!_countdownActive || IsPaused.Value) return;

            _countdownTime -= deltaTime;
            CountdownText.Value = $"{Mathf.CeilToInt(_countdownTime)}";

            if (_countdownTime <= 0)
            {
                _countdownTime = 0;
                OnCountdownExpired();
            }
        }

        /// <summary>
        /// 倒计时结束
        /// </summary>
        private void OnCountdownExpired()
        {
            StopCountdown();

            var cards = CardList.Value;
            if (cards == null || cards.Count == 0) return;

            int randomIndex = Random.Range(0, cards.Count);
            OnCountdownAutoChooseRequested?.Invoke(randomIndex);
        }

        #endregion

        public override void OnDestroy()
        {
            base.OnDestroy();
            StopCountdown();
        }

        #region 辅助类型

        /// <summary>
        /// 品质权重数据
        /// </summary>
        public class QualityWeightData
        {
            public QualityType Quality { get; set; }
            public int Weight { get; set; }
            public string CategoryName { get; set; }
            public Color Color { get; set; }
        }

        /// <summary>
        /// 卡牌显示数据
        /// </summary>
        public class CardDisplayData
        {
            public int CardId { get; set; }
            public string Name { get; set; }
            public SchoolCategoryType Category { get; set; }
            public string CategoryName { get; set; }
            public string Icon { get; set; }
            public QualityType Quality { get; set; }
            public Color QualityColor { get; set; }
            public List<int> Schools { get; set; }
            public List<string> SchoolIcons { get; set; }
            public int Star { get; set; }
            public string Description { get; set; }
            public int SlotIndex { get; set; }
            public int MaxStar { get; set; }
            public bool IsCategorySkill { get; set; }
            public bool IsRecommended { get; set; }
        }

        #endregion
    }
}
