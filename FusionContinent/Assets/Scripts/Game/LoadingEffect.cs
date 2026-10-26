using UnityEngine;
using System;
using DG.Tweening;

public class LoadingEffect : MonoBehaviour
{
    public static LoadingEffect Instance { get; private set; }

    [Header("正向物体")]
    public GameObject ForwardObj;

    [Header("反向物体")]
    public GameObject ReverseObj;

    private Animator forwardAnimator;
    private Animator reverseAnimator;

    private Tween currentTween; // 动画时长控制
    private Tween exitTween;    // 收尾动画

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (ForwardObj != null)
            forwardAnimator = ForwardObj.GetComponent<Animator>();

        if (ReverseObj != null)
            reverseAnimator = ReverseObj.GetComponent<Animator>();
    }

    /// <summary>
    /// 播放动画（正向或反向）
    /// </summary>
    public void PlayAnim(bool reverse = false, Action onComplete = null)
    {
        // 停止旧Tween
        currentTween?.Kill();
        exitTween?.Kill();

        GameObject targetObj = reverse ? ReverseObj : ForwardObj;
        Animator targetAnimator = reverse ? reverseAnimator : forwardAnimator;

        if (targetObj == null) return;

        // 如果当前有另一个物体正在显示 → 直接隐藏它（避免空白帧）
        if (!reverse && ReverseObj != null && ReverseObj.activeSelf)
            ReverseObj.SetActive(false);
        if (reverse && ForwardObj != null && ForwardObj.activeSelf)
            ForwardObj.SetActive(false);

        // 确保目标物体显示
        targetObj.SetActive(true);
        targetObj.transform.localScale = Vector3.one;

        // 播放动画
        float animLength = 1f;
        if (targetAnimator != null)
        {
            targetAnimator.Play(0, -1, 0f);
            if (targetAnimator.runtimeAnimatorController.animationClips.Length > 0)
                animLength = targetAnimator.runtimeAnimatorController.animationClips[0].length;
        }

        // 动画结束后 → 收尾
        currentTween = DOVirtual.DelayedCall(animLength, () =>
        {
            exitTween = targetObj.transform.DOScale(Vector3.zero, 0.25f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    targetObj.SetActive(false);
                    onComplete?.Invoke();
                });
        });
    }
}
