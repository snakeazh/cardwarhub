using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Animation
{
    public class UITweenMoveY : UIAnimation
    {
        public AnimationParamSingle MoveData;

        private UITweenTimer _timer;

        private void Awake()
        {
            _timer = GetComponent<UITweenTimer>();
        }

        [ContextMenu("播放")]
        protected override void OnPlay()
        {
            if (MoveData != null && _timer != null)
            {
                MoveData.Target.DOKill();
                MoveData.Target.SetAnchorPosY(MoveData.StartPos);
                MoveData.Target.DOAnchorPosY(MoveData.EndPos, _timer.duration).SetEase(EaseType)
                    .OnComplete(Complete).SetUpdate(true);
            }
        }
    }
}