using Spine.Unity;
using UnityEngine;

namespace FusionContinent.Monster.Animation
{
    /// <summary>
    /// 将 SkeletonAnimation 恢复为 Spine Runtime 可正常更新/渲染的状态。
    /// </summary>
    internal static class SpineRuntimeRestore
    {
        public static void Apply(SkeletonAnimation skeletonAnimation)
        {
            if (skeletonAnimation == null)
            {
                return;
            }

            skeletonAnimation.clearStateOnDisable = true;
            skeletonAnimation.UpdateMode = UpdateMode.FullUpdate;
            skeletonAnimation.enabled = true;

            if (!skeletonAnimation.valid)
            {
                skeletonAnimation.Initialize(false);
            }

            MeshFilter meshFilter = skeletonAnimation.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = null;
            }
        }
    }
}
