using System.Collections.Generic;
using System.Linq;
using Cardwar;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Manager;
using FusionContinent.Model;
using UIFramework;
using UIFramework.Services;

namespace Game.Station
{
    public class StationPanelViewModel : ViewModel
    {
        #region 属性
        public BindableProperty<List<RewardItem>> RewardItems { get; } = new BindableProperty<List<RewardItem>>(new List<RewardItem>());
        public BindableProperty<string> TakeTimeText { get; } = new BindableProperty<string>("");
        public BindableProperty<string> StoredNumText { get; } = new BindableProperty<string>("");
        public BindableProperty<bool> CanClaim { get; } = new BindableProperty<bool>(false);
        public BindableProperty<long> NextTakeTimestamp { get; } = new BindableProperty<long>(0);
        /// <summary>当前倒计时片段的起始服务器时间戳，配合 NextTakeTimestamp 计算进度比例</summary>
        public BindableProperty<long> NextTakeStartTimestamp { get; } = new BindableProperty<long>(0);
        public BindableProperty<RewardItem> NextTakeReward { get; } = new BindableProperty<RewardItem>(null);
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnReceiveCommand { get; private set; }
        #endregion

        #region 事件
        public System.Action OnCloseRequested;
        #endregion

        #region 依赖
        private StationService mService;
        #endregion

        private int mMaxStoredNum;

        public override void OnCreate()
        {
            base.OnCreate();
            mService = GameServiceManager.Get<StationService>();

            OnCloseCommand = new Command(OnClose);
            OnReceiveCommand = new Command(OnReceive);

            var constTable = DataConfigManager.Tables.TbGameConst;
            mMaxStoredNum = constTable.PostStationMaxPhysicalStrengthNum;
            TakeTimeText.Value = $"每天{string.Join("、", constTable.PostStationDailyTakeTime)}时可领取体力";
            ComputeNextTakeRange(constTable.PostStationDailyTakeTime,
                out var startTs, out var endTs);
            NextTakeStartTimestamp.Value = startTs;
            NextTakeTimestamp.Value = endTs;
            NextTakeReward.Value = new RewardItem(constTable.PostStationPhysicalStrengthNum.Id, constTable.PostStationPhysicalStrengthNum.Num);

            LoadData().Forget();
        }

        // 计算当前倒计时片段的 [起始, 结束] 服务器时间戳：结束点为下一个未到的领取时间，
        // 起始点为上一个刚过去的领取时间。
        private static void ComputeNextTakeRange(List<int> takeTimes, out long startTs, out long endTs)
        {
            var ordered = takeTimes.OrderBy(t => t).ToList();
            var now = ServerTimeManager.Instance.GetDataTime();

            // 把昨天/今天/明天三天的领取时间点展开成时间线，找到 now 落在哪个 [start, end) 区间
            // 兜底：取昨天最后一个点 ~ 明天第一个点（理论上不会走到）
            System.DateTime chosenStart = now.Date.AddDays(-1).AddHours(ordered[ordered.Count - 1]);
            System.DateTime chosenEnd = now.Date.AddDays(2).AddHours(ordered[0]);

            var timelineHours = new List<System.DateTime>();
            for (int d = -1; d <= 1; d++)
            {
                foreach (var h in ordered)
                {
                    timelineHours.Add(now.Date.AddDays(d).AddHours(h));
                }
            }
            timelineHours.Sort();

            for (int i = 0; i < timelineHours.Count - 1; i++)
            {
                if (timelineHours[i] <= now && now < timelineHours[i + 1])
                {
                    chosenStart = timelineHours[i];
                    chosenEnd = timelineHours[i + 1];
                    break;
                }
            }

            startTs = new System.DateTimeOffset(chosenStart).ToUnixTimeSeconds();
            endTs = new System.DateTimeOffset(chosenEnd).ToUnixTimeSeconds();
        }

        private async UniTask LoadData()
        {
            var stamina = await mService.GetStaminaInfoAsync();
            if (stamina != null)
            {
                ApplyStamina(stamina);
            }
        }

        private void ApplyStamina(StationStamina stamina)
        {
            var items = new List<RewardItem>();
            long total = 0;
            foreach (var slot in stamina.Slots)
            {
                items.Add(new RewardItem(slot.ItemId, slot.Num));
                total += slot.Num;
            }

            RewardItems.Value = items;
            StoredNumText.Value = $"已存放体力：{total}/{mMaxStoredNum}";
            CanClaim.Value = items.Count > 0;
        }

        private void OnClose()
        {
            if (OnCloseRequested != null)
            {
                OnCloseRequested.Invoke();
            }
        }

        private async void OnReceive()
        {
            if (!CanClaim.Value)
            {
                Toast.Show("暂无可领取的体力");
                return;
            }

            var claimedItems = RewardItems.Value;
            var proto = await mService.ClaimStaminaAsync();
            if (proto == null)
            {
                return;
            }

            if (proto.Stamina != null)
            {
                ApplyStamina(proto.Stamina);
            }

            if (claimedItems.Count > 0)
            {
                GlobalFunc.ShowRewardTips(claimedItems).Forget();
            }
        }
    }
}
