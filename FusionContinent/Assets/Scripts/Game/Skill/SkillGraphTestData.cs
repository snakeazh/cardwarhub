using FusionContinent.Framework.Graph;
using FusionContinent.Skill.Config;
using UnityEngine;

namespace FusionContinent.Skill
{
    /// <summary>
    /// 技能图测试数据 - 用于创建测试用的技能配置
    /// </summary>
    public static class SkillGraphTestData
    {
        /// <summary>
        /// 创建火球术测试技能
        /// </summary>
        public static SkillGraphConfig CreateFireBallSkill()
        {
            var config = ScriptableObject.CreateInstance<SkillGraphConfig>();

            // 创建节点
            var castNode = SkillNodeConfig.Create("Cast", new Vector2(100, 200));
            castNode.SetParam("animation", "Attack");
            castNode.SetParam("cast_time", "0.3");

            var telegraphNode = SkillNodeConfig.Create("Telegraph", new Vector2(300, 200));
            telegraphNode.SetParam("shape", "Circle");
            telegraphNode.SetParam("duration", "0.5");
            telegraphNode.SetParam("range", "3");
            telegraphNode.SetParam("color", "#FF4400");

            var projectileNode = SkillNodeConfig.Create("Projectile", new Vector2(500, 200));
            projectileNode.SetParam("prefab", "Prefabs/Skills/FireBall");
            projectileNode.SetParam("speed", "15");
            projectileNode.SetParam("damage", "100");
            projectileNode.SetParam("damage_type", "Magic");
            projectileNode.SetParam("trajectory", "Straight");

            var effectNode = SkillNodeConfig.Create("Effect", new Vector2(700, 200));
            effectNode.SetParam("vfx", "VFX/FireExplosion");
            effectNode.SetParam("sfx", "Audio/FireExplosion");
            effectNode.SetParam("position", "Target");

            var damageNode = SkillNodeConfig.Create("AoeDamage", new Vector2(900, 200));
            damageNode.SetParam("shape", "Circle");
            damageNode.SetParam("range", "3");
            damageNode.SetParam("damage", "80");
            damageNode.SetParam("damage_type", "Magic");

            var soundNode = SkillNodeConfig.Create("Sound", new Vector2(900, 100));
            soundNode.SetParam("sound", "Audio/Impact");

            // 添加节点到配置
            config.AddNode(castNode);
            config.AddNode(telegraphNode);
            config.AddNode(projectileNode);
            config.AddNode(effectNode);
            config.AddNode(damageNode);
            config.AddNode(soundNode);

            // 创建连线
            config.AddEdge(new EdgeConfig(castNode.NodeId, "exec_out", telegraphNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(telegraphNode.NodeId, "exec_out", projectileNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(projectileNode.NodeId, "exec_out", effectNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(effectNode.NodeId, "exec_out", damageNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(damageNode.NodeId, "exec_out", soundNode.NodeId, "exec_in"));

            return config;
        }

        /// <summary>
        /// 创建闪电链测试技能
        /// </summary>
        public static SkillGraphConfig CreateLightningChainSkill()
        {
            var config = ScriptableObject.CreateInstance<SkillGraphConfig>();

            var castNode = SkillNodeConfig.Create("Cast", new Vector2(100, 200));
            castNode.SetParam("animation", "Magic");
            castNode.SetParam("cast_time", "0.5");

            var effectNode = SkillNodeConfig.Create("Effect", new Vector2(300, 200));
            effectNode.SetParam("vfx", "VFX/Lightning");
            effectNode.SetParam("sfx", "Audio/Lightning");
            effectNode.SetParam("position", "Target");

            var damageNode = SkillNodeConfig.Create("AoeDamage", new Vector2(500, 200));
            damageNode.SetParam("shape", "Circle");
            damageNode.SetParam("range", "2");
            damageNode.SetParam("damage", "60");
            damageNode.SetParam("damage_type", "Magic");

            var delayNode = SkillNodeConfig.Create("Delay", new Vector2(700, 200));
            delayNode.SetParam("duration", "0.3");

            var buffNode = SkillNodeConfig.Create("ApplyBuff", new Vector2(900, 200));
            buffNode.SetParam("buff_id", "1001"); // 麻痹Buff
            buffNode.SetParam("target", "Target");
            buffNode.SetParam("duration", "2");

            config.AddNode(castNode);
            config.AddNode(effectNode);
            config.AddNode(damageNode);
            config.AddNode(delayNode);
            config.AddNode(buffNode);

            config.AddEdge(new EdgeConfig(castNode.NodeId, "exec_out", effectNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(effectNode.NodeId, "exec_out", damageNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(damageNode.NodeId, "exec_out", delayNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(delayNode.NodeId, "exec_out", buffNode.NodeId, "exec_in"));

            return config;
        }

        /// <summary>
        /// 创建治疗术测试技能
        /// </summary>
        public static SkillGraphConfig CreateHealSkill()
        {
            var config = ScriptableObject.CreateInstance<SkillGraphConfig>();

            var castNode = SkillNodeConfig.Create("Cast", new Vector2(100, 200));
            castNode.SetParam("animation", "Heal");
            castNode.SetParam("cast_time", "0.5");

            var effectNode = SkillNodeConfig.Create("Effect", new Vector2(300, 200));
            effectNode.SetParam("vfx", "VFX/Heal");
            effectNode.SetParam("sfx", "Audio/Heal");
            effectNode.SetParam("position", "Target");

            var delayNode = SkillNodeConfig.Create("Delay", new Vector2(500, 200));
            delayNode.SetParam("duration", "0.2");

            // 简单治疗效果（用AoeDamage的负值模拟，或者后续添加专门的HealHandler）
            var healEffectNode = SkillNodeConfig.Create("Effect", new Vector2(700, 200));
            healEffectNode.SetParam("vfx", "VFX/HealSparkle");

            config.AddNode(castNode);
            config.AddNode(effectNode);
            config.AddNode(delayNode);
            config.AddNode(healEffectNode);

            config.AddEdge(new EdgeConfig(castNode.NodeId, "exec_out", effectNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(effectNode.NodeId, "exec_out", delayNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(delayNode.NodeId, "exec_out", healEffectNode.NodeId, "exec_in"));

            LogTool.Log("[SkillGraphTestData] 创建治疗术测试技能完成");
            return config;
        }

        /// <summary>
        /// 创建简单普攻测试技能
        /// </summary>
        public static SkillGraphConfig CreateNormalAttackSkill()
        {
            var config = ScriptableObject.CreateInstance<SkillGraphConfig>();

            var castNode = SkillNodeConfig.Create("Cast", new Vector2(100, 200));
            castNode.SetParam("animation", "Attack");
            castNode.SetParam("cast_time", "0.2");

            var damageNode = SkillNodeConfig.Create("AoeDamage", new Vector2(300, 200));
            damageNode.SetParam("shape", "Circle");
            damageNode.SetParam("range", "1.5");
            damageNode.SetParam("damage", "30");
            damageNode.SetParam("damage_type", "Physical");

            var effectNode = SkillNodeConfig.Create("Effect", new Vector2(500, 200));
            effectNode.SetParam("sfx", "Audio/Hit");
            effectNode.SetParam("position", "Target");

            config.AddNode(castNode);
            config.AddNode(damageNode);
            config.AddNode(effectNode);

            config.AddEdge(new EdgeConfig(castNode.NodeId, "exec_out", damageNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(damageNode.NodeId, "exec_out", effectNode.NodeId, "exec_in"));

            LogTool.Log("[SkillGraphTestData] 创建普通攻击测试技能完成");
            return config;
        }

        /// <summary>
        /// 创建瞬袭斩测试技能: Cast → Telegraph → Dash → Slash
        /// </summary>
        public static SkillGraphConfig CreateSwiftAssaultSkill()
        {
            var config = ScriptableObject.CreateInstance<SkillGraphConfig>();

            var castNode = SkillNodeConfig.Create("Cast", new Vector2(100, 200));
            castNode.SetParam("animation", "Attack");
            castNode.SetParam("cast_time", "0.1");

            var telegraphNode = SkillNodeConfig.Create("Telegraph", new Vector2(250, 200));
            telegraphNode.SetParam("shape", "Circle");
            telegraphNode.SetParam("range", "2");
            telegraphNode.SetParam("duration", "0.2");

            var dashNode = SkillNodeConfig.Create("Dash", new Vector2(400, 200));
            dashNode.SetParam("distance", "200");
            dashNode.SetParam("speed", "10");
            dashNode.SetParam("easing", "OutQuad");

            var slashNode = SkillNodeConfig.Create("Slash", new Vector2(550, 200));
            slashNode.SetParam("prefab", "ani_xl_jn9_02");
            slashNode.SetParam("scale", "100");
            slashNode.SetParam("duration", "1");
            slashNode.SetParam("count", "1");
            slashNode.SetParam("interval", "0.1");
            slashNode.SetParam("direction_mode", "Same");
            slashNode.SetParam("knockback", "50");

            var effectNode = SkillNodeConfig.Create("Effect", new Vector2(700, 200));
            effectNode.SetParam("vfx", "VFX/SlashHit");
            effectNode.SetParam("sfx", "Audio/Slash");
            effectNode.SetParam("position", "Target");

            config.AddNode(castNode);
            config.AddNode(telegraphNode);
            config.AddNode(dashNode);
            config.AddNode(slashNode);
            config.AddNode(effectNode);

            config.AddEdge(new EdgeConfig(castNode.NodeId, "exec_out", telegraphNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(telegraphNode.NodeId, "exec_out", dashNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(dashNode.NodeId, "exec_out", slashNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(slashNode.NodeId, "exec_out", effectNode.NodeId, "exec_in"));

            LogTool.Log("[SkillGraphTestData] 创建瞬袭斩测试技能完成");
            return config;
        }

        /// <summary>
        /// 创建多重斩测试技能: Cast → Loop(3) → Slash
        /// </summary>
        public static SkillGraphConfig CreateMultiSlashSkill()
        {
            var config = ScriptableObject.CreateInstance<SkillGraphConfig>();

            var castNode = SkillNodeConfig.Create("Cast", new Vector2(100, 200));
            castNode.SetParam("animation", "Attack");
            castNode.SetParam("cast_time", "0.1");

            var loopNode = SkillNodeConfig.Create("Loop", new Vector2(300, 200));
            loopNode.SetParam("count", "3");
            loopNode.SetParam("interval", "0.15");

            var slashNode = SkillNodeConfig.Create("Slash", new Vector2(500, 200));
            slashNode.SetParam("prefab", "ani_xl_jn9_02");
            slashNode.SetParam("scale", "100");
            slashNode.SetParam("duration", "0.8");
            slashNode.SetParam("count", "1");
            slashNode.SetParam("interval", "0.1");
            slashNode.SetParam("direction_mode", "Rotate");
            slashNode.SetParam("angle_offset", "15");

            config.AddNode(castNode);
            config.AddNode(loopNode);
            config.AddNode(slashNode);

            // Cast -> Loop
            config.AddEdge(new EdgeConfig(castNode.NodeId, "exec_out", loopNode.NodeId, "exec_in"));
            // Loop 的 body -> Slash
            config.AddEdge(new EdgeConfig(loopNode.NodeId, "body", slashNode.NodeId, "exec_in"));

            LogTool.Log("[SkillGraphTestData] 创建多重斩测试技能完成");
            return config;
        }

        /// <summary>
        /// 创建爆炎斩测试技能: Cast → Loop(8) → Fork(3) → Slash
        /// </summary>
        public static SkillGraphConfig CreateFlamingBurstSkill()
        {
            var config = ScriptableObject.CreateInstance<SkillGraphConfig>();

            var castNode = SkillNodeConfig.Create("Cast", new Vector2(100, 200));
            castNode.SetParam("animation", "Attack");
            castNode.SetParam("cast_time", "0.2");

            var telegraphNode = SkillNodeConfig.Create("Telegraph", new Vector2(250, 200));
            telegraphNode.SetParam("shape", "Circle");
            telegraphNode.SetParam("range", "3");
            telegraphNode.SetParam("duration", "0.3");

            var loopNode = SkillNodeConfig.Create("Loop", new Vector2(400, 200));
            loopNode.SetParam("count", "8");
            loopNode.SetParam("interval", "0.2");

            var forkNode = SkillNodeConfig.Create("Fork", new Vector2(550, 200));
            forkNode.SetParam("count", "3");

            // 三个分叉都连接到 Slash
            var slashLeft = SkillNodeConfig.Create("Slash", new Vector2(700, 100));
            slashLeft.SetParam("prefab", "ani_xl_jn9_02");
            slashLeft.SetParam("scale", "80");
            slashLeft.SetParam("duration", "0.5");
            slashLeft.SetParam("count", "1");
            slashLeft.SetParam("direction_mode", "Fork");
            slashLeft.SetParam("fork_angle", "15");

            var slashCenter = SkillNodeConfig.Create("Slash", new Vector2(700, 200));
            slashCenter.SetParam("prefab", "ani_xl_jn9_02");
            slashCenter.SetParam("scale", "100");
            slashCenter.SetParam("duration", "0.5");
            slashCenter.SetParam("count", "1");
            slashCenter.SetParam("direction_mode", "Same");

            var slashRight = SkillNodeConfig.Create("Slash", new Vector2(700, 300));
            slashRight.SetParam("prefab", "ani_xl_jn9_02");
            slashRight.SetParam("scale", "80");
            slashRight.SetParam("duration", "0.5");
            slashRight.SetParam("count", "1");
            slashRight.SetParam("direction_mode", "Fork");
            slashRight.SetParam("fork_angle", "-15");

            config.AddNode(castNode);
            config.AddNode(telegraphNode);
            config.AddNode(loopNode);
            config.AddNode(forkNode);
            config.AddNode(slashLeft);
            config.AddNode(slashCenter);
            config.AddNode(slashRight);

            config.AddEdge(new EdgeConfig(castNode.NodeId, "exec_out", telegraphNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(telegraphNode.NodeId, "exec_out", loopNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(loopNode.NodeId, "body", forkNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(forkNode.NodeId, "fork_0", slashLeft.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(forkNode.NodeId, "fork_1", slashCenter.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(forkNode.NodeId, "fork_2", slashRight.NodeId, "exec_in"));

            LogTool.Log("[SkillGraphTestData] 创建爆炎斩测试技能完成");
            return config;
        }

        /// <summary>
        /// 创建条件分支测试技能: Cast → Branch → [True]Slash → [False]Effect
        /// </summary>
        public static SkillGraphConfig CreateConditionalSkill()
        {
            var config = ScriptableObject.CreateInstance<SkillGraphConfig>();

            var castNode = SkillNodeConfig.Create("Cast", new Vector2(100, 200));
            castNode.SetParam("animation", "Attack");
            castNode.SetParam("cast_time", "0.1");

            var branchNode = SkillNodeConfig.Create("Branch", new Vector2(300, 200));
            branchNode.SetParam("condition", "TargetIsMonster");
            branchNode.SetParam("probability", "0.5");

            var slashNode = SkillNodeConfig.Create("Slash", new Vector2(500, 100));
            slashNode.SetParam("prefab", "ani_xl_jn9_02");
            slashNode.SetParam("scale", "100");
            slashNode.SetParam("duration", "1");
            slashNode.SetParam("count", "1");
            slashNode.SetParam("direction_mode", "Same");

            var effectNode = SkillNodeConfig.Create("Effect", new Vector2(500, 300));
            effectNode.SetParam("vfx", "VFX/Miss");
            effectNode.SetParam("sfx", "Audio/Miss");
            effectNode.SetParam("position", "Target");

            config.AddNode(castNode);
            config.AddNode(branchNode);
            config.AddNode(slashNode);
            config.AddNode(effectNode);

            config.AddEdge(new EdgeConfig(castNode.NodeId, "exec_out", branchNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(branchNode.NodeId, "true", slashNode.NodeId, "exec_in"));
            config.AddEdge(new EdgeConfig(branchNode.NodeId, "false", effectNode.NodeId, "exec_in"));

            LogTool.Log("[SkillGraphTestData] 创建条件分支测试技能完成");
            return config;
        }
    }
}
