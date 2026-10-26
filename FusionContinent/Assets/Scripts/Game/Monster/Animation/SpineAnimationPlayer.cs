using System;
using FusionContinent;
using Spine;
using Spine.Unity;
using UnityEngine;
using AnimationState = Spine.AnimationState;
using SpineEvent = Spine.Event;

namespace FusionContinent.Monster.Animation
{
    /// <summary>
    /// 封装现有 Spine Runtime，行为与改造前一致。
    /// </summary>
    public class SpineAnimationPlayer : IMonsterAnimationPlayer, IMonsterAnimationVisual
    {
        private SkeletonAnimation _skeletonAnimation;
        private AnimationState _animationState;
        private string _currentAnimation;
        private bool _isPaused;
        private string _pausedAnimation;
        private bool _pausedLoop;

        private AnimationState.TrackEntryEventDelegate _eventWrapper;
        private AnimationState.TrackEntryDelegate _completeWrapper;

        public AnimationRenderMode RenderMode => AnimationRenderMode.Spine;

        public bool IsInitialized { get; private set; }

        public event Action<string> OnAnimationComplete;

        public event Action<string, string, float> OnAnimationEvent;

        public void Initialize(MonsterAnimationPlayerContext context)
        {
            _skeletonAnimation = context.SkeletonAnimation;
            SpineRuntimeRestore.Apply(_skeletonAnimation);
            _animationState = _skeletonAnimation.AnimationState;

            _eventWrapper = OnSpineEvent;
            _completeWrapper = OnTrackComplete;
            _animationState.Event += _eventWrapper;

            IsInitialized = true;
        }

        public void Play(string animationName, bool loop)
        {
            if (!IsInitialized || _animationState == null)
            {
                return;
            }

            _currentAnimation = animationName;
            _isPaused = false;
            TrackEntry track = _animationState.SetAnimation(0, animationName, loop);
            if (track != null)
            {
                track.Complete -= _completeWrapper;
                track.Complete += _completeWrapper;
            }
        }

        public void Stop()
        {
            if (!IsInitialized || _animationState == null)
            {
                return;
            }

            _animationState.ClearTracks();
            _currentAnimation = null;
            _isPaused = false;
        }

        public void Pause()
        {
            if (!IsInitialized || _animationState == null || _isPaused)
            {
                return;
            }

            TrackEntry current = _animationState.GetCurrent(0);
            if (current != null)
            {
                _pausedAnimation = current.Animation.Name;
                _pausedLoop = current.Loop;
                _animationState.ClearTracks();
                _isPaused = true;
            }
        }

        public void Resume()
        {
            ResumeWithLoop(_pausedLoop);
        }

        public void ResumeWithLoop(bool loop)
        {
            if (!IsInitialized || _animationState == null || !_isPaused)
            {
                return;
            }

            Play(_pausedAnimation, loop);
            _isPaused = false;
        }

        public void SetTimeScale(float speed)
        {
            if (!IsInitialized)
            {
                return;
            }

            if (_animationState != null)
            {
                _animationState.TimeScale = speed;
            }

            if (_skeletonAnimation != null)
            {
                _skeletonAnimation.timeScale = speed;
            }
        }

        public bool IsPlaying(string animationName)
        {
            if (!IsInitialized || _animationState == null)
            {
                return false;
            }

            TrackEntry current = _animationState.GetCurrent(0);
            return current != null && current.Animation.Name == animationName;
        }

        public void Update(float deltaTime)
        {
        }

        public void ResetForPool()
        {
            if (!IsInitialized)
            {
                return;
            }

            Stop();
            SetTimeScale(1f);
            SetTintColor(Color.white);
        }

        public void SetEmptyAnimation(int track, float duration)
        {
            if (!IsInitialized || _animationState == null)
            {
                return;
            }

            _animationState.SetEmptyAnimation(track, duration);
        }

        public void SetSkeletonScaleX(float scaleX)
        {
            if (_skeletonAnimation != null)
            {
                _skeletonAnimation.Skeleton.ScaleX = scaleX;
            }
        }

        public void SetSpineFacing(bool faceLeft)
        {
            if (_skeletonAnimation == null)
            {
                return;
            }

            Vector3 scale = _skeletonAnimation.transform.localScale;
            if (faceLeft)
            {
                _skeletonAnimation.transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
            }
            else
            {
                _skeletonAnimation.transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);
            }
        }

        public Color GetTintColor()
        {
            if (_skeletonAnimation == null)
            {
                return Color.white;
            }

            return _skeletonAnimation.Skeleton.GetColor();
        }

        public void SetTintColor(Color color)
        {
            if (_skeletonAnimation != null)
            {
                _skeletonAnimation.Skeleton.SetColor(color);
            }
        }

        public MeshRenderer GetMeshRenderer()
        {
            if (_skeletonAnimation == null)
            {
                return null;
            }

            return _skeletonAnimation.GetComponent<MeshRenderer>();
        }

        public void Release()
        {
            if (_animationState != null && _eventWrapper != null)
            {
                _animationState.Event -= _eventWrapper;
            }

            _eventWrapper = null;
            _completeWrapper = null;
            IsInitialized = false;
        }

        private void OnSpineEvent(TrackEntry trackEntry, SpineEvent e)
        {
            string animName = trackEntry.Animation.Name;
            if (OnAnimationEvent != null)
            {
                OnAnimationEvent.Invoke(animName, e.Data.Name, e.Time);
            }
        }

        private void OnTrackComplete(TrackEntry trackEntry)
        {
            if (trackEntry.Loop)
            {
                return;
            }

            string animName = trackEntry.Animation.Name;
            _currentAnimation = null;
            if (OnAnimationComplete != null)
            {
                OnAnimationComplete.Invoke(animName);
            }
        }
    }
}
