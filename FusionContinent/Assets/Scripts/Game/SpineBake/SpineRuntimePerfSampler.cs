using System.Diagnostics;
using FusionContinent;
using Spine.Unity;
using UnityEngine;

namespace FusionContinent.SpineBake
{
    /// <summary>
    /// 挂在 SkeletonAnimation 上，统计 Spine 骨骼实时更新耗时（非烘焙路径）。
    /// </summary>
    [DisallowMultipleComponent]
    public class SpineRuntimePerfSampler : MonoBehaviour
    {
        private SkeletonAnimation _skeletonAnimation;
        private bool _sampling;
        private long _sampleStartTicks;

        public void SetSamplingEnabled(bool enabled)
        {
            _sampling = enabled;
        }

        private void Awake()
        {
            _skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        private void OnEnable()
        {
            if (_skeletonAnimation == null)
            {
                return;
            }

            _skeletonAnimation.BeforeApply += OnBeforeApply;
            _skeletonAnimation.UpdateComplete += OnUpdateComplete;
        }

        private void OnDisable()
        {
            if (_skeletonAnimation == null)
            {
                return;
            }

            _skeletonAnimation.BeforeApply -= OnBeforeApply;
            _skeletonAnimation.UpdateComplete -= OnUpdateComplete;
        }

        private void OnBeforeApply(ISkeletonAnimation animated)
        {
            if (!_sampling)
            {
                return;
            }

            _sampleStartTicks = Stopwatch.GetTimestamp();
        }

        private void OnUpdateComplete(ISkeletonAnimation animated)
        {
            if (!_sampling || _sampleStartTicks == 0)
            {
                return;
            }

            long elapsed = Stopwatch.GetTimestamp() - _sampleStartTicks;
            double ms = elapsed * 1000.0 / Stopwatch.Frequency;
            SpineMeshBakePerfStats.RecordSpineRuntime(ms);
            _sampleStartTicks = 0;
        }
    }
}
