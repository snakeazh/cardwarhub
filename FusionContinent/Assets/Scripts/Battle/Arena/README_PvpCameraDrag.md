# PVP 滑动镜头（拖拽查看 AI 侧）

本文档说明 GamePlay 场景 PVP AI 模式下，拖拽空白处滑动镜头在「玩家侧 / AI 侧」之间查看的实现与调参。
相关主文档：[`README_PvePvpArena.md`](README_PvePvpArena.md)。

## 交互方式

- 在战场空白处按住并拖动屏幕，**只移动相机**，场景物体（英灵/怪物/子弹/特效）保持不动，避免拖拽时特效乱飞。
- **下滑 = 看向 AI 侧**：相机上移，AI 方（世界坐标在玩家方上方）进入视野。
- **上滑 = 回到玩家侧**：相机回到基准位置。
- 起点默认停在玩家侧；英灵 / 技能卡 / 按钮上按下不会抢镜头（`IsPvpCameraDragBlocked` 拦截），空白处、血条等均可拖动。
- 调试 GUI（测试阶段）左下角显示：`拖拽空白处滑动镜头查看 AI 方（下滑看向对面） 进度: xx% 拖动中: True/False`。

## 场景结构（尺寸依据）

```
MainCamera     (正交, size=9.6, pos=(0,0,-100), Screen Space - Camera 用相机)
GameMap        (根 Canvas, Screen Space - Camera, plane=100)
  pve          anchoredPos(0,0)  全拉伸     → 玩家侧
  pvp          anchoredPos(0,2566) 全拉伸   → AI 侧（默认隐藏，PVP 激活）
GameLand       (无 Canvas，世界挂点：英灵/怪物/子弹/特效)
  pve/pvp      pvp anchoredPos(0,2600)
GameLandUI     (根 Canvas，HUD/飘字/血条容器)
  pve/pvp      pvp anchoredPos(0,2600)
```

- AI 侧战场在玩家侧**上方**（y 偏移约 2566/2600，场景内无代码改位置，仅显隐切换）。
- 滑动上限取 `GameMap/pve → GameMap/pvp` 的世界坐标差（约 2566 canvas 单位 ≈ 20.2 世界单位），滑满时 AI 战场正好位于视野中央。
- Screen Space - Camera 的 canvas（GameMap/GameLandUI）随相机自动保持贴屏，因此背景与 HUD 不“飞”；只有相机位置变化。

## 实现位置（`GamePlayManager.cs`）

| 成员 | 说明 |
|------|------|
| `_pvpDragCamera` | 被移动的相机（GameMap canvas 的 worldCamera，通常为主相机） |
| `_pvpDragCameraBasePos` | 相机基准位置（玩家侧视角，收集时记录） |
| `_pvpDragOffsetY` | 当前相机上移量（世界单位，0=玩家侧，最大值=AI 侧） |
| `_pvpDragMaxOffsetY` | 相机上移上限，按 `GameMap` pve→pvp 实际偏移计算 |
| `_pvpDragWorldPerScreenPixel` | 屏幕像素 → 世界单位换算系数（正交相机 = 2*orthoSize / Screen.height） |
| `_pvpCameraDragLastPos` / `_pvpCameraDragActive` | 指针上一帧位置 / 是否正在拖动 |

| 方法 | 说明 |
|------|------|
| `CollectPvpDragCamera()` | 记录相机基准位置；计算滑动上限与像素换算系数 |
| `ApplyPvpDragOffset()` | `camera.position = 基准 + (0, offset, 0)`，只移动相机 |
| `UpdatePvpCameraDrag()` | 每帧处理触摸/鼠标拖动，换算像素增量并夹取偏移 |
| `IsPvpCameraDragBlocked(pointerId)` | 按下点落在可交互对象（英灵/技能卡/按钮）时阻止开始拖动 |
| `ResetPvpDrag()` | 开战/退局时相机回到玩家侧视角 |

调用链：`InitOpponentCardTeams` → `ResetPvpDrag` + `CollectPvpDragCamera`；`Update` 中 PVP 且未结算时每帧 `UpdatePvpCameraDrag`；退局 `ResetPvpDrag`。

## 关键逻辑

### 1) 只移动相机（物体不动）

```csharp
// ApplyPvpDragOffset
if (_pvpDragCamera == null) return;
_pvpDragCamera.transform.position = _pvpDragCameraBasePos + new Vector3(0f, _pvpDragOffsetY, 0f);
```

- 相机上移 offset → 视野上移 → 上方的 AI 侧进入视野；世界物体位置不变，特效/子弹/飘字不再“乱飞”。
- Screen Space - Camera canvas 由 Unity 每帧贴相机，自动保持贴屏，无需手动补偿。

### 2) 方向（拖动跟随手指）

```csharp
// UpdatePvpCameraDrag
float worldDelta = delta.y * _pvpDragWorldPerScreenPixel;
_pvpDragOffsetY = Mathf.Clamp(_pvpDragOffsetY - worldDelta, 0f, _pvpDragMaxOffsetY);
ApplyPvpDragOffset();
```

- `delta.y > 0`（上滑）→ offset 减小 → 相机下移 → 回到玩家侧。
- `delta.y < 0`（下滑）→ offset 增大 → 相机上移 → AI 侧进入视野。
- 如需改成「上滑看 AI」（滚动条式反向），把 `- worldDelta` 改回 `+ worldDelta` 即可。

### 3) 速度（像素 → 世界单位）

```csharp
// CollectPvpDragCamera 内
if (_pvpDragCamera != null && _pvpDragCamera.orthographic)
{
    _pvpDragWorldPerScreenPixel = 2f * _pvpDragCamera.orthographicSize / Mathf.Max(1f, Screen.height);
}
// 非正交回退：canvas 世界缩放(lossyScale.y) / canvas.scaleFactor
```

- 参考分辨率 1125×2436、正交 size=9.6：1 屏幕像素 ≈ 0.0079 世界单位，拖动 1:1 跟手。
- 之前直接 `offset += delta.y`（像素当世界单位）约快 100 倍，现已换算。

### 4) 滑动范围（按 GameMap pvp 的大小）

```csharp
// CollectPvpDragCamera 内
Transform mapPve = GameMap.Default.transform.Find("pve");
Transform mapPvp = GameMap.Default.transform.Find("pvp");
if (mapPve != null && mapPvp != null)
{
    _pvpDragMaxOffsetY = Mathf.Abs(mapPvp.position.y - mapPve.position.y);
}
// 兜底：找不到 pve/pvp 时退回两侧 FriendlySlot 世界坐标差
```

- 滑满时 AI 战场位于视野中央、地图完整显示、不越界；
- 之前按 `FriendlySlot` 坐标差（约 2600，与 2566 不一致）且可能受站位状态影响，导致显示不全/过头。

## 调试

- `OnGUI`（仅 PVP 测试阶段）实时显示滑动进度与拖动状态；字号较大便于查看。
- `LogTool.Log("[PVP滑动镜头] ...")` 输出：相机 / 上限 / 像素系数 / 玩家槽 / 对侧槽，用于确认范围与换算是否正常。

## 已知限制

- 只处理纵向滑动（`delta.y`）；横向滑动不移动相机。
- canvas 内的 pvp 专属元素（如 `GameMap/pvp` 背景、`GameLandUI/pvp` HUD）贴屏不会随相机平移出现；AI 英灵、血条、特效为 world 子物体，会随相机正常进入视野。
- 若其他系统也直接改相机位置（如震屏），会与滑动共用相机；当前 `CameraShake` 抖动的是 Bg 等物体而非相机，无冲突。
- 调试 GUI 与拖动拦截策略均为测试期实现，后续可按需移除/收紧。

## 改动历史

- 2026-08-05：修复方向（拖动跟随手指）、速度（像素→世界单位换算）、范围（按 `GameMap/pvp` 实际偏移计算）。
- 2026-08-05：由「整体移动 pve/pvp 内容」改为「只移动相机」，避免拖拽时特效/子弹/飘字乱飞。

