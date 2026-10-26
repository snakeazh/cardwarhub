using System;
using Config;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 眩晕 Buff — 禁止普通攻击
    /// OnAdd: 设置 CannotNormalAttack = true
    /// OnTick: 仅累加计时器
    /// OnRemove: 恢复 CannotNormalAttack
    /// </summary>
    public class StunBuff : BuffBase
    {
        /// <summary>被眩晕前是否允许普攻（OnRemove 时还原）</summary>
        private bool _previousCannotNormalAttack;

        public StunBuff(float duration)
            : base(BuffType.Vertigo, duration, 0, ValueType.Number)
        {
        }

        public override void OnAdd(object owner)
        {
            base.OnAdd(owner);
            if (owner is not SoldierCard soldier) return;

            _previousCannotNormalAttack = soldier.CannotNormalAttack;
            soldier.CannotNormalAttack = true;
            LogTool.L($"[StunBuff] 眩晕生效，禁止普攻");
        }

        public override void OnRemove(object owner)
        {
            if (owner is not SoldierCard soldier) return;

            soldier.CannotNormalAttack = _previousCannotNormalAttack;
            LogTool.L($"[StunBuff] 眩晕解除");
        }
    }
}
