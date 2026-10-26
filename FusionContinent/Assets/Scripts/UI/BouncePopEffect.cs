using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Transform))]
public class BouncePopEffect : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("初始放大比例")]
    public float initialScale = 1.2f;
    [Tooltip("缩小比例")]
    public float shrinkScale = 0.8f;
    [Tooltip("初始放大时间")]
    public float expandDuration = 0.2f;
    [Tooltip("缩小时间")]
    public float shrinkDuration = 0.15f;
    [Tooltip("恢复正常时间")]
    public float normalizeDuration = 0.1f;
    [Tooltip("动画缓动类型")]
    public Ease easeType = Ease.OutBack;

    private Vector3 originalScale;
    private Sequence bounceSequence;

    private void Awake()
    {
        // 保存原始大小
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        // 每次激活时播放动画
        PlayBounceAnimation();
    }

    private void OnDisable()
    {
        // 禁用时清理动画
        if (bounceSequence != null && bounceSequence.IsActive())
        {
            bounceSequence.Kill();
        }
        
        // 恢复原始大小
        transform.localScale = originalScale;
    }

    public void PlayBounceAnimation()
    {
        // 重置大小
        transform.localScale = Vector3.zero;

        // 创建动画序列
        bounceSequence = DOTween.Sequence();
        
        // 第一步：放大到初始比例
        bounceSequence.Append(transform.DOScale(originalScale * initialScale, expandDuration).SetEase(easeType));
        
        // 第二步：缩小到shrink比例
        bounceSequence.Append(transform.DOScale(originalScale * shrinkScale, shrinkDuration).SetEase(easeType));
        
        // 第三步：恢复正常大小
        bounceSequence.Append(transform.DOScale(originalScale, normalizeDuration).SetEase(easeType));
        
        // 动画完成时清理序列
        bounceSequence.OnComplete(() => bounceSequence = null);
    }

    // 在编辑器中测试用
    [ContextMenu("测试弹跳效果")]
    public void TestBounceEffect()
    {
        PlayBounceAnimation();
    }
}