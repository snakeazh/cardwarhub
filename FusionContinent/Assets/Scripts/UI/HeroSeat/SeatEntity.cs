using Assets.Scripts.Localization;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Helper;
using FusionContinent.Utils;
using Game.HeroSeat;
using TMPro;
using UIFramework;
using UIFramework.Services;
using UnityEngine;
using UnityEngine.UI;

public class SeatEntityData
{
    public int SeatSkillId;
    public bool IsSelect;
    public bool ShowFrame;
    public bool EnableOpenUpgradePanelOnClick = true;
    public bool CheckUnlock;
}

public class SeatEntity : MonoBehaviour
{
    public TextMeshProUGUI LevelText;
    public RewardEntity rewardEntity;
    public Slider ProgressBar;
    public GameObject SelectNode;
    public TextMeshProUGUI ProgressText;
    public GameObject Mask;
    public void SetData(SeatEntityData seatEntityData)
    {
        SelectNode.SetActive(seatEntityData.IsSelect);

        var config = DataConfigManager.Tables.TbHeroSeatConfig.GetOrDefault(seatEntityData.SeatSkillId);
        if (config == null)
        {
            LogTool.LogError($"[SeatEntity] 城墙技能配置不存在 SkillId[{seatEntityData.SeatSkillId}]");
            return;
        }

        ShowSkillReward(config, seatEntityData.SeatSkillId, seatEntityData.EnableOpenUpgradePanelOnClick);

        var seatService = GameServiceManager.Get<SeatService>();
        if (seatService.IsSkillActivated(seatEntityData.SeatSkillId))
        {
            ShowActivatedProgress(seatService, seatEntityData.SeatSkillId);
        }
        else
        {
            ShowNotActivatedProgress(config);
        }

        if (!seatEntityData.ShowFrame)
        {
            HideProgress();
        }

        RefreshUnlockMask(seatService, seatEntityData);
        RefreshLevelDisplay(seatService, seatEntityData.SeatSkillId);
    }

    private void RefreshLevelDisplay(SeatService seatService, int skillId)
    {
        if (LevelText == null)
        {
            return;
        }

        if (!seatService.IsSkillActivated(skillId))
        {
            LevelText.gameObject.SetActive(false);
            return;
        }

        var level = seatService.GetSkill(skillId)?.Level ?? 0;
        LevelText.gameObject.SetActive(level > 0);
        LevelText.text = $"{level}级";
    }

    private void RefreshUnlockMask(SeatService seatService, SeatEntityData seatEntityData)
    {
        if (Mask == null)
        {
            return;
        }

        var showMask = seatEntityData.CheckUnlock && !seatService.IsSkillActivated(seatEntityData.SeatSkillId);
        Mask.SetActive(showMask);
    }

    private void ShowSkillReward(HeroSeatConfig config, int skillId, bool enableOpenUpgradePanelOnClick)
    {
        rewardEntity.gameObject.SetActive(true);
        rewardEntity.SetData(new RewardEntityData
        {
            Quality = config.Quality,
            IconSprite = Global.ItemIconSpriteAtlas.GetSpriteExt(config.Icon),
            StyleType = RewardStyleType.Equip,
            Status = RewardStatusType.Default,
            OnClick = enableOpenUpgradePanelOnClick ? () => OnSkillRewardClicked(skillId) : null,
        });
    }

    private static void OnSkillRewardClicked(int skillId)
    {
        HandleSkillRewardClickAsync(skillId).Forget();
    }

    private static async UniTask HandleSkillRewardClickAsync(int skillId)
    {
        if (skillId <= 0)
        {
            return;
        }

        var seatService = GameServiceManager.Get<SeatService>();
        if (!seatService.IsSkillActivated(skillId))
        {
            if (!seatService.TryGetSkillActivateConsume(skillId, out var itemId, out var needCount))
            {
                OpenMechanicUpgradePanel(skillId);
                return;
            }

            if (Global.GetItemNumById(itemId) < needCount)
            {
                OpenMechanicUpgradePanel(skillId);
                return;
            }

            await seatService.ActivateSkillAsync(skillId);
            return;
        }

        OpenMechanicUpgradePanel(skillId);
    }

    private static void OpenMechanicUpgradePanel(int skillId)
    {
        if (skillId <= 0)
        {
            return;
        }

        var vm = new MechanicUpgradePanelViewModel();
        vm.OnCreate();
        vm.Setup(skillId);
        UIManager.OpenPanelAsync<MechanicUpgradePanelNew, MechanicUpgradePanelViewModel>(
            vm, UILevel.PopUI).Forget();
    }

    private void ShowNotActivatedProgress(HeroSeatConfig config)
    {
        if (config.NeedItems == null || config.NeedItems.Count == 0)
        {
            HideProgress();
            return;
        }

        ShowProgress(config.NeedItems[0]);
    }

    private void ShowActivatedProgress(SeatService seatService, int skillId)
    {
        var levelCfg = seatService.GetSkillLevelCfg(skillId);
        if (levelCfg == null || levelCfg.LevelUpItems == null || levelCfg.LevelUpItems.Count == 0)
        {
            HideProgress();
            return;
        }

        ShowProgress(levelCfg.LevelUpItems[0]);
    }

    private void ShowProgress(RewardAndNum consumeItem)
    {
        ProgressBar.gameObject.SetActive(true);
        ProgressBar.value = CalcItemProgress(consumeItem);

        ProgressText.gameObject.SetActive(true);
        var owned = Global.GetItemNumById(consumeItem.Id);
        ProgressText.text = $"{NumberTool.FormatNumber(owned)}/{NumberTool.FormatNumber(consumeItem.Num)}";
    }

    private void HideProgress()
    {
        ProgressBar.gameObject.SetActive(false);
        ProgressText.gameObject.SetActive(false);
    }

    private static float CalcItemProgress(RewardAndNum item)
    {
        if (item.Num <= 0)
        {
            return 0f;
        }

        var owned = Global.GetItemNumById(item.Id);
        return Mathf.Clamp01((float)owned / item.Num);
    }
}
