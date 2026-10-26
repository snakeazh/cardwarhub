using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.Player;
using Game.RedDot;
using UIFramework.Services;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 轮语者神器/武器可养成红点：已激活且升星材料足够时亮。
    /// 聚合到 SpeakerWeaponBtn（ArtifactBtn）与 NavBarSpeaker。
    /// </summary>
    public sealed class SpeakerWeaponUpgradeRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerWeaponUpgradeBtn;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.WEAPON_DATA_CHANGED, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.WEAPON_DATA_CHANGED, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Speaker, false))
            {
                return 0;
            }

            WeaponService weaponService = GameServiceManager.Get<WeaponService>();
            if (weaponService == null)
            {
                return 0;
            }

            return weaponService.CountCultivatableWeapons();
        }
    }
}
