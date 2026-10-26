using Config;
using DG.Tweening;
using Game.Skill.Logic;
using Manager;
using UnityEngine;

namespace FusionContinent.Skill
{
    /*
     * 暗夜彗星
     * LujiaSkillDarkComet
     **/
    public class DarkCometSkill : SkillBase
    {
        private float _localScale;
        private Tween _spawnTween;
        private float _spawnInterval = 0.2f;
        
        protected override void OnCast(object obj)
        {
            _localScale = 100 + Attributes.GetFinal(AttributeType.SkillRange);
            _spawnInterval = Mathf.Min(Attributes.GetFinal(AttributeType.SkillContinuousAtkSpeed), 0.2f);
            
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
            
            float salvoAngle = Attributes.GetFinal(AttributeType.SkillSalvoAngle);
            float maxSalvosNum = Attributes.GetFinal(AttributeType.SkillSalvosNum);
            float dropSpeed = GetLaunchSpeed();
            float salvosNum = 0;
        
            void SpawnNext()
            {
                salvosNum++;
                if (salvosNum > maxSalvosNum)
                {
                    return;
                }

                // 随机偏移落点
                Vector2 randomPos = Random.insideUnitCircle * 1.5f;
                Vector3 spawnPos = new Vector3(
                    directionPos.x + randomPos.x * 1.5f,
                    directionPos.y + randomPos.y,
                    0
                );
                
                SpawnSingleArrow(spawnPos, dropSpeed, salvoAngle);
        
                // 每 spawnInterval 执行一次
                _spawnTween = DOVirtual.DelayedCall(_spawnInterval, SpawnNext).SetUpdate(true);
            }
        
            // 立刻开始第一次
            SpawnNext();
        }

        private void SpawnSingleArrow(Vector3 directionPos, float dropSpeed, float salvoAngle)
        {
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}01");
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.localScale = Vector3.one * _localScale;
            
            // bullet.GetComponent<FireballDropEffect>().Play(directionPos, dropSpeed, salvoAngle);
            // bullet.GetComponent<FireballDropEffect>().OnHit += (targetPos) =>
            // {
            //     pool.Return(bullet);
            //     OnCastDarkComet(targetPos);
            // };

        }
        
        private void OnCastDarkComet(Vector3 directionPos)
        {
            if (Config.SoundId > 0) SoundManager.Instance.PlayVoiceInGame(Config.SoundId);
            
            var pool = ObjectPoolManager.Instance.GetGameObjectPoolOrCreate($"{Config.ProjectileName}02");
            if (pool == null) return;

            GameObject bullet = pool.Get();
            if (bullet == null) return;

            // 子弹设置
            bullet.transform.SetParent(GameLand.Default.Bullet);
            bullet.transform.position = directionPos;
            bullet.transform.localScale = Vector3.one * _localScale;
            bullet.GetComponent<DrawSlashBullet>().SetStartAttack(this);
            
            // 技能持续时间
            DOVirtual.DelayedCall(2.3f, () => { pool.Return(bullet); });
        }

    }
}