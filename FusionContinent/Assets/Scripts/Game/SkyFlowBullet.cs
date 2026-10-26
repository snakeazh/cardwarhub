using System.Collections;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

public class SkyFlowBullet : MonoBehaviour
{
    private ISkill _skillCard;
    private BoxCollider2D _collider;

    public void StartAttackSet(ISkill skillCard)
    {
        _skillCard = skillCard;
        _collider = GetComponent<BoxCollider2D>();
        StartCoroutine(ContinuousDamage());
    }

    private IEnumerator ContinuousDamage()
    {
        int hitCount = 0;

        while (hitCount < 3 && gameObject.activeSelf)
        {
            PerformBoxCast();
            hitCount++;
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void PerformBoxCast()
    {
        if (_collider == null) return;

        Vector2 size = _collider.size;
        Vector2 center = (Vector2)transform.position + _collider.offset;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, transform.eulerAngles.z);

        foreach (Collider2D hit in hits)
        {
            if (hit.name != "HitBox") continue;

            MonsterCard hitTarget = hit.transform.parent.GetComponent<MonsterCard>();
            if (hitTarget == null) continue;        

            Vector3 randomOffset = new Vector3(Random.Range(0, 2f), Random.Range(0, 2f), 0);

            hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(hitTarget, _skillCard, hitTarget.transform.position + randomOffset));
            
            // 直接固定一种伤害：真实伤害
            // hitTarget.TakeDamage(_damageType, (int)_damage, _skillCard, hitTarget.transform.position + randomOffset);

            SpawnHitEffect(hitTarget.transform.position);
        }
    }

    private void SpawnHitEffect(Vector3 position)
    {
        QFramework.GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPool("youxia_jn1_06");
        if (pool == null) return;

        var effect = pool.Get();
        if (effect == null) return;

        effect.transform.SetParent(GameLand.Default.DamageParent.transform);
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 200;

        StartCoroutine(DisableAfterDelay(pool, effect, 0.5f));
    }

    private IEnumerator DisableAfterDelay(QFramework.GenericObjectPool pool, GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effect != null)
        {
            pool.Return(effect);
        }
    }
}
