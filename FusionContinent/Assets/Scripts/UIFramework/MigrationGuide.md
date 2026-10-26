# QFramework UI 迁移到 UIFramework 完整指南

## 目录
1. [框架对比](#框架对比)
2. [迁移优势](#迁移优势)
3. [准备工作](#准备工作)
4. [逐步迁移步骤](#逐步迁移步骤)
5. [代码迁移示例](#代码迁移示例)
6. [注意事项](#注意事项)
7. [常见问题](#常见问题)

---

## 框架对比

| 特性 | QFramework (旧) | UIFramework (新) |
|------|-----------------|------------------|
| 架构模式 | 传统MVC | MVVM + Service |
| 异步支持 | Task/Callback | UniTask (零GC) |
| 数据绑定 | 手动更新 | BindableProperty自动绑定 |
| 资源加载 | 内置管理 | YooAssets集成 |
| 层级管理 | 硬编码 | Inspector配置化 |
| 数据持久化 | Panel内保存 | Service层统一管理 |
| 代码耦合 | 高耦合 | 低耦合，可测试 |

---

## 迁移优势

### 1. **性能提升**
- UniTask 替代 Task，无GC Alloc
- YooAssets 资源管理更高效
- 自动数据绑定减少手动更新

### 2. **代码质量**
- ViewModel/Service 可单元测试
- UI逻辑与业务逻辑分离
- 更好的代码复用性

### 3. **开发效率**
- Inspector配置层级，无需改代码
- 属性自动绑定，减少样板代码
- 构建器模式链式调用

### 4. **维护性**
- 数据结构统一在Service层
- ViewModel是Proxy，数据不会丢失
- 支持热重载和动态更新

---

## 准备工作

### 1. 确认项目结构
确保以下文件夹存在：
```
Assets/
├── Scripts/
│   ├── UIFramework/          # 新框架（已创建）
│   ├── Game/
│   │   └── Login/            # 登录模块（已创建）
│   │       ├── DataModels/
│   │       ├── ViewModels/
│   │       └── Services/
│   └── UI/Login1/            # 旧登录界面
├── Prefabs/UI/               # UI预制体
```

### 2. 场景配置
1. 创建 **UIRoot** 空物体
2. 挂载 **UIRoot** 脚本（新框架）
3. 挂载 **UIRootConfig** 脚本
4. 在UIRootConfig中配置层级（Bg, Common, PopUI, Guide等）

### 3. 初始化代码
在游戏入口添加：
```csharp
// GameMainController.cs 或类似的启动脚本
async void Start()
{
    // 1. 初始化UI框架
    UIManager.PathProvider = new ReflectionPrefabPathProvider("UI/Prefabs");
    UIManager.Initialize();
    
    // 2. 初始化Service系统
    await GameServiceManager.InitializeAsync();
    
    // 3. 注册Service
    GameServiceManager.Register(new LoginService());
    
    // 4. 打开登录界面
    OpenLoginPanel();
}
```

---

## 逐步迁移步骤

### 步骤1：创建ViewModel（对应原Panel的数据逻辑）

**旧代码（QFramework）：**
```csharp
// LoginPanel01.cs 中混合了UI和数据逻辑
public partial class LoginPanel01 : UIPanel
{
    private string account;
    private string password;
    
    void OnInit() 
    {
        account = PlayerPrefs.GetString("Account");
        // ... 其他初始化
    }
}
```

**新代码（分离到ViewModel）：**
```csharp
// Game/Login/ViewModels/LoginPanel01ViewModel.cs
public class LoginPanel01ViewModel : ViewModel
{
    public BindableProperty<string> Account { get; } = new BindableProperty<string>("");
    public BindableProperty<string> Password { get; } = new BindableProperty<string>("");
    
    public override void OnCreate()
    {
        // 从Service或本地读取数据
        Account.Value = PlayerPrefs.GetString("Account", "");
    }
}
```

### 步骤2：创建Service（对应原Panel的业务逻辑）

**旧代码：**
```csharp
// LoginPanel01.cs
void TryLogin(Dictionary<string, string> formData)
{
    // 直接发送请求
    var response = await WebDataFun.SendPostRequest(ApiKeys.ApiLogin, formData);
    // 处理响应...
}
```

**新代码（分离到Service）：**
```csharp
// Game/Login/Services/LoginService.cs
public class LoginService : GameServiceBase
{
    public async UniTask<LoginPlayer> TryLoginAsync(string account, string password)
    {
        var formData = new Dictionary<string, string>
        {
            { "Account", account },
            { "Password", password }
        };
        
        string response = await WebDataFun.SendPostRequest(ApiKeys.ApiLogin, formData);
        return JsonConvert.DeserializeObject<LoginPlayer>(response);
    }
}
```

### 步骤3：创建新Panel类（使用新框架）

**操作步骤：**
1. 复制原预制体，或直接在原预制体上添加新脚本
2. 创建新脚本继承 `UIPanel<TViewModel>`
3. 在Awake中复制老脚本的组件引用
4. 实现 `OnBindViewModel` 和 `OnUnbindViewModel`

**代码模板：**
```csharp
[PrefabPath("LoginPanel01")]  // 指定预制体路径
public class LoginPanelNew : UIPanel<LoginPanel01ViewModel>
{
    // 引用老脚本，直接复用其字段
    private LoginPanel01 mOldPanel;
    
    void Awake()
    {
        mOldPanel = GetComponent<LoginPanel01>();
        if (mOldPanel == null)
        {
            Debug.LogError("找不到老界面组件！");
            return;
        }
    }
    
    protected override void OnBindViewModel(LoginPanel01ViewModel vm)
    {
        // 绑定属性
        vm.Account.OnValueChanged += (oldVal, newVal) =>
        {
            mOldPanel.AccountField.text = newVal;  // 直接访问老界面字段
        };
        
        // 绑定按钮
        mOldPanel.LoginBtn.onClick.AddListener(() => vm.OnLoginCommand.Execute());
    }
    
    protected override void OnUnbindViewModel(LoginPanel01ViewModel vm)
    {
        // 清理监听
        mOldPanel.LoginBtn.onClick.RemoveAllListeners();
    }
}
```

### 步骤4：修改预制体

**方法一：双脚本共存（推荐过渡方案）**
1. 在预制体上同时挂载老脚本和新脚本
2. 新脚本Awake中自动获取老脚本的引用
3. 运行时新脚本接管逻辑

**方法二：直接替换（完成迁移后）**
1. 删除老脚本
2. 新脚本的字段用`[SerializeField]`标记
3. 在Inspector中重新绑定UI组件

### 步骤5：修改打开方式

**旧代码：**
```csharp
// 打开界面
UIKit.OpenPanel<LoginPanel01>();

// 关闭界面
UIKit.ClosePanel<LoginPanel01>();
```

**新代码：**
```csharp
// 创建ViewModel
var viewModel = new LoginPanel01ViewModel();
viewModel.OnCreate();

// 打开界面（同步）
UIManager.OpenPanel<LoginPanelNew, LoginPanel01ViewModel>(viewModel)
    .SetLevel(UILevel.Common)
    .SetData(vm => { /* 初始化数据 */ })
    .OnComplete(panel => { /* 打开完成回调 */ })
    .Open();

// 打开界面（异步）
var panel = await UIManager.OpenPanel<LoginPanelNew, LoginPanel01ViewModel>(viewModel)
    .SetLevel(UILevel.Common)
    .OpenAsync();

// 关闭界面
UIManager.ClosePanel<LoginPanelNew>();
// 或在Panel内部调用：CloseSelf();
```

---

## 代码迁移示例

### 完整迁移：LoginPanel01

#### 1. 原代码结构
```csharp
// LoginPanel01.cs (QFramework)
public partial class LoginPanel01 : UIPanel
{
    [SerializeField] public Button EnterGameBtn;
    [SerializeField] public TMP_Dropdown ServiceDropdown;
    // ... 其他字段
    
    protected override void OnInit(IUIData uiData = null)
    {
        // 1. 初始化数据
        var account = PlayerPrefs.GetString("Account");
        
        // 2. 绑定按钮事件
        EnterGameBtn.onClick.AddListener(() => {
            TryLogin();
        });
        
        // 3. 初始化服务器列表
        InitService();
    }
    
    private async void TryLogin()
    {
        // 直接在这里处理登录逻辑
    }
}
```

#### 2. 迁移后结构

**ViewModel:**
```csharp
// Game/Login/ViewModels/LoginPanel01ViewModel.cs
public class LoginPanel01ViewModel : ViewModel
{
    public BindableProperty<string> Account { get; } = new BindableProperty<string>("");
    public BindableProperty<bool> IsEnterGameBtnEnabled { get; } = new BindableProperty<bool>(true);
    public Command OnEnterGameCommand { get; private set; }
    
    private LoginService mLoginService;
    
    public override void OnCreate()
    {
        mLoginService = GameServiceManager.Get<LoginService>();
        
        // 初始化数据
        Account.Value = PlayerPrefs.GetString("Account", "");
        
        // 初始化命令
        OnEnterGameCommand = new Command(OnEnterGame, () => IsEnterGameBtnEnabled.Value);
    }
    
    private async void OnEnterGame()
    {
        IsEnterGameBtnEnabled.Value = false;
        var result = await mLoginService.TryLoginAsync(Account.Value, Password.Value);
        // 处理结果...
        IsEnterGameBtnEnabled.Value = true;
    }
}
```

**Service:**
```csharp
// Game/Login/Services/LoginService.cs
public class LoginService : GameServiceBase
{
    public async UniTask<LoginPlayer> TryLoginAsync(string account, string password)
    {
        // 网络请求逻辑
    }
    
    public List<ServerData> GetServerList()
    {
        // 返回服务器列表
    }
}
```

**New Panel:**
```csharp
// UI/Login1/LoginPanelNew.cs
[PrefabPath("LoginPanel01")]
public class LoginPanelNew : UIPanel<LoginPanel01ViewModel>
{
    private LoginPanel01 mOldPanel;
    
    void Awake()
    {
        mOldPanel = GetComponent<LoginPanel01>();
    }
    
    protected override void OnBindViewModel(LoginPanel01ViewModel vm)
    {
        // 数据绑定
        vm.Account.OnValueChanged += (old, val) => mOldPanel.AccountText = val;
        vm.IsEnterGameBtnEnabled.OnValueChanged += (old, val) => 
            mOldPanel.EnterGameBtn.interactable = val;
        
        // 事件绑定
        mOldPanel.EnterGameBtn.onClick.AddListener(() => vm.OnEnterGameCommand.Execute());
    }
}
```

---

## 注意事项

### 1. 数据持久化
- **旧框架**：数据存储在Panel中，Panel关闭数据可能丢失
- **新框架**：数据存储在Service中，Panel只是展示层
- **迁移建议**：将PlayerPrefs操作移到ViewModel或Service

### 2. 异步处理
- **旧框架**：使用async/void或回调
- **新框架**：使用UniTask，更安全
- **注意**：UniTask不能用在普通Task的上下文中

### 3. 生命周期
- **OnCreate**：ViewModel创建时调用（不是MonoBehaviour的Awake）
- **OnBindViewModel**：Panel初始化完成，绑定数据
- **OnUnbindViewModel**：Panel关闭前，清理监听

### 4. 资源路径
- 使用`[PrefabPath("路径")]`特性标记
- 或配置`UIManager.PathProvider`
- 支持运行时反射查找路径

### 5. 层级配置
- 在UIRootConfig中配置各层级
- UILevel枚举值就是SortingOrder
- 不要在代码中硬编码层级

---

## 常见问题

### Q1: 迁移后界面打不开？
**检查：**
1. UIManager.Initialize()是否调用？
2. UIRootConfig是否正确配置？
3. PrefabPath是否正确？
4. ViewModel.OnCreate()是否调用？

### Q2: 数据绑定不生效？
**检查：**
1. 是否使用了BindableProperty？
2. OnBindViewModel中是否正确订阅事件？
3. OnUnbindViewModel中是否清理了订阅？

### Q3: 老界面的组件找不到？
**检查：**
1. Awake中是否正确获取老脚本？`GetComponent<OldPanel>()`
2. 预制体上是否同时挂载了新老脚本？
3. 脚本执行顺序是否正确？

### Q4: 如何逐步迁移？
**建议顺序：**
1. 先迁移独立的、简单的界面
2. 保持老界面可用，新老共存
3. 测试通过后再删除老界面
4. 最后迁移核心复杂界面

### Q5: 新老框架能混用吗？
**可以：**
- 新框架打开的面板可以与旧框架打开的面板共存
- Service可以被新老框架同时使用
- 逐步替换，不影响现有功能

### Q6: 性能有提升吗？
**是的：**
- UniTask替代Task，无GC
- BindableProperty按需更新
- YooAssets资源管理更高效
- Service层缓存数据，减少重复请求

---

## 快速检查清单

迁移每个界面时检查：

- [ ] 创建对应的ViewModel
- [ ] 创建对应的Service（如果有业务逻辑）
- [ ] 创建新的Panel类继承UIPanel<T>
- [ ] 添加[PrefabPath]特性
- [ ] 在Awake中获取老界面引用
- [ ] 实现OnBindViewModel绑定数据
- [ ] 实现OnUnbindViewModel清理监听
- [ ] 修改打开方式为UIManager.OpenPanel
- [ ] 测试数据绑定是否正常
- [ ] 测试打开关闭是否正常

---

## 总结

迁移的核心是**分离关注点**：
1. **View（UI）**：负责显示，尽可能薄
2. **ViewModel**：负责UI逻辑，数据绑定
3. **Service**：负责业务逻辑，数据持久化

遵循MVVM原则，代码会更清晰、可测试、易维护。

如有问题，请参考：
- `Assets/Scripts/UIFramework/Documentation.md`
- `Assets/Scripts/UIFramework/GameServiceGuide.md`
- 示例代码：`LoginPanelNew.cs`, `LoginAccountPanelNew.cs`
