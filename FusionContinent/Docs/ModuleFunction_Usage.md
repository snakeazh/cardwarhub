# 功能解锁模块（ModuleFunction）使用文档

本文档说明功能解锁模块的整体结构、数据流，以及**如何为一个新模块添加功能解锁 + 解锁引导**。

---

## 1. 模块总览

功能解锁系统负责：按**玩家等级**控制各功能入口的开放，并在解锁时弹出解锁弹窗（飞图标动画）、播放强引导（遮罩 + 手指 + 对话）。

数据流：

```
ModuleFunctionConfig.xlsx（Luban 配表）
        │  Luban gen.bat 生成
        ▼
tbmodulefunctionconfig.json + Config.ModuleFunctionConfig / ModuleFunctionType（客户端代码）
        │
玩家升级 → MessagePublisher.CHANGE_USER_LEVEL_INFO
        │
        ├─ MainInterfacePanelNew.DailyPopUpLogic()
        │     ├─ FlyIcon=true 的模块 → UpgradePanel（升级解锁弹窗 + 图标飞跳到入口按钮）
        │     └─ UnlockGuideManager.StartGuide() → UnlockGuidePanel（强引导：遮罩/手指/对话）
        │
        ├─ UnlockGuideManager.UpdateLevelTrigger() → unlockQueue（按 GuidePriority 排序）
        │
        └─ 入口按钮 & 红点规则：ModuleFunction.CheckOpen(type) 实时判定
```

关键结论：

- **解锁条件只有「玩家等级」一个维度**（`UnlockUserLv` 字段 + `Status` 总开关），没有关卡/任务/道具条件。
- 判定唯一入口是静态类 `ModuleFunction.CheckOpen()`；没配表的功能**默认开放**。
- 「引导是否已做过」是独立数据，通过 proto 落服务器（`Global.PlayerModules`），与等级判定互不影响。

## 2. 核心文件清单

| 职责 | 文件 |
|---|---|
| 表结构定义 | `Luban/Defines/GamePlay.xml`（`ModuleFunctionConfig` bean，约 1098 行；`ModuleFunctionType` 枚举，约 337 行） |
| 配表数据源 | `Luban/Datas/GamePlay/ModuleFunctionConfig.xlsx` |
| 生成产物 | `Config/Luban/c/tbmodulefunctionconfig.json`、`FusionContinent/Assets/Scripts/Data/Client/Luban/ModuleFunctionConfig.cs`、`ModuleFunctionType.cs` |
| 解锁判定 | `FusionContinent/Assets/Scripts/Model/ModuleFunctionModel.cs`（静态类 `ModuleFunction`） |
| 引导完成状态 | `FusionContinent/Assets/Scripts/Model/PlayerModuleModel.cs`（`CheckModuleStatus` / `SaveModuleStatus`，走 proto `ApiModuleSaveStatus`，定义在 `Protos/Proto/module.proto`） |
| 解锁弹窗 + 飞图标 | `FusionContinent/Assets/Scripts/UI/UpgradePanel.cs` |
| 强引导管理器 | `FusionContinent/Assets/Scripts/UI/UnlockGuide/UnlockGuideManager.cs` |
| 强引导面板 | `FusionContinent/Assets/Scripts/UI/UnlockGuidePanel.cs`（UILevel.Guide 层） |
| 升级触发入口 | `MainInterfacePanelNew.DailyPopUpLogic()`（`MainInterfacePanelNew.cs` 约 1140 行） |
| 导航栏目标注册 | `GameMainPanelNew.cs` 约 296 行（`SetUnlockTypeStep` 导航重载的统一注册处） |

## 3. 配置字段说明

### ModuleFunctionConfig（每行一个功能）

| 字段 | 类型 | 说明 |
|---|---|---|
| `Id` | int | 行 Id，同时也是引导完成状态落服务器的 `ModuleId` |
| `ModuleType` | ModuleFunctionType | 功能枚举（见下） |
| `Name` | string | 功能名（解锁弹窗显示） |
| `WorldIcon` / `Icon` | string | 解锁弹窗图标 |
| `Desc` | string | 未解锁时点击入口弹的提示，如"玩家等级2解锁" |
| `Status` | int | 0=永久关闭，1=启用 |
| `UnlockUserLv` | int(ref TbPlayerLevelConfig) | 解锁所需玩家等级（唯一解锁条件） |
| `UnlockRewards` | list,RewardAndNum | 解锁奖励 |
| `TipType` | ModuleTipType | `StrongGuide`=播强引导 / `NoGuide`=无引导 |
| `StepList` | list,ModuleStepListStruct | 引导步骤（TipType=StrongGuide 时必填） |
| `FlyIcon` | bool | 是否进升级解锁弹窗并做飞图标动画 |
| `GuidePriority` | int | 多个引导同时触发时的排队优先级 |
| `NextGuideModule` | ModuleFunctionType | 本引导完成后接续的下一个模块引导 |
| `NextGuideStartStep` | int | 接续引导的起始 StepId |

### ModuleStepListStruct（引导步骤）

| 字段 | 说明 |
|---|---|
| `StepId` | 步骤 Id，与代码里 `SetUnlockTypeStep` / `Send` 的 stepId 一一对应 |
| `CommitServer` | 该步是否上报服务器 |
| `DialogType` | 目前只有 `Finger`（手指） |
| `DialogText` | 引导对话文案 |
| `DialogOffset` | float3，对话/手指偏移 |
| `ShowDialoge` | GuideShowType，对话显示方式 |
| `ClickMaskNext` | `ClickTarget`=必须点目标 / `ClickMask`=点遮罩任意处进下一步 |

### ModuleFunctionType 枚举

定义在 `Luban/Defines/GamePlay.xml`（不是 `__enums__.xlsx`），当前 54 个条目（Id 0–53），例如：`SetCardGroup(0)卡组`、`Shop(1)`、`Talent(8)天赋`、`Equip(9)装备`、`ActivityImport(14)活动`、`FirstReCharge(19)首充`、`PVP(36)`、`GeneralSkill(39)奥术卡`、`GemInlay(53)` 等。**新增模块取下一个未用 Id，不要复用 `None(38)` 或已占用的值。**

配套枚举（同在 GamePlay.xml）：`ModuleTipType`、`ModuleDialogType`、`GuideNextEvent`、`GuideShowType`。

## 4. 运行时机制

### 4.1 解锁判定

```csharp
// FusionContinent/Assets/Scripts/Model/ModuleFunctionModel.cs
public static bool CheckOpen(ModuleFunctionType type, bool isShowTip = true)
{
    var cfg = DataConfigManager.Tables.TbModuleFunctionConfig.DataList.Find(ele => ele.ModuleType == type);
    if (cfg == null) return true;                       // 没配置 = 默认开放
    if (cfg.Status == 0 || cfg.UnlockUserLv > Global.User.Level)
    {
        if (isShowTip) Toast.Show(cfg.Desc);            // 弹"玩家等级X解锁"
        return false;
    }
    return true;
}
```

所有入口按钮、红点规则都调它。红点规则写法（首行拦截）：

```csharp
// 例：Game/RedDot/Business/Rules/TalentUpgradeRedDotRule.cs
if (!ModuleFunction.CheckOpen(ModuleFunctionType.Talent, false)) return 0;
```

### 4.2 升级触发

玩家升级 → `CHANGE_USER_LEVEL_INFO` 消息 → 两条线：

1. **解锁弹窗**：`MainInterfacePanelNew.CheckModuleUnlockLv()` 收集「上次展示等级 < UnlockUserLv ≤ 当前等级」且 `FlyIcon=true` 的模块 → 打开 `UpgradePanel.SetData(lastLevel, currentLevel)` → 关闭时 `DoItemMoveAni()` 用 DOTween `DOJump` 把图标飞到入口按钮（`GetItemTarget()` 按 ModuleType 映射目标）。`LastDisplayLevel` 防重复弹。
2. **强引导入队**：`UnlockGuideManager.UpdateLevelTrigger()` 把落在等级区间内的 `StrongGuide` 模块 `Trigger()` 进 `unlockQueue`，按 `GuidePriority → UnlockUserLv → Id` 排序。主界面 `DailyPopUpLogic()` 调 `StartGuide()` 取队首 → `ShowGuide()` 打开 `UnlockGuidePanel`。

引导串联：`IsWaitingForPrerequisiteGuide()` 检查是否有未完成引导通过 `NextGuideModule` 指向本模块，有则暂不入队；前置完成后 `TryContinueAfterGuideDone()` 用 `StartGuideFrom(next, NextGuideStartStep)` 接续。

### 4.3 引导播放与步骤推进

- **目标注册**：业务面板在合适时机调用
  `UnlockGuideManager.Instance.SetUnlockTypeStep(moduleType, stepId, rectTransform)`
  注册该步骤要高亮的目标节点；导航栏按钮用重载
  `SetUnlockTypeStep(moduleType, stepId, GameMainButtonType)`（在 `GameMainPanelNew` 统一注册）。
- **遮罩与高亮**：`UnlockGuidePanel.StartGuide()` 协程等待目标节点出现 → `SetTarget()` 把 `Maskabled` 遮罩孔对齐目标矩形 → `AddCanvas()` 给目标加 `Canvas.overrideSorting + GraphicRaycaster` 使其可点（其余区域被遮罩挡住）→ 显示手指和对话。
- **步骤推进**：业务代码在用户完成操作处调
  `UnlockGuideManager.Send(moduleType, step)`（静态，内部发 `CHANGE_UNLOCK_GUIDE_CHANGE` 消息）→ `OnGuideChange` → `_panel.RunNextStep()`。
- **结束**：最后一步后调 `UnlockGuideDone(moduleType)` → `FinishGuideInternal()` → `PlayerModuleModel.SaveModuleStatus(cfg.Id)` 落服务器 → 接续 `NextGuideModule` 或队列下一个。

## 5. 新增一个模块的功能解锁引导（操作步骤）

> 实操版接入手册（含各类型步骤的埋点模式与踩坑预警）见 `UnlockGuide_Integration.md`。

以新模块 `Xxx` 为例：

1. **加枚举**：`Luban/Defines/GamePlay.xml` 的 `ModuleFunctionType` 中新增 `<var name="Xxx" value="54" />`（取下一个未用 Id，勿复用）。
2. **配表**：`Luban/Datas/GamePlay/ModuleFunctionConfig.xlsx` 新增一行：
   - `ModuleType=Xxx`、`Name`、`Icon`、`Desc`（未解锁提示，如"玩家等级10解锁"）
   - `Status=1`、`UnlockUserLv=解锁等级`、`UnlockRewards`（可空）
   - `FlyIcon`：要进升级解锁弹窗飞图标就填 true
   - 不需要引导：`TipType=NoGuide`，`StepList` 留空，跳到第 6 步
   - 需要强引导：`TipType=StrongGuide`，配 `GuidePriority`，并按引导流程逐行配 `StepList`（`StepId`/`DialogText`/`DialogOffset`/`ClickMaskNext`）
   - 可选：`NextGuideModule` + `NextGuideStartStep` 串联下一个模块引导
3. **生成配置**：运行 `Luban/gen.bat`，确认 `tbmodulefunctionconfig.json` 和 `ModuleFunctionType.cs` 已更新。
4. **入口接判定**：入口按钮点击处 `if (!ModuleFunction.CheckOpen(ModuleFunctionType.Xxx)) return;`；入口显隐用 `CheckOpen(Xxx, false)`；该模块的红点规则首行加同样判断。
5. **注册引导目标**（仅强引导）：相关面板在目标节点可用时调用
   ```csharp
   UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Xxx, stepId, targetRectTransform);
   ```
   导航栏按钮走 `GameMainPanelNew` 约 296 行的统一注册处（`GameMainButtonType` 重载）。
6. **推进步骤**（仅强引导）：用户完成每步操作处调
   ```csharp
   UnlockGuideManager.Send(ModuleFunctionType.Xxx, stepId);
   ```
   最后一步完成后确保走到 `UnlockGuideManager.Instance.UnlockGuideDone(ModuleFunctionType.Xxx)`，引导状态才会落服务器。
7. **飞图标映射**（仅 FlyIcon=true）：在 `UpgradePanel.GetItemTarget()` 中补一条 `ModuleFunctionType.Xxx → 入口按钮 GameObject` 的映射，否则解锁弹窗的图标没有飞行目标。
8. **自验清单**：
   - 升级跨过 `UnlockUserLv` 时，解锁弹窗（FlyIcon）与引导按 `GuidePriority` 顺序触发
   - 遮罩孔正确对齐目标节点，目标可点、其余区域被挡住
   - 每个 `Send(stepId)` 能推进到下一步，最后一步后 `SaveModuleStatus` 成功（重登不重复播）
   - 未达等级点击入口弹 `Desc` 提示，红点不亮
   - 配了 `NextGuideModule` 时，本引导结束后正确接续

## 6. 现有范例

### 例 1：天赋 Talent —— 全链路强引导

- 配置：`ModuleType=8`，`TipType=StrongGuide`，`StepList` 多步。
- 目标注册：`GameMainPanelNew.cs:296`（导航）+ `Talentshow.cs:439/453`、`TalentDetails.cs:50-51`、`TalentPanel.cs:157`：
  ```csharp
  UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Talent, 5, guideProTarget.GetComponent<RectTransform>());
  ```
- 步骤推进：`Talentshow.cs:79` `UnlockGuideManager.Send(ModuleFunctionType.Talent, 3);`
- 红点：`TalentUpgradeRedDotRule.Evaluate()` 首行 `CheckOpen(Talent, false)` 拦截。

### 例 2：首充 FirstReCharge —— 仅解锁弹窗，无引导

- 配置：`ModuleType=19`，`FlyIcon=true`，`TipType=NoGuide`（无 StepList）。
- 表现：升级 → `UpgradePanel` 显示首充图标 → 关闭时图标飞跳到 `FirstRechargeBtn`（`UpgradePanel.GetItemTarget()` 内映射）；入口显隐由 `MainInterfacePanelNew` 用 `CheckOpen(FirstReCharge, false)` 控制。全程无引导、不落 PlayerModule。

## 7. 注意事项

- **项目里另有两套引导，不要混淆**：
  - `GuideManager` + `TbGuideConfig`（`Luban/Defines/GuideFlow.xml`）：战斗内/教学引导，按 `GuideType` 由战斗事件触发，与功能解锁系统无耦合。
  - `GuideFlow` 面板 + `TbGuideFlowConfig`：主线引导任务/奖励，**目前 `GuideFlow.RefreshData()` 第一行即 return，主体逻辑已停用**。
- 解锁条件仅支持玩家等级。若新需求要求关卡/任务等条件，需要扩展 `ModuleFunctionConfig` 字段和 `ModuleFunction.CheckOpen()` 判定逻辑。
- `Id` 一旦上线不要随意改动——它是玩家引导完成状态（`PlayerModule.ModuleId`）的落库键，改 Id 等于重置该功能的引导记录。
