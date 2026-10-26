using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using UnityEngine;

public class BounceMove : IProjectileMove
{
    private Vector3 dir;
    private float speed;
    private int maxBounceCount;
    private int _bounceCount;
    private bool _resetHitOnBounce;
    private Collider2D colliderGoj;

    public BounceMove(Vector3 dir, float speed, int maxBounceCount, bool resetHitOnBounce = true)
    {
        this.dir = dir.sqrMagnitude > 1e-10f ? dir.normalized : dir;
        this.speed = speed;
        this.maxBounceCount = maxBounceCount;
        _resetHitOnBounce = resetHitOnBounce;
        _bounceCount = 0;
        colliderGoj = null;
        var direction = (Vector3)dir;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        LogTool.Log($"当前最大的反弹数 {maxBounceCount}");
    }

    public void Move(Projectile_new p, float dt)
    {
        p.transform.position += dir * speed * dt;
        
        Vector2 currentPos = p.transform.position;
        // 射线长度 = 本帧移动距离 + 额外检测范围
        // 保证子弹"跑过"的路径也被检测到，避免高速穿透漏检
        float moveDistance = speed * dt;
        RaycastHit2D wallHit = Physics2D.Raycast(currentPos,dir, 1f, LayerMask.GetMask("AngleWall"));
        Debug.DrawRay(currentPos, dir * 5f, wallHit ? Color.green : Color.red);
        if (wallHit.collider != colliderGoj)
        {
            colliderGoj = wallHit.collider;
            // 执行反弹
            Bounce(p,wallHit.normal);
        }
    }
    
    /// <summary>
    /// 执行墙壁反弹
    /// 
    /// 反弹原理：
    /// 1. 获取墙壁法线（垂直于墙壁表面的方向）
    /// 2. 使用 Vector2.Reflect 计算反射方向
    /// 3. 更新飞行方向和旋转角度
    /// 
    /// 额外处理：
    /// - _bounceCount++ 记录反弹次数
    /// - _hitTargets.Clear() 清空命中列表，允许反弹后再次伤害同一目标
    /// - 达到 maxBounceCount 后不再反弹，直接销毁
    /// </summary>
    /// <param name="normal">墙壁碰撞点的法线向量（垂直于墙壁表面）</param>
    private void Bounce(Projectile_new p,Vector2 normal)
    {
        LogTool.Log("执行反弹");
        // 达到最大反弹次数后销毁
        if (_bounceCount >= maxBounceCount)
        {
            p.MissionOver();
            return;
        }
            
        // Vector2.Reflect 计算反射方向
        // 公式：R = D - 2 * (D·N) * N
        // D = 入射方向, N = 法线, R = 反射方向
        dir = Vector2.Reflect(dir, normal).normalized;

        // 更新子弹旋转角度
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        p.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            
        _bounceCount++;
        // 清空已命中列表，允许反弹后再次伤害同一目标
        // 否则反弹后子弹会穿过之前击中的敌人（含 AttachBuff 写入的 ExcludedTargetIds）
        if (_resetHitOnBounce)
        {
            p.ResetHitTarget();
        }
    }
}