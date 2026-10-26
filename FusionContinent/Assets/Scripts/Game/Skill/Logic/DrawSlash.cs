using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using QFramework;
using UnityEngine;

namespace FusionContinent.Skill
{
    public class DrawSlash : SkillBase
    {
        [SerializeField] private float slashSpeed = 50f; // 光波飞行速度 (米/秒)
        [SerializeField] private float slashDistance = 100f; // 光波最大飞行距离
        [SerializeField] private float slashScale = 100f; // 光波缩放大小

        protected override void OnCast(object finalDirection)
        {
            CreanteDrawSlash((Vector3)Parent.transform.position);
        }

        private void CreanteDrawSlash(Vector3 pos)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null) return;

            GameObject drawSlash = pool.Get();
            if (drawSlash == null) return;

            drawSlash.transform.SetParent(GameLand.Default.Bullet);
            drawSlash.transform.localScale = Vector3.one * slashScale;
            drawSlash.transform.position = pos;
            drawSlash.GetComponent<DrawSlashBullet>().SetStartAttack(this);
            
            // 获取施法者前方（可根据你的需求换成 obj 的 forward）
            Vector3 forward =new Vector3(0,0.5f,1);

            // 终点位置
            Vector3 targetPos = pos + forward * slashDistance;

            // 根据速度计算时间
            float duration = slashDistance / slashSpeed;

            // DOTween 移动
            drawSlash.transform.DOMove(targetPos, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => { Object.Destroy(drawSlash); });
        }
    }
}