using FusionContinent;
using UnityEngine;

public class LaserSweepMove : IProjectileMove
{
    // sweepAngle：单侧最大角度（度），与 AboutScan rotation 一致
    // sweepDuration：完整来回总时长（秒），均分 4 段匀速
    // 轨迹：基准 → 左 → 基准 → 右 → 基准，无缝循环
    private float _maxAngle;
    private float _segDuration;
    private float _elapsed;
    private float _baseZ;    // 发射时基准欧拉角，初始化一次不再变
    private bool _started;

    public LaserSweepMove(float speed, float sweepAngle, float sweepDuration)
    {
        _maxAngle = sweepAngle;
        _segDuration = sweepDuration * 0.25f; // 总时长的 1/4
        _started = false;
    }

    public void Move(Projectile_new p, float dt)
    {
        if (!_started)
        {
            _started = true;
            _elapsed = 0f;
            _baseZ = p.transform.rotation.eulerAngles.z; // 发射时的欧拉角，只读一次
        }

        _elapsed += dt;

        float totalProgress = _elapsed / _segDuration;
        int seg = ((int)totalProgress) % 4;
        float localT = totalProgress - (int)totalProgress;

        float fromZ, toZ;
        switch (seg)
        {
            case 0: fromZ = _baseZ;           toZ = _baseZ - _maxAngle; break;
            case 1: fromZ = _baseZ - _maxAngle; toZ = _baseZ;            break;
            case 2: fromZ = _baseZ;           toZ = _baseZ + _maxAngle; break;
            default: fromZ = _baseZ + _maxAngle; toZ = _baseZ;          break;
        }

        float newZ = Mathf.Lerp(fromZ, toZ, localT);
        p.transform.rotation = Quaternion.Euler(0, 0, newZ);
    }
}
