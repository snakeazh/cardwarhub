using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

namespace Game.Player
{
    /// <summary>
    /// 可升星道具（武器 / 皮肤形象 / ...）的数据提供者接口。
    /// 抽象「配置表 + Service + 星级结构」三者的差异，使升星面板与特殊效果面板
    /// 能对武器与皮肤形象等系统通用。
    /// </summary>
    public interface IStarItemDataProvider
    {
        /// <summary>道具名称。</summary>
        string GetName(int configId);

        /// <summary>道具图标名。</summary>
        string GetIcon(int configId);

        /// <summary>是否已激活（拥有）。</summary>
        bool IsActivated(int configId);

        /// <summary>当前星级（未激活为 0）。</summary>
        int GetLevel(int configId);

        /// <summary>是否可升星。</summary>
        bool CanUpgrade(int configId);

        /// <summary>升星材料是否充足。</summary>
        bool CanAffordUpgrade(int configId);

        /// <summary>该道具星级上限（配置中最大 Level）。</summary>
        int GetMaxLevel(int configId);

        /// <summary>星级配置列表（按 Level 升序后的通用 DTO）。</summary>
        List<StarLevelInfo> GetStarLevelList(int configId);

        /// <summary>取指定 Level 的星级配置；未命中则取第一档。</summary>
        StarLevelInfo GetLevelInfo(int configId, int level);

        /// <summary>查询升星/激活所需消耗道具。</summary>
        StarUpgradeConsume GetNextUnlockConsume(int configId);

        /// <summary>查询升星所需消耗（仅已激活且未满星时有效）。</summary>
        StarUpgradeConsume GetUpgradeConsume(int configId);

        /// <summary>将道具图标设置到 Image 上（武器/皮肤图集加载方式不同）。</summary>
        void SetIcon(Image image, int configId, string iconName);

        /// <summary>执行升星请求。</summary>
        UniTask<bool> UpgradeAsync(int configId);

        /// <summary>该道具数据变化事件（configId 为变化的目标）。</summary>
        event Action<int> OnDataChanged;
    }
}
