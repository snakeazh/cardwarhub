using System;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

public class AngleBullet : MonoBehaviour
{
    [Header("子弹设置")]
    public float speed = 300f;
    public float lifetime = 5f;
    public LayerMask wallLayer;
    public LayerMask enemyLayer; // 添加敌人层级

    private Vector2 _direction;
    private Tween _moveTween;
    private RectTransform _rectTransform;
    
    private float _elapsedTime;
    private float vertigotime;
    private bool _hasPassedAngleWall = false; // 新增：标记是否已经通过anglewalldown

    private ISkill _skillCard;

    public void SetElectricBall(ISkill skillCard, float lifetime, float speed, float vertigotime)
    {
        this._skillCard = skillCard;
        this.lifetime = lifetime;
        this.speed = speed;
        this.vertigotime = vertigotime;
        Initialize(Vector2.up); // 默认向上
    }
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(Vector2 direction)
    {
        _direction = direction.normalized;
        //GameLand.Default.anglewalldown.localScale = Vector3.zero;
        _elapsedTime = 0f;
        _hasPassedAngleWall = false; // 重置标记
        MoveBullet();
        DOTween.To(v => { }, 0, 0, lifetime).SetEase(Ease.Linear).onComplete += () =>
        {
            Destroy(this.gameObject);
        };
    }

    private void FixedUpdate()
    {
        _elapsedTime += Time.deltaTime;
        
        // 检测是否已经通过anglewalldown
        if (!_hasPassedAngleWall && this.transform.position.y > GameLand.Default.anglewalldown.position.y)
        {
            _hasPassedAngleWall = true; // 标记为已通过
        }
        if(!_hasPassedAngleWall) return;
        
        // 碰撞检测
        Vector2 worldPos = _rectTransform.position;
        Vector2 rayDir = _direction;
        float rayDistance = 1f;

        // 墙壁碰撞检测
        RaycastHit2D wallHit = Physics2D.Raycast(worldPos, rayDir, rayDistance, wallLayer);
        if (wallHit.collider != null)
        {
            // 如果是anglewalldown且还没有通过，则不触发反弹
            if (wallHit.collider.transform == GameLand.Default.anglewalldown && !_hasPassedAngleWall)
            {
                // 第一次碰撞到anglewalldown，不反弹，直接穿透
                return;
            }
            
            // 获取反射方向
            Vector2 normal = wallHit.normal;
            Vector2 reflectDir = Vector2.Reflect(_direction, normal).normalized;
            _direction = reflectDir;

            // 修正旋转角度计算（子弹头朝上时的正确角度）
            float newAngle = Mathf.Atan2(reflectDir.y, reflectDir.x) * Mathf.Rad2Deg;
            _rectTransform.rotation = Quaternion.Euler(0, 0, newAngle);

            // 重新移动
            _moveTween.Kill();
            MoveBullet();
        }

        // 敌人伤害检测（使用射线检测）
        RaycastHit2D enemyHit = Physics2D.Raycast(worldPos, rayDir, rayDistance, enemyLayer);
        if (enemyHit.collider != null)
        {
            // 确保只检测到HitBox
            if (enemyHit.collider.name == "HitBox")
            {
                MonsterCard hitTarget = enemyHit.collider.transform.parent.GetComponent<MonsterCard>();
                if (hitTarget != null)
                {
                    // 受伤坐标位置
                    var damageHitPos = enemyHit.collider.bounds.ClosestPoint(worldPos);
                    try
                    {
                        hitTarget.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(hitTarget, _skillCard, damageHitPos));
                        // 可以在这里添加异常状态设置
                        // enemyHit.collider.transform.parent.GetComponent<MonsterAbnormal>().SetAbnormal(UnusualStatusType.Vertigo, vertigotime, 0);
                        
                        // 伤害检测后可以销毁子弹或继续飞行
                        // Destroy(this.gameObject);
                    }
                    catch (Exception e)
                    {
                        LogTool.LogError($"伤害处理错误: {e.Message}");
                    }
                }
            }
        }
    }

    private void MoveBullet()
    {
        Vector2 startPos = _rectTransform.anchoredPosition;
        float remainingTime = lifetime - _elapsedTime;

        // 关键：根据当前旋转角度计算移动方向
        float currentZRotation = _rectTransform.eulerAngles.z + 90;
        _direction = Quaternion.Euler(0, 0, currentZRotation) * Vector2.right;
        // 计算目标位置
        Vector2 targetPos = startPos + (_direction * speed * remainingTime);

        _moveTween = _rectTransform.DOAnchorPos(targetPos, remainingTime)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
            });
    }
    
    // 保留原有的OnTriggerEnter2D作为备用，或者可以删除
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 如果已经使用射线检测，可以注释或删除这个方法
        /*
        if (collision.name != "HitBox")
        {
            return;
        }
        LogTool.LogError("触发器检测到敌人");
        MonsterCard hitTarget = collision.transform.parent.GetComponent<MonsterCard>();
        if (hitTarget != null)
        {
            var damageHitPos = collision.bounds.ClosestPoint(transform.position);
            try
            {
                LogTool.LogError("对敌人造成伤害");
                hitTarget.TakeDamageTypes(_skillCard, damageHitPos);
            }
            catch
            {
            }
        }
        */
    }

    private void OnDestroy()
    {
        _moveTween?.Kill();
    }
}