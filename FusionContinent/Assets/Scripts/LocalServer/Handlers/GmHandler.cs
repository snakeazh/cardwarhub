using System.Collections.Generic;
using Cardwar;
using Config;

namespace FusionContinent.LocalServer
{
    /// <summary>
    /// GM 道具 Handler，逐行对照 Server/app/api/application/itemapplication/application_item.go。
    /// 单机版直接走 ItemEntrance（事务/行锁不迁移）。
    /// </summary>
    public class GmHandler
    {
        // 对照 application_item.go:14-25 AddItems + http/item.go:37-40（请求 map 转 Items）
        public AddItemsResponse AddItems(LocalServerContext ctx, AddItemsRequest req)
        {
            var items = new Dictionary<int, long>();
            foreach (var kv in req.Items)
            {
                items[kv.Key] = kv.Value;
            }

            ResultItems rsItems;
            try
            {
                rsItems = ctx.ItemEntrance.Execute(items);
            }
            catch (System.Exception e)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 发送道具失败: {e.Message}");
                throw new LocalServerException("发送道具失败");
            }
            return new AddItemsResponse { Items = rsItems };
        }

        // 对照 application_item.go:27-57 AddItemsAll：
        // 卡牌/装备/融合石/英雄皮肤/轮语者皮肤按 EffectValue 各发 1 个；头像/头像框按 EffectValue 各发 1 个；其余道具各发 99 个
        public AddItemsAllResponse AddItemsAll(LocalServerContext ctx, AddItemsAllRequest req)
        {
            var items = new Dictionary<int, long>();
            foreach (ItemConfig v in DataConfigManager.Tables.TbItemConfig.DataList)
            {
                switch (v.ModuleType)
                {
                    case ModuleType.Card:
                    case ModuleType.Equip:
                    case ModuleType.FusionStone:
                    case ModuleType.HeroSkin:
                    case ModuleType.PlayerSkin:
                        if (v.EffectValue != 0)
                        {
                            items[v.Id] = 1;
                        }
                        break;
                    default:
                        if (v.Type == ItemType.PlayerAvatar || v.Type == ItemType.PlayerAvatarFrame)
                        {
                            if (v.EffectValue != 0)
                            {
                                items[v.Id] = 1;
                            }
                        }
                        else
                        {
                            items[v.Id] = 99;
                        }
                        break;
                }
            }

            ResultItems rsItems;
            try
            {
                rsItems = ctx.ItemEntrance.Execute(items);
            }
            catch (System.Exception e)
            {
                LogTool.LogWarning($"userId={ctx.UserId} 发送道具失败: {e.Message}");
                throw new LocalServerException("发送道具失败");
            }
            return new AddItemsAllResponse { Items = rsItems };
        }
    }
}
