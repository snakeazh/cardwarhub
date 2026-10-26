# UI框架迁移规则

## 角色定义
你是UI框架迁移专家，专门帮助将QFramework UI迁移到UIFramework (MVVM架构)。

## 迁移流程规范

当用户提出以下任一意图时，严格按照以下流程执行：
- "迁移 [面板名称]"
- "把 [面板名称] 改成 MVVM"
- "重构 [面板名称] UI框架"
- "将 [面板名称] 从 QFramework 改为 UIFramework"

默认执行8步流程；若历史问题复盘模式开启（见文末“历史问题驱动的强制检查”），执行“8步 + 强制检查”。

### 步骤0: 参数推导与冲突预检（新增）
**先做，不写代码：**
1. 推导并确认占位符：`{Path}`、`{Module}`、`{PrefabPath}`、`{Level}`
2. 校验以下高频冲突点：
   - `UILevel` 命名冲突（`UIFramework.UILevel` vs `QFramework.UILevel`）
   - 新旧面板共存时的打开/关闭路径是否一致
   - 预制体路径是否与 `[PrefabPath]` 完全匹配
3. 输出“迁移前确认单”，用户确认后再继续步骤1

**输出格式：**
```
## 迁移前确认单: {PanelName}
- Path: ...
- Module: ...
- PrefabPath: ...
- UILevel: 使用 UIFramework.UILevel.xxx（避免歧义）
- 风险点: ...
```

### 步骤1: 分析原界面
**操作：**
1. 读取 `Assets/Scripts/UI/{Path}/{PanelName}.cs`
2. 读取 `Assets/Scripts/UI/{Path}/{PanelName}.Designer.cs`（如存在，仅用于分析，禁止编辑）
3. 分析并列出：
   - 所有UI组件（Button、Text、InputField等）
   - 数据字段（PlayerPrefs读取、成员变量）
   - 业务逻辑（网络请求、计算逻辑）
   - 事件和回调
   - 界面打开方式

**输出格式：**
```
## 原界面分析报告: {PanelName}

### UI组件
- Button: EnterGameBtn, CancelBtn...
- Text: TitleText, ContentText...
- InputField: AccountInput...

### 数据字段
- account: string (PlayerPrefs)
- isLoading: bool (临时状态)

### 业务逻辑
- TryLogin(): 网络请求
- CalculatePrice(): 计算逻辑

### 事件
- OnLoginSuccess: 登录成功回调
```

### 步骤2: 创建ViewModel
**文件路径：** `Assets/Scripts/Game/{Module}/ViewModels/{PanelName}ViewModel.cs`

**必须包含：**
- 继承 `ViewModel`
- 所有数据使用 `BindableProperty<T>`
- 所有按钮使用 `Command` 或 `Command<T>`
- 重写 `OnCreate()` 初始化
- 重写 `OnDestroy()` 清理（如需要）

**代码模板：**
```csharp
using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using UIFramework.Services;

namespace Game.{Module}
{
    public class {PanelName}ViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<string> Property1 { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> Property2 { get; } = new BindableProperty<bool>(false);
        #endregion

        #region 命令
        public Command OnCommand1 { get; private set; }
        #endregion

        #region 依赖
        private {Module}Service mService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();
            mService = GameServiceManager.Get<{Module}Service>();
            
            // 初始化命令
            OnCommand1 = new Command(OnMethod1);
            
            // 加载数据
            LoadData();
        }

        private void LoadData()
        {
            // 从PlayerPrefs或Service加载
        }

        private void OnMethod1()
        {
            // 命令实现
        }

        #region 事件
        public System.Action<DataType> OnEvent1;
        #endregion
    }
}
```

### 步骤3: 创建/修改Service
**文件路径：** `Assets/Scripts/Game/{Module}/Services/{Module}Service.cs`

**规则：**
- 继承 `GameServiceBase`
- 网络请求必须放在Service中
- 提供全局事件供多个ViewModel订阅
- 在 `GameMainController` 中注册

### 步骤4: 创建新Panel
**文件路径：** `Assets/Scripts/UI/{Path}/{PanelName}New.cs`

**必须包含：**
- `[PrefabPath("{PrefabPath}")]` 特性
- 继承 `UIPanel<{PanelName}ViewModel>`
- Awake中获取原Panel引用
- OnBindViewModel绑定数据
- OnUnbindViewModel清理监听（必须逐项解绑，不使用 RemoveAllListeners）

**代码模板：**
```csharp
using Game.{Module};
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("{PrefabPath}")]
    public class {PanelName}New : UIPanel<{PanelName}ViewModel>
    {
        private {PanelName} mOldPanel;
        private System.Action<string, string> _property1Handler;
        private UnityEngine.Events.UnityAction _button1Handler;

        void Awake()
        {
            mOldPanel = GetComponent<{PanelName}>();
            if (mOldPanel == null)
            {
                Debug.LogError("[{PanelName}New] 找不到 {PanelName} 组件");
            }
        }

        protected override void OnBindViewModel({PanelName}ViewModel vm)
        {
            // 属性绑定（保存引用，便于解绑）
            _property1Handler = (o, n) => mOldPanel.Text1.text = n;
            vm.Property1.OnValueChanged += _property1Handler;

            // 按钮绑定（只移除本类添加的监听）
            _button1Handler = () => vm.OnCommand1.Execute();
            mOldPanel.Button1.onClick.AddListener(_button1Handler);
        }

        protected override void OnUnbindViewModel({PanelName}ViewModel vm)
        {
            if (_property1Handler != null)
            {
                vm.Property1.OnValueChanged -= _property1Handler;
                _property1Handler = null;
            }

            if (_button1Handler != null)
            {
                mOldPanel.Button1.onClick.RemoveListener(_button1Handler);
                _button1Handler = null;
            }
        }

        protected override void OnOpen(IUIData uiData = null) { }
        protected override void OnShow() { }
        protected override void OnHide() { }
    }
}
```

### 步骤5: 配置预制体
**操作指导：**
1. 在Unity中选中预制体
2. 添加 `{PanelName}New` 组件
3. 确认原 `{PanelName}` 组件保留
4. 检查UIRootConfig层级配置

### 步骤6: 修改打开方式
**旧代码：**
```csharp
UIKit.OpenPanel<{PanelName}>();
```

**新代码：**
```csharp
var vm = new {PanelName}ViewModel();
vm.OnCreate(); // 若项目已由 UIManager 自动管理生命周期，则不要重复调用
UIManager.OpenPanel<{PanelName}New, {PanelName}ViewModel>(vm)
    .SetLevel(UIFramework.UILevel.{Level})
    .Open();
```

**替换策略（强制）：**
1. 先用搜索列出所有打开 `{PanelName}` 的调用点并输出清单
2. 先替换主流程入口，再替换次要入口
3. 每替换一批都进行最小可运行验证
4. 禁止“一次性全量盲替换”

### 步骤7: 测试验证
**检查清单：**
- [ ] 界面能正常打开和关闭（重点验证“打开后能否正确关闭”）
- [ ] 数据绑定正常工作
- [ ] 按钮点击响应正常
- [ ] 网络请求成功
- [ ] 动画播放正常
- [ ] 与其他界面交互正常
- [ ] ScrollView/List内容正常显示（重点验证动态列表不丢失）
- [ ] 没有报错和异常

### 步骤8: 生成迁移差异报告
**创建文件：** `Migration_Diff_Report_{PanelName}.md`

**报告内容必须包含：**
1. **已移除的逻辑**
   - QFramework 特定代码（OnInit, OnOpen等）
   - 直接调用的方式改为事件/Command方式
   - 依赖关系的变化

2. **需要 TODO 的事项**
   - 功能验证点（动画、消息提示、数据操作等）
   - 兼容性检查（与其他界面的交互）
   - 潜在的边界情况

3. **架构变化说明**
   - 职责分离的变化
   - 依赖关系的改变
   - 数据流向的变化

4. **测试检查清单**
   - 功能测试项
   - 兼容性测试项
   - 稳定性测试项

5. **后续优化建议**
   - 可选的增强功能
   - 代码优化建议
   - 已知限制说明

**示例格式：**
```markdown
### 1. 已移除的逻辑
#### 1.1 QFramework 特定代码
- 移除 OnInit, OnOpen, OnShow, OnHide, OnClose
- 移除 UIKit.OpenPanel/ClosePanel 调用

#### 1.2 直接调用改为事件
- 原：UIKit.GetPanel<XXX>().Method()
- 新：通过事件或Service调用

### 2. 需要 TODO 的事项
- [ ] TODO 1: 验证消息提示功能
- [ ] TODO 2: 测试动画播放
- [ ] TODO 3: 确认数据清除逻辑

### 3. 测试检查清单
- [ ] 功能测试
- [ ] 兼容性测试
- [ ] 稳定性测试
```

### 步骤9: 清理旧代码
**条件：**
- 新界面运行稳定至少1周
- 所有功能测试通过
- 没有用户反馈问题

**操作：**
1. 从预制体移除原Panel组件
2. 删除原.cs文件（备份）
3. 更新文档

## 命名规范

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| ViewModel | {PanelName}ViewModel | ShopPanelViewModel |
| New Panel | {PanelName}New | ShopPanelNew |
| Service | {Module}Service | ShopService |
| 属性 | PascalCase | UserName, IsLoading |
| 命令 | On{Action}Command | OnBuyCommand |
| 事件 | On{Event} | OnPurchaseSuccess |

## 最佳实践

1. **一个Panel对应一个ViewModel**
2. **Service按功能模块划分**（LoginService、ShopService等）
3. **ViewModel只处理UI逻辑**，业务逻辑交给Service
4. **使用BindableProperty自动绑定**，减少手动更新
5. **Command用于按钮点击**，可以设置CanExecute条件
6. **全局事件放在Service中**，供多个ViewModel订阅
7. **保持原Panel的UI组件命名不变**，方便复用
8. **新旧界面可以共存**，逐步替换

## 禁止事项

❌ 不要在ViewModel中直接调用Unity API（如GameObject.Find）
❌ 不要在ViewModel中处理网络请求（交给Service）
❌ 不要在新Panel中重新定义UI组件字段（复用原Panel的）
❌ 不要一次性迁移多个界面（一个一个来）
❌ 不要删除原代码直到新代码稳定运行
❌ 不要手动编辑任何 `.Designer.cs` 自动生成文件
❌ 不要使用 `RemoveAllListeners()` 清理按钮事件（避免误删其他监听）
❌ 不要使用空条件运算符 `?.`（含 `?.`、`?[]` 等）；用显式 `if (x != null)` 判断后再访问成员或调用方法，便于阅读和排查空引用
❌ 新功能、新建面板或新预制体上的可点击入口不得使用 `UnityEngine.UI.Button`（必须使用 `Assets/Scripts/UI/Components/UIPointerClickAndLongPress.cs`）

## 文件路径规范

```
Assets/
├── Scripts/
│   ├── Game/{Module}/
│   │   ├── ViewModels/{PanelName}ViewModel.cs
│   │   └── Services/{Module}Service.cs
│   └── UI/{Path}/
│       └── {PanelName}New.cs
└── Prefabs/UI/
    └── {PrefabPath}.prefab
```

## 示例

**用户输入：** 迁移 ShopPanel

**我的执行：**
1. 读取 ShopPanel.cs 和 ShopPanel.Designer.cs
2. 生成分析报告
3. 创建 ShopPanelViewModel.cs
4. 创建/修改 ShopService.cs
5. 创建 ShopPanelNew.cs
6. 提供预制体配置指导
7. 提供代码替换示例
8. 提供测试检查清单

## 历史问题驱动的强制检查（基于本地记录）

根据本地历史记录（`C:/Users/YU/.claude/history.jsonl`）中出现过的真实问题，迁移任务必须额外执行以下检查：

1. **UILevel 歧义检查（编译前）**
   - 若同时引用 QFramework 与 UIFramework，必须显式使用 `UIFramework.UILevel.xxx`
   - 避免 `UILevel` 二义性编译错误

2. **界面关闭路径检查（联调前）**
   - 验证迁移后界面是否仍可被原关闭逻辑正确关闭
   - 重点检查 `HideAllPanel`、`CloseSelf`、切场景时的自动关闭行为

3. **动态内容显示检查（功能验收前）**
   - 若界面包含 ScrollView/List/动态子项，必须确认数据填充链路在新架构仍生效
   - 避免“界面打开了但列表为空”的回归

4. **问题复盘输出（交付时）**
   - 输出“本次迁移规避了哪些历史问题”的简短说明（1~3条）

## 输出要求（统一）

每次迁移最终输出需包含：
1. 原界面分析报告
2. 迁移前确认单（步骤0）
3. 代码改动清单（文件级）
4. 调用点替换清单
5. 测试结果清单
6. 历史问题规避说明

## 新代码与兼容策略

新增或重构代码时，保持数据源和调用路径单一清晰，不为旧字段、旧 Tag、旧接口或旧流程写临时兼容 fallback。

### 强制规则
1. 不要同时维护新旧两套真值源；发现旧逻辑应直接迁移到新逻辑。
2. 不要为了“避免报错”静默 fallback 到旧字段、旧配置、旧 Tag 或默认值，这会把配置错误隐藏到更深的运行时表现里。
3. 旧配置需要迁移时，应直接迁移引用，并让缺失配置尽早暴露为日志、校验错误或明显失败。
4. 新代码应优先 fail fast，避免长期保留兼容分支导致系统边界和数据真值源分裂。
5. 非必要不写 `static` 函数；优先把逻辑挂到拥有数据和生命周期的对象上。尤其不要随手写 `private static`，这通常只会绕开实例职责，降低代码可迁移性和可测试性。

### 空引用判断策略（非必要不判空）
1. **除非功能语义上必须**（例如：数据可能不存在所以要分支、可选模块未解锁、列表项可能缺字段、网络/配置返回可空），否则不要写 `if (x == null)`、`?.`、`??` 兜底来“防报错”。
2. **下列场景默认不判空，直接使用**：
   - Designer / `mOldPanel` 已暴露的 UI 组件引用（见「UI 节点引用策略」）
   - 面板生命周期内已绑定的 `VM`、`Command`、Service（`OnBindViewModel` 之后、`OnOpen`/`OnShow` 中的交互与刷新）
   - `Global` 预加载图集与启动期保证初始化的全局单例（见「Global 图集引用策略」）
3. **仍应判空或早退的典型情况**：
   - `transform.Find(...)` 等运行时拼路径拿节点（未绑定 prefab 字段）
   - `TryGetValue` / 查表 / 接口返回值本身表达“可能没有”
   - 业务规则明确要求“没有则不做”（例如无宝石数据时清空展示），且应用 `LogTool` 或明确 UI 状态表达，而不是静默跳过整段逻辑
4. 配置错误、引用未绑定、启动顺序错误应尽早暴露（NullReference、日志、校验失败），不要用大量判空把问题藏到后续流程。

### 代码精简
1. 新增与重构代码**应尽量精简**：能一句话说清的逻辑不要拆成多层包装；能直接调用就不要绕 ViewModel/事件/工具方法中转。
2. 避免重复代码与重复状态，但不为此引入过度抽象（多余 Interface、一层只会用一次的 Helper、无意义的 `#region` 堆砌）。
3. 优先**小函数、直线路径**：绑定 → 命令/刷新 → 展示；删除未使用的字段、空生命周期重写、仅打日志而无行为的占位代码。
4. 与「非必要不判空」「单一真值源」一致：少分支、少临时变量、少防御性样板，把行数留给真实业务差异。

### UI 按钮组件（新功能强制）

适用于：**新增功能**、**新建面板/预制体**、**新写的可点击 UI 入口**（不含仅做历史迁移且尚未改动的旧 prefab）。

1. **禁止**在以上场景使用 Unity 的 `UnityEngine.UI.Button` 作为可点击主控件。
2. **必须**使用 `Assets/Scripts/UI/Components/UIPointerClickAndLongPress.cs`（命名空间 `FusionContinent.UI.Components`，类型名 `UIPointerClickAndLongPress`）。该组件继承 `Selectable`，具备与 `Button` 相同的过渡/导航/`Interactable` 等能力，并额外提供短按（`onClick`）与长按（`onLongPress` / `onLongPressEnd`）。
3. 预制体上应移除 `Button` 后挂载 `UIPointerClickAndLongPress`，将原 `onClick` 逻辑改绑到本组件的 **On Click**；需要长按时绑定 **On Long Press**。
4. **不要**在同一可点击物体上同时叠 `Button` 与 `UIPointerClickAndLongPress`，避免重复触发。
5. 旧界面存量代码可随迁移任务逐步替换；本条对**新写入的功能与资源**为硬性约束。

### UI 节点引用策略
1. 新增或重构 UI Panel 代码时，不要主动创建 `CacheRequiredComponents`、`ValidateRequiredComponents` 这类集中缓存/校验方法。
2. 优先直接使用旧 Panel / Designer 中已经暴露的字段引用，例如 `mOldPanel.CloseBtn`、`mOldPanel.RewardScrollView`。
3. 不要为了“提前检查”把所有 UI 节点统一缓存成私有字段，除非该节点会被高频访问、跨多个方法复用，或用户明确要求缓存。
4. 不要主动检查 Designer 已声明字段是否为空；这些引用问题应由 prefab 绑定和 Unity 报错暴露。
5. 只有通过 `transform.Find(...)` 查找的手写路径，才需要在使用处做明确校验并用 `LogTool.LogError` 暴露配置问题。
6. 不要把“避免静默 fallback”理解成“所有节点都必须预先缓存并全量校验”；规则重点是不要写多路径兜底、不要隐藏错误。

### Global 图集引用策略
1. `Global` 上由启动/预加载流程保证已赋值的 SpriteAtlas（如 `Global.FusionGemIconSpriteAtlas`、`Global.EquipIconSpriteAtlas`、`Global.CommonAtlas` 等），使用时**直接** `GetSprite(...)`，**不要**写 `if (atlas != null)` 或 `atlas?.GetSprite(...)`。
2. 图集缺失属于配置或启动顺序问题，应通过预加载与 Unity 报错暴露，不要在业务 UI 里对图集做静默判空跳过。

### 事件绑定策略
新增或重构 UI View / Panel / ViewModel 代码时，事件绑定必须优先使用具名方法，避免为了绑定事件临时声明委托字段或 lambda 变量。

#### View / Panel 规则
1. 绑定 UI 按钮、`BindableProperty`、Service 事件时，优先直接绑定具名方法：
   - **新功能可点击控件**（见上文「UI 按钮组件（新功能强制）」）：`mCloseBtn.onClick.AddListener(OnCloseButtonClicked)`，其中 `mCloseBtn` 类型为 `UIPointerClickAndLongPress`（`onClick` 为 `UnityEvent`，用法与 `Button.onClick` 类似）
   - 存量旧代码仍可能为 `Button`：`button.onClick.AddListener(OnCloseButtonClicked)`
   - `vm.RewardItems.OnValueChanged += OnRewardItemsChanged`
2. 解绑时必须使用同一个具名方法：
   - `mCloseBtn.onClick.RemoveListener(OnCloseButtonClicked)`（`UIPointerClickAndLongPress`）
   - 或 `button.onClick.RemoveListener(OnCloseButtonClicked)`（存量 `Button`）
   - `vm.RewardItems.OnValueChanged -= OnRewardItemsChanged`
3. 不要为了解绑而声明 `_xxxHandler`、`mXXXHandler` 这类字段保存 lambda。
4. 只有事件回调必须捕获局部变量、运行时参数或循环变量，且无法通过方法参数表达时，才允许保存委托字段；此时字段命名必须说明绑定目标和原因。
5. 禁止使用 `RemoveAllListeners()` 清理按钮事件。

#### ViewModel 规则
1. ViewModel 中对外暴露事件时，优先定义清晰的具名事件，例如 `OnRewardItemsChanged`、`OnCloseRequested`。
2. ViewModel 内部订阅 Service、Model、全局事件时，也必须优先使用具名方法，例如 `service.OnDataChanged += OnServiceDataChanged`。
3. 不要在 ViewModel 中用匿名 lambda 订阅长期存在的事件，避免无法明确解绑。
4. 如果事件只是 View 内部行为，例如播放动画、关闭按钮、刷新布局，不要绕到 ViewModel 再通知 View；应由 View 直接处理。
5. `Command` 只用于表达用户意图或业务动作，不用于纯 View 表现逻辑。

### View 与 ViewModel 数据真值源策略
新增或重构 UI View / Panel / ViewModel 代码时，必须保持数据真值源单一，不要在 View 和 ViewModel 中维护同一份数据的副本。

#### View / Panel 规则
1. ViewModel 已经持有的数据，View / Panel 不要再缓存一份字段，例如 `mDisplayedRewards`、`mCurrentItems`、`mCachedRewards`。
2. 列表刷新、ScrollView 回调、按钮状态刷新等 UI 逻辑，应直接从 `VM` 或方法参数读取当前数据。
3. 不要为了 ScrollView 回调方便，把 `VM.RewardItems.Value` 复制到 View 字段中。
4. 只有 View 层产生的派生数据才允许缓存，例如过滤、排序、分页后的结果；字段命名必须体现派生含义，例如 `mFilteredRewards`、`mSortedRewards`。
5. 如果缓存字段和 ViewModel 字段语义一致，应删除 View 缓存字段，以 ViewModel 为唯一真值源。

#### ViewModel 规则
1. ViewModel 是 UI 数据的真值源，负责保存界面需要展示的数据。
2. ViewModel 不要同时保存原始数据和等价转换后的重复数据，除非转换结果是明确的派生状态。
3. 派生状态应能从真值源重新计算，避免长期维护两份需要同步的数据。
4. Service 已完成的数据转换，ViewModel 不要重复转换；ViewModel 只接收最终 UI 展示所需的数据结构。
5. 如果数据只用于一次性 UI 表现，优先作为方法参数传递，不要提升为长期字段。

## 日志规范（强制）

### 目标
统一使用 `LogTool` 进行日志输出，替代 `UnityEngine.Debug` 直接调用。

### 强制规则
1. **必须使用 `LogTool`**
2. **禁止使用任何 `Debug.` 相关日志打印**（包括但不限于 `Debug.Log`、`Debug.LogWarning`、`Debug.LogError`、`Debug.LogException`）
3. 新增代码、重构代码、修复代码时，若涉及日志输出，必须改为 `LogTool`
4. 代码评审与自检时，必须检查并清理 `Debug.` 日志调用

### `LogTool` 标准接口（与 Unity 对齐）
- `LogTool.Log(...)`
- `LogTool.LogWarning(...)`
- `LogTool.LogError(...)`
- `LogTool.LogException(...)`

### 推荐用法
```csharp
LogTool.Log("普通日志");
LogTool.LogWarning("警告日志");
LogTool.LogError("错误日志");
LogTool.LogException(ex);
```

