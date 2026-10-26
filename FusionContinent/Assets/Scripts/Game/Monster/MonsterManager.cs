using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using Game.Battle;
using UnityEngine;
using Unity.Profiling;

public class MonsterManager : MonoBehaviour
{
    public static MonsterManager Instance { get; private set; }

    public readonly List<MonsterCard> enemies = new List<MonsterCard>();
    
    private int currentUpdateIndex = 0;
    private const int MonstersPerFrame = 3; // 基础每帧分批数量
    private const float TargetFrameTime = 1f / 60f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            Instance = null;
            return;
        }
        Instance = this;
        if (GetComponent<MonsterSortManager>() == null)
        {
            gameObject.AddComponent<MonsterSortManager>();
        }

        EventBus.Subscribe<ProjectileHitEvent>(OnAddHitEvent);
    }

    /// <summary>
    /// 怪物位移专用 FixedUpdate：每只怪每物理帧更新一次，保证移动平滑且不受分批 Tick 影响。
    /// 状态机逻辑仍在 Update 中分批执行。
    /// </summary>
    private void FixedUpdate()
    {
        if (GamePlayManager.Default.IsPaused() || enemies.Count == 0)
        {
            return;
        }

        float moveDeltaTime = Time.fixedDeltaTime * Global.SpeedMultiplier.Value;
        for (int i = 0; i < enemies.Count; i++)
        {
            var monster = enemies[i];
            if (monster == null || !monster.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (monster.monsterData.monsterState == MonsterState.die || monster.hp <= 0)
            {
                continue;
            }

            monster.FixedMoveTick(moveDeltaTime);
        }
    }

    private void Update()
    {
        if (GamePlayManager.Default.IsPaused() || enemies.Count == 0)
            return;

        // 先做一轮轻量目标有效性校验，避免分批 Tick 时漏掉索敌刷新
        RefreshMonsterTargets();

        // 掉帧补偿：帧耗时越大，本帧处理怪物越多，降低单怪 Tick 间隔抖动
        var frameScale = Mathf.Max(1f, Time.deltaTime / TargetFrameTime);
        var monstersThisFrame = Mathf.Clamp(Mathf.CeilToInt(MonstersPerFrame * frameScale), MonstersPerFrame, enemies.Count);

        int processed = 0;
        while (processed < monstersThisFrame && enemies.Count > 0)
        {
            if (currentUpdateIndex >= enemies.Count)
            {
                currentUpdateIndex = 0;
            }

            var monster = enemies[currentUpdateIndex];
            if (monster != null && monster.gameObject.activeInHierarchy)
            {
                monster.Tick();
            }

            currentUpdateIndex++;
            processed++;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            var monster = enemies[i];
            if (monster == null || !monster.gameObject.activeInHierarchy)
                continue;

            if (monster.monsterData.monsterState == MonsterState.die || monster.hp <= 0)
                continue;

            monster.BuffController?.Update(Time.deltaTime);
        }
    }

    private void RefreshMonsterTargets()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            var monster = enemies[i];
            if (monster == null || !monster.gameObject.activeInHierarchy)
            {
                continue;
            }

            // 死亡怪物不参与目标刷新
            if (monster.monsterData.monsterState == MonsterState.die)
            {
                continue;
            }

            if (monster.targetSolidierCard == null || monster.targetSolidierCard.isDie)
            {
                monster.EngageSoldierCard(monster.transform.position);
            }
        }
    }

    public void Register(MonsterCard enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void Unregister(MonsterCard enemy)
    {
        enemies.Remove(enemy);
    }
    
    /// <summary>
    /// 获取当前场上存活怪物数量（排除未激活和死亡状态）。
    /// </summary>
    public int GetAliveEnemyCount()
    {
        var aliveCount = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            var monster = enemies[i];
            if (monster == null || !monster.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (monster.monsterData.monsterState == MonsterState.die)
            {
                continue;
            }

            aliveCount++;
        }

        return aliveCount;
    }

    /// <summary>
    /// 外部聚合属性附加上去
    /// </summary>
    public void AddNewAttr()
    {
        // 外部模块聚合属性加成

        for (int i = 0; i < enemies.Count; i++)
        {
            // 外部模块聚合属性加成
            foreach (var attr in GamePlayManager.Default.MonsterAttributes.GetAll())
            {
                foreach (var mod in attr.Value.GetModifiers().Values)
                {
                    enemies[i].Attributes.GetAttr(attr.Key).AddModifier(mod);
                    if (attr.Key == AttributeType.MonsterMoveSpeed)
                    {
                        enemies[i].UpdateMove();
                    }
                }
            }
        }
    }

    public void OnAddHitEvent(ProjectileHitEvent projectileHitEvent)
    {
        using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.DamagePipeline))
        {
            var monster = projectileHitEvent.monsterCard;
            if (monster == null) return;

            var context = projectileHitEvent.skillContext;
            if (context?.Caster is not SoldierCard caster)
            {
                LogTool.LogWarning("[MonsterManager] 攻击方不是 SoldierCard，无法造成伤害");
                return;
            }

            var skillCard = caster.GetSkillById(context.SkillId);
            if (skillCard == null)
            {
                LogTool.LogWarning($"[MonsterManager] 找不到技能ID: {context.SkillId}");
                return;
            }
        
            // 投射物命中：将 SkillContext 中的暴击预掷结果写入 DamageContext（替代旧 CloneSkillCarrying）
            monster.damageContext.HasPreRolledCrit = context.HasPreRolledCrit;
            monster.damageContext.PreRolledCrit = context.PreRolledCrit;
            monster.damageContext.SkipBattleRuleBeforeTrigger = context.SkipBattleRuleBeforeTrigger;
            BattleRuleCastDamageSession.CopyToDamageContext(context, monster.damageContext);
            SoldierCard casetrSoldier = context.Caster.gameObject.GetComponent<SoldierCard>();
            monster.damageContext.PhysicalBloodSucking =
                casetrSoldier.BaseAttributes.GetFinal(AttributeType.PhysicalBloodSucking);
            monster.damageContext.MagicBloodSucking =
                casetrSoldier.BaseAttributes.GetFinal(AttributeType.MagicBloodSucking);

            // 普通攻击额外伤害（由 FanProjectileHandler 设置）
            float damageInheritance = 1f;
            if (context.GetExtraData<object>("ExtraNormalAttackDamage") is float extraDamage && extraDamage != 0f)
            {
                damageInheritance += extraDamage;
            }

            monster.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                monster, skillCard, projectileHitEvent.hitpoint, damageInheritance, casetrSoldier, context));
        }
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<ProjectileHitEvent>(OnAddHitEvent);
    }
}