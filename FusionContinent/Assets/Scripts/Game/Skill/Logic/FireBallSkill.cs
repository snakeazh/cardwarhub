using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Skill;
using Game.Skill.Logic;
using Manager;
using UnityEngine;

//远程单体伤害技能，发射一个火球，命中后造成伤害并播放爆炸特效.是一个样例
namespace FusionContinent.Skill
{
    public class FireballSkill : SkillBase
    {
        private float _localScale;
        private float _missileSpeed;
        private float _duration;
        
        protected override void OnCast(object obj)
        {
            // 读取属性
            _localScale = 1f + Attributes.GetFinal(AttributeType.SkillRange);
            _missileSpeed = Attributes.GetFinal(AttributeType.SkillMissileSpeed);
            _duration = Attributes.GetFinal(AttributeType.SkillDuration);
            
            // 获取发射方向
            Vector3 direction = ((Vector3)obj).normalized;
            
            // 发射单个火球
            SpawnFireball(direction);
        }
        
        private void SpawnFireball(Vector3 direction)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate(Config.ProjectileName);
            if (pool == null)
            {
                LogTool.LogError($"FireballSkill: 找不到对象池 {Config.ProjectileName}");
                return;
            }

            GameObject fireball = pool.Get();
            if (fireball == null) return;

            // 设置初始位置和大小
            fireball.transform.SetParent(GameLand.Default.Bullet);
            fireball.transform.position = Parent.transform.position;
            fireball.transform.localScale = Vector3.one * _localScale;
            
            // 使用 ProjectileBullet 组件
            var projectile = fireball.GetComponent<ProjectileBullet>();
            if (projectile != null)
            {
                projectile.SetStart(this, direction.normalized, 0f);
                projectile.SetPool(pool);
                
                projectile.OnHit += (hitPos) => 
                {
                    OnFireballHit(hitPos);
                };
                
                projectile.OnComplete += () => 
                {
                    if (fireball != null && fireball.activeInHierarchy)
                    {
                        pool.Return(fireball);
                    }
                };
            }
            else
            {
                // 备用：DOTween移动
                LogTool.LogWarning("FireballSkill: 火球预制体缺少 ProjectileBullet 组件");
                
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                fireball.transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
                
                Vector3 targetPos = Parent.transform.position + direction * 100f;
                fireball.transform.DOMove(targetPos, 100f / _missileSpeed)
                    .SetEase(Ease.Linear)
                    .SetSpeedBased()
                    .OnComplete(() => pool.Return(fireball));
            }
        }
        
        private void OnFireballHit(Vector3 hitPos)
        {
            // 播放爆炸特效
            if (!string.IsNullOrEmpty(Config.SkillHitEffect))
            {
                ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                {
                    PoolName = Config.SkillHitEffect,
                    ParentTran = GameLand.Default.DamageParent.transform,
                    Position = hitPos,
                    LocalScale = 150f,
                    DelayReturn = 0.5f
                });
            }
        }
    }
}
