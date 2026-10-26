using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalConst
{
    public static class Layer
    {
        public const int MonsterLayer = 8;
        public const int BossLayer = 999;
        public const int PlayerLayer = 9;
        public const int AngleWallLayer = 13;
    }
    
    public const float ATTACK_SPEED_FACTOR = 5f;
    /// <summary>怪物移动/攻击等 MonsterTweens 使用的 tween id</summary>
    public const string MonsterTween = "MonsterTween";
    /// <summary>局内战斗相关 tween 统一 id，ClearBattleLegacySkillState / 暂停时批量处理</summary>
    public const string BattleTween = "BattleTween";
    public const string DamageUIPool = "Damage";

    public static Vector3 monsterPosOffset = new Vector3(0, 0.5f, 0);
    public static List<int> soundBossHit = new List<int> { 18, 19, 20 };
    public static List<int> soundEliteHit = new List<int> { 21, 22, 23 };
    /// <summary>锁定攻击间隔（LockAtkInterval）时的固定间隔倍率/值</summary>
    public const float LOCK_ATTACK_INTERVAL = 2f;
    /// <summary>锁定攻击速度（LockAtkSpeed）时的默认固定攻击间隔（秒），配表 BaseValue 优先</summary>
    public const float LOCK_ATTACK_SPEED = 0.5f;
    public const float MAX_ATTACK_SPEED = 10f;
    public const float SKILL_CARD_DEFAULT_SCALE = 1.2f;
    
    public const string BurnEffect = "CharacterEntryEffect01";
}
