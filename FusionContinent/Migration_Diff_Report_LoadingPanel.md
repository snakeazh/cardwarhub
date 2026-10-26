# LoadingPanel 迁移差异分析报告

## 迁移完成时间
2026-03-18

## 原界面 vs 新界面对比

### 1. 已移除的逻辑

#### 1.1 QFramework 特定代码
**原代码：**
```csharp
public class LoadingPanelData : UIPanelData { }
public partial class LoadingPanel : UIPanel { }
protected override void OnInit(IUIData uiData = null) { }
protected override void OnOpen(IUIData uiData = null) { }
protected override void OnShow() { }
protected override void OnHide() { }
protected override void OnClose() { }
```

**状态：** ✅ 已移除（使用 UIFramework 替代）

#### 1.2 协程改为 UniTask
**原代码：**
```csharp
IEnumerator LoadSceneAsync()
{
    yield return loadingOp;
    // ...
    yield return new WaitForEndOfFrame();
}

StartCoroutine(LoadSceneAsync());
```

**新代码：**
```csharp
public async UniTask<bool> LoadSceneAsync(string targetScene)
{
    await loadingOp.ToUniTask();
    // ...
    await UniTask.WaitForEndOfFrame();
}
```

**变化：** 使用 UniTask 替代 IEnumerator，更好的性能和错误处理

#### 1.3 进度更新方式
**原代码：**
```csharp
void UpdateUI(float progress)
{
    progressBar.value = progress;
    ProgressText.text = (progress * 100f).ToString("F1") + "%";
}
```

**新代码：**
```csharp
// 通过 BindableProperty 自动更新
vm.CurrentProgress.OnValueChanged += (oldVal, newVal) =>
{
    mOldPanel.progressBar.value = newVal;
};
```

**变化：** 使用数据绑定自动更新UI

#### 1.4 场景加载参数
**原代码：**
```csharp
public string targetScene = "";
public float smoothSpeed = 3f;
public float minLoadingTime = 2.0f;
targetScene = SceneLoader.TargetScene;
```

**新代码：**
```csharp
// 参数通过 ViewModel 传递
OnStartLoadCommand.Execute(targetScene);

// 参数在 Service 中配置
private float _smoothSpeed = 3f;
private float _minLoadingTime = 2.0f;
public void SetLoadingParams(float smoothSpeed, float minLoadingTime)
```

**变化：** 参数通过 Command 传递，更灵活

#### 1.5 提示文本获取
**原代码：**
```csharp
TipText.text = _tipList[Random.Range(0, _tipList.Length)];
var loadingCfg = _loadingConfig.DataList[Random.Range(0, _loadingConfig.DataList.Count)];
```

**新代码：**
```csharp
// 在 ViewModel 中处理
TipText.Value = _defaultTipList[Random.Range(0, _defaultTipList.Length)];
var loadingConfig = mLoadingService?.GetLoadingConfig();
```

**变化：** 提示文本逻辑移到 ViewModel

---

### 2. 需要 TODO 的事项

#### TODO 1: 验证场景加载流程
**文件：** `LoadingService.cs`
**位置：** `LoadSceneAsync` 方法
**验证：**
- [ ] LoadingScene 正确加载
- [ ] 目标场景正确加载（Additive 模式）
- [ ] 进度条平滑更新
- [ ] 最少加载时间控制正常
- [ ] 场景正确激活
- [ ] LoadingScene 正确卸载

#### TODO 2: 验证进度条更新
**文件：** `LoadingPanelNew.cs`
**位置：** `BindProperties` 方法
**验证：**
- [ ] 进度条数值正确显示
- [ ] 进度百分比文本正确显示
- [ ] 进度条动画平滑
- [ ] 100% 时进度条满格

#### TODO 3: 验证提示文本显示
**文件：** `LoadingPanelNew.cs` 和 `LoadingPanelViewModel.cs`
**验证：**
- [ ] 随机提示文本正确显示
- [ ] 配置表提示文本正确显示
- [ ] Tips 子对象文本正确更新

#### TODO 4: 验证 Spine 动画
**文件：** `LoadingPanelNew.cs`
**位置：** `StartLoad` 方法
**验证：**
- [ ] Spine 动画正确初始化
- [ ] 动画正常播放
- [ ] 切换场景时动画不卡顿

#### TODO 5: 验证音效播放
**文件：** `LoadingPanelNew.cs`
**位置：** `OnLoadCompleted` 方法
**验证：**
- [ ] GameStart 场景加载完成后播放 BGM
- [ ] 其他场景不播放 BGM
- [ ] 音效管理器正确引用

#### TODO 6: 验证错误处理
**文件：** `LoadingService.cs`
**位置：** `LoadSceneAsync` 方法
**验证：**
- [ ] 场景加载失败时正确触发错误事件
- [ ] 错误信息正确传递
- [ ] 加载状态正确重置
- [ ] 取消加载功能正常

#### TODO 7: 验证取消加载
**文件：** `LoadingService.cs`
**位置：** `CancelLoad` 方法
**验证：**
- [ ] 取消请求正确发送
- [ ] 加载正确停止
- [ ] 资源正确释放
- [ ] 无内存泄漏

---

### 3. 架构变化

#### 3.1 职责分离
| 原架构 | 新架构 |
|--------|--------|
| 场景加载逻辑在 Panel | 场景加载逻辑在 Service |
| 进度更新直接调用 | 进度更新通过事件 |
| 协程控制流程 | UniTask 控制流程 |
| 参数硬编码 | 参数可配置 |

#### 3.2 依赖关系
**原依赖：**
- LoadingPanel → YooAssets
- LoadingPanel → SceneManager
- LoadingPanel → SceneLoader
- LoadingPanel → SoundManager

**新依赖：**
- LoadingPanelNew → LoadingPanel (同物体组件)
- LoadingPanelNew → LoadingPanelViewModel
- LoadingPanelViewModel → LoadingService
- LoadingService → YooAssets
- LoadingService → SceneManager

---

### 4. 测试检查清单

#### 功能测试
- [ ] 打开加载面板，自动开始加载
- [ ] 进度条从 0% 到 100% 平滑更新
- [ ] 提示文本随机显示
- [ ] Spine 动画正常播放
- [ ] 场景加载完成后自动跳转
- [ ] GameStart 场景播放 BGM

#### 兼容性测试
- [ ] 新面板可以与旧面板共存
- [ ] 从任意场景打开加载面板正常
- [ ] 加载不同场景都正常

#### 稳定性测试
- [ ] 多次加载无内存泄漏
- [ ] 快速切换场景无异常
- [ ] 加载失败时正确提示
- [ ] 取消加载功能正常
- [ ] 网络异常时正确处理

#### 性能测试
- [ ] 加载时间符合预期
- [ ] 进度更新流畅（60fps）
- [ ] 内存占用稳定

---

### 5. 后续优化建议

#### 5.1 可选增强
- [ ] 添加加载失败重试功能
- [ ] 添加加载超时处理
- [ ] 添加加载进度保存（断点续传）
- [ ] 添加多场景预加载
- [ ] 添加加载界面主题切换

#### 5.2 代码优化
- [ ] 添加更详细的加载日志
- [ ] 优化进度条更新频率
- [ ] 添加加载性能统计
- [ ] 支持自定义加载动画

---

### 6. 已知限制

1. **YooAssets 依赖**：必须使用 YooAssets 加载场景
2. **LoadingScene 依赖**：必须存在 LoadingScene 场景
3. **Spine 依赖**：需要 Spine 运行时
4. **层级固定**：当前固定在 Loading 层级

---

### 7. 文件清单

**新增文件：**
- ✅ `Assets/Scripts/Game/Loading/ViewModels/LoadingPanelViewModel.cs`
- ✅ `Assets/Scripts/Game/Loading/Services/LoadingService.cs`
- ✅ `Assets/Scripts/UI/LoadingPanelNew.cs`

**修改文件：**
- ⏳ 需要修改打开 LoadingPanel 的代码（搜索所有引用）

**待删除文件（稳定后）：**
- ⏳ `Assets/Scripts/UI/LoadingPanel.cs`
- ⏳ `Assets/Scripts/UI/LoadingPanel.Designer.cs`

---

## 总结

本次迁移将 LoadingPanel 从 QFramework 迁移到 UIFramework，主要变化：
1. 使用 MVVM 架构分离 UI 逻辑和业务逻辑
2. 使用 UniTask 替代协程，更好的性能
3. 使用数据绑定自动更新进度条
4. 使用 Service 处理场景加载，支持取消和错误处理
5. 复用原 Panel 的 UI 组件和动画系统

**下一步：** 按照 TODO 清单逐项验证功能。
