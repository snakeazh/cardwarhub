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
    /// 皮肤形象（PlayerFigureConfig）的 IStarItemDataProvider 实现。
    /// 数据源：TbPlayerFigureConfig + SpeakerService。
    /// </summary>
    public class FigureStarItemDataProvider : IStarItemDataProvider
    {
        private readonly TbPlayerFigureConfig mTbPlayerFigureConfig;
        private readonly SpeakerService mSpeakerService;

        public FigureStarItemDataProvider()
        {
            mTbPlayerFigureConfig = DataConfigManager.Tables.TbPlayerFigureConfig;
            mSpeakerService = GameServiceManager.Get<SpeakerService>();
        }

        public event Action<int> OnDataChanged
        {
            add
            {
                if (mSpeakerService != null) mSpeakerService.OnSkinDataChanged += value;
            }
            remove
            {
                if (mSpeakerService != null) mSpeakerService.OnSkinDataChanged -= value;
            }
        }

        private PlayerFigureConfig GetCfg(int configId)
        {
            return mTbPlayerFigureConfig?.GetOrDefault(configId);
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
            return mSpeakerService != null && mSpeakerService.IsActivated(configId);
        }

        public int GetLevel(int configId)
        {
            return mSpeakerService != null ? mSpeakerService.GetSkinLevelByConfigId(configId) : 0;
        }

        public bool CanUpgrade(int configId)
        {
            return mSpeakerService != null && mSpeakerService.CanUpgrade(configId);
        }

        public bool CanAffordUpgrade(int configId)
        {
            return mSpeakerService != null && mSpeakerService.CanAffordUpgrade(configId);
        }

        public int GetMaxLevel(int configId)
        {
            var list = GetCfg(configId)?.ImageStarList;
            if (list == null || list.Count == 0) return 0;
            return list.Max(v => v.Level);
        }

        public List<StarLevelInfo> GetStarLevelList(int configId)
        {
            var list = GetCfg(configId)?.ImageStarList;
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
            var list = GetCfg(configId)?.ImageStarList;
            if (list == null || list.Count == 0) return null;
            return ToDto(list.FirstOrDefault(v => v.Level == level) ?? list[0]);
        }

        public StarUpgradeConsume GetNextUnlockConsume(int configId)
        {
            if (mSpeakerService != null
                && mSpeakerService.TryGetNextUnlockConsume(configId, out var itemId, out var needCount)
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
            SpriteHelper.TryLoadPaintAsync(iconName, image, groupName: "Speaker");
        }

        public UniTask<bool> UpgradeAsync(int configId)
        {
            if (mSpeakerService == null) return UniTask.FromResult(false);
            return mSpeakerService.UpgradeAsync(configId);
        }

        private static StarLevelInfo ToDto(ImageStarListStruct src)
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
