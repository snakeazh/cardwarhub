# 功能解锁强引导接入文档

本文档是给「新模块接入功能解锁强引导」的实操手册，提炼自奥术卡分解、奥术卡共鸣、融合石抽奖、奇幻秘境四个模块的实际接入。模块整体机制见 `ModuleFunction_Usage.md`。

---

## 1. 接入总览

接入一个新模块的解锁引导 = **配置 1 张表 + 代码埋 2 类点**：

| 侧 | 内容 | 位置 |
|---|---|---|
| 配置 | `ModuleFunctionConfig.xlsx` 加/改一行：`TipType=强引导`、`UnlockUserLv`、`StepList`（逐步）、`GuidePriority`、`NextGuideModule` | `Luban/Datas/GamePlay/ModuleFunctionConfig.xlsx`，改完跑 `Luban/gen.bat` |
| 埋点 A（注册目标） | 每步要高亮的 UI 节点：`UnlockGuideManager.Instance.SetUnlockTypeStep(模块, stepId, rectTransform)` | 各业务面板绑定期 |
| 埋点 B（推进步骤） | 用户完成该步操作处：`UnlockGuideManager.Send(模块, stepId)` | 各业务面板点击 handler |

**第 1 步如果是主界面底部导航按钮，只需注册、不用写 Send**——`GameMainPanelNew.OnBottomButtonClick` 对任意导航点击都会 `SendNavigation(1)`（以当前引导模块发送）。注册统一加在 `GameMainPanelNew.RefreshGuideRef()`：

```csharp
UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.Xxx, 1, GameMainButtonType.Yyy);
```

**最后一步只需 Send，不用调 `UnlockGuideDone`**——`Send` 的 step ≥ 配置最后 StepId 时管理器自动结束引导并 `SaveModuleStatus(cfg.Id)` 落服务器。

所有埋点遵守两条铁律：

1. 注册一律带判空：`if (UnlockGuideManager.Instance != null)`（管理器登录时才创建，编辑器/异常时序下可能为 null）。
2. Send 无条件调用即可——管理器会按「当前引导模块 + 当前步骤」过滤，别的模块/别的入口误触发不影响。

## 2. 配置要点与常见坑

- `TipType` 必须选「强引导」，否则管理器 `Initialized()` 直接跳过，怎么埋点都不触发（奥术卡共鸣就踩过：步骤配好了但 TipType=「无」）。
- `NextGuideModule` 不接续时填 **None**；留空会落成 `0`=SetCardGroup，可能意外接续别的引导。
- `NextGuideModule` + `NextGuideStartStep` 可做引导串联。例：奥术卡(39) 配 `NextGuideModule=51, NextGuideStartStep=2`——奥术卡引导结束自动接续分解引导第 2 步（玩家已在奥术卡面板内，跳过第 1 步点入口）。串联的模块不会单独入队。
- `StepList` 步骤字段：`DialogText`/`ShowDialoge` 控制对话，`ClickMaskNext=ClickTarget` 表示必须点高亮目标；最后一个 StepId 即结束步。
- 各行 `Id` 是引导完成状态的落库键，上线后勿改。
- 对话文案别从别的行复制忘了改（奇幻秘境 step1 文案误用「融合石抽奖，快来体验一下」即为例）。

## 3. 各类型步骤的埋点模式

### 模式 A：主界面导航按钮（通常是第 1 步）

`GameMainPanelNew.RefreshGuideRef()` 加一行注册即可，Send 免写。已接入：GeneralSkill、ArcCardDecompose、ArcCardResonance（→ `GameMainButtonType.GeneralSkill`），GemLottery（→ `Shop`），EndlessTower（→ `World`）。

### 模式 B：普通按钮

绑定期注册 + 点击 handler 首行 Send。例（奥术卡分解 step2，GeneralSkillPanelNew.BindCombineButton）：

```csharp
_combineButtonHandler = () =>
{
    UnlockGuideManager.Send(ModuleFunctionType.ArcCardDecompose, 2);
    vm.NotifyOpenConbinePanel();
};
mOldPanel.CombineBtn.onClick.AddListener(_combineButtonHandler);

if (UnlockGuideManager.Instance != null)
{
    UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardDecompose, 2, mOldPanel.CombineBtn.GetComponent<RectTransform>());
}
```

### 模式 C：Toggle 页签（UISelectableToggle）

在 `isOn == true` 分支里 Send。例（奥术卡共鸣 step3，BottomBarNew）：

```csharp
_resonanceToggleHandler = (isOn) =>
{
    if (isOn)
    {
        UnlockGuideManager.Send(ModuleFunctionType.ArcCardResonance, 3);
        vm.OnTabSelectedCommand.Execute(BottomBarViewModel.TabType.Resonance);
    }
};
```

### 模式 D：滚动列表的某一项

在列表项更新回调里按下标/数据条件注册，项的 OnClick 里 Send。例（奥术卡分解 step4，GeneralCardResolvePanelNew.OnUpdateCardItem）：

```csharp
bagScrollItem.OnClick = () =>
{
    UnlockGuideManager.Send(ModuleFunctionType.ArcCardDecompose, 4);
    // ...原逻辑
};

if (dataIndex == 0 && UnlockGuideManager.Instance != null)
{
    UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.ArcCardDecompose, 4, scrollItem.GetComponent<RectTransform>());
}
```

按业务数据定位的例（融合石抽奖 step2，ChestCardsPanel01New.UpdateCardPackItem）：

```csharp
if (cfg.ChestPoolType == ChestPoolType.FusionStone && UnlockGuideManager.Instance != null)
{
    UnlockGuideManager.Instance.SetUnlockTypeStep(ModuleFunctionType.GemLottery, 2, oneDrawBtn.GetComponent<RectTransform>());
}
```

### 模式 E：公用弹窗组件

公用面板（如 ChooseCardNumPanelNew）也可以直接埋点——Send 会被管理器按模块过滤，别处使用不受影响。引导激活时遮罩挡住其他入口，公用组件不会被提前触发。

## 4. 关键机制约束（踩坑预警）

1. **遮罩孔只对齐一次，不逐帧跟随**（`UnlockGuidePanel.SetTarget`）。步骤目标注册后协程等 0.1s 就定位遮罩孔。如果目标会移动（滚动列表、飞入动画），必须在目标被取走**之前**把位置就位：
   - 需要"滚到底/滚到某处"时，用**瞬时滚动**（`scrollRect.verticalNormalizedPosition = 0`，先 `Canvas.ForceUpdateCanvases()`），不要用动画滚动。参考 ChestCardsPanel01New.OnOpen 的 GemLottery 处理。
2. **目标可以晚注册**：引导协程会逐帧等 `GetStepTarget(currentStep)` 非空（超时 100s 关引导）。所以"上一步点击后才打开的弹窗"里的按钮，在该弹窗绑定期注册即可。
3. **stepMap 按模块持久**：面板关闭再开，重新注册只是覆盖，无副作用。
4. **结束即落档**：最后一步 Send → 自动 `SaveModuleStatus` → 重登不重复播。
5. **引导面板在 UILevel.Guide 层**，目标节点会被加 `Canvas.overrideSorting + GraphicRaycaster` 提层可点，其余区域被遮罩挡住——所以不用担心引导期间玩家点到别的东西。
6. **串联引导的衔接环境**：上一个引导在哪个界面结束，下一个引导的第一步目标就得在哪个界面可达，否则会卡在等待目标。配置 `NextGuideStartStep` 时想清楚玩家当时站在哪个界面。

## 5. 已接入模块速查

| 模块 | ModuleType | 解锁等级 | 步骤 | 特殊处理 |
|---|---|---|---|---|
| 奥术卡 GeneralSkill | 39 | 4 | 3 | 结束接续 ArcCardDecompose step2 |
| 奥术卡分解 ArcCardDecompose | 51 | 4 | 6 | 跨 4 个面板（导航→CombineBtn→BottomBar页签→列表项→数量弹窗→分解按钮） |
| 奥术卡共鸣 ArcCardResonance | 50 | 4 | 4 | 同分解前半段，UpGradeBtn 结束 |
| 融合石抽奖 GemLottery | 52 | 7 | 2 | 商店默认页即抽卡页；OnOpen 瞬时滚到底部，融合石池(ChestPoolType.FusionStone)单抽按钮为目标 |
| 奇幻秘境 EndlessTower | 24 | 12 | 2 | 世界按钮→BigWorldPanelNew.EndlessTowerBtn |

## 6. 接入自检清单

- [ ] xlsx：TipType=强引导、UnlockUserLv、StepList 逐步与代码 stepId 一一对应、最后一步即结束步、NextGuideModule 不接续填 None、文案无复制错误
- [ ] 跑 `Luban/gen.bat`，确认 json 更新
- [ ] 每步目标已注册（带 Instance 判空），目标会动的场景已在取目标前就位
- [ ] 每步操作处已 Send（导航第 1 步除外）
- [ ] 入口按钮/红点规则已接 `ModuleFunction.CheckOpen`
- [ ] 实机：跨级触发 → 逐步高亮可点 → 结束后落档 → 重登不重复
- [ ] 配了串联时：前置结束后接续引导的第一个目标在当前界面可达
