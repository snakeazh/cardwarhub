using System;
using System.Collections;
using System.Collections.Generic;
using Config;
using QFramework;
using UnityEngine;

namespace FusionContinent
{
    public class BattleInfoPanelData : UIPanelData
    {
        
    }
    
    public partial class BattleInfoPanel : UIPanel
    {
        private TbStageBattleConfig _stageBattleConfig;
        private TbMonsterGenConfig _monsterGenConfig;
        private TbMonsterBattleConfig _monsterBattleConfig;
        private TbCardBaseConfig _cardBaseConfig;
        private TbItemConfig _itemConfig;
        
        private StageBattleConfig _currentStageBattleConfig;
        private MonsterGenConfig _currentMonsterGenConfig;
        private MonsterBattleConfig _currentMonsterBattleConfig;
        private CardBaseConfig _currentCardBaseConfig;

        private List<MonsterGenStage> _moList = new List<MonsterGenStage>();
        private List<RewardAndNum> _rewardList = new List<RewardAndNum>();
        
        private void Awake()
        {
            _stageBattleConfig = DataConfigManager.Tables.TbStageBattleConfig;
            _monsterGenConfig = DataConfigManager.Tables.TbMonsterGenConfig;
            _monsterBattleConfig = DataConfigManager.Tables.TbMonsterBattleConfig;
            _cardBaseConfig = DataConfigManager.Tables.TbCardBaseConfig;
            _itemConfig = DataConfigManager.Tables.TbItemConfig;
        }

        public void SetData(int levelID)
        {
            _currentStageBattleConfig = _stageBattleConfig.GetOrDefault(levelID);
            _currentMonsterGenConfig = _monsterGenConfig.GetOrDefault(levelID);
            _moList = _currentMonsterGenConfig.StageList;
            _rewardList = _currentStageBattleConfig.FirstRewards;
            Desc.text = _currentStageBattleConfig.StageDesc;

            var monsterContent = MonsterCardScroll.content;
            for (int i = 0; i < monsterContent.childCount; i++)
            {
                monsterContent.GetChild(i).gameObject.SetActive(false);
            }

            var molist = seachMonster(_moList);
            for (int i = 0; i < molist.Count; i++)
            {
                var monster = molist[i];
                var obj = i < monsterContent.childCount
                    ? monsterContent.GetChild(i).gameObject
                    : Instantiate(monsterContent.GetChild(0), monsterContent).gameObject;
                var cfg = _cardBaseConfig.GetOrDefault(monster.Id);
                obj.GetComponent<MonsterCardAtlasSlot>().SetData(
                    new MonsterCardAtlasSlotData()
                    {
                        Quality = monster.MonsterQuality,
                        CardName = cfg?.Name,
                        IconSprite = Global.MonsterIconSpriteAtlas.GetSpriteExt(cfg?.Icon),
                        IsUnlock = true,
                        Label = monster.Label,
                    });
                obj.gameObject.SetActive(true);
            }

            var rewardContent = RewardItemScroll.content;
            for (int i = 0; i < rewardContent.childCount; i++)
            {
                rewardContent.GetChild(i).gameObject.SetActive(false);
            }

            for (int i = 0; i < _rewardList.Count; i++)
            {
                var reward = _rewardList[i];
                var obj = i < rewardContent.childCount
                    ? rewardContent.GetChild(i).gameObject
                    : Instantiate(rewardContent.GetChild(0), rewardContent).gameObject;
                var itemCfg = _itemConfig.GetOrDefault(reward.Id);
                obj.GetComponent<RewardEntity>().SetData(new RewardEntityData()
                    {
                        Icon = itemCfg.Icon,
                        Quality = itemCfg.Quality,
                        Num = reward.Num
                    }
                );
                obj.gameObject.SetActive(true);
            }
            
        }

        private List<MonsterBattleConfig> seachMonster(List<MonsterGenStage> list)
        {
            List<int> monsterList = new List<int>();
            List<MonsterBattleConfig> list2 = new List<MonsterBattleConfig>();
            foreach (var wave in list)
            {
                foreach (var v in wave.WaveList)
                {
                    foreach (var card in v.GenCards)
                    {
                        if (!monsterList.Contains(card.CardId))
                        {
                            var m = _monsterBattleConfig.GetOrDefault(card.CardId);
                            monsterList.Add(card.CardId);
                            list2.Add(m);
                        }
                    }
                }
            }
            
            return list2;
        }
        
        public void Show(int stageId)
        {
            SetData(stageId);
        }
        
        

        protected override void OnClose()
        {
            
        }
    }
    
}