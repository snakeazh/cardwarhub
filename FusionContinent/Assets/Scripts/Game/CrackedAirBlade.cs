using System.Collections;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using UnityEngine;

public class CrackedAirBlade : MonoBehaviour
{
    private Vector3 _targetpos;

    public void SetCrackedAirBlade(Vector3 targetPos, ISkill skillCard, SoldierCard soldierCard,
        float launchSpeed)
    {
        _targetpos = targetPos;

        const float delayStormPierce = 0.5f;
        const float stormPierceDuration = 1f;
        const float windBladeDuration = 2.5f;
        const float windBladeRecycleDelay = 0.5f;

        Sequence seq = DOTween.Sequence();

        GameObject stormPierce = null;
        GameObject windBlade = null;

        // Step 1: 发射风刃前置特效
        seq.AppendInterval(delayStormPierce);
        seq.AppendCallback(() =>
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPool("AnnaSkillStormPierce01");
            stormPierce = pool?.Get();
            if (stormPierce == null) return;

            stormPierce.transform.position = _targetpos;

            var proj = stormPierce.GetComponent<Projectile>();
            proj?.Launch(Vector3.up, launchSpeed, skillCard, soldierCard);
        });

        // Step 2: 回收前置特效并生成主攻击体
        seq.AppendInterval(stormPierceDuration);
        seq.AppendCallback(() =>
        {
            if (stormPierce != null)
            {
                var pool = ObjectPoolManager.Instance.GetGameObjectPool("AnnaSkillStormPierce01");
                pool?.Return(stormPierce);
            }

            var pool2 = ObjectPoolManager.Instance.GetGameObjectPool("AnnaSkillWindBlade01");
            windBlade = pool2?.Get();
            if (windBlade == null) return;

            windBlade.transform.position = new Vector3(targetPos.x, transform.position.y, 0);
        });

        // Step 3: 碰撞检测
        seq.AppendInterval(windBladeDuration);
// 修改后的检测代码（关键修正点标记✅）
        seq.AppendCallback(() => 
        {
            if (windBlade == null) return;

            var box = windBlade.transform.Find("ani_liekongfengren_01/box");
            if (box == null) return;

            var collider = box.GetComponent<BoxCollider2D>();
            if (collider == null) return;

            //精确计算世界坐标（考虑父级缩放）
            Vector2 worldPos = box.transform.TransformPoint(collider.offset);
            Vector2 size = collider.size * new Vector2(box.transform.lossyScale.x, box.transform.lossyScale.y);
            float angle = box.transform.eulerAngles.z;

            // 增强调试绘制（显示碰撞体轮廓）
            DebugDrawBox(worldPos, size, angle, new Color(1,0,0,0.5f), 1f);

            //使用LayerMask过滤
            int enemyLayer = LayerMask.GetMask("MonsterBox");
            Collider2D[] hits = Physics2D.OverlapBoxAll(worldPos, size, angle, enemyLayer);

            LogTool.Log($"实际检测到碰撞体数量: {hits.Length}");
            
            foreach (var hit in hits)
            {
                if (hit == null) continue;

                //精确获取组件（限制检测层级）
                var monster = hit.transform.parent.GetComponent<MonsterCard>();
                if (monster != null)
                {
                    monster.ReceiveDamage(MonsterDamageRequest.ForSkillSumTypes(
                        monster, skillCard, hit.transform.position));
                }
            }
        });

        // Step 4: 回收风刃特效
        seq.AppendInterval(windBladeRecycleDelay);
        seq.AppendCallback(() =>
        {
            if (windBlade != null)
            {
                var pool = ObjectPoolManager.Instance.GetGameObjectPool("AnnaSkillWindBlade01");
                pool?.Return(windBlade);
            }
        });
    }
    
    /// <summary>
    /// 绘制世界空间 OverlapBox 区域（带旋转）
    /// </summary>
    private void DebugDrawBox(Vector2 center, Vector2 size, float angle, Color color, float duration)
    {
        // 更精确的旋转计算（处理各种旋转情况）
        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle));
        Vector2 halfSize = size * 0.5f;

        // 计算四个角点（局部空间）
        Vector2[] localCorners = new Vector2[]
        {
            new Vector2(-halfSize.x, -halfSize.y), // 左下
            new Vector2(halfSize.x, -halfSize.y),  // 右下
            new Vector2(halfSize.x, halfSize.y),   // 右上
            new Vector2(-halfSize.x, halfSize.y)   // 左上
        };

        // 转换到世界空间
        Vector2[] worldCorners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            worldCorners[i] = center + (Vector2)(rotationMatrix * localCorners[i]);
        }

        // 绘制四条边（确保闭合）
        Debug.DrawLine(worldCorners[0], worldCorners[1], color, duration);
        Debug.DrawLine(worldCorners[1], worldCorners[2], color, duration);
        Debug.DrawLine(worldCorners[2], worldCorners[3], color, duration);
        Debug.DrawLine(worldCorners[3], worldCorners[0], color, duration); // 闭合线
    }
}
