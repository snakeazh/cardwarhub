using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Animation
{
    public class UITweenCloseScale : UITweenScale
    {
        protected override void OnPlay()
        {
            if (tweenScaleData.target != null)
            {
                tweenScaleData.target.DOKill();
                tweenScaleData.target.DOScale(tweenScaleData.endScale, ReadTimer.duration).OnComplete(this.Complete).SetUpdate(true);
            }
        }
    }
}

