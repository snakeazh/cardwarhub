using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

public class ViolentConflicts : MonoBehaviour
{
    private float[] firepos = { -1.2f,-1f,-0.6f, -0.3f, -0.1f, 0f, 0.1f, 0.3f, 0.6f,1f,1.2f};

    public void SetViolentConf(int continuousAtkNum, Vector3 targetpos, ISkill skillCard, float lanuchspeed)
    {
        var projectileName = skillCard.Config.ProjectileName;
        var skillFirstpool = ObjectPoolManager.Instance.GetGameObjectPool($"{projectileName}01");
        var skillpool = ObjectPoolManager.Instance.GetGameObjectPool($"{projectileName}03Start");
        var skillpool2 = ObjectPoolManager.Instance.GetGameObjectPool($"{projectileName}03Loop");
        var skillpool3 = ObjectPoolManager.Instance.GetGameObjectPool($"{projectileName}03End");

        GameObject skillFirst = skillFirstpool.Get();
        if (skillCard == null) return;
        skillFirst.transform.position = targetpos;

        DOVirtual.DelayedCall(1.5f, () =>
        {
            skillFirstpool.Return(skillFirst);

            GameObject skillSecond = skillpool.Get();
            if (skillSecond == null) return;
            skillSecond.transform.position = targetpos;
            DOVirtual.DelayedCall(0.5f, () =>
            {
                skillpool.Return(skillSecond);

                Sequence attackSequence = DOTween.Sequence();

                GameObject skillSecond2Instance = skillpool2.Get();
                bool hasReturnedSkillSecond2 = false;

                if (skillSecond2Instance == null) return;
                skillSecond2Instance.transform.position = targetpos;

                // 创建攻击波次
                for (int wave = 0; wave < continuousAtkNum; wave++)
                {
                    // 添加延迟（除了第一波）
                    if (wave > 0)
                    {
                        attackSequence.AppendInterval(0.1f); // 波次之间的延迟
                    }

                    // 添加一波7支箭同时发射
                    attackSequence.AppendCallback(() =>
                    {
                        // 同时发射所有位置的箭
                        for (int i = 0; i < firepos.Length; i++)
                        {
                            float xOffset = firepos[i];
                            Vector3 fireTarget = targetpos - new Vector3(xOffset, -1f, 0f);

                            var bulletPool = ObjectPoolManager.Instance.GetGameObjectPool($"{projectileName}02");
                            GameObject bullet = bulletPool.Get();
                            if (bullet == null) continue;

                            var proj = bullet.GetComponent<Projectile>();
                            if (proj == null) continue;

                            bullet.transform.position = targetpos;
                            Vector3 dir = (fireTarget - targetpos).normalized;
                            proj.Launch(dir, lanuchspeed, skillCard, skillCard.Parent);
                        }
                    });
                }

                attackSequence.OnComplete(() =>
                {
                    LogTool.Log($"{gameObject.name} 的暴力剑雨攻击完成");

                    if (!hasReturnedSkillSecond2 && skillSecond2Instance != null)
                    {
                        skillpool2.Return(skillSecond2Instance);
                        hasReturnedSkillSecond2 = true;
                    }

                    GameObject skillThird = skillpool3.Get();
                    if (skillThird == null) return;
                    skillThird.transform.position = targetpos;

                    DOVirtual.DelayedCall(5f, () =>
                    {
                        skillpool3.Return(skillThird);
                        ObjectPoolManager.Instance.GetGameObjectPool($"{projectileName}02").ReturnAll();
                    });
                });
            });
        });
    }
}