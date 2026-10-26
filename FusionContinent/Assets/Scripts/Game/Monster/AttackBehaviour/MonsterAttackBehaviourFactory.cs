using System;
using System.Collections.Generic;
using Config;
using FusionContinent;

namespace FusionContinent.Monster.AttackBehaviour
{
    public static class MonsterAttackBehaviourFactory
    {
        private static readonly Dictionary<AttackBehaviourType, Func<IAttackBehaviour>> Registry =
            new Dictionary<AttackBehaviourType, Func<IAttackBehaviour>>();

        static MonsterAttackBehaviourFactory()
        {
            Register(AttackBehaviourType.MeleeImpact, () => new MeleeImpactAttackBehaviour());
            Register(AttackBehaviourType.RangedBullet, () => new LvDouAttackBehaviour());
            Register(AttackBehaviourType.FlameBreath, () => new DragonKingAttackBehaviour());
            Register(AttackBehaviourType.AlienLaser, () => new AlienLaserAttackBehaviour());
            Register(AttackBehaviourType.HamadryadParticle, () => new HamadryadAttackBehaviour());
            Register(AttackBehaviourType.JungleKnightDual, () => new SenLingAttackBehaviour());
            Register(AttackBehaviourType.MushroomBossMelee, () => new MushroomBossAttackBehaviour());
            Register(AttackBehaviourType.TargetCenterStrike, () => new TargetCenterAttackBehaviour());
        }

        public static void Register(AttackBehaviourType type, Func<IAttackBehaviour> factory)
        {
            Registry[type] = factory;
        }

        public static IAttackBehaviour Create(MonsterBattleConfig config)
        {
            var behaviourType = ResolveBehaviourType(config);
            if (!Registry.TryGetValue(behaviourType, out var factory))
            {
                LogTool.LogError(
                    "[MonsterAttackBehaviourFactory] 未注册 AttackBehaviourType=" + behaviourType
                    + ", MonsterId=" + config.Id
                    + ", MonsterType=" + config.Type
                    + ", AtkType=" + config.AtkType);
#if UNITY_EDITOR
                throw new InvalidOperationException(
                    "未注册 AttackBehaviourType=" + behaviourType + ", MonsterId=" + config.Id);
#else
                throw new InvalidOperationException(
                    "未注册 AttackBehaviourType=" + behaviourType + ", MonsterId=" + config.Id);
#endif
            }

            return factory();
        }

        private static AttackBehaviourType ResolveBehaviourType(MonsterBattleConfig config)
        {
            var behaviourType = ResolveByMonsterType(config.Type);
            if (behaviourType != AttackBehaviourType.None)
            {
                return behaviourType;
            }

            return ResolveByAtkType(config);
        }

        private static AttackBehaviourType ResolveByMonsterType(MonsterType monsterType)
        {
            switch (monsterType)
            {
                case MonsterType.Hamadryad:
                    return AttackBehaviourType.HamadryadParticle;
                case MonsterType.JungleKnight:
                    return AttackBehaviourType.JungleKnightDual;
                case MonsterType.MushroomGiant:
                    return AttackBehaviourType.MushroomBossMelee;
                case MonsterType.DragonKing:
                    return AttackBehaviourType.FlameBreath;
                default:
                    return AttackBehaviourType.None;
            }
        }

        private static AttackBehaviourType ResolveByAtkType(MonsterBattleConfig config)
        {
            switch (config.AtkType)
            {
                case AtkType.ZhuangJi:
                    return AttackBehaviourType.MeleeImpact;
                case AtkType.Shooting:
                    return AttackBehaviourType.RangedBullet;
                case AtkType.Flame:
                    return AttackBehaviourType.FlameBreath;
                case AtkType.TargetCenter:
                    return AttackBehaviourType.TargetCenterStrike;
                case AtkType.Special:
                    if (config.Type == MonsterType.Alien)
                    {
                        return AttackBehaviourType.AlienLaser;
                    }

                    return AttackBehaviourType.MeleeImpact;
                default:
                    LogTool.LogError(
                        "[MonsterAttackBehaviourFactory] 未知 AtkType=" + config.AtkType
                        + ", MonsterId=" + config.Id
                        + ", MonsterType=" + config.Type);
                    throw new InvalidOperationException(
                        "未知 AtkType=" + config.AtkType + ", MonsterId=" + config.Id);
            }
        }
    }
}
