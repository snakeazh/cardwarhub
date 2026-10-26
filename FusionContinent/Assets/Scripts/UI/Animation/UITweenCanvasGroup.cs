using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Animation
{
    [System.Serializable]
    public class TweenCanvasAlphaData
    {
        public float startAlpha;
        public float endAlpha;
        public CanvasGroup target;
    }
    public class UITweenCanvasGroup : UIAnimation
    {
        public TweenCanvasAlphaData alphaData;
        protected override void OnPlay()
        {
            if (alphaData.target != null)
            {
                alphaData.target.DOKill();
                alphaData.target.alpha = alphaData.startAlpha;
                alphaData.target.DOFade(alphaData.endAlpha, ReadTimer.duration).OnComplete(this.Complete).SetUpdate(true);
            }
        }
    }
}