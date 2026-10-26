using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 魔法震荡 LujiaSkillMagicShake
     * 
     * @return 
     * @author Zpp
     * @create 2025/10/24
     **/
    public class MagicShakeSkill : SkillBase
    {
        private float _localScale;
        private Vector3 _releasePos;
        
        private bool _isCasting = false;
        private float _startTime = 0.2f;
        private float _maxDurationTime = 5f;
        
        protected override void OnCast(object obj)
        {
            if (_isCasting) return; // 防止重复施放
            _isCasting = true;
            
            _localScale = 100 + Attributes.GetFinal(AttributeType.SkillRange);
            _releasePos = (Vector3)obj;
            
            PlayMineEffect();

            // 延迟播放
            DOVirtual.DelayedCall(_startTime, () =>
            {
                if (!_isCasting) return; // 检查是否还在施法状态
                
                CreateThunderCloud();
            });
        }

        private void PlayMineEffect()
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}01");
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = _releasePos;
            bullet.transform.localScale = Vector3.one * _localScale;
            
            DOVirtual.DelayedCall(_maxDurationTime, () =>
            {
                _isCasting = false;
                pool.Return(bullet);
            });
        }

        private int _spawnedCount;
        private Tween _spawnTween;
        private void CreateThunderCloud()
        {
            // 结束旧的生成
            _spawnTween?.Kill();
            _spawnedCount = 0;
            
            // 获取技能配置
            int maxSalvosNum = (int)Attributes.GetFinal(AttributeType.SkillSalvosNum);
            int knockUpMonster = (int)Attributes.GetFinal(AttributeType.SkillKnockUpMonster);
            float spawnInterval = (_maxDurationTime - 1f) / maxSalvosNum;
        
            void SpawnNext()
            {
                if (!_isCasting) return;
                _spawnedCount++;
                
                // 判断是否击飞怪物
                float knockUpMonsterVal = knockUpMonster > 0 && _spawnedCount == maxSalvosNum ? knockUpMonster : 0f;

                // 生成雷云
                SpawnThunderCloud(knockUpMonsterVal);
                
                // 每 spawnInterval 执行一次
                _spawnTween = DOVirtual.DelayedCall(spawnInterval, SpawnNext).SetUpdate(true);
            }
        
            // 立刻开始第一次
            SpawnNext();
        }

        private void SpawnThunderCloud(float knockUpMonsterVal)
        {
            var leftTime = 1.3f; 
            var suffixName = "02";
            if (knockUpMonsterVal > 0f)
            {
                leftTime = 2.5f;
                suffixName = "03";
                _isCasting = false;
            }
            
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}{suffixName}");
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = _releasePos;
            bullet.transform.localScale = Vector3.one * _localScale;
            
            // 子弹脚本
            bullet.GetComponent<BoxCollider2D>().enabled = true;
            bullet.GetComponent<DrawSlashBullet>().knockUpVal = knockUpMonsterVal;
            bullet.GetComponent<DrawSlashBullet>().SetStartAttack(this, 0);
            DOVirtual.DelayedCall(0.3f, () => bullet.GetComponent<BoxCollider2D>().enabled = false);
            DOVirtual.DelayedCall(leftTime, () => pool.Return(bullet));
        }

    }
}