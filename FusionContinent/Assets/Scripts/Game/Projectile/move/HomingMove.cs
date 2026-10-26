using FusionContinent;
using FusionContinent.Skill.Handler;
using UnityEngine;

public class HomingMove : IProjectileMove
{
    private float _searchSpeed;
    private float _homingSpeed;
    private float _searchDuration;
    private float _homingDelay;
    private float _homingStrength;
    private float _homingRotSpeed;
    private float _wanderMoveDuration;
    private float _wanderWaitDuration;
    private float _homingMoveDuration;
    private float _homingWaitDuration;
    private float _wanderRange;

    private float _timer;
    private float _phaseTimer;
    private bool _hasTarget = false;
    private Vector3 _wanderTargetPos;
    private Transform _target;
    private Transform _lockedTarget;
    private Phase _phase = Phase.Wander;
    private MoveSubPhase _subPhase = MoveSubPhase.Wait;

    private enum Phase
    {
        Wander,
        Homing
    }

    private enum MoveSubPhase
    {
        Move,
        Wait
    }

    public HomingMove(float searchSpeed, float homingSpeed, float searchDuration, float homingDelay,
        float homingRotSpeed, float homingStrength,
        float wanderMoveDuration, float wanderWaitDuration,
        float homingMoveDuration, float homingWaitDuration,
        float wanderRange = 5f, Transform lockedTarget = null)
    {
        _searchSpeed = searchSpeed;
        _homingSpeed = homingSpeed;
        _searchDuration = searchDuration;
        _homingDelay = homingDelay;
        _homingRotSpeed = homingRotSpeed;
        _homingStrength = homingStrength;
        _wanderMoveDuration = wanderMoveDuration;
        _wanderWaitDuration = wanderWaitDuration;
        _homingMoveDuration = homingMoveDuration;
        _homingWaitDuration = homingWaitDuration;
        _wanderRange = wanderRange;
        _wanderTargetPos = Vector3.zero;
        _lockedTarget = lockedTarget;

        if (_lockedTarget != null)
        {
            _target = _lockedTarget;
            _hasTarget = true;
            _phase = Phase.Homing;
            _subPhase = MoveSubPhase.Wait;
        }
    }

    public void Move(Projectile_new p, float dt)
    {
        _phaseTimer += dt;
        _timer += dt;

        switch (_phase)
        {
            case Phase.Wander:
                // 索敌
                if (!_hasTarget)
                {
                    _target = FindTarget(p,p._skillContext);
                    if (_target != null)
                    {
                        _hasTarget = true;
                        _phase = Phase.Homing;
                        _phaseTimer = 0f;
                        _timer = 0f;
                        _subPhase = MoveSubPhase.Wait;
                        LogTool.L($"[HomingMove] 索敌成功: {_target.name}，进入游荡-追踪循环");
                        break;
                    }
                }

                // 无目标时超时换方向
                if (_phaseTimer >= _searchDuration)
                {
                    _phaseTimer = 0f;
                    _wanderTargetPos = GetNextWanderPos(p.transform.position);
                    _subPhase = MoveSubPhase.Wait;
                }

                // 移动/等待子阶段
                if (_subPhase == MoveSubPhase.Move)
                {
                    if (_phaseTimer >= _wanderMoveDuration)
                    {
                        _subPhase = MoveSubPhase.Wait;
                        _phaseTimer = 0f;
                    }
                    else
                    {
                        DoWander(p, dt);
                    }
                }
                else
                {
                    if (_phaseTimer >= _wanderWaitDuration)
                    {
                        _subPhase = MoveSubPhase.Move;
                        _phaseTimer = 0f;
                        _wanderTargetPos = GetNextWanderPos(p.transform.position);
                    }
                }
                break;

            case Phase.Homing:
                if (_target == null || !_target.gameObject.activeInHierarchy)
                {
                    _hasTarget = false;
                    _target = null;
                    _phase = Phase.Wander;
                    _phaseTimer = 0f;
                    _subPhase = MoveSubPhase.Wait;
                    _wanderTargetPos = GetNextWanderPos(p.transform.position);
                    break;
                }

                // 子阶段
                if (_subPhase == MoveSubPhase.Move)
                {
                    if (_phaseTimer >= _homingMoveDuration)
                    {
                        _subPhase = MoveSubPhase.Wait;
                        _phaseTimer = 0f;
                    }
                    else
                    {
                        // 追踪延迟后执行
                        if (_timer >= _homingDelay)
                            DoHoming(p, dt);
                    }
                }
                else
                {
                    if (_phaseTimer >= _homingWaitDuration)
                    {
                        _subPhase = MoveSubPhase.Move;
                        _phaseTimer = 0f;
                    }
                }
                break;
        }
    }

    /// <summary>游荡：朝随机目标点移动（同 Tornado 的 StartMoving）</summary>
    private void DoWander(Projectile_new p, float dt)
    {
        Vector3 current = p.transform.position;
        Vector3 toTarget = _wanderTargetPos - current;
        float dist = toTarget.magnitude;

        if (dist <= 0.01f)
        {
            // 已到达目标，等待下一轮
            return;
        }

        float moveDist = _searchSpeed * dt;
        if (moveDist >= dist)
        {
            // 这一帧就能到达，直接瞬移
            p.transform.position = _wanderTargetPos;
        }
        else
        {
            p.transform.position += toTarget.normalized * moveDist;
        }
    }

    /// <summary>追踪：直接朝目标当前位置直线移动</summary>
    private void DoHoming(Projectile_new p, float dt)
    {
        if (_target == null) return;

        Vector3 targetPos = _target.position;
        Vector3 currentPos = p.transform.position;
        Vector3 toTarget = targetPos - currentPos;
        float dist = toTarget.magnitude;

        if (dist <= 0.01f) return;

        Vector3 dir = toTarget.normalized;
        float moveDist = _homingSpeed * dt;

        if (moveDist >= dist)
        {
            p.transform.position = targetPos;
        }
        else
        {
            p.transform.position += dir * moveDist;
        }
    }

    /// <summary>获取下一个随机游荡目标点（以当前位置为中心，半径内的随机点）</summary>
    private Vector3 GetNextWanderPos(Vector3 current)
    {
        Vector3 randomOffset = Random.insideUnitCircle * _wanderRange;
        Vector3 pos = current + randomOffset;
        pos.z = current.z; // 保持 Z 轴不变
        return pos;
    }

    private Transform FindTarget(Projectile_new p,SkillContext context)
    {
        if (_lockedTarget != null)
        {
            return _lockedTarget.gameObject.activeInHierarchy ? _lockedTarget : null;
        }

        if (context.Target != null && context.Target.gameObject.activeInHierarchy)
            return context.Target.transform;
        float searchRadius = 2f;
        Vector3 searchPos = p.transform.position;
        var colliders = Physics2D.OverlapCircleAll(searchPos, searchRadius, LayerMask.GetMask("MonsterBox"));

        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var col in colliders)
        {
            if (col.name != "HitBox") continue;
            var monster = col.transform.parent.GetComponent<UnityEngine.MonoBehaviour>();
            if (monster == null || !monster.enabled) continue;

            float dist = Vector3.Distance(searchPos, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = col.transform;
            }
        }

        return closest;
    }
}
