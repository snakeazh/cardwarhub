# Toast 接口迁移指南

## 新的使用方式

### 方式1：使用静态工具类（推荐）
```csharp
// 在任意地方调用
FusionContinent.Toast.Show("普通提示");
FusionContinent.Toast.Success("操作成功");
FusionContinent.Toast.Warning("警告信息");
FusionContinent.Toast.Error("错误信息");

// 带时长
FusionContinent.Toast.Show("加载中", 3f);
```

### 方式2：通过 Service 获取
```csharp
var toastService = GameServiceManager.Get<Game.UI.ToastService>();
toastService.Show("提示消息");
toastService.ShowSuccess("成功！");
```

## 旧接口替换对照表

| 旧代码 | 新代码 |
|--------|--------|
| `GameMainPanel.Default?.ShowTip("消息")` | `FusionContinent.Toast.Show("消息")` |
| `GameMainPanel.Default?.ShowTip("消息", 5f, false)` | `FusionContinent.Toast.Show("消息", 5f)` |
| `vm.OnShowTipRequested?.Invoke("消息")` | `FusionContinent.Toast.Show("消息")` |
| `OnShowTipRequested?.Invoke("消息")` | `FusionContinent.Toast.Show("消息")` |

## 迁移步骤

1. **ViewModel 中**：直接调用 `FusionContinent.Toast.Show()` 替换事件触发
2. **Panel 中**：移除 `OnShowTipRequested` 事件绑定
3. **旧代码**：全局替换 `GameMainPanel.Default?.ShowTip` 为 `Toast.Show`

## 注意事项

- `GameMainPanel.ShowTip()` 保留作为兼容接口
- 新的 ToastService 支持队列和类型（Success/Warning/Error）
- 默认时长为 2 秒（旧接口为 0.5 秒）
