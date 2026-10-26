# PVP pvp/start 接口与对侧上下文

## 接口

| 项 | 值 |
|----|-----|
| Path | `api/pvp/start` |
| 常量 | `ApiKeys.ApiPvpStart` |
| 请求 | `Cardwar.PvpStartRequest` |
| 响应 | `Cardwar.PvpStartResponse` |
| 默认参数 | `player_id = 0`（服务端按匹配/测试规则返回对手） |

## 客户端入口

```csharp
var ctx = await GameServiceManager.GetOrCreate<Game.Pvp.PvpService>()
    .StartPvpAsync(opponentId: 0);

// 内部：
// 1. SendProtoPostRequest → PvpStartResponse
// 2. BattleSession.BindOpponentFromInfo(resp)
// 3. 返回 BattleSession.Opponent（OpponentBattleContext）
```

编队界面：`CardGroupPanel` 的 `StartPVPGameBtn` → `CardGroupPanelViewModel.OnStartPvpGame`。

## OpponentBattleContext 职责

**唯一对侧真值源**（不要再散落解析 proto）：

1. **初始化**：`FromProfile` / `Initialize`
   - 映射 `Cards` → 本地 `PlayerCard`（等级、皮肤）
   - 解析出战 `Teams` → `SoldierTeams` + `CardsPosition`
   - 回退：`HeroTeam` → `Cards` 中士兵卡
2. **使用**
   - 上场：`SoldierTeams` / `ToTeamBuildResult()`
   - 查卡：`GetCardById(cardId)`（`SoldierCard` 对侧皮肤等）
   - 扩展：`Raw`（Equips、ArcResonances、FusionGems、Talents、CityWall、Weapons、SpeakerModule…）

## 养成属性（BuildBattleAttributes）

对侧英灵基础值/主属性按玩家侧同一套规则聚合（`OpponentBattleContext.BuildBattleAttributes()`，懒加载），来源：

| 来源 | 数据字段 | 生效规则 |
|------|----------|----------|
| 装备 | `Equips` | 词条 + 临时词条 + 特效 + 临时特效（跳过 Module 类规则属性） |
| 奥数卡 | `Teams.Cards` 中 GeneralSkill | 等级属性 + 主属性（跳 LearnConditionBuff / LearnSkillRule） |
| 奥数共鸣 | `ArcResonances` | `ResonanceList` 当前等级 + 阶段属性 |
| 融合宝石 | `FusionGems` | 佩戴在装备上（`WearerId>0`）的宝石主属性 + 数值属性 |
| 天赋 | `Talents` | `Status==1` 使用中的天赋按 `Lv` / `ExtraLv` 叠加基础 + 额外属性 |
| 城墙 | `CityWall.Wall.Level` | `TbHeroSeatLevelConfig` 当前等级 `BaseAttributes` |
| 武器 | `Weapons` + `SpeakerModule.EquippedWeaponId` | 当前穿戴武器对应星级 Base + Extra 属性 |
| 皮肤 | `Skins` + `SpeakerModule.UseSkinId` | 当前穿戴皮肤对应星级 Base + Extra 属性 |
| 城墙槽位技能 | `CityWall.Skills` / `CityWall.SlotSkills` | 已解锁槽位且已激活技能按当前等级 Base + Extra 属性 |

局内由 `GamePlayManager.ApplyOpponentBattleAttributesToSoldier` 应用到对侧英灵；数值类属性（如 `KillAnEnemyExtraBattleGoldCoin`）自动计入 AI 击杀收益。

## BattleSession 约定

```csharp
BattleSession.BindPvpStart(resp); // 设 PvpAi + Opponent
BattleSession.IsPvpAi;
BattleSession.Opponent;
BattleSession.ResetToPve();                 // 清对侧；退局 / 进 PVE 时调用
```

## 士兵构建优先级

1. 出战队 `Teams[].Cards`（JSON int 列表）中 `CardType.Soldier`
2. 同队 `HeroTeam` JSON 的 `HeroCardId`
3. 响应 `Cards` 列表中全部士兵配置卡

## 相关代码

- `Scripts/Game/Pvp/Services/PvpService.cs`
- `Scripts/Battle/Arena/OpponentBattleContext.cs`
- `Scripts/Battle/Arena/BattleSession.cs`
- `Scripts/ApiKeys.cs` → `ApiPvpStart`
- Proto：`Scripts/Generated/Proto/Pvp.cs`

更完整的战场显隐 / 车道说明见 [README_PvePvpArena.md](./README_PvePvpArena.md)。
