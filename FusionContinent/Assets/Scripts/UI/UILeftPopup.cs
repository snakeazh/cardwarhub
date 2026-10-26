using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.UI;

public class UILeftPopup : MonoBehaviour
{
    [Header("核心引用")]
    [Tooltip("请拖入实际滑动的面板对象 (SlidingPanel)")]
    [SerializeField] private RectTransform slidingPanel; 
    
    [Tooltip("请拖入遮罩对象的 CanvasGroup (BackgroundMask)")]
    [SerializeField] private CanvasGroup backgroundMask; 
    
    [Header("动画设置 (已调优)")]
    // 修改1：时间缩短，0.25秒通常是“干脆”的黄金时间
    [SerializeField] private float animationDuration = 0.25f;
    
    // 修改2：使用 OutQuart 或 OutExpo，去除回弹，快速刹车
    [SerializeField] private Ease showEase = Ease.OutQuart;
    
    // 修改3：退出时使用 InQuart，加速离开
    [SerializeField] private Ease hideEase = Ease.InQuart;
    
    [Header("事件")]
    public UnityEvent OnShowComplete;
    public UnityEvent OnHideComplete;
    
    private Vector2 originalPosition;
    private Vector2 hiddenPosition;
    
    void Awake()
    {
        if (slidingPanel == null) slidingPanel = GetComponent<RectTransform>();

        // 记录原始位置
        originalPosition = slidingPanel.anchoredPosition;
        
        // 计算隐藏位置
        hiddenPosition = new Vector2(
            originalPosition.x - slidingPanel.rect.width - 2000f, 
            originalPosition.y
        );
        
        // 初始化遮罩
        if (backgroundMask != null)
        {
            backgroundMask.alpha = 0f;
            backgroundMask.blocksRaycasts = false;
            if (backgroundMask.TryGetComponent<Button>(out var btn))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Hide);
            }
        }
        
        slidingPanel.anchoredPosition = hiddenPosition;
    }

    private void Start()
    {
        // 延迟一帧显示，确保布局计算完成
        DOVirtual.DelayedCall(0.1f, Show);
    }

    public void Show()
    {
        DOTween.Kill(slidingPanel);
        if (backgroundMask != null) DOTween.Kill(backgroundMask);

        this.gameObject.SetActive(true);

        // 面板：快速滑入
        slidingPanel.DOAnchorPos(originalPosition, animationDuration)
            .SetEase(showEase)
            .SetUpdate(true) // 即使游戏暂停(Time.timeScale=0)也能播放动画
            .OnComplete(() => OnShowComplete?.Invoke());

        // 遮罩：同步淡入
        if (backgroundMask != null)
        {
            backgroundMask.blocksRaycasts = true;
            backgroundMask.DOFade(1f, animationDuration).SetUpdate(true);
        }
    }
    
    public void Hide()
    {
        DOTween.Kill(slidingPanel);
        if (backgroundMask != null) DOTween.Kill(backgroundMask);

        // 面板：快速滑出
        slidingPanel.DOAnchorPos(hiddenPosition, animationDuration)
            .SetEase(hideEase)
            .SetUpdate(true)
            .OnComplete(() => {
                OnHideComplete?.Invoke();
                // 可选：动画结束后禁用
                // this.gameObject.SetActive(false); 
            });

        // 遮罩：同步淡出
        if (backgroundMask != null)
        {
            backgroundMask.DOFade(0f, animationDuration)
                .SetUpdate(true)
                .OnComplete(() => backgroundMask.blocksRaycasts = false);
        }
    }
    
    public void Toggle()
    {
        if (slidingPanel.anchoredPosition.x < originalPosition.x - 10f)
            Show();
        else
            Hide();
    }

    void OnEnable()
    {
        // 每次激活都重置位置重新播放 Show 动画
        slidingPanel.anchoredPosition = hiddenPosition;
        if (backgroundMask != null) backgroundMask.alpha = 0f;
        Show();
    }
    
    void OnDestroy()
    {
        DOTween.Kill(slidingPanel);
        if (backgroundMask != null) DOTween.Kill(backgroundMask);
    }
}