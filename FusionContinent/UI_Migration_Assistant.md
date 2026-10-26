# UI迁移助手 - 交互式指南

## 🎯 使用方法

直接告诉我：**"迁移 [面板名称]"**

例如：
- `迁移 ShopPanel`
- `迁移 InventoryPanel`
- `迁移 SettingsPanel`

我会按照标准流程帮你完成迁移。

---

## 📋 标准迁移流程（8步）

### 步骤1: 分析原界面
**我会帮你：**
- 读取原 Panel 的 .cs 文件
- 列出所有 UI 组件
- 识别数据字段和业务逻辑
- 生成分析报告

**你需要提供：**
- 面板名称
- 所属模块（如 Login、Shop、Battle 等）

---

### 步骤2: 创建 ViewModel
**自动生成：**
```csharp
// Assets/Scripts/Game/{Module}/ViewModels/{PanelName}ViewModel.cs
public class {PanelName}ViewModel : ViewModel
{
    public BindableProperty<string> Property1 { get; }
    public Command OnCommand1 { get; private set; }
    // ...
}
```

**你需要完善：**
- [ ] 添加具体的 BindableProperty 属性
- [ ] 添加 Command 命令
- [ ] 实现 OnCreate() 初始化
- [ ] 从原 Panel 迁移数据逻辑

---

### 步骤3: 创建/修改 Service
**自动生成：**
```csharp
// Assets/Scripts/Game/{Module}/Services/{Module}Service.cs
public class {Module}Service : GameServiceBase
{
    // 业务逻辑方法
}
```

**你需要完善：**
- [ ] 将网络请求迁移到 Service
- [ ] 添加全局事件（如需要）
- [ ] 在 GameMainController 中注册 Service

---

### 步骤4: 创建新 Panel
**自动生成：**
```csharp
// Assets/Scripts/UI/{Path}/{PanelName}New.cs
[PrefabPath("{PrefabPath}")]
public class {PanelName}New : UIPanel<{PanelName}ViewModel>
{
    private {PanelName} mOldPanel;
    
    void Awake()
    {
        mOldPanel = GetComponent<{PanelName}>();
    }
    // ...
}
```

**你需要完善：**
- [ ] 在 OnBindViewModel 中绑定数据
- [ ] 绑定按钮事件
- [ ] 实现生命周期方法

---

### 步骤5: 配置预制体
**操作步骤：**
1. 在 Unity 中选中原预制体
2. 添加 {PanelName}New 组件
3. 确认原 {PanelName} 组件保留
4. 检查 UIRootConfig 层级配置

---

### 步骤6: 修改打开方式
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

**我会帮你：**
- 搜索所有打开该界面的代码
- 提供替换后的代码

---

### 步骤7: 测试验证
**检查清单：**
- [ ] 界面能正常打开和关闭
- [ ] 数据绑定正常工作
- [ ] 按钮点击响应正常
- [ ] 网络请求成功
- [ ] 动画播放正常
- [ ] 没有报错和异常

---

### 步骤8: 清理旧代码
**条件：**
- 新界面运行稳定至少 1 周
- 所有功能测试通过
- 没有用户反馈问题

**操作：**
- 从预制体移除原 Panel 组件
- 删除原 .cs 文件（备份）

---

## 💡 快速开始

**告诉我你想迁移哪个界面，例如：**

```
迁移 ShopPanel
```

我会立即开始步骤1，分析原界面并生成报告。

或者使用自动化脚本：
```bash
python Tools/ui_migration_helper.py ShopPanel Shop UI/Shop
```

---

## 📁 文件命名规范

| 类型 | 命名 | 路径 |
|------|------|------|
| ViewModel | {PanelName}ViewModel | Assets/Scripts/Game/{Module}/ViewModels/ |
| New Panel | {PanelName}New | Assets/Scripts/UI/{Path}/ |
| Service | {Module}Service | Assets/Scripts/Game/{Module}/Services/ |

---

## ⚠️ 注意事项

1. **不要删除原代码**，新旧共存
2. **一次迁移一个界面**，不要批量
3. **每步完成后测试**，确保正常
4. **保留原 UI 组件命名**，方便复用

---

**准备好开始了吗？告诉我你想迁移哪个界面！**
