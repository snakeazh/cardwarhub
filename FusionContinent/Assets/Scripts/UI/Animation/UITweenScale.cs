using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;


namespace FusionContinent.Animation
{
    [System.Serializable]
    public class TweenScaleData
    {
        public Vector3 startScale;
        public Vector3 endScale;
        public RectTransform target;
    }
    public class UITweenScale : UIAnimation
    {
        public TweenScaleData tweenScaleData;

        
        protected override void OnPlay()
        {
            if (tweenScaleData.target != null)
            {
                tweenScaleData.target.DOKill();
                tweenScaleData.target.localScale = tweenScaleData.startScale;
                tweenScaleData.target.DOScale(tweenScaleData.endScale, ReadTimer.duration).OnComplete(this.Complete).SetUpdate(true);
            }
        }
    }

}
