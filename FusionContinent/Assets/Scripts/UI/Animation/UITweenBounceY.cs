using System;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Animation
{
    public class UITweenBounceY : UITweenMove
    {
        [Header("动画参数")]
        public float returnDuration = 0.5f; // 回弹耗时
        private Sequence seq = null;
        private void Start()
        {
            seq = DOTween.Sequence();
            
        }

        protected override void OnPlay()
        {
            seq.Kill();
            seq = DOTween.Sequence();
            this.MoveData.target.DOKill();
            this.MoveData.target.anchoredPosition =
                new Vector2(this.MoveData.target.anchoredPosition.x, this.MoveData.startPosition.y);
            // seq.Append(this.MoveData.target.DOAnchorPosY(this.MoveData.endPosition.y - dropDistance, dropDuration));
            seq.Append(this.MoveData.target.DOAnchorPosY(this.MoveData.endPosition.y , returnDuration).SetEase(EaseType)).SetUpdate(true);
            seq.OnComplete(Complete);
        }
    }
}