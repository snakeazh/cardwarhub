using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.SceneManagement;

namespace FusionContinent
{
    public class MainPanelData : UIPanelData
    {
    }

    public partial class MainPanel : UIPanel
    {
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as MainPanelData ?? new MainPanelData();
            // please add init code here

            ContinueGameBtn.onClick.AddListener(() =>
            {
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                UIKit.ClosePanel<MainPanel>();
                GamePlayManager.Default.StartGame();
            });
            NewGameBtn.onClick.AddListener(() =>
            {
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                DifficultyBtn.gameObject.SetActive(true);
                SettingsBtn.gameObject.SetActive(false);
                ChooseMenu.gameObject.SetActive(false);
            });

            DifficultyBtn.onClick.AddListener(() =>
            {
                UIKit.ClosePanel<MainPanel>();
                UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
                SceneManager.LoadScene("GamePlay");
                Global.Difficulty.Value = 1;
                // GamePlay.gamePlay.Init();
            });

            ExitBtn.onClick.AddListener(() =>
            {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>().Play();
#else
                Application.Quit();
#endif
            });
        }

        public void ChangeContinueBtn(bool isOver)
        {
            ContinueGameBtn.gameObject.SetActive(isOver);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            DifficultyBtn.gameObject.SetActive(false);
            SettingsBtn.gameObject.SetActive(false);
            ChooseMenu.gameObject.SetActive(true);
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