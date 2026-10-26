using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.Player;
using Game.RedDot;
using UIFramework.Services;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 轮语者神器/武器可激活红点：存在未激活且材料足够的武器时亮。
    /// 驱动 SpeakWhellPanel ArtifactBtn，并聚合到 NavBarSpeaker。
    /// </summary>
    public sealed class SpeakerWeaponActivateRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerWeaponActivateBtn;

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

            return weaponService.CountActivatableWeapons();
        }
    }
}
