using System;
using System.Collections.Generic;
using System.Threading;
using Config;
using FusionContinent.Attribute;
using UnityEngine;

namespace FusionContinent.Skill.Handler
{
    /// <summary>
    /// 技能执行上下文
    /// </summary>
    public class SkillContext
    {
        /// <summary>
        /// 技能ID
        /// </summary>
        public int SkillId { get; set; }

        /// <summary>
        /// 技能等级
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 施法者
        /// </summary>
        public Component Caster { get; set; }

        /// <summary>
        /// 目标对象
        /// </summary>
        public Component Target { get; set; }

        /// <summary>
        /// 目标位置（拖拽释放时为世界坐标落点）
        /// </summary>
        public Vector3 TargetPosition { get; set; }

        /// <summary>施法者到释放点的拖拽距离（ExecuteToPoint 时写入）</summary>
        public float DragDistance { get; set; }

        /// <summary>拖拽方向（归一化，XY 平面）</summary>
        public Vector2 DragDirection { get; set; }

        /// <summary>是否已写入拖拽距离/方向</summary>
        public bool HasDragInfo { get; set; }

        /// <summary>
        /// 技能图配置
        /// </summary>
        public Config.SkillGraphConfig Config { get; set; }

        /// <summary>
        /// 取消令牌
        /// </summary>
        public CancellationTokenSource CancellationToken { get; set; }

        /// <summary>
        /// 特效/动画特效节点的随机偏移量（米）
        /// 第一个特效节点计算并缓存，后续特效节点复用，保持多个特效位置一致
        /// </summary>
        public Vector2 EffectOffset { get; set; }

        /// <summary>
        /// EffectOffset 是否已被当前技能执行计算过
        /// </summary>
        public bool HasEffectOffset { get; set; }

        /// <summary>
        /// 当前执行的节点ID
        /// </summary>
        public string CurrentNodeId { get; set; }

        /// <summary>
        /// 本次技能执行中创建的特效实例列表，用于 RecycleEffect 节点统一回收
        /// </summary>
        public List<UnityEngine.GameObject> TrackedEffects { get; } = new List<UnityEngine.GameObject>();

        /// <summary>
        /// AOE命中的目标列表（仅限MonsterCard）
        /// </summary>
        public List<MonsterCard> HitMonsters { get; } = new List<MonsterCard>();

        /// <summary>
        /// 命中/选取时记录的怪物 uniqueId 快照，用于对象池复用后校验目标身份。
        /// </summary>
        public Dictionary<MonsterCard, string> HitMonsterSnapshotIds { get; } = new Dictionary<MonsterCard, string>();

        /// <summary>
        /// 写入 HitMonsters 并记录当前 uniqueId 快照。
        /// </summary>
        public void RegisterHitMonster(MonsterCard monster)
        {
            if (monster == null)
            {
                return;
            }

            if (!HitMonsters.Contains(monster))
            {
                HitMonsters.Add(monster);
            }

            HitMonsterSnapshotIds[monster] = monster.uniqueId;
        }

        /// <summary>
        /// SelectTarget 节点写入的选取目标，供 ApplyBuff / Effect / Projectile / FanProjectile 等复用。
        /// </summary>
        public List<Component> SelectedTargets { get; } = new List<Component>();

        /// <summary>
        /// 本次技能执行中 SelectTarget 随机选取过的目标（用于循环内不重复随机）。
        /// </summary>
        public HashSet<Component> SelectTargetHistory { get; } = new HashSet<Component>();

        /// <summary>
        /// 额外数据
        /// </summary>
        private Dictionary<string, object> _extraData = new Dictionary<string, object>();

        /// <summary>
        /// 暴击预掷标记（替代旧 SkillCarrying.isCritical）。
        /// 由 SkillRuntime 在施法时写入，经投射物 SkillContext 传递到 MonsterManager → DamageContext。
        /// </summary>
        public bool HasPreRolledCrit { get; set; }

        /// <summary>预掷暴击结果；TakeDamage 命中时若 HasPreRolledCrit 为 true 则直接采用，避免二次掷骰</summary>
        public bool PreRolledCrit { get; set; }

        /// <summary>
        /// 连击/追加施法等二次攻击为 true 时，伤害管线跳过 OnAttackBefore / OnSkillBefore 等 Before 规则，避免递归连击。
        /// </summary>
        public bool SkipBattleRuleBeforeTrigger { get; set; }

        /// <summary>施法时刻规则写入的临时伤害修正（随 SkillContext 传递到多段/延迟命中）。</summary>
        public bool HasCastDamageModifier { get; set; }

        public AttributeType CastDamageModifierAttributeType { get; set; }

        public float CastDamageModifierBaseValue { get; set; }

        public float CastDamageModifierPreValue { get; set; }

        /// <summary>
        /// 奥术回音等重复施放为 true 时，技能图跳过 ApplyBuff 等增益节点，仅保留伤害/控制等非增益效果。
        /// </summary>
        public bool SkipBuffOnCast { get; set; }

        /// <summary>
        /// 命中模块 / ApplyBuff 节点注册的斩杀 BuffId，供同次命中伤害管线判定斩杀线。
        /// </summary>
        public List<int> HitExecuteBuffIds { get; } = new List<int>();

        /// <summary>
        /// 技能图开始执行时施法者 <see cref="SoldierAnimationController.CurrentPlayId"/> 快照。
        /// 在 Spine atk 触发、<see cref="SkillRuntime"/> 创建上下文后写入，供
        /// <see cref="ReturnAnimationHandler"/> 与 hide 等待态联合校验 Return 是否仍可播放。
        /// </summary>
        public int SkillAnimationPlayId { get; set; }

        public SkillContext()
        {
            CancellationToken = new CancellationTokenSource();
            TargetPosition = Vector3.zero;
        }

        /// <summary>
        /// 取消技能执行
        /// </summary>
        public void Cancel()
        {
            CancellationToken?.Cancel();
        }

        /// <summary>
        /// 获取额外数据
        /// </summary>
        public T GetExtraData<T>(string key) where T : class
        {
            return _extraData.TryGetValue(key, out var value) ? value as T : null;
        }

        /// <summary>
        /// 设置额外数据
        /// </summary>
        public void SetExtraData(string key, object value)
        {
            _extraData[key] = value;
        }

        /// <summary>
        /// 获取节点数据
        /// </summary>
        public T GetNodeData<T>(string nodeId, string key)
        {
            var fullKey = $"{nodeId}_{key}";
            return _extraData.TryGetValue(fullKey, out var value) ? (T)(object)value : default;
        }

        /// <summary>
        /// 设置节点数据
        /// </summary>
        public void SetNodeData(string nodeId, string key, object value)
        {
            var fullKey = $"{nodeId}_{key}";
            _extraData[fullKey] = value;
        }
    }
}
