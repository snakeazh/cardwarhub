using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;
using DG.Tweening;

public class ArrowBullet : MonoBehaviour
{
    private ISkill _skillCard;
    private bool _hasTriggered = false;
    private BoxCollider2D _collider;
    private bool _isAgainDamage;
    private float _thunderboltDamage;
    private TakeDamageType _damageType;
    private HashSet<MonsterCard> _hitTargets = new HashSet<MonsterCard>();
    private Tween _detectionTween;

    private int _spawnHitEffectMaxNum = 10;
    private int _spawnHitEffectNum;

    public void StartAttackSet(ISkill skillCard, float thunderboltDamage, float duration = 0.5f, bool isAgainDamage = false, TakeDamageType damageType = TakeDamageType.RealDamage)
    {
        _hasTriggered = false;
        _skillCard = skillCard;
        _collider = GetComponent<BoxCollider2D>();
        _thunderboltDamage = thunderboltDamage;
        _isAgainDamage = isAgainDamage;
        _damageType = damageType;
        _hitTargets.Clear();

        _spawnHitEffectNum = 0;

        // CameraShake

        // 使用 DOTween 每帧检测一次，持续 0.5 秒（或者根据需求调整）
        _detectionTween?.Kill();
        _detectionTween = DOTween.To(() => 0f, x =>
            {
                PerformBoxCast();
            }, 1f, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => _hasTriggered = true);
    }

    private void PerformBoxCast()
    {
        if (_collider == null || _hasTriggered) return;

        Vector2 size = _collider.size * transform.localScale;
        Vector2 center = (Vector2)transform.position + _collider.offset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, transform.eulerAngles.z);

        foreach (Collider2D hit in hits)
        {
            if (hit.name != "HitBox") continue;

            MonsterCard hitTarget = hit.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget == null) continue;

            // 已经伤害过的怪物跳过
            if (_hitTargets.Contains(hitTarget)) continue;

            Vector3 randomOffset = new Vector3(Random.Range(0, 2f), Random.Range(0, 2f), 0);

            if (_thunderboltDamage > 0f)
            {
                hitTarget.ReceiveDamage(MonsterDamageRequest.Single(
                    hitTarget,
                    _damageType,
                    (int)_thunderboltDamage,
                    _skillCard,
                    hitTarget.transform.position + randomOffset,
                    isAgainDamage: _isAgainDamage));
            }
            else
            {
                hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                    hitTarget, _skillCard, hitTarget.transform.position + randomOffset));
            }

            _hitTargets.Add(hitTarget); // 标记这个怪物已被伤害
            SpawnHitEffect(hitTarget.transform.position);
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        // 受击表现配表，如果没有跳过
        if (_skillCard.Config.SkillHitEffect == "") return;
        
        if (_spawnHitEffectNum > _spawnHitEffectMaxNum) return;
        _spawnHitEffectNum++;
        
        // GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPool("youxia_jn1_06");
        QFramework.GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(_skillCard.Config.SkillHitEffect);
        if (pool == null) return;

        var effect = pool.Get();
        if (effect == null) return;

        effect.transform.SetParent(GameLand.Default.DamageParent.transform);
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 100;

        // 使用 DOTween 延迟回收
        DOVirtual.DelayedCall(0.5f, () =>
        {
            if (effect != null)
                pool.Return(effect);
        });
    }

    private void OnDisable()
    {
        _detectionTween?.Kill();
    }
}
