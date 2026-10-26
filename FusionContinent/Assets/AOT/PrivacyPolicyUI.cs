using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PrivacyPolicyUI : MonoBehaviour
{
    // UI组件引用
    [Header("UI组件")]
    public Button agreeBtn;
    public Button refuseBtn;
    public GameObject privacyPanel;

    // 隐私协议链接（替换为你的实际链接）
    private const string PRIVACY_AGREEMENT_URL = "https://your-domain.com/privacy.html";

    // 持久化Key（记录用户是否已选择）
    private const string PLAYERPREFS_PRIVACY_SELECTED = "PrivacyPolicy_Selected";
    private const string PLAYERPREFS_PRIVACY_AGREED = "PrivacyPolicy_Agreed";
    public GameObject privacyPanelEnd;
    private void Start()
    {
        // 注册按钮事件
        agreeBtn.onClick.AddListener(OnAgreePrivacy);
        refuseBtn.onClick.AddListener(OnRefusePrivacy);
        // 检查是否需要显示隐私弹窗
        CheckShowPrivacyPanel();
    }

    /// <summary>
    /// 检查是否显示隐私弹窗（未选择过才显示）
    /// </summary>
    private void CheckShowPrivacyPanel()
    {
        if (PlayerPrefs.HasKey(PLAYERPREFS_PRIVACY_SELECTED))
        {
            // 用户已选择过，直接执行对应逻辑
            privacyPanel.SetActive(false);
            bool isAgreed = PlayerPrefs.GetInt(PLAYERPREFS_PRIVACY_AGREED) == 1;
            if (isAgreed)
            {
                privacyPanelEnd?.SetActive(true);
                TalkSdk.Instance.StartSdk();
            }
            else
            {
                privacyPanel?.SetActive(true);
            }
        }
        else
        {
            // 未选择过，显示弹窗
            privacyPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 同意隐私政策
    /// </summary>
    private void OnAgreePrivacy()
    {
        // 记录用户选择（持久化）
        PlayerPrefs.SetInt(PLAYERPREFS_PRIVACY_SELECTED, 1);
        PlayerPrefs.SetInt(PLAYERPREFS_PRIVACY_AGREED, 1);
        PlayerPrefs.Save();

        // 初始化友盟（正式采集数据）
        TalkSdk.Instance.StartSdk();

        // 关闭弹窗，进入游戏
        privacyPanel.SetActive(false);
        EnterGame();
    }

    /// <summary>
    /// 拒绝隐私政策
    /// </summary>
    private void OnRefusePrivacy()
    {
        // 记录用户选择（持久化）
        PlayerPrefs.SetInt(PLAYERPREFS_PRIVACY_SELECTED, 1);
        PlayerPrefs.SetInt(PLAYERPREFS_PRIVACY_AGREED, 0);
        PlayerPrefs.Save();
            
        // 禁用友盟所有功能


        // 处理拒绝逻辑（可选：退出游戏/仅禁用统计）
        RefuseHandler();
    }

    /// <summary>
    /// 打开隐私协议网页
    /// </summary>
    private void OnOpenAgreement()
    {
        // 调用系统浏览器打开隐私协议链接
        Application.OpenURL(PRIVACY_AGREEMENT_URL);
    }

    /// <summary>
    /// 进入游戏（同意后执行）
    /// </summary>
    private void EnterGame()
    {
        // 这里写游戏启动逻辑（如加载主界面、关闭启动页等）
        Debug.Log("用户同意隐私政策，进入游戏");
        privacyPanelEnd?.SetActive(true);
    }

    /// <summary>
    /// 拒绝隐私政策的处理（可选）
    /// </summary>
    private void RefuseHandler()
    {
        // 方案1：退出游戏（移动端）
        #if UNITY_ANDROID
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        activity.Call("finish");
        #elif UNITY_IPHONE
        Application.Quit();
        #endif

        // 方案2：仅禁用统计，仍允许玩游戏（注释上面代码，打开下面）
        // privacyPanel.SetActive(false);
        // Debug.Log("用户拒绝隐私政策，禁用友盟统计");
    }

    // 可选：提供重置隐私选择的方法（测试用）
    [ContextMenu("Reset Privacy Selection")]
    private void ResetPrivacySelection()
    {
        PlayerPrefs.DeleteKey(PLAYERPREFS_PRIVACY_SELECTED);
        PlayerPrefs.DeleteKey(PLAYERPREFS_PRIVACY_AGREED);
        Debug.Log("隐私选择已重置");
    }
}