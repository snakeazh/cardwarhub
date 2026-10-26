using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Helper;
using UIFramework.Services;
using UnityEngine.UI;

namespace Game.Player
{
    /// <summary>
    /// 武器的 IStarItemDataProvider 实现。
    /// 数据源：TbPlayerWeaponConfig + WeaponService。
    /// </summary>
    public class WeaponStarItemDataProvider : IStarItemDataProvider
    {
        private readonly TbPlayerWeaponConfig mTbPlayerWeaponConfig;
        private readonly WeaponService mWeaponService;

        public WeaponStarItemDataProvider()
        {
            mTbPlayerWeaponConfig = DataConfigManager.Tables.TbPlayerWeaponConfig;
            mWeaponService = GameServiceManager.Get<WeaponService>();
        }

        public event Action<int> OnDataChanged
        {
            add
            {
                if (mWeaponService != null) mWeaponService.OnWeaponDataChanged += value;
            }
            remove
            {
                if (mWeaponService != null) mWeaponService.OnWeaponDataChanged -= value;
            }
        }

        private PlayerWeaponConfig GetCfg(int configId)
        {
            return mTbPlayerWeaponConfig?.GetOrDefault(configId);
        }

        public string GetName(int configId)
        {
            return GetCfg(configId)?.Name ?? string.Empty;
        }

        public string GetIcon(int configId)
        {
            return GetCfg(configId)?.Icon ?? string.Empty;
        }

        public bool IsActivated(int configId)
        {
            return mWeaponService != null && mWeaponService.IsActivated(configId);
        }

        public int GetLevel(int configId)
        {
            return mWeaponService != null ? mWeaponService.GetWeaponLevelByConfigId(configId) : 0;
        }

        public bool CanUpgrade(int configId)
        {
            return mWeaponService != null && mWeaponService.CanUpgrade(configId);
        }

        public bool CanAffordUpgrade(int configId)
        {
            return mWeaponService != null && mWeaponService.CanAffordUpgrade(configId);
        }

        public int GetMaxLevel(int configId)
        {
            var list = GetCfg(configId)?.WeaponStarList;
            if (list == null || list.Count == 0) return 0;
            return list.Max(v => v.Level);
        }

        public List<StarLevelInfo> GetStarLevelList(int configId)
        {
            var list = GetCfg(configId)?.WeaponStarList;
            var result = new List<StarLevelInfo>();
            if (list == null) return result;
            foreach (var v in list)
            {
                result.Add(ToDto(v));
            }
            return result;
        }

        public StarLevelInfo GetLevelInfo(int configId, int level)
        {
            var list = GetCfg(configId)?.WeaponStarList;
            if (list == null || list.Count == 0) return null;
            return ToDto(list.FirstOrDefault(v => v.Level == level) ?? list[0]);
        }

        public StarUpgradeConsume GetNextUnlockConsume(int configId)
        {
            if (mWeaponService != null
                && mWeaponService.TryGetNextUnlockConsume(configId, out var itemId, out var needCount)
                && itemId > 0 && needCount > 0)
            {
                return new StarUpgradeConsume { HasConsume = true, ItemId = itemId, NeedCount = needCount };
            }
            return default;
        }

        public StarUpgradeConsume GetUpgradeConsume(int configId)
        {
            if (!IsActivated(configId) || !CanUpgrade(configId))
            {
                return default;
            }

            return GetNextUnlockConsume(configId);
        }

        public void SetIcon(Image image, int configId, string iconName)
        {
            if (image == null || string.IsNullOrEmpty(iconName))
            {
                return;
            }

            image.enabled = true;
            SpriteHelper.TryLoadTextureAsync(iconName, image, false, "UIWeaponTextures");
        }

        public UniTask<bool> UpgradeAsync(int configId)
        {
            if (mWeaponService == null) return UniTask.FromResult(false);
            return mWeaponService.UpgradeAsync(configId);
        }

        private static StarLevelInfo ToDto(WeaponStarListStruct src)
        {
            if (src == null) return null;
            return new StarLevelInfo
            {
                Level = src.Level,
                Desc1 = src.Desc1,
                Desc2 = src.Desc2,
            };
        }
    }
}
