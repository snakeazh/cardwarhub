using Config;

namespace FusionContinent.Monster.Mechanic
{
    public abstract class MonsterMechanicBase : IMonsterMechanic
    {
        protected MonsterCard Owner { get; private set; }

        protected MonsterMechanicConfig Config { get; private set; }

        protected MonsterMechanicBase(MonsterMechanicConfig config)
        {
            Config = config;
        }

        public void OnInit(MonsterCard owner)
        {
            Owner = owner;
        }

        public virtual void OnSpawn()
        {
        }

        public virtual void OnTakeDamage(ref DamageContext context)
        {
        }

        public virtual void OnDealDamage(ref int damage)
        {
        }

        public virtual void OnBuffAdd(ref BuffAddContext context)
        {
        }

        public virtual void OnHpChanged(float hpPercent)
        {
        }

        public virtual void OnDeath()
        {
        }

        public virtual void OnCleanup()
        {
            Owner = null;
        }
    }
}
