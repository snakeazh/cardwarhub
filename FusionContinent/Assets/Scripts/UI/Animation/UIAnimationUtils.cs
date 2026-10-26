using UnityEngine;
using UnityEngine.Events;

namespace FusionContinent.Animation
{
    public static class UIAnimationUtils
    {
        public static void PlayAnimations(UIAnimation[]  animations)
        {
            foreach (var animation in animations)
            {
                animation?.Play();
            }
        }
        public static void PlayAnimations(UIAnimation[] animations, UnityAction allComplete)
        {
            if (animations == null || animations.Length == 0)
            {
                allComplete?.Invoke();
                return;
            }
            int remaining = 0;
            foreach (var a in animations) if (a != null) remaining++;
            if (remaining == 0)
            {
                allComplete?.Invoke();
                return;
            }
            foreach (var animation in animations)
            {
                if (animation == null) continue;
                animation.Play(() =>
                {
                    remaining--;
                    if (remaining <= 0) allComplete?.Invoke();
                });
            }
        }
        public static void PlayAnimations(GameObject target)
        {
            var animations = target.GetComponents<UIAnimation>();
            PlayAnimations(animations);
        }
        public static void PlayAnimations(GameObject target, UnityAction allComplete)
        {
            var animations = target.GetComponents<UIAnimation>();
            PlayAnimations(animations, allComplete);
        }
        
        public static void PlayAnimations(Transform target)
        {
            if (target != null)
            {
                PlayAnimations(target.gameObject);
            }
        }
        public static void PlayAnimations(Transform target, UnityAction allComplete)
        {
            if (target != null)
            {
                PlayAnimations(target.gameObject, allComplete);
            }
        }
        
        public static void PlayAnimations(RectTransform target)
        {
            if (target != null)
            {
                PlayAnimations(target.gameObject);
            }
        }
        public static void PlayAnimations(RectTransform target, UnityAction allComplete)
        {
            if (target != null)
            {
                PlayAnimations(target.gameObject, allComplete);
            }
        }
    }
}