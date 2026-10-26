using System;
using UnityEngine;

/// <summary>
/// 投射物配置文件
/// 存储投射物的静态配置参数
/// </summary>
public class ProjectileConfig
{
    /// <summary>
    /// 投射物飞行速度
    /// </summary>
    public float speed = 10f;
    
    /// <summary>
    /// 投射物生命周期（秒）
    /// </summary>
    public float lifeTime = 10f;
    
    /// <summary>
    /// 碰撞检测半径
    /// </summary>
    public float checkRadius = 0.5f;

    /// <summary>
    /// 命中特效路径
    /// </summary>
    public string hitEffect = "";

    /// <summary>
    /// 子弹视觉长度，沿飞行方向（Y轴）的缩放
    /// </summary>
    public float bulletLength = 1f;

    /// <summary>
    /// 子弹视觉宽度，垂直飞行方向（X轴）的缩放
    /// </summary>
    public float bulletWidth = 1f;

    /// <summary>
    /// 固定朝向角度（度），-1 表示不固定
    /// </summary>
    public float fixedRotation = -1f;

    /// <summary>
    /// 结束特效路径（投射物消失时播放）
    /// </summary>
    public string endEffect = "";
    
    // ======== 以下参数已移除，因为它们与 Projectile_new 或其他模块重复 ========
    
    // maxDistance - 已移除，使用 lifeTime 控制生命周期
    
    // sweepAngle, sweepDuration - 已移除，激光扫射参数应放在专门的激光扫射模块中
    
    // fixedStartX, fixedStartY - 已移除，起始位置由 ProjectileHandler 在创建时直接传入
}

public interface IProjectileHitModule
{
    /// <summary>
    /// 收集命中结果 - 检测碰撞并添加到 HitContext
    /// </summary>
    /// <param name="p">投射物实例</param>
    /// <param name="context">命中上下文（存储所有命中的目标）</param>
    /// <returns>是否有新的命中</returns>
    bool CollectHits(Projectile_new p, HitContext context);
    
    /// <summary>
    /// 处理命中 - 对已收集的目标执行伤害等逻辑
    /// </summary>
    /// <param name="p">投射物实例</param>
    /// <param name="context">命中上下文</param>
    void OnHit(Projectile_new p, HitContext context);

    /// <summary>
    /// 重置命中目标列表 - 用于特殊移动规则（如反弹）后清空
    /// </summary>
    void ResetHitTarget(Projectile_new p);
}