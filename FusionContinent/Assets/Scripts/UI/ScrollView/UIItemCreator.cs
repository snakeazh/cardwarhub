using System;
using System.Collections;
using System.Collections.Generic;
using FusionContinent.Pool;
using UnityEngine;

public class UIItemCreator
{
    private static UIItemCreator instance;

    public static UIItemCreator Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new UIItemCreator();
            }
            
            return instance;
        }
    }

    public RectTransform GetPrefab(string name,RectTransform parent)
    {
        var pool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate(name);
        return pool.Get(parent).GetComponent<RectTransform>();
    }

    public void ReleasePrefab(string name, RectTransform key)
    {
        var pool = CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate(name);
        pool.Recycle(key.gameObject);
    }
}
