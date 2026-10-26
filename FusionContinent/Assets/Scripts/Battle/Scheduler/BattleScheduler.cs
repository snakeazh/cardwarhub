using System.Collections.Generic;
using FusionContinent;
using UnityEngine;
using Unity.Profiling;

namespace Game.Battle
{
    /// <summary>
    /// 战斗技能表现调度器：按帧预算削峰，不修改伤害与规则逻辑。
    /// </summary>
    public class BattleScheduler
    {
        private static BattleScheduler _instance;

        public static BattleScheduler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BattleScheduler();
                }

                return _instance;
            }
        }

        private readonly BattleSchedulerConfig _config = new BattleSchedulerConfig();
        private readonly Queue<BattleCastRequest> _waitQueue = new Queue<BattleCastRequest>();
        private readonly BattleCastDispatcher _dispatcher = new BattleCastDispatcher();
        private readonly List<BattleCastRequest> _cancelScratch = new List<BattleCastRequest>();

        private ulong _nextRequestId;
        private int _consumedThisFrame;
        private long _lastProcessedFrame = -1;
        private int _bypassDepth;

        public BattleSchedulerConfig Config => _config;

        public bool IsBypassing => _bypassDepth > 0;

        public int PendingCount => _waitQueue.Count;

        public void ResetForBattle()
        {
            Clear();
        }

        public void Clear()
        {
            _waitQueue.Clear();
            _consumedThisFrame = 0;
            _lastProcessedFrame = -1;
        }

        public bool RequestCastSkill(BattleCastRequest request)
        {
            if (!ValidateRequest(request))
            {
                return false;
            }

            request.RequestId = ++_nextRequestId;

            if (!ShouldSchedule(request))
            {
                _dispatcher.Dispatch(request);
                return true;
            }

            if (_waitQueue.Count >= _config.MaxQueueSize)
            {
                LogTool.LogWarning(
                    $"[BattleScheduler] 队列已满，拒绝入队: skillId={request.SkillId}, queue={_waitQueue.Count}");
                return false;
            }

            _waitQueue.Enqueue(request);
            return true;
        }

        public bool ShouldScheduleSkillGraph()
        {
            if (!_config.Enabled)
            {
                return false;
            }

            if (_bypassDepth > 0)
            {
                return false;
            }

            if (_config.OnlyInAutoMode && !Global.AutoReleaseSkillState.Value)
            {
                return false;
            }

            return true;
        }

        public void ProcessLateUpdate()
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.UpdateSkill))
            {
                ProcessLateUpdateInternal();
            }
        }

        private void ProcessLateUpdateInternal()
        {
            var frame = Time.frameCount;
            if (frame != _lastProcessedFrame)
            {
                _consumedThisFrame = 0;
                _lastProcessedFrame = frame;
            }

            if (!_config.Enabled || _waitQueue.Count == 0)
            {
                return;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay != null && gamePlay.IsPaused())
            {
                return;
            }

            while (_waitQueue.Count > 0 && _consumedThisFrame < _config.MaxSkillPerFrame)
            {
                var request = _waitQueue.Dequeue();
                if (!IsRequestStillValid(request))
                {
                    request.Complete(false);
                    continue;
                }

                _consumedThisFrame++;
                _dispatcher.Dispatch(request);
            }
        }

        public void CancelByCaster(SoldierCard caster)
        {
            if (caster == null || _waitQueue.Count == 0)
            {
                return;
            }

            _cancelScratch.Clear();
            while (_waitQueue.Count > 0)
            {
                var request = _waitQueue.Dequeue();
                if (request.Caster == caster)
                {
                    request.Complete(false);
                    continue;
                }

                _cancelScratch.Add(request);
            }

            for (int i = 0; i < _cancelScratch.Count; i++)
            {
                _waitQueue.Enqueue(_cancelScratch[i]);
            }

            _cancelScratch.Clear();
        }

        internal void EnterBypassScope()
        {
            _bypassDepth++;
        }

        internal void ExitBypassScope()
        {
            if (_bypassDepth > 0)
            {
                _bypassDepth--;
            }
        }

        private bool ShouldSchedule(BattleCastRequest request)
        {
            if (!_config.Enabled)
            {
                return false;
            }

            if (_bypassDepth > 0)
            {
                return false;
            }

            if (request.DeferSkillCardCooldown)
            {
                return true;
            }

            if (_config.ManualBypassQueue && request.Priority == BattleCastPriority.High)
            {
                return false;
            }

            if (_config.OnlyInAutoMode && !Global.AutoReleaseSkillState.Value)
            {
                return false;
            }

            return true;
        }

        private static bool ValidateRequest(BattleCastRequest request)
        {
            if (request == null)
            {
                LogTool.LogWarning("[BattleScheduler] request 为空");
                return false;
            }

            if (request.Kind == BattleCastKind.SkillGraphToPoint ||
                request.Kind == BattleCastKind.SkillGraphToTarget)
            {
                if (request.SkillId <= 0)
                {
                    LogTool.LogWarning("[BattleScheduler] SkillGraph 请求 skillId 无效");
                    return false;
                }

                if (request.GraphCaster == null && request.Caster == null)
                {
                    LogTool.LogWarning("[BattleScheduler] SkillGraph 请求 caster 无效");
                    return false;
                }

                return true;
            }

            if (request.Caster == null)
            {
                LogTool.LogWarning("[BattleScheduler] caster 为空");
                return false;
            }

            if (request.Caster.isDie)
            {
                LogTool.LogWarning($"[BattleScheduler] caster 已死亡: {request.Caster.cardId}");
                return false;
            }

            if (request.Kind == BattleCastKind.FullActiveSkill && request.SkillId <= 0)
            {
                LogTool.LogWarning("[BattleScheduler] FullActiveSkill skillId 无效");
                return false;
            }

            return true;
        }

        private static bool IsRequestStillValid(BattleCastRequest request)
        {
            if (request.Kind == BattleCastKind.SkillGraphToPoint ||
                request.Kind == BattleCastKind.SkillGraphToTarget)
            {
                return request.SkillId > 0;
            }

            if (request.Caster == null || request.Caster.isDie)
            {
                return false;
            }

            var gamePlay = GamePlayManager.Default;
            if (gamePlay != null && gamePlay.IsPaused())
            {
                return false;
            }

            return true;
        }
    }
}
