# 迁移差异报告: SchoolChooseCardPanel

## 迁移前确认单

| 项目 | 值 |
|------|-----|
| Path | SchoolChooseCard |
| Module | Game.SchoolChooseCard |
| PrefabPath | ChooseCard/SchoolChooseCardPanel |
| UILevel | UIFramework.UILevel.PopUI |
| 风险点 | 多层卡牌动画、倒计时逻辑、静态Default引用 |

---

## 1. 已移除的逻辑

### 1.1 QFramework 特定代码
- 移除 `OnInit`, `OnOpen`, `OnShow`, `OnHide`, `OnClose` 生命周期覆盖
- 移除 `UIKit.OpenPanel` / `UIKit.HidePanel` 调用
- 移除 `mData = uiData as SchoolChooseCardPanelData` 数据模式
- 移除 `Default = this` 静态单例模式

### 1.2 直接调用改为事件/Command
- 原：外部直接调用 `UIKit.OpenPanel<SchoolChooseCardPanel>().OpenSingleChoose(count)`
- 新：通过 `UIManager.OpenPanel<SchoolChooseCardPanelNew, SchoolChooseCardViewModel>(vm).Open(data)` 打开
- 原：卡牌按钮事件在 `OnInit` 中直接绑定
- 新：按钮事件通过 `UnityAction` 引用保存，在 `OnBindViewModel` / `OnUnbindViewModel` 中管理

### 1.3 依赖关系变化
- 原：SchoolChooseCardPanel 内部持有所有业务逻辑（卡池、随机、刷新）
- 新：业务逻辑拆分到 `SchoolChooseCardService`，UI 状态在 `SchoolChooseCardViewModel`，UI 更新在 `SchoolChooseCardPanelNew`

---

## 2. 需要 TODO 的事项

### 功能验证点
- [ ] **倒计时自动选择**：倒计时结束自动选择第一张卡牌，验证是否正常工作
- [ ] **刷新卡牌**：点击刷新按钮，验证金币消耗和卡牌替换
- [ ] **全部获得**：点击"全部获得"按钮，验证逐层选择流程
- [ ] **多层卡牌**：验证 `OpenCount > 1` 时多层卡牌切换动画
- [ ] **卡牌飞入特效**：选择流派卡后验证飞入 GameLandUI 的特效
- [ ] **品质概率显示**：验证每层刷新后概率列表是否正确更新
- [ ] **星级动画**：验证星级闪烁动画是否正常播放和清理

### 兼容性检查
- [ ] 从 GamePlayManager 打开（测试快捷键 C）
- [ ] 从 GameLand 升级时打开（城市升级触发）
- [ ] 从 BattleInterViewModel 购买技能/经验时打开
- [ ] 面板关闭后游戏是否正常恢复（`GamePlayManager.StartGame`）
- [ ] 引导系统集成（`TriggerGuide(GuideType.FirstChooseCard)`）

### 潜在边界情况
- [ ] 卡池为空时的处理（`RandomSchoolChooseCards` 返回空列表）
- [ ] 品质权重列表为空时标题显示
- [ ] DOTween 动画未完成时关闭面板（已通过 `CloseChooseCard` 清理）
- [ ] 倒计时极短时的自动选择

---

## 3. 架构变化说明

### 职责分离
```
SchoolChooseCardPanel (旧)
├── UI 组件持有
├── 业务逻辑（随机、刷新、选择）
├── 动画管理
└── 数据管理（卡池）

→ 拆分后：

SchoolChooseCardService (Game.Services)
├── 卡池管理（RefreshQualityPoolSchoolCards）
├── 随机卡牌（RandomSchoolChooseCards）
├── 卡牌选择逻辑（ChooseCard）
└── 刷新逻辑（RefreshSingleCard）

SchoolChooseCardViewModel (Game.SchoolChooseCard)
├── UI 状态（TitleText, GoldCount, CardList）
├── 命令（OnGetAllCommand, OnChooseCardCommand）
├── 倒计时管理
└── 数据转换

SchoolChooseCardPanelNew (FusionContinent)
├── UI 绑定（OnBindViewModel）
├── UI 刷新（InitCardUI, RefreshCardUI）
├── 动画播放（SetCardUIEffect）
└── 特效管理（EffectCard）
```

### 数据流向
```
外部调用 → ViewModel.Initialize(openCount)
  → Service.SetData()
    → Service.RefreshQualityPoolSchoolCards()
  → ViewModel.GenerateCardList()
    → Service.RandomSchoolChooseCards()
  → ViewModel.CardList 更新
    → Panel.RefreshCardUI()
      → Panel.InitCardUI() (每个卡牌)

用户选择 → Command.Execute(cardData)
  → Service.ChooseCard()
    → SchoolManager.AddSchoolCard()
    → Service.OnSchoolCardFlyRequested (流派飞入)
  → ViewModel.OnRefreshNextLayerRequested
    → Panel.RefreshNextLayerCards()
  → Service.SetCurrentLayers()

倒计时结束 → ViewModel.OnUpdate(deltaTime)
  → ViewModel.OnCountdownExpired()
    → Command.Execute(firstCard)
```

---

## 4. 测试检查清单

### 功能测试
- [ ] 界面能正常打开和关闭
- [ ] 打开后游戏暂停（GamePlayManager.PauseGame）
- [ ] 关闭后游戏恢复（GamePlayManager.StartGame）
- [ ] 数据绑定正常工作（标题、金币、品质概率）
- [ ] 按钮点击响应正常（获得全部、刷新、选择）
- [ ] 动画播放正常（选中上移、未选中下移、下一层移入）
- [ ] 与其他界面交互正常（流派飞入特效）

### 兼容性测试
- [ ] 新旧面板共存时无冲突
- [ ] 关闭逻辑正确（UIKit.HidePanel<SchoolChooseCardPanel> 仍可工作）
- [ ] 引导系统正常触发

### 稳定性测试
- [ ] 多次打开/关闭无内存泄漏
- [ ] DOTween 动画正确清理
- [ ] 事件监听正确解绑

---

## 5. 后续优化建议

1. **Service 复用**：SchoolManager 中的 `AddSchoolCard` 逻辑可以考虑整合到 Service 中，减少直接依赖
2. **动画抽象**：DOTween 动画可以封装为通用的 Panel 动画工具类
3. **数据模型**：CardDisplayData 和 QualityWeightData 可以考虑序列化以便调试
4. **关闭方式**：当前使用 `UIKit.HidePanel<SchoolChooseCardPanel>` 关闭旧面板，迁移稳定后可改为 `UIManager.ClosePanel`

---

## 6. 历史问题规避说明

本次迁移已规避以下历史问题：

1. **UILevel 歧义**：显式使用 `UIFramework.UILevel.PopUI` 避免与 `QFramework.UILevel` 冲突
2. **事件解绑规范**：使用 `UnityAction` 引用保存，确保 `OnUnbindViewModel` 中逐项解绑，不使用 `RemoveAllListeners`
3. **跨命名空间 Service 访问**：通过 `GameServiceManager.Get<Game.Services.SchoolChooseCardService>()` 显式指定命名空间
4. **动画清理**：在 `CloseChooseCard()` 中统一清理所有 Tween，并在 `OnClose` / `OnHide` 中调用
