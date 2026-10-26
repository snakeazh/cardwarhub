using System;
using UnityEngine;

/// <summary>
/// 特效生命周期管理组件（优化版）
/// 支持：
/// 1. 多粒子系统自动回收（推荐）
/// 2. Animator 播放完回收
/// 3. 指定时间回收
/// 优先级：Particle > Animator > Timer
/// </summary>
public class EffectLifecycle : MonoBehaviour
{
    private ItemGenericObjectPool _pool;
    private Timer _lifetimeTimer;

    private bool _poolReturned;
    private Action _onRecycled;

    // Particle
    private ParticleSystem[] _particles;
    private bool _hasParticle;

    // Animator
    private Animator _animator;
    private bool _hasAnimator;

    // 状态控制
    private bool _watchByParticle;
    private bool _watchByAnimator;
    private bool _waitParticleAfterTimer;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="resetVisualState">为 false 时跳过 Clear+Play，由调用方统一 Reset（避免重复重启粒子）</param>
    public void Init(ItemGenericObjectPool pool, float lifetime, Action onRecycled = null, bool resetVisualState = true)
    {
        enabled = true;
        _pool = pool;
        _onRecycled = onRecycled;
        _poolReturned = false;
        _waitParticleAfterTimer = false;

        StopAndClearTimer();
        CacheComponents();
        if (resetVisualState)
        {
            ResetState();
        }

        // ===== 优先级判断 =====

        if (_hasParticle)
        {
            // 优先使用粒子系统回收
            _watchByParticle = true;
            _watchByAnimator = false;
        }
        else if (_hasAnimator && lifetime <= 0)
        {
            // 没粒子，用动画
            _watchByAnimator = true;
            _watchByParticle = false;
        }
        else
        {
            // 最后 fallback 到 Timer
            _watchByParticle = false;
            _watchByAnimator = false;

            if (lifetime > 0)
            {
                _lifetimeTimer = TimerSystem.Instance.Create(lifetime, OnLifetimeExpired);
            }
        }
    }

    /// <summary>
    /// 投射物专用：仅按 max_time 定时回收，不监听粒子/动画。
    /// 到期时优先走 Projectile_new.MissionOver，保证与 ProjectileManager 回收链路一致。
    /// </summary>
    public void InitProjectileTimer(ItemGenericObjectPool pool, float maxTime)
    {
        enabled = true;
        _pool = pool;
        _onRecycled = null;
        _poolReturned = false;
        _waitParticleAfterTimer = false;
        _watchByParticle = false;
        _watchByAnimator = false;

        StopAndClearTimer();
        CacheComponents();

        if (maxTime > 0f)
        {
            _lifetimeTimer = TimerSystem.Instance.Create(maxTime, OnProjectileLifetimeExpired);
        }
    }

    private void CacheComponents()
    {
        // 多粒子系统（关键修复点）
        _particles = GetComponentsInChildren<ParticleSystem>(true);
        _hasParticle = _particles != null && _particles.Length > 0;

        // Animator
        _animator = GetComponent<Animator>();
        _hasAnimator = _animator != null && _animator.enabled;

        // 设置 StopAction（优化点）
        if (_hasParticle)
        {
            foreach (var p in _particles)
            {
                if (p == null) continue;

                var main = p.main;
                main.stopAction = ParticleSystemStopAction.Callback;
            }
        }
    }

    private void ResetState()
    {
        // Animator Reset
        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0);
        }

        // 粒子 Reset
        if (_particles != null)
        {
            foreach (var p in _particles)
            {
                if (p == null) continue;

                p.Clear();
                p.Play();
            }
        }
    }

    private void Update()
    {
        if (_poolReturned || _pool == null) return;

        // ===== 粒子模式 =====
        if (_watchByParticle)
        {
            if (!IsAnyParticleAlive())
            {
                ReturnToPool();
            }
            return;
        }

        // ===== Animator 模式 =====
        if (_watchByAnimator && _animator != null)
        {
            if (_animator.IsInTransition(0))
                return;

            var stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

            if (stateInfo.normalizedTime >= 1f && !stateInfo.loop)
            {
                ReturnToPool();
            }
            return;
        }

        // ===== Timer + 等粒子结束 =====
        if (_waitParticleAfterTimer)
        {
            if (!IsAnyParticleAlive())
            {
                ReturnToPool();
            }
        }
    }

    /// <summary>
    /// 粒子 StopAction 回调（无需轮询）
    /// </summary>
    private void OnParticleSystemStopped()
    {
        if (_poolReturned || !_watchByParticle) return;

        if (!IsAnyParticleAlive())
        {
            ReturnToPool();
        }
    }

    private bool IsAnyParticleAlive()
    {
        if (_particles == null) return false;

        foreach (var p in _particles)
        {
            if (p != null && p.IsAlive(true))
            {
                return true;
            }
        }
        return false;
    }

    private void OnLifetimeExpired()
    {
        _lifetimeTimer = null;

        // 时间到了，如果粒子还活着 → 等粒子
        if (_hasParticle && IsAnyParticleAlive())
        {
            _waitParticleAfterTimer = true;
            return;
        }

        ReturnToPool();
    }

    private void OnProjectileLifetimeExpired()
    {
        _lifetimeTimer = null;

        var projectile = GetComponent<Projectile_new>();
        if (projectile != null && !projectile.destoryself)
        {
            projectile.MissionOver();
            return;
        }

        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_poolReturned) return;
        _poolReturned = true;

        StopAndClearTimer();

        _onRecycled?.Invoke();
        _onRecycled = null;

        if (_pool != null)
        {
            _pool.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        _pool = null;
    }

    private void StopAndClearTimer()
    {
        if (_lifetimeTimer != null)
        {
            TimerSystem.Instance.Cancel(_lifetimeTimer);
            _lifetimeTimer = null;
        }
    }

    /// <summary>
    /// 手动回收
    /// </summary>
    public void RecycleNow()
    {
        StopAndClearTimer();
        ReturnToPool();
    }

    /// <summary>
    /// 暂停自动回收（投射物由 ProjectileManager 管理生命周期时使用）
    /// </summary>
    public void SuspendAutoRecycle()
    {
        StopAndClearTimer();
        _watchByParticle = false;
        _watchByAnimator = false;
        _waitParticleAfterTimer = false;
        enabled = false;
    }

    private void OnDisable()
    {
        StopAndClearTimer();
    }

    private void OnDestroy()
    {
        StopAndClearTimer();
    }
}