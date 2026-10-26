using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using QFramework;
using Spine;
using UnityEngine;
using Spine.Unity;

/// <summary>
/// 英灵 Spine 动画控制器。
/// <para>
/// 技能释放主链路（未改动）：
/// PlayAtkAnimation(skillId) → 播放 skillId 动画 → Spine <c>atk</c> 事件 → OnAttackHit → 技能节点图。
/// </para>
/// <para>
/// 隐藏类技能（钻地/潜行）扩展：Spine <c>hide</c> 事件进入等待 Return 状态，技能动画结束后保持姿态；
/// 技能图末尾的 <see cref="FusionContinent.Skill.Handler.ReturnAnimationHandler"/> 调用
/// <see cref="PlayReturnAnimation"/> 播放 return 并回 Idle。详见
/// <c>Assets/Scripts/Game/Soldier/SoldierAnimationController_README.md</c>。
/// </para>
/// </summary>
public class SoldierAnimationController
{
    private SoldierCard _soldier;

    // Spine 组件
    private SkeletonGraphic _skeletonGraphic;

    private int playId = 0;

    /// <summary>
    /// 当前技能动画播放序号。每次 <see cref="PlayAtkAnimation"/> 抢占播放时递增，
    /// 用于 Complete 回调与 Return 节点校验动画是否仍归本次技能所有。
    /// </summary>
    public int CurrentPlayId => playId;
    
    // 动画时间缩放
    private float _animationTimeScale = 1f;
    private float _cachedTimeScaleBeforePause = 1f;
    private bool _isPaused;

    // 攻击命中回调（Spine "atk" 事件触发）
    private Action _onAttackHit;

    private Action _onOverAtk;
    
    // 当前播放技能ID（防多技能覆盖）
    private bool _atkTriggered = false;

    #region Hide / Return 表现动画

    /// <summary>
    /// 是否处于 hide 事件后的 Return 等待态。
    /// 为 true 时技能动画 Complete 不会触发 <see cref="_onOverAtk"/>，角色保持最后一帧姿态。
    /// </summary>
    private bool _waitReturn;

    /// <summary>收到 hide 事件时的 <see cref="playId"/>，与 <see cref="CanPlayReturn"/> 配合校验所有权。</summary>
    private int _waitReturnPlayId;

    #endregion

    /// <summary>
    /// 当前 Spine 骨骼上已有的动画名（skillId 与动画名一致时可直接查表）。
    /// </summary>
    private HashSet<string> _animationNames;

    public SoldierAnimationController(SoldierCard soldier)
    {
        _soldier = soldier;
    }

    #region 初始化

    public void Initialize()
    {
        var spineObj = _soldier.PlayerspineObj;
        if (spineObj != null)
        {
            _skeletonGraphic = spineObj.GetComponent<SkeletonGraphic>();
        }
    }

    public void CreateSpineObject(string spinePath, float spineScale)
    {
        var spineObj = GamePlayManager.Default._resLoader.Get<GameObject>(spinePath).Instantiate();
        spineObj.transform.SetParent(_soldier.transform);
        spineObj.transform.localScale = spineScale > 0 ? Vector3.one * spineScale : Vector3.one;
        spineObj.transform.SetSiblingIndex(1);
        spineObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -135f);

        _soldier.PlayerspineObj = spineObj;
        _skeletonGraphic = spineObj.GetComponent<SkeletonGraphic>();
        _skeletonGraphic.AnimationState.Event += OnSpineEvent;
        RebuildAnimationCache();

        if (HasSkillAnimation("blink"))
        {
            var faceTrack = _skeletonGraphic.AnimationState.SetAnimation(1, "blink", true);
            // 可调混合权重（关键）
            faceTrack.Alpha = 1f;
        }

        // try
        // {
        //     // 表情动画（叠加）
        //     var faceTrack = _skeletonGraphic.AnimationState.SetAnimation(1, "blink", true);
        //
        //     // 可调混合权重（关键）
        //     faceTrack.Alpha = 1f;
        // }
        // catch (Exception e)
        // {
        //     LogTool.E("缺少blink表情");
        //     throw;
        // }
        
        
    }

    #endregion

    #region 动画时间缩放

    public void SetAnimationTimeScale(float attackSpeed)
    {
        float finalScale = Mathf.Max(attackSpeed, 1);  //攻击动画最慢为1
        _animationTimeScale = finalScale;
        if (_skeletonGraphic != null)
        {
            _skeletonGraphic.AnimationState.TimeScale = finalScale;
        }
    }

    public float GetAnimationTimeScale() => _animationTimeScale;

    #endregion

    #region 基础动画播放

    public void PlayIdle()
    {
        if (_skeletonGraphic == null) return;
        _animationTimeScale = 1f;
        _skeletonGraphic.AnimationState.TimeScale = 1f;
        _skeletonGraphic.AnimationState.AddAnimation(0, "idle", true, 0);
    }

    public void PlayHit()
    {
        if (_skeletonGraphic == null) return;
        var track = _skeletonGraphic.AnimationState.SetAnimation(0, "hit", false);
        track.Complete += (t) =>
        {
            if (t.Animation.Name == "hit")
                _skeletonGraphic.AnimationState.AddAnimation(0, "idle", true, 0);
        };
    }

    #endregion

    #region Hide / Return 表现动画

    /// <summary>
    /// 是否仍可播放本次技能的 Return 动画。
    /// 条件：仍处于 hide 等待，且快照 playId 与当前 playId 一致（期间无新技能动画抢占）。
    /// </summary>
    /// <param name="cachedPlayId">技能图开始时写入的 <c>SkillContext.SkillAnimationPlayId</c></param>
    public bool CanPlayReturn(int cachedPlayId)
    {
        return _waitReturn && cachedPlayId == playId;
    }

    /// <summary>
    /// 播放技能节点图配置的 Return 表现动画；结束后 <see cref="PlayIdle"/> 并补发因 hide 延迟的 <see cref="_onOverAtk"/>。
    /// </summary>
    /// <param name="animName">Spine 动画名，默认 <c>return</c></param>
    public void PlayReturnAnimation(string animName = "return")
    {
        _waitReturn = false;

        if (_skeletonGraphic == null)
        {
            return;
        }

        if (!HasSkillAnimation(animName))
        {
            LogTool.LogWarning(
                $"[SoldierAnimation] {_soldier.cardId} 缺少 Return 动画: {animName}, spine={_soldier.soldierBattle?.Spine}");
            PlayIdle();
            InvokeDeferredOverAtk();
            return;
        }

        var track = _skeletonGraphic.AnimationState.SetAnimation(0, animName, false);
        track.Complete += OnReturnAnimationComplete;

        void OnReturnAnimationComplete(TrackEntry trackEntry)
        {
            if (trackEntry.Animation.Name != animName)
            {
                return;
            }

            trackEntry.Complete -= OnReturnAnimationComplete;
            PlayIdle();
            InvokeDeferredOverAtk();
        }
    }

    /// <summary>hide 技能动画结束时未触发的 <see cref="_onOverAtk"/>（通常为 SwitchState Idle）。</summary>
    private void InvokeDeferredOverAtk()
    {
        if (_onOverAtk == null)
        {
            return;
        }

        var callback = _onOverAtk;
        _onOverAtk = null;
        callback();
    }

    #endregion
    
    #region 技能动画播放（skillId 驱动）

    private void RebuildAnimationCache()
    {
        _animationNames = null;
        var skeletonData = _skeletonGraphic?.Skeleton?.Data;
        if (skeletonData == null)
        {
            return;
        }

        _animationNames = new HashSet<string>();
        var animations = skeletonData.Animations;
        for (int i = 0; i < animations.Count; i++)
        {
            _animationNames.Add(animations.Items[i].Name);
        }
    }

    /// <summary>
    /// 当前角色 Spine 是否包含以 skillId 命名的施法动画。
    /// </summary>
    public bool HasSkillAnimation(string skillId)
    {
        if (_skeletonGraphic == null)
        {
            return false;
        }

        if (_animationNames == null)
        {
            RebuildAnimationCache();
        }

        return _animationNames != null && _animationNames.Contains(skillId.ToString());
    }

    /// <summary>
    /// 播放技能/普攻 Spine 动画。每次调用递增 <see cref="playId"/> 并清除 hide 等待态。
    /// <para>Complete：若收到 hide 且 playId 匹配则保持姿态；否则触发 <paramref name="onOverAtk"/>。</para>
    /// </summary>
    /// <param name="skillId">技能 ID，对应 Spine 动画名</param>
    /// <param name="onAttackHitCallback">Spine atk 事件回调（触发技能节点图）</param>
    /// <param name="onOverAtk">动画结束回调（非 hide 技能或 Return 播完后触发）</param>
    public void PlayAtkAnimation(int skillId, Action onAttackHitCallback = null, Action onOverAtk = null)
    {
        ++playId;
        _waitReturn = false;
        int myId = playId;

        if (_skeletonGraphic == null) return;

        if (!HasSkillAnimation(skillId.ToString()))
        {
            LogTool.LogWarning(
                $"[SoldierAnimation] {_soldier.cardId} 缺少技能动画: skillId={skillId}, spine={_soldier.soldierBattle?.Spine}");
            onAttackHitCallback?.Invoke();
            onOverAtk?.Invoke();
            return;
        }
        
        // 🔹 先补触发之前未触发的攻击
        if (!_atkTriggered && _onAttackHit != null)
        {
            _atkTriggered = true;
            _onAttackHit?.Invoke();
        }
        
        // 🔹 重置状态
        _atkTriggered = false;
        _onAttackHit = onAttackHitCallback;
        _onOverAtk = onOverAtk;
        
        string animName = skillId.ToString();
        var track = _skeletonGraphic.AnimationState.SetAnimation(0, animName, false);
        track.Complete += OnSkillAnimationComplete;

        void OnSkillAnimationComplete(TrackEntry trackEntry)
        {
            if (myId != playId)
            {
                return;
            }

            // hide 技能：保持最后一帧，等待 ReturnAnimation 节点播放 return
            if (_waitReturn && myId == _waitReturnPlayId)
            {
                return;
            }

            if (_onOverAtk != null)
            {
                _onOverAtk();
            }
        }
    }

    /// <summary>
    /// 播放混合轨道动画（技能节点图 PlayMixAnimation 节点调用）
    /// </summary>
    public void PlayMixAnimation(string animName, int track, float alpha, bool loop)
    {
        if (_skeletonGraphic == null)
        {
            return;
        }

        if (!HasSkillAnimation(animName))
        {
            LogTool.LogWarning(
                $"[SoldierAnimation] {_soldier.cardId} 缺少混合动画: {animName}, spine={_soldier.soldierBattle?.Spine}");
            return;
        }

        var mixTrackEntry = _skeletonGraphic.AnimationState.SetAnimation(track, animName, loop);
        mixTrackEntry.Alpha = alpha;
    }

    /// <summary>
    /// 关闭混合动画（按动画名查找轨道，淡出后清除）
    /// </summary>
    public void StopMixAnimation(string animName, float fadeDuration = 0.2f)
    {
        if (_skeletonGraphic == null || string.IsNullOrEmpty(animName))
        {
            return;
        }

        var animationState = _skeletonGraphic.AnimationState;
        var cleared = false;

        for (int i = 0; i < animationState.Tracks.Count; i++)
        {
            var trackEntry = animationState.GetCurrent(i);
            if (trackEntry?.Animation?.Name != animName)
            {
                continue;
            }

            animationState.SetEmptyAnimation(i, fadeDuration);
            cleared = true;
        }

        if (!cleared)
        {
            LogTool.LogWarning(
                $"[SoldierAnimation] {_soldier.cardId} 未找到正在播放的混合动画: {animName}");
        }
    }

    #endregion

    #region 事件监听

    /// <summary>
    /// Spine 轨道事件：atk 触发技能逻辑；hide 进入 Return 等待（钻地/潜行等）。
    /// </summary>
    private void OnSpineEvent(TrackEntry trackEntry, Spine.Event e)
    {
        if (e.Data.Name == "atk")
        {
            _atkTriggered = true;
            if (_onAttackHit != null)
            {
                _onAttackHit();
            }
        }
        else if (e.Data.Name == "hide")
        {
            // 记录当前 playId，技能动画 Complete 时不回 Idle，由 Return 节点收尾
            _waitReturn = true;
            _waitReturnPlayId = playId;
        }
    }

    private void ClearAttackHitCallback()
    {
        _onAttackHit = null;
    }

    #endregion

    #region 旋转控制
    
    public void RotateToTarget(Vector3 targetPostion)
    {
        if (_soldier.PlayerspineObj == null) return;

        Vector3 targetUIPos = Camera.main.WorldToScreenPoint(_soldier.transform.position);
        Vector3 selfUIPos = RectTransformUtility.WorldToScreenPoint(Camera.main, targetPostion);
        _soldier.PlayerspineObj.transform.rotation = targetUIPos.x < selfUIPos.x
            ? Quaternion.Euler(_soldier.PlayerspineObj.transform.rotation.eulerAngles.x, 0, _soldier.PlayerspineObj.transform.rotation.eulerAngles.z)
            : Quaternion.Euler(_soldier.PlayerspineObj.transform.rotation.eulerAngles.x, 180, _soldier.PlayerspineObj.transform.rotation.eulerAngles.z);
    }

    #endregion

    
    public void SetTransparencyForCopyBody()
    {
        if (_skeletonGraphic != null)
        {
            Color c = _skeletonGraphic.color;
            c.a = 120f / 255f;
            _skeletonGraphic.color = c;
        }
    }
    
    public void StopAllAnimations()
    {
        if (_skeletonGraphic != null)
            _skeletonGraphic.AnimationState.ClearTrack(0);
        _skeletonGraphic.AnimationState.Event -= OnSpineEvent;
        _waitReturn = false;
        ClearAttackHitCallback();
    }

    public void PauseAnimation()
    {
        if (_skeletonGraphic == null || _isPaused) return;

        _cachedTimeScaleBeforePause = _skeletonGraphic.AnimationState.TimeScale;
        _skeletonGraphic.AnimationState.TimeScale = 0f;
        _isPaused = true;
    }

    public void ResumeAnimation()
    {
        if (_skeletonGraphic == null || !_isPaused) return;

        var resumeScale = _cachedTimeScaleBeforePause > 0f ? _cachedTimeScaleBeforePause : _animationTimeScale;
        if (resumeScale <= 0f) resumeScale = 1f;
        _skeletonGraphic.AnimationState.TimeScale = resumeScale;
        _isPaused = false;
    }
    
}