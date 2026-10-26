# Migration_Diff_Report_GuidePanel2

## 迁移前确认单

- Path: `UI/Guide`
- Module: `Game/Guide`
- PrefabPath: `GuidePanel2`（复用 `Assets/Res/UIPrefab/GuidePanel2.prefab`)
- UILevel: `UIFramework.UILevel.Guide`（显式限定，避免与 QFramework.UILevel 歧义）
- Service: 不新建。引导无网络请求，`GuideManager` 本身就是编排者（相当于 Service 层）
- 风险点：`GuideContext.Panel` 换类型、场景内实例手动 Init、打开改异步

## 1. 已移除/变更的逻辑

### 1.1 打开方式
- 原：`UIKit.OpenPanel<GuidePanel2>(UILevel.Guide, data)`(QFramework 同步加载，WebGL 不支持）
- 新：`UIManager.OpenPanelAsync<GuidePanel2New, GuidePanel2ViewModel>(vm, UIFramework.UILevel.Guide)`(UIFramework 全程异步，WebGL 原生支持）
- `GuideManager.StartCurrent` 因此从同步改为 `async void`,try/catch 从 `RunStart` 收进方法内部

### 1.2 数据传递
- 原：`GuidePanel2Data`(Step / Current / CardId）作为 IUIData 传入，面板 OnInit/OnOpen 里读 `mData`
- 新：`GuidePanel2ViewModel` 持有 Step / Current / CardId，打开前写入，复用时每次步骤刷新

### 1.3 GuideContext
- `GuideContext.Panel` 类型 `GuidePanel2` → `GuidePanel2New`
- `GuidePanel2New` 通过转发属性暴露旧面板全部 12 个 Designer 节点，8 个 GuideMethod 零改动

### 1.4 死代码清理
- 删除 `StartCurrent` 中 `if (true)` 的永不执行 else 分支（旧 GuidePanel 打开路径）
- 移除 `GameStartMono` 启动流程里的 `GuidePanel2` 预载（上一轮为 UIKit 同步打开加的，UIFramework 异步加载后不再需要）
- `GuideManager` 移除 `using QFramework;`，新增 `using Game.Guide;` / `using UIFramework;`

## 2. 改动清单（文件级）

| 文件 | 操作 |
|------|------|
| `Assets/Scripts/Game/Guide/ViewModels/GuidePanel2ViewModel.cs` | 新增 |
| `Assets/Scripts/UI/Guide/GuidePanel2New.cs` | 新增 |
| `Assets/Scripts/UI/Guide/GuideContext.cs` | Panel 字段换类型 |
| `Assets/Scripts/UI/Guide/GuideManager.cs` | 字段/Setter 换类型，StartCurrent 改异步，Setter 内手动 Init 场景实例 |
| `Assets/Scripts/Scenes/LoadingModules/Display/SceneInitManagers.cs` | `GetComponent<GuidePanel2>` → `GetComponent<GuidePanel2New>` |
| `Assets/Scripts/GameStartMono.cs` | 移除 GuidePanel2 启动预载 |

## 3. 调用点替换清单

- `GuideManager.StartCurrent` UIScene 分支：UIKit.OpenPanel → UIManager.OpenPanelAsync（唯一打开点）
- `SceneInitManagers.OnStartLoad`：场景实例组件获取换类型（唯一 SetInWarPanel 调用点）
- `SetInUIPanel` 无现存调用点，仅签名换类型

## 4. 需要 TODO 的事项

- [ ] **Unity 操作**：给 `Assets/Res/UIPrefab/GuidePanel2.prefab` 挂 `GuidePanel2New` 组件（保留旧 `GuidePanel2`)
- [ ] **Unity 操作**：给战斗场景 `GameGuide/InGameGuidePanel` 物体挂 `GuidePanel2New` 组件
- [ ] 验证 UIScene 引导（主城）能打开、步骤结束能 Hide
- [ ] 验证 InWar 引导（战斗内）手指/遮罩/对话显示正常
- [ ] 验证切场景后再次进战斗，引导面板复用不重复创建
- [ ] 旧组件稳定 1 周后再从 prefab 移除 `GuidePanel2` 并删除旧脚本（步骤9)

## 5. 架构变化说明

- 数据真值源：`GuidePanel2Data`（每次 Open 传入）→ `GuidePanel2ViewModel`（面板持有，跨步骤复用时刷新）
- 面板生命周期：UIKit(QFramework)→ UIManager(UIFramework),`Open()/Show()/Hide()` 实例方法签名一致，GuideManager 主流程调用不变
- 引导方法与 View 解耦路径不变：仍通过 `GuideContext.Panel` 拿节点，只是类型换成转发层

## 6. 已知限制 / 注意点

- `GuidePanel2New.OnOpen` 会调 `VM.Current.Init(...)`，与 GuideManager 里的显式 `_current.Init` 形成双重 Init——这是**沿用旧行为**（旧 OnOpen 同样调 Init)，各 GuideMethod 的 Init 已验证可重入（只设置节点状态与监听）
- 场景内实例（InWar）不在 UIManager.mPanels 里，`Close()` 不会被框架管理；当前引导流程只用 Hide()，与旧行为一致
- `ShouldUpdateUserToolOnBecameVisible` 重写为 false：场景实例 Level 默认为 Common，不屏蔽会触发 UserTool 隐藏逻辑（新增保护，旧 QFramework 无此逻辑）

## 7. 历史问题规避说明

1. **UILevel 歧义**：显式使用 `UIFramework.UILevel.Guide`,GuideManager 已移除 `using QFramework;`，无歧义残留
2. **界面关闭路径**：引导面板从不 Close 只 Hide（旧行为），UIFramework Managed 面板在 `UIManager.ClosePanel` / 切场景清理时才销毁；`Hide()` 路径已验证签名一致
3. **WebGL 同步加载**：迁移后不再走 `YooPanelLoaderPool.LoadPanelPrefab` 同步路径，上一轮报错的根因消除
