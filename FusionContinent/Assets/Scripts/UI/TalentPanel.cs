using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Manager;
using FusionContinent.Model;
using FusionContinent.Utils;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.U2D;

namespace FusionContinent
{
    [System.Serializable]
    public class Details
    {
        public Dictionary<PropertyType, float> properties = new Dictionary<PropertyType, float>();
    }

    public class TalentPanelData : UIPanelData
    {
    }

    public partial class TalentPanel : UIPanel
    {
        public Button returnBtn;
        
        public static TalentPanel Default;
        public Action cardSavdAction;
        public TbTalentConfig _tbTalentConfig;
        private TbPropertyConfig _tbPropertyConfig;
        public LevelSliderController talentSlider;
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as TalentPanelData ?? new TalentPanelData();
            RefBtn.onClick.AddListener(() =>
            {
                UIKit.HidePanel<TalentPanel>();
                UIKit.OpenPanel<PlayerPaneal>();
                //Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
            });
            // Talentshow.TalentRef = () =>
            // {
            //     Talent.gameObject.SetActive(true);
            //     Talentshow.gameObject.SetActive(false);
            //     for (int i = 0; i < talentSlider.content.childCount; i++)
            //     {
            //         if (talentSlider.content.GetChild(i).GetComponent<LevelCard>() == null) continue;
            //         int talentid = _tbTalentConfig
            //             .DataList[talentSlider.content.GetChild(i).GetComponent<LevelCard>().levelId].Id;
            //         talentSlider.content.GetChild(i).GetComponent<TalentObj>().SetTalent(talentid);
            //     }
            // };
            
            returnBtn.onClick.AddListener(() =>
            {
                UnlockGuideManager.Send(ModuleFunctionType.Talent,7);
                UIKit.ClosePanel(this);
            });
            // please add init code here
        }

        private void OnEnable()
        {
        }

        private void Awake()
        {
            Default = this;
            _tbTalentConfig = DataConfigManager.Tables.TbTalentConfig;
            _tbPropertyConfig = DataConfigManager.Tables.TbPropertyConfig;
        }

        private GameObject BattleOver;
        LevelCard levelCard;

        private void SetTalet()
        {
            Talentshow.gameObject.SetActive(true);
            this.gameObject.SetActive(true);
            Talentshow.SetTalentShow(101);
        }

        private void SetTalentDetails(int talentId)
        {
        }

        private Tween talenttween;
        private int selectedTalentID;

        private void OnTalentChange(LevelCard talent)
        {
            levelCard = talent;
            int talentid = _tbTalentConfig.DataList[talent.levelId].Id;
            if (talenttween != null)
            {
                talenttween.Kill();
            }
            
            CardAtlasBtn.gameObject.SetActive(true);
            RefBtn.gameObject.SetActive(true);
            GoBattleBtn.gameObject.SetActive(true);
            
            if (Global.PlayerTalents[talentid].Status == 0)
            {
                GoBattleBtn.transform.GetChild(1).GetComponent<TMP_Text>().text =
                    "上阵";
                GoBattleBtn.transform.GetChild(0).localScale = Vector3.zero;
            }
            else
            {
                GoBattleBtn.transform.GetChild(1).GetComponent<TMP_Text>().text =
                    "已上阵";
                GoBattleBtn.transform.GetChild(0).localScale = Vector3.one;
            }

            CardAtlasBtn.onClick.AddListener(() => TalentUpdateBtn(talentid));
            
            CardAtlasBtn.onClick.RemoveAllListeners();
            CardAtlasBtn.onClick.AddListener(() =>
            {
                // UIKit.OpenPanel<CardGroupPanel01>(UILevel.PopUI).SetData(talentid);
            });
            
            GoBattleBtn.onClick.RemoveAllListeners();
            GoBattleBtn.onClick.AddListener(() =>
            {
                if (Global.PlayerTalents[talentid].Status == 0)
                {
                    Global.PlayerTalents[talentid].UseTalent(PlayerTalent.STATUS_IN_USE);
                    GoBattleBtn.transform.GetChild(1).GetComponent<TMP_Text>().text =
                        "已上阵";
                    GoBattleBtn.transform.GetChild(0).localScale = Vector3.one;
                }
            });
            
        }

        private void TalentUpdateBtn(int talentId)
        {
        }


        protected override void OnOpen(IUIData uiData = null)
        {
            
            //Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, false);
        }

        protected override void OnShow()
        {
            SetTalet();
            UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent,7,this.returnBtn.GetComponent<RectTransform>());

            
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }
    }
}