# UIVerticalScrollViewLockRank 使用文档

## 概述

`UIVerticalScrollViewLockRank` 继承自 `UIVerticalScrollViewLock`，在原有竖向虚拟化列表（锁定每行排列数）的基础上，增加了**排行榜式底部上拉加载更多**能力：

- **底部上拉触发请求** - 列表已滚到底部时继续上拉超过阈值并松手，触发加载回调（在此处发送服务器请求）
- **追加数据刷新** - 服务器数据返回后调用 `AppendData` 追加数据并刷新界面，滚动位置保持不变
- **请求防重** - 等待服务器返回期间自动屏蔽重复上拉，防止连发请求

脚本路径：`Assets/Scripts/UI/ScrollView/Impl/UIVerticalScrollViewLockRank.cs`，命名空间 `ScrollView`。

## 核心特性

1. **拖拽增量检测** - 通过累计拖拽距离判断上拉，ScrollRect 的 `MovementType` 无论是 Elastic 还是 Clamped 都能正常工作
2. **追加不重置位置** - `AppendData` 底层只扩充 content 高度并刷新可见项，玩家当前滚动位置不受影响
3. **状态自管理** - 触发后进入等待状态，`AppendData` / `EndRefresh` / `ReleaseScrollView` 自动解除

## Inspector 属性

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| PullRefreshThreshold | float | 100 | 底部上拉触发加载的距离（像素） |
| ReboundTimeout | float | 1 | 等待弹性回弹结束的最长时间（秒），超时强制触发回调 |

其余属性（ItemPrefab、ItemSize、ItemSpacing、ItemsPerLine 等）继承自基类，配置方式与 `UIVerticalScrollViewLock` 完全一致。

## API

### SetPullRefreshCallback / OnPullRefresh

注册底部上拉加载回调。底部上拉超过阈值并松手后，**等弹性回弹结束**才触发（最长等待 ReboundTimeout 秒），在回调里发送服务器请求。

```csharp
public virtual void SetPullRefreshCallback(Action onPullRefresh)
public Action OnPullRefresh; // 也可以直接赋值
```

### AppendData

服务器数据返回后调用：解除等待状态，追加 `addedCount` 条数据并刷新界面。

```csharp
/// <param name="addedCount">本次追加的数据条数，传 0 表示没有更多数据</param>
public void AppendData(int addedCount)
```

注意：调用前请先把新数据加进你自己的数据列表，`addedCount` 只是告诉 ScrollView 总数增加了多少，界面刷新仍然走 `OnUpdateItem` 回调取数据。

### EndRefresh

请求失败或本次不需要追加数据时调用，仅解除等待状态，不改变列表。

```csharp
public void EndRefresh()
```

### IsWaitingResponse

是否正在等待服务器返回。等待期间上拉不会重复触发 `OnPullRefresh`。

```csharp
public bool IsWaitingResponse { get; }
```

## 完整流程

```
玩家在列表底部上拉超过阈值松手
        ↓
进入等待状态，等弹性回弹结束（超时强制继续）
        ↓
OnPullRefresh 触发（等待期间屏蔽重复触发）
        ↓
发送服务器请求
        ↓
   ┌────────────┬────────────┐
   ↓            ↓            ↓
 成功有数据    成功无数据    请求失败
   ↓            ↓            ↓
数据加入列表  AppendData(0)  EndRefresh()
AppendData(n) （仅解除等待） （仅解除等待）
   ↓
界面追加刷新，滚动位置不变
```

## 使用示例

排行榜分页加载场景：

```csharp
public class RankPanelNew : UIPanel<RankPanelViewModel>
{
    [SerializeField] private UIVerticalScrollViewLockRank mRankScrollView;

    private readonly List<RankItemData> mRankList = new List<RankItemData>();
    private int mPage = 0;
    private const int PageSize = 20;

    protected override void OnOpen(IUIData uiData = null)
    {
        // 1. 注册 item 刷新回调（与普通 ScrollView 相同）
        mRankScrollView.SetUpdateFunc((dataIndex, scrollItem) =>
        {
            var item = scrollItem as RankScrollItem;
            item.SetData(mRankList[dataIndex]);
        });

        // 2. 注册底部上拉加载回调：请求下一页
        mRankScrollView.SetPullRefreshCallback(() =>
        {
            NetManager.ReqRankList(mPage + 1, PageSize, OnRankListResp, OnRankListError);
        });

        // 3. 首次加载第一页
        NetManager.ReqRankList(1, PageSize, OnRankListResp, OnRankListError);
    }

    private void OnRankListResp(int page, List<RankItemData> items)
    {
        mPage = page;
        mRankList.AddRange(items);

        if (mPage == 1)
        {
            // 首次填充
            mRankScrollView.ShowList(mRankList.Count);
        }
        else
        {
            // 追加刷新，滚动位置保持不变
            mRankScrollView.AppendData(items.Count);
        }
    }

    private void OnRankListError()
    {
        mRankScrollView.EndRefresh();
    }
}
```

## 注意事项

1. **prefab 迁移**：组件与 `UIVerticalScrollViewLock` 使用相同的序列化字段，直接把 prefab 上的旧组件替换为 `UIVerticalScrollViewLockRank` 即可，原有配置不丢失。
2. **事件依赖**：上拉检测依赖 `ScrollRect` 所在物体接收 EventSystem 拖拽事件（基类已 `[RequireComponent(typeof(ScrollRect))]`），不要在其上覆盖会拦截拖拽的透明 Raycast Target。
3. **数据一致性**：`AppendData` 的 `addedCount` 必须与实际新增的数据条数一致，否则 `OnUpdateItem` 取数据时会越界或留空。
4. **整页重刷**：如果需要完全重新加载（如切换榜单类型），直接重新 `ShowList(count)` 即可；等待状态会在下次 `AppendData` / `EndRefresh` / `ReleaseScrollView` 时重置。
5. **无更多数据**：服务器返回空列表时传 `AppendData(0)`，仅解除等待状态不刷新界面；如需提示"没有更多了"，由业务侧自行处理。
