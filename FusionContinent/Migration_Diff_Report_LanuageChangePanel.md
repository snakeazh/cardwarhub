# LanuageChangePanel 迁移差异分析报告

## 迁移完成时间
2026-03-18

## 原界面 vs 新界面对比

### 1. 已移除的逻辑

#### 1.1 QFramework 特定代码
**原代码：**
```csharp
public class LanuageChangePanelData : UIPanelData { }
public partial class LanuageChangePanel : UIPanel { }
protected override void OnInit(IUIData uiData = null) { }
protected override void OnOpen(IUIData uiData = null) { }
UIKit.ClosePanel(this);
```

**状态：** ✅ 已移除（使用 UIFramework 替代）

#### 1.2 动态按钮生成逻辑变化
**原代码：**
```csharp
var obj = i < LanuageChangeScroll.transform.childCount 
    ? LanuageChangeScroll.transform.GetChild(0).gameObject
    : Instantiate(LanuageChangeScroll.transform.GetChild(0).gameObject, LanuageChangeScroll);
```

**新代码：**
```csharp
GameObject buttonObj;
if (i == 0 && mOldPanel.LanuageBtn1 != null)
{
    buttonObj = mOldPanel.LanuageBtn1.gameObject;
}
else if (i < mOldPanel.LanuageChangeScroll.childCount)
{
    buttonObj = mOldPanel.LanuageChangeScroll.GetChild(i).gameObject;
}
else
{
    buttonObj = Instantiate(mOldPanel.LanuageBtn1?.gameObject, mOldPanel.LanuageChangeScroll);
}
```

**变化：** 更健壮的按钮获取逻辑

#### 1.3 语言切换调用方式
**原代码：**
```csharp
LocalizationManager.Instance.SetLanguage((LanguageType)idx);
```

**新代码：**
```csharp
mLocalizationService?.SetLanguage(languageType);
```

**变化：** 通过 Service 层调用，支持全局事件

#### 1.4 按钮点击事件绑定
**原代码：**
```csharp
obj.gameObject.GetComponent<Button>().onClick.AddListener(() =>
{
    OnLanguageSelected(index);
});
```

**新代码：**
```csharp
button.onClick.AddListener(() =>
{
    vm.OnSelectLanguageCommand.Execute(index);
});
```

**变化：** 使用 Command 模式，ViewModel 处理逻辑

---

### 2. 需要 TODO 的事项

#### TODO 1: 验证动态按钮生成
**文件：** `LanguageChangePanelNew.cs`
**位置：** `InitLanguageButtons` 方法
**验证：**
- [ ] 从配置表正确读取语言列表
- [ ] 按钮数量与语言数量一致
- [ ] 第一个按钮复用 LanuageBtn1 模板
- [ ] 后续按钮正确实例化

#### TODO 2: 验证按钮视觉状态更新
**文件：** `LanguageChangePanelNew.cs`
**位置：** `UpdateButtonVisual` 方法
**验证：**
- [ ] Selected 图片正确显示/隐藏
- [ ] SelectedText 正确显示/隐藏
- [ ] UnSelectedText 正确显示/隐藏
- [ ] 切换语言时视觉状态同步更新

#### TODO 3: 验证语言切换功能
**文件：** `LanguageChangeViewModel.cs` 和 `LocalizationService.cs`
**验证：**
- [ ] 点击语言按钮触发语言切换
- [ ] LocalizationManager 正确切换语言
- [ ] 全局语言切换事件触发
- [ ] 其他界面语言同步更新

#### TODO 4: 验证退出按钮
**文件：** `LanguageChangePanelNew.cs`
**位置：** `Awake` 方法
**问题：** quitBtn 通过 `CloseAnimation.GetComponentInChildren<Button>()` 查找
**验证：**
- [ ] CloseAnimation 节点下有 Button 组件
- [ ] 点击退出按钮播放关闭动画
- [ ] 动画完成后面板关闭

#### TODO 5: 验证上次选择恢复
**文件：** `LanguageChangeViewModel.cs`
**位置：** `LoadLastSelectedLanguage` 方法
**验证：**
- [ ] 从 PlayerPrefs 正确读取上次选择
- [ ] 面板打开时自动触发对应按钮
- [ ] 默认选择第一个语言（如果没有上次记录）

#### TODO 6: 验证事件清理
**文件：** `LanguageChangePanelNew.cs`
**位置：** `OnUnbindViewModel` 方法
**验证：**
- [ ] 所有按钮点击事件正确移除
- [ ] ViewModel 事件正确取消订阅
- [ ] 动态生成的按钮正确销毁（避免内存泄漏）

---

### 3. 架构变化

#### 3.1 职责分离
| 原架构 | 新架构 |
|--------|--------|
| 按钮生成在 Panel | 按钮生成在 Panel，数据来自 ViewModel |
| 语言切换直接调用 | 语言切换通过 Service |
| UI 更新直接操作 | UI 更新通过数据绑定 |

#### 3.2 依赖关系
**原依赖：**
- LanuageChangePanel → DataConfigManager
- LanuageChangePanel → LocalizationManager

**新依赖：**
- LanguageChangePanelNew → LanuageChangePanel (同物体组件)
- LanguageChangePanelNew → LanguageChangeViewModel
- LanguageChangeViewModel → LocalizationService
- LocalizationService → LocalizationManager

---

### 4. 测试检查清单

#### 功能测试
- [ ] 面板正常打开，显示所有可用语言
- [ ] 点击语言按钮，切换语言成功
- [ ] 选中状态正确显示（高亮、文字颜色等）
- [ ] 点击退出按钮，面板关闭
- [ ] 关闭时播放动画
- [ ] 重新打开面板，显示上次选择的语言

#### 兼容性测试
- [ ] 新面板可以与旧面板共存
- [ ] 从 LoginPanelNew 打开 LanguageChangePanelNew 正常
- [ ] 语言切换后，LoginPanelNew 界面语言同步更新
- [ ] 配置表新增/删除语言后正常显示

#### 稳定性测试
- [ ] 多次打开关闭无内存泄漏
- [ ] 快速切换语言无异常
- [ ] 事件订阅正确清理
- [ ] 动态按钮正确销毁

---

### 5. 后续优化建议

#### 5.1 可选增强
- [ ] 添加语言切换确认对话框
- [ ] 添加语言切换动画效果
- [ ] 支持拖拽排序语言列表
- [ ] 添加语言搜索功能

#### 5.2 代码优化
- [ ] 按钮对象池优化（避免频繁创建销毁）
- [ ] 缓存按钮组件引用（避免每次查找）
- [ ] 添加更详细的日志输出
- [ ] 添加错误处理（如配置表读取失败）

---

### 6. 已知限制

1. **按钮模板依赖**：依赖原 LanuageChangePanel 的 LanuageBtn1 作为模板
2. **层级固定**：当前固定在 PopUI 层级
3. **配置表依赖**：启动时必须能读取 TbLanguageConfig

---

### 7. 文件清单

**新增文件：**
- ✅ `Assets/Scripts/Game/Login/ViewModels/LanguageChangeViewModel.cs`
- ✅ `Assets/Scripts/Game/Login/Services/LocalizationService.cs`
- ✅ `Assets/Scripts/UI/Login1/LanguageChangePanelNew.cs`

**修改文件：**
- ✅ `Assets/Scripts/UI/Login1/LoginPanelNew.cs` (更新打开方式)

**待删除文件（稳定后）：**
- ⏳ `Assets/Scripts/UI/Login1/LanuageChangePanel.cs`
- ⏳ `Assets/Scripts/UI/Login1/LanuageChangePanel.Designer.cs`

---

## 总结

本次迁移将 LanuageChangePanel 从 QFramework 迁移到 UIFramework，主要变化：
1. 使用 MVVM 架构分离 UI 逻辑和业务逻辑
2. 动态按钮生成逻辑优化，更健壮
3. 语言切换通过 Service 层，支持全局事件
4. 使用 Command 模式处理按钮点击
5. 复用原 Panel 的 UI 组件和动画系统

**下一步：** 按照 TODO 清单逐项验证功能。
