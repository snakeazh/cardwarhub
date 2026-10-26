using DG.Tweening;
using UnityEngine;

public class CardGo : MonoBehaviour
{
    [SerializeField] private bool flipRight = false; // 默认向左翻转，设为true则向右翻转
    private Sequence flipSequence; // 将动画序列作为成员变量

    // 翻转卡牌（带动画）
    public void FlipCard(bool flipToRight = false)
    {
        // 停止之前的翻牌动画（不影响其他动画）
        if (flipSequence != null && flipSequence.IsActive())
        {
            FlipImmediately(false);
            flipSequence.Kill();
        }
        
        // 重置到初始状态
        ResetToInitialState();
        
        flipRight = flipToRight;
        
        // 创建新的动画序列
        flipSequence = DOTween.Sequence();
        
        // 第一步：稍微放大
        flipSequence.Append(transform.DOScale(new Vector3(1.1f, 1.1f, 1.1f), 0.1f));
        
        // 第二步：根据方向旋转 + 同时恢复原始大小
        float targetYRotation = flipRight ? -180 : 180;
        flipSequence.Append(transform.DORotate(new Vector3(0, targetYRotation, 0), 0.5f));
        flipSequence.Join(transform.DOScale(Vector3.one, 0.5f));
    }

    // 直接显示翻转结果（无动画）
    public void FlipImmediately(bool flipToRight = false)
    {
        // 停止翻牌动画（不影响其他动画）
        if (flipSequence != null && flipSequence.IsActive())
        {
            flipSequence.Kill();
        }
        
        // 重置到初始状态
        ResetToInitialState();
        
        flipRight = flipToRight;
        float targetYRotation = flipRight ? -180 : 180;
        
        // 直接设置最终状态
        transform.rotation = Quaternion.Euler(0, targetYRotation, 0);
        transform.localScale = Vector3.one;
    }

    // 重置到初始状态（未翻转）
    private void ResetToInitialState()
    {
        transform.rotation = Quaternion.Euler(0, 0, 0);
        transform.localScale = Vector3.one;
    }

    // 设置翻转方向
    public void SetFlipDirection(bool right)
    {
        flipRight = right;
    }

    // 当对象禁用时，清理动画
    private void OnDisable()
    {
        if (flipSequence != null)
        {
            flipSequence.Kill();
        }
    }
}