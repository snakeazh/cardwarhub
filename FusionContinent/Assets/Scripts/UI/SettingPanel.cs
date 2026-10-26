using System;
using System.Collections.Generic;
using Assets.Scripts.Localization;
using Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FusionContinent.Animation;
using FusionContinent.Model;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using TMPro;
using UnityEngine.Events;

namespace FusionContinent
{
    public class SettingPanelData : UIPanelData
    {
    }

    public partial class SettingPanel : UIPanel
    {
        private bool _audioClick;
        private bool _soundClick;

        private int _toolClickCount;
        private GameObject _feedbackPanel;

        protected override void OnAnimationClose()
        {
            this.gameObject.SetActive(false);
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, true);
        }
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as SettingPanelData ?? new SettingPanelData();

            Quit.onClick.AddListener(() =>
            {
                UIKit.HidePanel<SettingPanel>();
                // GetComponent<UIBottomPopup>().Hide();
                //this.gameObject.SetActive(false);
                //Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, true);
            });

            CloseBtn.onClick.AddListener(() => Quit.onClick.Invoke());

            _audioClick = true;
            _soundClick = true;
            MusicBtn.onClick.AddListener(() =>
            {
                _audioClick = !_audioClick;
                if (_audioClick)
                {
                    MusicBtn.transform.Find("开").localScale = Vector3.one;
                    MusicBtn.transform.Find("开btn").localScale = Vector3.one;
                    MusicBtn.transform.Find("关").localScale = Vector3.zero;
                    MusicBtn.transform.Find("关btn").localScale = Vector3.zero;
        
                    Global.Music.Value = true;
                }
                else
                {
                    MusicBtn.transform.Find("开").localScale = Vector3.zero;
                    MusicBtn.transform.Find("开btn").localScale = Vector3.zero;
                    MusicBtn.transform.Find("关").localScale = Vector3.one;
                    MusicBtn.transform.Find("关btn").localScale = Vector3.one;
        
                    Global.Music.Value = false;
                }
            });
            SoundBtn.onClick.AddListener(() =>
            {
                _soundClick = !_soundClick;
                if (_soundClick)
                {
                    SoundBtn.transform.Find("开").localScale = Vector3.one;
                    SoundBtn.transform.Find("开btn").localScale = Vector3.one;
                    SoundBtn.transform.Find("关").localScale = Vector3.zero;
                    SoundBtn.transform.Find("关btn").localScale = Vector3.zero;
                    //AudioKit.ResumeVoice();
                    Global.Sound.Value = true;
                    UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().volume = 1;
                }
                else
                {
                    SoundBtn.transform.Find("开").localScale = Vector3.zero;
                    SoundBtn.transform.Find("开btn").localScale = Vector3.zero;
                    SoundBtn.transform.Find("关").localScale = Vector3.one;
                    SoundBtn.transform.Find("关btn").localScale = Vector3.one;
                    //AudioKit.PauseVoice();
                    Global.Sound.Value = false;
                    UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().volume = 0;
                }
            });
            FeedbackBtn.onClick.AddListener(() =>
            {
                if (_feedbackPanel == null)
                {
                    _feedbackPanel = ResLoader.Allocate().LoadSync<GameObject>("FeedbackPanel").Instantiate();
                    _feedbackPanel.transform.SetParent(this.transform);
                    _feedbackPanel.transform.localPosition = Vector3.zero;
                    _feedbackPanel.transform.localScale = Vector3.one;
                }
                else
                {
                    _feedbackPanel.SetActive(true);
                }
            });

            LogOutBtn.onClick.AddListener(() =>
            {
                UserModel.LogOut();
                gameObject.SetActive(false);
                // LoadingEffect.Instance.PlayAnim(true);
            });
            
            GiftCodeBtn.onClick.AddListener(() =>
            {
                var vm = new Game.GiftCode.GiftCodeInterfacePanelViewModel();
                vm.OnCreate();
                UIFramework.UIManager.OpenPanelAsync<GiftCodeInterfacePanelNew, Game.GiftCode.GiftCodeInterfacePanelViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
            });

            // 工具面板
            if (Global.Debug.Value == 1)
            {
                UserName.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _toolClickCount++;
                    if (_toolClickCount >= 8)
                    {
                        _toolClickCount = 0;
                        UIKit.OpenPanel<ToolPanel>();
                    }
                });
            }

            // UserAvatarBtn.onClick.AddListener(() =>
            // {
            //     // transform.parent.GetComponent<MainInterfacePanel>().iconListPaneal.SetActive(true);
            // });
            
            // 初始化多语言列表
            var langOptions = new List<string> { };
            var langList = DataConfigManager.Tables.TbLanguageConfig.DataList;
            foreach (var cfg in langList)
            {
                if (cfg.State == StateType.Close) continue;
                langOptions.Add(cfg.Name);
            }

            LanguageDropdown.AddOptions(langOptions);
            LanguageDropdown.value = (int)LocalizationManager.Instance.CurrentLanguage;
            LanguageDropdown.RefreshShownValue();
            LanguageDropdown.onValueChanged.AddListener((int idx) =>
            {
                var langName = langOptions[idx];
                LogTool.Log($"选择的语言 - idx: {idx}, langName: {langName}");
                LocalizationManager.Instance.SetLanguage((LanguageType)idx);
            });
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (Global.User.Avatar <= 0)
            {
                Global.User.Avatar = UserData.DefaultAvatarId;
            }

            // UserAvatarBtn.GetComponent<Image>().sprite = Global.AvatarIconSpriteAtlas.GetSprite( "Head_"+Global.User.Avatar);
            // UserName.text = Global.User.UserName;
            // UserId.text = "UID:" + Global.User.UserId;
            // ScoreNum.text = Global.PlayerBattle.Score.ToString();
            // StageName.text = $"{Global.GetMaxClearStageName()}";

            // Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_FLOW_POINTER_BTN_ENABLED, false);
        }
        protected override void RunAnimation(GameObject target, UnityAction complete)
        {
            UIAnimationUtils.PlayAnimations(target,complete);
        }

        protected override GameObject GetAnimationCloseNode
        {
            get => this.CloseAnimation.gameObject;
        }

        protected override GameObject GetAnimationOpenNode
        {
            get => this.OpenAnimation.gameObject;
        }
        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnClose()
        {
        }
    }
}