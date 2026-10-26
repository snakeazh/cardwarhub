# 对象池界面替换清单

生成时间：2026-04-09

## 1. 目标

梳理项目中所有界面脚本里与对象池相关的使用点，识别：

- 已经完成替换的界面
- 仍在使用旧式/手工对象池管理的界面
- 适合迁移到 `UIPoolScope` 或现有三类对象池管理器的界面

当前对象池体系：

- `CommonItemPoolManager`
- `UIPanelItemPoolManager`
- `BatttleItemPoolManager`
- `UIPoolScope`

---

## 2. 已扫描到使用对象池的界面文件

### 2.1 老 QFramework 界面

- `Scripts/UI/BackpackPanel.cs`
- `Scripts/UI/CreatePlayerMaincityPanel.cs`
- `Scripts/UI/Shop/ProbabilityDescPanel.cs`
- `Scripts/UI/Shop/NormalShopList01.cs`
- `Scripts/UI/Soldier/AwakeningPanel.cs`
- `Scripts/UI/MainInterface/MainInterfacePanel.cs`
- `Scripts/UI/SevenDaySingInPanel.cs`
- `Scripts/UI/LoginPanel.cs`
- `Scripts/UI/GameSettlementPanel1.cs`
- `Scripts/UI/GamePausePanel.cs`
- `Scripts/UI/normalShopList.cs`
- `Scripts/UI/Talentshow.cs`
- `Scripts/UI/ProbabilityDesc.cs`
- `Scripts/UI/LevelScrollController.cs`
- `Scripts/UI/TalentDetails.cs`
- `Scripts/UI/SkillDetails.cs`
- `Scripts/UI/GeneralCardSlot.cs`
- `Scripts/UI/CardSlot.cs`
- `Scripts/UI/CombatTipController.cs`

### 2.2 UIFramework 新界面

- `Scripts/UI/Shop/ProbabilityDescPanelNew.cs`
- `Scripts/UI/Soldier/SkinSoliderPanelNew.cs`
- `Scripts/UI/Soldier/AwakeningPanelNew.cs`
- `Scripts/UI/MainInterface/MainInterfacePanelNew.cs`
- `Scripts/UI/CombatTipPanelNew.cs`
- `Scripts/UI/Shop/NormalShopList01New.cs`

### 2.3 辅助类

- `Scripts/UI/ScrollView/UIItemCreator.cs`
- `Scripts/UI/Pool/UIPoolWarmUpScheduler.cs`

---

## 3. 已完成替换/已接入新逻辑的界面

### 3.1 `BackpackPanel`

状态：**已部分完成替换**

已处理：

- 老 Panel 已挂接 `UIPoolScope`
- 详情区 `RewardEntity` 已改为 `CommonItemPoolManager + UIPoolScope`
- 列表项 `BackPackItem` 已不再通过 `UIItemCreator.Instance` 获取/释放
- 列表项改为直接通过 `CommonItemPoolManager.GetGameObjectPoolOrCreate("BackPackItem")`
- 列表项释放时，会先回收子节点 `RewardEntity`，再回收 `BackPackItem`
- `OnHide()` / `OnClose()` 已补 `scrollView.ReleaseScrollView()` 释放链路

当前建议：

- 可视为已切到现有对象池体系
- 后续只需要继续观察高频开关界面时是否存在重复释放/二次绑定问题

### 3.2 `CreatePlayerMaincityPanel`

状态：**已部分完成替换**

已处理：

- 老 Panel 已挂接 `UIPoolScope`
- `ClothingObj` 已从 `PoolBinder` 逻辑切换为 `UIPoolScope` 分组管理
- `InitClothing()` 改为：先 `ReleaseGroup("ClothingObj")`，再从 `CommonItemPoolManager` 获取并登记到 scope
- `OnHide()` / `OnClose()` 已补统一释放

当前未处理：

- `PlayerTitleObj` 仍然是 `Instantiate`
- 属性展示节点仍然是 `Instantiate/Destroy`
- 装扮特效节点仍然是普通创建销毁

当前建议：

- 保持老 Panel，不做 UIFramework 迁移
- 继续把 `PlayerTitleObj` 和属性项逐步纳入 `UIPoolScope` 或 `CommonItemPoolManager`

---

## 4. 高优先级替换界面

这些界面对象池使用最集中，且改造收益高。

### 4.1 `Scripts/UI/Shop/ProbabilityDescPanelNew.cs`

现状：

- 已经是 `UIFramework.UIPanel`
- 奖励列表使用 `CommonItemPoolManager.Instance.GetGameObjectPool("RewardEntity")`
- 刷新逻辑是“先清旧，再生成新”

问题：

- 当前仍是手动批量处理
- 未使用 `PoolScope.ReleaseGroup(...)`
- 关闭时的对象回收责任不够明确

建议替换方案：

- 使用 `PoolScope` 管理奖励列表分组，如 `reward-list`
- 刷新前 `PoolScope.ReleaseGroup("reward-list")`
- 创建时 `Track` 或封装为统一 `Spawn` 流程

优先级：**P0**

---

### 4.2 `Scripts/UI/CombatTipPanelNew.cs`

现状：

- 已经是 `UIFramework.UIPanel`
- `CombatTip` 通过对象池获取/归还
- 当前是手动 `Get` 和手动 `Return`

问题：

- 仍然是手工管理生命周期
- 没完全复用 `UIPoolScope`

建议替换方案：

- 将 `CombatTip` 纳入 `UIPoolScope` 托管
- 如需要，可扩展 `UIPoolScope` 对 `CommonItemPoolManager` 的统一封装接口

优先级：**P0**

---

### 4.3 `Scripts/UI/Shop/NormalShopList01New.cs`

现状：

- 已经是 `UIFramework.UIPanel`
- 仍然使用 `Instantiate` + `mActiveShopItems/mInactiveShopItems` 手工缓存

问题：

- 等于自己维护了一套局部小型对象池
- 与项目现有对象池体系重复

建议替换方案：

- 商店 item 改用 `CommonItemPoolManager` 或 `UIPanelItemPoolManager`
- 标题对象改用 `CommonItemPoolManager`
- 将 `mActiveShopItems/mInactiveShopItems` 迁移为 scope/group 管理

优先级：**P1**

---

### 4.4 `Scripts/UI/Soldier/SkinSoliderPanelNew.cs`

现状：

- 已经是 `UIFramework.UIPanel`
- 动态生成皮肤按钮列表
- 更偏“动态 UI 列表”场景

建议替换方案：

- 皮肤按钮列表改用 `UIPoolScope + UIPanelItemPoolManager/CommonItemPoolManager`
- 面板关闭时自动统一回收

优先级：**P1**

---

### 4.5 `Scripts/UI/Soldier/AwakeningPanelNew.cs`

现状：

- 已经是 `UIFramework.UIPanel`
- 存在材料项等动态内容
- 代码中已有 `RecycleMaterials()` 这种手工清理痕迹

建议替换方案：

- 材料项、动态技能展示项纳入 `PoolScope`
- 把手工清理逻辑替换为 group 释放

优先级：**P1**

---

## 5. 中优先级替换界面

### 5.1 `Scripts/UI/Shop/ProbabilityDescPanel.cs`

现状：

- 老 QFramework Panel
- 奖励项通过 `CommonItemPoolManager.GetGameObjectPool("RewardEntity")` 获取
- 未看到关闭时明确归还逻辑

建议：

- 短期：老 Panel 上挂 `UIPoolScope`
- 长期：功能迁移到 `ProbabilityDescPanelNew`

优先级：**P1**

---

### 5.2 `Scripts/UI/Shop/NormalShopList01.cs`

现状：

- 老 QFramework Panel
- 标题对象/商品对象存在手动缓存与池化混用

建议：

- 短期：继续留在老框架，但统一收口对象池分组管理
- 长期：让 `NormalShopList01New` 完全替代它

优先级：**P1**

---

### 5.3 `Scripts/UI/GamePausePanel.cs`

现状：

- 奖励项使用 `RewardEntity`
- `OnHide/OnClose` 手动循环 `Return`
- 学派卡部分仍有 `Instantiate`

建议：

- 奖励项改为 `UIPoolScope` 管理
- 学派卡列表根据需求改为对象池或保留 Instantiate

优先级：**P2**

---

### 5.4 `Scripts/UI/GameSettlementPanel1.cs`

现状：

- 面板初始化时 `CommonItemPoolManager.Instance.ReturnAll()`
- 奖励展示直接 `LoadSync(...).Instantiate()`
- 统计卡片也是 `Instantiate`

问题：

- 创建/回收策略粗放
- `ReturnAll()` 影响面过大

建议：

- 奖励项、统计卡片改成明确的对象池类型
- 避免界面初始化时直接对 `CommonItemPoolManager` 全量回收

优先级：**P2**

---

### 5.5 `Scripts/UI/LoginPanel.cs`

现状：

- 登录成功后对 `arrow` 执行对象池预取
- 属于 UI 里夹带战斗对象池预热

建议：

- 从面板逻辑中移出
- 迁到启动阶段或战斗相关 warmup 调度器中

优先级：**P2**

---

## 6. 低优先级或待进一步确认的界面

这些文件扫描到了对象池相关调用，但需要结合业务再判断是否立即替换：

- `Scripts/UI/MainInterface/MainInterfacePanelNew.cs`
- `Scripts/UI/MainInterface/MainInterfacePanel.cs`
- `Scripts/UI/SevenDaySingInPanel.cs`
- `Scripts/UI/normalShopList.cs`
- `Scripts/UI/Talentshow.cs`
- `Scripts/UI/ProbabilityDesc.cs`
- `Scripts/UI/LevelScrollController.cs`
- `Scripts/UI/TalentDetails.cs`
- `Scripts/UI/SkillDetails.cs`
- `Scripts/UI/GeneralCardSlot.cs`
- `Scripts/UI/CardSlot.cs`
- `Scripts/UI/CombatTipController.cs`
- `Scripts/UI/Soldier/AwakeningPanel.cs`

建议：

- 后续逐个复查是否属于 UI 动态节点、战斗特效、或仅为对象池预热/透传
- 不建议一次性全量盲改

---

## 7. 建议替换顺序

### 第一批（立即替换）

1. `ProbabilityDescPanelNew`
2. `CombatTipPanelNew`
3. `NormalShopList01New`

### 第二批（老界面过渡治理）

4. `ProbabilityDescPanel`
5. `NormalShopList01`
6. `GamePausePanel`

### 第三批（结构性优化）

7. `GameSettlementPanel1`
8. `LoginPanel`
9. `CreatePlayerMaincityPanel` 继续补全标题/属性项

---

## 8. 统一替换原则

### 8.1 新界面（UIFramework）

优先使用：

- `UIPoolScope`
- `PoolScope.ReleaseGroup(...)`
- `PoolScope.ReleaseAll()`
- `CommonItemPoolManager` / `UIPanelItemPoolManager` / `BatttleItemPoolManager`

### 8.2 老界面（QFramework）

优先使用过渡方案：

- 老 Panel 直接挂 `UIPoolScope`
- 保持原打开方式不变
- 仅收口动态节点生命周期

### 8.3 禁止继续扩散的写法

- `UIItemCreator.Instance` 直接作为界面对象池入口
- 面板里直接 `Instantiate` 大量可复用 UI item
- `CommonItemPoolManager.Instance.ReturnAll()` 这种全局粗暴回收
- 业务代码中散落的手动 `Get/Return` 且缺乏生命周期归属

---

## 9. 当前结论

当前项目中最值得优先替换的对象池界面主要集中在：

- 商店类列表界面
- 奖励展示界面
- 详情类动态 item 界面
- 已迁 UIFramework 但仍保留手工对象池管理的界面

现阶段建议采用：

- **新界面：UIFramework + UIPoolScope + 分组释放**
- **老界面：QFramework Panel 上直接挂 UIPoolScope 做过渡治理**

