using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Skill.Handler;
using Game;
using QFramework;
using UnityEngine;

public class BurnArea : MonoBehaviour
{
    public const string PoolKey = "__BurnArea_RuntimePool";

    private sealed class TargetStayState
    {
        public MonsterCard Monster;
        public float StayTime;
    }

    private SkillContext _skillContext;
    private ISkill _skillCard;
    private string _effectName;
    private float _duration;
    private float _radius;
    private float _tickInterval;
    private float _areaDamage;
    private bool _useSkillDamage;
    private int _buffId;
    private float _buffStayTime;

    private float _lifeTimer;
    private float _tickTimer;
    private readonly Dictionary<string, TargetStayState> _stayStates = new();
    private readonly HashSet<string> _inAreaThisTick = new();
    private GameObject _effectInstance;
    private GenericObjectPool _pool;

    public void Init(
        SkillContext skillContext,
        string effectName,
        float duration,
        float radius,
        float tickInterval,
        float areaDamage,
        bool useSkillDamage,
        int buffId,
        float buffStayTime,
        GenericObjectPool pool)
    {
        _skillContext = skillContext;
        _skillCard = ResolveSkillCard(skillContext);
        _effectName = effectName;
        _duration = duration;
        _radius = radius;
        _tickInterval = tickInterval;
        _areaDamage = areaDamage;
        _useSkillDamage = useSkillDamage;
        _buffId = buffId;
        _buffStayTime = buffStayTime;
        _pool = pool;
        _lifeTimer = 0f;
        _tickTimer = 0f;
        _stayStates.Clear();
        _inAreaThisTick.Clear();

        if (!string.IsNullOrEmpty(_effectName) && EffectManager.Instance != null)
        {
            var effectParent = GameLand.Default != null ? GameLand.Default.Bullet : null;
            _effectInstance = EffectManager.Instance.PlayEffect(
                _effectName,
                transform.position,
                lifetime: _duration,
                parent: effectParent,
                layer: EffectLayer.World);
        }

        ProcessAreaTick();
    }

    private void OnDisable()
    {
        // 不在 OnDisable 回收特效：父物体停用过程中 SetParent 会报错；由 RecycleSelf 提前回收，或依赖特效自身 lifetime
        _effectInstance = null;
        _stayStates.Clear();
        _inAreaThisTick.Clear();
        _lifeTimer = 0f;
        _tickTimer = 0f;
        _skillContext = null;
        _skillCard = null;
        _pool = null;
    }

    private void Update()
    {
        if (GamePlayManager.Default != null && GamePlayManager.Default.IsPaused())
        {
            return;
        }

        float dt = Time.deltaTime * Global.SpeedMultiplier.Value;
        _lifeTimer += dt;
        _tickTimer += dt;

        if (_tickInterval > 0f && _tickTimer >= _tickInterval)
        {
            while (_tickTimer >= _tickInterval)
            {
                _tickTimer -= _tickInterval;
                ProcessAreaTick();
            }
        }

        if (_lifeTimer >= _duration)
        {
            RecycleSelf();
        }
    }

    private void ProcessAreaTick()
    {
        _inAreaThisTick.Clear();

        var hits = HitSystem2D.Circle(transform.position, _radius, LayerMask.GetMask("MonsterBox"));
        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            if (hit.target == null || hit.target.name != "HitBox")
            {
                continue;
            }

            var monster = hit.target.transform.parent.GetComponent<MonsterCard>();
            if (monster == null || !monster.enabled)
            {
                continue;
            }

            var uniqueId = monster.uniqueId;
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = monster.GetInstanceID().ToString();
            }

            _inAreaThisTick.Add(uniqueId);

            if (!ApplyAreaDamage(monster, hit.hitPoint))
            {
                continue;
            }

            if (!_stayStates.TryGetValue(uniqueId, out var state))
            {
                state = new TargetStayState { Monster = monster, StayTime = 0f };
            }

            state.Monster = monster;
            state.StayTime += _tickInterval;
            TryApplyBuffLayers(state);
            _stayStates[uniqueId] = state;
        }

        if (_stayStates.Count == 0)
        {
            return;
        }

        var leftAreaIds = new List<string>();
        foreach (var pair in _stayStates)
        {
            if (!_inAreaThisTick.Contains(pair.Key))
            {
                leftAreaIds.Add(pair.Key);
            }
        }

        for (int i = 0; i < leftAreaIds.Count; i++)
        {
            _stayStates.Remove(leftAreaIds[i]);
        }
    }

    private bool ApplyAreaDamage(MonsterCard monster, Vector3 hitPoint)
    {
        if (_useSkillDamage)
        {
            if (_skillContext == null)
            {
                LogTool.LogWarning("[BurnArea] use_skill_damage 已开启但 SkillContext 为空");
                return false;
            }

            var damageTypes = _skillCard != null ? _skillCard.SumDamageTypes() : null;
            if (damageTypes != null)
            {
                foreach (var pair in damageTypes)
                {
                    if (pair.Value <= 0)
                    {
                        continue;
                    }

                    LogTool.Log(
                        $"[BurnArea] 技能伤害路径 skillId={_skillContext.SkillId} damageType={pair.Key} baseDamage={pair.Value} monster={monster.uniqueId}");
                }
            }
            else
            {
                LogTool.Log(
                    $"[BurnArea] 技能伤害路径 skillId={_skillContext.SkillId} SumDamageTypes=null monster={monster.uniqueId}");
            }

            EventBus.Publish(new ProjectileHitEvent
            {
                skillContext = _skillContext,
                hitpoint = hitPoint,
                monsterCard = monster
            });
            return true;
        }

        if (_areaDamage <= 0f)
        {
            return false;
        }

        int baseDamage = Mathf.Max(1, Mathf.RoundToInt(_areaDamage));
        var soldier = _skillContext != null ? _skillContext.Caster as SoldierCard : null;
        LogTool.Log(
            $"[BurnArea] 固定魔法伤害 baseDamage={baseDamage} rawAreaDamage={_areaDamage} tickInterval={_tickInterval} radius={_radius} skillId={_skillCard?.Id ?? 0} monster={monster.uniqueId} soldier={soldier?.name ?? "null"}");

        var applied = monster.ReceiveDamage(MonsterDamageRequest.Single(
            monster,
            TakeDamageType.MagicDamage,
            baseDamage,
            _skillCard,
            hitPoint,
            soldier,
            pipelineType: DamagePipelineType.BuffSkillDamage,
            isAgainDamage: true));
        LogTool.Log(
            $"[BurnArea] 固定魔法伤害结算 applied={applied} baseDamage={baseDamage} monster={monster.uniqueId} hp={monster.hp}");
        return true;
    }

    private void TryApplyBuffLayers(TargetStayState state)
    {
        if (_buffId <= 0 || _buffStayTime <= 0f || state.Monster == null)
        {
            return;
        }

        while (state.StayTime >= _buffStayTime)
        {
            state.StayTime -= _buffStayTime;
            BuffApplier.ApplyBuff(_buffId, _skillContext, state.Monster);
        }
    }

    private static ISkill ResolveSkillCard(SkillContext context)
    {
        if (context?.Caster is not SoldierCard soldierCard)
        {
            return null;
        }

        return soldierCard.GetSkillById(context.SkillId);
    }

    private void RecycleSelf()
    {
        CleanupEffect();

        if (_pool != null)
        {
            _pool.Return(gameObject);
            return;
        }

        gameObject.SetActive(false);
    }

    private void CleanupEffect()
    {
        if (_effectInstance == null)
        {
            return;
        }

        var effect = _effectInstance;
        _effectInstance = null;

        if (!effect)
        {
            return;
        }

        if (effect.transform.parent == transform)
        {
            effect.transform.SetParent(null, true);
        }

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.RecycleEffect(effect);
        }
    }
}
