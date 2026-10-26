# UIFramework 迁移规则包

## 📦 包含内容

这个规则包帮助你将 QFramework UI 迁移到 UIFramework (MVVM架构)。

### 文件结构
```
项目根目录/
├── AGENTS.md                          # 迁移规则说明（自动加载）
├── .opencode/
│   ├── ui-migration.json              # 规则配置
│   └── templates/                     # 代码模板
│       ├── ViewModel.cs.template
│       ├── PanelNew.cs.template
│       └── Service.cs.template
└── Tools/
    └── ui_migration_helper.py         # 自动化脚本
```

## 🚀 快速开始

### 方式1：直接对话（推荐）

在 opencode 中直接说：
```
迁移 ShopPanel
```

AI 会自动按照 AGENTS.md 中的 8 步流程执行：
1. 分析原界面
2. 创建 ViewModel
3. 创建/修改 Service
4. 创建新 Panel
5. 配置预制体
6. 修改打开方式
7. 测试验证
8. 清理旧代码

### 方式2：使用自动化脚本

```bash
# 生成代码框架
python Tools/ui_migration_helper.py ShopPanel Shop UI/Shop

# 参数说明:
#   ShopPanel     - 面板名称
#   Shop          - 模块名称
#   UI/Shop       - 预制体路径（可选，默认使用面板名）
```

脚本会生成：
- `Assets/Scripts/Game/Shop/ViewModels/ShopPanelViewModel.cs`
- `Assets/Scripts/Game/Shop/Services/ShopService.cs`
- `Assets/Scripts/UI/Shop/ShopPanelNew.cs`

### 方式3：使用模板手动创建

复制模板文件并替换变量：
- `${PanelName}` - 面板名称
- `${Module}` - 模块名称
- `${PrefabPath}` - 预制体路径
- `${Date}` - 当前日期

## 📋 迁移检查清单

每个界面迁移时，按以下步骤检查：

### 步骤1: 分析原界面
- [ ] 读取原 Panel 的 .cs 文件
- [ ] 列出所有 UI 组件（Button、Text、InputField 等）
- [ ] 识别数据字段（PlayerPrefs 读取、成员变量）
- [ ] 识别业务逻辑（网络请求、计算等）
- [ ] 识别事件和回调

### 步骤2: 创建 ViewModel
- [ ] 创建 `{PanelName}ViewModel.cs`
- [ ] 定义 `BindableProperty<T>` 属性
- [ ] 定义 `Command` 命令
- [ ] 实现 `OnCreate()` 初始化
- [ ] 实现 `OnDestroy()` 清理

### 步骤3: 创建/修改 Service
- [ ] 创建或修改 `{Module}Service.cs`
- [ ] 迁移业务逻辑到 Service
- [ ] 添加全局事件（如需要）
- [ ] 在 `GameMainController` 中注册 Service

### 步骤4: 创建新 Panel
- [ ] 创建 `{PanelName}New.cs`
- [ ] 添加 `[PrefabPath]` 特性
- [ ] 在 `Awake` 中获取原 Panel 引用
- [ ] 实现 `OnBindViewModel` 绑定数据
- [ ] 实现 `OnUnbindViewModel` 清理监听

### 步骤5: 配置预制体
- [ ] 在 Unity 中选中原预制体
- [ ] 添加新 Panel 组件
- [ ] 确认原 Panel 组件保留
- [ ] 检查 UIRootConfig 层级配置

### 步骤6: 修改打开方式
- [ ] 搜索所有打开该界面的代码
- [ ] 替换为新框架打开方式

**旧代码：**
```csharp
UIKit.OpenPanel<ShopPanel>();
```

**新代码：**
```csharp
var vm = new ShopPanelViewModel();
vm.OnCreate();
UIManager.OpenPanel<ShopPanelNew, ShopPanelViewModel>(vm)
    .SetLevel(UILevel.PopUI)
    .Open();
```

### 步骤7: 测试验证
- [ ] 界面能正常打开和关闭
- [ ] 数据绑定正常工作（输入、显示）
- [ ] 按钮点击响应正常
- [ ] 网络请求成功
- [ ] 动画播放正常
- [ ] 与其他界面交互正常
- [ ] 没有报错和异常

### 步骤8: 清理旧代码
- [ ] 新界面运行稳定至少 1 周
- [ ] 从预制体移除原 Panel 组件
- [ ] 删除原 .cs 文件（备份）

## 🎯 命名规范

| 类型 | 命名规则 | 示例 |
|------|----------|------|
| ViewModel | {PanelName}ViewModel | ShopPanelViewModel |
| New Panel | {PanelName}New | ShopPanelNew |
| Service | {Module}Service | ShopService |
| 属性 | PascalCase | UserName, IsLoading |
| 命令 | On{Action}Command | OnBuyCommand |
| 事件 | On{Event} | OnPurchaseSuccess |

## 📁 文件路径规范

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

## 💡 最佳实践

1. **一个 Panel 对应一个 ViewModel**
2. **Service 按功能模块划分**（LoginService、ShopService 等）
3. **ViewModel 只处理 UI 逻辑**，业务逻辑交给 Service
4. **使用 BindableProperty 自动绑定**，减少手动更新
5. **Command 用于按钮点击**，可以设置 CanExecute 条件
6. **全局事件放在 Service 中**，供多个 ViewModel 订阅
7. **保持原 Panel 的 UI 组件命名不变**，方便复用
8. **新旧界面可以共存**，逐步替换

## ⚠️ 注意事项

### 禁止事项
❌ 不要在 ViewModel 中直接调用 Unity API（如 GameObject.Find）  
❌ 不要在 ViewModel 中处理网络请求（交给 Service）  
❌ 不要在新 Panel 中重新定义 UI 组件字段（复用原 Panel 的）  
❌ 不要一次性迁移多个界面（一个一个来）  
❌ 不要删除原代码直到新代码稳定运行  

### 常见问题

**Q: 新旧框架能混用吗？**  
A: 可以！新老界面可以互相打开、共存，逐步替换。

**Q: 数据会丢失吗？**  
A: 不会。数据存储在 Service 层，ViewModel 只是代理。

**Q: 需要修改预制体吗？**  
A: 只需要添加新脚本，原脚本的组件绑定保持不变。

**Q: 迁移一个界面需要多久？**  
A: 简单界面 30 分钟，复杂界面 1-2 小时。

## 📚 相关文档

- `AGENTS.md` - 详细迁移规则（自动加载到 AI 上下文）
- `UIFramework/MigrationGuide.md` - 完整迁移指南
- `UIFramework/QuickStart.md` - 快速开始
- `UIFramework/Documentation.md` - 框架使用文档

## 🔧 自定义配置

可以修改 `.opencode/ui-migration.json` 来自定义：
- 模板路径
- 输出目录
- 命名规范
- 文件路径

## 🤝 贡献

如果你改进了迁移流程或添加了新功能，欢迎提交 PR！

## 📄 许可证

MIT License
