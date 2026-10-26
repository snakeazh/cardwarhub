using System;
using UnityEngine;

namespace FusionContinent.Monster.Animation
{
    public interface IMonsterAnimationPlayer
    {
        AnimationRenderMode RenderMode { get; }

        bool IsInitialized { get; }

        void Initialize(MonsterAnimationPlayerContext context);

        void Play(string animationName, bool loop);

        void Stop();

        void Pause();

        void Resume();

        void SetTimeScale(float speed);

        bool IsPlaying(string animationName);

        void Update(float deltaTime);

        /// <summary>对象池回收时重置帧、动作与播放状态。</summary>
        void ResetForPool();

        event Action<string> OnAnimationComplete;

        event Action<string, string, float> OnAnimationEvent;
    }

    /// <summary>
    /// 颜色、Renderer 等表现层能力（不进入核心播放接口，供 MonsterAnimationController 使用）。
    /// </summary>
    public interface IMonsterAnimationVisual
    {
        Color GetTintColor();

        void SetTintColor(Color color);

        MeshRenderer GetMeshRenderer();
    }

    public class MonsterAnimationPlayerContext
    {
        public Transform SpineRoot;
        public Spine.Unity.SkeletonAnimation SkeletonAnimation;
        public string SkeletonName;
        public Config.MonsterQuality MonsterQuality;
        public AnimationRenderMode RenderMode;
    }
}
