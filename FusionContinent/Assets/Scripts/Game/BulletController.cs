using Config;
using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;

public class BulletController : MonoBehaviour
{    public int atk;
    public float moveDistance = 1000f;
    public float moveDuration = 10f;
    public GenericObjectPool pool;
    public MonsterCard monsterCard;

    private Tween _moveTween;
    private bool _hasHit;

    private void OnEnable()
    {
        _hasHit = false;
        transform.rotation = Quaternion.identity;
        KillMoveTween();
    }

    private void OnDisable()
    {
        KillMoveTween();
        monsterCard = null;
    }

    /// <summary>
    /// 配置完成后由 LvDouAttackBehaviour 调用，朝索敌英灵飞行。
    /// </summary>
    public void LaunchToSoldier(SoldierCard target)
    {
        KillMoveTween();

        if (target == null || target.isDie)
        {
            ReturnSelf();
            return;
        }

        Vector3 destination = GetSoldierAimPoint(target);
        FaceToward(destination);
        float duration = moveDuration > 0f ? moveDuration : 0.5f;
        _moveTween = transform.DOMove(destination, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnMoveComplete);
    }

    private Vector3 GetSoldierAimPoint(SoldierCard target)
    {
        if (target.PlayerspineObj != null)
        {
            return target.PlayerspineObj.transform.position;
        }

        return target.transform.position;
    }

    private void FaceToward(Vector3 destination)
    {
        Vector3 direction = destination - transform.position;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);
    }

    private void OnMoveComplete()
    {
        if (_hasHit || monsterCard == null)
        {
            ReturnSelf();
            return;
        }

        var target = monsterCard.targetSolidierCard;
        if (target != null && !target.isDie)
        {
            TryDamageSoldier(target);
        }

        ReturnSelf();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasHit || collision.name != "HitBox")
        {
            return;
        }

        var soldier = collision.GetComponentInParent<SoldierCard>();
        if (soldier == null || monsterCard == null)
        {
            return;
        }

        TryDamageSoldier(soldier);
        ReturnSelf();
    }

    private void TryDamageSoldier(SoldierCard soldier)
    {
        if (_hasHit || soldier == null || soldier.isDie || monsterCard == null)
        {
            return;
        }

        _hasHit = true;
        KillMoveTween();

        if (!soldier.CanReceiveIncomingDamage(atk))
        {
            return;
        }

        var result = DamageContextBuilder.CalculateIncoming(
            soldier,
            monsterCard,
            TakeDamageType.PhysicalDamage,
            atk);
        soldier.TakeDamage(result);
    }

    private void ReturnSelf()
    {
        if (pool != null)
        {
            pool.Return(gameObject);
            return;
        }

        ObjectPoolManager.Instance.GetGameObjectPool("Bullet").Return(gameObject);
    }

    private void KillMoveTween()
    {
        if (_moveTween != null && _moveTween.IsActive())
        {
            _moveTween.Kill();
        }

        _moveTween = null;
    }

    private void Update()
    {
        if (monsterCard == null && transform.localScale != Vector3.zero)
        {
            transform.localScale = Vector3.zero;
            ObjectPoolManager.Instance.GetGameObjectPool("Bullet").Return(gameObject);
        }
    }
}
