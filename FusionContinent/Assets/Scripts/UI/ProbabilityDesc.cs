using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using FusionContinent;
using FusionContinent.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProbabilityDesc : MonoBehaviour
{
    public Transform itemParent;

    public Transform downBtns;

    public Transform proibabilityList;

    public GameObject CardGroupName1;
    
    private TbChestCardConfig _chestCardConfig;

    private TbChestPoolConfig _chestPoolConfig;

    private TbGameBattleConst _gameBattleConst;
    
    private TbItemConfig _itemConfig;
    
    public Button quitBtn;

    // Start is called before the first frame update
    private Dictionary<int, Button> probabilityDescBtns = new Dictionary<int, Button>();

    private void Awake()
    {
        _chestCardConfig = DataConfigManager.Tables.TbChestCardConfig;
        _chestPoolConfig = DataConfigManager.Tables.TbChestPoolConfig;
        _gameBattleConst=DataConfigManager.Tables.TbGameBattleConst;
        _itemConfig=DataConfigManager.Tables.TbItemConfig;
    }

    void Start()
    {
        quitBtn.onClick.AddListener(()=>
        {
            this.gameObject.SetActive(false);
        });
        SetChestCardProbability();
    }

    private void OnEnable()
    {
        if (probabilityDescBtns.Count > 0)
        {
            var firstKey = _chestPoolConfig.DataList[0].Id;
            var firstBtn = probabilityDescBtns[firstKey];
            firstBtn.onClick.Invoke(); // 触发点击事件
        }
    }

    public void SetChestCardProbability()
    {
        probabilityDescBtns.Clear();
        for (int i = downBtns.childCount - 1; i >= 1; i--)
        {
            Destroy(downBtns.GetChild(i).gameObject);
        }
        
        for (int i = 0; i < _chestPoolConfig.DataList.Count; i++)
        {
            GameObject cardGroupBtn = Instantiate(CardGroupName1,downBtns);
            cardGroupBtn.transform.localScale = new Vector3(1, 1, 1);
            cardGroupBtn.gameObject.SetActive(true);
            probabilityDescBtns.Add(_chestPoolConfig.DataList[i].Id,
                cardGroupBtn.GetComponent<Button>());
        }

        foreach (var kvp in probabilityDescBtns)
        {
            var key = kvp.Key;       // 保存当前的 key，避免闭包问题
            var btn = kvp.Value;
            btn.transform.Find("Name").GetComponent<TMP_Text>().text =
                _chestPoolConfig.GetOrDefault(key).Name;// 按钮对象
            btn.onClick.AddListener(() =>
            {
                foreach (var btns in probabilityDescBtns)
                {
                    btns.Value.transform.Find("Select").transform.localScale=Vector3.zero;
                }

                btn.transform.Find("Select").transform.localScale = Vector3.one;
                SetProbabilityDesc(key);
                SetCardGroupReward(key);
            });
        }
        if (probabilityDescBtns.Count > 0)
        {
            var firstKey = _chestPoolConfig.DataList[0].Id;
            var firstBtn = probabilityDescBtns[firstKey];
            firstBtn.onClick.Invoke(); // 触发点击事件
        }
    }

    public void SetProbabilityDesc(int cardGroupId)
    {
        int num = _chestPoolConfig.GetOrDefault(cardGroupId).ProbabilityDesc.Count;
        for (int i = 0; i < proibabilityList.childCount; i++)
        {
            proibabilityList.GetChild(i).gameObject.SetActive(true);
            if (i > num - 1)
            {
                proibabilityList.GetChild(i).gameObject.SetActive(false);
            }
        }
        for (int i = 0; i < num; i++)
        {
            proibabilityList.GetChild(i).GetComponent<TMP_Text>().text = 
                _chestPoolConfig.GetOrDefault(cardGroupId).ProbabilityDesc[i].Desc;
            var probabilitycolor = _gameBattleConst.QualityTypeColors
                .FirstOrDefault(t => t.Type == _chestPoolConfig.GetOrDefault(cardGroupId).ProbabilityDesc[i].Quality).Color;
            proibabilityList.GetChild(i).GetComponent<TMP_Text>().color =
                new Color32((byte)probabilitycolor.X, (byte)probabilitycolor.Y, (byte)probabilitycolor.Z, 255);
        }
    }

    public void SetCardGroupReward(int cardGroupId)
    {
        var GroupIds = _chestPoolConfig.GetOrDefault(cardGroupId).CardGroup;
        // CommonItemPoolManager.Instance.GetGameObjectPool("RewardEntity").ReturnAllEffect();
        var cardList = _chestCardConfig.DataList
            .Where(t => GroupIds.Contains(t.CardGroup))
            .ToList();

        // cardList 里就是所有符合 GroupIds 的卡片
        cardList = cardList
            .OrderByDescending(t => t.Quality)
            .ToList();
        for (int i = 0; i < cardList.Count; i++)
        {
            var cardpool = CommonItemPoolManager.Instance.GetGameObjectPool("RewardEntity");
            GameObject cardObj = cardpool.Get();
            if(cardObj==null) return;
            var cfg = DataConfigManager.Tables.TbItemConfig.GetOrDefault(cardList[i].ItemId);
            cardObj.GetComponent<RewardEntity>().
            SetData(new RewardEntityData()
            {
                Num = cardList[i].ItemNum,
                ItemId = cardList[i].ItemId,
                Icon = cfg.Icon,
                Name = cfg.Name,
                Desc = cfg.Desc,
                Access = cfg.Access,
                StyleType = RewardStyleType.Default,
                Quality = cardList[i].Quality,
                IsEffect = false
            });
            //cardObj.transform.Find("Num").gameObject.SetActive(false);
            cardObj.transform.SetParent(itemParent);
            cardObj.transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }

}