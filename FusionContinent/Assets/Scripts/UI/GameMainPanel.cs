using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using QFramework;
using UnityEngine.U2D;
using DG.Tweening;
using FusionContinent.Manager;
using FusionContinent.Model;
using Game.Shop;
using Manager;
using UIFramework;
using UILevel = UIFramework.UILevel;
using UIManager = UIFramework.UIManager;


namespace FusionContinent
{
    public class GameMainPanelData : UIPanelData
    {
        public string DefaultButton = "Battle";
    }

    public partial class GameMainPanel : QFramework.UIPanel
    {
        // public ResLoader _resLoader;
        public static GameMainPanel Default;
        // public SpriteAtlas GameMainSpriteAtlas;
        // public SpriteAtlas EquipSpriteAtlas;
        public UIEntryController UIEntry;
        public Dictionary<QualityType, Color32> QualityTypeColors = new Dictionary<QualityType, Color32>();
        private bool _canClick = true;
        private float _clickCooldown = 0.3f; // 点击间隔（秒）
        private GameObject _confirmDialog;
        private bool _isAnimating = false;
        private Dictionary<string, string> GameMainName = new Dictionary<string, string>();
        private GameMainButtonType _currentPanelType = GameMainButtonType.Battle;
        
        // 当前显示的子界面（用于管理子界面生命周期）
        private UIFramework.UIPanel _currentSubPanel;

        private void Awake()
        {
            Default = this;
        }


        void RefreshScale()
        {
            if (Application.platform == RuntimePlatform.WindowsPlayer) return;
            
            var rate = 2460f / Screen.height;
            Bottom.localScale = Vector3.one  * rate;
        }

        private void Start()
        {
            // RefreshScale();
        }

        private new void OnDestroy()
        {
            Default = null;
            // Global.Publisher.UnsubscribeKeyAll(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED);
        }

        private Tween _animImage;
        private Tween _animLayout;
        private Button _defaultButton;
        Dictionary<GameMainButtonType,Button> buttomBtns = new Dictionary<GameMainButtonType, Button>();
        protected override void OnInit(QFramework.IUIData uiData = null)
        {
            mData = uiData as GameMainPanelData ?? new GameMainPanelData();
            UIEntry = GetComponent<UIEntryController>();
            SetUserInfo();
            InitMoneys();
            // AvatarBtn.onClick.AddListener(() =>
            // {
            //     UIKit.OpenPanel<SettingPanel>(QFramework.UILevel.PopUI);
            // });
            // 初始化稀有度颜色
            var qualityTypeColorList = DataConfigManager.Tables.TbGameBattleConst.QualityTypeColors;
            foreach (var v in qualityTypeColorList)
            {
                QualityTypeColors.Add(v.Type, new Color(v.Color.X / 255.0f, v.Color.Y / 255.0f, v.Color.Z / 255.0f));
            }
            buttomBtns.Clear();
            // 初始化主按钮
            var buttonList = DataConfigManager.Tables.TbGameConst.GameMainButtonList;
            for (int i = 0; i < Bottom.childCount; i++)
            {
                int idx = i;
                if (idx >= buttonList.Count) break;

                var v = buttonList[idx];
                Transform changeBtn = Bottom.GetChild(idx);
                changeBtn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    // 点击冷却中，禁止点击
                    if (!_canClick || _isAnimating)
                        return;

                    // 判断是否点击了当前已选中的按钮
                    if (_currentPanelType == v.Type)
                        return;
                    // 按钮类型
                    switch (v.Type)
                    {
                        // case  "Challenge":
                            // 挑战判断是否开启
                            // if (!CheckModuleFunction(ModuleFunctionType.BattleChallenge)) return;
                            // break;
                        case GameMainButtonType.Battle:
                            // 播放特效
                            var effectTran = changeBtn.Find("Layout/Img/MainInterfaceBattleIcon01");
                            if (effectTran != null)
                            {
                                effectTran.gameObject.SetActive(false);
                                effectTran.gameObject.SetActive(true);
                            }
                            break; 
                    }
                    
                    _canClick = false;
                    _isAnimating = true;
                    _currentPanelType = v.Type; // 记录当前激活的按钮类型

                    // 开始点击冷却计时
                    DOVirtual.DelayedCall(_clickCooldown, () => _canClick = true);

                    for (int j = 0; j < Bottom.childCount; j++)
                    {
                        if (idx != j)
                        {
                            Bottom.GetChild(j).Find("Layout/Img").localScale = Vector3.one * 0.7f;
                            Bottom.GetChild(j).Find("Layout").GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
                            Bottom.GetChild(j).Find("Bg/Select").gameObject.SetActive(false);
                            Bottom.GetChild(j).Find("Layout/Canvas/Text").gameObject.SetActive(true);
                        }
                    }

                    _animLayout.Kill();
                    _animLayout = DOVirtual.Float(0f, 20f, 0.5f, (a) =>
                    {
                        changeBtn.Find("Layout").GetComponent<RectTransform>().anchoredPosition = new Vector3(0, a, 0);
                    }).SetEase(Ease.OutQuad);

                    _animImage.Kill();
                    _animImage = DOVirtual.Float(0.7f, 1f, 0.2f, (a) =>
                    {
                        changeBtn.Find("Layout/Img").localScale = Vector3.one * a;
                    }).SetEase(Ease.OutQuad);

                    // 等待动画播放完毕再解除 _isAnimating
                    _animLayout.OnComplete(() => _isAnimating = false);

                    changeBtn.Find("Bg/Select").gameObject.SetActive(true);
                    changeBtn.Find("Layout/Canvas/Text").gameObject.SetActive(true);
                    // Stopwatch sw = new Stopwatch();
                    // sw.Start();
                    ChangePanel(v.Type);
                    // sw.Stop();
                    // FusionContinent.Toast.Show($"[UI Performance] {v.Type} 打开耗时: {sw.ElapsedMilliseconds} ms");
                });

                // 默认选中
                if (v.Type.Equals(Data.DefaultButton))
                {
                    _defaultButton = changeBtn.GetComponent<Button>();
                }
                buttomBtns.Add(v.Type,changeBtn.GetComponent<Button>());
                changeBtn.gameObject.SetActive(true);

                //暂时隐藏主界面的配卡界面
                if (i == Bottom.childCount - 1)
                {
                    changeBtn.gameObject.SetActive(false);
                }
            }
            
       
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, (enabledBtn) => {
                if (this == null) return; 
                transform.Find("Canvas/Bottom").gameObject.SetActive((bool)enabledBtn);
                transform.Find("Canvas/down").gameObject.SetActive((bool)enabledBtn);
            });
        }

        private void ChangePanel(GameMainButtonType type)
        {
            UIManager.HidePanel<ShopPanel01New>();
            UIFramework.UIManager.HidePanel<UnlockGuidePanelNew>();
            UIFramework.UIManager.ClosePanel<BigWorldPanelNew>();

            switch (type)
            {
                case GameMainButtonType.Battle:
                {
                    // 使用新的 UIFramework 打开主界面
                    var vm = new Game.MainInterface.MainInterfacePanelViewModel();
                    vm.OnCreate();
                    
                    UIFramework.UIManager.OpenPanelAsync<MainInterfacePanelNew, Game.MainInterface.MainInterfacePanelViewModel>(vm,UIFramework.UILevel.Common,null,(panel =>
                        {
                        }));
                    break;
                }
                case GameMainButtonType.Shop:
                    // 使用新框架打开商店
                    GotoShop();
                    break;
                case GameMainButtonType.Hero:
				{
                    OpenSoldierPanel();
                    break;
                }
                case GameMainButtonType.World:
                    {
                        var vm = new Game.Challenge.BigWorldPanelViewModel();
                        vm.OnCreate();
                        UIFramework.UIManager
                            .OpenPanelAsync<BigWorldPanelNew, Game.Challenge.BigWorldPanelViewModel>(vm);
                    }
                    break;
            }

            gameObject.SetActive(true);
            // 这里可以加载新场景、播放声音、激活UI等等
        }

        protected override void OnOpen(QFramework.IUIData uiData = null)
        {
            SetUpList();
            _canClick = true;
            _isAnimating = false;
            _currentPanelType = GameMainButtonType.Battle;
            if (_defaultButton != null)
            {
                _defaultButton.onClick.Invoke();
            }
        }

        protected override void OnShow()
        {
            // RefreshScale();
        }

        protected override void OnHide()
        {
        }

        public void OpenSoldierPanel(int soldierId = 0)
        {
            var vm = new Game.Soldier.SoldierPanelViewModel();
            vm.OnCreate();
            if (soldierId > 0)
            {
                vm.Initialize(soldierId);
            }

            UIFramework.UIManager.OpenPanelAsync<SoldierPanelNew, Game.Soldier.SoldierPanelViewModel>(vm,
                UIFramework.UILevel.PopUI);
        }

        public void GotoShop(ShopType shopType, int param = 0)
        {
            // Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
            // Bottom.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();
            
            
            // 使用 GetOrOpenPanel，自动处理复用
            // UIManager.GetOrOpenPanel<ShopPanel01New, Game.Shop.ShopPanel01ViewModel>()
            //     .SetLevel(UIFramework.UILevel.Common)
            //     .OnComplete(panel => {
            //         panel.ChangeShopPage(shopType, param);
            //     })
            //     .Open();
        }
        public void GotoShop()
        {
            // Global.Publisher.Publish(MessagePublisher.CHANGE_GAME_MAIN_FOOT_BTN_ENABLED, true);
            // Bottom.transform.GetChild(0).GetComponent<Button>().onClick.Invoke();

            // 使用 GetOrOpenPanel，自动处理复用
            UIManager.OpenPanelAsync<ShopPanel01New, Game.Shop.ShopPanel01ViewModel>(
                new Game.Shop.ShopPanel01ViewModel(), UIFramework.UILevel.Common);
        }



        private SoldierPanel GotoSoldierPanel()
        {
            Bottom.transform.GetChild(3).GetComponent<Button>().onClick.Invoke();
            var atlasPanel = UIKit.GetPanel<SoldierPanel>();
            return atlasPanel;
        }
        
        public BigWorldPanelNew GotoBigWorldPanel()
        {
            Bottom.transform.GetChild(4).GetComponent<Button>().onClick.Invoke();
            var vm = new Game.Challenge.BigWorldPanelViewModel();
            vm.OnCreate();
            BigWorldPanelNew resultPanel = null;
            UIFramework.UIManager.OpenPanelAsync<BigWorldPanelNew, Game.Challenge.BigWorldPanelViewModel>(vm,UILevel.Common,null,panel => resultPanel = panel);
            return resultPanel;
        }

        public void GotoSkipPath(SkipPathType type, string param = "")
        {
            switch (type)
            {
                case SkipPathType.Talent:
                    GotoBigWorldPanel();
                    UIKit.OpenPanel<TalentPanel>();
                    break;
                case SkipPathType.Appearance:
                    GotoBigWorldPanel();
                    break;
                case SkipPathType.GameGem:
                    GotoBigWorldPanel();
                    break;
                case SkipPathType.CardGroup:
                case SkipPathType.PlayerFigure:
                    Bottom.transform.GetChild(1).GetComponent<Button>().onClick.Invoke();
                    //这里可能存在bug
                    switch (type)
                    {
                        case SkipPathType.Talent:
                            UIKit.OpenPanel<TalentPanel>();
                            break;
                        case SkipPathType.PlayerFigure:
                            UIKit.OpenPanel<PlayerPanel>();
                            break;
                        default:
                            GlobalFunc.PreparePanel(Global.StageId.Value);
                            break;
                    }
                    break;
                
                case SkipPathType.Chest:
                    GotoShop(ShopType.Cardgroups, int.Parse(param));
                    break;
                
                case SkipPathType.BuyHero:
                    GotoShop(ShopType.Soldier, int.Parse(param));
                    break;
                
                case SkipPathType.AtlasSoldier:
                    GotoSoldierPanel();
                    break;
                    
                case SkipPathType.AtlasSkill:
                    GotoSoldierPanel();
                    break;
                case SkipPathType.HeroDev:
                    GotoSoldierPanel();
                    break;
                case SkipPathType.Home:
                 
                    switch (param)
                    {

                        default:
                            var mainPanel = UIKit.GetPanel<MainInterfacePanel>();
                            // GuideFlow.Instance.SetFinger(mainPanel.PlayGameBtn.gameObject);///主界面战斗按钮
                            break;
                    }
                    break;
                
                case SkipPathType.Equip:
                    // if (param.Equals("Gem"))
                    // {
                    //     GotoSoldierPanel().OpenEquipPanel().ChangeGemPage();
                    // }
                    // else
                    //     GotoSoldierPanel().EquipBtn.onClick?.Invoke();
                    break;
                
                case SkipPathType.FusionGemChallenge:
                    {
                        Bottom.transform.GetChild(4).GetComponent<Button>().onClick.Invoke();
                        var vm = new Game.Challenge.BigWorldPanelViewModel();
                        vm.OnCreate();
                        UIFramework.UIManager
                            .OpenPanelAsync<BigWorldPanelNew, Game.Challenge.BigWorldPanelViewModel>(vm);
                    }
                    break;
            }
        }

        protected override void OnClose()
        {
            // Global.ResLoader.Recycle2Cache();
            // Global.ResLoader = null;
        }

        /**
         * 显示通用确认框
         */
        public void ShowConfirm(string title, string message, Action onConfirm, Action onCancel = null)
        {
            
            DialogManager.ShowConfirm(title,message, onConfirm, onCancel);
        }
        
        
        /**
        * 通用UI条数效果
        */
        public void GoToUIEntry(List<RectTransform> uiObjects)
        {
            if (uiObjects.Count == 0) return;
            
            UIEntry.SetUIObjects(uiObjects);
            UIEntry.enabled = false;
            UIEntry.enabled = true;
        }
        
        public bool CheckModuleFunction(ModuleFunctionType type, bool isTip = true)
        {
            var tipStr = DataConfigManager.Tables.TbPlayerConst.ModuleFunctionUnlockTip;
            var mfCfg = DataConfigManager.Tables.TbModuleFunctionConfig.DataList.Find(ele=>ele.ModuleType == type);
            if (mfCfg != null && mfCfg.UnlockUserLv > Global.User.Level)
            {
                if (isTip) FusionContinent.Toast.Show(tipStr.Replace("[UserLv]", $"{mfCfg.UnlockUserLv}"), 1f);
                return false;
            }

            return true;
        }
        
        /**
         * 设置顶部资源列表
         *
         * @return
         * @author Zpp
         * @create 2025/10/28
         **/
        public void SetCanvasUpMoneyList(Dictionary<string, float> list = null)
        {
            if (list == null) return;

            foreach (var v in list)
            {
                var obj = GameObject.Find($"UIRoot/CanvasUpList/UpList/MoneyList/{v.Key}");
                if (obj == null) continue;
                obj.SetActive(v.Value != 0 ? true : false);
                // obj.transform.localScale = Vector3.one * v.Value;
            }
        }
        private void SetUserInfo()
        {
            if (Global.User.Avatar <= 0) Global.User.Avatar = UserData.DefaultAvatarId;

            // Avatar.sprite = Global.AvatarIconSpriteAtlas.GetSprite( "Head_"+Global.User.Avatar);
            // UserName.text = Global.User.UserName;
        }
        /**
                 * 设置顶部列表
                 */
        private void SetUpList()
        {
            // var upList = GameObject.Find("UIRoot/CanvasUpList");
            // if (upList == null)
            // {
            //     DontDestroyOnLoad(CanvasUpList);
            //     CanvasUpList.transform.SetParent(QFramework.UIRoot.Instance.transform);
            // }
            // else
            // {
            //     upList.SetActive(true);
            //     upList.transform.localScale = Vector3.one;
            //     GameMainPanel.Default?.SetCanvasUpMoneyList(new Dictionary<string, float>
            //     {
            //         { "GoldBox", 1 },
            //         { "StaminaBox", 1 },
            //         { "DiamondBox", 1 },
            //         { "PointCoupon", 0 }
            //     });
            // }
        }
        
        
        private void InitMoneys()
        {
            // 体力的消息订阅
            // PhysicalStrengthText.GetComponent<StaminaManager>().Init();
            // Global.Publisher.Subscribe(MessagePublisher.CHANGE_STAMINA, (stamina) =>
            // {
            //     if (PhysicalStrengthText != null)
            //     {
            //         LogTool.Log($"体力的消息订阅: {stamina}");
            //         PhysicalStrengthText.GetComponent<RollingNumberManager>().Play((int)stamina);
            //         PhysicalMaxLimitText.text = $"/{Global.PlayerResource.GetMaxStamina()}";
            //     }
            // }, Global.PlayerResource.Stamina);
            // StaminaBox.onClick.AddListener(() =>
            // {
            //     GameMainPanel.Default.GotoShop(ShopType.ItemMaterial);
            // });
            //
            // // 金币的消息订阅
            // var goldId = Global.ItemTypeIds[ItemType.Gold];
            // Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{goldId}", (gold) =>
            // {
            //     if (GoldText == null) return;
            //     LogTool.Log($"金币的消息订阅: {gold}");
            //     GoldText.GetComponent<RollingNumberManager>().Play((int)gold);
            // }, Global.GetItemNumById(goldId));
            // // 点卷的消息订阅
            // var couponId = Global.ItemTypeIds[ItemType.Coupon];
            // Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{couponId}", (coupon) =>
            // {
            //     if (PointCouponText == null) return;
            //     LogTool.Log($"金币的消息订阅: {coupon}");
            //     PointCouponText.GetComponent<RollingNumberManager>().Play((int)coupon);
            // }, Global.GetItemNumById(couponId));
            // GoldBox.onClick.AddListener(() =>
            // {
            //     GameMainPanel.Default.GotoShop(ShopType.ItemMaterial);
            // });
            //
            // // 钻石的消息订阅
            // var diamondId = Global.ItemTypeIds[ItemType.Diamond];
            // Global.Publisher.Subscribe($"{MessagePublisher.CHANGE_ITEM_NUM}{diamondId}", (diamonds) =>
            // {
            //     if (DiamondText == null) return;
            //     LogTool.Log($"宝石的消息订阅: {diamonds}");
            //     DiamondText.GetComponent<RollingNumberManager>().Play((int)diamonds);
            // }, Global.GetItemNumById(diamondId));
            // DiamondBox.GetComponent<Button>().onClick.AddListener(() =>
            // {
            //     GameMainPanel.Default.GotoShop(ShopType.Gem);
            // });
            //
            // // 等级消息订阅
            // var maxLevelCfg = DataConfigManager.Tables.TbPlayerLevelConfig.DataList.Last();
            // Global.Publisher.Subscribe(MessagePublisher.CHANGE_USER_LEVEL_INFO, (info) =>
            // {
            //     if (UserLevelText == null) return;
            //     
            //     // 玩家等级
            //     var levelInfo = (UserLevelInfo)info;
            //     UserLevelText.text = $"{levelInfo.Level}";
            //
            //     // 经验值
            //     var lvCfg = DataConfigManager.Tables.TbPlayerLevelConfig.GetOrDefault(levelInfo.Level);
            //     float progress = Mathf.Clamp01((float)levelInfo.Exp / (float)lvCfg.Exp);
            //     LevelExpText.text = levelInfo.Level >= maxLevelCfg.Lv ? "MAX" : $"{levelInfo.Exp}/{lvCfg.Exp}";
            //     LevelExpProgressBar.fillAmount = progress; // 确保填充范围在0到1之间
            // }, Global.User.GetLevelInfo());
        }

    }
}