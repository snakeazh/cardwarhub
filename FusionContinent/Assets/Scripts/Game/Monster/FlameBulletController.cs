using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;

public class FlameBulletController : MonoBehaviour
{
    public int atk;
    public GenericObjectPool pool;
    public int takeDamageNum = 4; // 造成伤害次数
    public float moveDistance = 1.3f; // 子弹飞行的距离
    public float moveDuration = 0.3f;  // 子弹飞行的持续时间
    public MonsterCard monsterCard;
    private RectTransform rectTransform;
    Tween Tween;
    private void OnEnable()
    {
        // 获取 RectTransform 组件
        rectTransform = GetComponent<RectTransform>();
        // 如果 Tween 已经存在并且正在运行，先杀死它
        if (Tween != null && Tween.IsPlaying())
        {
            Tween.Kill();
        }
        
        
        
        GenericObjectPool pool3 = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("ChaosDragonKingAttack03");
        if (pool3 != null)
        {
            var bullet = pool3.Get();
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.localPosition = new Vector2(transform.localPosition.x, -840f) ;
            bullet.transform.localScale = Vector3.one;
            StartCoroutine(DisableAfterDelay(pool3, bullet.gameObject, 4f));
        }

        // 使用 DOTween 
        Tween = transform.DOScaleY(moveDistance, moveDuration).SetEase(Ease.Linear);
        StartCoroutine(DisableAfterDelay(pool, this.gameObject, 4f));
        StartCoroutine(TakeDamage());
    }
    private void OnDisable()
    {
        Tween.Kill();
    }


    private void Update()
    {
        if(monsterCard == null&& transform.localScale!=Vector3.zero)
        {
            transform.localScale = Vector3.zero;
            ObjectPoolManager.Instance.GetGameObjectPool("ChaosDragonKingAttack01").Return(this.gameObject);
        }
    }

    private IEnumerator TakeDamage()
    {
        for (int i = 0; i < takeDamageNum; i++)
        {
            // 等待指定的时间
            yield return new WaitForSeconds(0.2f);
            var wall = monsterCard != null
                ? GameLand.Default.GetLane(monsterCard.BattleSide)?.Wall
                : null;
            if (wall == null)
            {
                wall = GameLand.Default.wall;
            }

            wall.TakeDamage(atk, monsterCard);
        }
    }

    private IEnumerator DisableAfterDelay(GenericObjectPool pool, GameObject damageObject, float delay)
    {
        // 等待指定的时间
        yield return new WaitForSeconds(delay);

        // 检查对象是否仍然有效
        if (damageObject != null)
        {
            try
            {
                pool.Return(damageObject);
            }
            catch
            {
                ObjectPoolManager.Instance.GetGameObjectPool("ChaosDragonKingAttack01").Return(this.gameObject);
            }
        }
    }
}
