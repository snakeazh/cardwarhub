using System;
using System.Collections.Generic;
using Config;
using FusionContinent.Buff;
using FusionContinent.Skill.Handler;
using FusionContinent.Utils;
using Manager;
using UnityEngine;
using AttributeType = Config.AttributeType;
using ValueType = Config.ValueType;
using SkillContext = FusionContinent.Skill.Handler.SkillContext;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 触发型Buff
    /// 监听 ConditionsTypeEvent 事件，判断触发时机后根据 EffectsConfig 执行效果
    /// </summary>
    public class TriggerBuff : IBuff
    {
        private BuffEffectsConfig _config;
        private int _triggerCount;
        private int _currentStack;
        private float _duration;
        private float _elapsed;
        private float _value;
        private int _stack = 1;
        private string _vfxPath;
        private GameObject _vfxInstance;
        private float _tickInterval;
        private float _cooldownAccumulator;
        private float _triggerInterval;
        private int _requiredTriggers = 1; // 触发次数达到多少才执行效果
        private int _accumulatedTriggers; // 累计触发次数（不重置）
        private bool _resetOnEffectExecute; // 执行效果后是否重置累计计数
        private float _accumulatedTriggerTimer; // 累计触发的计时器（用于超时重置）
        private const float AccumulatedTriggerTimeout = 2f; // 累计触发超时时间（秒）
        private bool _enableAccumulatedTimeout; // 是否启用累计超时重置
        private object _lastTarget; // 上一次触发时的目标（用于检测目标切换）
        private object _caster;
        private SkillContext _skillContext;
        private string _sourceKey;
        private object _owner;
        private float _skillCooldownTime; // 技能冷却时间（秒）
        private float _skillCooldownAccumulator; // 技能冷却倒计时
        
        public BuffType Type { get; set; } = (BuffType)100;

        public int BuffId { get; set; }

        public int Stack
        {
            get => _stack;
            set => _stack = value;
        }

        public int MaxStack { get; set; } = 1;

        public float TickInterval
        {
            get => _tickInterval;
            set => _tickInterval = value;
        }

        public string BuffVfx { get; set; }

        /// <summary>
        /// Buff持续时间类型
        /// </summary>
        public BuffDurationType DurationType { get; set; } = BuffDurationType.Duration;

        public float Value
        {
            get => _value;
            set => _value = value;
        }

        public ValueType ValueType { get; set; } = Config.ValueType.Number;

        public TakeDamageType DamageType { get; set; } = TakeDamageType.General;

        public object Caster
        {
            get => _caster;
            set => _caster = value;
        }

        public SkillContext SkillContext
        {
            get => _skillContext;
            set => _skillContext = value;
        }

        public string SourceKey
        {
            get => _sourceKey;
            set => _sourceKey = value;
        }

        public int TriggerCount => _triggerCount;

        /// <summary>
        /// 当前叠层层数（叠层触发用）
        /// </summary>
        public int CurrentStack
        {
            get => _currentStack;
            set => _currentStack = value;
        }

        /// <summary>
        /// 触发需要的层数（从配置读取）
        /// </summary>
       

        /// <summary>
        /// 触发后是否重置层数
        /// </summary>
        

        /// <summary>
        /// 执行效果后是否重置累计触发次数
        /// </summary>
        public bool ResetOnEffectExecute
        {
            get => _resetOnEffectExecute;
            set => _resetOnEffectExecute = value;
        }

        /// <summary>
        /// 累计触发次数
        /// </summary>
        public int AccumulatedTriggers => _accumulatedTriggers;

        /// <summary>
        /// 需要的触发次数（达到此次数才执行效果）
        /// </summary>
        public int RequiredTriggers
        {
            get => _requiredTriggers;
            set => _requiredTriggers = Mathf.Max(1, value);
        }

        /// <summary>
        /// Buff是否已结束
        /// </summary>
        public bool IsFinished
        {
            get
            {
                if (_duration < 0) return false;
                return _elapsed >= _duration;
            }
        }

        public TriggerBuff()
        {
            _sourceKey = Guid.NewGuid().ToString();
        }

        public TriggerBuff(BuffEffectsConfig config, float duration)
        {
            Init(config, duration);
        }

        /// <summary>
        /// 初始化触发Buff
        /// </summary>
        /// <param name="config">Buff配置表</param>
        /// <param name="duration">持续时间</param>
        public void Init(BuffEffectsConfig config, float duration)
        {
            _config = config;
            BuffId = config.ID;
            _duration = duration;
            _elapsed = 0f;
            _triggerCount = 0;
            _currentStack = 0;
            _cooldownAccumulator = 0f;
            _triggerInterval = 0f;
            _accumulatedTriggers = 0;
            _requiredTriggers = 1;
            _resetOnEffectExecute = false;
            _sourceKey = $"Trigger_{config.ID}";
        }

        /// <summary>
        /// 设置触发间隔（可选）
        /// </summary>
        public void SetTriggerInterval(float interval)
        {
            _triggerInterval = interval;
        }

        /// <summary>
        /// 设置累计触发机制：达到指定次数才执行效果
        /// </summary>
        /// <param name="requiredTriggers">需要触发多少次</param>
        /// <param name="resetOnExecute">执行效果后是否重置累计计数</param>
        public void SetAccumulatedTrigger(int requiredTriggers, bool resetOnExecute = false)
        {
            _requiredTriggers = Mathf.Max(1, requiredTriggers);
            _resetOnEffectExecute = resetOnExecute;
            _accumulatedTriggers = 0;
        }

        private Action<ConditionsTypeEvent> _onBuffTakeTypeHandler;

        /// <summary>
        /// Buff被添加时调用，订阅触发时机事件
        /// </summary>
        public void OnAdd(object owner)
        {
            if (_config == null)
            {
                LogTool.E($"[TriggerBuff] OnAdd: config is null");
                return;
            }

            _owner = owner;
            _triggerCount = 0;
            _currentStack = 0;
            _cooldownAccumulator = 0f;
            _accumulatedTriggers = 0;
            _accumulatedTriggerTimer = 0f;
            _lastTarget = null;

            _onBuffTakeTypeHandler = OnBuffTakeType;
            EventBus.Subscribe(_onBuffTakeTypeHandler);

            // 在 OnAdd 时处理 EffectsType.TakeSkill（学习技能）
            TryLearnSkillsOnAdd(owner);

            // 检查 TakeEffects 中是否有 EffectsType.TakeSpecial，并读取累计触发层数
            TryInitAccumulatedTriggerFromConfig();

            LogTool.L($"[TriggerBuff] OnAdd: owner={owner}, TakeType={_config.ConditionType}, ID={_config.ID}, Probability={_config.TriggerProbability}");
        }

        /// <summary>
        /// 在 OnAdd 时查找并学习 EffectsType.TakeSkill 类型的技能
        /// </summary>
        private void TryLearnSkillsOnAdd(object owner)
        {
            if (_config.TakeEffects == null || _config.TakeEffects.Count == 0)
                return;

            if (owner is not SoldierCard soldierCard)
                return;

            var tables = DataConfigManager.Tables;
            if (tables == null)
                return;

            foreach (var effectId in _config.TakeEffects)
            {
                var effectConfig = tables.TbEffectsConfig.GetOrDefault(effectId);
                if (effectConfig == null)
                    continue;

                if (effectConfig.Type == EffectsType.TakeSkill)
                {
                    var skillId = effectConfig.EffectsId;
                    if (skillId > 0)
                    {
                        soldierCard.LearnSkill(skillId, 1);
                        
                        LogTool.L($"[TriggerBuff] OnAdd: Learned skill {skillId} from EffectsType.TakeSkill and reset cooldown");
                    }
                }
            }
        }

        /// <summary>
        /// 检查 TakeEffects 中是否有 EffectsType.TakeSpecial 类型
        /// 如果有，从 Attributes 中读取 MonsterContinuousDamage3CauseTrueDamage 作为累计触发层数（写死为3）
        /// 同时读取 SkillCdTime 作为技能冷却时间
        /// </summary>
        private void TryInitAccumulatedTriggerFromConfig()
        {
            if (_config.TakeEffects == null || _config.TakeEffects.Count == 0)
                return;

            var tables = DataConfigManager.Tables;
            if (tables == null)
                return;

            foreach (var effectId in _config.TakeEffects)
            {
                var effectConfig = tables.TbEffectsConfig.GetOrDefault(effectId);
                if (effectConfig == null)
                    continue;

                // 检查是否为 EffectsType.TakeSpecial 类型
                if (effectConfig.Type == EffectsType.TakeSpecial)
                {
                    // 从 Attributes 中读取 MonsterContinuousDamage3CauseTrueDamage
                    if (effectConfig.Attributes != null && effectConfig.Attributes.Count > 0)
                    {
                        foreach (var attr in effectConfig.Attributes)
                        {
                            if (attr.Type == AttributeType.MonsterContinuousDamage3CauseTrueDamage)
                            {
                                // 写死为3层
                                int requiredTriggers = 3;
                                _requiredTriggers = requiredTriggers;
                                _resetOnEffectExecute = true; // 执行效果后重置
                                _enableAccumulatedTimeout = true; // 启用累计超时重置（2秒）
                                _accumulatedTriggerTimer = 0f;
                                LogTool.L($"[TriggerBuff] TakeSpecial found: Set accumulated triggers to {requiredTriggers}, ResetOnEffectExecute=true, EnableTimeout=true");
                            }
                        }
                    }
                } else if (effectConfig.Type == EffectsType.TakeSkill)
                {
                    var skillId = effectConfig.EffectsId;
                    SpecialEffectsConfig specialEffectsConfig = tables.TbSpecialEffectsConfig.GetOrDefault(skillId);
                    if (specialEffectsConfig?.LevelPropertyList != null && specialEffectsConfig.LevelPropertyList.Count > 0)
                    {
                        foreach (var attr in specialEffectsConfig.LevelPropertyList[0].Attributes)
                        {
                            if (attr.Type == AttributeType.SkillCdTime && attr.Value > 0)
                            {
                                _skillCooldownTime = attr.Value;
                                _skillCooldownAccumulator = 0f; // 初始为0，可以立即触发
                                LogTool.L($"[TriggerBuff] TakeSkill found: Set skill cooldown time to {attr.Value}s");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 每帧更新，累加时间
        /// </summary>
        public void OnTick(object owner, float dt)
        {
            _elapsed += dt;

            // 累计触发超时检测
            if (_enableAccumulatedTimeout && _accumulatedTriggers > 0)
            {
                _accumulatedTriggerTimer += dt;
                if (_accumulatedTriggerTimer >= AccumulatedTriggerTimeout)
                {
                    _accumulatedTriggerTimer = 0f;
                    _accumulatedTriggers = 0;
                    LogTool.L($"[TriggerBuff] Accumulated triggers timeout, reset to 0");
                }
            }

            if (_triggerInterval > 0)
            {
                _cooldownAccumulator += dt;
            }

            // 技能冷却倒计时
            if (_skillCooldownTime > 0 && _skillCooldownAccumulator < _skillCooldownTime)
            {
                _skillCooldownAccumulator += dt;
            }
        }

        /// <summary>
        /// 间隔触发回调
        /// </summary>
        public void OnIntervalTick(object owner, float intervalAccumulator)
        {
        }

        /// <summary>
        /// Buff被移除时调用，取消订阅事件
        /// </summary>
        public void OnRemove(object owner)
        {
            if (_onBuffTakeTypeHandler != null)
            {
                EventBus.Unsubscribe(_onBuffTakeTypeHandler);
                _onBuffTakeTypeHandler = null;
            }

            StopVfx();
            LogTool.L($"[TriggerBuff] OnRemove: owner={owner}, TotalTriggers={_triggerCount}");
        }

        /// <summary>
        /// 叠加层数时调用
        /// </summary>
        public void OnStackUp(object owner)
        {
            _stack++;
        }

        /// <summary>
        /// 刷新持续时间
        /// </summary>
        public void OnRefreshDuration(object owner)
        {
            _elapsed = 0f;
        }

        /// <summary>
        /// 重置累计触发次数（时间到了刷新或换目标时调用）
        /// </summary>
        public void ResetAccumulatedTriggers()
        {
            _accumulatedTriggers = 0;
            LogTool.L($"[TriggerBuff] ResetAccumulatedTriggers: BuffId={BuffId}");
        }

        /// <summary>
        /// 触发时机事件回调
        /// </summary>
        private void OnBuffTakeType(ConditionsTypeEvent evt)
        {
            if (evt.coditionsType != _config.ConditionType) return;

            // 判断事件触发者是否与Buff持有者一致
            if (evt.caster != _owner)
            {
                return;
            }

            if (!TryTrigger()) return;

            // 累计触发次数机制
            _accumulatedTriggers++;
            _accumulatedTriggerTimer = 0f; // 重置累计触发计时器

            // 检测目标切换：目标发生变化时重置累计计数
            if (_lastTarget != null && evt.target != null && evt.target != _lastTarget)
            {
                _accumulatedTriggers = 1; // 重置为1（当前这次触发）
                _accumulatedTriggerTimer = 0f;
                LogTool.L($"[TriggerBuff] Target changed from {_lastTarget} to {evt.target}, reset accumulated triggers to 1");
            }
            _lastTarget = evt.target;

            LogTool.L($"[TriggerBuff] Accumulated triggers: {_accumulatedTriggers}/{_requiredTriggers}");

            // 判断是否达到累计触发次数
            if (_accumulatedTriggers < _requiredTriggers)
            {
                // 未达到累计触发次数，不执行效果
                return;
            }

            // 达到累计触发次数，执行效果
            LogTool.L($"[TriggerBuff] Required triggers reached: {_accumulatedTriggers}/{_requiredTriggers}, executing effects");

            // 重置累计计数（如果配置要求）
            if (_resetOnEffectExecute)
            {
                _accumulatedTriggers = 0;
            }

            // 处理 TakeEffects - 根据 EffectsConfig 的 EffectsType 执行不同操作
            // 目标默认为事件目标，如果事件没有目标则使用持有者
            var target = evt.target ?? _owner;
            var direction = evt.direction;
            if (_config.TakeEffects != null && _config.TakeEffects.Count > 0)
            {
                // 检查技能冷却：使用内部倒计时
                if (CheckSkillCooldownForEffects(_config.TakeEffects))
                {
                    LogTool.L($"[TriggerBuff] OnBuffTakeType: Skill on cooldown, skip execution");
                    return; // 冷却中，跳过执行
                }

                // 冷却已过，执行效果
                EffectsExecutor.ExecuteEffects(_config.TakeEffects, _skillContext, _owner,direction,target );
                LogTool.L($"[TriggerBuff] OnBuffTakeType: TakeType={evt.coditionsType} matched, executed {_config.TakeEffects.Count} effects");
            
                // 执行后重置冷却
                ResetSkillCooldown();
            }
            else
            {
                // 兼容旧逻辑：如果没有 TakeEffects，则使用原有的 ApplyBuff 方式
                var buffable = _owner as IBuffable;
                if (buffable != null)
                {
                    BuffApplier.ApplyBuff(BuffId, _skillContext, buffable);
                    LogTool.L($"[TriggerBuff] OnBuffTakeType: TakeType={evt.coditionsType} matched, added buff {BuffId} to owner (legacy)");
                }
                else
                {
                    LogTool.E($"[TriggerBuff] OnBuffTakeType: owner is not IBuffable, owner={_owner}");
                }
            }
        }

        /// <summary>
        /// 检查 Effects 列表中的 TakeSkill 技能是否在冷却中（使用内部倒计时）
        /// </summary>
        private bool CheckSkillCooldownForEffects(List<int> effectIds)
        {
            // 如果没有配置冷却时间，允许执行
            if (_skillCooldownTime <= 0)
                return false;

            // 检查冷却是否已过
            if (_skillCooldownAccumulator < _skillCooldownTime)
            {
                LogTool.L($"[TriggerBuff] CheckSkillCooldownForEffects: Skill on cooldown, elapsed={_skillCooldownAccumulator:F2}s, total={_skillCooldownTime}s");
                return true; // 冷却中
            }

            return false; // 冷却已过
        }

        /// <summary>
        /// 执行技能后重置冷却倒计时
        /// </summary>
        private void ResetSkillCooldown()
        {
            _skillCooldownAccumulator = 0;
            LogTool.L($"[TriggerBuff] ResetSkillCooldown: Reset cooldown accumulator to 0");
        }

        /// <summary>
        /// 判断是否可以触发
        /// </summary>
        private bool TryTrigger()
        {
            if (_config == null) return false;

            // 检查触发间隔冷却
            if (_triggerInterval > 0 && _cooldownAccumulator < _triggerInterval)
            {
                LogTool.L($"[TriggerBuff] Cooldown not elapsed");
                return false;
            }

            // 检查触发概率
            if (RandomTool.RandomRangeTarget(1, 100) > _config.TriggerProbability * 100)
            {
                LogTool.L($"[TriggerBuff] Trigger probability missed: {_config.TriggerProbability}");
                return false;
            }

            _triggerCount++;
            _cooldownAccumulator = 0f;

            return true;
        }

        private void StopVfx()
        {
            if (EffectManager.Instance == null) return;
            if (_vfxInstance == null) return;

            EffectManager.Instance.RecycleEffect(_vfxInstance);
            _vfxInstance = null;
        }

        public override string ToString()
        {
            return $"[TriggerBuff: Id={BuffId}, TakeType={_config?.ConditionType}, Probability={_config?.TriggerProbability}]";
        }
    }
}
