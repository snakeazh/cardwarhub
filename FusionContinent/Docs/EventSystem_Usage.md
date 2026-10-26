# 项目事件订阅系统使用文档

## 概述

项目使用 `Global.Publisher` 作为全局事件发布订阅中心，基于字符串消息类型进行事件通信。

## 基础 API

### 1. 订阅事件

```csharp
// 方式1：使用对象参数的事件处理器
Global.Publisher.Subscribe(string messageType, Action<object> handler);

// 方式2：使用泛型（如果支持）
Global.Publisher.Subscribe<T>(string messageType, Action<T> handler);
```

### 2. 取消订阅

```csharp
// 必须保存 handler 引用用于取消订阅
Global.Publisher.Unsubscribe(string messageType, Action<object> handler);
```

### 3. 发布事件

```csharp
// 发布事件（带数据）
Global.Publisher.Publish(string messageType, object data);

// 发布事件（不带数据）
Global.Publisher.Publish(string messageType);
```

## 常用消息类型常量

```csharp
public static class MessagePublisher
{
    // 卡牌数据变化
    public const string CHANGE_CARD_DATA = "ChangeCardData";
    
    // 道具数据变化
    public const string CHANGE_ITEM_DATA = "ChangeItemData";
    
    // 卡牌皮肤变更
    public const string CARD_SKIN_CHANGED = "CardSkinChanged";
    
    // 士兵技能列表刷新
    public const string REFRESH_SOLDIER_SKILL_LIST = "RefreshSoldierSkillList";
    
    // 士兵奥术卡列表刷新
    public const string REFRESH_SOLDIER_ARCANE_LIST = "RefreshSoldierArcaneList";
    
    // 红点数据变化
    public const string RED_DOT_CHANGED = "RedDotChanged";
    
    // 其他...
}
```

## 完整使用示例

### 示例1：ViewModel 中订阅数据变化事件

```csharp
public class SoldierPanelViewModel : ViewModel
{
    // 保存事件处理器引用，用于取消订阅
    private Action<object> mCardDataChangedHandler;
    
    public override void OnCreate()
    {
        base.OnCreate();
        
        // 初始化其他...
        
        // 订阅数据变化事件
        SubscribeDataChangeEvents();
    }
    
    public override void OnDestroy()
    {
        base.OnDestroy();
        
        // 取消订阅（必须！避免内存泄漏）
        UnsubscribeDataChangeEvents();
    }
    
    private void SubscribeDataChangeEvents()
    {
        // 创建事件处理器并保存引用
        mCardDataChangedHandler = OnCardDataChanged;
        
        // 订阅事件
        Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_DATA, mCardDataChangedHandler);
    }
    
    private void UnsubscribeDataChangeEvents()
    {
        // 使用保存的引用取消订阅
        if (mCardDataChangedHandler != null)
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_CARD_DATA, mCardDataChangedHandler);
            mCardDataChangedHandler = null;
        }
    }
    
    /// <summary>
    /// 事件处理器 - 处理卡牌数据变化
    /// </summary>
    private void OnCardDataChanged(object data)
    {
        // 解析事件数据（支持多种数据格式）
        int cardId = 0;
        if (data is int id)
        {
            cardId = id;
        }
        else if (data is Dictionary<string, object> dict && dict.ContainsKey("cardId"))
        {
            cardId = Convert.ToInt32(dict["cardId"]);
        }
        
        Debug.Log($"[SoldierPanelViewModel] 收到卡牌数据变化: CardId={cardId}");
        
        // 执行业务逻辑...
        RefreshData();
    }
}
```

### 示例2：Panel 中订阅全局事件

```csharp
public class SoldierPanelNew : UIPanel<SoldierPanelViewModel>
{
    // 保存事件处理器引用
    private Action<object> mCardSkinChangedHandler;
    private Action<object> mRefreshSkillListHandler;
    
    protected override void OnBindViewModel(SoldierPanelViewModel vm)
    {
        // 创建并订阅事件
        mCardSkinChangedHandler = (data) =>
        {
            if (data is int skinId)
            {
                // 处理皮肤变更
                UpdateSkinDisplay(skinId);
            }
        };
        Global.Publisher.Subscribe(MessagePublisher.CARD_SKIN_CHANGED, mCardSkinChangedHandler);
        
        // 订阅另一个事件
        mRefreshSkillListHandler = (_) =>
        {
            RefreshSkillList();
        };
        Global.Publisher.Subscribe(MessagePublisher.REFRESH_SOLDIER_SKILL_LIST, mRefreshSkillListHandler);
    }
    
    protected override void OnUnbindViewModel(SoldierPanelViewModel vm)
    {
        // 取消所有订阅（必须！）
        if (mCardSkinChangedHandler != null)
        {
            Global.Publisher.Unsubscribe(MessagePublisher.CARD_SKIN_CHANGED, mCardSkinChangedHandler);
            mCardSkinChangedHandler = null;
        }
        
        if (mRefreshSkillListHandler != null)
        {
            Global.Publisher.Unsubscribe(MessagePublisher.REFRESH_SOLDIER_SKILL_LIST, mRefreshSkillListHandler);
            mRefreshSkillListHandler = null;
        }
    }
}
```

### 示例3：发布事件

```csharp
public class SkillUpGradePanel : UIPanel
{
    private async void OnUpgradeSkill()
    {
        // 发送升级请求到服务器
        var result = await UpgradeSkillAsync(skillId, consumeItems);
        
        if (result.Success)
        {
            // 更新本地数据
            Global.PlayerCards[result.Card.CardId] = new PlayerCard(result.Card);
            
            // 发布事件通知其他界面刷新
            Global.Publisher.Publish(MessagePublisher.CHANGE_CARD_DATA, result.Card.CardId);
            
            // 也可以发布带更多数据的事件
            var eventData = new Dictionary<string, object>
            {
                ["cardId"] = result.Card.CardId,
                ["oldLevel"] = oldLevel,
                ["newLevel"] = result.Card.Lv
            };
            Global.Publisher.Publish(MessagePublisher.CHANGE_CARD_DATA, eventData);
        }
    }
}
```

## 最佳实践

### 1. 必须保存 Handler 引用

```csharp
// ❌ 错误：匿名函数无法取消订阅
Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_DATA, (data) => { /* ... */ });
// 无法取消订阅！

// ✅ 正确：保存引用
private Action<object> mHandler = (data) => { /* ... */ };
Global.Publisher.Subscribe(MessagePublisher.CHANGE_CARD_DATA, mHandler);
Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_CARD_DATA, mHandler);
```

### 2. 成对使用 Subscribe/Unsubscribe

```csharp
// 在 OnCreate/OnBindViewModel 中订阅
// 在 OnDestroy/OnUnbindViewModel 中取消订阅

public override void OnCreate()
{
    SubscribeEvents();  // 订阅
}

public override void OnDestroy()
{
    UnsubscribeEvents(); // 取消订阅（必须！）
}
```

### 3. 处理数据类型转换

```csharp
private void OnEventReceived(object data)
{
    // 支持多种数据格式，做好类型检查
    if (data is int intValue)
    {
        // 处理 int 类型
    }
    else if (data is string strValue)
    {
        // 处理 string 类型
    }
    else if (data is Dictionary<string, object> dict)
    {
        // 处理字典类型
        var cardId = Convert.ToInt32(dict["cardId"]);
    }
}
```

### 4. 避免内存泄漏

```csharp
// ❌ 错误：在闭包中捕获 this，且未正确取消订阅
Global.Publisher.Subscribe(MessagePublisher.EVENT, (data) =>
{
    this.DoSomething(); // 捕获了 this
});

// ✅ 正确：使用成员方法或弱引用
private Action<object> mHandler;

private void Subscribe()
{
    mHandler = OnEventReceived;
    Global.Publisher.Subscribe(MessagePublisher.EVENT, mHandler);
}

private void OnEventReceived(object data)
{
    DoSomething();
}

private void Unsubscribe()
{
    if (mHandler != null)
    {
        Global.Publisher.Unsubscribe(MessagePublisher.EVENT, mHandler);
        mHandler = null;
    }
}
```

### 5. 事件命名规范

```csharp
// 使用常量定义，避免拼写错误
public const string EVENT_NAME = "EventName";

// 命名规范：
// - 使用大写字母和下划线
// - 动词开头表示动作
// - 名词结尾表示对象
public const string CHANGE_CARD_DATA = "ChangeCardData";
public const string REFRESH_SKILL_LIST = "RefreshSkillList";
public const string PLAYER_LEVEL_UP = "PlayerLevelUp";
```

## 注意事项

1. **线程安全**：事件回调可能在主线程或其他线程执行，更新 UI 时需确保在主线程
2. **性能考虑**：避免在事件处理器中执行耗时操作
3. **事件顺序**：不保证事件处理的顺序，不要依赖事件处理的先后顺序
4. **异常处理**：事件处理器中的异常不要抛出，使用 try-catch 处理

```csharp
private void OnEventReceived(object data)
{
    try
    {
        // 业务逻辑
    }
    catch (Exception ex)
    {
        Debug.LogError($"[OnEventReceived] 处理事件失败: {ex.Message}");
    }
}
```

## 调试技巧

```csharp
// 添加日志追踪事件流
private void OnEventReceived(object data)
{
    Debug.Log($"[{GetType().Name}] 收到事件 {MessagePublisher.CHANGE_CARD_DATA}, 数据: {data}");
    // ...
}
```
