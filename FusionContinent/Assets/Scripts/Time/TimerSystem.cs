using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Collections.Generic;
using UnityEngine;

public class TimerSystem : MonoBehaviour
{
    public static TimerSystem Instance;

    private List<Timer> timers = new();
    private Stack<Timer> pool = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        float delta = Time.deltaTime;

        for (int i = timers.Count - 1; i >= 0; i--)
        {
            var timer = timers[i];
            timer.elapsed += delta;

            if (timer.elapsed >= timer.duration)
            {
                timer.callback?.Invoke();

                if (timer.loop)
                {
                    timer.elapsed = 0f;
                }
            }
        }

        for (int i = timers.Count - 1; i >= 0; i--)
        {
            if (timers[i].IsFinished)
            {
                Recycle(timers[i]);
                timers.RemoveAt(i);
            }
        }
    }

    #region 创建Timer
    public Timer Create(float duration, Action callback, bool loop = false)
    {
        Timer timer = pool.Count > 0 ? pool.Pop() : new Timer();

        timer.Reset(duration, loop, callback);
        timers.Add(timer);

        return timer;
    }
    #endregion

    #region 取消Timer
    public void Cancel(Timer timer)
    {
        if (timers.Remove(timer))
        {
            Recycle(timer);
        }
    }
    #endregion

    #region 回收
    private void Recycle(Timer timer)
    {
        timer.callback = null;
        pool.Push(timer);
    }
    #endregion
}


public class Timer
{
    public float duration;
    public float elapsed;
    public bool loop;

    public Action callback;

    public bool IsFinished => !loop && elapsed >= duration;

    public void Reset(float duration, bool loop, Action callback)
    {
        this.duration = duration;
        this.loop = loop;
        this.callback = callback;
        this.elapsed = 0f;
    }
}