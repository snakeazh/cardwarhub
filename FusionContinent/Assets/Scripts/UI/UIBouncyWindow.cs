using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class UIBouncyWindow : MonoBehaviour
{
    [Header("UI 组件")]
    public RectTransform mainBody; // 界面主体
    // public RectTransform tabBar;   // 顶部的标签栏
    public Button actionButton1;    // 触发按钮
    public Button actionButton2;

    public UnityEvent OnOutScreenRefresh;
    
    [Header("动画参数")]
    public float dropDistance = 1000f; // 向下移出的距离
    public float dropDuration = 0.3f;  // 下落耗时 (越小越快)
    public float returnDuration = 0.5f; // 回弹耗时
    public float tabDelay = 0.15f;      // 标签栏滞后的时间 (秒)
    public float tabBounceAmplitude = 1f; //标签栏回弹振幅

    // 记录初始位置
    private Vector2 mainBodyStartPos;
    private Vector2 tabBarStartPos;

    void Start()
    {
        // 1. 记录初始坐标
        mainBodyStartPos = mainBody.anchoredPosition;
        // tabBarStartPos = tabBar.anchoredPosition;

        // 2. 绑定按钮
        if (actionButton1 != null)
            actionButton1.onClick.AddListener(PlayBounceAnimation);
        
        if (actionButton2 != null)
            actionButton2.onClick.AddListener(PlayBounceAnimation);
    }

    void PlayBounceAnimation()
    {
        // 防止多次点击导致动画冲突，先杀掉当前动画
        mainBody.DOKill();
        // tabBar.DOKill();

        // 确保位置重置（可选，视需求而定，防止连点导致位置偏移）
        mainBody.anchoredPosition = mainBodyStartPos;
        // tabBar.anchoredPosition = tabBarStartPos;

        // 创建序列
        Sequence seq = DOTween.Sequence();

        // --- 第一阶段：快速下落 (Exit) ---
        // 使用 Join 让两者同时发生
        // Ease.InBack 会有一个先向上提再向下冲的预备动作，很有打击感
        // Ease.InCubic 或 InQuart 则是单纯的加速下落
        seq.Append(mainBody.DOAnchorPosY(mainBodyStartPos.y - dropDistance, dropDuration));
        // seq.Join(tabBar.DOAnchorPosY(tabBarStartPos.y - dropDistance, dropDuration).SetEase(Ease.InBack));

        // --- 第二阶段：回弹 (Return) ---
        
        // 1. 主体立刻弹回
        // Append 会在下落完成后立即执行
        // Ease.OutBack 提供“超过终点再弹回来”的Q弹效果
        seq.Append(mainBody.DOAnchorPos(mainBodyStartPos, returnDuration));

        // 2. 标签栏延迟弹回 (实现滞后效果)
        // Insert 允许我们插入到序列的具体时间点
        // 计算公式：下落结束时间 + 延迟时间
        // 这里的 Ease 可以用 OutElastic 做的更夸张一点，或者同样用 OutBack
        // float returnStartTime = dropDuration + tabDelay;
        //
        // seq.Insert(returnStartTime, 
        //     tabBar.DOAnchorPos(tabBarStartPos, returnDuration).SetEase(Ease.OutBack,tabBounceAmplitude)
        // );
        
        
    }
}