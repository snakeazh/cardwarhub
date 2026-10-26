using System;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

[RequireComponent(typeof(RectTransform))]
public class UIBottomPopup : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;
    
    [Header("Events")]
    public UnityEvent OnShowComplete;
    public UnityEvent OnHideComplete;
    
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector2 hiddenPosition;
    
    void Awake()
    {
        // 获取RectTransform组件
        rectTransform = GetComponent<RectTransform>();
        
        // 保存原始位置
        originalPosition = rectTransform.anchoredPosition;
        
        // 计算隐藏位置（屏幕底部之外）
        hiddenPosition = new Vector2(
            originalPosition.x,
            -rectTransform.rect.height - 2000f // 额外偏移确保完全隐藏
        );
        
        // 初始状态设置为隐藏
        rectTransform.anchoredPosition = hiddenPosition;
    }

    private void Start()
    {
        this.gameObject.SetActive(false);
        DOTween.To(v => { }, 0, 0, 0.2f).onComplete += () =>
        {
            this.gameObject.SetActive(true);
            Show();
        };
    }

    /// <summary>
    /// 显示UI（从底部弹出）
    /// </summary>
    public void Show()
    {
        // 确保没有正在进行的动画
        DOTween.Kill(rectTransform);
        this.gameObject.SetActive(true);
        // 执行弹出动画
        rectTransform.DOAnchorPos(originalPosition, animationDuration)
            .SetEase(showEase)
            .OnComplete(() => OnShowComplete?.Invoke());
    }
    
    /// <summary>
    /// 隐藏UI（滑回底部）
    /// </summary>
    public void Hide()
    {
        // 确保没有正在进行的动画
        DOTween.Kill(rectTransform);
        
        // 执行隐藏动画
        rectTransform.DOAnchorPos(hiddenPosition, animationDuration)
            .SetEase(hideEase)
            .OnComplete(() => OnHideComplete?.Invoke());
    }
    
    /// <summary>
    /// 切换UI显示状态
    /// </summary>
    public void Toggle()
    {
        if (rectTransform.anchoredPosition.y < originalPosition.y - 10f)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    
    /// <summary>
    /// 立即显示UI（无动画）
    /// </summary>
    public void ShowImmediate()
    {
        DOTween.Kill(rectTransform);
        rectTransform.anchoredPosition = originalPosition;
    }
    
    /// <summary>
    /// 立即隐藏UI（无动画）
    /// </summary>
    public void HideImmediate()
    {
        DOTween.Kill(rectTransform);
        rectTransform.anchoredPosition = hiddenPosition;
    }
    
    // 组件启用时自动注册到管理器（可选）
    void OnEnable()
    {
        rectTransform.anchoredPosition = hiddenPosition;
        Show();
        // 可选的：注册到UI管理器
    }
    
    void OnDisable()
    {
        // 可选的：从UI管理器取消注册
    }
    
    void OnDestroy()
    {
        // 清理DOTween动画
        DOTween.Kill(rectTransform);
    }
}