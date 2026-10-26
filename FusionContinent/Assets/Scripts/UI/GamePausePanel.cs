using System.Collections.Generic;
using Config;
using Manager;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.SceneManagement;

namespace FusionContinent
{
    public class GamePausePanelData : UIPanelData
    {
    }

    public partial class GamePausePanel : UIPanel
    {
        private readonly Dictionary<int, GameObject> _schoolCards = new Dictionary<int, GameObject>();
        private TbSchoolCardConfig _schoolCardConfig;
        private TbMonsterGenConfig _monsterGenConfig;

        protected void Awake()
        {
            _schoolCardConfig = DataConfigManager.Tables.TbSchoolCardConfig;
            _monsterGenConfig=DataConfigManager.Tables.TbMonsterGenConfig;
        }
        

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as GamePausePanelData ?? new GamePausePanelData();
            BackBtn.onClick.AddListener(() =>
            {
                DialogManager.ShowConfirm("确认撤退?", "", () =>
                {
                    GamePlayManager.Default.BackHome();
                    UIKit.ClosePanel<GamePausePanel>();
                });
            });
            ContineBtn.onClick.AddListener(() =>
            {
                UIKit.ClosePanel<GamePausePanel>();
                GamePlayManager.Default.StartGame();
            });
            
            SchoolBtn.onClick.AddListener(() =>
            {
                var scroll = SchoolCardScroll.transform.Find("Viewport/Content");
                if (scroll.childCount > 1 && SchoolBtn.transform.Find("Select").localScale == Vector3.one) return;
                
                SchoolBtn.transform.Find("Select").localScale = Vector3.one;
                RewardBtn.transform.Find("Select").localScale = Vector3.zero;
                SchoolCardScroll.gameObject.SetActive(true);
                RewardCardScroll.gameObject.SetActive(false);
                InitSchoolCard();
            });
            RewardBtn.onClick.AddListener(() =>
            {
                if (RewardBtn.transform.Find("Select").localScale == Vector3.one) return;
                
                SchoolBtn.transform.Find("Select").localScale = Vector3.zero;
                RewardBtn.transform.Find("Select").localScale = Vector3.one;
                SchoolCardScroll.gameObject.SetActive(false);
                RewardCardScroll.gameObject.SetActive(true);
                InitRewardCard();

            });
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            SchoolBtn.GetComponent<Button>().onClick.Invoke();
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
            if (rewardCards.Count!=0)
            {
                for (int i = 0; i < rewardCards.Count; i++)
                {
                    CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity").Return(rewardCards[i]);
                }
                rewardCards.Clear();
            }
        }

        protected override void OnClose()
        {
            if (rewardCards.Count!=0)
            {
                for (int i = 0; i < rewardCards.Count; i++)
                {
                    CommonItemPoolManager.Instance.GetGameObjectPoolOrCreate("RewardEntity").Return(rewardCards[i]);
                }
                rewardCards.Clear();
            }
        }
        
        private void InitSchoolCard()
        {
            var scroll = SchoolCardScroll.transform.Find("Viewport/Content");
            var schoolCards = SchoolManager.Instance.GetSchoolCardAll();
            if (schoolCards.Count <= 0)
            {
                return;
            }
            int index = 0;
            foreach (var v in schoolCards.Values)
            {
                // if (!_schoolCards.TryGetValue(v.CardId, out var obj))
                // {
                //     obj = index < scroll.childCount ? scroll.Find("ChooseCardSlot").gameObject : Instantiate(scroll.Find("ChooseCardSlot").gameObject, scroll);
                //     obj.SetActive(true);
                //     _schoolCards.Add(v.CardId, obj);
                // }
                // obj.GetComponent<ChooseCardSlot>().SetData(v.CardId, false);
                if (!_schoolCards.TryGetValue(v.CardId, out var obj))
                {
                    obj = index < scroll.childCount ? scroll.Find("ChooseCard").gameObject : Instantiate(scroll.Find("ChooseCard").gameObject, scroll);
                    obj.SetActive(true);
                    _schoolCards.Add(v.CardId, obj);
                }
                SetChooseCardData(obj,v.CardId);
                obj.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // UIKit.OpenPanel<ChooseCardSlot>(UILevel.PopUI).SetData(v.CardId, false,obj.GetComponent<RectTransform>().position);
                });
                index++;
            }
        }

        private void SetChooseCardData(GameObject obj, int cardId)
        {
            var schoolCardCfg = DataConfigManager.Tables.TbSchoolCardConfig.GetOrDefault(cardId);
            obj.transform.Find("BuffIcon").GetComponent<Image>().sprite =
                Global.SchoolChooseCardIconSpriteAtlas.GetSpriteExt(schoolCardCfg.Icon);
            obj.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = schoolCardCfg.Name;

        }
        
        private List<GameObject> rewardCards = new List<GameObject>();
        private void InitRewardCard()
        {
            rewardCards.Clear();
            int stageId = Global.StageId.Value;
            int cityLevel = Global.CityLevel.Value;
            var genCfg = _monsterGenConfig.GetOrDefault(stageId);
            if (genCfg == null || genCfg.StageList.Count < cityLevel)
            {
                return;
            }
            
            var rewards = genCfg.StageList[(cityLevel - 1 < 0) ? 0 : cityLevel - 1].Rewards;
            Transform rewardCardParent = RewardCardScroll.transform.Find("Viewport/Content");
            for (int i = 0; i <rewards.Count; i++)
            {
                var rewardObj = i < rewardCardParent.childCount 
                    ? rewardCardParent.GetChild(i).gameObject 
                    : Instantiate(rewardCardParent.GetChild(0), rewardCardParent).gameObject;
                if (rewardObj == null) return;
                
                rewardObj.SetActive(true);
                rewardObj.transform.localScale = Vector3.one;
                var rewardEntity = rewardObj.GetComponent<RewardEntity>();
                rewardObj.transform.localPosition = new Vector3(0f, 0f, 0f);
                rewardEntity.SetData(rewards[i].Id, rewards[i].Num);
                rewardCards.Add(rewardObj);
            }
        }
    }
}