# PVE / PVP 双车道战场（Arena）

本文档说明 GamePlay 场景下 `pve` / `pvp` 子树切换、按侧挂点（Lane）、以及 `api/pvp/start` 对侧数据的初始化与使用。

## 场景结构

```
GameMap
  pve / pvp          # 背景等
GameLand
  pve / pvp          # wall、MonsterPos、FriendlySlot、Ordinary/Elite/Boss…
GameLandUI
  pve / pvp          # DamageParent、血条、BuySkillBtn/BuyExpBtn 等（SkillRect 仅玩家侧需要）
```

| 模式 | 显示 |
|------|------|
| PVE | 仅各根下的 `pve` |
| PVP AI | `pve`（我方）+ `pvp`（对侧） |

当前场景中 `pvp` 根节点多为横向偏移（如 `anchoredPosition.x ≈ 1153`），对侧兵会出现在**右侧副本**。

## 核心类型（`Scripts/Battle/Arena/`）

| 类型 | 职责 |
|------|------|
| `BattleMode` | `Pve` / `PvpAi` |
| `BattleSide` | `Player`（pve）/ `Opponent`（pvp） |
| `BattleSession` | 本局模式 + 对侧上下文 + `CurrentMatchId`；退局 `ResetToPve()` |
| `BattleArenaVisibility` | 统一切换三对 `pve`/`pvp` 显隐；隐藏对侧 HUD（SkillRect 等） |
| `LaneContext` | 单侧挂点：wall、MonsterPos、FriendlySlot、DamageParent、前进方向… |
| `OpponentBattleContext` | 由 `pvp/start` 解析出的对侧局内真值源 |
| `MonsterTransferOnDeathRule` | 击杀后对侧刷新一次（带 `HasTransferred`） |

**禁止**业务里到处 `Find("pvp")`；挂点走 `GameLand.GetLane(side)` 或已缓存的对侧站位。
**禁止** `GetLane(Opponent)` 回退到玩家侧（会把敌兵刷到我方坐标）。

## 进局流程（PVP）

```
CardGroupPanel StartPVPGameBtn
  → PvpService.StartPvpAsync(opponentId: 0)
  → POST api/pvp/start
  → BattleSession.BindPvpStart(resp)
       · CurrentMode = PvpAi
       · CurrentMatchId = resp.MatchId
       · Opponent = OpponentBattleContext.FromProfile(resp.Opponent)
  → GlobalFunc.StartBattle(stageId)
  → GamePlay 加载
  → SceneInitStartSpawning
       · GameLand.InitializeBattleArena()   // 显隐 + BuildLanes
       · GamePlayManager.InitOpponentCardTeams()  // 对侧初始英灵（同步刷）
  → OnInitComplete 再确保一次 InitOpponentCardTeams
```

PVE 进局会 `BattleSession.ResetToPve()`，只初始化玩家侧。

## pvp/start 数据用法

接口：`api/pvp/start`（`ApiKeys.ApiPvpStart`）
请求：`PvpStartRequest { OpponentId }`（默认 `0`，临时测试按匹配规则返回对手）
响应：`PvpStartResponse { MatchId, Opponent }`，其中 `Opponent` 为 `PlayerProfile`

| 字段 | 用途 |
|------|------|
| `MatchId` | 本局 match_id（结算上报时使用），存入 `BattleSession.CurrentMatchId` |
| `Opponent.BaseInfo` | 对手基础信息（如 `PlayerName`） |
| `Opponent.Teams` | 出战编队；优先 `CurrentUse==1` |
| `Opponent.Teams.Cards` | JSON `int[]` 卡组（士兵/技能等） |
| `Opponent.Teams.CardsPosition` | JSON 站位映射 |
| `Opponent.Teams.HeroTeam` | Cards 无士兵时的回退 |
| `Opponent.Cards` | 卡牌实例（等级/皮肤）；再回退可扫士兵卡 |
| `Opponent.Equips` / `Opponent.ArcResonances` | 佩戴装备 / 奥数共鸣（已解析进 `BattleSession.Opponent`） |
| `Opponent.FusionGems` | 佩戴在装备上的融合宝石（`WearerId>0` 才生效） |
| `Opponent.Talents` | 天赋列表（`Status==1` 使用中的天赋生效） |
| `Opponent.CityWall.Wall.Level` | 城墙等级（`TbHeroSeatLevelConfig` 基础属性加成） |
| `Opponent.Weapons` / `Opponent.SpeakerModule.EquippedWeaponId` | 武器列表 / 当前穿戴武器（星级 Base + Extra 属性加成） |
| `Opponent.Skins` / `Opponent.SpeakerModule.UseSkinId` | 皮肤列表 / 当前穿戴皮肤（星级 Base + Extra 属性加成） |
| `Opponent.CityWall.Skills` / `CityWall.SlotSkills` | 城墙技能（SkillId→等级）/ 槽位技能顺序（已解锁且激活的技能生效） |

局内访问入口：

```csharp
BattleSession.IsPvpAi
BattleSession.Opponent              // OpponentBattleContext
BattleSession.Opponent.GetCardById(cardId)
BattleSession.Opponent.SoldierTeams
BattleSession.Opponent.Raw          // 原始 proto（Cardwar.PlayerProfile）
```

`SoldierCard.Init`：玩家读 `Global.GetCardById`；对侧读 `BattleSession.Opponent.GetCardById`（皮肤等）。

## 刷兵 / 挂点

### 玩家侧
- `SceneInitPositions` → `friendlyPositions`（`GameLand.FriendlySlot`，绑定 pve）
- `InitCardTeams` → `AddSoldierCard(..., BattleSide.Player)` → `villageGameCards`

### 对侧（PVP）
- `InitOpponentFriendlyPositions`：固定解析 `GameLand/pvp/Canvas/FriendlySlot`
- `AddSoldierCard(..., BattleSide.Opponent)`：父节点为对侧 `PosN`，`localPosition=0`
- 列表：`opponentGameCards`
- **同步生成**（加载期 `timeScale=0` 时不要用 `ActionKit.Delay`）

### 怪物
- `AddMonsterCard(..., BattleSide side)` → `GameLand.GetLane(side)` 取父节点 / MonsterPos
- 移动方向：`LaneContext.MoveDirectionY` 按墙/出生点 Y 布局自动推导（当前横向镜像布局两侧均为 -1，朝各自墙移动）
- 索敌：`MonsterCard.EngageSoldierCard` 按 `BattleSide` 选 `villageGameCards` / `opponentGameCards`（对侧怪锁定对侧英灵）
- PVP 波次：`GamePlayManager.SpawnEnemies` 在 PVP 下把每一波怪同步镜像刷到对侧（`BattleSide.Opponent`），双方城市同时被怪攻打
- 击杀转移：`MonsterTransferOnDeathRule.TryTransfer` 在怪物死亡时把同一只怪刷新到另一侧（一次，`HasTransferred` 防重复）

## UI 裁剪原则

| 双份（跟侧走） | 仅玩家 pve |
|----------------|------------|
| DamageParent、MonsterHealth、MonsterIconEffects | SkillRect、Schools、CitySkillCards、主 HUD / BattlePanel |

`BattleArenaVisibility.HideOpponentHudOnlyControls` 会强制隐藏对侧 SkillRect 等；场景里 SkillRect 通常只在 `GameLandUI/pve`。

## 关键文件

```
Scripts/Battle/Arena/
  BattleMode.cs
  BattleSide.cs
  BattleSession.cs
  BattleArenaVisibility.cs
  LaneContext.cs
  OpponentBattleContext.cs
  MonsterTransferOnDeathRule.cs

Scripts/Game/Pvp/Services/PvpService.cs
Scripts/Game/GameLand.cs                    # InitializeBattleArena / GetLane
Scripts/Game/GamePlayManager.cs             # InitOpponentCardTeams / TickOpponentAi / AddSoldierCard(side)
Scripts/Scenes/LoadingModules/Display/SceneInitStartSpawning.cs
Scripts/Game/CardGroup/ViewModels/CardGroupPanelViewModel.cs  # StartPVP
Scripts/UI/CardGroup/CardGroupPanelNew.cs
```

## 联调检查清单

- [ ] PVE：三处 `pvp` 关闭，单侧玩法正常
- [ ] PVP：`BattleSession.IsPvpAi`，右侧 `pvp` 可见
- [ ] 日志含 `对侧站位就绪 ... GameLand/pvp/...` 与 `对侧初始上场完成 spawned≥1`
- [ ] 对侧英灵不出现在我方 FriendlySlot
- [ ] 对侧无 SkillRect / 不可误点技能栏
- [ ] 对侧 AI 不会开局瞬间买满人物（按经验/等级逐步上场，日志含 `PVP 对侧AI购买经验` / `PVP 对侧AI升级`）
- [ ] 对侧英灵会自己放主动技能（日志含 `PVP 对侧AI释放技能`）
- [ ] 对侧英灵学到编队技能卡（日志含 `对侧出战技能卡: count=N` 与 `PVP 对侧英灵学习技能`）
- [ ] 对侧怪被 AI 击杀时金币不再飞向玩家金币条
- [ ] 屏幕左上角显示 AI 金币 / 等级 / 经验等调试信息（OnGUI）
- [ ] 对侧买三选一时玩家侧有 Toast 与对侧飘字提示（`AI购买三选一`）
- [ ] 退局后 `BattleSession.ResetToPve()`，下次 PVE 不受影响
- [ ] PVP 胜/负/中途退出均走 `api/pvp/settle`（日志含 `[PvpService] SettlePvp ok`），不打 `api/battle/settle`
- [ ] 结算弹窗显示奖励；无「双倍奖励」「再来一次」；返回主界面后可再开 PVP

## 胜负规则（PVP）

- 一方英灵全灭立即判定：对侧（对手）英灵全灭 → 我方胜利；我方英灵全灭 → 战斗失败
- 战斗满 5 分钟按双方剩余英灵血量总和判定：血量高者胜；打平视为我方胜
- PVP 下跳过 PVE 清场结算（`TryHandleDayClearedAfterEnemyDefeated` 直接 return）
- 实现：`GamePlayManager.TryCheckPvpEndByTeam` / `TrySettlePvpByRemainingHp`（`_pvpBattleTime` 满 300s 触发）

## 结算上报（PVP）

```
GameSuccess / GameFailure
  → BattleModel.Settle
  → BattleSession.IsPvpAi ?
       是 → SettlePvp → PvpService.SettlePvpAsync(isWin)
            POST api/pvp/settle  { match_id, is_win }
            ← items / rewards / season_info
            同步 Items、SeasonInfo，PlayerBattle.Status=0
            打开 GameSettlementPanel1（IsPvpMode：无双倍/无再来一次）
       否 → 原 PVE api/battle/settle
```

| 字段 | 来源 |
|------|------|
| `match_id` | `BattleSession.CurrentMatchId`（`pvp/start`） |
| `is_win` | `winner == Global.User.UserId` |

结算成功后调用 `BattleSession.ClearMatchIdAfterSettle()`，避免返回/退局重复上报。

**注意**：PVP 结算禁止再走 `/api/battle/settle`；中途退局 `BackHome` 在仍有 `match_id` 或 `PlayerBattle.Status!=0` 时会进入上述分支并按失败上报。

## 对侧 AI（全自动，与玩家一致的经济）

- **初始金币与玩家一致**：`TbGameBattleConst.InitialCapitalReserves`；若对侧携带贪婪预支奥数卡再 + `SpecialArcCardGoldNumOne`（装备 / 奥数卡 / 编队属性均生效）
- **购买人物 = 玩家购买流程**：花 `CityLevelConfig.NeedGold` 金币买经验 → 经验 + `CityExp` → 攒够升级 → 仅 `IsAddHero==1` 的等级（如 Lv3/5/7）才上场 1 名编队英灵，费用随等级递增
- **击杀收益**：与玩家一致 —— 击杀怪物按掉落 `CombatCurrency × DorpGoldBaseNumber` 结算（含对侧自己的 `KillAnEnemyExtraBattleGoldCoin` / Boss `KillAnBossGoldCoin` 属性加成）；无被动收入（玩家也没有）
- **养成属性（装备/奥数/天赋/城墙/武器/皮肤/城墙槽位技能）**：开局解析 `PlayerProfile` 的 Equips / ArcResonances / FusionGems / Talents / CityWall / Weapons / Skins / SpeakerModule，按玩家侧同一套规则聚合后写入对侧英灵基础值与主属性（`OpponentBattleContext.BuildBattleAttributes` → `ApplyOpponentBattleAttributesToSoldier`），新增来源自动计入 AI 金币加成等数值属性：
  - 装备：词条 + 临时词条 + 特效 + 临时特效（跳过 Module 类规则属性）
  - 奥数卡：出战编队 GeneralSkill 的等级属性 + 主属性（跳 LearnConditionBuff / LearnSkillRule）
  - 奥数共鸣：`ResonanceList` 当前等级 + 阶段属性
  - 融合宝石：佩戴在装备上（`WearerId>0`）的宝石主属性 + 数值属性
  - 天赋：`Status==1` 使用中的天赋按 `Lv` / `ExtraLv` 叠加基础 + 额外属性
  - 城墙：当前城墙等级 `HeroSeatLevelConfig.BaseAttributes`
  - 武器：当前穿戴武器（`SpeakerModule.EquippedWeaponId`）对应星级的 Base + Extra 属性
  - 皮肤：当前穿戴皮肤（`SpeakerModule.UseSkinId`）对应星级的 Base + Extra 属性
  - 城墙槽位技能：已解锁槽位（按 `TbHeroSeatLevelConfig.UnLockSeat` 配置）且已激活技能的 Base + Extra 属性
- **三选一**：人物全部上场后按 `SkillInitBuyChooseCardGoldNum × 次数` 递增扣费（与玩家一致），随机从 3 张候选卡选 1 并应用；对侧无购买 UI，但玩家侧可看到提示
  - Soldier 类属性写英灵基础属性；Skill 类属性（如法术洪流全技能伤害、奥术魔方冷却）写入已学技能 Attributes（`ApplyOpponentAiSkillAttr`）
  - 场地/全场技能卡（风舞疾袭、雷霆领域、能量迸发等 LearnSkill*）购买时学到对侧英灵，并被 AI 自动释放（`LearnOpponentAiSkillLogic`）
  - 专属被动（散射箭矢/猛击/万剑归宗等 ExclusiveLearningEffects）按 `IsAddHero` 应用到对应英灵的被动技能修正器（`ApplyOpponentAiExclusiveLearningEffect`）
  - 金币收藏家、潘多拉的魔盒对侧同样生效；LearnSkillRule 规则卡暂不对对侧生效（规则引擎为玩家侧全局，注册会同时作用到玩家）
- **自动放技能**：对侧英灵每 0.5s 检查一轮，冷却完成且有索敌目标时自动释放主动技能（跳过普攻 / 被动技能）
- **技能卡挂载**：对侧编队技能卡（Skill/SkillPassive）解析进 `OpponentBattleContext.SkillTeams`，英灵上场时按 `ActsOnCard` 学到身上（`InitializeOpponentNewSoldierSkills`），否则无主动技能可放
- **对侧怪掉落**：对侧怪被 AI 击杀时金币归 AI 侧，`MonsterCard.Die` 不再向玩家飞金币 / 不再触发玩家掉落（CreateCoin、Boss 金币、飞向 GoldBar 特效均跳过）
- **调试显示**：PVP 下 `GamePlayManager.OnGUI` 实时显示 AI 金币 / 等级 / 经验 / 英灵 / 技能数（测试阶段用，后续删除）
- **滑动镜头**：PVP 下拖拽空白处滑动屏幕，可在玩家侧 / AI 侧之间查看（`GamePlayManager.UpdatePvpCameraDrag` 只移动主相机，场景物体（英灵/怪物/子弹/特效）不动，避免拖拽时特效乱飞；滑动上限按 `GameMap` 的 pve→pvp 实际偏移计算，像素按正交相机视野换算为世界位移，拖动跟随手指：下滑看向 AI 侧）
- 实现：`GamePlayManager.TickOpponentAi` / `GetOpponentAiHeroBuyCost` / `TickOpponentAiSkillRelease` / `TryOpponentAiBuySchoolCard` / `ShowOpponentAiPurchaseHint`

## 已知后续项

- 对侧技能栏（当前刻意不做）
- 上下对阵为玩家在下 / AI 在上；镜头默认停在玩家侧，可拖拽滑动屏幕查看 AI 侧（下滑看向对面）



