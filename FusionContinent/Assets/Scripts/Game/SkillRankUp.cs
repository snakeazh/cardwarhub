using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillRankUp : MonoBehaviour
{
    [Header("UI References")]
    public Button upBtn;
    public Transform consumes;
    public TMP_Text Desc;
    public LevelSliderController rankcardScroll;
    public GameObject Lock;

    public Transform RankUpConsume;

    private TbCardDevelopConfig _developConfig;
    private TbSpecialEffectsConfig _specialConfig;

    private readonly List<int> _awakingLevels = new List<int>();
    private readonly List<SpecialLevelPropertyList> _matchingSpecialProps = new List<SpecialLevelPropertyList>();

    private readonly Dictionary<QualityType, string> _qualityDict = new Dictionary<QualityType, string>
    {
        { QualityType.Ordinary, "青铜品质" },
        { QualityType.Rarity, "白银品质" },
        { QualityType.Epic, "黄金品质" },
        { QualityType.Legend, "钻石品质" }
    };

    public int _level;
    private int _cardId;
    int GoToLevelId;

    private void Awake()
    {
        _developConfig = DataConfigManager.Tables.TbCardDevelopConfig;
        _specialConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;
    }

    public void SetCardRank(int cardId, int cardLevel)
    {
        
        // 先把旧的东西全清掉
        for (int i = RankUpConsume.childCount - 1; i >= 0; i--)
            DestroyImmediate(RankUpConsume.GetChild(i).gameObject);

        for (int i = rankcardScroll.content.childCount - 1; i >= 0; i--)
            DestroyImmediate(rankcardScroll.content.GetChild(i).gameObject);

        rankcardScroll.levelSprites.Clear();
        _cardId = cardId;
        _level = cardLevel;
        GoToLevelId = 0; // 先默认 0
        upBtn.transform.SetAsLastSibling();
        _matchingSpecialProps.Clear();
        _awakingLevels.Clear();

        var devConfig = _developConfig.GetOrDefault(cardId);
        if (devConfig == null)
        {
            LogTool.LogWarning($"未找到 CardId={cardId} 的 DevelopConfig");
            return;
        }

        ExtractAwakingLevels(devConfig);

        if (_awakingLevels.Count == 0)
        {
            ShowUnrankable();
            return;
        }

        var specialConfig = _specialConfig.GetOrDefault(cardId);
        if (specialConfig == null)
        {
            LogTool.LogWarning($"未找到 CardId={cardId} 的 SpecialConfig");
            return;
        }

        BuildMatchingSpecialProps(specialConfig);

        var skillBaseConfig = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(cardId);
        if (skillBaseConfig == null)
        {
            LogTool.LogWarning($"未找到 CardId={cardId} 的 BaseConfig");
            return;
        }

        SetupRankCards(skillBaseConfig);

        // 找到最近的升阶点
        GoToLevelId = FindClosestRankIndex();

        // 重设事件
        rankcardScroll.onCardClicked.RemoveAllListeners();
        rankcardScroll.onCenterChanged.RemoveAllListeners();
        rankcardScroll.onCardClicked.AddListener(OnSkillChange);
        rankcardScroll.onCenterChanged.AddListener(OnSkillChange);

        // 跳转到最近的品质卡
        rankcardScroll.SnapToCardAfterLayout(GoToLevelId);
    }

    
    /// <summary>
    /// 找到距离当前等级最近的升阶点索引
    /// </summary>
    private int FindClosestRankIndex()
    {
        int closestIndex = 0;
        int minDelta = int.MaxValue;

        for (int i = 0; i < _matchingSpecialProps.Count; i++)
        {
            int delta = _matchingSpecialProps[i].Level - (_level + 1);
            if (delta >= 0 && delta < minDelta)
            {
                minDelta = delta;
                closestIndex = i;
            }
        }

        // 如果所有升阶点都比当前等级小，默认跳到最后一个
        if (minDelta == int.MaxValue)
        {
            closestIndex = _matchingSpecialProps.Count - 1;
        }

        return closestIndex;
    }


    private void ExtractAwakingLevels(CardDevelopConfig devConfig)
    {
        _awakingLevels.Clear();
        var list = devConfig.LevelPropertyList;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].AwakeningRewards.Count > 0)
            {
                _awakingLevels.Add(list[i].Level + 1);
            }
        }
    }

    private void BuildMatchingSpecialProps(SpecialEffectsConfig specialConfig)
    {
        _matchingSpecialProps.Clear();

        if (specialConfig == null || specialConfig.LevelPropertyList == null) return;

        var awakingSet = new HashSet<int>(_awakingLevels); // O(1) 查找
        var addedLevels = new HashSet<int>();              // 防止同一 level 被加入多次
        var list = specialConfig.LevelPropertyList;

        for (int i = 0; i < list.Count; i++)
        {
            var p = list[i];
            if (p == null) continue;

            if (awakingSet.Contains(p.Level))
            {
                if (addedLevels.Add(p.Level))
                {
                    _matchingSpecialProps.Add(p);
                }
                else
                {
                    //LogTool.Log($"BuildMatchingSpecialProps: skip duplicate special level {p.Level}");
                }
            }
        }

        // 加入基础等级（如果还没加入）
        if (list.Count > 0)
        {
            var baseProp = list[0];
            if (baseProp != null && addedLevels.Add(baseProp.Level))
            {
                _matchingSpecialProps.Add(baseProp);
            }
            else if (baseProp != null)
            {
                //LogTool.Log($"BuildMatchingSpecialProps: base level {baseProp.Level} already present, not adding again.");
            }
        }

        // 按品质排序（按需）
        _matchingSpecialProps.Sort((a, b) => a.Quality.CompareTo(b.Quality));

        //LogTool.Log($"BuildMatchingSpecialProps: final count={_matchingSpecialProps.Count}; levels=[{string.Join(",", _matchingSpecialProps.Select(p => p.Level))}]");
    }
    private void SetupRankCards(CardBaseConfig skillBaseConfig)
    {
        // 先清空 content 下的旧卡牌
        for (int i = rankcardScroll.content.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(rankcardScroll.content.GetChild(i).gameObject);
        }

        // 清空 scroll 内部缓存
        rankcardScroll.levelSprites.Clear();
        // 重新准备数据
        var levelSprites = _matchingSpecialProps
            .Select(prop => Global.CarFrameAtlas.GetSprite($"{prop.Quality}Card"))
            .Where(sprite => sprite != null)
            .ToList();
        rankcardScroll.SetCardId(_matchingSpecialProps.Select(c => c.Level).ToList(), levelSprites);
        rankcardScroll.GenerateCards();

        // 更新 UI
        for (int i = 0; i < rankcardScroll.content.childCount; i++)
        {
            var levelCard = rankcardScroll.content.GetChild(i).GetComponent<LevelCard>();
            if (levelCard == null) continue;

            UpdateCardUI(rankcardScroll.content.GetChild(i), levelCard, skillBaseConfig);
        }
    }


    private void UpdateCardUI(Transform cardTransform, LevelCard levelCard, CardBaseConfig skillBaseConfig)
    {
        var special = _matchingSpecialProps[levelCard.levelId];
        cardTransform.Find("iconKuang/CardSlot/Icon").GetComponent<Image>().sprite =
            Global.CardIconSpriteAtlas.GetSpriteExt(skillBaseConfig.Icon);
        cardTransform.Find("iconKuang/CardSlot/CardName").GetComponent<TMP_Text>().text = skillBaseConfig.Name;
        cardTransform.Find("iconKuang/CardSlot/Title").GetComponent<TMP_Text>().text = _qualityDict[special.Quality];
    }
    private int cardindex = 0;
    private void ShowUnrankable()
    {
        upBtn.gameObject.SetActive(false);
        consumes.gameObject.SetActive(false);
        Desc.gameObject.SetActive(true);
        Lock.gameObject.SetActive(true);
        var property =_developConfig.GetOrDefault(_cardId).LevelPropertyList
            .FirstOrDefault(t => t.Level ==cardindex*10);
        if (property != null)
        {
            Lock.transform.Find("Tip").GetComponent<TMP_Text>().text = property.Name+ "解锁";  
        }

    }
    private void OnSkillChange(LevelCard card)
    {
        if (card.levelId < 0 || card.levelId >= _matchingSpecialProps.Count) return;

        var skillSpecial = _matchingSpecialProps[card.levelId];
        var property = _developConfig.GetOrDefault(_cardId)
            .LevelPropertyList
            .FirstOrDefault(t => t.Level == skillSpecial.Level);
        
        for (int i = RankUpConsume.childCount - 1; i >= 0; i--)
        {
            Destroy(RankUpConsume.GetChild(i).gameObject);
        }
        foreach (var v in property.ConsumeItems)
        {
            var consumeNum = v.Num;
            var rewardObj = Global.ResLoader.LoadSync<GameObject>("RewardEntity").Instantiate();
            rewardObj.transform.SetParent(RankUpConsume);
            rewardObj.LocalScale(new Vector3(1f, 1f, 1f));
            var rewardEntity = rewardObj.GetComponent<RewardEntity>();
            rewardEntity.SetData(v.Id, consumeNum, RewardStyleType.Consume);
        }
        
        if (property == null) return;
        cardindex=card.levelId;
        Lock.gameObject.SetActive(false);
        Desc.gameObject.SetActive(true);
        RankUpConsume.gameObject.SetActive(true);
        consumes.gameObject.SetActive(false);
        Desc.text = property.UpQualityDesc.FirstOrDefault();
        int targetLevel = skillSpecial.Level - 1;
        if (targetLevel == _level)
        {
            upBtn.gameObject.SetActive(true);
            consumes.gameObject.SetActive(false);
            //Desc.text = property.UpQualityDesc[0];
        }
        else if (targetLevel < _level)
        {
            upBtn.gameObject.SetActive(false);
            consumes.gameObject.SetActive(false);
            RankUpConsume.gameObject.SetActive(false);
            //Desc.text = property.UpQualityDesc[0];
        }
        else
        {
            ShowUnrankable();
        }
    }
}
