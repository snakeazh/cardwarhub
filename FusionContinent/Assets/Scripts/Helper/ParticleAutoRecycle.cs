using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ParticleAutoRecycle : MonoBehaviour
{
    [Header("Play")]
    public bool playOnEnable = true;
    public bool waitOneFrame = true;
    public bool forceClear = true;

    [Header("Auto Recycle")]
    [Tooltip("所有粒子播放完成后自动 SetActive(false)")]
    public bool autoRecycle = true;

    [Tooltip("检测间隔（秒），不建议太小")]
    public float recycleCheckInterval = 0.2f;

    private ParticleSystem[] _systems;
    private Coroutine _playCoroutine;
    private Coroutine _recycleCoroutine;

    void Awake()
    {
        _systems = GetComponentsInChildren<ParticleSystem>(true);
    }

    void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    void OnDisable()
    {
        StopAllCoroutines();
        StopAndClear();
    }

    /// <summary>
    /// 外部播放接口
    /// </summary>
    public void Play()
    {
        StopAllCoroutines();
        _playCoroutine = StartCoroutine(PlayRoutine());
    }

    /// <summary>
    /// 外部强制停止
    /// </summary>
    public void Stop()
    {
        StopAllCoroutines();
        StopAndClear();
    }

    private IEnumerator PlayRoutine()
    {
        if (waitOneFrame)
            yield return new WaitForEndOfFrame();

        foreach (var ps in _systems)
        {
            if (ps == null) continue;

            if (forceClear)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            else
                ps.Stop(true);

            ps.Play(true);
        }

        if (autoRecycle)
            _recycleCoroutine = StartCoroutine(AutoRecycleRoutine());
    }

    private IEnumerator AutoRecycleRoutine()
    {
        // 等所有粒子都播完
        while (true)
        {
            bool anyAlive = false;

            foreach (var ps in _systems)
            {
                if (ps != null && ps.IsAlive(true))
                {
                    anyAlive = true;
                    break;
                }
            }

            if (!anyAlive)
                break;

            yield return new WaitForSeconds(recycleCheckInterval);
        }

        gameObject.SetActive(false);
    }

    private void StopAndClear()
    {
        if (_systems == null) return;

        foreach (var ps in _systems)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
