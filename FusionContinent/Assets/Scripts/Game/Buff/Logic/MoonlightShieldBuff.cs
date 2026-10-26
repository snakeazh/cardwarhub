using Config;
using FusionContinent.Attribute;
using Manager;
using UnityEngine;
using ValueType = Config.ValueType;

namespace FusionContinent.Buff
{
    /// <summary>
    /// 月影护盾 Buff
    /// 持有护盾层数、护盾特效、破裂特效，受击时层数递减，归零时播放破裂效果并移除属性加成
    /// </summary>
    public class MoonlightShieldBuff : BuffBase
    {
        /// <summary>护盾层数</summary>
        public int ShieldNum { get; private set; }

        /// <summary>护盾存在时的VFX预制体名（ObjectPool 池名）</summary>
        public string ShieldVfx { get; private set; }

        /// <summary>护盾破裂时的VFX预制体名</summary>
        public string BreakVfx { get; private set; }

        /// <summary>每层护盾提供的魔法强度加成（百分比）</summary>
        private float _spellPowerPerLayer;

        /// <summary>当前护盾GameObject（ObjectPool实例）</summary>
        private GameObject _shieldObj;

        /// <summary>护盾ObjectPool引用</summary>
        private QFramework.GenericObjectPool _shieldPool;

        /// <summary>属性修饰器的唯一Key</summary>
        private readonly string _modifierKey;

        /// <summary>护盾是否已破裂（受击归零=破裂；正常消退=不破裂）</summary>
        private bool _broken;

        public MoonlightShieldBuff()
            : base()
        {
            _modifierKey = $"MoonlightShieldBuff_{System.Guid.NewGuid():N}";
            Type = BuffType.MoonlightShield;
            DurationType = BuffDurationType.Permanent;
        }

        /// <summary>
        /// 初始化护盾Buff
        /// </summary>
        public void Init(float duration, int shieldNum, float spellPowerPerLayer,
            string shieldVfx = "EliaSkillMoonshadowWard01",
            string breakVfx = "EliaSkillMoonshadowWard02")
        {
            Duration = duration;
            ShieldNum = shieldNum;
            _spellPowerPerLayer = spellPowerPerLayer;
            ShieldVfx = shieldVfx;
            BreakVfx = breakVfx;
            _broken = false;
        }

        public override void OnAdd(object owner)
        {
            if (owner is not SoldierCard soldier) return;

            if (_shieldObj == null)
            {
                SpawnShieldVfx(soldier);
            }

            SyncSpellPowerModifier(soldier);
        }

        /// <summary>
        /// 护盾受击消耗：仅消耗 1 层；层数归零时才移除属性与特效。
        /// </summary>
        public void OnHitConsume(object owner)
        {
            if (owner is not SoldierCard soldier) return;
            if (ShieldNum <= 0 || _broken) return;

            ShieldNum--;

            if (ShieldNum > 0)
            {
                return;
            }

            ShieldNum = 0;
            _broken = true;
            RemoveSpellPowerModifier(soldier);
            PlayBreakVfxAndReturnPool(soldier);
            soldier.BuffController.RemoveBuff(this);
        }

        public override void OnRemove(object owner)
        {
            if (owner is not SoldierCard soldier) return;

            if (!_broken)
            {
                RemoveSpellPowerModifier(soldier);
                ReturnShieldVfxQuiet();
            }
        }

        public override void OnTick(object owner, float dt)
        {
            if (Duration < 0) return;
            base.OnTick(owner, dt);
        }

        /// <summary>
        /// 累加护盾层数；每层叠加一份魔法强度加成。
        /// </summary>
        public void AddLayers(int layers, float spellPowerPerLayer, object owner = null)
        {
            if (layers <= 0)
            {
                return;
            }

            if (spellPowerPerLayer > 0f)
            {
                _spellPowerPerLayer = spellPowerPerLayer;
            }

            ShieldNum += layers;

            if (_broken && _shieldObj == null && owner is SoldierCard soldier)
            {
                _broken = false;
                SpawnShieldVfx(soldier);
            }

            if (owner is SoldierCard targetSoldier)
            {
                SyncSpellPowerModifier(targetSoldier);
            }
        }

        private void SpawnShieldVfx(SoldierCard soldier)
        {
            if (string.IsNullOrEmpty(ShieldVfx)) return;

            var pos = soldier.transform.position;
            (_shieldPool, _shieldObj) = ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
            {
                PoolName = ShieldVfx,
                ParentTran = soldier.transform,
                Position = new Vector3(pos.x, pos.y, pos.z),
                LocalScale = 160,
                DelayReturn = 0
            });
        }

        private void PlayBreakVfxAndReturnPool(SoldierCard soldier)
        {
            if (_shieldPool == null) return;

            var pos = _shieldObj != null ? _shieldObj.transform.position : Vector3.zero;

            if (_shieldObj != null && !string.IsNullOrEmpty(BreakVfx))
            {
                ObjectPoolManager.Instance.PlayGameObjectPool(new GameObjectPoolData()
                {
                    PoolName = BreakVfx,
                    ParentTran = _shieldObj.transform.parent,
                    Position = pos,
                    LocalScale = 165,
                    DelayReturn = 0.5f
                });
            }

            _shieldPool.Return(_shieldObj);
            _shieldObj = null;
            _shieldPool = null;
        }

        private void ReturnShieldVfxQuiet()
        {
            if (_shieldPool == null || _shieldObj == null) return;
            _shieldPool.Return(_shieldObj);
            _shieldObj = null;
            _shieldPool = null;
        }

        private void SyncSpellPowerModifier(SoldierCard soldier)
        {
            RemoveSpellPowerModifier(soldier);

            if (ShieldNum <= 0 || _spellPowerPerLayer <= 0f)
            {
                return;
            }

            var totalBonus = _spellPowerPerLayer * ShieldNum;
            AttributeManager.SetAttributeLogic(new AttributeModifier
            {
                SourceKey = _modifierKey,
                SourceType = AttributeSourceType.Skill,
            }, new AttributeData
            {
                Type = AttributeType.SoldierSpellPower,
                Value = totalBonus,
                ValueType = ValueType.Percentage,
            }, false, 1, soldier.cardId);
        }

        private void RemoveSpellPowerModifier(SoldierCard soldier)
        {
            if (soldier == null)
            {
                return;
            }

            soldier.BaseAttributes.GetAttr(AttributeType.SoldierSpellPower)
                .RemoveModifier(AttributeSourceType.Skill, _modifierKey);
        }
    }
}
