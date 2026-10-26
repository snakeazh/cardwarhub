using System.Collections;
using System.Collections.Generic;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using Game.Login;
using QFramework;
using UnityEngine;
using UIManager = UIFramework.UIManager;

public class GameStartScene : MonoBehaviour
{
    // Start is called before the first frame update
    IEnumerator Start()
    {
        
        if (Global.User.UserId == 0)
        {
            // if (GameObject.Find("UIRoot/CanvasUpList") != null)
            // {
            //     Destroy(GameObject.Find("UIRoot/CanvasUpList").gameObject);
            // }
            var loginVm = new LoginPanel01ViewModel();
            loginVm.OnCreate();
            yield return UIManager.OpenPanelAsync<LoginPanelNew, LoginPanel01ViewModel>(
                loginVm,
                UIFramework.UILevel.Common).ToCoroutine();
            // UIKit.OpenPanel<LoginPanel01>();
        }
        else
        {
            // 检查新框架的加载面板
            var loadingPanel = UIFramework.UIManager.GetPanel<FusionContinent.LoadingPanelNew>();
            if (loadingPanel != null)
            {
                yield return new WaitUntil(() => { return !loadingPanel.gameObject.activeInHierarchy; });
            }
            else
            {

            }

            var id = Global.StageId.Value;
            var stageConfig = DataConfigManager.Tables.TbStageBattleConfig.Get(id);
            if (stageConfig != null && stageConfig.StageType == StageType.GreaterRift)
            {
                // 使用新框架打开主界面
                var vm = new Game.GameMain.GameMainPanelViewModel();
                vm.OnCreate();
                // TODO: 设置默认按钮为 "Challenge"
                
                yield return UIFramework.UIManager.OpenPanelAsync<GameMainPanelNew, Game.GameMain.GameMainPanelViewModel>(
                    vm,
                    UIFramework.UILevel.Const,
                    onComplete: panel =>
                    {
                        // TODO: 调用 OpenWorld 方法
                        // panel.OpenWorld();
                    }).ToCoroutine();
                
                // TODO: 打开 RealmRoomNew
                // UIFramework.UIManager.GetPanel<BigWorldPanelNew>()?.OpenRealmRoomPanel();
                Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
                yield break;
            }
            
            // 使用新框架打开主界面
            var mainVm = new Game.GameMain.GameMainPanelViewModel();
            mainVm.OnCreate();
            
            yield return UIFramework.UIManager.OpenPanelAsync<GameMainPanelNew, Game.GameMain.GameMainPanelViewModel>(
                mainVm,
                UIFramework.UILevel.Const).ToCoroutine();
            yield return null;

        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
