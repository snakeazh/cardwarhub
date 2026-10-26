using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureChestAnimation : MonoBehaviour
{
    public Animator animator;

    public Animator Animator
    {
        get
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            return animator;
        }
    }

    private void Awake()
    {
     
    }

    public string RuntimeAnimationName;
    public bool closeOnDone = false;
    public Action OnPlayDone;
    public void PlayAnimation(string animationName ,bool _closeOnDone = false,Action playDone = null)
    {
        RuntimeAnimationName = animationName;
        closeOnDone = _closeOnDone;
        OnPlayDone = playDone;
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        animationCoroutine = StartCoroutine(StartAnimation());
    }
    Coroutine animationCoroutine;
    IEnumerator StartAnimation()
    {
        this.animator.enabled = true;
        this.animator.Play(RuntimeAnimationName, 0, 0f);
        yield return null;
        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        while (stateInfo.IsName(RuntimeAnimationName) && stateInfo.normalizedTime < 1.0f)
        {
            yield return null;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            // 判断动画是否播放完成
            if ( stateInfo.normalizedTime >= 1.0f)
            {
                if (closeOnDone)
                {
                    // this.animator.enabled = false;
                }
            
                if (OnPlayDone != null)
                {
                    OnPlayDone();
                }
            }
        }
    }
}
