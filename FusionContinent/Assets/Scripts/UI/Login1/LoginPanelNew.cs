using System;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent.Model;
using FusionContinent.Sounds;
using Game.Login;
using Manager;
using QFramework;
using UnityEngine;
using FusionContinent.Utils;
using Newtonsoft.Json;
using UIFramework;
using UIFramework.Services;
using IUIData = UIFramework.IUIData;
using UILevel = UIFramework.UILevel;
using UIManager = UIFramework.UIManager;
using Cardwar;
using Game.RedDot;
using Game.RedDot.Business;
using Game.RedDot.View;

namespace FusionContinent
{
    /// <summary>
    /// 登录界面 - 使用新 UIFramework (MVVM版本)
    /// 复用原 LoginPanel01 的 UI 组件
    /// 单机化：隐藏选服下拉与切换账号入口
    /// </summary>
    [PrefabPath("LoginPanel01")]  // 使用属性标签指定预制体路径
    public class LoginPanelNew : UIPanel<LoginPanel01ViewModel>
    {

        private LoginService mLoginService;
        private LoginPanel01 panel;
        void Awake()
        {
            // 从同物体上的 LoginPanel01 复制 UI 组件引用
            panel = GetComponent<LoginPanel01>();
        
        }

        protected override void OnBindViewModel(LoginPanel01ViewModel vm)
        {
            // 获取服务
            mLoginService = GameServiceManager.Get<LoginService>();

            // 绑定属性到 UI
            BindProperties(vm);
            
            // 绑定命令
            BindCommands(vm);
            
            // 初始化 UI 状态
            InitializeUI(vm);
            
            // 绑定 ViewModel 事件
            vm.OnLoginSuccess += OnLoginSuccess;
            vm.OnOpenLanguagePanel += OnOpenLanguagePanel;
            vm.OnOpenCancelPanel += OnOpenCancelPanel;
        }

        protected override void OnUnbindViewModel(LoginPanel01ViewModel vm)
        {
            // 取消事件订阅
            vm.OnLoginSuccess -= OnLoginSuccess;
            vm.OnOpenLanguagePanel -= OnOpenLanguagePanel;
            vm.OnOpenCancelPanel -= OnOpenCancelPanel;
            
            // 清理按钮监听
            if (panel.EnterGameBtn != null) panel.EnterGameBtn.onClick.RemoveAllListeners();
            if (panel.LanguageChangeBtn != null) panel.LanguageChangeBtn.onClick.RemoveAllListeners();
            if (panel.CancelPanelBtn != null) panel.CancelPanelBtn.onClick.RemoveAllListeners();
            if (panel.Toggle != null) panel.Toggle.onValueChanged.RemoveAllListeners();
        }

        private void BindProperties(LoginPanel01ViewModel vm)
        {
            // 版本号
            vm.Version.OnValueChanged += (oldVal, newVal) =>
            {
                if (panel.VersionNum != null) panel.VersionNum.text = newVal;
            };

            // 登录按钮可用状态
            vm.IsEnterGameBtnEnabled.OnValueChanged += (oldVal, newVal) =>
            {
                if (panel.EnterGameBtn != null) panel.EnterGameBtn.interactable = newVal;
            };

            // 错误提示
            vm.ErrorMessage.OnValueChanged += (oldVal, newVal) =>
            {
                if (!string.IsNullOrEmpty(newVal))
                {
                    ShowTip(newVal);
                }
            };

            // Toggle 状态
            vm.IsToggleOn.OnValueChanged += (oldVal, newVal) =>
            {
                if (panel.Toggle != null && panel.Toggle.isOn != newVal)
                {
                    panel.Toggle.isOn = newVal;
                }
            };
        }

        private void BindCommands(LoginPanel01ViewModel vm)
        {
            if (panel.EnterGameBtn != null)
            {
                panel.EnterGameBtn.onClick.AddListener(() => vm.OnEnterGameCommand.Execute());
            }

            if (panel.LanguageChangeBtn != null)
            {
                panel.LanguageChangeBtn.onClick.AddListener(() => vm.OnLanguageChangeCommand.Execute());
            }

            if (panel.CancelPanelBtn != null)
            {
                panel.CancelPanelBtn.onClick.AddListener(() => vm.OnCancelAccountCommand.Execute());
            }

            if (panel.Toggle != null)
            {
                panel.Toggle.onValueChanged.AddListener(isOn => vm.OnToggleChangeCommand.Execute(isOn));
            }
        }

        private void InitializeUI(LoginPanel01ViewModel vm)
        {
            // 设置初始值
            if (panel.VersionNum != null) panel.VersionNum.text = vm.Version.Value;
            if (panel.EnterGameBtn != null) panel.EnterGameBtn.interactable = vm.IsEnterGameBtnEnabled.Value;
            if (panel.Toggle != null) panel.Toggle.isOn = vm.IsToggleOn.Value;

            // 单机化：隐藏选服下拉与切换账号入口（prefab 未改，仅代码隐藏）
            panel.ServiceDropdown.gameObject.SetActive(false);
            panel.SelectServerLabel.gameObject.SetActive(false);
            panel.AccountChangeBtn.gameObject.SetActive(false);
        }

        // private SexType defaultSexType = SexType.Man; 
        public async UniTask CreateNamePlayer(Action callback = null)
        {
            Global.Player.FigureData = DataConfigManager.Tables.TbPlayerConst.InitManFigures;//默认为男性装扮
            try
            {
                var sex = (int)0;
                var figure = Global.Player.FigureData;
                // FusionContinent.Toast.Show("操作中....");
                var req = new SavePlayerRequest
                {
                    UserName = "User_" + Global.Player.Id,
                    Sex = sex,
                    Figures = JsonConvert.SerializeObject(figure),
                    IsCreate = true,
                };

                var proto = await WebDataFun.SendProtoPostRequest<SavePlayerResponse>(ApiKeys.ApiPlayerSavePlayer, req);

                if (proto == null)
                {
                    return;
                }

                if (proto.User != null)
                {
                    var local = ProtoMappers.MapUser(proto.User);
                    if (local != null)
                    {
                        if (mLoginService != null)
                        {
                            mLoginService.ApplyUserData(local);
                        }
                        else
                        {
                            Global.UID = local.UserId;
                            Global.User = local;
                        }
                    }
                }

                if (proto.Player != null)
                {
                    var model = ProtoMappers.MapUserPlayer(proto.Player);
                    if (model != null)
                    {
                        Global.Player = model;
                    }
                }

                if (callback != null) callback();
            }
            catch (System.Exception ex)
            {
                LogTool.LogError($"操作失败：{ex.Message}");
                FusionContinent.Toast.Show($"操作失败：{ex.Message}");
            }
            finally
            {
            }
        }
        private async void OnLoginSuccess(LoginPlayer loginPlayer)
        {
            // 全屏面板实例级预载（登录成功后后台进行；原在启动期 GameStartMono 中，会拖慢进登录界面）
            // SoldierPanelNew 以 PopUI 层级打开，不预加载
            UIManager.PreloadPanelsAsync(new System.Collections.Generic.List<System.Type>
            {
                typeof(MainInterfacePanelNew),
                typeof(ShopPanel01New),
                typeof(CardGroupPanelNew),
            }).Forget();

            // 初始化红点管理器
            GameServiceManager.Get<RedDotService>();
            RedDotBusinessSetup.RegisterAll(RedDotService.Instance);
            new GameObject("UnlockGuideManager", typeof(UnlockGuideManager));
            if (!mLoginService.CheckGuideComplete(GuideType.SetFigure))
            {
                // 未设置形象，打开创建角色界面（MVVM）
                 await CreateNamePlayer();
            }
            if (!mLoginService.CheckGuideComplete(GuideType.GuideStage))
            {
                var guideStageId = DataConfigManager.Tables.TbPlayerConst.GuidesStageId;
                if (guideStageId == 0)
                {
                    LogTool.LogError("操作失败：未配置新手引导关卡ID");
                    return;
                }
                // GlobalFunc.PreparePanel(guideStageId);
                Global.StageId.Value = guideStageId;
                await GlobalFunc.StartBattle(Global.StageId.Value, () =>
                {
                }, (err) =>
                {
                });
            }
            // 检查引导状态，决定打开哪个界面
            else 
            {
                // 已设置形象，进入主界面（新框架）
                var vm = new Game.GameMain.GameMainPanelViewModel();
                vm.OnCreate();
                
                await UIManager.OpenPanelAsync<GameMainPanelNew, Game.GameMain.GameMainPanelViewModel>(
                    vm,
                    UILevel.Const);
                SoundManager.Instance.PlayBgm(SoundType.SoundMainBGM);
            }
            
            // 关闭登录界面
            // CloseSelf();
        }

        private void OnOpenLanguagePanel()
        {
            // 打开语言切换面板（新框架方式）
            var viewModel = new LanguageChangeViewModel();
            viewModel.OnCreate();

            UIManager.OpenPanelAsync<LanguageChangePanelNew, LanguageChangeViewModel>(viewModel, UILevel.PopUI).Forget();
        }

        private void OnOpenCancelPanel()
        {
            // 打开注销面板（新框架方式）
            var vm = new CancelAccountViewModel();
            vm.OnCreate();

            UIManager.OpenPanelAsync<CancelAccountPanelNew, CancelAccountViewModel>(vm, UILevel.PopUI).Forget();
        }

        private void ShowTip(string message)
        {
            FusionContinent.Toast.Show(message);
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            // 面板打开时的逻辑
        }

        protected override void OnShow()
        {
            // 面板显示时的逻辑
        }

        protected override void OnHide()
        {
            // 面板隐藏时的逻辑
        }
    }
}
