using DG.Tweening;
using FusionContinent.Animation;
using UnityEngine;
using UnityEngine.Events;

namespace FusionContinent.Animation
{
    [RequireComponent(typeof(UITweenTimer))]
    public abstract class UIAnimation : MonoBehaviour
    {
        public Ease EaseType =  Ease.Linear;
        private UnityAction _complete;
        protected UITweenTimer ReadTimer
        {
            get
            {
                if (timer == null)
                {
                    timer = GetComponent<UITweenTimer>();
                }

                return timer;
            }
        }
        protected UITweenTimer timer;
        private void Awake()
        {
            timer = GetComponent<UITweenTimer>();
        }

        public virtual void Play(UnityAction complete)
        {
            _complete = complete;
            OnPlay();
        }
        public virtual void Play()
        {
            _complete = null;
            OnPlay();
        }

        protected void Complete()
        {
            var c = _complete;
            _complete = null;
            c?.Invoke();
        }

        protected abstract void OnPlay();
    }
}
