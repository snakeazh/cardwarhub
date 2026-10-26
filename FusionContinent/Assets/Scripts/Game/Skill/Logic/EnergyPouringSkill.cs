using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent.Attribute;
using Manager;
using UnityEngine;
using FusionContinent.Buff;
using Object = System.Object;

namespace FusionContinent.Skill
{
    /**
     * 能量倾注
     * EliaSkillEnergySaturation
     * 
     * @return 
     * @author Zpp
     * @create 2026/2/10
     **/
    public class EnergyPouringSkill : SkillBase
    {
        private float _localScale;
        private float _delayReturn;
        private Vector3 _parentPosition;
        
        // 固定加成属性
        private readonly AttributeModifier _modifier = new AttributeModifier
        {
            SourceKey = "EnergyPouringSkill",
            SourceType = AttributeSourceType.Skill,
        };

        private List<AttributeData> _attributes = new List<AttributeData>();

        protected override void OnCast(object finalDirection)
        {
            if (Config.ProjectileName == "")
            {
                LogTool.LogError("能量倾注技能子弹名未配置");
                return;
            }

            _localScale = 100f + Attributes.GetFinal(AttributeType.SkillRange);
            _delayReturn = Mathf.Min(Attributes.GetFinal(AttributeType.SkillDuration), 5f);
            _parentPosition = this.Parent.transform.position;

            RaiseTakeHand();
            DOVirtual.DelayedCall(1f, () => {
                AddBuff();
                StartDraw();
                DrawBullet();
            });
        }
        
        private void RaiseTakeHand()
        {
            ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = $"{Config.ProjectileName}03",
                ParentTran = GameLand.Default.Bullet,
                Position = new Vector3(0, _parentPosition.y, 0),
                LocalScale = _localScale,
                DelayReturn = 2.2f
            });
        }

        private void StartDraw()
        {
            ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = $"{Config.ProjectileName}01",
                ParentTran = GameLand.Default.Bullet,
                Position = new Vector3(0, _parentPosition.y - 1.5f, 0),
                LocalScale = _localScale,
                DelayReturn = _delayReturn
            });
        }

        private void DrawBullet()
        {
            var (_, bullet) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = $"{Config.ProjectileName}02",
                ParentTran = GameLand.Default.Bullet,
                Position = new Vector3(0, _parentPosition.y - 1.5f, 0),
                VectorScale = new Vector3(_localScale, _localScale + 20f, 0),
                DelayReturn = _delayReturn
            });
            
            var bulletScripts = bullet.GetComponent<DrawSlashBullet>();
            if (bulletScripts != null)
            {
                bulletScripts.SetStartAttack(this, 1f);
            }
        }

        private void AddBuff()
        {
            if (_attributes.Count == 0)
            {
                var lvCfg = Config.LevelPropertyList[Level - 1];
                foreach (var attr in lvCfg.Attributes)
                {
                    if (attr.Type == AttributeType.SoldierAttackPower)
                    {
                        _attributes.Add(new AttributeData(attr));
                    }
                }
            }

            GamePlayManager.Default.TraverseVillageSoldier((soldierCard) =>
            {
                 // soldierCard.AddBuff(new BuffBase(BuffType.RequiemChant, _delayReturn, EffectCallback));
            });
        }

        private void EffectCallback(bool isState, Object obj)
        {
            var redouble = 1;
            var soldierCard = (SoldierCard)obj;
            
            // 结束
            if (!isState)
                redouble = 0;

            AttributeManager.SetAttributesLogic(_modifier, _attributes, false, redouble, soldierCard.cardId);
        }
        
    }
}