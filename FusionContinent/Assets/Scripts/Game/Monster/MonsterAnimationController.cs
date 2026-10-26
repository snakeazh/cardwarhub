using System;
using Config;
using FusionContinent;
using FusionContinent.Monster.Animation;
using FusionContinent.SpineBake;
using Spine;
using Spine.Unity;
using UnityEngine;
using AnimationState = Spine.AnimationState;
using SpineEvent = Spine.Event;

/// <summary>
/// 怪物动画门面：对外 API 保持不变，内部统一走 <see cref="IMonsterAnimationPlayer"/>。
/// </summary>
public class MonsterAnimationController
{
    private Transform _spineContent;
    private Transform _spineRoot;
    private SkeletonAnimation _skeletonAnimation;
    private IMonsterAnimationPlayer _player;
    private string _spineAssetKey;
    private MonsterQuality _monsterQuality;

    private Action<TrackEntry, SpineEvent> _legacySpineEventCallback;
    private Action<string, string, float> _playerEventHandler;
    private Action<string> _playerCompleteHandler;

    public IMonsterAnimationPlayer Player => _player;

    public AnimationRenderMode RenderMode =>
        _player != null ? _player.RenderMode : AnimationRenderMode.Spine;

    public SkeletonAnimation SkeletonAnimation => _skeletonAnimation;
    public Transform SpineRoot => _spineRoot;
    public Transform SpineContent => _spineContent;
    public bool HasSkeletonAnimation => _skeletonAnimation != null;

    /// <summary>兼容性能采样。</summary>
    public bool UseBakedMesh => RenderMode == AnimationRenderMode.BakeMesh;

    public string PlayerTypeName => _player != null ? _player.GetType().Name : "null";

    public void AwakeCache(MonsterCard monster)
    {
        _spineContent = monster.transform.Find("MonsterCard/Spine");
    }

    public void RebindActiveSpine(Transform spinePrefabRoot, string spineAssetKey, MonsterQuality monsterQuality)
    {
        ReleasePlayer();
        UnregisterAllSpineCallbacks();

        _spineRoot = spinePrefabRoot;
        _spineAssetKey = spineAssetKey;
        _monsterQuality = monsterQuality;
        _skeletonAnimation = spinePrefabRoot.GetChild(0).GetComponent<SkeletonAnimation>();

        var context = new MonsterAnimationPlayerContext
        {
            SpineRoot = _spineRoot,
            SkeletonAnimation = _skeletonAnimation,
            SkeletonName = spineAssetKey,
            MonsterQuality = monsterQuality,
            RenderMode = MonsterAnimationSettings.ResolveRenderMode(monsterQuality, spineAssetKey)
        };

        _player = MonsterAnimationPlayerFactory.CreateForMonster(context);
        ConfigurePerfSampler();

        if (SpineMeshBakeSettings.EnablePerfLog.Value)
        {
            LogTool.Log(
                "[AnimationPlayer] Rebind "
                + spineAssetKey
                + " GlobalMode=" + MonsterAnimationSettings.GlobalRenderMode.Value
                + " Resolved=" + _player.RenderMode
                + " Type=" + PlayerTypeName);
        }
    }

    /// <summary>
    /// 渲染模式切换后，用当前 Spine 重新绑定播放器（不重载 prefab）。
    /// </summary>
    public void RebindRenderPlayer()
    {
        if (_spineRoot == null || string.IsNullOrEmpty(_spineAssetKey))
        {
            return;
        }

        RebindActiveSpine(_spineRoot, _spineAssetKey, _monsterQuality);
    }

    public void ResetSpineContentLocalPosition()
    {
        if (_spineContent != null)
        {
            _spineContent.localPosition = Vector3.zero;
        }
    }

    public TrackEntry Play(int track, string animName, bool loop = true)
    {
        if (_player == null)
        {
            return null;
        }

        if (track != 0)
        {
            return PlayOnSpineTrack(track, animName, loop);
        }

        _player.Play(animName, loop);
        return null;
    }

    public void SetEmptyAnimation(int track, float duration)
    {
        SpineAnimationPlayer spinePlayer = _player as SpineAnimationPlayer;
        if (spinePlayer != null)
        {
            spinePlayer.SetEmptyAnimation(track, duration);
        }
    }

    public void SetTimeScale(float scale)
    {
        if (_player != null)
        {
            _player.SetTimeScale(scale);
        }
    }

    public void SetSkeletonTimeScale(float scale)
    {
        SetTimeScale(scale);
    }

    public void SetSkeletonScaleX(float scaleX)
    {
        if (_player is SpineAnimationPlayer spinePlayer)
        {
            spinePlayer.SetSkeletonScaleX(scaleX);
            return;
        }

        if (_player is BakeMeshAnimationPlayer bakePlayer)
        {
            bakePlayer.SetSkeletonScaleX(scaleX);
        }
    }

    public void SetSpineFacing(bool faceLeft)
    {
        if (_player is SpineAnimationPlayer spinePlayer)
        {
            spinePlayer.SetSpineFacing(faceLeft);
            return;
        }

        if (_player is BakeMeshAnimationPlayer bakePlayer)
        {
            bakePlayer.SetSpineFacing(faceLeft);
        }
    }

    public Color GetSkeletonColor()
    {
        IMonsterAnimationVisual visual = _player as IMonsterAnimationVisual;
        if (visual != null)
        {
            return visual.GetTintColor();
        }

        return Color.white;
    }

    public void SetSkeletonColor(Color color)
    {
        IMonsterAnimationVisual visual = _player as IMonsterAnimationVisual;
        if (visual != null)
        {
            visual.SetTintColor(color);
        }
    }

    public MeshRenderer GetMeshRenderer()
    {
        IMonsterAnimationVisual visual = _player as IMonsterAnimationVisual;
        if (visual != null)
        {
            return visual.GetMeshRenderer();
        }

        return null;
    }

    public void Pause()
    {
        if (_player != null)
        {
            _player.Pause();
        }
    }

    public void Resume(bool loop = true)
    {
        if (_player == null)
        {
            return;
        }

        SpineAnimationPlayer spinePlayer = _player as SpineAnimationPlayer;
        if (spinePlayer != null)
        {
            spinePlayer.ResumeWithLoop(loop);
            return;
        }

        _player.Resume();
    }

    public void RegisterSpineEvent(Action<TrackEntry, SpineEvent> onSpineEvent)
    {
        _legacySpineEventCallback = onSpineEvent;
    }

    public void UnregisterSpineEvent(Action<TrackEntry, SpineEvent> onSpineEvent)
    {
        if (_legacySpineEventCallback == onSpineEvent)
        {
            _legacySpineEventCallback = null;
        }
    }

    public TrackEntry PlayAttack(
        Action<TrackEntry, SpineEvent> onSpineEvent,
        AnimationState.TrackEntryDelegate onComplete)
    {
        RegisterSpineEvent(onSpineEvent);
        UnhookPlayerCallbacks();
        HookPlayerCallbacks(onComplete);

        if (_player != null)
        {
            _player.Play("atk", false);
        }

        return null;
    }

    public void UnregisterAllSpineCallbacks()
    {
        UnhookPlayerCallbacks();
        _legacySpineEventCallback = null;
    }

    public void ResetForPool()
    {
        UnregisterAllSpineCallbacks();
        if (_player != null)
        {
            _player.ResetForPool();
        }
    }

    public void ReleasePlayer()
    {
        UnhookPlayerCallbacks();
        if (_player is SpineAnimationPlayer spinePlayer)
        {
            spinePlayer.Release();
        }
        else if (_player is BakeMeshAnimationPlayer bakePlayer)
        {
            bakePlayer.Release();
        }
        else if (_player is GpuVertexAnimationPlayer gpuPlayer)
        {
            gpuPlayer.Release();
        }

        _player = null;
    }

    private TrackEntry PlayOnSpineTrack(int track, string animName, bool loop)
    {
        SpineAnimationPlayer spinePlayer = _player as SpineAnimationPlayer;
        if (spinePlayer == null || _skeletonAnimation == null)
        {
            return null;
        }

        return _skeletonAnimation.AnimationState.SetAnimation(track, animName, loop);
    }

    private void HookPlayerCallbacks(AnimationState.TrackEntryDelegate onComplete)
    {
        if (_player == null)
        {
            return;
        }

        _playerEventHandler = (animName, eventName, eventTime) => DispatchLegacySpineEvent(eventName);
        _playerCompleteHandler = animName =>
        {
            if (animName == "atk")
            {
                onComplete?.Invoke(null);
            }
        };

        _player.OnAnimationEvent += _playerEventHandler;
        _player.OnAnimationComplete += _playerCompleteHandler;
    }

    private void UnhookPlayerCallbacks()
    {
        if (_player == null)
        {
            _playerEventHandler = null;
            _playerCompleteHandler = null;
            return;
        }

        if (_playerEventHandler != null)
        {
            _player.OnAnimationEvent -= _playerEventHandler;
            _playerEventHandler = null;
        }

        if (_playerCompleteHandler != null)
        {
            _player.OnAnimationComplete -= _playerCompleteHandler;
            _playerCompleteHandler = null;
        }
    }

    private void DispatchLegacySpineEvent(string eventName)
    {
        if (_legacySpineEventCallback == null)
        {
            return;
        }

        var eventData = new EventData(eventName);
        var spineEvent = new SpineEvent(0f, eventData);
        _legacySpineEventCallback.Invoke(null, spineEvent);
    }

    private void ConfigurePerfSampler()
    {
        if (_skeletonAnimation == null)
        {
            return;
        }

        SpineRuntimePerfSampler sampler = _skeletonAnimation.GetComponent<SpineRuntimePerfSampler>();
        if (sampler == null)
        {
            sampler = _skeletonAnimation.gameObject.AddComponent<SpineRuntimePerfSampler>();
        }

        sampler.SetSamplingEnabled(RenderMode == AnimationRenderMode.Spine);
    }
}
