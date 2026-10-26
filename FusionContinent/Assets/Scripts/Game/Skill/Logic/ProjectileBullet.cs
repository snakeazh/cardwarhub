using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

namespace FusionContinent.Skill
{
    /// <summary>
    /// 通用投射物子弹组件
    /// 
    /// 功能概述：
    /// - 沿指定方向直线飞行
    /// - 碰撞敌人造成伤害、击飞、击退效果
    /// - 支持穿透模式（穿透多个敌人）
    /// - 支持墙壁反弹模式
    /// - 自动从技能属性表读取速度、射程、生存时间
    /// - 对象池支持，自动回收复用
    /// 
    /// 使用流程：
    /// 1. 从对象池获取预制体（预制体需挂载本组件）
    /// 2. 调用 SetStart() 初始化
    /// 3. 可选调用 SetPool() / SetPenetrate() / SetBounce()
    /// 4. 可选订阅 OnHit / OnComplete 事件
    /// 5. 子弹自动飞行、碰撞、回收
    /// 
    /// 碰撞规则：
    /// - 只检测名为 "HitBox" 的碰撞体（约定怪物子物体）
    /// - 通过父物体获取 MonsterCard
    /// - 同一目标在命中列表有效期外不会重复受伤
    /// 
    /// @author Assistant
    /// @create 2026/2/25
    /// </summary>
    public class ProjectileBullet : MonoBehaviour
    {
        // ============================================================
        // 基础移动参数
        // ============================================================
        
        /// <summary>
        /// 子弹飞行速度（米/秒）
        /// 实际移动：每帧 position += direction * speed * Time.deltaTime
        /// 
        /// 默认值：50
        /// 运行时覆盖：自动从 SkillMissileSpeed 属性读取
        /// </summary>
        [Header("基础移动")]
        [SerializeField] private float speed = 50f;
        
        /// <summary>
        /// 子弹最大飞行距离（米）
        /// 超出此距离后子弹销毁/回收
        /// 
        /// 默认值：100
        /// 注意：此参数目前仅在 CheckLifeTime 中使用，未通过属性覆盖
        /// </summary>
        [SerializeField] private float maxDistance = 100f;
        
        /// <summary>
        /// 子弹最大生存时间（秒）
        /// 超出此时间后子弹销毁/回收，防止无限飞行
        /// 
        /// 默认值：5
        /// 运行时覆盖：自动从 SkillDuration 属性读取
        /// </summary>
        [SerializeField] private float lifeTime = 5f;
        
        // ============================================================
        // 碰撞检测参数
        // ============================================================
        
        /// <summary>
        /// 敌人所在的 Unity LayerMask
        /// 用于 Physics2D.RaycastAll 射线检测
        /// 
        /// 典型值：LayerMask.GetMask("MonsterBox")
        /// 注意：需与 Projectile.cs 中的 BoxCastAll 保持一致
        /// </summary>
        [Header("碰撞检测")]
        [SerializeField] private LayerMask enemyLayer;
        
        /// <summary>
        /// 墙壁所在的 Unity LayerMask
        /// 用于墙壁反弹检测（isBounce=true 时生效）
        /// 
        /// 典型值：LayerMask.GetMask("Wall") 或自定义层
        /// 注意：需与游戏地图的墙体 Layer 一致
        /// </summary>
        [SerializeField] private LayerMask wallLayer;
        
        /// <summary>
        /// 碰撞检测半径（米）
        /// 射线检测向前延伸 moveDistance + checkRadius 的距离
        /// 值越大检测范围越大，但可能误检测
        /// 
        /// 默认值：0.5
        /// 建议：与子弹预制体视觉大小匹配
        /// </summary>
        [SerializeField] private float checkRadius = 0.5f;
        
        // ============================================================
        // 穿透参数
        // ============================================================
        
        /// <summary>
        /// 是否启用穿透模式
        /// true: 穿透敌人后继续飞行，可以击中多个敌人
        /// false: 命中第一个敌人后立即销毁
        /// 
        /// 默认值：false
        /// 运行时覆盖：自动根据 SkillPenetrateNum > 0 判断
        /// </summary>
        [Header("穿透设置")]
        [SerializeField] private bool isPenetrate = false;
        
        /// <summary>
        /// 最大穿透敌人数量
        /// 穿透次数耗尽后子弹销毁/回收
        /// 
        /// 默认值：999（几乎等于不限制）
        /// 运行时覆盖：自动从 SkillPenetrateNum 属性读取
        /// </summary>
        [SerializeField] private int maxPenetrateCount = 999;
        
        // ============================================================
        // 反弹参数
        // ============================================================
        
        /// <summary>
        /// 是否启用墙壁反弹模式
        /// true: 碰到墙壁时按反射角反弹，继续飞行
        /// false: 子弹不受墙壁影响
        /// 
        /// 默认值：false
        /// 运行时覆盖：自动根据 SkillWallRebound > 0 判断
        /// </summary>
        [Header("反弹设置")]
        [SerializeField] private bool isBounce = false;
        
        /// <summary>
        /// 最大反弹次数
        /// 反弹次数耗尽后子弹销毁/回收
        /// 
        /// 默认值：3
        /// 建议：与技能设计匹配，通常 1-3 次
        /// </summary>
        [SerializeField] private int maxBounceCount = 3;
        
        // ============================================================
        // 伤害效果参数
        // ============================================================
        
        /// <summary>
        /// 击飞效果高度值
        /// 传递给 MonsterCard.KnockUpMonster()
        /// 值越大怪物被击飞得越高
        /// 
        /// 默认值：0（无击飞）
        /// 运行时覆盖：自动从 SkillKnockUpMonster 属性读取
        /// </summary>
        [Header("伤害效果")]
        public float knockUpVal;
        
        /// <summary>
        /// 击退效果值
        /// 传递给 MonsterCard.RepelMonster()
        /// 正值向前击退，负值向后拉扯
        /// 
        /// 默认值：0（无击退）
        /// 运行时覆盖：自动从 SkillRepelMonster 属性读取
        /// </summary>
        public float repelMonsterVal;
        
        // ============================================================
        // 运行时数据（私有）
        // ============================================================
        
        /// <summary>
        /// 发射此子弹的技能实例
        /// 用于读取属性和造成伤害
        /// 伤害调用：monster.TakeDamageTypes(_skillCard, hitPos)
        /// </summary>
        private ISkill _skillCard;
        
        /// <summary>
        /// 子弹飞行方向（归一化向量）
        /// 每帧更新位置：position += direction * speed * deltaTime
        /// 反弹时会通过 Vector2.Reflect 改变方向
        /// </summary>
        private Vector3 _direction;
        
        /// <summary>
        /// 子弹发射位置（世界坐标）
        /// 用于计算飞行距离：distance = |pos - _startPos|
        /// </summary>
        private Vector3 _startPos;
        
        /// <summary>
        /// 子弹发射时的时间戳
        /// 用于计算生存时间：elapsed = Time.time - _startTime
        /// </summary>
        private float _startTime;
        
        /// <summary>
        /// 当前已穿透的敌人数量
        /// 每次命中敌人时 +1，达到 maxPenetrateCount 后销毁
        /// </summary>
        private int _penetrateCount;
        
        /// <summary>
        /// 当前已反弹的次数
        /// 每次碰到墙壁时 +1，达到 maxBounceCount 后销毁
        /// </summary>
        private int _bounceCount;
        
        // ============================================================
        // 命中记录（穿透/持续伤害）
        // ============================================================
        
        /// <summary>
        /// 已命中目标的唯一ID集合
        /// 用于防止穿透模式下同一目标被重复伤害
        /// Key = MonsterCard.uniqueId
        /// 
        /// 穿透模式下：每次穿透后不清空，直到子弹销毁
        /// 反弹模式下：每次反弹后清空，允许反弹后再次伤害同一目标
        /// </summary>
        private readonly HashSet<string> _hitTargets = new HashSet<string>();
        
        /// <summary>
        /// 对同一目标的重复伤害间隔（秒）
        /// = 0：每个目标只能受伤一次
        /// > 0：每隔此时间允许对同一目标再次造成伤害
        /// 
        /// 用途：实现"持续伤害"效果，如毒液每秒扣血
        /// 计时逻辑在 Update 中，每隔 duration 重置 _hitTargets
        /// </summary>
        private float _hitTargetDuration;
        
        /// <summary>
        /// 当前已过去的重复伤害间隔时间
        /// 每次 Update 累加 Time.deltaTime
        /// 达到 _hitTargetDuration 时重置命中记录
        /// </summary>
        private float _hitTargetDurationTime;
        
        // ============================================================
        // 状态管理
        // ============================================================
        
        /// <summary>
        /// 子弹是否处于活跃状态
        /// = false 时 Update 不执行任何逻辑
        /// 设置为 true 的时机：SetStart()
        /// 设置为 false 的时机：DestroyOrReturn()
        /// </summary>
        private bool _isActive = false;
        
        /// <summary>
        /// 子弹所属的对象池
        /// 用于子弹销毁/回收
        /// = null 时直接 Destroy(gameObject)
        /// </summary>
        private QFramework.GenericObjectPool _pool;
        
        // ============================================================
        // 事件回调
        // ============================================================
        
        /// <summary>
        /// 命中敌人时触发
        /// 参数：命中点的世界坐标
        /// 
        /// 使用场景：
        /// - 在命中点播放爆炸特效
        /// - 生成额外的视觉/音效
        /// - 触发技能的特殊效果
        /// </summary>
        public event Action<Vector3> OnHit;
        
        /// <summary>
        /// 子弹完成使命时触发（销毁/回收前）
        /// 时机：在 _isActive = false 之后、pool.Return() 之前
        /// 
        /// 使用场景：
        /// - 技能特定的清理逻辑
        /// - 记录统计数据
        /// - 预加载下一个子弹
        /// </summary>
        public event Action OnComplete;

        // ============================================================
        // Unity 生命周期
        // ============================================================
        
        /// <summary>
        /// 当对象被禁用/回收时调用
        /// 重置所有运行时状态，为下次复用做准备
        /// </summary>
        void OnDisable()
        {
            ResetState();
        }

        // ============================================================
        // 公共方法
        // ============================================================
        
        /// <summary>
        /// 初始化子弹（主要入口方法）
        /// 
        /// 参数说明：
        /// - skillCard: 发射子弹的技能实例，用于读取属性和造成伤害
        /// - direction: 飞行方向向量，会被归一化处理
        /// - hitTargetDuration: 对同一目标的重复伤害间隔（秒）
        ///   * 0 或负值：每个目标只能受伤一次
        ///   * 正值：如 1.0，则每秒可以对同一目标造成一次伤害
        /// 
        /// 自动读取并覆盖的参数：
        /// - speed：SkillMissileSpeed（飞行速度）
        /// - lifeTime：SkillDuration（生存时间）
        /// - knockUpVal：SkillKnockUpMonster（击飞高度）
        /// - repelMonsterVal：SkillRepelMonster（击退值）
        /// - isPenetrate：SkillPenetrateNum > 0 时启用
        /// - maxPenetrateCount：穿透数量上限
        /// - isBounce：SkillWallRebound > 0 时启用
        /// 
        /// 调用示例：
        ///   var projectile = fireball.GetComponent<ProjectileBullet>();
        ///   projectile.SetStart(this, direction.normalized, 0f);
        ///   projectile.SetPool(pool);
        /// </summary>
        /// <param name="skillCard">技能卡片实例</param>
        /// <param name="direction">飞行方向（会自动归一化）</param>
        /// <param name="hitTargetDuration">重复伤害间隔（秒），0表示只伤一次</param>
        public void SetStart(ISkill skillCard, Vector3 direction, float hitTargetDuration = 0f)
        {
            _skillCard = skillCard;
            _direction = direction.normalized;
            _hitTargetDuration = hitTargetDuration;
            
            _startPos = transform.position;
            _startTime = Time.time;
            _isActive = true;
            
            // 根据飞行方向计算2D旋转角度（子弹头朝上为基准）
            // direction.x -> 水平, direction.y -> 垂直
            // atan2(y,x) 得到弧度，乘以 Rad2Deg 得到角度
            // -90 是因为子弹模型默认朝上，旋转到 direction 方向
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            
            // 从技能属性表读取配置，覆盖 SerializeField 默认值
            if (_skillCard != null)
            {
                speed = _skillCard.Attributes.GetFinal(AttributeType.SkillMissileSpeed);
                if (speed < 1f) speed = 50f;
                
                lifeTime = _skillCard.Attributes.GetFinal(AttributeType.SkillDuration);
                if (lifeTime <= 0f) lifeTime = 5f;
                
                knockUpVal = _skillCard.Attributes.GetFinal(AttributeType.SkillKnockUpMonster);
                repelMonsterVal = _skillCard.Attributes.GetFinal(AttributeType.SkillRepelMonster);
                
                // 穿透属性：SkillPenetrateNum 表示最大穿透数量
                float penetrateNum = _skillCard.Attributes.GetFinal(AttributeType.SkillPenetrateNum);
                isPenetrate = penetrateNum > 0f;
                maxPenetrateCount = isPenetrate ? Mathf.Max(1, (int)penetrateNum) : 0;
                
                // 反弹属性：SkillWallRebound > 0 时启用
                float wallRebound = _skillCard.Attributes.GetFinal(AttributeType.SkillWallRebound);
                isBounce = wallRebound > 0f;
            }
        }
        
        /// <summary>
        /// 设置子弹所属的对象池
        /// 
        /// 用途：子弹销毁时自动调用 pool.Return() 回收
        /// 如果未设置，则调用 Destroy(gameObject) 直接销毁
        /// 
        /// 调用时机：SetStart() 之后
        /// </summary>
        /// <param name="pool">子弹预制体所属的对象池</param>
        public void SetPool(QFramework.GenericObjectPool pool)
        {
            _pool = pool;
        }
        
        /// <summary>
        /// 手动设置穿透模式（覆盖自动检测结果）
        /// 
        /// 参数：
        /// - enable: 是否启用穿透
        /// - maxCount: 最大穿透次数（默认999，几乎无限制）
        /// 
        /// 用途：当需要强制穿透或限制穿透次数时调用
        /// 注意：通常不需要手动调用，SetStart 会自动根据属性设置
        /// </summary>
        /// <param name="enable">是否启用穿透</param>
        /// <param name="maxCount">最大穿透次数</param>
        public void SetPenetrate(bool enable, int maxCount = 999)
        {
            isPenetrate = enable;
            maxPenetrateCount = maxCount;
        }
        
        /// <summary>
        /// 手动设置墙壁反弹模式（覆盖自动检测结果）
        /// 
        /// 参数：
        /// - enable: 是否启用反弹
        /// - maxCount: 最大反弹次数（默认3次）
        /// 
        /// 用途：当需要强制反弹或限制反弹次数时调用
        /// 注意：通常不需要手动调用，SetStart 会自动根据属性设置
        /// </summary>
        /// <param name="enable">是否启用反弹</param>
        /// <param name="maxCount">最大反弹次数</param>
        public void SetBounce(bool enable, int maxCount = 3)
        {
            isBounce = enable;
            maxBounceCount = maxCount;
        }

        // ============================================================
        // 每帧逻辑
        // ============================================================
        
        private void Update()
        {
            // 非活跃状态不执行
            if (!_isActive) return;
            
            // 游戏暂停时不执行（支持技能暂停功能）
            if (GamePlayManager.Default != null && GamePlayManager.Default.IsPaused()) return;
            
            // ============================================================
            // 持续伤害模式：每隔 _hitTargetDuration 重置命中记录
            // ============================================================
            // 用途：实现类似"每秒扣血"的持续伤害效果
            // 例如：hitTargetDuration = 1f，则每秒可以对同一目标造成一次伤害
            if (_hitTargetDuration > 0f)
            {
                _hitTargetDurationTime += Time.deltaTime;
                if (_hitTargetDurationTime >= _hitTargetDuration)
                {
                    _hitTargets.Clear();  // 重置后可以让同一目标再次受伤
                    _hitTargetDurationTime = 0f;
                }
            }
            
            // 每帧执行：移动 → 碰撞检测 → 生命周期检查
            Move();
            CheckCollision();
            CheckLifeTime();
        }
        
        // ============================================================
        // 移动逻辑
        // ============================================================
        
        /// <summary>
        /// 子弹移动：沿方向向量匀速飞行
        /// 
        /// 移动公式：position += direction * speed * deltaTime
        /// - direction：已归一化的飞行方向
        /// - speed：每米所需秒数的倒数（speed=50 表示每秒飞50米）
        /// - deltaTime：距离上一帧的秒数
        /// 
        /// 特点：简单高效，适用于大多数直射子弹
        /// 缺点：不支持曲线运动、追踪等复杂轨迹
        /// </summary>
        private void Move()
        {
            transform.position += _direction * speed * Time.deltaTime;
        }
        
        // ============================================================
        // 碰撞检测逻辑
        // ============================================================
        
        /// <summary>
        /// 碰撞检测：射线检测敌人 + 墙壁反弹
        /// 
        /// 检测流程：
        /// 1. 以当前位置为起点，沿飞行方向发射射线
        /// 2. 射线长度 = 帧移动距离 + 检测半径
        /// 3. 检测所有与 enemyLayer 匹配的碰撞体
        /// 4. 命中名为 "HitBox" 的碰撞体后获取 MonsterCard
        /// 5. 检查是否已命中过，根据穿透/持续伤害设置决定后续行为
        /// 
        /// 关键约定：
        /// - 怪物必须有名为 "HitBox" 的子碰撞体
        /// - HitBox 的父物体必须是 MonsterCard 组件所在对象
        /// </summary>
        private void CheckCollision()
        {
            Vector2 currentPos = transform.position;
            // 射线长度 = 本帧移动距离 + 额外检测范围
            // 保证子弹"跑过"的路径也被检测到，避免高速穿透漏检
            float moveDistance = speed * Time.deltaTime;
            
            // 射线检测敌人
            // RaycastAll 返回所有命中的碰撞体（按距离排序）
            RaycastHit2D[] hits = Physics2D.RaycastAll(currentPos, _direction, moveDistance + checkRadius, enemyLayer);
            
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                
                // 关键约定：怪物必须有名为 "HitBox" 的子物体作为碰撞体
                if (hit.collider.name != "HitBox") continue;
                
                // 通过父物体获取 MonsterCard
                MonsterCard monster = hit.collider.transform.parent.GetComponent<MonsterCard>();
                if (monster == null || !monster.enabled) continue;
                
                // 检查是否已命中此目标
                // HashSet.Add() 返回 false 表示已存在
                string uniqueId = monster.uniqueId;
                if (!_hitTargets.Add(uniqueId))
                {
                    // 已命中过，检查是否允许持续伤害模式
                    // _hitTargetDuration > 0 才允许重复伤害
                    if (_hitTargetDuration <= 0f) continue;
                }
                
                // ============================================================
                // 造成伤害
                // ============================================================
                ApplyDamage(monster, hit.point);
                
                // 触发命中事件（外部可订阅播放特效等）
                OnHit?.Invoke(hit.point);
                
                // ============================================================
                // 穿透/销毁判断
                // ============================================================
                if (!isPenetrate)
                {
                    // 非穿透模式：命中后立即销毁
                    DestroyOrReturn();
                    return;
                }
                else
                {
                    // 穿透模式：穿透计数 +1
                    _penetrateCount++;
                    if (_penetrateCount >= maxPenetrateCount)
                    {
                        // 达到最大穿透次数后销毁
                        DestroyOrReturn();
                        return;
                    }
                    // 未达上限则继续飞行，可以继续穿透下一个敌人
                }
            }
            
            // ============================================================
            // 墙壁反弹检测（仅 isBounce = true 时）
            // ============================================================
            if (isBounce)
            {
                RaycastHit2D wallHit = Physics2D.Raycast(currentPos, _direction, moveDistance + 0.1f, wallLayer);
                if (wallHit.collider != null)
                {
                    // 执行反弹
                    Bounce(wallHit.normal);
                }
            }
        }
        
        // ============================================================
        // 伤害应用
        // ============================================================
        
        /// <summary>
        /// 对命中目标应用伤害和各种效果
        /// 
        /// 包含以下效果：
        /// 1. 基础伤害：monster.TakeDamageTypes(_skillCard, hitPos)
        /// 2. 击飞效果：monster.KnockUpMonster(knockUpVal)
        /// 3. 击退效果：monster.RepelMonster(repelMonsterVal)
        /// 4. 受击特效：从对象池播放 SkillHitEffect
        /// </summary>
        /// <param name="monster">被命中的怪物</param>
        /// <param name="hitPoint">命中点世界坐标</param>
        private void ApplyDamage(MonsterCard monster, Vector2 hitPoint)
        {
            // 基础伤害：通过技能系统统一处理
            Vector3 damageHitPos = hitPoint;
            monster.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(monster, _skillCard, damageHitPos));
            
            // 击飞效果：怪物被垂直击飞
            // knockUpVal 为正值时生效，值越大飞得越高
            if (knockUpVal > 0f)
            {
                monster.KnockUpMonster(knockUpVal);
            }
            
            // 击退效果：怪物被水平推动
            // repelMonsterVal > 0 向飞行方向击退
            // repelMonsterVal < 0 向飞行反方向拉扯
            if (repelMonsterVal != 0f)
            {
                monster.RepelMonster(repelMonsterVal);
            }
            
            // 播放受击特效：从对象池获取并播放
            SpawnHitEffect(damageHitPos);
        }
        
        // ============================================================
        // 墙壁反弹
        // ============================================================
        
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
        private void Bounce(Vector2 normal)
        {
            // 达到最大反弹次数后销毁
            if (_bounceCount >= maxBounceCount)
            {
                DestroyOrReturn();
                return;
            }
            
            // Vector2.Reflect 计算反射方向
            // 公式：R = D - 2 * (D·N) * N
            // D = 入射方向, N = 法线, R = 反射方向
            _direction = Vector2.Reflect(_direction, normal).normalized;
            
            // 更新子弹旋转角度
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
            
            _bounceCount++;
            
            // 清空已命中列表，允许反弹后再次伤害同一目标
            // 否则反弹后子弹会穿过之前击中的敌人
            _hitTargets.Clear();
        }
        
        // ============================================================
        // 生命周期管理
        // ============================================================
        
        /// <summary>
        /// 检查子弹是否应该销毁/回收
        /// 
        /// 三种销毁条件（满足任一即销毁）：
        /// 1. 飞行距离超出 maxDistance
        /// 2. 生存时间超过 lifeTime
        /// 3. 飞出屏幕边界
        /// </summary>
        private void CheckLifeTime()
        {
            // 条件1：飞行距离超限
            float flyDistance = Vector3.Distance(transform.position, _startPos);
            if (flyDistance >= maxDistance)
            {
                DestroyOrReturn();
                return;
            }
            
            // 条件2：生存时间超限
            if (Time.time - _startTime >= lifeTime)
            {
                DestroyOrReturn();
                return;
            }
            
            // 条件3：飞出屏幕
            if (!IsInScreen(transform.position))
            {
                DestroyOrReturn();
                return;
            }
        }
        
        /// <summary>
        /// 判断世界坐标点是否在屏幕范围内
        /// 
        /// 使用视口坐标判断：
        /// - (0,0) 为屏幕左下角，(1,1) 为右上角
        /// - 允许超出 [-0.5, 1.5] 范围，防止屏幕边缘的子弹过早消失
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <returns>true=在屏幕范围内，false=已出屏幕</returns>
        private bool IsInScreen(Vector3 position)
        {
            if (Camera.main == null) return true;
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(position);
            // 允许 -0.5~1.5 范围，比屏幕略大，避免边缘子弹闪没
            return screenPoint.x > -0.5f && screenPoint.x < 1.5f && 
                   screenPoint.y > -0.5f && screenPoint.y < 1.5f;
        }
        
        // ============================================================
        // 受击特效
        // ============================================================
        
        /// <summary>
        /// 在命中点播放受击特效
        /// 
        /// 特效来源：_skillCard.Config.SkillHitEffect（技能配置表）
        /// 特效对象池：ObjectPoolManager 管理
        /// 特效缩放：固定 200 倍
        /// 自动回收：0.5秒后回池
        /// 
        /// 注意：如果 Config.SkillHitEffect 为空或对象池不存在则不播放
        /// </summary>
        /// <param name="position">特效播放位置（世界坐标）</param>
        private void SpawnHitEffect(Vector3 position)
        {
            // 检查技能配置是否有受击特效
            if (_skillCard == null || string.IsNullOrEmpty(_skillCard.Config.SkillHitEffect)) return;
            
            // 从对象池获取特效预制体（不存在则创建）
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(_skillCard.Config.SkillHitEffect);
            if (pool == null) return;
            
            var effect = pool.Get();
            if (effect == null) return;
            
            // 设置父物体、位置、缩放
            effect.transform.SetParent(GameLand.Default.DamageParent.transform);
            effect.transform.position = position;
            effect.transform.localScale = Vector3.one * 200;
            
            // 0.5秒后自动回池
            DOVirtual.DelayedCall(0.5f, () => pool.Return(effect));
        }
        
        // ============================================================
        // 销毁/回收逻辑
        // ============================================================
        
        /// <summary>
        /// 子弹使命结束，统一处理销毁或回收
        /// 
        /// 流程：
        /// 1. 设置 _isActive = false（防止重复触发）
        /// 2. 触发 OnComplete 事件（通知外部）
        /// 3. 优先使用对象池回收，备选直接销毁
        /// 
        /// 调用时机：
        /// - 命中敌人（非穿透模式）
        /// - 穿透次数耗尽
        /// - 反弹次数耗尽
        /// - 飞行距离超限
        /// - 生存时间耗尽
        /// - 飞出屏幕
        /// </summary>
        private void DestroyOrReturn()
        {
            if (!_isActive) return;
            _isActive = false;
            
            // 通知外部子弹即将销毁（如技能可以做额外处理）
            OnComplete?.Invoke();
            
            // 有对象池则回收，无则直接销毁
            if (_pool != null)
            {
                _pool.Return(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 重置所有运行时状态
        /// 
        /// 调用时机：
        /// - OnDisable()（对象被禁用/回池时）
        /// 
        /// 目的：为对象池复用做准备，确保下次使用时状态干净
        /// </summary>
        private void ResetState()
        {
            _isActive = false;
            _skillCard = null;
            _hitTargets.Clear();
            _penetrateCount = 0;
            _bounceCount = 0;
            _hitTargetDurationTime = 0f;
            OnHit = null;
            OnComplete = null;
        }
    }
}
