# SchoolChooseCardPanelNew AI 上下文

> 供后续 AI 对话或开发接续使用。  
> 最后更新：2026-06-23  
> 代码文件：`Assets/Scripts/UI/SchoolChooseCard/SchoolChooseCardPanelNew.cs`  
> ViewModel：`Assets/Scripts/Game/SchoolChooseCard/ViewModels/SchoolChooseCardViewModel.cs`  
> 预制体：`Assets/Res/UIPrefab/ChooseCard/SchoolChooseCardPanel.prefab`

---

## 1. 界面定位

流派三选一（SchoolChooseCard）的 MVVM 视图层，继承 `UIPanel<SchoolChooseCardViewModel>`。

| 项 | 值 |
|----|-----|
| PrefabPath | `SchoolChooseCardPanel` |
| UILevel | `PopUI` |
| 旧 Panel | 同预制体上的 `SchoolChooseCardPanel`（Designer 字段引用） |
| 打开入口 | `SchoolChooseCardPanel.OpenSingleChoose()` / 战斗内多处 `OpenPanelAsync<SchoolChooseCardPanelNew, ...>` |

打开时 `mOldPanel.enabled = false`，关闭时恢复，避免旧 QFramework Panel 逻辑与 New 冲突。

**已弃用 `UIEntryController`**：打开/选中均不依赖飞入飞出，仅走 ChoseCard Animator 与 `EffectCard` 入场。

预制体根节点仍可能挂着 `UIEntryController` 组件，但须 **`m_Enabled = false`** 且 `UiObjects` 为空；否则 `Instantiate` 时 `OnEnable` 会因空引用报错。逻辑上不再调用 `SetUIObjects` / `Play`。

---

## 2. 预制体结构（ChoseCard）

每张可选卡牌节点命名：`ChoseCard1{槽位}`（如 `ChoseCard11` = 槽位 0，`ChoseCard12` = 槽位 1）。

| 组件 | 说明 |
|------|------|
| `Animator` | Controller：`Art/Effect/UI/ChooseCard/Ani/ani_choosecard01.controller`，`UpdateMode = UnscaledTime` |
| `CanvasGroup` | 根节点 alpha/scale；**禁止用 alpha=0 隐藏 Legend**（Animator 会写回 alpha，见 §9） |
| `Button` | 整卡点击选择 |
| 子节点 | `Title` / `Category` / `Card/Icon` / `Bg` / `Description` / `StarList` / `BelongSchool` / `SkillStar` / `SuperTip` / `recommendIcon` / `RefreshBtn` / `hor` 等 |

### UI 显隐约定（PopulateCardVisuals）

| 节点 | 规则 |
|------|------|
| `Title` / `Card` / `Bg` / `Description` / `StarList` / `Mask` | 卡牌展示时始终显示 |
| `Category` | **始终隐藏**（仍赋值 `CategoryName` 但不显示） |
| `SuperTip` | **始终隐藏** |
| `SkillStar` | 仅 `data.IsCategorySkill` 时显示 |
| `BelongSchool` | 非技能卡且有流派 icon 时显示 |
| `recommendIcon` | `data.IsRecommended` 时显示 |
| `RefreshBtn` / `hor` | 由 `SetAllRefreshAreaVisible` 控制（选中/关闭时隐藏） |

### Animator bool 参数（同一时刻仅一个为 `true`）

| 参数 | State 名 | 用途 |
|------|----------|------|
| `normal` | `ani_xk_yin_moren` | 默认 idle |
| `noselect` | `ani_xk_weixuanze` | 点击后未选中卡（会动画 CanvasGroup.alpha → 0） |
| `yin_danji` | `ani_xk_yin_dianji` | Rarity 选中 |
| `jin_danji` | `ani_xk_jin_dianji` | Epic 选中 |
| `cai_danji` | `ani_xk_cai_dianji` | Legend（彩色）选中 |

`PlayCardAnimatorClip`：**先 `animator.enabled = true`**，同步全部 bool，再 `animator.Play(stateName, 0, 0f)` + `Update(0)`，避免仅 `SetBool` 时 AnyState 不触发。

品质 → 选中动画映射：

| QualityType | Animator 参数 | State |
|-------------|---------------|-------|
| Rarity | `yin_danji` | `ani_xk_yin_dianji` |
| Epic | `jin_danji` | `ani_xk_jin_dianji` |
| Legend | `cai_danji` | `ani_xk_cai_dianji` |
| 其他 | fallback `yin_danji` | `ani_xk_yin_dianji` |

**注意：** `danji` 动画片段会改 `Title` / `Card` 的 `m_IsActive`；`ResetCardAnimator` 之后必须再调一次 `PopulateCardVisuals` 恢复节点显隐与文案。

---

## 3. 生命周期与渲染时机

```
OnBindViewModel
  → 绑定 VM / Service 事件
  → RefreshHeaderUI + RenderQualityWeights（不渲染卡牌列表）

OnOpen
  → mOldPanel.enabled = false
  → PauseGame（timeScale=0）
  → RefreshAllUI(VM, animateAllCards: true) // Legend 翻牌后播特效；Epic/Rarity 播品质粒子 + scaleX；其他非 Legend 只播 scaleX
  → TriggerGuide(FirstChooseCard)

OnShow
  → TriggerGuide(FirstChooseCard)

CardList 变更（含刷新单卡）
  → RenderCardList(animateAllCards: false, animateSlotIndex: _pendingAnimateSlotIndex)
  → _pendingAnimateSlotIndex 消费后置 -1

Update
  → VM.OnUpdate(Time.unscaledDeltaTime)   // 倒计时用 unscaled 时间
```

---

## 4. 卡牌渲染管线

```
RenderCardList
  → StopAllCardEffects（Kill tween + 恢复被中断卡的 UI，见下）
  → ResetAllCardEffectTransforms / ClearCardButtonHandlers / HideAllCardSlots
  → 【第一遍】对每个槽位 i：
       cardObj.SetActive(true)
  → 若 animateAllCards → EnsureChooseListLayoutReady()（GridLayout 排布后再播放入场）
  → 【第二遍】对每个槽位 i：
       RenderSingleCard(card, playAnimation)
       BindCardButtons
  → RefreshGoldCostUI / TriggerGuide(RefreshTip) / SetAllRefreshAreaVisible(true)
```

```
RenderSingleCard
  → SetLegendAnimatorEnabled(true)
  → PopulateCardVisuals
  → ResetCardAnimator
  → PopulateCardVisuals（Animator 可能改 m_IsActive，必须再刷一次）
  → RestoreCardCanvasGroup
  → EffectCard（Legend 翻牌后播常驻粒子；Epic/Rarity 播品质粒子 + scaleX；其他非 Legend 只播 scaleX）
```

| 方法 | 职责 |
|------|------|
| `PopulateCardVisuals` | 填充文案/图标/星级；统一设置各节点显隐（真值源） |
| `HideCardVisualsForLegendEntrance` | 历史翻转逻辑保留方法；当前入场规则不主动调用 |
| `PrepareCardForEntrance` | 历史翻转逻辑保留方法；当前入场规则不主动调用 |
| `EffectCard` | Legend 播放 `ChooseCardColor02` 翻牌后显示 `ChooseCardColor01`；Epic/Rarity 显示对应品质粒子并 scaleX；其他非 Legend 只 scaleX |
| `ShowLegendFlipEffect` | 历史翻转逻辑保留方法；当前入场规则不主动调用 |
| `PlayEffectParticlesUnscaled` | 粒子 `useUnscaledTime=true`，先 `Stop/Clear` 再 `Play(true)`，刷新复用特效时会从头播放 |
| `EnsureChooseListLayoutReady` | 打开面板时 `Canvas.ForceUpdateCanvases` + `LayoutRebuilder.ForceRebuildLayoutImmediate` |
| `RestoreCardCanvasGroup` | `alpha=1`，`scaleX=1`，`DOKill` |
| `RestoreAllActiveCardVisuals` | StopAllCardEffects 时：恢复 canvas + 启用 Animator + **PopulateCardVisuals** |
| `FinishLegendEntrance` | 历史翻转逻辑保留方法；当前入场规则不主动调用 |
| `PlayCardAnimatorClip` | 启用 Animator + Play State + 同步 bool |

### Legend 入场隐藏节点列表（`LegendEntranceHiddenNodes`）

`Title`, `Category`, `Card`, `Bg`, `Description`, `StarList`, `Mask`, `BelongSchool`, `SkillStar`, `recommendIcon`, `SuperTip`, `RefreshBtn`, `hor`

当前规则下 `ChooseCardColor02(Clone)` 不参与打开/刷新入场，仅保留历史翻转清理逻辑。

---

## 5. 品质入场特效（EffectCard）

所有 DOTween 使用 `.SetUpdate(true)` 或 `DOVirtual.DelayedCall(..., ignoreTimeScale: true)`，**暂停游戏时仍可播放**。

品质粒子（`ChooseCardColor02` / `ChooseCardColor01` / `ChooseCardGold01` / `ChooseCardSilver01`）预制体默认 `useUnscaledTime=0`，显示时必须经 `PlayEffectParticlesUnscaled` 先清空再播放，否则 `OnOpen` 里 `PauseGame()`（`timeScale=0`）后粒子不 sim，刷新复用同一个特效时也不会从头播。

| 品质 | playEntranceAnimation=true（打开/刷新该槽） | playEntranceAnimation=false（普通重绘） |
|------|---------------------------------------------|----------------------------------------|
| **Legend** | `ShowLegendFlipEffect`（color02 播放到结束）→ 0.4s → 显示 legend 粒子，不播放 scaleX | 直接显示 legend 粒子 |
| **Epic** | 显示 gold 粒子 + scaleX 0→1（0.3s），不播放 color02 翻转 | 直接显示 gold 粒子 |
| **Rarity** | 显示 silver 粒子 + scaleX 0→1（0.3s），不播放 color02 翻转 | 直接显示 silver 粒子 |
| **其他非 Legend** | 只播放 scaleX 0→1（0.3s） | 直接显示 |

### 非 Legend ScaleX 入场时间线

```
t=0      ResetCardAnimator → PopulateCardVisuals → RestoreCardCanvasGroup → scaleX 0→1
t=0.3s   scaleX 完成
```

### 打开 vs 刷新：布局时机

| 场景 | 布局 | 入场表现 |
|------|------|---------------|
| **打开**（`animateAllCards=true`） | 先全部 `SetActive(true)` → `EnsureChooseListLayoutReady` → 再 `RenderSingleCard` | Legend 只播特效；非 Legend 只播 scaleX |
| **刷新单卡** | 卡牌已在正确位置，无需强制布局 | Legend 翻牌后播特效；Epic/Rarity 播品质粒子 + scaleX；其他非 Legend 只播 scaleX |

粒子资源名：

- `ChooseCardColor01`（Legend 常驻粒子）
- `ChooseCardColor02` / `ChooseCardGold01` / `ChooseCardSilver01`（历史逻辑保留，当前打开/刷新入场不主动播放）

Tween 记入 `_cardEffectTweens`。

非 Legend scaleX 入场前必须先 `RestoreCardCanvasGroup`，避免同帧 `DOKill` 清掉新 tween。

---

## 6. 选中 / 全选动画

### 单选（OnClickChoose）

1. `CancelLegendEntranceForCard` + `PrepareCardForChooseAnimation`（启用 Animator、Populate、避免入场回调覆盖选中动画）
2. `SetAllCardButtonsInteractable(false)` + `SetAllRefreshAreaVisible(false)`
3. `PlayChooseCardAnimations`：选中卡 → 品质 `danji`；其余 → `noselect`
4. `ChooseCloseDelay`（0.5s）后 `ChooseCard` + `ChooseOver`

### 全选（OnClickGetAll）

对每张卡：`CancelLegendEntranceForCard` → `PrepareCardForChooseAnimation` → 播对应 `danji` → 0.5s 后批量 `ChooseCard` + `ChooseOver`

---

## 7. 刷新单卡

```csharp
_pendingAnimateSlotIndex = slotIndex;
VM.CardList.Value = updatedCards;
```

- 被刷新槽：`playAnimation=true`，Legend 只播放 `ChooseCardColor01`，非 Legend 只播放 0.3s scaleX 入场
- **其余槽**：`playAnimation=false`，`RenderSingleCard` 双次 `PopulateCardVisuals` 保证信息可见
- `StopAllCardEffects` 会打断其他槽 scale tween，并通过 `RestoreAllActiveCardVisuals` + 后续 `RenderSingleCard` 恢复 UI

---

## 8. 关键字段

| 字段 | 说明 |
|------|------|
| `_pendingAnimateSlotIndex` | 刷新后待播放入场的槽位索引，CardList 回调消费后置 -1 |
| `_cardEffectTweens` | scale、DelayedCall、关闭延迟等 DOTween |
| `_legendEntranceTweens` | 历史翻转逻辑保留字段，当前打开/刷新入场不主动使用 |
| `_detachedFlipEffects` | 历史翻转逻辑保留字段，当前打开/刷新入场不主动使用 |
| `_starTweens` | 星级 Shine 循环动画 |
| `_refreshBtnHandlers` / `_chooseBtnHandlers` | 按槽位解绑按钮，禁止 `RemoveAllListeners` |

### 常量

| 常量 | 值 | 说明 |
|------|-----|------|
| `LegendEntranceDelay` | 0.4f | 历史翻转逻辑保留常量，当前打开/刷新入场不主动使用 |
| `ChooseCloseDelay` | 0.5f | 选中动画后提交并关面板 |

---

## 9. 历史问题与规避（必读）

| 问题 | 原因 | 当前规避 |
|------|------|----------|
| 全部卡牌 alpha=0 | 用 `CanvasGroup.alpha=0` 隐藏 + tween 在 pause 下不完成 | Legend 用 **子节点 SetActive** 隐藏；Epic/Rarity 不藏 alpha；tween 用 `SetUpdate(true)` |
| Legend 翻转不显示 | color02 reparent 到场景根脱离 Canvas | reparent 到 **面板 `transform`** + `SetAsLastSibling` |
| Legend 翻转后仍 alpha=0 | Animator `ani_xk_weixuanze` 等会写 `m_Alpha` | **不用 alpha 隐藏**；保持 alpha=1；用子节点隐藏 |
| 选中彩色不播 `cai_danji` | 入场时 Animator 关闭；0.4s 回调 `ResetCardAnimator` 覆盖选中 | 选中前 `CancelLegendEntranceForCard`；`PlayCardAnimatorClip` 强制 `enabled=true` |
| 点击后出现 SkillStar/SuperTip | `SetActive(true)` 整棵子树 | 只用 `PopulateCardVisuals` 控制显隐；SuperTip/Category 固定隐藏 |
| 刷新打断入场后他牌无信息 | Animator `ResetCardAnimator` 把 Title/Card `m_IsActive` 写回 false | `ResetCardAnimator` **之后**再 `PopulateCardVisuals`；`StopAllCardEffects` 时也 Populate |
| 刷新时 legend tween 泄漏/串台 | 全局 Kill 不清 per-card 状态 | `_legendEntranceTweens` 按卡记录；`CancelLegendEntranceForCard` |
| Animator 点击不播 | 仅 SetBool | `PlayCardAnimatorClip` 直接 Play State |
| noselect 不生效 | Controller 过渡误绑 | `noselect` → `ani_xk_weixuanze` |
| 打开时 Legend 中间光不显示 | GridLayout 未排布就播 color02；粒子受 `timeScale=0` 影响 | 打开时两遍渲染 + `EnsureChooseListLayoutReady`；`PlayEffectParticlesUnscaled` |
| `UIEntryController` 报错 | 预制体组件仍启用且 `UiObjects` 含空引用 | 预制体禁用组件并清空 `UiObjects`；`UIEntryController.OnEnable` 跳过 null 项 |

### 开发约束

1. **不要**用 `SetLegendCardContentVisible` 式「整棵子树 SetActive」——会误开 `SkillStar`/`SuperTip`
2. **不要**用根节点 `alpha=0` 或 `scaleX=0` 做 Legend 入场
3. **任何** `ResetCardAnimator` / `PlayCardAnimatorClip` 之后，若需正确 UI，应再调 `PopulateCardVisuals`
4. 新增可隐藏 UI 节点时，同步更新 `LegendEntranceHiddenNodes` 与 `PopulateCardVisuals` 显隐规则
5. 事件绑定优先具名方法；禁止 `RemoveAllListeners`；日志用 `LogTool`
6. 显示品质粒子必须走 `PlayEffectParticlesUnscaled`，不要依赖粒子预制体 `playOnAwake` 在暂停下自动播放
7. 不要在三选一预制体上启用 `UIEntryController`；该面板入场已由 `EffectCard` 负责

---

## 10. 测试检查清单

- [ ] 打开面板：Legend 翻牌后显示 `ChooseCardColor01`；Epic/Rarity 显示对应品质粒子并 scaleX；其他非 Legend 只 scaleX
- [ ] 打开面板不报 `UIEntryController` / `UnassignedReferenceException`
- [ ] `Category` / `SuperTip` 全程不显示
- [ ] 彩色入场中刷新另一张卡：两张卡信息（标题/图标/描述）均正常
- [ ] 刷新 Legend 槽：只播放 `ChooseCardColor01`；刷新非 Legend 槽只播放 scaleX
- [ ] 点击 Legend：`cai_danji=true`，播 `ani_xk_cai_dianji`；其余 `noselect`
- [ ] 点击 Rarity/Epic：对应 `yin_danji` / `jin_danji`
- [ ] 入场未完成时点击选中：仍能播选中动画并正常关闭
- [ ] 倒计时自动选择、全选、关闭面板无 tween 泄漏
- [ ] 游戏暂停（timeScale=0）下动画与倒计时正常
- [ ] 关闭后 `mOldPanel.enabled` 恢复

---

## 11. 相关文件

| 路径 | 说明 |
|------|------|
| `SchoolChooseCardPanelNew.cs` | 视图主逻辑 |
| `SchoolChooseCardViewModel.cs` | 数据、倒计时、CardList |
| `SchoolChooseCardService.cs` | 抽卡、刷新、ChooseOver |
| `ani_choosecard01.controller` | ChoseCard Animator |
| `ani_xk_cai_dianji.anim` 等 | 选中动画（会改 Title/Card active） |
| `UIEntryController.cs` | 其他面板飞入动画；三选一预制体上须禁用 |
| `ChooseCardColor02.prefab` | Legend 打开/刷新时的中间翻转光效 |
