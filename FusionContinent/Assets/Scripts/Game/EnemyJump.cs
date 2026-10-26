using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EnemyJump : MonoBehaviour
{
    public RectTransform uiElement;  // 需要移动的 UI 元素
    public float JumpmoveDistance = 30f; // 移动的距离
    public float JumpDownmoveDistance = 50f; // 移动的距离
    public float JumpDuration = 1f;   // 移动的持续时间
    public float JumpDownDuration = 0.8f;
    public float DealyTime = 2;
    public RectTransform Shadow;

    void Start()
    {
        // 在游戏开始时调用
        MoveUIElement();
    }

    public void MoveUIElement()
    {
        Shadow.gameObject.SetActive(false);
        DOTween.To(() => DealyTime, a => DealyTime = a, 0f, 1f).OnComplete(() =>
        {
            Shadow.gameObject.SetActive(true);
            Sequence sequence = DOTween.Sequence();
            sequence.Append(uiElement.DOScale(Vector3.one * 1.05f, 1).OnComplete(() =>
            {
                uiElement.DOScale(Vector3.one * 1f, JumpDownDuration);
            }));
            // 使用 DOTween 进行位置动画
            sequence.Join(uiElement.DOLocalMoveY(uiElement.localPosition.y + JumpmoveDistance, JumpDuration).OnComplete(() =>
            {
                Sequence sequence2 = DOTween.Sequence();
                sequence2.Append(Shadow.DOLocalMoveY(Shadow.localPosition.y - JumpDownmoveDistance+ JumpmoveDistance, JumpDownDuration));
                sequence2.Join(uiElement.DOLocalMoveY(uiElement.localPosition.y - JumpDownmoveDistance, JumpDownDuration).OnComplete(MoveUIElement));
            }));

        });

    }
}