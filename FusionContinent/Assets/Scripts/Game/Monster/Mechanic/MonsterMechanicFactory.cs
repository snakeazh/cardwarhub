using System;
using System.Collections.Generic;
using Config;

namespace FusionContinent.Monster.Mechanic
{
    public static class MonsterMechanicFactory
    {
        public static List<IMonsterMechanic> CreateAll(MonsterCard owner, MonsterBattleConfig battleConfig)
        {
            var result = new List<IMonsterMechanic>();
            int mechanismId = battleConfig.MonsterMechanismId;
            if (mechanismId <= 0)
            {
                return result;
            }

            var mechanicConfig = DataConfigManager.Tables.TbMonsterMechanicConfig.GetOrDefault(mechanismId);
            if (mechanicConfig == null)
            {
                LogTool.LogError(
                    "[MonsterMechanicFactory] Missing mechanic group. MechanicGroupId="
                    + mechanismId
                    + ", MonsterId=" + battleConfig.Id);
                throw new InvalidOperationException(
                    "Missing mechanic group. MechanicGroupId=" + mechanismId
                    + ", MonsterId=" + battleConfig.Id);
            }

            LogTool.Log(
                "[MonsterMechanic] 创建机制组"
                + " MonsterId=" + battleConfig.Id
                + " MechanismId=" + mechanismId
                + " Desc=" + mechanicConfig.Desc
                + " RuleCount=" + mechanicConfig.RuleIds.Count
                + " RuleIds=" + string.Join("|", mechanicConfig.RuleIds));

            var mechanic = new RuleBasedMonsterMechanic(mechanicConfig);
            mechanic.OnInit(owner);
            result.Add(mechanic);
            return result;
        }
    }
}
