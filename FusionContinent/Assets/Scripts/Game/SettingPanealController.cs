using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FusionContinent;
using FusionContinent.Model;
using QFramework;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanealController : MonoBehaviour
{
    public Button Playericon;
    private Text Playername;
    private Text PlayerUID;
    private Text PlayerScore;
    private Text PlayerLevel;
    private Button Audiobtn;
    private Button Soundbtn;
    private Button Feedbackbtn;
    private Button LogOutBtn;
    private Button Quitbtn;
    public bool Audioclick;
    public bool Soundlick;
    private GameObject FeedbackPaneal;
    private Button PlayerNameBtn;

    private int _toolClickCount;
    // Start is called before the first frame update
    void Awake()
    {
        Playericon = transform.Find("middle/Playersetting/Playericon/icon").GetComponent<Button>();
        Playername = transform.Find("middle/Playersetting/PlayerName/playername").GetComponent<Text>();
        PlayerNameBtn = transform.Find("middle/Playersetting/PlayerName").GetComponent<Button>();
        PlayerUID = transform.Find("middle/Playersetting/PlayerUid/playeruid").GetComponent<Text>();
        PlayerScore = transform.Find("middle/Playersetting/PlayerScore/Score").GetComponent<Text>();
        PlayerLevel = transform.Find("middle/Playersetting/Level/Level").GetComponent<Text>();
        Audiobtn = transform.Find("setting/Music/Musicbtn").GetComponent<Button>();
        Soundbtn = transform.Find("setting/Sound/soundbtn").GetComponent<Button>();
        Feedbackbtn = transform.Find("feedbackbtn").GetComponent<Button>();
        LogOutBtn = transform.Find("LogOutBtn").GetComponent<Button>();
        Quitbtn = transform.Find("Quit").GetComponent<Button>();
        
        Audioclick = true;
        Soundlick = true;
        Audiobtn.onClick.AddListener(() =>
        {
            Audioclick = !Audioclick;
            if (Audioclick)
            {
                Audiobtn.transform.Find("开").localScale = Vector3.one;
                Audiobtn.transform.Find("开btn").localScale = Vector3.one;
                Audiobtn.transform.Find("关").localScale = Vector3.zero;
                Audiobtn.transform.Find("关btn").localScale = Vector3.zero;
           
                Global.Music.Value = true;
            }
            else
            {
                Audiobtn.transform.Find("开").localScale = Vector3.zero;
                Audiobtn.transform.Find("开btn").localScale = Vector3.zero;
                Audiobtn.transform.Find("关").localScale = Vector3.one;
                Audiobtn.transform.Find("关btn").localScale = Vector3.one;
                Global.Music.Value = false;
            }
        });
        Soundbtn.onClick.AddListener(() =>
        {
            Soundlick = !Soundlick;
            if (Soundlick)
            {
                Soundbtn.transform.Find("开").localScale = Vector3.one;
                Soundbtn.transform.Find("开btn").localScale = Vector3.one;
                Soundbtn.transform.Find("关").localScale = Vector3.zero;
                Soundbtn.transform.Find("关btn").localScale = Vector3.zero;
                //AudioKit.ResumeVoice();
                Global.Sound.Value = true;
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().volume = 1;
            }
            else
            {
                Soundbtn.transform.Find("开").localScale = Vector3.zero;
                Soundbtn.transform.Find("开btn").localScale = Vector3.zero;
                Soundbtn.transform.Find("关").localScale = Vector3.one;
                Soundbtn.transform.Find("关btn").localScale = Vector3.one;
                //AudioKit.PauseVoice();
                Global.Sound.Value = false;
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().volume = 0;
            }
        });
        Feedbackbtn.onClick.AddListener(() => { 
            if(FeedbackPaneal == null)
            {
                FeedbackPaneal = ResLoader.Allocate().LoadSync<GameObject>("FeedbackPaneal").Instantiate();
                FeedbackPaneal.transform.SetParent(this.transform);
                FeedbackPaneal.transform.localPosition = Vector3.zero;
                FeedbackPaneal.transform.localScale = Vector3.one;
            }
            else
            {
                FeedbackPaneal.SetActive(true);
            }
        });
        
        LogOutBtn.onClick.AddListener(() =>
        {
            UserModel.LogOut();
            gameObject.SetActive(false);
        });
        
        // 工具面板
        PlayerNameBtn.GetComponent<Button>().GetComponent<Button>().onClick.AddListener(() =>
        {
            _toolClickCount++;
            if (_toolClickCount >= 8)
            {
                _toolClickCount = 0;
                UIKit.OpenPanel<ToolPanel>();
            }
        });
    }

    private void OnEnable()
    {
            if (Global.User.Avatar <= 0)
            {
                Global.User.Avatar = UserData.DefaultAvatarId;
            }
            // Playericon.GetComponent<Image>().sprite = Global.pl.GetSprite(Global.User.GetAvatarIconSpriteName());
        Playername.text = Global.User.UserName;
        PlayerUID.text = "UID:" + Global.User.UserId;
        PlayerScore.text = Global.PlayerBattle.Score.ToString();
        PlayerLevel.text = $"{Global.GetMaxClearStageName()}";
        Playericon.onClick.AddListener(() =>
        {
            // transform.parent.GetComponent<MainInterfacePanel>().iconListPaneal.SetActive(true);
        });
        Quitbtn.onClick.AddListener(() =>
        {
            this.gameObject.SetActive(false);
        });
    }
}
