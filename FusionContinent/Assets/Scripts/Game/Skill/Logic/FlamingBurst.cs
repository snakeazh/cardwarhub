using UnityEngine;
using DG.Tweening;

namespace FusionContinent.Skill
{
    public class FlamingBurstData
    {
        public Vector3 ReleasePosition;// 释放位置
        public float ForkAngle = 10;// 左右分叉角度
        public int ForkIndex = 0;// 分叉下标
        public int RoundIndex = 0;// 回合下标（总共打几轮）
    }

    /**
     * 爆炎斩
     */
    public class FlamingBurst : SkillBase
    {
        [SerializeField] private int exlevel = 3;          // 发射几路（1=正前，2=+左，3=+右）
        [SerializeField] private float interval = 0.2f;    // 每轮发射间隔
        [SerializeField] private float forwardStep = 2f;   // 每轮推进距离（Y轴）
        [SerializeField] private float forkAngle = 10f;        // 左右分叉角度
        [SerializeField] private float scale = 100f;       // 火球缩放
        [SerializeField] private int totalRounds = 8;      // 总共打几轮

        protected override void OnCast(object obj)
        {
            var data = (FlamingBurstData)obj;
            SpawnFireballDirection(data.ReleasePosition, data.ForkIndex, data.RoundIndex, data.ForkAngle);
        }
        
        // protected override void OnCast(object obj)
        // {
        //     CastFlamingBurst((Vector3)obj);
        // }

        private void CastFlamingBurst(Vector3 basePos)
        {
            for (int j = 0; j < totalRounds; j++)
            {
                int roundIndex = j;

                // 每轮延迟 interval * j，然后同时生成这一轮的所有火球
                DOVirtual.DelayedCall(interval * roundIndex, () =>
                {
                    for (int i = 0; i < exlevel; i++)
                    {
                        // SpawnFireball(basePos, i, roundIndex);
                        // SpawnFireballDirection(basePos, i, roundIndex);
                    }
                });
            }
        }
        
        
        private void SpawnFireballDirection(Vector3 direction, int i, int j, float sideOffset)
        {
            Vector3 basePos = Parent.transform.position;

            // 归一化方向
            direction = direction.normalized;

            // 计算“前进偏移”——沿着 direction 方向推进
            Vector3 forwardOffset = direction * forwardStep * j;

            // 基于 direction 计算左右分叉偏移（垂直方向）
            // 使用 Vector3.Cross 求出“右方向”
            Vector3 rightDir = Vector3.Cross(direction, Vector3.forward).normalized;

            // 分叉角度转为弧度
            // float rad = forkAngle * Mathf.Deg2Rad;

            // 左右偏移
            // float sideOffset = 0f;
            // if (i == 1) // 左
            //     sideOffset = -Mathf.Tan(rad) * forwardStep * j;
            // else if (i == 2) // 右
            //     sideOffset =  Mathf.Tan(rad) * forwardStep * j;

            // 组合位置偏移
            // Vector3 spawnPos = basePos + forwardOffset + rightDir * sideOffset;
            Vector3 spawnPos = basePos + forwardOffset + rightDir;

            var pool = ObjectPoolManager.Instance.GetGameObjectPool(this.Config.ProjectileName);
            if (pool == null) return;

            GameObject flamingBurst = pool.Get();
            if (flamingBurst == null) return;

            // 子弹设置
            flamingBurst.transform.SetParent(GameLand.Default.Bullet);
            flamingBurst.transform.position = spawnPos;
            flamingBurst.transform.localScale = Vector3.one * scale;
            flamingBurst.GetComponent<DrawSlashBullet>().SetStartAttack(this);

            // 让子弹朝向正确方向（与传入的 direction 一致）
            flamingBurst.transform.up = direction;

            DOTween.To(v => { }, 0, 0, 1f).onComplete += () =>
            {
                pool.Return(flamingBurst);
            };
        }
        
        
        private void SpawnFireball(Vector3 direction, int i, int j)
        {
            Vector3 basePos = Parent.transform.position;

            // 🚀 每一轮往前推进（Y轴增加）
            float yOffset = forwardStep * (j);
            float xOffset = 0f;

            // 🚀 根据分叉角度算 X 偏移
            if (i == 1) // 左
                xOffset = -Mathf.Tan(forkAngle * Mathf.Deg2Rad) * yOffset;
            else if (i == 2) // 右
                xOffset =  Mathf.Tan(forkAngle * Mathf.Deg2Rad) * yOffset;

            // 最终位置
            Vector3 spawnPos = basePos + new Vector3(xOffset, yOffset, 0f);

            var pool = ObjectPoolManager.Instance.GetGameObjectPool(this.Config.ProjectileName);
            if (pool == null) return;

            GameObject flamingBurst = pool.Get();
            if (flamingBurst == null) return;

            // 子弹设置
            flamingBurst.transform.SetParent(GameLand.Default.Bullet);
            flamingBurst.transform.position = spawnPos;
            
            flamingBurst.transform.localScale = Vector3.one * scale;
            flamingBurst.GetComponent<DrawSlashBullet>().SetStartAttack(this);

            // 朝向目标方向（正中 / 左右）
            Vector3 dir = new Vector3(xOffset, yOffset, 0f).normalized;
            flamingBurst.transform.up = dir;
            DOTween.To(v => { }, 0, 0, 1f).onComplete += () =>
            {
                pool.Return(flamingBurst);
            };
        }
        
    }
}
