using System;
using System.Collections;
using System.Collections.Generic;
using FusionContinent;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance;

    private List<Projectile_new> list = new List<Projectile_new>();

    private List<Projectile_new> hasDestroyedList = new List<Projectile_new>();
    
    void Awake()
    {
        Instance = this;
    }

    void FixedUpdate()
    {
        if (GamePlayManager.Default != null && GamePlayManager.Default.IsPaused())
        {
            return;
        }

        float dt = Time.deltaTime * Global.SpeedMultiplier.Value;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null || list[i].destoryself) continue;  // ❗关键
                list[i].Tick(dt);
        }

        if (hasDestroyedList.Count > 0)
        {
            for (int i = hasDestroyedList.Count - 1; i >= 0; i--)
            {
                DestroyProjectile(hasDestroyedList[i]);
            }
        }
    }

    public void DestroyProjectile(Projectile_new p)
    {
        list.Remove(p);
        hasDestroyedList.Remove(p);
        if (p != null && EffectManager.Instance != null)
        {
            EffectManager.Instance.RecycleEffect(p.gameObject);
        }
    }

    public void Add(Projectile_new p)
    {
        list.Add(p);
    }

    public void Recycle(Projectile_new p)
    {
        if (p == null) return;
        if (!hasDestroyedList.Contains(p)) // ❗关键
        {
            //投射物加入待销毁里面 之后统一销毁
            hasDestroyedList.Add(p);
        }
    }

    public void CancelRecycle(Projectile_new p)
    {
        if (p == null)
        {
            return;
        }

        hasDestroyedList.Remove(p);
    }

    private void OnDestroy()
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            Projectile_new p = list[i];
            if (p != null && EffectManager.Instance != null)
            {
                EffectManager.Instance.RecycleEffect(p.gameObject);
            }
            list.Remove(p);
        }
        hasDestroyedList.Clear();
    }
}
