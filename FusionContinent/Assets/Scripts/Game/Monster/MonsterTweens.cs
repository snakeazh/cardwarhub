using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using Spine;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Sequence = DG.Tweening.Sequence;

namespace FusionContinent
{
//管理monster上面所有的tween
    public class MonsterTweens : MonoBehaviour
    {
        private Tween moveTween;
        private Tween atkTween;
        private Tween damageTween;
        private Tween damageImgTween;
        private MonsterCard m;

        public MonsterCard Monster => m;

        /// <summary>
        /// 增加攻速
        /// </summary>
        /// <param name="speed"></param>
        public void AddAtkSpeed(float speed)
        {
            atkTween.timeScale += speed;
        }
        
        /// <summary>
        /// 增加移动速度倍率（委托 MoveBehaviour，Tick 逐帧生效）
        /// </summary>
        public void AddMoveSpeed(float speed)
        {
            m.MoveBehaviour.AddSpeedScale(speed);
        }

        /// <summary>
        /// 获取当前移动速度倍率
        /// </summary>
        public float GetMoveSpeed()
        {
            return m.MoveBehaviour.GetSpeedScale(m);
        }

        // 停止移动（委托 MoveBehaviour）
        public void PauseMove()
        {
            m.MoveBehaviour.StopMove();
        }
        
        // 怪物首次受击的移速修正
        public void MonsterFirstHitMoveSpeedCorrection(float takeMoveSpeed)
        {
            m.MoveBehaviour.AddSpeedScale(takeMoveSpeed / 100f);
        }

        // 异常状态刷新移速倍率（实际倍率由 MonsterAbnormal.monsterspeed + MoveBehaviour 加成决定）
        public void SetMoveSpeed(float monsterspeed)
        {
        }

        //外部使用的缴械行为导致的攻击停止
        public void AtkTweenState(bool atkStop)
        {
            if (atkStop)
            {
                atkTween?.Pause();
            }
            else
            {
                atkTween?.Play();
            }
        }

        //初始化所有的tweeen
        public void InitTweens(MonsterCard monsterCard)
        {
            KillAllTweens();
            moveTween = null;
            atkTween = null;
            damageImgTween = null;
            m = monsterCard;
        }

        //受击以后 缩放Tween
        public void TriggerHitEffectTween()
        {
            damageTween?.Kill();
            damageTween = DOTween.Sequence()
                .Append(transform.DOScale(m.selfScale, 0.05f).SetEase(Ease.OutBack))
                .OnComplete(() => damageTween = null);
            damageTween.SetId(GlobalConst.MonsterTween);
        }

        public void PullToPosition((Vector2,float) pullinfo)
        {
            Vector2 targetPos = pullinfo.Item1;
            float duration = pullinfo.Item2;
            m.MoveBehaviour.StopMove();
            moveTween?.Kill();
            atkTween?.Kill();
            atkTween = null;
            // Move with tween（走 Rigidbody2D，避免 transform 直接改位导致静态接触重建）
            moveTween = m.PhysicsBody.DOMove(targetPos, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    moveTween = null;
                    m.Play("walk", true);
                    m.Iswall = false;
                    m.hasEngagedTarget = false;
                    m.monsterData.monsterState = MonsterState.move;
                    m.Tick();
                    //当前在行走
                    //这里的spinewale可以先不用管 因为状态机会强制它继续行走
                    // SpineWalk();
                });
        }
        
        
        /// <summary>
        /// 让怪物击飞
        /// </summary>
        /// <param name="height">击飞高度</param>
        /// <param name="durationUp">上升时间</param>
        /// <param name="durationDown">下落时间</param>
        /// <param name="airTime">空中停留时间</param>
        public void KnockUpMonster((float,float,float,float) knockUpinfo)
        {
            float height = knockUpinfo.Item1;
            float durationUp = knockUpinfo.Item2;
            float airTime = knockUpinfo.Item3;
            float durationDown = knockUpinfo.Item4;

            m.MoveBehaviour.StopMove();
            moveTween?.Kill();
            atkTween?.Kill();
            
            var spineTran = m.AnimationController.SpineContent;
            Vector3 startPos = spineTran.localPosition;
            Vector3 topPos = startPos + Vector3.up * height;
            Vector3 endPos = startPos; // 回到原位
        
            // 上升阶段
            moveTween = spineTran.DOLocalMove(topPos, durationUp)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 空中停留阶段
                    DOVirtual.DelayedCall(airTime, () =>
                    {
                        // 下落阶段
                        atkTween = spineTran.DOLocalMove(endPos, durationDown)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() =>
                            {
                                moveTween = null;
                                atkTween = null;
                                m.hasEngagedTarget = false;
                                m.Iswall = false;
                                m.monsterData.monsterState = MonsterState.move;
                                m.MoveBehaviour.Enter(m);
                                m.Tick();
                            });
                    });
                });
        }

        //停止攻击动画（遗留接口，攻击循环已迁至 AttackBehaviour）
        public void PauseAtkTween()
        {
        }

        public void PlayAtkTween()
        {
        }

        //清除所有的tween
        public void KillAllTweens()
        {
            moveTween?.Kill();
            atkTween?.Kill();
            damageTween?.Kill();
            damageImgTween?.Kill();

            moveTween = null;
            atkTween = null;
            damageImgTween = null;
        }

        private void OnDestroy()
        {
            KillAllTweens();
        }
    }
}
