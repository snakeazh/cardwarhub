# CancelAccountPanel 迁移差异分析报告

## 迁移完成时间
2026-03-18

## 原界面 vs 新界面对比

### 1. 已移除的逻辑

#### 1.1 QFramework 特定代码
**原代码：**
```csharp
public class CancelAccountPanelData : UIPanelData { }
public partial class CancelAccountPanel : UIPanel { }
protected override void OnInit(IUIData uiData = null) { }
protected override void OnOpen(IUIData uiData = null) { }
protected override void OnShow() { }
protected override void OnHide() { }
protected override void OnClose() { }
UIKit.ClosePanel(this);
UIKit.ClosePanel<CancelAccountPanel>();
```

**状态：** ✅ 已移除（使用 UIFramework 替代）

#### 1.2 动画系统调用方式
**原代码：**
```csharp
protected override void RunAnimation(GameObject target, UnityAction complete)
{
    UIAnimationUtils.PlayAnimations(target, complete);
}

RunAnimation(this.GetAnimationCloseNode, () => { ... });
```

**新代码：**
```csharp
private void PlayCloseAnimation(System.Action onComplete)
{
    UIAnimationUtils.PlayAnimations(mOldPanel.CloseAnimation.gameObject, () => onComplete?.Invoke());
}
```

**变化：** 从虚函数重写改为实例方法调用

#### 1.3 消息提示方式
**原代码：**
```csharp
UIKit.GetPanel<LoginPanel01>().transform.GetComponent<ShowTipManager>().SetTiptxt(msg, 2f, false);
```

**新代码：**
```csharp
var tipManager = GetComponent<ShowTipManager>();
if (tipManager == null) tipManager = FindObjectOfType<ShowTipManager>();
tipManager?.SetTiptxt(message, 2f, false);
```

**变化：** 不再依赖 LoginPanel01，直接从自身或场景查找

---

### 2. 需要 TODO 的事项

#### TODO 1: 测试消息提示功能
**文件：** `CancelAccountPanelNew.cs`
**位置：** `OnShowMessage` 方法
**问题：** 消息提示现在从 CancelAccountPanelNew 自身查找 ShowTipManager，而不是从 LoginPanel01
**验证：** 确保 ShowTipManager 在 CancelAccountPanel 预制体上存在

#### TODO 2: 验证退出按钮查找
**文件：** `CancelAccountPanelNew.cs`
**位置：** `Awake` 方法
**问题：** exitBtn 通过 `CloseAnimation.GetComponentInChildren<Button>()` 查找
**验证：** 确保 CloseAnimation 节点下有 Button 组件

#### TODO 3: 动画播放测试
**文件：** `CancelAccountPanelNew.cs`
**位置：** `PlayOpenAnimation` 和 `PlayCloseAnimation` 方法
**验证：** 
- [ ] 打开面板时动画正常播放
- [ ] 关闭面板时动画正常播放
- [ ] 动画完成后面板正确关闭

#### TODO 4: 数据清除确认
**文件：** `CancelAccountViewModel.cs`
**位置：** `ClearAccountData` 方法
**验证：**
- [ ] 点击注销按钮后，PlayerPrefs 中的 Account 被删除
- [ ] PlayerPrefs 中的 Password 被删除
- [ ] 提示消息 "已退出登录" 正常显示

#### TODO 5: 事件订阅清理
**文件：** `CancelAccountPanelNew.cs`
**位置：** `OnUnbindViewModel` 方法
**验证：**
- [ ] 面板关闭时所有事件正确取消订阅
- [ ] 没有内存泄漏

---

### 3. 架构变化

#### 3.1 职责分离
| 原架构 | 新架构 |
|--------|--------|
| 数据清除逻辑在 Panel | 数据清除逻辑在 ViewModel |
| 消息提示直接调用 | 消息提示通过事件触发 |
| 按钮事件直接处理 | 按钮事件通过 Command 处理 |

#### 3.2 依赖关系
**原依赖：**
- CancelAccountPanel → LoginPanel01 (通过 UIKit.GetPanel)

**新依赖：**
- CancelAccountPanelNew → CancelAccountPanel (同物体组件)
- CancelAccountViewModel → LoginService
- CancelAccountPanelNew → CancelAccountViewModel

---

### 4. 测试检查清单

#### 功能测试
- [ ] 点击退出按钮，播放关闭动画后关闭面板
- [ ] 点击注销按钮，清除数据并显示提示
- [ ] 提示消息 "已退出登录" 正确显示
- [ ] 数据清除后，LoginPanel01 的账号密码为空

#### 兼容性测试
- [ ] 新面板可以与旧面板共存
- [ ] 从 LoginPanelNew 打开 CancelAccountPanelNew 正常
- [ ] 动画播放正常，无卡顿

#### 稳定性测试
- [ ] 多次打开关闭无内存泄漏
- [ ] 快速点击按钮无异常
- [ ] 事件订阅正确清理

---

### 5. 后续优化建议

#### 5.1 可选增强
- [ ] 添加注销确认对话框（防止误触）
- [ ] 添加注销后自动跳转到登录界面
- [ ] 添加注销动画效果

#### 5.2 代码优化
- [ ] 将 ShowTipManager 查找缓存，避免每次查找
- [ ] 添加更详细的日志输出
- [ ] 添加错误处理（如 PlayerPrefs 操作失败）

---

### 6. 已知限制

1. **ShowTipManager 依赖**：如果场景中不存在 ShowTipManager，消息提示将不显示
2. **动画依赖**：依赖原 CancelAccountPanel 的动画组件，如果原组件被删除，动画将失效
3. **层级固定**：当前固定在 PopUI 层级，如需动态调整需要修改代码

---

### 7. 文件清单

**新增文件：**
- ✅ `Assets/Scripts/Game/Login/ViewModels/CancelAccountViewModel.cs`
- ✅ `Assets/Scripts/UI/Login1/CancelAccountPanelNew.cs`

**修改文件：**
- ✅ `Assets/Scripts/UI/Login1/LoginPanelNew.cs` (更新打开方式)

**待删除文件（稳定后）：**
- ⏳ `Assets/Scripts/UI/Login1/CancelAccountPanel.cs`
- ⏳ `Assets/Scripts/UI/Login1/CancelAccountPanel.Designer.cs`

---

## 总结

本次迁移将 CancelAccountPanel 从 QFramework 迁移到 UIFramework，主要变化：
1. 使用 MVVM 架构分离 UI 逻辑和业务逻辑
2. 使用 Command 模式处理按钮点击
3. 使用事件机制解耦组件依赖
4. 复用原 Panel 的 UI 组件，减少重复工作

**下一步：** 按照 TODO 清单逐项验证功能。
