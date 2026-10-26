using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using UIFramework;
using UIFramework.Services;

namespace Game.Soldier
{
    /// <summary>
    /// 增伤计算服务
    /// 公式：总增伤 = 拥有英灵数量 × GetNewHeroGetAttributes + Σ(每个英灵等级-1) × LevelUpHeroGetAttributes
    /// 自动监听英灵升级事件实时更新
    /// </summary>
    public class DamageBonusService : GameServiceBase
    {
        private TbPlayerConst mPlayerConst;

        /// <summary>拥有英灵数量 A</summary>
        public int HeroCount { get; private set; }

        /// <summary>所有英灵的 (Lv-1) 总和 B</summary>
        public int TotalLevelContribution { get; private set; }

        /// <summary>获得新英灵增伤值</summary>
        public float NewHeroBonus { get; private set; }

        /// <summary>英灵升级增伤值</summary>
        public float LevelUpBonus { get; private set; }

        /// <summary>总增伤值（暴露给外界绑定）</summary>
        public BindableProperty<float> TotalDamageBonus { get; } = new BindableProperty<float>(0);

        private System.Action<object> mCardUpgradedHandler;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            mPlayerConst = DataConfigManager.Tables.TbPlayerConst;
            BindEvents();
        }

        private void BindEvents()
        {
            if (mCardUpgradedHandler != null) return;

            mCardUpgradedHandler = _ => RefreshDamageBonus();
            Global.Publisher.Subscribe(MessagePublisher.CARD_UPGRADED, mCardUpgradedHandler);
        }

        private void UnbindEvents()
        {
            if (mCardUpgradedHandler != null)
            {
                Global.Publisher.Unsubscribe(MessagePublisher.CARD_UPGRADED, mCardUpgradedHandler);
                mCardUpgradedHandler = null;
            }
        }

        public override void OnDestroy()
        {
            UnbindEvents();
            base.OnDestroy();
        }

        /// <summary>
        /// 刷新增伤数值（外部可主动调用，也会在英灵升级后自动触发）
        /// </summary>
        public void RefreshDamageBonus()
        {
            if (Global.PlayerCards == null || Global.PlayerCards.Count == 0)
            {
                HeroCount = 0;
                TotalLevelContribution = 0;
                NewHeroBonus = 0;
                LevelUpBonus = 0;
                TotalDamageBonus.Value = 0;
                return;
            }

            var heroes = Global.PlayerCards.Values
                .Where(c => c != null && c.IsHave && c.Config?.Type == CardType.Soldier)
                .ToList();

            HeroCount = heroes.Count;
            TotalLevelContribution = heroes.Sum(h => h.Lv > 0 ? h.Lv - 1 : 0);

            NewHeroBonus = SumAttributes(mPlayerConst.GetNewHeroGetAttributes) * HeroCount;
            LevelUpBonus = SumAttributes(mPlayerConst.LevelUpHeroGetAttributes) * TotalLevelContribution;
            TotalDamageBonus.Value = NewHeroBonus + LevelUpBonus;
        }

        /// <summary>
        /// 将 AttributeStruct 列表的 Value 求和
        /// </summary>
        private static float SumAttributes(System.Collections.Generic.List<AttributeStruct> attrs)
        {
            if (attrs == null || attrs.Count == 0) return 0f;

            float sum = 0f;
            foreach (var attr in attrs)
            {
                sum += attr.Value;
            }
            return sum;
        }
    }
}
