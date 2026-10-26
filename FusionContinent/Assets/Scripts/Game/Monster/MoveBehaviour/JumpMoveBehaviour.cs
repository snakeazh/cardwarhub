using Config;
using UnityEngine;

namespace FusionContinent.Monster.MoveBehaviour
{
    /// <summary>
    /// 跳跃移动（MoveType.Jumping）。
    ///
    /// 两种模式（由 monster.runStage 决定）：
    ///   - jumpmove：Delay → Rise → Fall 循环（原 MonsterTweens.MoveUIElement）
    ///   - 其他：继承 StraightMoveBehaviour 的直线位移
    ///
    /// 注意：当前配置 MoveType.Jumping 映射为 runStage.StraightTravel，默认走直线模式。
    /// </summary>
    public class JumpMoveBehaviour : StraightMoveBehaviour
    {
        private enum JumpPhase
        {
            None,
            Delay,  // 等待 DealyTime 归零
            Rise,   // 向上跳 JumpmoveDistance
            Fall,   // 向下落 JumpDownmoveDistance
        }

        private JumpPhase _jumpPhase = JumpPhase.None;
        private float _phaseElapsed;
        private float _delayStartValue;
        private Vector3 _jumpStartPosition;
        private Vector3 _jumpBaseScale;

        /// <summary>触墙后是否已执行过弹起偏移，防止重复触发。</summary>
        private bool _isJumpWallMoving;

        public override void Enter(MonsterCard monster)
        {
            _monster = monster;
            ResetSpeedScale();
            monster.monsterData.isMoveEngaged = false;

            if (monster.runStage == MonsterCard.RunStage.jumpmove)
            {
                StartJumpDelay(monster);
                return;
            }

            StartDownwardMove();
        }

        public override void Update(MonsterCard monster, float deltaTime)
        {
            if (_jumpPhase != JumpPhase.None)
            {
                UpdateJumpCycle(monster, deltaTime);
                return;
            }

            base.Update(monster, deltaTime);
        }

        public override void StopMove()
        {
            base.StopMove();
            _jumpPhase = JumpPhase.None;
            _phaseElapsed = 0f;
            _isJumpWallMoving = false;
        }

        /// <summary>跳跃过程中若需要索敌转向，退出跳跃循环并切回直线移动。</summary>
        protected override void TryEngageTargetMove(MonsterCard monster)
        {
            if (_jumpPhase != JumpPhase.None)
            {
                _jumpPhase = JumpPhase.None;
            }

            base.TryEngageTargetMove(monster);
        }

        private void StartJumpDelay(MonsterCard monster)
        {
            _jumpPhase = JumpPhase.Delay;
            _phaseElapsed = 0f;
            _delayStartValue = monster.DealyTime;
            _isJumpWallMoving = false;
            _phase = MonsterMovePhase.None;
        }

        private float GetAbnormalSpeedFactor(MonsterCard monster)
        {
            var abnormal = monster.monsterData.monsterAbnormal;
            return abnormal.monsterspeed * (abnormal.vertigo ? 0f : 1f);
        }

        private void UpdateJumpCycle(MonsterCard monster, float deltaTime)
        {
            if (monster.Iswall)
            {
                UpdateJumpWallPush(monster);
                return;
            }

            switch (_jumpPhase)
            {
                case JumpPhase.Delay:
                    UpdateJumpDelay(monster, deltaTime);
                    break;
                case JumpPhase.Rise:
                    UpdateJumpRise(monster, deltaTime);
                    break;
                case JumpPhase.Fall:
                    UpdateJumpFall(monster, deltaTime);
                    break;
            }
        }

        /// <summary>触墙时向上弹起（原 MoveUIElement 的 Iswall 分支）。</summary>
        private void UpdateJumpWallPush(MonsterCard monster)
        {
            if (_isJumpWallMoving)
            {
                return;
            }

            RectTransform rect = monster.transform.GetComponent<RectTransform>();
            rect.anchoredPosition += new Vector2(0, 200);
            _isJumpWallMoving = true;
        }

        private void UpdateJumpDelay(MonsterCard monster, float deltaTime)
        {
            float speedFactor = GetAbnormalSpeedFactor(monster);
            if (speedFactor <= 0f)
            {
                return;
            }

            float duration = 1f * speedFactor;
            _phaseElapsed += deltaTime;
            float progress = Mathf.Clamp01(_phaseElapsed / duration);
            monster.DealyTime = Mathf.Lerp(_delayStartValue, 0f, progress);

            if (_phaseElapsed < duration)
            {
                return;
            }

            if (monster._monsterConfig.Type == MonsterType.TheFrogPrince)
            {
                monster.Play("walk", true);
                monster.AnimationController.SetTimeScale(1 *
                    Global.SpeedMultiplier.Value *
                    monster.monsterData.monsterAbnormal.monsterspeed *
                    (monster.monsterData.monsterAbnormal.vertigo ? 0 : 1) *
                    monster._tbMonsterBattleConfig.GetOrDefault(monster._cardId).MoveAnimationSpeed);
            }

            _jumpStartPosition = monster.uiElement.position;
            _jumpBaseScale = monster.uiElement.localScale;
            _phaseElapsed = 0f;
            _jumpPhase = JumpPhase.Rise;
        }

        private void UpdateJumpRise(MonsterCard monster, float deltaTime)
        {
            _phaseElapsed += deltaTime;
            float progress = Mathf.Clamp01(_phaseElapsed / monster.JumpDuration);

            Vector3 position = _jumpStartPosition;
            position.y += monster.JumpmoveDistance * progress;
            monster.uiElement.position = position;

            // 起跳时略微放大，营造跳跃感
            float scaleProgress = Mathf.Clamp01(_phaseElapsed / 1f);
            float scaleFactor = scaleProgress <= 1f
                ? Mathf.Lerp(1f, 1.05f, scaleProgress)
                : Mathf.Lerp(1.05f, 1f, Mathf.Clamp01((_phaseElapsed - 1f) / monster.JumpDownDuration));
            monster.uiElement.localScale = _jumpBaseScale * scaleFactor;

            if (_phaseElapsed < monster.JumpDuration)
            {
                return;
            }

            _jumpStartPosition = monster.uiElement.position;
            _phaseElapsed = 0f;
            _jumpPhase = JumpPhase.Fall;
        }

        private void UpdateJumpFall(MonsterCard monster, float deltaTime)
        {
            _phaseElapsed += deltaTime;
            float progress = Mathf.Clamp01(_phaseElapsed / monster.JumpDownDuration);

            Vector3 position = _jumpStartPosition;
            position.y -= monster.JumpDownmoveDistance * progress;
            monster.uiElement.position = position;

            if (_phaseElapsed < monster.JumpDownDuration)
            {
                return;
            }

            // 落地后进入下一轮跳跃
            StartJumpDelay(monster);
        }
    }
}
