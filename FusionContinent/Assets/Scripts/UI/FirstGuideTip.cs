using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using FusionContinent;

using System.Linq;
using Config;
using FusionContinent.Model;
using FusionContinent.Utils;
using TMPro;

public class FirstGuideTip : MonoBehaviour
{
    [SerializeField]
    private TMP_Text tipText;
    public Image mask;

    private float _duration;
    private float _guideTime;
    private int _showStatus;
    private int _guidesStep;
    private int _guidesStepIdx;
    private GuideConfig _guideConfig;
    private int _clickCount; // 添加点击计数器
    public Button closeButton;
    public Image IconBg;
    public Sprite[] Aletters;

    private void Awake()
    {
        _guideConfig = DataConfigManager.Tables.TbGuideConfig.DataList.FirstOrDefault(v => v.Type == GuideType.GuideStage);
        if (_guideConfig == null || Global.Player.CheckGuideStatus(_guideConfig.Id))
        {
            return;
        }

        Global.Publisher.Subscribe(MessagePublisher.TRIGGER_GUIDE_TIP, (step) =>
        {
            // LogTool.Log($">>>>>>> 战内引导消息订阅: {step}");
            if (_guidesStep + 1 != int.Parse(step.ToString()) || _duration > 0f || _guidesStep >= _guideConfig.StepList.Count)
            {
                return;
            }
            
            var cfg = _guideConfig.StepList[_guidesStep];
            if (cfg.Type == GuideType.GuideStageStory)
            {
                GamePlayManager.Default.PauseGame();
            }
            
            ShowStatus(1);
            ShowTipType(cfg);

            _duration = cfg.Duration;
            StartCoroutine(StringTool.Typing(tipText, cfg.TipText, cfg.TypingSpeed));

            // 重置点击计数器
            _clickCount = 0;
            
            // 点击事件
            // transform.GetComponent<Button>().onClick.RemoveAllListeners();
            // transform.GetComponent<Button>().onClick.AddListener(OnClick);
        });

        ShowStatus();
    }
    
    void Start()
    {
        _guidesStep = 0;
        _clickCount = 0; // 初始化点击计数器
        closeButton.onClick.AddListener(() => OnClickBtn());
        IconBg.sprite = Aletters[0];
    }

    void Update()
    {
        if (_duration > 0f)
        {
            _guideTime += Time.deltaTime;
            if (_guideTime >= _duration)
            {
                OnClose();
            }
        }
    }

    // 添加点击处理方法
    private void OnClickBtn()
    {
        if (_showStatus != 1) return;
        
        _clickCount++;
        
        // 添加点击反馈效果（可选）
        transform.DOScale(0.95f, 0.1f).OnComplete(() => 
        {
            transform.DOScale(1f, 0.1f);
        });
        
        // 更新提示文本显示点击次数（可选）
        if (tipText != null)
        {
            tipText.text = $"点击继续 ({_clickCount}/3)";
        }
        
        
        // 达到2次点击后关闭
        if (_clickCount >= 3)
        {
            OnClose();
        }
        else
        {
            IconBg.sprite = Aletters[_clickCount%3];
            if (_clickCount == 2)
            {
                closeButton.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = "开始冒险";
            }
            else
            {
                closeButton.transform.Find("Text (TMP)").GetComponent<TMP_Text>().text = "下一步";
            }
        }
    }

    private void OnClose()
    {
        var cfg = _guideConfig.StepList[_guidesStep];
        if (cfg.Type == GuideType.GuideStageStory) GamePlayManager.Default.StartGame();
        
        ShowStatus();
        _guidesStep++;
        _duration = 0f;
        _guideTime = 0f;
        _clickCount = 0; // 重置点击计数器
    }

    private void ShowTipType(GuidesStepListStruct cfg)
    {
        transform.Find("tip").gameObject.SetActive(false);
        transform.Find("Text").gameObject.SetActive(false);
        transform.Find("kuang").gameObject.SetActive(false);
        transform.Find("arrow").gameObject.SetActive(false);
        transform.Find("StoryImage").gameObject.SetActive(false);
        
        switch (cfg.TipType)
        {
            case GuideTipType.TipText:
                transform.Find("tip").gameObject.SetActive(true);
                transform.Find("Text").gameObject.SetActive(true);
                transform.Find("kuang").gameObject.SetActive(true);
                transform.Find("arrow").gameObject.SetActive(true);
                transform.GetComponent<RectTransform>().anchoredPosition = new Vector3(cfg.Position.X, cfg.Position.Y, cfg.Position.Z);
                break;
            
            case GuideTipType.Finger:
                
                break;
            
            case GuideTipType.StoryImage:
                transform.Find("StoryImage").gameObject.SetActive(true);
                // 修改故事图片模式的关闭按钮监听
                transform.Find("StoryImage/Canvas/CloseBtn").GetComponent<Button>().onClick.RemoveAllListeners();
                break;
            
            default:
                transform.Find("Text").gameObject.SetActive(true);
                transform.GetComponent<RectTransform>().anchoredPosition = new Vector3(cfg.Position.X, cfg.Position.Y, cfg.Position.Z);
                break;
        }
    }

    private void ShowStatus(int status = 0)
    {
        gameObject.SetActive(status == 1);
        mask.gameObject.SetActive(false);
        transform.Find("Text").gameObject.SetActive(status == 1);
        _showStatus = status;
    }
}