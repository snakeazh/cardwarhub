using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using Game.StateMachine;
using Manager;
using UnityEngine;

namespace Game.StateMachine
{
    public class MonsterWeakState : State
    {
        private Timer t;
        
        
        // Start is called before the first frame update
        public MonsterWeakState(StateMachine sm, MonsterCard monster) : base(sm, monster)
        {
        }
        
        public override void Enter()
        {
            // 停止移动
            LogTool.Log("进入了虚弱状态");
            monster.monsterData.monsterTweens.KillAllTweens();
            //开启眩晕动画
            // monster.monsterData.monsterAbnormal.SetAbnormal(UnusualStatusType.Vertigo, monster.monsterData.WeaknessDuration, 0, ValueType.Number);
            // 虚弱期间提升易伤倍率，受到伤害变为原来的3倍
            monster.monsterData.monsterAbnormal.vulnerabilitydamage = 3f;
            monster.AnimationController.SetSkeletonTimeScale(1);
            monster.Play("weak", true);
            //播放破防特效 和音效
            SoundManager.Instance.PlayVoiceInGame(15);
            PlayVFX();

            // 进入虚弱恢复保护期，防止 Timer 触发前受到新伤害导致竞态
            monster.monsterData.isRecoveringFromWeak = true;
            monster.monsterData.isMoveEngaged = false;
            monster.hasEngagedTarget = false;
            monster.Iswall = false;
            if (monster.AttackBehaviour != null)
            {
                monster.AttackBehaviour.StopEngage(monster);
            }
            t = TimerSystem.Instance.Create(monster.monsterData.WeaknessDuration, () =>
            {
                if (monster == null) return;
                // 保护：如果恢复期间再次触发过韧性伤害，此 Timer 失效
                if (!monster.monsterData.isRecoveringFromWeak) return;
                monster.monsterData.monsterState = MonsterState.move;
                // 强制调用 Tick() 立即触发状态机转换
                monster.Tick();
            });

            // 韧性条从0缓慢增长到满
            StartStaggerBarGrowth();
        }

        public override void Update()
        {
        }

        public override void Exit()
        {
            TimerSystem.Instance.Cancel(t);
            StopStaggerBarGrowth();
            // 退出虚弱状态后恢复易伤倍率
            if (monster != null && monster.monsterData?.monsterAbnormal != null)
            {
                monster.monsterData.monsterAbnormal.vulnerabilitydamage = 1f;
            }
            //关闭眩晕动画
            monster.monsterData.CurrentResilience = monster.monsterData.MaxResilience;
            // 解除恢复保护期，允许新的韧性伤害正常触发
            monster.monsterData.isRecoveringFromWeak = false;
            monster.hasEngagedTarget = false;
            monster.Iswall = false;
        }

        private void PlayVFX()
        {
            DOVirtual.DelayedCall(0.1f, () =>
            {
                QFramework.GenericObjectPool pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate("ShieldDownEffect01");
                if (pool == null) return;

                var effect = pool.Get();
                if (effect == null) return;

                effect.transform.SetParent(GameLand.Default.DamageParent.transform);
                effect.transform.position = monster.transform.position;
                effect.transform.localScale = Vector3.one;

                //延迟1s后
                DOVirtual.DelayedCall(1f, () =>
                {
                    if (effect == null) return;
                    // 隐藏特效，交由对象池或其他管理逻辑处理
                    pool.Return(effect);
                    GamePlayManager.Default.TriggerGuide(GuideType.WeaknessWordTip);
                });
            });
        }

        private Tween _staggerBarTween;

        private void StartStaggerBarGrowth()
        {
            var staggerBarRect = monster.StaggerBarRect;
            var parentRect = monster.StaggerBarParentRect;
            var staggerBarEffectImage = monster.StaggerBarEffectImage;
            if (staggerBarRect == null || parentRect == null) return;
            float fullWidth = parentRect.rect.width;

            // 前景层初始设为0
            staggerBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);

            // 背景层初始也设为0，保持两层同步（从最大宽度变到0）
            if (staggerBarEffectImage != null)
            {
                var effectRect = staggerBarEffectImage.rectTransform;
                effectRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0f);
            }

            _staggerBarTween = DOTween.To(
                    () => 0f,
                    x =>
                    {
                        // 前景层：0 -> 最大
                        staggerBarRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * x);
                        // 背景层同步：0 -> 最大（两层一起从0增长到最大）
                        if (staggerBarEffectImage != null)
                        {
                            staggerBarEffectImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fullWidth * x);
                        }
                    },
                    1f,
                    monster.monsterData.WeaknessDuration
                )
                .SetEase(Ease.Linear);
        }

        private void StopStaggerBarGrowth()
        {
            _staggerBarTween?.Kill();
            _staggerBarTween = null;
        }
    }
}