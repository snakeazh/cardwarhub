using System;
using Cysharp.Threading.Tasks;
using FusionContinent;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 单次技能表现调度请求。
    /// </summary>
    public class BattleCastRequest
    {
        public ulong RequestId;
        public BattleCastKind Kind;
        public BattleCastPriority Priority = BattleCastPriority.Normal;
        public SoldierCard Caster;
        public Component GraphCaster;
        public int SkillId;
        public Vector3 TargetPosition;
        public bool HasTargetPosition;
        public bool UseNearestEnemy;
        public Component SkillGraphTarget;
        public bool SkipBattleRuleBeforeTrigger;
        public bool SkipBuffOnCast;

        /// <summary>为 true 时，在调度出队时再进入技能卡 CD 并施法（自动批量释放）。</summary>
        public bool DeferSkillCardCooldown;

        private UniTaskCompletionSource<bool> _completionSource;
        private Action<bool> _onComplete;

        public void SetOnComplete(Action<bool> onComplete)
        {
            _onComplete = onComplete;
        }

        public UniTask<bool> WaitAsync()
        {
            if (_completionSource == null)
            {
                _completionSource = new UniTaskCompletionSource<bool>();
            }

            return _completionSource.Task;
        }

        public void Complete(bool success)
        {
            if (_onComplete != null)
            {
                _onComplete(success);
            }

            if (_completionSource != null)
            {
                _completionSource.TrySetResult(success);
            }
        }
    }
}
