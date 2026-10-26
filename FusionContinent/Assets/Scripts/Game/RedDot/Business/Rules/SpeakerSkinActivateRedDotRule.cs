using Config;
using FusionContinent;
using FusionContinent.Model;
using Game.Player;
using Game.RedDot;
using UIFramework.Services;

namespace Game.RedDot.Business.Rules
{
    /// <summary>
    /// 轮语者皮肤可激活红点：存在未激活且材料足够的皮肤时亮。
    /// 驱动 SpeakWhellPanel SkinBtn，并聚合到 NavBarSpeaker（GameMainPanel Btn(2) Prefab 自带 Binder）。
    /// </summary>
    public sealed class SpeakerSkinActivateRedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.SpeakerSkinActivateBtn;

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Subscribe(MessagePublisher.SPEAKER_SKIN_DATA_CHANGED, OnDirty);
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_RED_TYPE + NodeType, OnDirty);
            Global.Publisher.Unsubscribe(MessagePublisher.SPEAKER_SKIN_DATA_CHANGED, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Speaker, false))
            {
                return 0;
            }

            if (!ModuleFunction.CheckOpen(ModuleFunctionType.PlayerAvatar, false))
            {
                return 0;
            }

            SpeakerService speakerService = GameServiceManager.Get<SpeakerService>();
            if (speakerService == null)
            {
                return 0;
            }

            return speakerService.CountActivatableSkins();
        }
    }
}
