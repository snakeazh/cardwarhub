using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

public class StormPuncture : MonoBehaviour
{
    private Vector3 _targetpos;
    private GameObject stormPuncture;
    private BoxCollider2D collider;

    public void SetStormPuncture(Vector3 targetPos, ISkill skillCard, SoldierCard soldierCard,
        float launchSpeed)
    {
        _targetpos = targetPos;

        const float delayStormPuncture = 1f;
        const float stormPunctureDuration = 1f;
        const float detectTime = 0.5f; // 在发射后0.5秒进行碰撞检测

        Sequence seq = DOTween.Sequence();

        stormPuncture = null;

        var pool2 = CommonItemPoolManager.Instance.GetGameObjectPool("AnnaSkillStormPierce03");
        GameObject pierce = pool2.Get();
        if(pierce==null) return;
        pierce.transform.position = _targetpos;

        seq.AppendInterval(delayStormPuncture);
        seq.AppendCallback(() =>
        {
            pool2.Return(pierce);
            var pool = CommonItemPoolManager.Instance.GetGameObjectPool("AnnaSkillStormPierce02");
            stormPuncture = pool?.Get();
            ResetHitMonsters();
            if (stormPuncture == null) return;

            stormPuncture.transform.position = _targetpos;

            var proj = stormPuncture.GetComponent<Projectile>();
            if (proj != null)
                proj.Launch(Vector3.up, launchSpeed, skillCard, soldierCard);
            collider = stormPuncture.GetComponent<BoxCollider2D>();
        });
        // 回收特效
        seq.AppendInterval(stormPunctureDuration - detectTime);
        seq.AppendCallback(() =>
        {
            if (stormPuncture != null)
            {
                var pool = CommonItemPoolManager.Instance.GetGameObjectPool("AnnaSkillStormPierce02");
                pool?.Return(stormPuncture);
                stormPuncture = null;
            }
        });
    }

    private HashSet<MonsterCard> hitMonsters = new HashSet<MonsterCard>(); // 存储已命中的怪物

    private void Update()
    {
        if (stormPuncture == null || collider == null) return;

        Vector2 worldPos = collider.transform.TransformPoint(collider.offset);
        Vector2 size = collider.size;
        float angle = collider.transform.eulerAngles.z;

        //使用LayerMask过滤
        int enemyLayer = LayerMask.GetMask("MonsterBox");
        Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, size, angle, enemyLayer);

        LogTool.Log($"实际检测到碰撞体数量: {hits.Length}");
            
        foreach (var hit in hits)
        {
            if (hit == null) continue;

            //精确获取组件（限制检测层级）
            var monster = hit.transform.parent.GetComponent<MonsterCard>();
            if (monster != null&& !hitMonsters.Contains(monster))
            {
                monster.PushPull(1, 500, false);
                hitMonsters.Add(monster); // 标记为已命中
            }
        }
    }

// 在攻击结束时调用（例如动画事件或技能结束时）
    public void ResetHitMonsters()
    {
        hitMonsters.Clear();
        LogTool.Log("已重置命中列表，可再次触发怪物");
    }
}
