using System;
using FusionContinent;
using FusionContinent.Model;

namespace Game.Equip.RedDot
{
    /// <summary>
    /// 装备红点刷新事件：道具数量变更 + 资源（体力等）变更。
    /// </summary>
    public static class EquipRedDotRefreshEvents
    {
        public static void Subscribe(Action<object> handler)
        {
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_ITEM_CHANGE, handler);
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_STAMINA, handler);
        }

        public static void Unsubscribe(Action<object> handler)
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_ITEM_CHANGE, handler);
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_STAMINA, handler);
        }
    }
}
