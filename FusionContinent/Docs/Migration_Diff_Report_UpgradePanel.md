# Migration_Diff_Report_UpgradePanel

## 1. 已移除的逻辑

### 1.1 QFramework 特定代码
- 打开方式：`UIKit.OpenPanel<UpgradePanel>(QFramework.UILevel.PopUI)` → `UIFramework.UIManager.OpenPanelAsync<UpgradePanelNew, UpgradePanelViewModel>(vm, UIFramework.UILevel.PopUI)`
- 关闭方式：`UIKit.ClosePanel(this)`（动画完成后）→ `CloseSelf()`
- 面板基类：`QFramework.UIPanel` → `UIFramework.UIPanel<UpgradePanelViewModel>`

### 1.2 数据流改为 ViewModel
- 原 `SetData(lastLevel, currentLevel)` 在面板内筛配置 → 移至 `UpgradePanelViewModel.Setup`，面板（View）只做列表实例化与贴图
- 原 `Action OnClosePanel` 字段回调 → `UpgradePanelViewModel.OnClosed` 事件，`UpgradePanelNew.OnClose` 中触发

### 1.3 未迁移部分（保留在 View）
- `DoItemMoveAni` / `GetItemTarget`：纯表现层动画与主界面节点查找，保留在 `UpgradePanelNew`
- `OnShow` 的 `Global.Player.SetModuleUnlockLv` 副作用保留在 View 生命周期

## 2. 需要 TODO 的事项
- [ ] 验证升级时弹窗正常打开（UIFramework 异步加载预制体）
- [ ] 验证 ExitBtn 点击后飞行动画播完自动关闭，并继续 DailyPopUpLogic 后续流程（引导等）
- [ ] 验证无解锁项时 UnlockList 隐藏、直接可关闭
- [ ] 确认旧 `UpgradePanel` 组件在 prefab 上共存无干扰（不再经 QFramework 打开）
- [ ] 稳定 1 周后可执行步骤9：移除 prefab 上的旧组件、删除 `UpgradePanel.cs/.Designer.cs`

## 3. 架构变化说明
- 职责分离：配置筛选（VM）/ 列表构建与动画（View）/ 打开与回调编排（MainInterfacePanelNew）
- 依赖方向：View 不再直接读 `TbModuleFunctionConfig`，只消费 VM 的 `UnlockItems`
- 附带收益：UIFramework 面板预制体走异步加载，原 WebGL `LoadSync: UpgradePanel` 报错路径被根除

## 4. 测试检查清单
- [ ] 功能测试：升级跨等级解锁多项功能时列表完整、图标（UpgradeItemAtlas）与名称正确
- [ ] 功能测试：飞行动画目标节点（签到/月卡/活动/英灵/大世界/商店/轮语者/奥术卡）位置正确
- [ ] 兼容性测试：面板关闭后 `OnClosed` 正确回调，每日弹出流程不中断
- [ ] 稳定性测试：WebGL 构建下打开无 "WebGL 不支持同步加载" 报错

## 5. 后续优化建议
- 旧 `UpgradePanel` 组件与 `.Designer.cs` 待稳定后清理
- `ExitBtn` 为存量 `UnityEngine.UI.Button`，后续如需改动该入口应换 `UIPointerClickAndLongPress`
