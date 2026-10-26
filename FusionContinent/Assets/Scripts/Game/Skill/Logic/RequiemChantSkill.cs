using System.Collections;
using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Buff;
using FusionContinent.Skill;
using Manager;
using QFramework;
using TMPro;
using UnityEngine;
using Object = System.Object;

namespace FusionContinent.Skill
{
    /**
     * 慰灵圣咏
     * EliaSkillSoulComfort
     **/
    public class RequiemChantSkill : SkillBase
    {
        private bool _onCast;
        private float _cdTime;
        private float _duration;

        private Sequence _mainSequence;

        // 固定加成属性
        private readonly AttributeModifier _modifier = new AttributeModifier
        {
            SourceKey = "RequiemChantSkill",
            SourceType = AttributeSourceType.Skill,
        };

        private List<AttributeData> _attributes = new List<AttributeData>();
        private readonly List<AttributeType> _attributeTypes = new List<AttributeType>()
        {
            AttributeType.SoldierAttackPower,
            AttributeType.SoldierAttackSpeed,
            AttributeType.SoldierSpellPower
        };
        
        protected override void OnCast(object finalDirection)
        {
            if (_onCast) return;

            _cdTime = Global.UnlimitedCdTime.Value ? 0.1f : GetCooldownTime(false);
            _duration = Attributes.GetFinal(AttributeType.SkillDuration);
            StartSkill();
        }

        private void StartSkill()
        {
            if (GamePlayManager.Default == null) return;
            
            _attributes = new List<AttributeData>();
            var atkInheritance = Attributes.GetFinal(AttributeType.SkillAttackInheritance);
            foreach (var attrType in _attributeTypes)
            {
                _attributes.Add(new AttributeData()
                {
                    Type = attrType,
                    Value = atkInheritance,
                    ValueType = ValueType.Percentage,
                });
            }
            
            var parentPos = this.Parent.transform.position;
            
            GamePlayManager.Default.TraverseVillageSoldier((soldierCard) =>
            {
                var pos = soldierCard.transform.position;
                
                // 第一步，从奶妈身上出现珠子
                var mainSequence = DOTween.Sequence();
                mainSequence.AppendCallback(() =>
                {
                    ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                    {
                        PoolName = $"{Config.ProjectileName}01",
                        ParentTran = this.Parent.transform,
                        Position = new Vector3(parentPos.x, parentPos.y + 1f, 0),
                        LocalScale = 150f,
                        DelayReturn = 1.9f
                    });
                });
                mainSequence.AppendInterval(1f);
                
                // 第二步，开始移动珠子飞到目标角色身上
                mainSequence.AppendCallback(() =>
                {
                    var (pool, effectBall) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                    {
                        PoolName = $"{Config.ProjectileName}02",
                        ParentTran = soldierCard.transform,
                        Position = new Vector3(parentPos.x, parentPos.y + 1f, 0),
                        LocalScale = 150f,
                        DelayReturn = 0f
                    });

                    // 抛物线效果
                    effectBall.transform
                        .DOJump(
                            new Vector3(pos.x, pos.y, 0),
                            jumpPower: 2f,   // 抛物线高度（越大越高）
                            numJumps: 1,     // 1 = 单次抛物线
                            duration: 0.8f
                        )
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() => pool.Return(effectBall));
                });
                
                // 第三步，目标角色脚下出现法阵&增加Buff
                mainSequence.OnComplete(() =>
                {
                    // soldierCard.AddBuff(new BuffBase(BuffType.RequiemChant, _duration, EffectCallback));
                });
            });
            
            _onCast = true;
            GameLand.Default.SetSkillDragState(Id, DragState.DragFalse);
        }

        private void EffectCallback(bool isState, Object obj)
        {
            _onCast = false;
            var redouble = 1;
            var soldierCard = (SoldierCard)obj;
            
            // 结束
            if (!isState)
            {
                redouble = 0;
                GameLand.Default.ReleaseSkillCallBack(Id, _cdTime);
            }
            else
            {
                var pos = soldierCard.transform.position;
                ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                {
                    PoolName = $"{Config.ProjectileName}03",
                    ParentTran = soldierCard.transform,
                    Position = new Vector3(pos.x, pos.y - 0.8f, pos.z),
                    LocalScale = 160,
                    DelayReturn = _duration
                });
            }

            AttributeManager.SetAttributesLogic(_modifier, _attributes, false, redouble, soldierCard.cardId);
        }

    }
}