using FusionContinent.Buff;

namespace FusionContinent.Monster.Mechanic
{
    public struct BuffAddContext
    {
        public IBuff Buff;
        public bool Cancelled;
    }

    /// <summary>
    /// 怪物特殊机制生命周期接口。由 MonsterMechanicFactory 按配置创建，MonsterCard 统一转发事件。
    /// </summary>
    public interface IMonsterMechanic
    {
        void OnInit(MonsterCard owner);

        void OnSpawn();

        void OnTakeDamage(ref DamageContext context);

        void OnDealDamage(ref int damage);

        void OnBuffAdd(ref BuffAddContext context);

        void OnHpChanged(float hpPercent);

        void OnDeath();

        void OnCleanup();
    }
}
