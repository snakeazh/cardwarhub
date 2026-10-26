using FusionContinent;
using UnityEngine;

public static class MonsterDeathHandler
{
    public static void HandleDeath(MonsterCard m)
    {
        Cleanup(m);
    }

    public static void Cleanup(MonsterCard m)
    {
        m.monsterData.monsterTweens.KillAllTweens();
        if (m.AttackBehaviour != null)
        {
            m.AttackBehaviour.OnCleanup(m);
        }
    }
}
