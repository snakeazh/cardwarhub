using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Animation
{
    
    [System.Serializable]
    public class TweenAlphaData
    {
        public float startAlpha;
        public float endAlpha;
        public MaskableGraphic target;
    }
    public class UITweenAlpha : UIAnimation
    {
        public TweenAlphaData alphaData;
        protected override void OnPlay()
        {
            if (alphaData.target != null)
            {
                alphaData.target.DOKill();
                alphaData.target.color  =  new Color(alphaData.target.color.r,alphaData.target.color.g,alphaData.target.color.b,alphaData.startAlpha);
                alphaData.target.DOFade(alphaData.endAlpha, ReadTimer.duration).OnComplete(this.Complete).SetUpdate(true);
            }
        }
    }
}