using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent.Animation;
using UnityEngine;

namespace FusionContinent.Animation
{
    public class UITweenAlphaLoop : UIAnimation
    {

        public TweenAlphaData alphaData;
        public bool autoPlay = true;
        protected override void OnPlay()
        {
            if (alphaData.target != null)
            {
                alphaData.target.DOKill();
                alphaData.target.color = new Color(alphaData.target.color.r, alphaData.target.color.g,
                    alphaData.target.color.b, alphaData.startAlpha);
                alphaData.target.DOFade(alphaData.endAlpha, ReadTimer.duration).OnComplete(this.Complete).SetLoops(-1, LoopType.Yoyo).SetEase(this.EaseType).SetUpdate(true);
            }
        }

        private void Start()
        {
            if (autoPlay)
            {
                Play();
            }
        }

        private void OnDestroy()
        {
            alphaData.target.DOKill();
        }
    }
}