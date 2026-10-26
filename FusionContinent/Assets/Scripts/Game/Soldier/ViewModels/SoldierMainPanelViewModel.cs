using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using UIFramework.Services;
using Config;
using System.Linq;
using FusionContinent;
using FusionContinent.Model;
using Game.Soldier;
using Game.UserTool;

namespace Game.Soldier
{
    public class SoldierMainPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<List<PlayerCard>> SoldierList { get; } = new BindableProperty<List<PlayerCard>>();
        public BindableProperty<List<PlayerCard>> FilteredSoldierList { get; } = new BindableProperty<List<PlayerCard>>();
        public BindableProperty<CareerType?> CurrentFilter { get; } = new BindableProperty<CareerType?>();
        public BindableProperty<string> CardNumText { get; } = new BindableProperty<string>("");
        public BindableProperty<string> DamageLevelText { get; } = new BindableProperty<string>("");
        #endregion

        #region 命令
        public Command<int> OnFilterClickedCommand { get; private set; }
        #endregion

        #region 依赖
        private SoldierService mSoldierService;
        private DamageBonusService mDamageBonusService;
        #endregion

        private System.Action<float, float> mDamageBonusChangedHandler;
        private System.Action<object> mCardChangedHandler;

        #region 配置数据
        private TbCardBaseConfig mCardBaseConfig;
        private TbSoldierBattleConfig mSoldierBattleConfig;
        #endregion
        public override UserToolData GetUserToolConfig()
        {
            return GlobalFunc.GetUserResourceConfig(PageType.HeroPage);
        }
        public override void OnCreate()
        {
            base.OnCreate();
            mSoldierService = GameServiceManager.Get<SoldierService>();

            mCardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            mSoldierBattleConfig = DataConfigManager.Tables.TbSoldierBattleConfig;

            OnFilterClickedCommand = new Command<int>(OnFilterClicked);

            mDamageBonusService = GameServiceManager.Get<DamageBonusService>();

            SoldierList.Value = new List<PlayerCard>();
            FilteredSoldierList.Value = new List<PlayerCard>();

            InitSoldierList();
            ApplyFilter();

            BindDamageBonus();
            BindCardChange();
        }

        public override void OnDestroy()
        {
            UnbindCardChange();
            UnbindDamageBonus();
            base.OnDestroy();
            SoldierList.Value?.Clear();
            FilteredSoldierList.Value?.Clear();
            CurrentFilter.Value = null;
        }

        private void BindDamageBonus()
        {
            if (mDamageBonusService == null) return;

            mDamageBonusService.RefreshDamageBonus();
            UpdateDamageLevelText(mDamageBonusService.TotalDamageBonus.Value);

            mDamageBonusChangedHandler = (_, newVal) => UpdateDamageLevelText(newVal);
            mDamageBonusService.TotalDamageBonus.OnValueChanged += mDamageBonusChangedHandler;
        }

        private void UnbindDamageBonus()
        {
            if (mDamageBonusService != null && mDamageBonusChangedHandler != null)
            {
                mDamageBonusService.TotalDamageBonus.OnValueChanged -= mDamageBonusChangedHandler;
                mDamageBonusChangedHandler = null;
            }
        }

        /// <summary>
        /// 订阅卡片变化事件，获得新英雄/卡牌数据变更时自动刷新列表
        /// </summary>
        private void BindCardChange()
        {
            mCardChangedHandler = _ => RefreshSoldierList();
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_CHANGE, mCardChangedHandler);
        }

        private void UnbindCardChange()
        {
            if (mCardChangedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_CARD_CHANGE, mCardChangedHandler);
                mCardChangedHandler = null;
            }
        }

        private void UpdateDamageLevelText(float bonusValue)
        {
            DamageLevelText.Value = $"{bonusValue * 100f:F1}%";
        }

        private void InitSoldierList()
        {
            var list = new List<PlayerCard>();
            foreach (var cfg in mCardBaseConfig.DataList)
            {
                if (cfg.Type != CardType.Soldier)
                    continue;

                var cardInfo = Global.GetCardById(cfg.Id, new PlayerCard()
                {
                    Lv = 1,
                    IsHave = false,
                    Config = cfg,
                    CardId = cfg.Id,
                    ObjData = new CardDataStruct(),
                });
                list.Add(cardInfo);
            }
            SoldierList.Value = list;
        }

        private void OnFilterClicked(int index)
        {
            if (index == 0)
            {
                CurrentFilter.Value = null;
            }
            else
            {
                CurrentFilter.Value = (CareerType)(index - 1);
            }
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var source = SoldierList.Value;
            if (source == null) return;

            List<PlayerCard> filtered;
            if (CurrentFilter.Value == null)
            {
                filtered = source;
            }
            else
            {
                filtered = source.Where(soldier =>
                {
                    var soldierCfg = mSoldierBattleConfig.GetOrDefault(soldier.CardId);
                    return soldierCfg != null && soldierCfg.CarType == CurrentFilter.Value;
                }).ToList();
            }

            FilteredSoldierList.Value = filtered;

            var haveCount = filtered.Count(s => s.IsHave);
            var totalCount = filtered.Count;
            CardNumText.Value = $"{haveCount}/{totalCount}";
        }

        public void RefreshSoldierList()
        {
            InitSoldierList();
            ApplyFilter();
        }
    }
}