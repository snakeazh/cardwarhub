using DG.Tweening;
using FusionContinent.Animation;

namespace FusionContinent.Animation
{
    public class UITweenCloseAlpha : UITweenAlpha
    {
        protected override void OnPlay()
        {
            if (alphaData.target != null)
            {
                alphaData.target.DOKill();
                alphaData.target.DOFade(alphaData.endAlpha, ReadTimer.duration).OnComplete(this.Complete).SetUpdate(true);
            }
        }
    }
}
