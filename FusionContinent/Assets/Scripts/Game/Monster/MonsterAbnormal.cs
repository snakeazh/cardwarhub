using System;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using UnityEngine;
using ValueType = Config.ValueType;

public class MonsterAbnormal : MonoBehaviour
{
    // ===== 状态字段 =====
    public float monsterspeed = 1;
    public float vulnerabilitydamage = 1;
    public bool vertigo; //眩晕
    public bool weak; //虚弱
    public float lastPhysical_Resistance;
    public float lastMana_Resistance;
    public bool invincible;//无敌
    public bool disarm;//缴械

    public GameObject abnormal;

    public bool IsAbnormal = false;
    public float AbnormalTime = 1;
    public float Abnormaldamage = 0;

    private void OnDisable()
    {
        // 仅重置数值/本地 abnormal 子节点；Buff 池化特效由 EffectManager 延迟回池（避免 OnDisable 期间 SetParent）
        ResetAbnormalFields();
    }

    /// <summary>
    /// 对象池复用 / 回池时重置异常状态与特效（不依赖 Buff OnRemove）
    /// </summary>
    public void ResetAllAbnormalState()
    {
        ResetAbnormalFields();

        var monster = GetComponent<MonsterCard>();
        if (monster != null)
        {
            // 兜底：Buff 已清但 BuffVfx 未走 OnRemove 时，回收挂在怪物上的池化特效
            RecycleOrphanedBuffVfx(monster.transform);
            var effectPos = monster.transform.Find("EffectPos");
            if (effectPos != null)
                RecycleOrphanedBuffVfx(effectPos);
        }
    }

    private void ResetAbnormalFields()
    {
        monsterspeed = 1;
        vulnerabilitydamage = 1;
        vertigo = false;
        weak = false;
        invincible = false;
        disarm = false;
        IsAbnormal = false;
        AbnormalTime = 1;
        Abnormaldamage = 0;

        var monster = GetComponent<MonsterCard>();
        if (monster != null)
        {
            if (monster.DisarmIcon != null)
                monster.DisarmIcon.gameObject.SetActive(false);

            // Init 之前 monsterData.monsterTweens 可能尚未就绪
            if (monster.monsterData?.monsterTweens != null)
                monster.AttackState(false);
        }

        if (abnormal == null)
            return;

        DOTween.Kill(abnormal);

        for (int i = 0; i < abnormal.transform.childCount; i++)
        {
            HideEffectNode(abnormal.transform.GetChild(i));
        }
    }

    public void SetSpeed()
    {
        GetComponent<MonsterCard>().monsterData.monsterTweens.SetMoveSpeed((vertigo ? 0 : 1) * monsterspeed);
    }

    // ===== 状态应用入口（供 AbnormalBuff.OnAdd 调用） =====
    public void Apply(UnusualStatusType abnormalId, float value, ValueType valueType)
    {
        if (!isActiveAndEnabled || GetComponent<MonsterCard>() == null) return;

        switch (abnormalId)
        {
            case UnusualStatusType.Moderate:
                ApplySlow(value, valueType);
                break;
            case UnusualStatusType.Accelerate:
                ApplyAccelerate(value, valueType);
                break;
            case UnusualStatusType.Burn:
                ApplyBurn(value, valueType);
                break;
            case UnusualStatusType.Vulnerable:
                ApplyVulnerable(value, valueType);
                break;
            case UnusualStatusType.Vertigo:
                ApplyVertigo();
                break;
            case UnusualStatusType.KnockBack:
                ApplyKnockBack(value, valueType);
                break;
            case UnusualStatusType.Invincible:
                ApplyInvincible();
                break;
            case UnusualStatusType.Disarm:
                ApplyDisarm();
                break;
        }
    }

    // ===== 状态移除入口（供 AbnormalBuff.OnRemove 调用） =====
    public void Remove(UnusualStatusType abnormalId)
    {
        switch (abnormalId)
        {
            case UnusualStatusType.Moderate:
                RemoveSlow();
                break;
            case UnusualStatusType.Accelerate:
                RemoveAccelerate();
                break;
            case UnusualStatusType.Burn:
                RemoveBurn();
                break;
            case UnusualStatusType.Vulnerable:
                RemoveVulnerable();
                break;
            case UnusualStatusType.Vertigo:
                RemoveVertigo();
                break;
            case UnusualStatusType.KnockBack:
                // 击退是一次性效果，无需移除
                break;
            case UnusualStatusType.Invincible:
                RemoveInvincible();
                break;
            case UnusualStatusType.Disarm:
                RemoveDisarm();
                break;
        }
    }

    // ===== 各状态的具体实现 =====

    private void ApplySlow(float value, ValueType valueType)
    {
        if (GetComponent<MonsterCard>().monsterData.monsterState != MonsterState.move) return;
        monsterspeed = valueType == ValueType.Number ? value : GetComponent<MonsterCard>().monsterData.monsterTweens.GetMoveSpeed() * (-value);
        abnormal.transform.Find("slow").localScale = Vector3.one * 5;
        SetSpeed();
    }

    private void RemoveSlow()
    {
        monsterspeed = 1;
        SetSpeed();
        abnormal.transform.Find("slow").localScale = Vector3.zero;
    }

    private void ApplyAccelerate(float value, ValueType valueType)
    {
        if (GetComponent<MonsterCard>().monsterData.monsterState != MonsterState.move) return;
        monsterspeed = valueType == ValueType.Number ? value : GetComponent<MonsterCard>().monsterData.monsterTweens.GetMoveSpeed() * (value);
        abnormal.transform.Find("accelerate").localScale = Vector3.one * 1;
        SetSpeed();
    }

    private void RemoveAccelerate()
    {
        monsterspeed = 1;
        SetSpeed();
        abnormal.transform.Find("accelerate").localScale = Vector3.zero;
    }

    private void ApplyBurn(float value, ValueType valueType)
    {
        // 灼烧伤害现在由 BurnAbnormalBuff.OnIntervalTick 处理
        // 这里只负责显示图标与粒子
        ShowEffectNode(abnormal.transform.Find("fire"), new Vector3(2f, 2f, 0f));
    }

    private void RemoveBurn()
    {
        if (abnormal == null)
            return;

        HideEffectNode(abnormal.transform.Find("fire"));
    }

    private void ApplyVulnerable(float value, ValueType valueType)
    {
        vulnerabilitydamage = value;
        abnormal.transform.Find("vulnerabilitydamage").localScale = Vector3.one * 5;
    }

    private void RemoveVulnerable()
    {
        vulnerabilitydamage = 1;
        abnormal.transform.Find("vulnerabilitydamage").localScale = Vector3.zero;
    }

    private void ApplyVertigo()
    {
        vertigo = true;
        abnormal.transform.Find("vertigo").localScale = Vector3.one * 5;
        abnormal.transform.Find("vertigo/BuffFreeze01Loop").gameObject.SetActive(true);
        abnormal.transform.Find("vertigo/BuffFreeze01End").gameObject.SetActive(false);
        SetSpeed();
    }

    private void RemoveVertigo()
    {
        vertigo = false;
        SetSpeed();
        abnormal.transform.Find("vertigo/BuffFreeze01Loop").gameObject.SetActive(false);
        abnormal.transform.Find("vertigo/BuffFreeze01End").gameObject.SetActive(false);
        abnormal.transform.Find("vertigo/BuffFreeze01End").gameObject.SetActive(true);
        DOTween.To(v => { }, 0, 0, 0.5f).onComplete += () =>
        {
            abnormal.transform.Find("vertigo/BuffFreeze01End").gameObject.SetActive(false);
            abnormal.transform.Find("vertigo").localScale = Vector3.zero;
        };
    }

    private void ApplyKnockBack(float value, ValueType valueType)
    {
        var monster = GetComponent<MonsterCard>();
        var targetPos = new Vector2(monster.transform.position.x, monster.transform.position.y + value / 500);
        monster.PullToPosition(targetPos, 0.3f);
    }

    private void ApplyInvincible()
    {
        invincible = true;
        abnormal.transform.Find("invincible").localScale = Vector3.one * 1;
    }

    private void RemoveInvincible()
    {
        invincible = false;
        abnormal.transform.Find("invincible").localScale = Vector3.zero;
    }

    private void ApplyDisarm()
    {
        disarm = true;
        GetComponent<MonsterCard>().DisarmIcon.gameObject.SetActive(true);
        GetComponent<MonsterCard>().AttackState(true);
    }

    private void RemoveDisarm()
    {
        disarm = false;
        GetComponent<MonsterCard>().DisarmIcon.gameObject.SetActive(false);
        GetComponent<MonsterCard>().AttackState(false);
    }

    // ===== 向后兼容：旧接口仍然支持 =====

    /// <summary>
    /// 设置异常状态持续时间读表
    /// </summary>
    public void SetAbnormal(UnusualStatusType abnormaluid)
    {
        SetAbnormal(abnormaluid, 3, 5, 0);
    }

    /// <summary>
    /// 异常状态持续时间手动设置
    /// </summary>
    public void SetAbnormal(UnusualStatusType abnormaluid, float duration, float value, ValueType valueType)
    {
        Apply(abnormaluid, value, valueType);
    }

    public void MonsterGain(int uid, float duration)
    {
        switch (uid)
        {
            case 5: // 无敌
                ApplyInvincible();
                break;
            case 1:
                break;
        }
    }

    public void SetResistance(int abnormaluid, float pervalue, float value, float duration)
    {
        // 保留原有抗性逻辑（暂不重构）
    }

    private static void ShowEffectNode(Transform node, Vector3 scale)
    {
        if (node == null)
            return;

        node.gameObject.SetActive(true);
        node.localScale = scale;

        var particleSystems = node.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Play(true);
        }
    }

    private static void HideEffectNode(Transform node)
    {
        if (node == null)
            return;

        node.localScale = Vector3.zero;

        var particleSystems = node.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            var ps = particleSystems[i];
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
        }

        node.gameObject.SetActive(false);
    }

    /// <summary>
    /// 回收通过 EffectManager 挂在角色上的 Buff 特效（带 EffectLifecycle 的子节点）
    /// </summary>
    private static void RecycleOrphanedBuffVfx(Transform root)
    {
        if (root == null || EffectManager.Instance == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i);
            if (child.GetComponent<EffectLifecycle>() != null)
            {
                EffectManager.Instance.RecycleEffect(child.gameObject);
                continue;
            }

            RecycleOrphanedBuffVfx(child);
        }
    }
}
