using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameObjectDelayShow : MonoBehaviour
{
    public GameObject Target;
    public float Delay;
    private void OnEnable()
    {
        Target.transform.localScale = Vector3.zero;
        Invoke("DelayShow",Delay);
    }

    void DelayShow()
    {
        if (Target != null)
        {
            Target.transform.DOScale(new Vector3(150,150,150), 0.2f);
        }
    }
    
}
