using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using UnityEngine;

public class StraightMove : IProjectileMove
{
    private Vector3 dir;
    private float speed;

    public StraightMove(Vector3 dir, float speed)
    {
        this.dir =  dir;
        this.speed = speed;
        
    }

    public void SetDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        dir = direction.normalized;
    }

    public void Move(Projectile_new p, float dt)
    {
        // 计算目标位置并开始移动
        // Vector3 targetPosition = transform.position + direction * 100f;
        // pTween = transform.DOMove(targetPosition, speed).SetEase(Ease.Linear).SetSpeedBased();
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        p.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        
        p.transform.position += dir * speed * dt;
    }
}
