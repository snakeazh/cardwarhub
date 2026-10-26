# FusionContinent AI 上下文索引

> 集中索引各模块 AI 上下文文档，供 Cursor / Claude 接续任务时快速定位。  
> 最后更新：2026-07-24  

---

## 使用说明

1. 接到任务后先查本索引，打开对应模块的 `*_Context.md` 或 `README_*.md`
2. 上下文文档记录：架构、文件清单、已知问题、约束规范、排查清单
3. 详细策划/公式以各模块 README 为准；Context 文件侧重 **AI 接续开发**

---

## 索引表

| 模块 | 上下文文件 | 完整文档 | 关键路径 |
|------|-----------|----------|----------|
| 属性系统 | [AttributeSystem_Context.md](Game/Attribute/AttributeSystem_Context.md) | [README_MainAttribute.md](Game/Attribute/README_MainAttribute.md) | `Game/Attribute/` |
| 伤害系统 | — | [DamageSystem_README.md](Game/DamageCalculator/DamageSystem_README.md) | `Game/DamageCalculator/` |
| 局内战斗特效 | [EffectManager_Context.md](Game/EffectManager_Context.md) | — | 全局 `EffectManager`（非 `Game.Battle`） |
| 战斗规则 | [BattleRuleService_Context.md](Battle/Rules/BattleRuleService_Context.md) | [README_BattleRuleSystem.md](Battle/Rules/README_BattleRuleSystem.md) | `Battle/Rules/` |
| 祈祷被动 RoundStart | [BattleRule_RoundStart_2150102_Context.md](Battle/Rules/BattleRule_RoundStart_2150102_Context.md) | 同上 README §Trigger | `GamePlayManager.cs` |
| 装备 LearnSkillRule | [BattleRule_EquipLearnSkillRule_Context.md](Battle/Rules/BattleRule_EquipLearnSkillRule_Context.md) | 同上 README §装备 | `Model/PlayerEquipModel.cs` |
| 链式学习来源参数 | [BattleRule_SkillSourceBuff_Context.md](Battle/Rules/BattleRule_SkillSourceBuff_Context.md) | 同上 README §策划配置 | `Battle/Rules/BattleRuleSourceParamSupport.cs` |
| 流派卡英雄专属 | [BattleRule_HeroScopedSchoolCard_Context.md](Battle/Rules/BattleRule_HeroScopedSchoolCard_Context.md) | 同上 README §IsAddHero | `Manager/SchoolManager.cs` |
| 战斗 HeroDetails | [HeroDetails_Context.md](UI/HeroDetails_Context.md) | — | `UI/HeroDetails.cs` |
| 英灵技能重构 | [SoldierCard_Refactor_Context.md](Game/Soldier/SoldierCard_Refactor_Context.md) | — | `Game/Soldier/` |
| 怪物职责拆分 | [MonsterCard_Refactor_Context.md](Game/Monster/MonsterCard_Refactor_Context.md) | — | `Game/Monster/`（排序 / Spine 动画） |
| 怪物攻击行为 | [MonsterAttackBehaviour_Context.md](Game/Monster/MonsterAttackBehaviour_Context.md) | — | `Game/Monster/AttackBehaviour/` |
| 精英/Boss 场景血条 | [MonsterHealthBar_Context.md](Game/Monster/MonsterHealthBar_Context.md) | [Migration_Diff_Report_MonsterHealthBar.md](../../Migration_Diff_Report_MonsterHealthBar.md) | `GameLandUI/MonsterHealth` |
| 怪物状态图标外移 | [MonsterIconEffects_Context.md](Game/Monster/MonsterIconEffects_Context.md) | [Migration_Diff_Report_MonsterIconEffects.md](../../Migration_Diff_Report_MonsterIconEffects.md) | `GameLandUI/MonsterIconEffects` |
| UI 框架迁移 | — | [AGENTS.md](../../AGENTS.md)（项目根） | `Scripts/UI/` |
| 项目总览 | — | [CLAUDE.md](../../CLAUDE.md)（项目根） | — |

---

## 近期问题速查

### SkillFireDamage 未配置异常

- **现象：** `Init AttributeValue Not Configured Type SkillFireDamage`
- **栈顶：** `MonsterCard.TakeDamage` → `AttributeSet.GetFinal(SkillFireDamage)`
- **文档：** [AttributeSystem_Context.md §3](Game/Attribute/AttributeSystem_Context.md)
- **修复：** `tbattributeconfig.json` 补 AttributeType 194（灼烧增伤）

### 属性枚举新增检查

新增 `AttributeType` 后必须确认：
- [ ] `tbattributeconfig.json` 有条目
- [ ] 引用该 Type 的特效/规则配置已对齐
- [ ] 无与旧枚举语义重复（如 167 vs 194）

### 伤害前置 vs 管线

怪物受击前置修正（SkillDamage / 灼烧）已迁入 `MonsterDamageService` / `DamageContextBuilder`，详见 [DamageSystem_README.md §10](Game/DamageCalculator/DamageSystem_README.md)。

### 怪物攻击分发

- **现象：** 新增怪物攻击无效或 Init 抛 `AttackBehaviourType` 异常  
- **文档：** [MonsterAttackBehaviour_Context.md §4–§6](Game/Monster/MonsterAttackBehaviour_Context.md)  
- **修复：** JSON/Excel 补 `AttackBehaviourType` + Factory Register 对应 Behaviour  

### 装备词条 / 月影祝福 / 攻强 UI

- **现象：** 装备 LearnSkillRule 不叠加、月影祝福无效、HeroDetails 攻强仍为装备前  
- **文档：** [BattleRule_EquipLearnSkillRule_Context.md](Battle/Rules/BattleRule_EquipLearnSkillRule_Context.md)、[HeroDetails_Context.md](UI/HeroDetails_Context.md)  
- **排查：** 对比 `[Attack]` 与 `[HeroDetails]` 的 `SoldierAttackPower final`；查 `GloballyEffective` 配表；确认技能初始化后 `ApplyEquipLearnedRulesToSoldier`  

### 流派卡英雄专属规则不生效

- **现象：** 三选一选了 `IsAddHero` 卡，击杀不加属性；或其他英雄击杀也触发  
- **文档：** [BattleRule_HeroScopedSchoolCard_Context.md](Battle/Rules/BattleRule_HeroScopedSchoolCard_Context.md)  
- **排查：** 日志是否有 `ScopeHero=cardId`；`ConditionType` 是否为 0；是否误用 Type=122→被动；`Trigger`/`ComputerGroup` 是否与击杀时机匹配  

### 祈祷被动（2150102）规则已学但不生效

- **现象：** 日志有 `RuleId=113002`，但波开始无 Buff / `triggered=0`  
- **文档：** [BattleRule_RoundStart_2150102_Context.md](Battle/Rules/BattleRule_RoundStart_2150102_Context.md)  
- **排查：** `300007.MaxValue` 是否为 1；是否有 `RoundStart 规则跳过 ... Computer 未达标`；首日是否走到 `OnInitComplete` 触发；换波是否 `isDayInProgress=true`  

---

## 项目规范摘要（AI 必遵）

来源：[AGENTS.md](../../AGENTS.md)

| 类别 | 规则 |
|------|------|
| 日志 | 只用 `LogTool`，禁止 `Debug.Log` |
| 空引用 | 非必要不判空；禁止 `?.` |
| 事件绑定 | 具名方法 + 成对 RemoveListener |
| 数据真值 | ViewModel 单一真值源，View 不缓存副本 |
| 新 UI 按钮 | 用 `UIPointerClickAndLongPress`，不用 `Button` |
| 局内战斗特效 | 统一全局 `EffectManager`，非 `Game.Battle.BattleRuleEffectManager` |
| 配置错误 | fail fast，不写静默 fallback |
| UI 迁移 | 8 步流程，见 AGENTS.md |

---

## 配置路径速查

| 用途 | 路径 |
|------|------|
| 属性元数据 | `Assets/Res/Config/Json/tbattributeconfig.json` |
| 技能规则 | `Assets/Res/Config/Json/tbskillruleconfig.json` |
| 条件组 | `Assets/Res/Config/Json/tbconditiongroupconfig.json` |
| 效果组 | `Assets/Res/Config/Json/tbeffectgroupconfig.json` |
| 技能特效 | `Assets/Res/Config/Json/tbspecialeffectsconfig.json` |
| 怪物战斗表 | `Assets/Res/Config/Json/tbmonsterbattleconfig.json`（含 `AttackBehaviourType`） |
| Luban 生成代码 | `Assets/Scripts/Data/Client/Luban/`（含 `AttackBehaviourType.cs`） |

---

## 维护约定

- 完成较大重构或修复典型问题时，更新对应 `*_Context.md` 的「已知问题」和「文件清单」
- 新增模块上下文时在本索引表追加一行
- Context 文件命名：`{Module}_Context.md` 或 `{Feature}_Context.md`
