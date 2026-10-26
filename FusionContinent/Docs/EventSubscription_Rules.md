# 项目事件订阅规则文档

## 核心原则

**所有全局事件订阅必须在 ViewModel 中处理，Panel 只订阅 ViewModel 的事件。**

```
Global Event → ViewModel Subscribe → ViewModel Event → Panel Subscribe → UI Update
```

## 规则清单

### 规则1：ViewModel 订阅全局事件

```csharp
// ✅ 正确：ViewModel 订阅全局事件
public class SoldierPanelViewModel : ViewModel
{
    private Action<object> mCardDataChangedHandler;
    
    private void SubscribeDataChangeEvents()
    {
        mCardDataChangedHandler = OnCardDataChanged;
        Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_DATA, mCardDataChangedHandler);
    }
    
    private void UnsubscribeDataChangeEvents()
    {
        if (mCardDataChangedHandler != null)
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_CARD_DATA, mCardDataChangedHandler);
            mCardDataChangedHandler = null;
        }
    }
    
    private void OnCardDataChanged(object data)
    {
        // 处理数据逻辑
        RefreshData();
        // 触发 ViewModel 事件通知 Panel
        OnDataChanged?.Invoke();
    }
}
```

### 规则2：Panel 只订阅 ViewModel 事件

```csharp
// ✅ 正确：Panel 只订阅 ViewModel 的事件
public class SoldierPanelNew : UIPanel<SoldierPanelViewModel>
{
    protected override void OnBindViewModel(SoldierPanelViewModel vm)
    {
        // ❌ 禁止：不要在这里订阅全局事件
        // Global.Publisher.Subscribe(...)
        
        // ✅ 正确：只订阅 ViewModel 的事件
        vm.OnDataChanged += OnDataChanged;
        vm.OnSkillDataChanged += OnSkillDataChanged;
    }
    
    protected override void OnUnbindViewModel(SoldierPanelViewModel vm)
    {
        // 取消订阅
        vm.OnDataChanged -= OnDataChanged;
        vm.OnSkillDataChanged -= OnSkillDataChanged;
    }
    
    private void OnDataChanged()
    {
        // 更新 UI
    }
}
```

### 规则3：必须保存 Handler 引用

```csharp
// ✅ 正确：保存 handler 引用用于取消订阅
private Action<object> mEventHandler;

private void Subscribe()
{
    mEventHandler = OnEventReceived;
    Global.Publisher.Subscribe(MessagePublisher.EVENT_NAME, mEventHandler);
}

private void Unsubscribe()
{
    if (mEventHandler != null)
    {
        Global.Publisher.Unsubscribe(MessagePublisher.EVENT_NAME, mEventHandler);
        mEventHandler = null;
    }
}

// ❌ 禁止：匿名函数无法取消订阅
Global.Publisher.Subscribe(MessagePublisher.EVENT_NAME, (data) => { ... });
```

### 规则4：在 OnCreate/OnDestroy 中管理订阅

```csharp
public class MyViewModel : ViewModel
{
    public override void OnCreate()
    {
        base.OnCreate();
        SubscribeEvents();      // 订阅
    }
    
    public override void OnDestroy()
    {
        UnsubscribeEvents();    // 取消订阅（必须！）
        base.OnDestroy();
    }
}
```

### 规则5：一个事件一个 Handler

```csharp
// ✅ 正确：每个事件独立的 handler
private Action<object> mCardDataHandler;
private Action<object> mItemDataHandler;
private Action<object> mSkinChangedHandler;

// ❌ 禁止：不要复用 handler
// mHandler = OnEvent1; Subscribe(EVENT1, mHandler);
// mHandler = OnEvent2; Subscribe(EVENT2, mHandler); // 错误！
```

### 规则6：事件命名规范

```csharp
// ViewModel 中定义事件（给 Panel 订阅）
public System.Action OnDataChanged;           // 数据变化
public System.Action<int> OnCardDataChanged;  // 带参数
public System.Action OnSkillDataChanged;      // 技能数据变化
public System.Action<int> OnCardSkinChanged;  // 皮肤变更

// 全局事件常量（MessagePublisher 中定义）
public const string CHANGE_CARD_DATA = "ChangeCardData";
public const string REFRESH_SKILL_LIST = "RefreshSkillList";
public const string CARD_SKIN_CHANGED = "CardSkinChanged";
```

## 完整示例

### 场景：士兵界面需要响应卡牌数据变化

**步骤1：在 MessagePublisher 中定义全局事件**

```csharp
public static class MessagePublisher
{
    public const string CHANGE_CARD_DATA = "ChangeCardData";
}
```

**步骤2：在 ViewModel 中订阅全局事件并触发本地事件**

```csharp
public class SoldierPanelViewModel : ViewModel
{
    // 全局事件 handler
    private Action<object> mCardDataChangedHandler;
    
    // ViewModel 事件（供 Panel 订阅）
    public System.Action OnSkillDataChanged;
    
    public override void OnCreate()
    {
        base.OnCreate();
        SubscribeEvents();
    }
    
    public override void OnDestroy()
    {
        UnsubscribeEvents();
        base.OnDestroy();
    }
    
    private void SubscribeEvents()
    {
        // 订阅全局事件
        mCardDataChangedHandler = OnCardDataChanged;
        Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_DATA, mCardDataChangedHandler);
    }
    
    private void UnsubscribeEvents()
    {
        if (mCardDataChangedHandler != null)
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_CARD_DATA, mCardDataChangedHandler);
            mCardDataChangedHandler = null;
        }
    }
    
    private void OnCardDataChanged(object data)
    {
        // 解析数据
        int cardId = ParseCardId(data);
        
        // 更新 ViewModel 数据
        RefreshSoldierListData();
        RefreshSkillListData();
        
        // 触发本地事件通知 Panel
        OnSkillDataChanged?.Invoke();
    }
    
    private int ParseCardId(object data)
    {
        if (data is int id) return id;
        if (data is Dictionary<string, object> dict && dict.ContainsKey("cardId"))
            return Convert.ToInt32(dict["cardId"]);
        return 0;
    }
}
```

**步骤3：在 Panel 中订阅 ViewModel 事件**

```csharp
public class SoldierPanelNew : UIPanel<SoldierPanelViewModel>
{
    protected override void OnBindViewModel(SoldierPanelViewModel vm)
    {
        // ✅ 只订阅 ViewModel 的事件
        vm.OnSkillDataChanged += OnSkillDataChanged;
    }
    
    protected override void OnUnbindViewModel(SoldierPanelViewModel vm)
    {
        vm.OnSkillDataChanged -= OnSkillDataChanged;
    }
    
    private void OnSkillDataChanged()
    {
        // 刷新 UI
        RefreshSkillScrollView(VM.SkillList.Value, mOldPanel.SkillCardScroll);
        RefreshGeneralSkillScrollView(VM.GeneralSkillList.Value, mOldPanel.GeneralSkillScroll);
    }
}
```

**步骤4：在其他地方发布事件**

```csharp
// 技能升级成功后发布事件
public class SkillUpGradePanel : UIPanel
{
    private async void OnUpgrade()
    {
        var result = await UpgradeSkillAsync(skillId);
        if (result.Success)
        {
            // 更新数据
            Global.PlayerCards[result.Card.CardId] = new PlayerCard(result.Card);
            
            // 发布全局事件
            Global.Publisher.Publish(MessagePublisher.CHANGE_CARD_DATA, result.Card.CardId);
        }
    }
}
```

## 常见错误

### 错误1：Panel 直接订阅全局事件

```csharp
// ❌ 禁止
public class SoldierPanelNew : UIPanel<SoldierPanelViewModel>
{
    protected override void OnBindViewModel(SoldierPanelViewModel vm)
    {
        // 不要在 Panel 中订阅全局事件！
        Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_DATA, (data) => {
            RefreshUI();
        });
    }
}
```

### 错误2：使用匿名函数无法取消订阅

```csharp
// ❌ 禁止
private void Subscribe()
{
    Global.Publisher.Subscribe(MessagePublisher.EVENT, (data) => { ... });
    // 无法取消订阅！
}
```

### 错误3：Handler 复用

```csharp
// ❌ 禁止
private Action<object> mHandler;

private void Subscribe()
{
    mHandler = OnEvent1;
    Global.Publisher.Subscribe(EVENT1, mHandler);
    
    mHandler = OnEvent2;  // 覆盖！
    Global.Publisher.Subscribe(EVENT2, mHandler);  // EVENT1 的订阅丢失！
}
```

### 错误4：忘记取消订阅

```csharp
// ❌ 禁止
public override void OnDestroy()
{
    // 忘记取消订阅！
    base.OnDestroy();
}
```

## 检查清单

添加新事件时，请检查：

- [ ] 全局事件定义在 `MessagePublisher` 中
- [ ] ViewModel 中订阅全局事件并保存 handler 引用
- [ ] ViewModel 中定义本地事件供 Panel 订阅
- [ ] ViewModel 在 `OnDestroy` 中取消订阅
- [ ] Panel 只订阅 ViewModel 的事件，不订阅全局事件
- [ ] Panel 在 `OnUnbindViewModel` 中取消订阅
- [ ] 事件处理器使用成员方法，不使用匿名函数
- [ ] 每个事件有独立的 handler 字段

## 文档更新记录

| 日期 | 版本 | 修改内容 |
|------|------|----------|
| 2026-03-27 | 1.0 | 初始版本，规范事件订阅规则 |

---

**警告：违反以上规则可能导致内存泄漏、事件重复触发或界面不刷新等问题。**
