using System;
using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Attribute;
using FusionContinent.Helper;
using FusionContinent.Model;
using Manager;
using QFramework;
using UnityEngine;
using Object = UnityEngine.Object;
using UIManager = UIFramework.UIManager;
using Game.UI;
using Game.Common;
using Game.Equip;
using Game.UserTool;
using UIFramework.Services;
using FusionContinent.Utils;
using TMPro;
using RewardItem = FusionContinent.Model.RewardItem;

public static class GlobalFunc
{

    public static int GetCommonTeamId()
    {
        return Talent.GetUseTalent().TalentId;
    }

    public static int GetHeroSlotNumByUserLevel()
    {
        return DataConfigManager.Tables.TbPlayerLevelConfig.Get(Global.User.Level).HeroSlotNum;
    }

    public static int GetSkillSlotNumByUserLevel()
    {
        return DataConfigManager.Tables.TbPlayerLevelConfig.Get(Global.User.Level).SkillSlotNum;
    }

    public static int GetArcaneSlotNumByUserLevel()
    {
        return DataConfigManager.Tables.TbPlayerLevelConfig.Get(Global.User.Level).ArcaneSlotNum;
    }

    public static int GetArcCardSlotCount()
    {
        return DataConfigManager.Tables.TbPlayerConst.ArcCardUnlockLevel.Count;
    }

    public static int GetArcCardUnlockLevel(int slotIndex)
    {
        return DataConfigManager.Tables.TbPlayerConst.ArcCardUnlockLevel[slotIndex];
    }

    public static bool IsArcCardSlotUnlocked(int slotIndex)
    {
        var unlockLevels = DataConfigManager.Tables.TbPlayerConst.ArcCardUnlockLevel;
        if (slotIndex < 0 || slotIndex >= unlockLevels.Count)
        {
            return false;
        }

        return Global.User.Level >= unlockLevels[slotIndex];
    }

    public static int GetUnlockedArcCardSlotCount()
    {
        int count = 0;
        var unlockLevels = DataConfigManager.Tables.TbPlayerConst.ArcCardUnlockLevel;
        for (int i = 0; i < unlockLevels.Count; i++)
        {
            if (Global.User.Level >= unlockLevels[i])
            {
                count++;
            }
        }

        return count;
    }
    public static string GetQualityName(QualityType qualityType)
    {
        switch (qualityType)
        {
            case QualityType.Default:
                return "UI_QUALITY_DEFAULT";
            case QualityType.Ordinary:
                return "UI_QUALITY_ORDINARY";
            case QualityType.Rarity:
                return "UI_QUALITY_RARE";
            case QualityType.Epic:
                return "UI_QUALITY_EPIC";
            case QualityType.Legend:
                return "UI_QUALITY_LEGEND";
            case QualityType.Ultimate:
                return "UI_QUALITY_ULTIMATE";
            case QualityType.Mythology:
                return "UI_QUALITY_MYTHOLOGY";
            default:
                throw new ArgumentOutOfRangeException(nameof(qualityType), qualityType, null);
        }
    }

    #region 品质样式

    private static Dictionary<QualityType, Color32> sQualityColors;

    /// <summary>
    /// 根据品质获取展示样式，颜色读取 TbGameBattleConst.QualityTypeColors 配置（懒加载缓存）。
    /// 未配置的品质返回白色，与既有 GetQualityColor 各处实现行为一致。
    /// </summary>
    public static QualityStyle GetQualityStyle(QualityType qualityType)
    {
        EnsureQualityColorsLoaded();
        return new QualityStyle
        {
            MainColor = sQualityColors.TryGetValue(qualityType, out var color) ? color : (Color32)Color.white
        };
    }

    private static void EnsureQualityColorsLoaded()
    {
        if (sQualityColors != null) return;

        sQualityColors = new Dictionary<QualityType, Color32>();
        foreach (var v in DataConfigManager.Tables.TbGameBattleConst.QualityTypeColors)
        {
            sQualityColors[v.Type] = new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f);
        }
    }

    #endregion
    /// <summary>
    /// 判断融合石是否已穿戴。
    /// WearerId 表示穿戴者装备唯一Id，0 表示未穿戴。
    /// </summary>
    public static bool IsFusionGemEquipped(int gemOnlyId)
    {
        if (gemOnlyId <= 0) return false;
        return Global.PlayerFusionGems != null
               && Global.PlayerFusionGems.TryGetValue(gemOnlyId, out var gem)
               && gem != null
               && gem.WearerId > 0;
    }

    public static bool IsFusionGemEquipped(PlayerFusionGem gem)
    {
        if (gem == null) return false;
        return IsFusionGemEquipped(gem.Id);
    }

    public static void FormatAttrNameValue(Config.AttributeType attrType,AttributeSet attributeSet, CareerType cardType,out string attrName, out string attrValue)
    {
        var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attrType);
        if (attrCfg == null)
        {
            attrName = string.Empty;
            attrValue = string.Empty;
            return;
        }
        attrName = LocalizationManager.Instance.GetText(LocalizationModule.UITexts, attrCfg.Name);
        attrName = attrName[..^3];
        var value = attributeSet.GetFinal(attrType);

        if (attrType == AttributeType.SoldierAttackSpeed)
        {
            // var soldierCfg = VM.GetSoldierConfig(this.VM.CurrentCard.Value.CardId);
            float attackTime = 1;
            for (int i = 0; i < DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType.Count; i++)
            {
                if (DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType[i].Type == cardType)
                {
                    attackTime = DataConfigManager.Tables.TbGameBattleConst.AttackSpeedGapOfCarType[i].Count;
                }
            }
            var nextAttackTime = Formula.AttackInterval(attributeSet.GetFinal(Config.AttributeType.SoldierNumOfAttackSpeed),  DataConfigManager.Tables.TbGameBattleConst.AttackSpeedBase,
                DataConfigManager.Tables.TbGameBattleConst.AfkTimeLimitMax);
            nextAttackTime = nextAttackTime / attackTime;
            attrValue = nextAttackTime.ToString("F0");
        }
        else
        {
            attrValue = value.ToString("F0");
        }
            
    }
    public static void PreparePanel(int stageId)
    {
        var vm = new Game.CardGroup.CardGroupPanelViewModel();
        vm.OnCreate();
        vm.SetData(stageId);
        UIFramework.UIManager.OpenPanelAsync<CardGroupPanelNew, Game.CardGroup.CardGroupPanelViewModel>(vm, UIFramework.UILevel.Panel).Forget();
    }

    public static async UniTask StartBattle(int stageId,Action successCallback, Action<string> errorCallback = null, Action finallyCallback = null)
    {
        // 验证体力
        var stageCfg = DataConfigManager.Tables.TbStageBattleConfig.DataList.Find(ele=>ele.Id == stageId);
        if (!Global.PlayerResource.CheckStamina(stageCfg.ConsumeStamina))
        {
            FusionContinent.Toast.Show("体力不足，请稍后重试");
            return;
        }
			
			

        if (GameServiceManager.GetOrCreate<Game.Team.TeamService>().BuildBattleTeamsForCurrentTalent())
        {
            FusionContinent.Toast.Show("请配置卡组");
            return;
        }
        Global.StageId.Value = stageId;
        BattleModel.Start(() =>
        {
            successCallback?.Invoke();
            LogTool.Log($"战斗开始");
            if (GameObject.Find("UIRoot/CanvasUpList") != null)
            {
                Object.Destroy(GameObject.Find("UIRoot/CanvasUpList"));
            }
            GuideFlow.Instance?.ClearFinger();
            // SceneManager.LoadScene("GamePlay", LoadSceneMode.Single);
            Global.ResetProperties();
            Global.Difficulty.Value = 1;
            Global.GameReviveCount.Value = 0; // 重置游戏复活次数
  
            SceneLoader.Load("GamePlay");
            
            // 完成引导
            //GuideFlow.Instance.SetStepComplete(GuideType.StartBattleStage);
        }, (err) =>
        {
            errorCallback?.Invoke(err);
            Toast.Error(err);
        }, () =>
        {
            finallyCallback?.Invoke();
            // PlayGameBtn.interactable = true; // 恢复按钮
        });
    }
    public static void OpenChooseNum(int maxValue,Action<int> cnConfirmed,Action onCancel = null)
    {
        var vm = new ChooseCardNumPanelViewModel();
        vm.OnCreate();
        vm.Initialize(maxValue, initialSliderValue: 1f); // 0~1，1f 表示默认选满
        vm.OnConfirmed = cnConfirmed;
        vm.OnCancelled = onCancel;
        UIManager.OpenPanelAsync<FusionContinent.ChooseCardNumPanelNew, ChooseCardNumPanelViewModel>(
            vm,
            UIFramework.UILevel.PopUI
        );
    }

    /// <summary>
    /// 打开通用详情小弹窗，定位到点击节点下方。
    /// </summary>
    public static void OpenCommonDetailPopPanel(Transform anchor, string title, string content)
    {
        var vm = new CommonDetailPopPanelViewModel();
        vm.OnCreate();
        vm.Setup(title, content);
        UIManager.OpenPanelAsync<CommonDetailPopPanelNew, CommonDetailPopPanelViewModel>(
            vm,
            UIFramework.UILevel.PopUI,
            null,
            panel => panel.PlaceBelow(anchor)
        ).Forget();
    }

    public static void OpenEquipDetailsPanel(int equipOnlyId, int soldierId,
        UIFramework.UILevel level = UIFramework.UILevel.PopUI,
        Action<EquipDetailsPanel1New> onPanelCreated = null)
    {
        Debug.Log(equipOnlyId);
        var vm = new EquipDetailsPanel1ViewModel();
        vm.OnCreate();
        vm.Initialize(equipOnlyId, soldierId);
        UIManager.OpenPanelAsync<EquipDetailsPanel1New, EquipDetailsPanel1ViewModel>(
            vm,
            level,
            null,
            panel => onPanelCreated?.Invoke(panel)
        ).Forget();
    }

    #region 奖励提示公用方法

    /// <summary>
    /// 显示奖励提示（List<RewardItem>版本）
    /// </summary>
    public static async UniTask ShowRewardTips(List<RewardItem> rewardItems, string title = "恭喜获得", string tipText = "")
    {
        if (rewardItems == null || rewardItems.Count == 0)
        {
            LogTool.LogWarning("[GlobalFunc] 奖励列表为空，不显示提示");
            return;
        }

        var service = GameServiceManager.GetOrCreate<RewardTipsService>();
        var data = new RewardTipsViewData
        {
            Title = title,
            TipText = tipText
        };
        await service.ShowRewardTips(rewardItems, data);
    }

    /// <summary>
    /// 显示奖励提示（List<RewardAndNum>版本）
    /// </summary>
    public static async UniTask ShowRewardTips(List<RewardAndNum> rewardItems, string title = "恭喜获得", string tipText = "")
    {
        if (rewardItems == null || rewardItems.Count == 0)
        {
            LogTool.LogWarning("[GlobalFunc] 奖励列表为空，不显示提示");
            return;
        }

        var service = GameServiceManager.GetOrCreate<RewardTipsService>();
        var data = new RewardTipsViewData
        {
            Title = title,
            TipText = tipText
        };
        await service.ShowRewardTips(rewardItems, data);
    }

    /// <summary>
    /// 显示奖励提示（Dictionary<string, int>版本）
    /// </summary>
    public static async UniTask ShowRewardTips(Dictionary<string, int> rewardItems, string title = "恭喜获得", string tipText = "")
    {
        if (rewardItems == null || rewardItems.Count == 0)
        {
            LogTool.LogWarning("[GlobalFunc] 奖励列表为空，不显示提示");
            return;
        }

        var service = GameServiceManager.GetOrCreate<RewardTipsService>();
        var data = new RewardTipsViewData
        {
            Title = title,
            TipText = tipText
        };
        await service.ShowRewardTips(rewardItems, data);
    }
    /// <summary>
    /// 显示奖励提示（Dictionary<string, int>版本）
    /// </summary>
    public static async UniTask ShowRewardTips(Dictionary<int, long> rewardItems, string title = "恭喜获得", string tipText = "")
    {
        if (rewardItems == null || rewardItems.Count == 0)
        {
            LogTool.LogWarning("[GlobalFunc] 奖励列表为空，不显示提示");
            return;
        }

        var service = GameServiceManager.GetOrCreate<RewardTipsService>();
        var data = new RewardTipsViewData
        {
            Title = title,
            TipText = tipText
        };
        await service.ShowRewardTips(rewardItems, data);
    }
    /// <summary>
    /// 显示奖励提示（简化版，只传奖励列表）
    /// </summary>
    public static async UniTask ShowRewardTips(List<RewardItem> rewardItems)
    {
        await ShowRewardTips(rewardItems, "恭喜获得", "");
    }

    /// <summary>
    /// 关闭奖励提示
    /// </summary>
    public static void CloseRewardTips()
    {
        var service = GameServiceManager.GetOrCreate<RewardTipsService>();
        service.CloseRewardTips();
    }

    #endregion

    /// <summary>
    /// 同步加载材质球
    /// </summary>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public static Material LoadMaterial(string assetName)
    {
       return Global.ResLoader.LoadSync<Material>("Fonts_"+assetName);
    }

    public static List<DisplayAttributeRow> GetAttributeRows(System.Collections.Generic.List<AttributeStruct> attributeStructs)
    {
        var rows = new List<DisplayAttributeRow>();

        foreach (var attr in attributeStructs)
        {
            var attrCfg = DataConfigManager.Tables.TbAttributeConfig.GetOrDefault(attr.Type);
            if (attrCfg == null) continue;
            var symbol = attr.Value > 0 ? "+" : "";
            rows.Add(new DisplayAttributeRow
            {
                NameKey = attrCfg.Name,
                ValueText = $"{symbol}{attr.Value}"
            });
        }

        return rows;
    }
    public static List<DisplayAttributeRow> GetMainAttributeRows(System.Collections.Generic.List<MainAttributeStruct> mainAttributeStructs)
    {
        var rows = new List<DisplayAttributeRow>();

        foreach (var attr in mainAttributeStructs)
        {
            var attrCfg = DataConfigManager.Tables.TbMainAttributeConfig.GetOrDefault(attr.Type);
            if (attrCfg == null) continue;
            var symbol = attr.Value > 0 ? "+" : "";
            rows.Add(new DisplayAttributeRow
            {
                NameKey = attrCfg.Name,
                ValueText = $"{symbol}{attr.Value}"
            });
        }

        return rows;
    }



    public static UserToolData GetUserResourceConfig(PageType pageType)
    {
        UserToolData toolData = null;
        foreach (var item in DataConfigManager.Tables.TbResourceShowConfig.DataList)
        {
            if (item.PageType == pageType)
            {
                toolData = new UserToolData();
                toolData.ItemIds = item.Resource;
                toolData.ShowAvatar = item.IsNeedAvatar == 1;
                toolData.Direction = item.PositionType;
                break;
            }
        }

        return toolData;
    }

    public static string FormatNumber(long num)
    {
        return NumberTool.FormatNumber(num);
    }

    public static int GetNextStageId(int stageId = 0,int addChapter = 0)
    {
        int currentStageId = Global.StageId.Value;
        if (stageId != 0)
        {
            currentStageId = stageId;
        }

        var currentConfig = DataConfigManager.Tables.TbStageBattleConfig.Get(currentStageId);
        foreach (var stageBattleConfig in DataConfigManager.Tables.TbStageBattleConfig.DataList)
        {
            if (stageBattleConfig.ChapterId == currentConfig.ChapterId+addChapter)
            {
                currentStageId =  stageBattleConfig.Id;
                break;
            }
        }

        return currentStageId;
    }
    
    public static void SetTMPVerticalGradient(TextMeshProUGUI tmpText, List<int> topRGB, List<int> bottomRGB)
    {
        Utils.TextMeshProTools.SetTMPVerticalGradient(tmpText, topRGB, bottomRGB);
    }

}