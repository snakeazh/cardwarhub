using Config;
using DG.Tweening;
using UnityEngine;

namespace FusionContinent.Skill
{
    /**
     * 能量迸发
     * SpearmenHeavySplittingChop
     **/
    public class CdTimeEnergyBurstSkill : SkillBase
    {
        private float _duration;
        private float _localScale;
        
        protected override void OnCast(object obj)
        {
            _duration = Mathf.Max(Attributes.GetFinal(AttributeType.SkillDuration), 1f);
            
            OnPlayFullScreen();
            OnCastBlow();
        }
        
        private void OnCastBlow()
        {
            var (pool, bullet) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = Config.ProjectileName,
                ParentTran = GameLand.Default.Bullet,
                Position = new Vector3(0, -3, 0),
                LocalScale = 120 + Attributes.GetFinal(AttributeType.SkillRange),
                DelayReturn = 0f
            });
            
            if (bullet == null) return;
            
            // 脚本控制
            bullet.GetComponent<CdTimeEnergyBurstController>().Pool = pool;
            bullet.GetComponent<CdTimeEnergyBurstController>().SetStart(this, _duration);
        }

        private void OnPlayFullScreen()
        {
            ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = "BondMagicFullScreenEffect01",
                ParentTran = GameLand.Default.Bullet,
                Position = new Vector3(0, -8.5f, 0),
                VectorScale = new Vector3(130f, 90f, 130f),
                DelayReturn = _duration + 1f
            });
        }

    }
}