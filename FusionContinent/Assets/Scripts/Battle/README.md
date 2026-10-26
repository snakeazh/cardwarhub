# 战斗场景架构使用指南

## 架构概览

```
BattleScene (战斗场景)
├── BattleSceneLifecycle (生命周期管理)
├── BattleServiceManager (服务管理)
│   ├── BattleTimeService
│   ├── BattleCityLevelService
│   └── ...
└── GameLandUI (UI根节点)
    └── BattlePanelNew (主界面，SubPanelManager)
        ├── BattleTimePanelNew (时间管理子界面)
        ├── CityLevelPanelNew (城市等级子界面)
        ├── SchoolPanelNew (流派子界面)
        └── ... (其他子界面并存)
```

## PVE / PVP 双车道

GameMap / GameLand / GameLandUI 下 `pve`·`pvp` 子树切换、Lane 挂点、`opponentInfo` 对侧数据：

- [Arena/README_PvePvpArena.md](./Arena/README_PvePvpArena.md) — 场景结构、进局流程、刷兵与检查清单
- [Arena/README_PvpOpponentInfo.md](./Arena/README_PvpOpponentInfo.md) — `api/pvp/opponentInfo` 与 `OpponentBattleContext`

## 特点

- **多个子界面并存**：可以同时显示时间管理、城市等级、流派等多个子界面
- **独立于 UIManager**：战斗界面使用独立的 SubPanelManager，不参与 UIManager 堆栈
- **自动生命周期**：进入战斗场景自动初始化，退出自动清理

## 快速开始

### 1. 在战斗场景中设置

1. 创建空物体，命名为 "BattleSceneManager"
2. 添加 `BattleSceneLifecycle` 组件
3. 配置参数：
   - `Auto Initialize`: 勾选（场景加载时自动初始化）
   - `Auto Cleanup On Destroy`: 勾选（场景销毁时自动清理）

### 2. 在 GameLandUI 上添加 BattlePanelNew

```csharp
// 在 GameLandUI 的 Awake 或 Start 中
void Start()
{
    // 添加 BattlePanelNew 组件
    var battlePanel = gameObject.AddComponent<BattlePanelNew>();
    
    // 创建并初始化 ViewModel
    var vm = new BattlePanelViewModel();
    vm.OnCreate();
    
    // 通过反射或接口设置 ViewModel（BattlePanelNew 继承 UIPanel）
    // 实际上 BattlePanelNew 会自动处理
}
```

### 3. 使用战斗场景Service

```csharp
// 在子界面中获取 Service
var timeService = BattleServiceManager.Instance.GetService<BattleTimeService>();

// 在 ViewModel 中使用
public class MyViewModel : ViewModel
{
    private BattleTimeService mTimeService;
    
    public override void OnCreate()
    {
        mTimeService = BattleServiceManager.Instance?.GetService<BattleTimeService>();
    }
}
```

### 4. 退出战斗场景

```csharp
// 方式1：通过 BattleSceneLifecycle
var lifecycle = FindObjectOfType<BattleSceneLifecycle>();
lifecycle.ExitBattleScene("MainScene");

// 方式2：通过 BattlePanelNew
FindObjectOfType<BattlePanelNew>()?.VM?.OnExitBattleCommand.Execute();
```

## 创建新的战斗子界面

### 步骤1：创建子界面 ViewModel

```csharp
using UIFramework;
using UnityEngine;

namespace Game.Battle
{
    public class MyBattleSubViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<int> MyData { get; } = new BindableProperty<int>(0);
        #endregion

        #region 命令
        public Command OnMyCommand { get; private set; }
        #endregion

        #region 依赖
        private BattleTimeService mTimeService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mTimeService = BattleServiceManager.Instance?.GetService<BattleTimeService>();
            OnMyCommand = new Command(OnMyAction);
        }

        private void OnMyAction()
        {
            // 业务逻辑
        }
    }
}
```

### 步骤2：创建子界面 Panel

```csharp
using Game.Battle;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    /// <summary>
    /// 我的战斗子界面
    /// </summary>
    public class MyBattleSubPanelNew : UIPanel<MyBattleSubViewModel>
    {
        // 设置为 Unmanaged，作为子界面
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        // 引用 GameLandUI 的组件
        private GameLandUI mGameLandUI;

        void Awake()
        {
            mGameLandUI = GameLandUI.Default;
        }

        protected override void OnBindViewModel(MyBattleSubViewModel vm)
        {
            // 绑定数据
            vm.MyData.OnValueChanged += (oldVal, newVal) =>
            {
                // 更新UI
            };

            // 绑定按钮
            if (mGameLandUI?.MyButton != null)
            {
                mGameLandUI.MyButton.onClick.AddListener(() => vm.OnMyCommand.Execute());
            }
        }

        protected override void OnUnbindViewModel(MyBattleSubViewModel vm)
        {
            // 解绑
        }

        protected override void OnOpen(IUIData uiData = null) 
        {
            gameObject.SetActive(true);
        }

        protected override void OnHide()
        {
            gameObject.SetActive(false);
        }
    }
}
```

### 步骤3：在 BattlePanelNew 中注册子界面

```csharp
// BattlePanelNew.cs
private void ShowSubPanel(BattleSubPanelType panelType, BattlePanelViewModel vm)
{
    switch (panelType)
    {
        case BattleSubPanelType.Time:
            ShowTimePanel();
            break;
        case BattleSubPanelType.MySubPanel:  // 新增
            ShowMySubPanel();
            break;
        // ...
    }
}

private void ShowMySubPanel()
{
    var myVm = new MyBattleSubViewModel();
    myVm.OnCreate();
    
    CreateLocalSubPanel<MyBattleSubPanelNew, MyBattleSubViewModel>(
        myVm,
        (panel) =>
        {
            Debug.Log("[BattlePanelNew] MyBattleSubPanelNew 创建完成");
        }
    );
}
```

### 步骤4：添加到枚举

```csharp
// BattlePanelViewModel.cs
public enum BattleSubPanelType
{
    Time,
    CityLevel,
    School,
    SkillCard,
    CombatInfo,
    Settings,
    MySubPanel,  // 新增
}
```

## 创建新的战斗场景Service

### 步骤1：创建Service类

```csharp
using UIFramework.Services;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Battle
{
    public class MyBattleService : GameServiceBase
    {
        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            // 初始化逻辑
        }
        
        // 业务逻辑...
        
        /// <summary>
        /// 重写OnDestroy进行清理
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();
            
            // 清理事件监听
            // 重置状态
            // 释放资源
        }
    }
}
```

### 步骤2：在 BattleServiceManager 中注册

```csharp
// BattleServiceManager.cs - RegisterBattleServices 方法
private void RegisterBattleServices()
{
    mBattleServices.Clear();
    
    mBattleServices.Add(new BattleTimeService());
    mBattleServices.Add(new BattleCityLevelService());
    mBattleServices.Add(new MyBattleService());  // 添加新Service
    
    Debug.Log($"[BattleServiceManager] 注册了 {mBattleServices.Count} 个战斗场景Service");
}
```

## 注意事项

1. **不要在 GameStartMono 中注册战斗场景Service**
   - 战斗场景Service由 BattleServiceManager 管理
   - 全局Service才在 GameStartMono 中注册

2. **必须重写 OnDestroy() 方法**
   - 清理所有事件监听
   - 重置所有状态
   - 释放所有资源引用

3. **使用事件通信**
   - 避免直接引用其他Service
   - 使用事件机制解耦

4. **生命周期管理**
   - 战斗场景加载时自动初始化
   - 战斗场景退出时自动清理
   - 无需手动管理Service生命周期

## 示例：城市等级UI绑定

```csharp
using Game.Battle;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CityLevelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text mLevelText;
    [SerializeField] private TMP_Text mExpText;
    [SerializeField] private Slider mExpSlider;
    
    private BattleCityLevelService mService;
    
    void Start()
    {
        mService = BattleServiceManager.Instance?.GetService<BattleCityLevelService>();
        if (mService != null)
        {
            // 订阅事件
            mService.OnLevelUp += OnLevelUp;
            mService.OnExpChanged += OnExpChanged;
            
            // 初始化显示
            UpdateUI();
        }
    }
    
    void OnDestroy()
    {
        if (mService != null)
        {
            mService.OnLevelUp -= OnLevelUp;
            mService.OnExpChanged -= OnExpChanged;
        }
    }
    
    void OnLevelUp(int newLevel)
    {
        mLevelText.text = $"Lv.{newLevel}";
        UpdateUI();
    }
    
    void OnExpChanged(int newExp)
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (mService == null) return;
        
        mLevelText.text = $"Lv.{mService.CurrentLevel}";
        mExpText.text = $"{mService.CurrentExp}/{mService.RequiredExp}";
        mExpSlider.value = mService.ExpProgress;
    }
}
```
