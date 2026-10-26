using System;
using DG.Tweening;
using FusionContinent.Skill;

namespace FusionContinent.Model
{
    // 主城技能
    public class CitySkill
    {
        public int Id; // 技能Id
        public bool IsCool = false;
        public bool InCast = false;// 释放中
        public float LastTime = 0;
        public float LastTime0 = 0;
        public Tween CoolTween = null;
        public float RemainTime = 0f; // 剩余时间
        public ISkill BaseSkill = null;// 技能数据
    }
}