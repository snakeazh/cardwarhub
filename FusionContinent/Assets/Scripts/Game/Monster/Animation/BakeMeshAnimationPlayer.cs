using System;
using System.Collections.Generic;
using System.Diagnostics;
using FusionContinent;
using FusionContinent.SpineBake;
using Spine.Unity;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace FusionContinent.Monster.Animation
{
    /// <summary>
    /// 查表播放预烘焙 Mesh 帧，不运行 Spine Runtime。
    /// </summary>
    public class BakeMeshAnimationPlayer : IMonsterAnimationPlayer, IMonsterAnimationVisual
    {
        private const string MeshUpdateSample = "BakeMesh.UpdateMesh";
        private const string UploadVerticesSample = "BakeMesh.UploadVertices";
        private const string UploadColorsSample = "BakeMesh.UploadColors";
        private const string TopologySample = "BakeMesh.Topology";
        private const string BoundsSample = "BakeMesh.Bounds";
        private const string TintColorsSample = "BakeMesh.TintColors";

        private static readonly MeshUpdateFlags VertexUploadFlags =
            MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;

        private SkeletonAnimation _skeletonAnimation;
        private MeshFilter _meshFilter;
        private Mesh _runtimeMesh;
        private SpineMeshFrameData _frameData;

        private string _currentAnimation;
        private bool _isLoop;
        private float _tickTime;
        private float _timeScale = 1f;
        private bool _isPaused;
        private int _lastFrameIndex = -1;
        private Color _tintColor = Color.white;

        private string _topologyActionName;
        private int _topologyVertexCount = -1;
        private short _topologySubMeshCount = -1;
        private bool _topologyReady;
        private bool _sharedColorsUploaded;
        private bool _useVertexBufferApi = true;

        private Color32[] _tintedColors;
        private int _tintedColorsVertexCount = -1;
        private Color _lastAppliedTint = Color.white;
        private int _lastTintedFrameIndex = -1;

        private readonly List<SpineMeshFrameEvent> _pendingEvents = new List<SpineMeshFrameEvent>();

        public AnimationRenderMode RenderMode => AnimationRenderMode.BakeMesh;

        public bool IsInitialized { get; private set; }

        public event Action<string> OnAnimationComplete;

        public event Action<string, string, float> OnAnimationEvent;

        public void Initialize(MonsterAnimationPlayerContext context)
        {
            _skeletonAnimation = context.SkeletonAnimation;
            _frameData = BakeMeshFrameLoader.Load(context.SkeletonName);
            if (_frameData == null)
            {
                LogTool.LogError("[BakeMeshAnimationPlayer] 烘焙数据加载失败: " + context.SkeletonName);
                return;
            }

            _meshFilter = _skeletonAnimation.GetComponent<MeshFilter>();
            _runtimeMesh = new Mesh { name = "BakeMeshRuntime" };
            _runtimeMesh.MarkDynamic();
            ResetMeshUploadCache();

            // 必须先关 SkeletonAnimation，再绑定 Mesh；否则 OnDisable(clearStateOnDisable) 会把 sharedMesh 清成 null。
            _skeletonAnimation.clearStateOnDisable = false;
            _skeletonAnimation.UpdateMode = UpdateMode.Nothing;
            _skeletonAnimation.enabled = false;
            _meshFilter.sharedMesh = _runtimeMesh;

            MonsterAnimationPlayerRegistry.Register(this);
            IsInitialized = true;
        }

        public void Play(string animationName, bool loop)
        {
            if (!IsInitialized || _frameData == null || !_frameData.HasAction(animationName))
            {
                LogTool.LogWarning("[BakeMeshAnimationPlayer] 动作未烘焙: " + animationName);
                return;
            }

            _currentAnimation = animationName;
            _isLoop = loop;
            _tickTime = 0f;
            _lastFrameIndex = -1;
            _isPaused = false;
            _pendingEvents.Clear();
            InvalidateTopologyCache();

            SpineMeshActionFrames actionFrames = _frameData.ActionDic[animationName];
            if (actionFrames.Events != null)
            {
                for (int i = 0; i < actionFrames.Events.Length; i++)
                {
                    _pendingEvents.Add(actionFrames.Events[i]);
                }
            }

            ApplyCurrentFrame();
        }

        public void Stop()
        {
            _currentAnimation = null;
            _tickTime = 0f;
            _lastFrameIndex = -1;
            _pendingEvents.Clear();
            _isPaused = false;
            InvalidateTopologyCache();
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        public void SetTimeScale(float speed)
        {
            _timeScale = speed;
        }

        public bool IsPlaying(string animationName)
        {
            return _currentAnimation == animationName && !_isPaused;
        }

        public void Update(float deltaTime)
        {
            if (!IsInitialized || _isPaused || string.IsNullOrEmpty(_currentAnimation))
            {
                return;
            }

            _tickTime += deltaTime * _timeScale;
            ApplyCurrentFrame();
        }

        public void ResetForPool()
        {
            Stop();
            SetTimeScale(1f);
            SetTintColor(Color.white);
            ResetMeshUploadCache();
        }

        public Color GetTintColor()
        {
            return _tintColor;
        }

        public void SetTintColor(Color color)
        {
            if (_tintColor == color)
            {
                return;
            }

            _tintColor = color;
            _lastTintedFrameIndex = -1;
            _sharedColorsUploaded = false;

            if (_lastFrameIndex >= 0 && !string.IsNullOrEmpty(_currentAnimation))
            {
                ApplyCurrentFrameForceRefresh();
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

        public void Release()
        {
            MonsterAnimationPlayerRegistry.Unregister(this);
            ResetForPool();

            SpineRuntimeRestore.Apply(_skeletonAnimation);

            if (_runtimeMesh != null)
            {
                UnityEngine.Object.Destroy(_runtimeMesh);
                _runtimeMesh = null;
            }

            _frameData = null;
            _skeletonAnimation = null;
            _meshFilter = null;
            _tintedColors = null;
            _tintedColorsVertexCount = -1;
            IsInitialized = false;
        }

        private void ApplyCurrentFrameForceRefresh()
        {
            _lastFrameIndex = -1;
            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            SpineMeshActionFrames actionFrames = _frameData.ActionDic[_currentAnimation];
            if (actionFrames.CompactFrames == null || actionFrames.CompactFrames.Length == 0)
            {
                SpineMeshFrameData.BuildCompactFrames(actionFrames);
            }

            if (actionFrames.CompactFrames == null || actionFrames.CompactFrames.Length == 0)
            {
                LogTool.LogWarning("[BakeMeshAnimationPlayer] 动作无有效帧: " + _currentAnimation);
                return;
            }

            int frame = Mathf.FloorToInt(_tickTime / _frameData.FrameLength);
            if (frame >= actionFrames.CompactFrames.Length)
            {
                if (_isLoop)
                {
                    _tickTime %= actionFrames.CompactFrames.Length * _frameData.FrameLength;
                    frame = Mathf.FloorToInt(_tickTime / _frameData.FrameLength);
                }
                else
                {
                    frame = actionFrames.CompactFrames.Length - 1;
                    UpdateMesh(actionFrames, actionFrames.CompactFrames[frame], frame);
                    FireEventsUpTo(frame);
                    CompleteCurrentAnimation();
                    return;
                }
            }

            if (frame == _lastFrameIndex)
            {
                return;
            }

            _lastFrameIndex = frame;
            UpdateMesh(actionFrames, actionFrames.CompactFrames[frame], frame);
            FireEventsUpTo(frame);
        }

        private void FireEventsUpTo(int frame)
        {
            if (OnAnimationEvent == null)
            {
                return;
            }

            string animName = _currentAnimation;
            for (int i = _pendingEvents.Count - 1; i >= 0; i--)
            {
                if (_pendingEvents[i].FrameIndex <= frame)
                {
                    float eventTime = _pendingEvents[i].FrameIndex * _frameData.FrameLength;
                    OnAnimationEvent.Invoke(animName, _pendingEvents[i].EventName, eventTime);
                    _pendingEvents.RemoveAt(i);
                }
            }
        }

        private void CompleteCurrentAnimation()
        {
            string finished = _currentAnimation;
            _currentAnimation = null;
            _pendingEvents.Clear();
            _lastFrameIndex = -1;
            InvalidateTopologyCache();
            if (OnAnimationComplete != null)
            {
                OnAnimationComplete.Invoke(finished);
            }
        }

        private void UpdateMesh(SpineMeshActionFrames action, SpineMeshCompactFrame frame, int frameIndex)
        {
            Profiler.BeginSample(MeshUpdateSample);
            long totalStart = Stopwatch.GetTimestamp();

            EnsureTopology(action, frame);

            long boundsStart = Stopwatch.GetTimestamp();
            Profiler.BeginSample(BoundsSample);
            _runtimeMesh.bounds = frame.Bounds;
            Profiler.EndSample();
            double boundsMs = ElapsedMs(boundsStart);
            SpineMeshBakePerfStats.RecordBounds(boundsMs);

            UploadVertices(frame.Vertices);
            UploadColors(action, frame, frameIndex);

            double totalMs = ElapsedMs(totalStart);
            SpineMeshBakePerfStats.RecordBakedMeshUpdate(totalMs);
            Profiler.EndSample();
        }

        private void EnsureTopology(SpineMeshActionFrames action, SpineMeshCompactFrame frame)
        {
            if (action.HasSharedTopology)
            {
                EnsureSharedTopology(action);
                return;
            }

            EnsurePerFrameTopology(frame);
        }

        private void EnsureSharedTopology(SpineMeshActionFrames action)
        {
            if (_topologyReady
                && _topologyActionName == _currentAnimation
                && _topologyVertexCount == action.SharedUvs.Length
                && _topologySubMeshCount == action.SharedSubMeshCount)
            {
                return;
            }

            Profiler.BeginSample(TopologySample);
            long start = Stopwatch.GetTimestamp();

            int vertexCount = action.SharedUvs.Length;
            short subMeshCount = action.SharedSubMeshCount;

            _runtimeMesh.subMeshCount = subMeshCount;
            SetupVertexBufferLayout(vertexCount);
            UploadStaticUvs(action.SharedUvs, vertexCount);

            for (int i = 0; i < subMeshCount; i++)
            {
                _runtimeMesh.SetTriangles(action.SharedTriangles[i], i, false);
            }

            _topologyActionName = _currentAnimation;
            _topologyVertexCount = vertexCount;
            _topologySubMeshCount = subMeshCount;
            _topologyReady = true;
            _sharedColorsUploaded = false;

            _meshFilter.sharedMesh = _runtimeMesh;
            Profiler.EndSample();
            SpineMeshBakePerfStats.RecordTopology(ElapsedMs(start));
        }

        private void EnsurePerFrameTopology(SpineMeshCompactFrame frame)
        {
            int vertexCount = frame.Vertices.Length;
            short subMeshCount = frame.SubMeshCount;
            if (_topologyReady
                && _topologyActionName == _currentAnimation
                && _topologyVertexCount == vertexCount
                && _topologySubMeshCount == subMeshCount)
            {
                return;
            }

            Profiler.BeginSample(TopologySample);
            long start = Stopwatch.GetTimestamp();

            _runtimeMesh.subMeshCount = subMeshCount;
            SetupVertexBufferLayout(vertexCount);
            UploadStaticUvs(frame.Uvs, vertexCount);

            for (int i = 0; i < subMeshCount; i++)
            {
                _runtimeMesh.SetTriangles(frame.Triangles[i], i, false);
            }

            _topologyActionName = _currentAnimation;
            _topologyVertexCount = vertexCount;
            _topologySubMeshCount = subMeshCount;
            _topologyReady = true;
            _sharedColorsUploaded = false;

            _meshFilter.sharedMesh = _runtimeMesh;
            Profiler.EndSample();
            SpineMeshBakePerfStats.RecordTopology(ElapsedMs(start));
        }

        private void SetupVertexBufferLayout(int vertexCount)
        {
            _runtimeMesh.SetVertexBufferParams(
                vertexCount,
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 1),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2, 2));
        }

        private void UploadStaticUvs(Vector2[] uvs, int vertexCount)
        {
            _runtimeMesh.SetVertexBufferData(uvs, 0, 0, vertexCount, 2, VertexUploadFlags);
        }

        private void UploadVertices(Vector3[] vertices)
        {
            Profiler.BeginSample(UploadVerticesSample);
            long start = Stopwatch.GetTimestamp();

            if (_useVertexBufferApi)
            {
                try
                {
                    _runtimeMesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, VertexUploadFlags);
                }
                catch (Exception ex)
                {
                    LogTool.LogWarning("[BakeMeshAnimationPlayer] SetVertexBufferData 顶点失败，回退 legacy: " + ex.Message);
                    _useVertexBufferApi = false;
                    _runtimeMesh.vertices = vertices;
                }
            }
            else
            {
                _runtimeMesh.vertices = vertices;
            }

            _meshFilter.sharedMesh = _runtimeMesh;
            Profiler.EndSample();
            SpineMeshBakePerfStats.RecordUploadVertices(ElapsedMs(start));
        }

        private void UploadColors(SpineMeshActionFrames action, SpineMeshCompactFrame frame, int frameIndex)
        {
            if (IsWhiteTint())
            {
                UploadColorsWithoutTint(action, frame);
                return;
            }

            UploadColorsWithTint(action, frame, frameIndex);
        }

        private void UploadColorsWithoutTint(SpineMeshActionFrames action, SpineMeshCompactFrame frame)
        {
            if (action.HasSharedColors)
            {
                if (_sharedColorsUploaded)
                {
                    return;
                }

                Profiler.BeginSample(UploadColorsSample);
                long start = Stopwatch.GetTimestamp();
                UploadColorBuffer(action.SharedColors, action.SharedColors.Length);
                _sharedColorsUploaded = true;
                Profiler.EndSample();
                SpineMeshBakePerfStats.RecordUploadColors(ElapsedMs(start));
                return;
            }

            if (frame.Colors == null)
            {
                return;
            }

            Profiler.BeginSample(UploadColorsSample);
            long frameStart = Stopwatch.GetTimestamp();
            UploadColorBuffer(frame.Colors, frame.Colors.Length);
            Profiler.EndSample();
            SpineMeshBakePerfStats.RecordUploadColors(ElapsedMs(frameStart));
        }

        private void UploadColorsWithTint(SpineMeshActionFrames action, SpineMeshCompactFrame frame, int frameIndex)
        {
            if (_lastAppliedTint == _tintColor && _lastTintedFrameIndex == frameIndex)
            {
                Profiler.BeginSample(UploadColorsSample);
                long cachedStart = Stopwatch.GetTimestamp();
                UploadColorBuffer(_tintedColors, _tintedColors.Length);
                Profiler.EndSample();
                SpineMeshBakePerfStats.RecordUploadColors(ElapsedMs(cachedStart));
                return;
            }

            Profiler.BeginSample(TintColorsSample);
            long tintStart = Stopwatch.GetTimestamp();

            Color32[] sourceColors = action.HasSharedColors ? action.SharedColors : frame.Colors;
            EnsureTintedColorBuffer(sourceColors.Length);
            ApplyTintToColors(sourceColors, _tintedColors);
            _lastAppliedTint = _tintColor;
            _lastTintedFrameIndex = frameIndex;

            Profiler.EndSample();
            SpineMeshBakePerfStats.RecordTintColors(ElapsedMs(tintStart));

            Profiler.BeginSample(UploadColorsSample);
            long uploadStart = Stopwatch.GetTimestamp();
            UploadColorBuffer(_tintedColors, _tintedColors.Length);
            Profiler.EndSample();
            SpineMeshBakePerfStats.RecordUploadColors(ElapsedMs(uploadStart));
        }

        private void UploadColorBuffer(Color32[] colors, int vertexCount)
        {
            if (_useVertexBufferApi)
            {
                _runtimeMesh.SetVertexBufferData(colors, 0, 0, vertexCount, 1, VertexUploadFlags);
            }
            else
            {
                _runtimeMesh.colors32 = colors;
            }
        }

        private void EnsureTintedColorBuffer(int vertexCount)
        {
            if (_tintedColors == null || _tintedColorsVertexCount != vertexCount)
            {
                _tintedColors = new Color32[vertexCount];
                _tintedColorsVertexCount = vertexCount;
                _lastTintedFrameIndex = -1;
            }
        }

        private void ApplyTintToColors(Color32[] sourceColors, Color32[] destination)
        {
            float tintR = _tintColor.r;
            float tintG = _tintColor.g;
            float tintB = _tintColor.b;
            float tintA = _tintColor.a;

            for (int i = 0; i < sourceColors.Length; i++)
            {
                Color32 src = sourceColors[i];
                destination[i] = new Color32(
                    (byte)(src.r * tintR),
                    (byte)(src.g * tintG),
                    (byte)(src.b * tintB),
                    (byte)(src.a * tintA));
            }
        }

        private bool IsWhiteTint()
        {
            return _tintColor.r == 1f && _tintColor.g == 1f && _tintColor.b == 1f && _tintColor.a == 1f;
        }

        private void InvalidateTopologyCache()
        {
            _topologyActionName = null;
            _topologyReady = false;
            _sharedColorsUploaded = false;
            _lastTintedFrameIndex = -1;
        }

        private void ResetMeshUploadCache()
        {
            InvalidateTopologyCache();
            _topologyVertexCount = -1;
            _topologySubMeshCount = -1;
            _lastFrameIndex = -1;
            _lastAppliedTint = Color.white;
            _tintedColorsVertexCount = -1;
        }

        private static double ElapsedMs(long startTicks)
        {
            long elapsed = Stopwatch.GetTimestamp() - startTicks;
            return elapsed * 1000.0 / Stopwatch.Frequency;
        }
    }
}
