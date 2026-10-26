# UI框架迁移快速操作指南

## 🚀 3分钟快速开始

### 1️⃣ 场景配置（一次性）
```
1. 创建空物体命名为 "UIRoot"
2. 挂载脚本：UIRoot（单例） + UIRootConfig（配置）
3. 在UIRootConfig中拖拽配置各层级Canvas
```

### 2️⃣ 游戏入口代码（一次性）
```csharp
void Start()
{
    // 设置路径提供器（使用反射读取[PrefabPath]标签）
    UIManager.PathProvider = new ReflectionPrefabPathProvider("UI/Prefabs");
    
    // 初始化UI框架
    UIManager.Initialize();
    
    // 初始化Service
    GameServiceManager.InitializeAsync().Forget();
    
    // 打开登录界面（新框架方式）
    var vm = new LoginPanel01ViewModel();
    vm.OnCreate();
    
    UIManager.OpenPanel<LoginPanelNew, LoginPanel01ViewModel>(vm)
        .SetLevel(UILevel.Common)
        .Open();
}
```

### 3️⃣ 迁移单个界面（以LoginPanel01为例）

#### 第1步：创建ViewModel
创建文件：`Assets/Scripts/Game/Login/ViewModels/LoginPanel01ViewModel.cs`
```csharp
using UIFramework;
using Game.Login;

public class LoginPanel01ViewModel : ViewModel
{
    // 1. 定义属性（自动绑定）
    public BindableProperty<string> Account { get; } = new BindableProperty<string>("");
    public BindableProperty<bool> IsLoginBtnEnabled { get; } = new BindableProperty<bool>(true);
    
    // 2. 定义命令
    public Command OnLoginCommand { get; private set; }
    
    private LoginService mService;
    
    public override void OnCreate()
    {
        mService = GameServiceManager.Get<LoginService>();
        
        // 初始化数据
        Account.Value = PlayerPrefs.GetString("Account", "");
        
        // 初始化命令
        OnLoginCommand = new Command(OnLogin);
    }
    
    private async void OnLogin()
    {
        IsLoginBtnEnabled.Value = false;
        var result = await mService.TryLoginAsync(Account.Value, "");
        IsLoginBtnEnabled.Value = true;
    }
}
```

#### 第2步：创建新Panel
创建文件：`Assets/Scripts/UI/Login1/LoginPanelNew.cs`
```csharp
using Game.Login;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("LoginPanel01")]  // ← 标记预制体路径
    public class LoginPanelNew : UIPanel<LoginPanel01ViewModel>
    {
        // 引用老界面，复用其UI组件
        private LoginPanel01 mOld;
        
        void Awake()
        {
            mOld = GetComponent<LoginPanel01>();  // ← 获取老界面
        }
        
        protected override void OnBindViewModel(LoginPanel01ViewModel vm)
        {
            // 属性绑定
            vm.Account.OnValueChanged += (o, n) => mOld.AccountText = n;
            vm.IsLoginBtnEnabled.OnValueChanged += (o, n) => mOld.EnterGameBtn.interactable = n;
            
            // 按钮绑定
            mOld.EnterGameBtn.onClick.AddListener(() => vm.OnLoginCommand.Execute());
        }
        
        protected override void OnUnbindViewModel(LoginPanel01ViewModel vm)
        {
            // 清理监听
            mOld.EnterGameBtn.onClick.RemoveAllListeners();
        }
    }
}
```

#### 第3步：修改预制体
```
1. 选中 LoginPanel01 预制体
2. 添加组件：LoginPanelNew（新脚本）
3. 保留老脚本：LoginPanel01（用于复用UI组件）
4. 不需要修改任何Inspector绑定，新脚本会自动复用
```

#### 第4步：修改打开代码
```csharp
// 旧方式（QFramework）
UIKit.OpenPanel<LoginPanel01>();

// 新方式（UIFramework）
var vm = new LoginPanel01ViewModel();
vm.OnCreate();

UIManager.OpenPanel<LoginPanelNew, LoginPanel01ViewModel>(vm)
    .SetLevel(UILevel.Common)
    .Open();
```

---

## 📋 迁移检查清单

对于每个要迁移的界面，按以下步骤：

- [ ] **创建 ViewModel**（在Game/XXX/ViewModels/下）
  - [ ] 继承ViewModel
  - [ ] 定义BindableProperty属性
  - [ ] 定义Command命令
  - [ ] 重写OnCreate()初始化

- [ ] **创建新 Panel**（在UI/XXX/下）
  - [ ] 继承UIPanel<TViewModel>
  - [ ] 添加[PrefabPath]标签
  - [ ] Awake中获取老界面引用
  - [ ] 实现OnBindViewModel绑定数据
  - [ ] 实现OnUnbindViewModel清理

- [ ] **预制体设置**
  - [ ] 同时挂载新老脚本
  - [ ] 确认层级配置正确

- [ ] **修改调用代码**
  - [ ] 创建ViewModel
  - [ ] 使用UIManager.OpenPanel打开

---

## 🔧 快速复制模板

### ViewModel模板
```csharp
using UIFramework;
using UnityEngine;

namespace Game.XXX
{
    public class XXXViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<string> Property1 { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> Property2 { get; } = new BindableProperty<bool>(false);
        #endregion

        #region 命令
        public Command OnCommand1 { get; private set; }
        public Command<int> OnCommand2 { get; private set; }
        #endregion

        public override void OnCreate()
        {
            // 初始化命令
            OnCommand1 = new Command(OnMethod1);
            OnCommand2 = new Command<int>(OnMethod2);
            
            // 初始化数据
            LoadData();
        }

        private void LoadData()
        {
            // 加载数据
        }

        private void OnMethod1()
        {
            // 命令实现
        }

        private void OnMethod2(int param)
        {
            // 带参数命令实现
        }

        #region 事件
        public System.Action OnEvent1;
        public System.Action<DataType> OnEvent2;
        #endregion
    }
}
```

### Panel模板
```csharp
using Game.XXX;
using UIFramework;
using UnityEngine;

namespace FusionContinent
{
    [PrefabPath("PrefabName")]
    public class XXXPanelNew : UIPanel<XXXViewModel>
    {
        private OldPanel mOld;
        
        void Awake()
        {
            mOld = GetComponent<OldPanel>();
        }

        protected override void OnBindViewModel(XXXViewModel vm)
        {
            // 属性绑定
            vm.Property1.OnValueChanged += (o, n) => mOld.Text1.text = n;
            
            // 按钮绑定
            mOld.Button1.onClick.AddListener(() => vm.OnCommand1.Execute());
        }

        protected override void OnUnbindViewModel(XXXViewModel vm)
        {
            mOld.Button1.onClick.RemoveAllListeners();
        }

        protected override void OnOpen(IUIData uiData = null) { }
        protected override void OnShow() { }
        protected override void OnHide() { }
    }
}
```

### 打开方式模板
```csharp
// 方式1: 同步打开
var vm = new XXXViewModel();
vm.OnCreate();
UIManager.OpenPanel<XXXPanelNew, XXXViewModel>(vm)
    .SetLevel(UILevel.Common)
    .SetData(v => { v.Property1.Value = "初始值"; })
    .OnComplete(panel => Debug.Log("打开完成"))
    .Open();

// 方式2: 异步打开
var panel = await UIManager.OpenPanel<XXXPanelNew, XXXViewModel>(vm)
    .SetLevel(UILevel.Common)
    .OpenAsync();

// 方式3: 隐式转换
UniTask<XXXPanelNew> task = UIManager.OpenPanel<XXXPanelNew, XXXViewModel>(vm);
var panel = await task;
```

---

## 🐛 常见问题速查

### 问题1: "找不到老界面组件"
**解决：** 确保预制体上同时挂载了新老脚本，且老脚本名正确

### 问题2: "数据绑定不生效"
**解决：** 
1. 检查BindableProperty是否正确定义
2. 检查OnBindViewModel中是否正确订阅
3. 检查OnUnbindViewModel是否清理了订阅

### 问题3: "打开界面失败"
**解决：**
1. UIManager.Initialize()是否调用
2. UIRoot场景配置是否正确
3. PrefabPath标签是否正确
4. ViewModel.OnCreate()是否调用

### 问题4: "如何混用新老框架"
**答：** 完全可以，新老界面可以互相打开、共存，逐步替换

---

## 📚 详细文档

更多详情参考：
- 完整迁移指南：`Assets/Scripts/UIFramework/MigrationGuide.md`
- 框架使用文档：`Assets/Scripts/UIFramework/Documentation.md`
- Service架构：`Assets/Scripts/UIFramework/GameServiceGuide.md`

## 💡 迁移建议

1. **小步快跑**：每次只迁移一个界面，测试通过后再迁移下一个
2. **先易后难**：先迁移简单的界面（如设置、帮助），再迁移复杂的（如主界面、战斗）
3. **保留备份**：迁移前保存老代码，方便回滚
4. **持续测试**：每迁移一个界面都完整测试功能

---

**祝你迁移顺利！** 🎉
