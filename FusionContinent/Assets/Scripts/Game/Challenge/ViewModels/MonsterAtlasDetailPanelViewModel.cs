using System;
using System.Collections.Generic;
using Config;
using FusionContinent;
using FusionContinent.Model;
using FusionContinent.Utils;
using Game.RedDot;
using UIFramework;
using UIFramework.Services;

namespace Game.Challenge
{
    /// <summary>
    /// 怪物图鉴详情面板 ViewModel（UIFramework MVVM）。
    /// 由外部调用 Setup(monsterId) 注入要展示的怪物，构建展示数据；领奖通过 AtlasService 完成。
    /// </summary>
    public class MonsterAtlasDetailPanelViewModel : ViewModel
    {
        #region 属性
        /// <summary>怪物名（loc key）</summary>
        public BindableProperty<string> MonsterName { get; } = new BindableProperty<string>("");
        /// <summary>怪物描述（loc key）</summary>
        public BindableProperty<string> MonsterDesc { get; } = new BindableProperty<string>("");
        /// <summary>能力描述（loc key）</summary>
        public BindableProperty<string> AbilityDesc { get; } = new BindableProperty<string>("");
        /// <summary>线稿图 sprite 名（如 "RockGiantLineArt"），空串表示无</summary>
        public BindableProperty<string> LineArtSpriteName { get; } = new BindableProperty<string>("");
        /// <summary>Spine 预制体名，空串表示无</summary>
        public BindableProperty<string> SpineName { get; } = new BindableProperty<string>("");
        /// <summary>Spine 缩放，<=0 表示用默认 1</summary>
        public BindableProperty<float> SpineScale { get; } = new BindableProperty<float>(1f);
        /// <summary>奖励列表（供 View 渲染 Content）</summary>
        public BindableProperty<List<RewardAndNum>> AtlasRewards { get; } = new BindableProperty<List<RewardAndNum>>(new List<RewardAndNum>());
        /// <summary>当前图鉴状态</summary>
        public BindableProperty<RewardStatus> AtlasStatus { get; } = new BindableProperty<RewardStatus>(RewardStatus.Default);
        #endregion

        #region 命令
        public Command OnCloseCommand { get; private set; }
        public Command OnReceiveCommand { get; private set; }
        #endregion

        private AtlasService _atlasService;
        private int _monsterId;

        public override void OnCreate()
        {
            base.OnCreate();

            _atlasService = GameServiceManager.Get<AtlasService>();

            OnCloseCommand = new Command(OnClose);
            OnReceiveCommand = new Command(OnReceive);
        }

        /// <summary>
        /// 配置本面板要展示的怪物。从配置表与玩家数据构建展示数据并推送给 View。
        /// </summary>
        public void Setup(int monsterId)
        {
            _monsterId = monsterId;

            var cardCfg = DataConfigManager.Tables.TbCardBaseConfig.GetOrDefault(monsterId);
            var monsterCfg = DataConfigManager.Tables.TbMonsterBattleConfig.GetOrDefault(monsterId);
            if (monsterCfg == null || cardCfg == null)
            {
                LogTool.LogError("[MonsterAtlasDetailPanelViewModel] 配置缺失，monsterId=" + monsterId);
                return;
            }

            MonsterName.Value = cardCfg.Name;
            MonsterDesc.Value = cardCfg.Desc;
            AbilityDesc.Value = monsterCfg.AbilityDesc;

            // 线稿 sprite 名：Type 名 + "LineArt"
            LineArtSpriteName.Value = monsterCfg.Type.ToString() + "LineArt";

            // Spine
            SpineName.Value = monsterCfg.Spine != null ? monsterCfg.Spine : "";
            SpineScale.Value = monsterCfg.SpineScale;

            // 奖励
            AtlasRewards.Value = monsterCfg.AtlasRewards != null
                ? new List<RewardAndNum>(monsterCfg.AtlasRewards)
                : new List<RewardAndNum>();

            // 解锁/领取状态
            AtlasStatus.Value = Global.PlayerAtlas.GetMonsterAtlasStatus(monsterId);

            OnDataReady?.Invoke();
        }

        /// <summary>领奖后重新读取状态（仅刷新状态相关，避免重建 Spine/LineArt）</summary>
        public void RefreshStatus()
        {
            AtlasStatus.Value = Global.PlayerAtlas.GetMonsterAtlasStatus(_monsterId);
        }

		private async void OnReceive()
		{
			if (_atlasService == null)
			{
				OnShowTip?.Invoke("服务未就绪", 2f);
				return;
			}
			if (AtlasRewards.Value == null || AtlasRewards.Value.Count == 0)
			{
				OnShowTip?.Invoke("无可领取奖励", 2f);
				return;
			}
			OnReceiveStarted?.Invoke();
			var success = await _atlasService.ReceiveMonsterRewardAsync(_monsterId);
			OnReceiveFinished?.Invoke();
			if (!success) return;
			// 刷新本面板状态（已领取）
			RefreshStatus();
			// 通知图鉴列表刷新解锁/红点 + 红点系统
			Global.Publisher.Publish(MessagePublisher.MONSTER_ATLAS_STATUS_CHANGED, null);
			Global.Publisher.Publish(MessagePublisher.CHANGE_RED_TYPE + RedNodeType.MonsterAtlasBtn, null);
		}

        private void OnClose()
        {
            OnClosePanel?.Invoke();
        }

        #region 事件
        public Action OnClosePanel;
        /// <summary>数据准备完成，View 可据此渲染 Spine/LineArt/ScrollView/Content</summary>
        public Action OnDataReady;
        /// <summary>开始领取（可禁用按钮防重复点击）</summary>
        public Action OnReceiveStarted;
        /// <summary>领取结束（无论成败）</summary>
        public Action OnReceiveFinished;
        /// <summary>展示奖励，参数为 rewardsDict</summary>
        /// <summary>展示提示</summary>
        public Action<string, float> OnShowTip;
        #endregion
    }
}
