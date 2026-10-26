using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using FusionContinent.Skill.Handler;
using Game.Battle;
using UnityEngine;

/// <summary>
/// 基于子弹自身碰撞体的伤害检测组件
/// 挂在子弹预制体上，从 BoxCollider2D 读取世界坐标和尺寸做碰撞检测
/// 与旧版 DrawSlashBullet 逻辑一致，使用 Unity 物理引擎的碰撞结果
/// </summary>
public class BulletDamageDetector : MonoBehaviour
{
    [Header("碰撞体配置（可选，拖拽外部碰撞体）")]
    [Tooltip("外部指定的碰撞体，不设置则自动获取自身组件")]
    public BoxCollider2D externalCollider;

    private BoxCollider2D _collider;
    private bool _useExternalCollider;
    private readonly HashSet<string> _hitTargets = new HashSet<string>();
    private float _hitTargetDuration;
    private float _hitTargetDurationTime;
    private int _maxPenetrateCount;
    private int _currentPenetrateCount;
    private SkillContext _skillContext;
    private string _hitEffect;
    private int _spawnHitEffectNum;
    private int _spawnHitEffectMaxNum = 10;
    private ISkill _skillCard;

    /// <summary>
    /// 初始化
    /// </summary>
    public void Init(SkillContext context, ISkill skillCard, int maxPenetrate, float hitTargetDuration, string hitEffect)
    {
        _skillContext = context;
        _skillCard = skillCard;
        _maxPenetrateCount = maxPenetrate;
        _hitTargetDuration = hitTargetDuration;
        _hitEffect = hitEffect;
        _currentPenetrateCount = 0;
        _hitTargetDurationTime = 0f;
        _spawnHitEffectNum = 0;
        _hitTargets.Clear();
    }

    void Awake()
    {
        if (externalCollider != null)
        {
            _collider = externalCollider;
            _useExternalCollider = true;
        }
        else
        {
            _collider = GetComponent<BoxCollider2D>();
            _useExternalCollider = false;
        }
    }

    void Update()
    {
        if (GamePlayManager.Default != null && GamePlayManager.Default.IsPaused())
        {
            return;
        }

        // 持续伤害模式：每隔 _hitTargetDuration 重置命中记录
        if (_hitTargetDuration > 0f)
        {
            _hitTargetDurationTime += Time.deltaTime * Global.SpeedMultiplier.Value;
            if (_hitTargetDurationTime >= _hitTargetDuration)
            {
                _hitTargets.Clear();
                _hitTargetDurationTime = 0f;
            }
        }
    }

    void FixedUpdate()
    {
        DetectCollisions();
    }

    private void DetectCollisions()
    {
        if (_collider == null || !enabled) return;

        // 使用 BoxCollider2D 的世界坐标和尺寸
        Vector2 center = _collider.bounds.center;
        Vector2 size = new Vector2(_collider.bounds.size.x, _collider.bounds.size.y);
        float angle = transform.eulerAngles.z;

        // 通过 OverlapBox 获取碰撞结果
        var hits = HitSystem2D.Box(center, size, angle, LayerMask.GetMask("MonsterBox"));

        foreach (var hit in hits)
        {
            if (hit.target == null) continue;
            if (hit.target.name != "HitBox") continue;

            var monster = hit.target.transform.parent?.GetComponent<MonsterCard>();
            if (monster == null || !monster.enabled) continue;

            string uniqueId = monster.uniqueId;
            if (!_hitTargets.Add(uniqueId))
            {
                // 已命中过该目标
                if (_hitTargetDuration <= 0f) continue;
            }

            // 穿透上限检测
            _currentPenetrateCount++;
            if (_maxPenetrateCount > 0 && _currentPenetrateCount >= _maxPenetrateCount)
            {
                enabled = false;
                LogTool.L("[BulletDamageDetector] 达到穿透上限，关闭检测");
                return;
            }

            // 造成伤害
            monster.damageContext.HasPreRolledCrit = _skillContext.HasPreRolledCrit;
            monster.damageContext.PreRolledCrit = _skillContext.PreRolledCrit;
            monster.damageContext.SkipBattleRuleBeforeTrigger = _skillContext.SkipBattleRuleBeforeTrigger;
            BattleRuleCastDamageSession.CopyToDamageContext(_skillContext, monster.damageContext);
            monster.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                monster, _skillCard, hit.hitPoint, 1f, _skillContext.Caster as SoldierCard, _skillContext));

            // 播放命中特效
            SpawnHitEffect(hit.hitPoint);
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        if (string.IsNullOrEmpty(_hitEffect) || _spawnHitEffectNum >= _spawnHitEffectMaxNum) return;
        _spawnHitEffectNum++;

        var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(_hitEffect);
        if (pool == null) return;

        var effect = pool.Get();
        if (effect == null) return;

        effect.transform.SetParent(GameLand.Default.DamageParent.transform);
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 200;

        StartCoroutine(DisableAfterDelay(pool, effect, 0.5f));
    }

    private System.Collections.IEnumerator DisableAfterDelay(QFramework.GenericObjectPool pool, GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (effect != null)
        {
            pool.Return(effect);
        }
    }

    void OnDisable()
    {
        _hitTargets.Clear();
    }
}
