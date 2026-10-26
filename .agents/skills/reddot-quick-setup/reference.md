# 红点快速接入 — 参考

## 关键文件路径

| 用途 | 路径 |
|------|------|
| 枚举 | `FusionContinent/Assets/Scripts/Game/RedDot/RedNodeType.cs` |
| **全局树结构（唯一入口）** | `FusionContinent/Assets/Scripts/Game/RedDot/Business/GlobalRedDotTreeSetup.cs` |
| Rule 目录 | `FusionContinent/Assets/Scripts/Game/RedDot/Business/Rules/` |
| Rule 注册 | `FusionContinent/Assets/Scripts/Game/RedDot/Business/RedDotRuleBootstrap.cs` |
| Rule 基类 | `.../Rules/RedDotRuleBase.cs` |
| 登录初始化 | `FusionContinent/Assets/Scripts/Game/RedDot/Business/RedDotBusinessSetup.cs` |
| UI 绑定 | `RedDotBinder.SetRedType` / `RedDotExtensions.BindRedDot` |
| 局部 Scope 示例 | `FusionContinent/Assets/Scripts/Game/Soldier/RedDot/SoldierPanelRedDotScope.cs` |
| 文档 | `FusionContinent/Assets/Scripts/Game/RedDot/RedDot.md` |
| 工程 | `FusionContinent/HotFix.csproj`（新 Rule 文件需 Compile Include） |

> **已废弃（勿再引用）：** `GlobalRedDotTreeConfig`、`GlobalRedDotTreeConfigProvider`、`GlobalRedDotTreeConfig.asset`、Global Tree Editor 窗口。全局树层级**只在** `GlobalRedDotTreeSetup.Build` 中硬编码维护。

## RedNodeType 号码段

| 段 | Tab | 导航根 |
|----|-----|--------|
| 0–100 | 主界面 | `NavBarBattle`(0) 主界面 Tab；0–5 导航根，11–30 子功能 |
| 100–200 | 英灵 | `NavBarSoldier`(5) 未接入树；103/104 实际挂 `NavBarGeneralSkill` |
| 200–300 | 商城 | `NavBarShop`(2) |
| 300–400 | 世界 | `NavBarBigWord`(3) |
| 400–500 | 奥术卡 | `NavBarGeneralSkill`(1) |
| 500–600 | 世界扩展 | 501/502 挂 `NavBarBigWord` |

新增枚举：在对应段内取下一个空闲整数；写 `/// <summary>` 中文注释。

## GlobalRedDotTreeSetup 默认父级

| 父节点 | 典型子节点 |
|--------|------------|
| `NavBarBattle` | MainPageAFKBtn, SignIn, MonthCard, Task, Activity… |
| `MainPageActivityBtn` | DayGiftBtn, DayPayGiftBtn, PayGoodGiftBtn |
| `NavBarShop` | ChestBtn |
| `NavBarBigWord` | MonsterAtlasBtn, TalentUpgradeBtn, EquipUpGradeBtn |
| `NavBarSpeaker` | SpeakerEquipUpGradeBtn |
| `NavBarGeneralSkill` | SoliderSkillUpGrade, GeneralSkillUpGrade, SoliderCanUpgrade |
| 独立根 (Any) | MainPageFirstBtn, FirstChargePage*, SoliderUpgradeCard/DevelopCard |

## GlobalRedDotTreeSetup 追加节点

在 `GlobalRedDotTreeSetup.Build` 内追加：

```csharp
// 导航根（Any，无父）
Add(RedNodeType.{NavRoot}, any);

// 叶子 / 中间层（Sum，挂父节点）
Add(RedNodeType.{NodeType}, sum, true, RedNodeType.{Parent});
```

## Rule 模板 A：事件驱动（最常用）

```csharp
using Config;
using Game.RedDot;
using FusionContinent;
using FusionContinent.Model;

namespace Game.RedDot.Business.Rules
{
    public sealed class {Name}RedDotRule : RedDotRuleBase
    {
        public override RedNodeType NodeType => RedNodeType.{NodeType};

        protected override void OnBind()
        {
            Global.Publisher.Subscribe(
                MessagePublisher.CHANGE_RED_TYPE + RedNodeType.{NodeType}, OnDirty);
            // 按需追加 MessagePublisher.xxx
        }

        protected override void OnUnbind()
        {
            Global.Publisher.Unsubscribe(
                MessagePublisher.CHANGE_RED_TYPE + RedNodeType.{NodeType}, OnDirty);
        }

        void OnDirty(object _) => Context.MarkDirty();

        public override int Evaluate()
        {
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.Xxx, false))
                return 0;

            // TODO: 业务条件
            return condition ? 1 : 0;
        }
    }
}
```

## Bootstrap 注册

```csharp
registry.RegisterGlobal(RedNodeType.{NodeType}, () => new {Name}RedDotRule());
// 主界面 + 子页同步：
registry.RegisterGlobal(RedNodeType.MainPageXxx, () => new XxxRedDotRule());
registry.RegisterGlobal(RedNodeType.XxxPageBtn, () => new XxxRedDotRule());
```

## UI 绑定

```csharp
using Game.RedDot;

// 方式 A
myButton.SetRedType(RedNodeType.{NodeType});
// 方式 B
myButton.BindRedDot(RedNodeType.{NodeType});
```

## 业务刷新

```csharp
using Game.RedDot;

// 推荐：帧末合并
RedDotService.Instance.MarkDirty(RedNodeType.{NodeType});

// 或沿用项目习惯
Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.{NodeType}, null);
```

## 用户需求描述模板（可转发给策划/程序）

```
【红点快速接入】
功能：
显示位置：
RedNodeType：新建 xxx / 复用 xxx
父节点：
亮灭条件：
数据变化时：
UI 绑定文件/按钮：
备注：
```

## 示例需求 → 动作

**邮件未读**：搜 MailModel → 新建 `MainPageMailBtn`(11–30) → 父 `NavBarBattle` → Rule 查未读 → MainInterface 绑 MailBtn。

**某活动可领**：搜 ActivityType / UserCreatePlayerActivity → 参考 `GiftPackRedDotRule` → 父 `MainPageActivityBtn` 或 `NavBarBattle` → 订阅 `CHANGE_ACTIVITY_INFO`。

**轮语者装备锻造**：参考 `SpeakerEquipForgeRedDotRule` → 父 `NavBarSpeaker` → `SpeakWhellPanelNew` 绑 `EquipUpGradeBtn`。

**面板内当前卡可升级**：局部 Scope → `RegisterNodeType` + `SoldierPanelLocalRedDotRule` 模式 → `BindLocalRedDot`。
