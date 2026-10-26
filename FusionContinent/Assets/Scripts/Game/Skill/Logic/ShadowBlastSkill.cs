using Config;
using DG.Tweening;
using Game.Skill.Logic;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 魔影轰杀 LujiaSkillShadowBlast
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class ShadowBlastSkill : SkillBase
    {
        private int _spawnedCount;
        private float _localScale;
        private Tween _spawnTween;
        private float _spawnInterval = 0.2f;
        
        protected override void OnCast(object obj)
        {
            _localScale = 100 + Attributes.GetFinal(AttributeType.SkillRange);
            _spawnInterval = Mathf.Max(Attributes.GetFinal(AttributeType.SkillContinuousAtkSpeed), 0.2f);
            
            var directionPos = (Vector3)obj;
            CreateArrowRain(directionPos);
        }
        
        /**
         * DOTween Sequence 在一帧里执行了大量 AppendCallback
         * 只创建 1 个 Tween，不会一次性创建 N 个 Tween，不卡顿
         **/
        private void CreateArrowRain(Vector3 directionPos)
        {
            // 结束旧的生成
            _spawnTween?.Kill();
            _spawnedCount = 0;
            
            int maxSalvosNum = (int)Attributes.GetFinal(AttributeType.SkillSalvosNum);
            float repelMonster = (int)Attributes.GetFinal(AttributeType.SkillRepelMonster);
            float bulletSpeed = GetLaunchSpeed();
        
            void SpawnNext()
            {
                _spawnedCount++;
                if (_spawnedCount > maxSalvosNum)
                {
                    return;
                }
                
                // 判断是否击飞怪物
                float repelMonsterVal = repelMonster > 0 && _spawnedCount == maxSalvosNum ? repelMonster : 0f;
                SpawnSingleArrow(directionPos, bulletSpeed, repelMonsterVal);
        
                // 每 spawnInterval 执行一次
                _spawnTween = DOVirtual.DelayedCall(_spawnInterval, SpawnNext).SetUpdate(true);
            }
        
            // 立刻开始第一次
            SpawnNext();
        }

        private void SpawnSingleArrow(Vector3 direction, float bulletSpeed, float repelMonsterVal)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}01");
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = Parent.transform.position;
            bullet.transform.localScale = Vector3.one * _localScale;
            
            // 子弹脚本
            var bulletScript = bullet.GetComponent<BulletFiringCollisionEffect>();
            bulletScript.Launch(direction, bulletSpeed);
            bulletScript.OnHit += (targetPos) =>
            {
                OnCastBombHit(targetPos, repelMonsterVal);
            };
            bulletScript.OnComplete += () =>
            {
                DOVirtual.DelayedCall(0.3f, () => bullet.gameObject.SetActive(false));
            };

            DOVirtual.DelayedCall(2f, () => pool.Return(bullet));
        }
        
        private void OnCastBombHit(Vector3 targetPos, float repelMonsterVal)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}02");
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = targetPos;
            bullet.transform.localScale = Vector3.one * _localScale;
            
            // 子弹脚本
            bullet.GetComponent<BoxCollider2D>().enabled = true;
            bullet.GetComponent<DrawSlashBullet>().repelMonsterVal = repelMonsterVal;
            bullet.GetComponent<DrawSlashBullet>().SetStartAttack(this);
            DOVirtual.DelayedCall(0.2f, () => bullet.GetComponent<BoxCollider2D>().enabled = false);
            
            // 技能持续时间
            DOVirtual.DelayedCall(1.5f, () => pool.Return(bullet));
        }

    }
}