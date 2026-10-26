using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using FusionContinent.Animation;


namespace FusionContinent.Animation
{
    
    [System.Serializable]
    public class TweenMoveData
    {
        public Vector2 startPosition;
        public Vector2 endPosition;
        public RectTransform target;
    }
    public class UITweenMove : UIAnimation
    {
        public TweenMoveData MoveData;

        private UITweenTimer _timer;

        private void Awake()
        {
            _timer = GetComponent<UITweenTimer>();
        }

        [ContextMenu("播放")]
        protected override void OnPlay()
        {
            if (MoveData!= null && _timer!= null)
            {
                MoveData.target.DOKill();
                MoveData.target.anchoredPosition = MoveData.startPosition;
                MoveData.target.DOAnchorPos( MoveData.endPosition, _timer.duration).SetEase(EaseType)
                    .OnComplete(Complete).SetUpdate(true);
            }
        }
    
    }

}
