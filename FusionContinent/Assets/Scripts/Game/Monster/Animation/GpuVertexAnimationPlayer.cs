using FusionContinent;

namespace FusionContinent.Monster.Animation
{
    /// <summary>
    /// 预留：GPU 顶点贴图采样播放器。当前回退到 Spine 路径，由 Factory 保证不实例化。
    /// </summary>
    public class GpuVertexAnimationPlayer : IMonsterAnimationPlayer, IMonsterAnimationVisual
    {
        private SpineAnimationPlayer _fallbackSpinePlayer = new SpineAnimationPlayer();

        public AnimationRenderMode RenderMode => AnimationRenderMode.GpuVertex;

        public bool IsInitialized => _fallbackSpinePlayer.IsInitialized;

        public event System.Action<string> OnAnimationComplete
        {
            add { _fallbackSpinePlayer.OnAnimationComplete += value; }
            remove { _fallbackSpinePlayer.OnAnimationComplete -= value; }
        }

        public event System.Action<string, string, float> OnAnimationEvent
        {
            add { _fallbackSpinePlayer.OnAnimationEvent += value; }
            remove { _fallbackSpinePlayer.OnAnimationEvent -= value; }
        }

        public void Initialize(MonsterAnimationPlayerContext context)
        {
            LogTool.LogWarning("[GpuVertexAnimationPlayer] 尚未实现，回退 SpineAnimationPlayer");
            context.RenderMode = AnimationRenderMode.Spine;
            _fallbackSpinePlayer.Initialize(context);
        }

        public void Play(string animationName, bool loop)
        {
            _fallbackSpinePlayer.Play(animationName, loop);
        }

        public void Stop()
        {
            _fallbackSpinePlayer.Stop();
        }

        public void Pause()
        {
            _fallbackSpinePlayer.Pause();
        }

        public void Resume()
        {
            _fallbackSpinePlayer.Resume();
        }

        public void SetTimeScale(float speed)
        {
            _fallbackSpinePlayer.SetTimeScale(speed);
        }

        public bool IsPlaying(string animationName)
        {
            return _fallbackSpinePlayer.IsPlaying(animationName);
        }

        public void Update(float deltaTime)
        {
            _fallbackSpinePlayer.Update(deltaTime);
        }

        public void ResetForPool()
        {
            _fallbackSpinePlayer.ResetForPool();
        }

        public UnityEngine.Color GetTintColor()
        {
            return _fallbackSpinePlayer.GetTintColor();
        }

        public void SetTintColor(UnityEngine.Color color)
        {
            _fallbackSpinePlayer.SetTintColor(color);
        }

        public UnityEngine.MeshRenderer GetMeshRenderer()
        {
            return _fallbackSpinePlayer.GetMeshRenderer();
        }

        public void Release()
        {
            _fallbackSpinePlayer.Release();
        }
    }
}
