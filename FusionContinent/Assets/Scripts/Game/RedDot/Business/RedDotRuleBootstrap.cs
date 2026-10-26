using Game.Challenge.RedDot;
using Game.RedDot;
using Game.RedDot.Business.Rules;
using Game.RedDot.Rules;
using Game.Equip.RedDot;
using Game.Soldier.RedDot;

namespace Game.RedDot.Business
{
    public static class RedDotRuleBootstrap
    {
        public static void RegisterAll(RedDotRuleRegistry registry)
        {
            SoldierPanelRedDotScope.RegisterRules(registry);

            EquipUpGradePanelRedDotScope.RegisterRules(registry);

            SpeakWhellPanelRedDotScope.RegisterRules(registry);

            FusionGemCompoundPanelRedDotScope.RegisterRules(registry);

            GeneralCardConbinePanelRedDotScope.RegisterRules(registry);

            MonsterAtlasPanelRedDotScope.RegisterRules(registry);

            registry.RegisterGlobal(RedNodeType.MainPageAFKBtn, () => new MainAfkRewardRedDotRule());
            registry.RegisterGlobal(RedNodeType.AFKPageClaimBtn, () => new MainAfkRewardRedDotRule());

            registry.RegisterGlobal(RedNodeType.MainPageTaskBtn, () => new TaskDayReceiveRedDotRule());
            registry.RegisterGlobal(RedNodeType.TaskPageDayBtn, () => new TaskDayReceiveRedDotRule());

            registry.RegisterGlobal(RedNodeType.MonsterAtlasBtn, () => new MonsterAtlasRedDotRule());
            registry.RegisterGlobal(RedNodeType.StationReceiveBtn, () => new StationReceiveRedDotRule());
            registry.RegisterGlobal(RedNodeType.ArcCardCombine, () => new ArcCardCombineRedDotRule());
            registry.RegisterGlobal(RedNodeType.SoliderCanUpgrade, () => new SoliderDevelopRedDotRule());
            registry.RegisterGlobal(RedNodeType.SoliderCanUpgrade, () => new SoliderUpgradeRedDotRule());
            registry.RegisterGlobal(RedNodeType.SoliderCanActivate, () => new SoliderActivateRedDotRule());
            registry.RegisterGlobal(RedNodeType.SoliderCanAwakening, () => new SoliderAwakeningRedDotRule());
            registry.RegisterGlobal(RedNodeType.TalentUpgradeBtn, () => new TalentUpgradeRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerEquipForgeRedDot, () => new SpeakerEquipForgeRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerEquipRefineRedDot, () => new SpeakerEquipRefineRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerEquipReplaceRedDot, () => new SpeakerEquipReplaceRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerFusionGemReplaceRedDot, () => new SpeakerFusionGemReplaceRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerFusionGemBreakthroughRedDot, () => new SpeakerFusionGemBreakthroughRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerWeaponActivateBtn, () => new SpeakerWeaponActivateRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerWeaponUpgradeBtn, () => new SpeakerWeaponUpgradeRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerSkinActivateBtn, () => new SpeakerSkinActivateRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerHeroSeatActivateBtn, () => new SpeakerHeroSeatActivateRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerHeroSeatUpgradeBtn, () => new SpeakerHeroSeatUpgradeRedDotRule());
            registry.RegisterGlobal(RedNodeType.SpeakerHeroSeatWallUpgradeBtn, () => new SpeakerHeroSeatWallUpgradeRedDotRule());
        }
    }
}
