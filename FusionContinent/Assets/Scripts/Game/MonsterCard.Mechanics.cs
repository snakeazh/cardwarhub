using System.Collections.Generic;
using FusionContinent.Buff;
using FusionContinent.Monster.Mechanic;

namespace FusionContinent
{
    public partial class MonsterCard
    {
        private List<IMonsterMechanic> _mechanics = new List<IMonsterMechanic>();

        private void InitializeMechanics()
        {
            CleanupMechanics();
            _mechanics = MonsterMechanicFactory.CreateAll(this, _monsterConfig);
            LogTool.Log(
                "[MonsterMechanic] InitializeMechanics"
                + " MonsterId=" + _cardId
                + " UniqueId=" + uniqueId
                + " MechanismId=" + _monsterConfig.MonsterMechanismId
                + " MechanicCount=" + _mechanics.Count);
            for (int i = 0; i < _mechanics.Count; i++)
            {
                _mechanics[i].OnSpawn();
            }
        }

        private void CleanupMechanics()
        {
            for (int i = 0; i < _mechanics.Count; i++)
            {
                _mechanics[i].OnCleanup();
            }

            _mechanics.Clear();
        }

        internal void NotifyMechanicsTakeDamage(ref DamageContext context)
        {
            for (int i = 0; i < _mechanics.Count; i++)
            {
                _mechanics[i].OnTakeDamage(ref context);
            }
        }

        internal void NotifyMechanicsDealDamage(ref int damage)
        {
            for (int i = 0; i < _mechanics.Count; i++)
            {
                _mechanics[i].OnDealDamage(ref damage);
            }
        }

        internal bool NotifyMechanicsBuffAdd(IBuff buff)
        {
            var context = new BuffAddContext { Buff = buff };
            for (int i = 0; i < _mechanics.Count; i++)
            {
                _mechanics[i].OnBuffAdd(ref context);
                if (context.Cancelled)
                {
                    return false;
                }
            }

            return true;
        }

        internal void NotifyMechanicsHpChanged(float hpPercent)
        {
            for (int i = 0; i < _mechanics.Count; i++)
            {
                _mechanics[i].OnHpChanged(hpPercent);
            }
        }

        internal void NotifyMechanicsDeath()
        {
            for (int i = 0; i < _mechanics.Count; i++)
            {
                _mechanics[i].OnDeath();
            }
        }
    }
}
