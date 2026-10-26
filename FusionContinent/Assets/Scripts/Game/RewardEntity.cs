using System;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent;
using FusionContinent.Helper;
using FusionContinent.Model;
using FusionContinent.UI.Components;
using FusionContinent.Utils;
using QFramework;
using UnityEngine;
using TMPro;
using UnityEngine.U2D;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;
using UILevel = UIFramework.UILevel;
using UIManager = UIFramework.UIManager;

public class RewardEntity : MonoBehaviour
{
    public UIPointerClickAndLongPress rewardBtn;
    public Image bg;
    public Image bgFrame;
    public Image icon;
    public Image frame;
    public Image frameOut;
    public Image frameCheck;
    public Image maskImg;
    public Image lockImg;
    public TMP_Text num;
    public TMP_Text consumeNum;
    public Transform effect;
    public Transform equipIsHave;
    public bool isEnough = false;
    private RewardEntityData _rewardEntityData;

    public GameObject LevelNode;
    public TMP_Text levelText;
    
    private Dictionary<QualityType, QualityTypeEffectstruct> _qualityTypeEffects = new Dictionary<QualityType, QualityTypeEffectstruct>();
    Dictionary<string,GameObject> cacheEffects = new Dictionary<string, GameObject>();
    private void Awake()
    {
        var qualityTypeEffects = DataConfigManager.Tables.TbGameBattleConst.QualityTypeEffects;
        foreach (var v in qualityTypeEffects)
        {
            _qualityTypeEffects.Add(v.Type, v);
        }
    }

    void Start()
    {
        if (transform.localScale.x > 100f) transform.localScale = Vector3.one;
        if (rewardBtn != null)
        {
            rewardBtn.onClick.AddListener(() =>
            {
                if (_rewardEntityData == null)
                {
                    return;
                }
                // UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                if (_rewardEntityData.OnClick != null)
                {
                    _rewardEntityData.OnClick();
                    return;
                }

                ItemShowPanelViewModel vm = new ItemShowPanelViewModel();
                vm.OnCreate();
                vm.Data = _rewardEntityData;
                UIManager.OpenPanelAsync<ItemShowPanelNew, ItemShowPanelViewModel>(vm, UILevel.PopUI).Forget();
            });
        }
    }


    private void OnDisable()
    {
    }

    public bool SetData(RewardEntityData data)
    {
        if (data.Quality == QualityType.Default) data.Quality = QualityType.Ordinary;
        _rewardEntityData = data;
        
        // bg.color = Global.QualityTypeFrameColors[data.Quality];
        // bgFrame.color = Global.QualityTypeFrameOutColors[data.Quality];
        icon.sprite = data.IconSprite != null ? data.IconSprite : Global.ItemIconSpriteAtlas.GetSpriteExt(data.Icon);
        if (frame) frame.sprite = Global.ItemQualityFrameAtlas.GetSprite($"{data.Quality}");
        
        // 边框
        // if (frame != null) frame.color = Global.QualityTypeFrameColors[data.Quality];
        // if (frameOut != null) frameOut.color = Global.QualityTypeFrameOutColors[data.Quality];
        if (frameCheck) frameCheck.gameObject.SetActive(data.Check);

        // 特效效果
        SetEffect(data);
        RefreshEquipLevelBadge(data);
        // 道具数量
        num.gameObject.SetActive(data.StyleType == RewardStyleType.Default);
        consumeNum.gameObject.SetActive(data.StyleType is RewardStyleType.Consume or RewardStyleType.ConsumeBrief
            or RewardStyleType.ConsumeBriefWhiteColor);

        // 状态
        if (maskImg != null) maskImg.gameObject.SetActive(data.Status == RewardStatusType.Received); // 已领取
        // if (maskImg != null) maskImg.transform.Find("Received").gameObject.SetActive(data.Status == RewardStatusType.Received); // 已领取
        if (lockImg != null) lockImg.gameObject.SetActive(data.Status == RewardStatusType.Lock); // 锁定

        // 消耗
        isEnough = true;
        if (consumeNum.gameObject.activeSelf)
        {
            var itemNum = Global.GetItemNumById(data.ItemId);
            consumeNum.color = new Color(0.533f, 1f, 0.49f, 1f); // 数量足够时显示绿色 #88FF7D
            if (data.Num > itemNum)
            {
                isEnough = false;
                consumeNum.color = data.StyleType == RewardStyleType.ConsumeBriefWhiteColor ? new Color(255, 114, 114, 255) : Color.red;
            }

            if (data.StyleType is RewardStyleType.ConsumeBrief or RewardStyleType.ConsumeBriefWhiteColor)
            {
                consumeNum.text = NumberTool.FormatNumber(data.Num);
            }
            else
            {
                // 「拥有/所需」格式：拥有数量单独上色（充足=#488719 深绿，不足=红色），所需数量用 #705148
                ColorUtility.TryParseHtmlString("#705148", out var requiredColor);
                consumeNum.color = requiredColor;
                var ownColor = isEnough ? "#488719" : "#FF7272";
                consumeNum.text = $"<color={ownColor}>{NumberTool.FormatNumber(itemNum)}</color>/{NumberTool.FormatNumber(data.Num)}";
            }
            // consumeNum.GetComponent<LayoutGroup>()?();
            if (transform.GetComponent<LayoutGroup>())
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.GetComponent<RectTransform>());
            }
            

        }
        else
        {
            num.text = $"{NumberTool.FormatNumber(data.Num)}";
        }
        
        if (equipIsHave != null)
            equipIsHave.gameObject.SetActive(data.EquipIsWear);

        return isEnough;
    }

    /// <summary>
    /// 装备道具显示 LevelBg/Text，等级跟随 EquipConfig.EquipLevel（或显式传入的 EquipLevel）。
    /// </summary>
    private void RefreshEquipLevelBadge(RewardEntityData data)
    {
        var showLevel = false;
        var displayLevel = 0;

        if (data.EquipLevel > 0)
        {
            showLevel = true;
            displayLevel = data.EquipLevel;
        }
        else if (data.ItemId > 0)
        {
            var itemConfig = DataConfigManager.Tables.TbItemConfig.Get(data.ItemId);
            if (itemConfig.Type == ItemType.SoldierEquip)
            {
                var equipCfg = DataConfigManager.Tables.TbEquipConfig.GetOrDefault(itemConfig.EffectValue);
                if (equipCfg == null)
                {
                    LogTool.LogError("[RewardEntity] 装备配置缺失，itemId=" + data.ItemId + ", equipId=" + itemConfig.EffectValue);
                }
                else
                {
                    showLevel = true;
                    displayLevel = equipCfg.EquipLevel;
                }
            }
        }

        LevelNode.SetActive(showLevel);
        if (showLevel)
        {
            levelText.text = $"{displayLevel}级";
        }
    }

    private static float effectRate = 120f;

    private GameObject persistentEffectObj;
    private GameObject startEffectObj;
    // 特效效果
    private void SetEffect(RewardEntityData data)
    {
        if(effect==null) return;
        // if (effectPoolStart != null) effectPoolStart.ReturnAll();
        // if (effectPoolLoop != null) effectPoolLoop.ReturnAll();
        // 特效
        startEffectObj?.SetActive(false);
        if (data.IsEffect && data.Status == RewardStatusType.Default && _qualityTypeEffects.ContainsKey(data.Quality))
        {
            var qtEffectCfg = _qualityTypeEffects[data.Quality];
            if (qtEffectCfg != null && !string.IsNullOrEmpty(qtEffectCfg.EffectName))
            {
                if (effect == null || !effect.gameObject.activeInHierarchy) return;
                var pool = CommonItemPoolManager.Instance.GetGameObjectPool(qtEffectCfg.EffectName);
                if (pool!= null)
                {

                    if (cacheEffects.ContainsKey(qtEffectCfg.EffectName))
                    {
                        startEffectObj = cacheEffects[qtEffectCfg.EffectName];
                    }
                    else
                    {
                        startEffectObj =  pool.Get();
                        cacheEffects.Add(qtEffectCfg.EffectName, startEffectObj);
                    }
                    startEffectObj.GetOrAddComponent<ParticleClipParent>();

                        
                    if (startEffectObj != null)
                    {
                        startEffectObj.SetActive(true);
                        // ✅ 继承局部坐标，无需再设 position
                        startEffectObj.transform.SetParent(effect, false);

                        // 如有偏移再设局部位置
                        startEffectObj.transform.localPosition = Vector3.zero;
                        startEffectObj.transform.localScale = Vector3.one * effectRate;
                    }
                }
            }
        }
        persistentEffectObj?.SetActive(false);
        if ( _qualityTypeEffects.ContainsKey(data.Quality))
        {
            var qtEffectCfg = _qualityTypeEffects[data.Quality];
            
            // 常驻效果
            if (qtEffectCfg != null && !string.IsNullOrEmpty(qtEffectCfg.EffectName2))
            {
                // 如果已有，就回收旧特效
                //  if (persistentEffectObj != null)
                //  {
                //      effectPoolLoop?.Return(persistentEffectObj);
                //      persistentEffectObj = null;
                //  }
                var pool = CommonItemPoolManager.Instance.GetGameObjectPool(qtEffectCfg.EffectName2);
                if (pool!= null)
                {
                    if (cacheEffects.ContainsKey(qtEffectCfg.EffectName2))
                    {
                        persistentEffectObj = cacheEffects[qtEffectCfg.EffectName2];
                    }
                    else
                    {
                        persistentEffectObj =  pool.Get();
                        cacheEffects.Add(qtEffectCfg.EffectName2, persistentEffectObj);
                    }

                    persistentEffectObj.GetOrAddComponent<ParticleClipParent>();
                    persistentEffectObj.transform.SetParent(effect, false);
                    persistentEffectObj.SetActive(true);
                    persistentEffectObj.transform.localScale = Vector3.one * effectRate;
                    persistentEffectObj.transform.localPosition = Vector3.zero;
                }
            }
        }
    }

    public void ReturnEffect(QualityType qualityType)
    {
        if (effect == null || !_qualityTypeEffects.ContainsKey(qualityType))
            return;

        var targetEffect = _qualityTypeEffects[qualityType]; // 缓存当前 qualityType 的效果

        for (int i = 0; i < effect.childCount; i++)
        {
            Transform child = effect.GetChild(i);
            if (child == null) continue;

            string childBaseName = child.name.Split('(')[0]; // 只计算一次

            // 如果 childBaseName 既不等于 EffectName，也不等于 EffectName2，则回收
            if (childBaseName != targetEffect.EffectName &&
                childBaseName != targetEffect.EffectName2)
            {
                var pool = CommonItemPoolManager.Instance.GetGameObjectPool(childBaseName);
                pool?.Return(child.gameObject); // 使用 ?. 避免空引用异常
            }
        }
    }


    public bool SetData(RewardItem rewardItem, RewardStyleType styleType = default, bool isEffect = false)
    {
        var cfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(rewardItem.Id);
        return SetData(new RewardEntityData()
        {
            Num = rewardItem.Num,
            ItemId = cfg.Id,
            Icon = cfg.Icon,
            Name = cfg.Name,
            Desc = cfg.Desc,
            Access = cfg.Access,
            Quality = cfg.Quality,
            StyleType = styleType,
            IsEffect = isEffect
        });
    }

    public bool SetData(object itemId, long itemNum, RewardStyleType styleType = default, bool isEffect = false)
    {
        return SetData(new RewardItem(ObjectTool.ObjectToInt(itemId), itemNum), styleType, isEffect);
    }

    public bool SetData(RewardAndNum rewardItem, RewardStyleType styleType = default, bool isEffect = false)
    {
        return SetData(new RewardItem(rewardItem.Id, rewardItem.Num), styleType, isEffect);
    }

    public bool SetDataParam(RewardAndNum rewardItem, RewardEntityData data = null)
    {
        var cfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(rewardItem.Id);
        data ??= new RewardEntityData();
        if (data.Num == 0)
        {
            data.Num = rewardItem.Num;
        }

        if (data.ItemId == 0) data.ItemId = cfg.Id;
        if (data.Icon == "") data.Icon = cfg.Icon;
        if (data.Name == "") data.Name = cfg.Name;
        if (data.Desc == "") data.Desc = cfg.Desc;
        if (data.Access == "") data.Access = cfg.Access;
        if (data.Quality == default) data.Quality = cfg.Quality;

        return SetData(data);
    }
}

public class RewardEntityData : UIPanelData
{
    public long Num = 0;
    public int ItemId = 0;
    /// <summary>装备等级角标；&gt;0 时优先使用，否则从 ItemId 对应装备配置读取 EquipLevel。</summary>
    public int EquipLevel = 0;
    public string Icon = "";
    public int SoldierId = 0;
    public Sprite IconSprite = null;
    public string Name = "";
    public string Desc = "";
    public string Access = "";
    public bool Check = false;
    public bool EquipIsWear = false;
    public RewardStatusType Status;
    public QualityType Quality;
    public RewardStyleType StyleType;
    public bool IsEffect = false;
    public Action OnClick = null;
}

/**
 * 奖励状态类型
 */
public enum RewardStatusType
{
    /// <summary>
    /// 默认
    /// </summary>
    Default = 0,

    /// <summary>
    /// 可领取
    /// </summary>
    CanReceive = 1,

    /// <summary>
    /// 已领取
    /// </summary>
    Received = 2,

    /// <summary>
    /// 锁定
    /// </summary>
    Lock = 3,
}


/**
 * 数量样式类型
 */
public enum RewardStyleType
{
    /// <summary>
    /// 默认
    /// </summary>
    Default = 0,

    /// <summary>
    /// 消耗
    /// </summary>
    Consume = 1,

    /// <summary>
    /// 消耗简洁
    /// </summary>
    ConsumeBrief = 2,

    /// <summary>
    /// 消耗简洁(白色)
    /// </summary>
    ConsumeBriefWhiteColor = 3,
    
    /// <summary>
    /// 装备
    /// </summary>
    Equip = 4,
}