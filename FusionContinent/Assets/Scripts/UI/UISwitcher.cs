using UnityEngine;
using UnityEngine.UI; // 用于 Button
using DG.Tweening;
using UnityEngine.Events; // DOTween 核心

public class UISwitcher : MonoBehaviour
{
    [Header("UI 对象 (需挂载 RectTransform)")]
    public RectTransform ui1; // 界面1
    public RectTransform ui2; // 界面2
    public Button toggleButton1; // 切换按钮
    public Button toggleButton2; // 切换按钮

    
    [Header("动画参数")]
    public float moveDuration = 0.5f;   // 移动耗时
    public float punchDuration = 0.2f;  // Q弹耗时
    public Vector3 punchStrength = new Vector3(0.2f, 0.2f, 0); // Q弹力度(缩放增量)
    public float moveDistance = 800f;   // 向左移出的距离 (像素)

    private bool isUI1Active = true;    // 当前状态标记
    private Vector2 centerPos;          // 屏幕中心位置 (显示点)
    private Vector2 leftPos;            // 屏幕左侧位置 (隐藏点)
    // private UnityEvent OnHideCmplete;
    
    void Start()
    {
        DOTween.Init();

        // 记录初始位置
        centerPos = ui1.anchoredPosition;
        leftPos = centerPos + new Vector2(-moveDistance, 0);
        ui1.anchoredPosition = centerPos; // UI1 在中间
        ui2.anchoredPosition = leftPos;   // UI2 在左边隐藏

        // 绑定按钮事件
        if (toggleButton1 != null)
            toggleButton1.onClick.AddListener(OnToggleClicked);
        
        if (toggleButton2 != null)
            toggleButton2.onClick.AddListener(OnToggleClicked);
    }

    void OnToggleClicked()
    {
        // 避免动画冲突，先停止当前对象上正在播放的动画
        ui1.DOKill();
        ui2.DOKill();

        if (isUI1Active)
        {
            SwitchToUI2();
        }
        else
        {
            SwitchToUI1();
        }
        isUI1Active = !isUI1Active;
    }

    // 切换到 UI2 (UI1 退出，UI2 进入)
    public void SwitchToUI2()
    {
        Sequence seq = DOTween.Sequence();

        // --- 1. UI1 退出流程：直接开始移出 (Append) ---
        // 移除 Q弹调用。UI1 从中心移到左侧。
        seq.Append(ui1.DOAnchorPos(leftPos, moveDuration).SetEase(Ease.InBack));

        // --- 2. UI2 进入流程：同时开始移入 (Insert T=0) ---
        // 移除 Q弹时间，让它与 UI1 同时开始移动 (T=0)
        seq.Insert(0, 
            // 移除 OutBack，改为平滑的 Ease.OutSine
            ui2.DOAnchorPos(centerPos, moveDuration).SetEase(Ease.OutSine) 
        );
    }

    // 切换到 UI1 (UI2 退出，UI1 进入)
    public void SwitchToUI1()
    {
        Sequence seq = DOTween.Sequence();

        // --- 1. UI2 退出流程：直接开始移出 (Append) ---
        // 移除 Q弹调用。UI2 从中心移到左侧。
        seq.Append(ui2.DOAnchorPos(leftPos, moveDuration).SetEase(Ease.InBack));

        // --- 2. UI1 进入流程：同时开始移入 (Insert T=0) ---
        seq.Insert(0, 
            // 移除 OutBack，改为平滑的 Ease.OutSine
            ui1.DOAnchorPos(centerPos, moveDuration).SetEase(Ease.OutSine) 
        );
        
    }

    
}