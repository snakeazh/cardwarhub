# 卡琳 Buff 联动（KaLinCd / KaLinDamage）改动说明

## 功能概述

当怪物携带 `GameBattleConst.KaLinBuffId`（配置值 `2140104`）时，若技能对其造成技能伤害，且技能携带以下属性，则触发额外效果：

| 属性 | AttributeType | 效果 |
|------|---------------|------|
| 暗杀者冷却修正 | `KaLinCd` (205) | 将技能卡**当前剩余 CD** 缩短至 `Lasttime0 × Max(1 + KaLinCd, \|BaseValue\|)`，多段叠加不低于 BaseValue 对应比例 |
| 暗杀者额外增伤 | `KaLinDamage` (206) | 税后伤害再结算一次相同数值（不重复跑伤害管线） |

典型示例：CD 17s、`KaLinCd = -0.5` → 剩余 CD 变为 **8.5s**。

---

## 改动文件清单

| 文件 | 类型 | 说明 |
|------|------|------|
| `Scripts/Game/DamageCalculator/KaLinBuffCombatSupport.cs` | **新增** | 卡琳 Buff 检测、CD 修正、追加伤害 |
| `Scripts/Game/DamageCalculator/MonsterDamageService.cs` | 修改 | 伤害结算后接入卡琳联动 |
| `Scripts/Game/DamageCalculator/MonsterDamageRequest.cs` | 修改 | 增加 `SuppressKaLinCooldown` 防多段重复修正 |
| `Scripts/Game/GameLand.SkillCards.cs` | 修改 | CD 字典写入、动态缩短 CD、快照日志、UI 事件 |
| `Scripts/Battle/UI/BattleSkillsPanelNew.cs` | 修改 | CD 修正后从 GameLand 重新同步 UI |

### 依赖配置（未改代码，仅引用）

| 文件 | 说明 |
|------|------|
| `Res/Config/Json/tbgamebattleconst.json` | `KaLinBuffId: 2140104` |
| `Res/Config/Json/tbattributeconfig.json` | `KaLinCd`(205)、`KaLinDamage`(206) 属性定义 |
| `Res/Config/Json/tbspecialeffectsconfig.json` | 如技能 `2140104` 的 `KaLinCd: -0.5` |

---

## 各文件改动详情

### 1. `KaLinBuffCombatSupport.cs`（新增）

**职责：** 卡琳联动核心逻辑，与伤害管线解耦。

**主要 API：**

- `HasKaLinBuff(MonsterCard)` — 检查怪物 `BuffId == TbGameBattleConst.KaLinBuffId`
- `TryApplyCooldownReduction(ISkill hittingSkill, SoldierCard soldierCard)` — CD 修正
- `TryApplyDuplicateDamage(...)` — 追加一次税后伤害

**CD 目标解析：**

- 优先：命中技能自身带 `KaLinCd` 且 `GameLand` 中正在冷却
- 备选：扫描英灵已学技能，找带 `KaLinCd` 且正在冷却的技能卡（解决命中技能为普攻 `2140101`、CD 在技能卡 `2140104` 上的情况）

**KaLinCd 读取（重要）：**

- 配置 `KaLinCd = -0.5`，但属性表 `ValueLimit.Minimum = 0` 会使 `GetFinal(KaLinCd)` 被截断为 **0**
- 使用 `GetKaLinCdValue()` 读取未截断值：`BaseValue × 百分比修正 + 固定加成`
- 日志同时输出 `kaLinCdRaw` 与 `kaLinCdFinal` 便于对比

**CD 计算公式：**

```
minRatio = |BaseValue|          // 如 0.5
cdRatio  = Max(1 + kaLinCdRaw, minRatio)   // 如 Max(0.5, 0.5) = 0.5
targetRemain = Lasttime0 × cdRatio          // 17 × 0.5 = 8.5
delta = targetRemain - RemainTime           // 负值，传给 ReduceSkillCooldown
```

---

### 2. `MonsterDamageService.cs`（修改）

**接入点：**

- `ApplySkillDamageTypes`：多段伤害循环结束后，若命中卡琳 Buff 且造成过伤害，调用一次 CD 修正（内层 `ReceiveSingleDamage` 设 `suppressKaLinCooldown: true`）
- `ReceiveSingleDamage`：单次伤害 `applied > 0` 且怪物有卡琳 Buff 时：
  - 调用 `TryApplyDuplicateDamage`
  - 若未 suppress，调用 `TryApplyCooldownReduction`

**条件：**

- `!isAgainDamage` — 追加伤害不再触发联动，避免循环
- `skillCard != null` — 必须为技能伤害

---

### 3. `MonsterDamageRequest.cs`（修改）

新增字段：

```csharp
public bool SuppressKaLinCooldown;
```

`Single(...)` 工厂方法增加参数 `suppressKaLinCooldown = false`，供多段伤害内层请求使用。

---

### 4. `GameLand.SkillCards.cs`（修改）

#### `CoolingSkillCard`

- **修复：** 原先 `card == null` 时直接 `return`，不写入 `skill` 字典，导致 UI 有倒计时但 `GameLand.skill` 无数据、卡琳修正读不到 CD
- **现行为：** 先写入 `skill` 字典（`RemainTime` / `Lasttime0` / `Cool`），找不到 UI 卡时仍保留数据并打警告日志

#### `TryGetSkillCooldownProgress`

- 增加 `Cool == true` 判断，与 `ReduceSkillCooldown` 条件一致

#### `ReduceSkillCooldown`

- 修正成功后调用 `NotifySkillCooldownAdjusted`，发布 `SKILL_COOLING_STARTED` 同步 UI
- 增加失败/成功日志与 `LogSkillCooldownSnapshot`

#### `LogSkillCooldownSnapshot`（新增）

- 输出：`Cool`、`RemainTime`、`Lasttime`、`Lasttime0`、`tweenActive`
- 日志前缀：`[GameLand][CD快照][原因]`

#### `NotifySkillCooldownAdjusted`（新增）

- 发布 `MessagePublisher.SKILL_COOLING_STARTED`，携带修正后的剩余 CD

---

### 5. `BattleSkillsPanelNew.cs`（修改）

**问题：** 面板独立 DOTween 倒计时与 `GameLand.skill` 脱节，CD 修正后仍显示原 17s。

**改动：**

- 增加 `_skillCoolingTweens`，修正/重开冷却前先 Kill 旧 tween
- `SKILL_COOLING_STARTED` 改为 `SyncSkillCoolingUIFromGameLand`：从 `GameLand.TryGetSkillCooldownProgress` 读 `remain/total`
- `UpdateSkillCoolingUI(cardId, remainTime, totalTime)`：`fillAmount = remain / total`
- `OnUnbindViewModel` 时清理全部 tween

---

## 数据流

```
技能命中怪物 (MonsterDamageService)
    │
    ├─ HasKaLinBuff? ──否──► 正常结束
    │
    └─是
        ├─ TryApplyDuplicateDamage (KaLinDamage > 0)
        │       └─ TakeDamage(IsAgainDamage=true)，相同 FinalDamage
        │
        └─ TryApplyCooldownReduction
                ├─ ResolveKaLinCdCooldownSkills
                ├─ GetKaLinCdValue (未截断)
                ├─ GameLand.ReduceSkillCooldown
                └─ SKILL_COOLING_STARTED → UI 同步
```

---

## 已知问题与规避

### 1. KaLinCd GetFinal 被截断为 0

- **现象：** CD 快照正常，但 `CD修正跳过: 无 KaLinCd`
- **原因：** `tbattributeconfig.json` 中属性 205 的 `ValueLimit.Minimum = 0`
- **代码规避：** `GetKaLinCdValue()` 不经过 ValueLimit 截断
- **配置可选修复：** 将属性 205 的 `Minimum` 改为 `-1` 或更小，使 `GetFinal` 全局正确

### 2. UI 与 GameLand CD 双轨

- **现象：** 界面显示 17s，但 `GameLand.skill` 无记录
- **规避：** `CoolingSkillCard` 先写字典再处理 UI

### 3. 命中技能 ID 与冷却技能 ID 不一致

- **现象：** 日志 `hitSkillId=2140101`，技能卡 `2140104` 在冷却
- **规避：** `ResolveKaLinCdCooldownSkills` 扫描英灵所有带 `KaLinCd` 且正在冷却的技能

---

## 调试日志

Console 过滤：`[KaLinBuff]`、`[GameLand][CD快照]`

| 日志 | 含义 |
|------|------|
| `CD修正入口 ... kaLinCdRaw=-0.500, kaLinCdFinal=0.000` | 截断问题可从此对比确认 |
| `[CD快照][KaLin-入口] Cool=True, RemainTime=16.x, Lasttime0=17` | GameLand CD 正常 |
| `CD修正计算 ... targetRemain=8.500` | 计算正确 |
| `ReduceSkillCooldown 成功: before=16.xx, after=8.50` | 数据层修正成功 |
| `未找到带 KaLinCd 且正在冷却的技能` | KaLinCd 为 0 或未进 CD |
| `skillId=xxx 不在 skill 字典` | CoolingSkillCard 未写入（旧版 bug） |

---

## 测试检查清单

### 功能

- [ ] 怪物挂卡琳 Buff（`2140104`，通常由技能 `2140103` 等施加）
- [ ] 使用带 `KaLinCd` 的技能卡（如 `2140104`）命中该怪物
- [ ] 剩余 CD 约为原始的 50%（17s → ~8.5s）
- [ ] 带 `KaLinDamage` 的技能造成两次相同税后伤害飘字
- [ ] 多段伤害（如 `2140104` Loop）仅触发 **一次** CD 修正

### 边界

- [ ] 怪物无卡琳 Buff 时不触发修正/追加伤害
- [ ] 技能无 `KaLinCd` / `KaLinDamage` 时不触发对应效果
- [ ] 追加伤害不再次触发卡琳联动（无死循环）
- [ ] 同一次冷却内多次命中，第二次 `remain <= target` 时不再重复缩短

### UI

- [ ] `BattleSkillsPanelNew` 修正后倒计时与遮罩同步（约 8～9s，fill ≈ 0.5）
- [ ] `SkillScroll` / `BattleSkillCardScrollItem` 显示与 GameLand 一致

### 日志

- [ ] 出现 `CD修正成功` 与 `[CD快照][KaLin-修正后] RemainTime≈8.5`

---

## 推荐测试流程

1. 上阵卡琳（`2040001`），确保学会 `2140103`（挂 Buff）与 `2140104`（含 KaLinCd）
2. 用 `2140103` 给怪物上卡琳标记
3. 释放 `2140104`，观察 Console 与技能卡 CD
4. 确认 `kaLinCdRaw=-0.5`、修正后 `RemainTime≈8.5`

---

## 后续可选优化

1. **配置表：** 调整属性 205 `ValueLimit.Minimum`，避免其他系统误用 `GetFinal(KaLinCd)`
2. **统一 CD UI 真值源：** 所有技能栏 UI 仅读 `GameLand.TryGetSkillCooldownProgress`，避免多套 tween
3. **KaLinDamage：** 若需按配置百分比追加（非 100% 等额），可扩展 `GetKaLinDamageValue` 逻辑

---

*文档生成对应当前仓库中卡琳 Buff 联动相关代码改动。*
