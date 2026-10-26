using UnityEngine;

public class MoveToPositionMove : IProjectileMove
{
    private readonly Vector3 _targetPos;
    private readonly float _speed;
    private readonly float _arriveDistance;
    private bool _hasTriggeredHit;

    public MoveToPositionMove(Vector3 targetPos, float speed, float arriveDistance = 0.05f)
    {
        _targetPos = targetPos;
        _speed = speed;
        _arriveDistance = arriveDistance;
    }

    public void Move(Projectile_new p, float dt)
    {
        if (_hasTriggeredHit)
        {
            return;
        }

        Vector3 current = p.transform.position;
        Vector3 toTarget = _targetPos - current;
        float distance = toTarget.magnitude;

        if (distance <= _arriveDistance)
        {
            p.transform.position = _targetPos;
            _hasTriggeredHit = true;
            p.TriggerHitModulesAtCurrentPosition();
            return;
        }

        Vector3 dir = toTarget.normalized;
        p.Direction = dir;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        p.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

        float moveDistance = _speed * dt;
        if (moveDistance >= distance)
        {
            p.transform.position = _targetPos;
            _hasTriggeredHit = true;
            p.TriggerHitModulesAtCurrentPosition();
            return;
        }

        p.transform.position += dir * moveDistance;
    }
}
