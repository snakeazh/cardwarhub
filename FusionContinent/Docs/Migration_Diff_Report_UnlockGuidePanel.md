# Migration Diff Report: UnlockGuidePanel

## 1. 已移除的逻辑

### 1.1 QFramework 特定代码
- 打开：`UIKit.OpenPanel<UnlockGuidePanel>(UILevel.Guide)` → `UIFramework.UIManager.OpenPanelAsync<UnlockGuidePanelNew, UnlockGuidePanelViewModel>(vm, UIFramework.UILevel.Guide)`（UnlockGuideManager.ShowGuide，异步，顺带根治 WebGL 同步加载报错；已有面板先 ClosePanel 再重开，保证 OnOpen 重跑、按 PendingStartStep 重开始，对齐原 OpenPanel 重开语义）
- 关闭：`UIKit.ClosePanel(_panel)` → `UIFramework.UIManager.ClosePanel<UnlockGuidePanelNew>()`
- 隐藏：`UIKit.HideAllPanel(typeof(UnlockGuidePanel))`（GameMainPanel.ChangePanel）→ `UIFramework.UIManager.HidePanel<UnlockGuidePanelNew>()`
- `UnlockGuidePanelData`（空 UIPanelData）不再使用，新面板走 UIFramework IUIData

### 1.2 直接调用改为 Command
- `SkipBtn` 原在 OnInit 里匿名 lambda → ViewModel `OnSkipCommand`（→ ForceCompleteGuide）
- `NextBtn` 原在 StartGuide 里 `RemoveAllListeners()` + 匿名 lambda 重绑 → ViewModel `OnNextCommand`（→ Send(模块, CurrentStep)），View 在 OnBindViewModel 一次性绑定具名方法 `OnNextButtonClicked`，消除了项目禁止的 RemoveAllListeners 用法

### 1.3 数据真值源
- `currentStep` 字段 → `VM.CurrentStep`（BindableProperty<int>），面板暴露 `CurrentStep` 属性供 UnlockGuideManager 读取（原 `_panel.currentStep` 两处调用点已改）

## 2. 需要 TODO 的事项

- [ ] 预制体 `Assets/Res/UIPrefab/UnlockGuidePanel.prefab` 需手动挂载 `UnlockGuidePanelNew` 组件（保留原 `UnlockGuidePanel` 组件，新面板靠它拿 Designer 节点引用）
- [ ] 验证解锁引导完整流程：弹面板 → 手指框选目标 → 点击目标/遮罩推进 → 对话框缩放动画 → 跳过按钮
- [ ] 验证 WebGL 下不再报 `WebGL 不支持同步加载: UnlockGuidePanel`
- [ ] 验证引导进行中切页（GameMainPanel.ChangePanel）面板能隐藏、回来后状态正确

## 3. 架构变化说明

- 新面板 `UnlockGuidePanelNew : UIPanel<UnlockGuidePanelViewModel>`，`[PrefabPath("UnlockGuidePanel")]`，UIFramework 负责加载与生命周期
- 步骤数据真值源移到 ViewModel（CurrentStep），按钮意图走 Command；目标节点轮询、框选高亮、DOTween 动画等纯表现逻辑保留在 View（移植自旧面板，行为逐行对齐）
- 旧面板组件保留在预制体上仅作 Designer 节点容器，其 QFramework 生命周期不再被驱动

## 4. 测试检查清单

- [ ] 功能：引导各步骤（ClickTarget / ClickMask / 对话框显示）正常
- [ ] 兼容：引导中目标节点晚出现（异步加载）时轮询等待正常，100s 超时自动关闭
- [ ] 稳定：引导完成/跳过后再触发新模块引导，面板能正常重开（ShowGuide 先关后开，OnOpen 重跑）
- [ ] WebGL：无同步加载报错，面板层级（UILevel.Guide = 400）正确盖在其他 UI 上

## 5. 后续优化建议

- 稳定运行 1 周后可按步骤 9 清理：预制体移除旧 `UnlockGuidePanel` 组件（需先把 Designer 字段引用转移到新面板或另建绑定）、删除 `UnlockGuidePanel.cs` / `.Designer.cs`
- `SkipBtn` 目前是存量 `UnityEngine.UI.Button`，后续随预制体整改可换 `UIPointerClickAndLongPress`

## 历史问题规避说明

1. UILevel 歧义：全部显式 `UIFramework.UILevel.Guide`，不与 QFramework.UILevel 混用
2. 关闭路径：CloseGuide / ChangePanel 隐藏均切到 UIFramework 对应 API，新旧关闭路径不混用
3. 动态内容：引导目标节点轮询等待逻辑原样保留，避免"面板开了但框选不上"的回归
