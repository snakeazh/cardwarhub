using System;
using System.Collections.Generic;
using FusionContinent.Buff;
using FusionContinent.Skill;

namespace FusionContinent
{
    /// <summary>
    /// 友方角色 Buff 控制器
    /// 继承纯 C# BuffController，额外管理月影盾
    /// </summary>
    public class SoldierBuffController : BuffController
    {
        private MoonlightShield _moonlightShield;

        /// <summary>
        /// 月影盾属性（用于伤害计算器）
        /// </summary>
        public MoonlightShield MoonlightShield => _moonlightShield;

        public SoldierBuffController(SoldierCard soldier) : base(soldier)
        {
            _moonlightShield = new MoonlightShield();
        }

        /// <summary>
        /// 添加月影盾
        /// </summary>
        public void AddMoonlightShield(float addVal)
        {
            if (Owner is SoldierCard soldier)
                _moonlightShield.AddShield(soldier, addVal);
        }

        public override void ClearAll()
        {
            base.ClearAll();
            _moonlightShield = new MoonlightShield();
        }
    }
}
