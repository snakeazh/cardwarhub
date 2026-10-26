using Config;
using FusionContinent;
using Game.Audio;
using Manager;
using UnityEngine;

public static class BattleAudioSystem
{
    private enum HitSoundCategory
    {
        Normal = 0,
        Weak = 1,
        Elite = 2,
        Boss = 3,
    }

    private const float NormalHitCooldown = 0.08f;

    private const float SameSoundPitchWindow = 0.25f;
    private const float PitchRandomMin = 0.92f;
    private const float PitchRandomMax = 1.08f;

    private static float _lastPlayTime = -999f;
    private static int _lastSoundId = -1;
    private static int _lastPriority;

    public static void Init()
    {
        EventBus.Subscribe<HpDamageEvent>(OnDamage);
        EventBus.Subscribe<UseSkillEvent>(OnSkillCast);
    }

    private static void OnDamage(HpDamageEvent data)
    {
        PlayHitSound(data);
    }

    private static void OnSkillCast(UseSkillEvent data)
    {
        if (!string.IsNullOrEmpty(data.SfXName))
        {
            AudioManager.Instance.SFX.Play(data.SfXName);
        }
    }

    private static void PlayHitSound(HpDamageEvent data)
    {
        if (data.IsMiss || data.TargetCard == null)
        {
            return;
        }

        var monsterConfig = DataConfigManager.Tables.TbMonsterBattleConfig.GetOrDefault(data.TargetCard._cardId);
        if (monsterConfig == null)
        {
            return;
        }

        if (!TryResolveHitSound(monsterConfig, data.TargetCard, out int soundId, out HitSoundCategory category))
        {
            return;
        }

        int priority = (int)category;
        if (!ShouldPlayHitSound(category, priority))
        {
            return;
        }

        float pitchMultiplier = 1f;
        if (soundId == _lastSoundId && Time.time - _lastPlayTime < SameSoundPitchWindow)
        {
            pitchMultiplier = Random.Range(PitchRandomMin, PitchRandomMax);
        }

        PlayHitSoundClip(soundId, pitchMultiplier);

        _lastPlayTime = Time.time;
        _lastSoundId = soundId;
        _lastPriority = priority;
    }

    private static bool TryResolveHitSound(
        MonsterBattleConfig monsterConfig,
        MonsterCard targetCard,
        out int soundId,
        out HitSoundCategory category)
    {
        soundId = 0;
        category = HitSoundCategory.Normal;

        if (monsterConfig.MonsterQuality is MonsterQuality.Elite or MonsterQuality.Boss)
        {
            if (targetCard.monsterData.monsterState == MonsterState.weak)
            {
                soundId = 17;
                category = HitSoundCategory.Weak;
                return true;
            }

            if (monsterConfig.MonsterQuality is MonsterQuality.Boss)
            {
                soundId = GlobalConst.soundBossHit[Random.Range(0, GlobalConst.soundBossHit.Count)];
                category = HitSoundCategory.Boss;
            }
            else
            {
                soundId = GlobalConst.soundEliteHit[Random.Range(0, GlobalConst.soundEliteHit.Count)];
                category = HitSoundCategory.Elite;
            }

            return true;
        }

        soundId = 16;
        category = HitSoundCategory.Normal;
        return true;
    }

    private static bool ShouldPlayHitSound(HitSoundCategory category, int priority)
    {
        // 精英 / Boss / 破韧后不受冷却限制
        if (category != HitSoundCategory.Normal)
        {
            return true;
        }

        float elapsed = Time.time - _lastPlayTime;
        if (elapsed >= NormalHitCooldown)
        {
            return true;
        }

        // 小怪受击音冷却内，仅更高优先级可插入（Boss > Elite > Weak > Normal）
        return priority > _lastPriority;
    }

    private static void PlayHitSoundClip(int soundType, float pitchMultiplier)
    {
        if (!DataConfigManager.Tables.TbGameSoundConfig.DataMap.ContainsKey(soundType))
        {
            return;
        }

        var soundInfo = DataConfigManager.Tables.TbGameSoundConfig[soundType];
        if (soundInfo == null)
        {
            return;
        }

        float pitch = Global.SpeedMultiplier.Value * pitchMultiplier;
        SoundManager.Instance.PlayVoice(soundInfo.SoundEffects, soundInfo.SoundVolume, pitch);
    }

    public static void OnDisable()
    {
        EventBus.Unsubscribe<HpDamageEvent>(OnDamage);
        EventBus.Unsubscribe<UseSkillEvent>(OnSkillCast);

        _lastPlayTime = -999f;
        _lastSoundId = -1;
        _lastPriority = 0;
    }
}
