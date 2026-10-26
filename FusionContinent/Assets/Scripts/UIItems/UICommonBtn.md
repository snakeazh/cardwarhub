# UICommonBtn 使用文档

通用样式按钮。外部**只通过** `SetData` / `SetInteractable` 使用，不要直接访问内部 `Btn` 与内容节点。

- 脚本：`FusionContinent/Assets/Scripts/UIItems/UICommonBtn.cs`
- 底图图集短名：`Button`（`Global.ButtonAtlas`，地址 `SpriteAtlas_Button`）
- 消耗图标：`ItemIcon`（经 `UIConsumeItem`）
- 字号样式：`Assets/Res/UIConfig/TextStyleSheet.asset`

---

## 1. Prefab 绑定

| 字段 | 说明 |
|------|------|
| `Bg` | 按钮底图 Image |
| `Btn` | `UIPointerClickAndLongPress`（私有，Inspector 绑定） |
| **纯文字** | |
| `TextOnlyRoot` | 纯文字内容根节点 |
| `TextOnlyDesc` | 纯文字 TMP |
| **文字 + 消耗** | |
| `TextAndConsumeRoot` | 该模式内容根节点 |
| `TextAndConsumeDesc` | 该模式文字 TMP（独立节点，不是 TextOnlyDesc） |
| `TextAndConsumeItem` | `UIConsumeItem`（Icon + CountText） |
| **仅消耗** | |
| `ConsumeOnlyRoot` | 该模式内容根节点 |
| `ConsumeOnlyItem` | `UIConsumeItem` |

约定：

- 三套 `*Root` **互斥**，`SetData` 时只激活当前 `ContentMode` 对应根节点。
- TMP 需挂载项目 Style Sheet（或 TMP Default 指向同一份 `TextStyleSheet`）。
- 消耗数量文案由 `UIConsumeItem` 渲染为 `拥有/需求`，并按是否足够着色。

建议层级示例：

```
UICommonBtn
├── Bg
├── Btn (UIPointerClickAndLongPress)
├── TextOnlyRoot
│   └── Desc (TMP)
├── TextAndConsumeRoot
│   ├── Desc (TMP)
│   └── Consume (UIConsumeItem: Icon + CountText)
└── ConsumeOnlyRoot
    └── Consume (UIConsumeItem)
```

---

## 2. 对外 API

```csharp
void SetData(ButtonData buttonData);
void SetInteractable(bool interactable);
```

### ButtonData

| 字段 | 类型 | 说明 |
|------|------|------|
| `buttonStyle` | `ButtonStyleType` | 底图样式，对应图集内 Sprite 名 |
| `ContentMode` | `ButtonContentMode` | 三种内容布局，默认 `TextOnly` |
| `TextStyle` | `ButtonTextStyle` | 字号样式，默认 `Default`（不改 Prefab） |
| `ButtonDesc` | `string` | 按钮文案（TextOnly / TextAndConsume） |
| `ConsumeItemId` | `int` | 消耗道具 Id |
| `ConsumeNeedCount` | `int` | 消耗所需数量 |
| `OnClick` | `Action` | 点击回调 |

### ButtonStyleType（底图）

Sprite 名须与枚举名一致：`ButtonBlue` / `ButtonRed` / `ButtonYellow` / `ButtonGreen` / `ButtonPink` / `ButtonGray`。

### ButtonContentMode（内容）

| 值 | 表现 | 会写入的数据 |
|----|------|--------------|
| `TextOnly` | 只显示文字根 | `ButtonDesc` |
| `TextAndConsume` | 文字 + 消耗 | `ButtonDesc` + `ConsumeItemId` / `ConsumeNeedCount` |
| `ConsumeOnly` | 只显示消耗 | `ConsumeItemId` / `ConsumeNeedCount` |

### ButtonTextStyle（字号）

对应 `TextStyleSheet` 样式名；作用于**当前模式**的文案 TMP，以及消耗 `CountText`。

| 值 | 说明 |
|----|------|
| `Default` | 不修改，沿用 Prefab |
| `Normal` | 空样式 |
| `ButtonSmall` | 约 133% |
| `ButtonMiddle` | 约 167% |
| `ButtonLarge` | 约 200% |
| `Content24` / `Content36` / `Content48` | 约 67% / 100% / 133% |

---

## 3. 代码示例

### 纯文字

```csharp
mConfirmBtn.SetData(new ButtonData
{
    buttonStyle = ButtonStyleType.ButtonBlue,
    ContentMode = ButtonContentMode.TextOnly,
    TextStyle = ButtonTextStyle.ButtonMiddle,
    ButtonDesc = "确定",
    OnClick = OnConfirmClicked,
});
```

### 文字 + 消耗

```csharp
mBuyBtn.SetData(new ButtonData
{
    buttonStyle = ButtonStyleType.ButtonYellow,
    ContentMode = ButtonContentMode.TextAndConsume,
    TextStyle = ButtonTextStyle.ButtonSmall,
    ButtonDesc = "购买",
    ConsumeItemId = 1010001,
    ConsumeNeedCount = 10,
    OnClick = OnBuyClicked,
});
```

### 仅消耗

```csharp
mCostBtn.SetData(new ButtonData
{
    buttonStyle = ButtonStyleType.ButtonGreen,
    ContentMode = ButtonContentMode.ConsumeOnly,
    TextStyle = ButtonTextStyle.ButtonMiddle,
    ConsumeItemId = 1010001,
    ConsumeNeedCount = 10,
    OnClick = OnBuyClicked,
});
```

### 可点击状态

```csharp
mConfirmBtn.SetInteractable(false);
```

刷新展示（例如货币变化后）再次调用 `SetData` 即可。

---

## 4. 预加载注意

- 底图依赖图集 **`Button`**（`Global.ButtonAtlas`），已放在 `UIAtlasPreloadTable.CommonAtlasKeys`。
- 消耗图标依赖 **`ItemIcon`**（通常已在 Common）。
- Common 里的 `UICommonBtn` 是另一张图集，与本组件底图 `Button` **不是同一个**。

---

## 5. 禁止事项

- 不要外部拿 `Btn` / 子节点直接改文案或监听（`Btn` 已 private）。
- 不要用 `RemoveAllListeners` 清点击；组件内部已成对绑定/解绑。
- 新功能可点击控件统一用 `UIPointerClickAndLongPress`，不要再挂 `Button`。
