using Assets.Scripts.Localization;
using Config;
using FusionContinent;
using FusionContinent.Model;
using System.Collections.Generic;
using UIFramework;
using UnityEngine;

namespace Game.Battle
{
    /// <summary>
    /// 游戏时间管理 ViewModel
    /// 管理游戏时间显示、倍速控制
    /// </summary>
    public class BattleTimeViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 游戏开始时间（秒）
        /// </summary>
        public BindableProperty<float> StartTime { get; } = new BindableProperty<float>(0f);

        /// <summary>
        /// 格式化后的时间字符串（MM:SS）
        /// </summary>
        public BindableProperty<string> FormattedTime { get; } = new BindableProperty<string>("00:00");

        /// <summary>
        /// 是否暂停
        /// </summary>
        public BindableProperty<bool> IsPaused { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 倍速文本显示
        /// </summary>
        public BindableProperty<string> SpeedText { get; } = new BindableProperty<string>("x1");

        /// <summary>
        /// 是否为二倍速（用于外部初始化同步）
        /// </summary>
        public BindableProperty<bool> IsX2Speed { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 是否自动释放技能
        /// </summary>
        public BindableProperty<bool> IsAutoReleaseSkill { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 是否可以开启自动释放技能（根据关卡状态）
        /// </summary>
        public BindableProperty<bool> CanAutoReleaseSkill { get; } = new BindableProperty<bool>(true);

        /// <summary>
        /// 关卡名称
        /// </summary>
        public BindableProperty<string> StageTitle { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 关卡名称渐变颜色1（顶部）
        /// </summary>
        public BindableProperty<List<int>> StageTitleColor1 { get; } = new BindableProperty<List<int>>();

        /// <summary>
        /// 关卡名称渐变颜色2（底部）
        /// </summary>
        public BindableProperty<List<int>> StageTitleColor2 { get; } = new BindableProperty<List<int>>();

        /// <summary>
        /// 波数/天数显示文本
        /// </summary>
        public BindableProperty<string> CityLevelText { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 总伤害显示文本
        /// </summary>
        public BindableProperty<string> TotalDamageText { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 是否显示总伤害文本
        /// </summary>
        public BindableProperty<bool> ShowTotalDamage { get; } = new BindableProperty<bool>(false);
        #endregion

        #region 命令
        public Command OnPauseCommand { get; private set; }
        public Command OnResumeCommand { get; private set; }
        public Command ToggleSpeedCommand { get; private set; }
        public Command<bool> OnAutoReleaseSkillChangedCommand { get; private set; }
        #endregion

        #region 常量
        private const float SPEED_X1 = 1f;
        private const float SPEED_X2 = 2f;
        #endregion

        #region 依赖
        private BattleTimeService mTimeService;
        #endregion

        public override void OnCreate()
        {
            base.OnCreate();

            // 获取 BattleTimeService
            mTimeService = BattleServiceManager.Instance?.GetService<BattleTimeService>();

            // 初始化命令
            OnPauseCommand = new Command(OnPause);
            OnResumeCommand = new Command(OnResume);
            ToggleSpeedCommand = new Command(OnToggleSpeed);
            OnAutoReleaseSkillChangedCommand = new Command<bool>(OnAutoReleaseSkillChanged);

            // 初始化自动释放技能状态
            InitializeAutoReleaseSkill();

            // 初始化关卡信息显示
            InitializeStageInfo();

            // 订阅时间变化，自动格式化
            StartTime.OnValueChanged += (oldVal, newVal) =>
            {
                FormattedTime.Value = FormatTime(newVal);
            };

            // 初始化显示
            FormattedTime.Value = FormatTime(0f);
            SpeedText.Value = "x1";
        }

        #region 私有方法

        private void OnPause()
        {
            IsPaused.Value = true;
            mTimeService?.Pause();
            OnPauseRequested?.Invoke();
        }

        private void OnResume()
        {
            IsPaused.Value = false;
            mTimeService?.Resume();
            OnResumeRequested?.Invoke();
        }

        private void OnToggleSpeed()
        {
            if (IsX2Speed.Value)
            {
                // 当前是 x2，切回 x1
                IsX2Speed.Value = false;
                SpeedText.Value = "x1";
                mTimeService?.SpeedDown();
                OnSpeedChanged?.Invoke(1f);
            }
            else
            {
                // 当前是 x1，切换到 x2
                IsX2Speed.Value = true;
                SpeedText.Value = "x2";
                mTimeService?.SpeedUp();
                OnSpeedChanged?.Invoke(2f);
            }
        }

        private void OnAutoReleaseSkillChanged(bool isOn)
        {
            // 功能开启判断
            if (!ModuleFunction.CheckOpen(ModuleFunctionType.AutomaticBattle))
            {
                // 如果功能未开启，强制关闭
                IsAutoReleaseSkill.Value = false;
                Toast.Show("功能未开启");
                OnAutoReleaseSkillFailed?.Invoke("功能未开启");
                return;
            }

            // 更新状态
            Global.AutoReleaseSkillState.Value = isOn;
            IsAutoReleaseSkill.Value = isOn;
            PlayerPrefs.SetString("AutoReleaseSkillState", isOn.ToString());

            // 通知业务层状态变化（由 Service 处理技能卡牌逻辑）
            OnAutoReleaseSkillStateChanged?.Invoke(isOn);

            LogTool.Log($"[BattleTimeViewModel] 自动释放技能状态: {isOn}");
        }

        /// <summary>
        /// 设置自动释放技能状态（由外部调用，如手动点技能取消自动）
        /// </summary>
        public void SetAutoReleaseSkillState(bool isOn)
        {
            Global.AutoReleaseSkillState.Value = isOn;
            IsAutoReleaseSkill.Value = isOn;
            PlayerPrefs.SetString("AutoReleaseSkillState", isOn.ToString());
            OnAutoReleaseSkillStateChanged?.Invoke(isOn);
        }

        #endregion

        #region 关卡信息初始化

        /// <summary>
        /// 初始化关卡信息显示
        /// </summary>
        private void InitializeStageInfo()
        {
            // 1. 初始化关卡名称
            InitializeStageTitle();

            // 2. 初始化波数显示
            InitializeCityLevelText();

            // 3. 初始化伤害显示
            InitializeTotalDamageText();
        }

        /// <summary>
        /// 初始化关卡名称
        /// </summary>
        private void InitializeStageTitle()
        {
            var stageConfig = DataConfigManager.Tables.TbStageBattleConfig
                .GetOrDefault(Global.StageId.Value);
            StageTitle.Value = stageConfig?.StageName ?? "";
            StageTitleColor1.Value = stageConfig?.StageNameColor1;
            StageTitleColor2.Value = stageConfig?.StageNameColor2;
        }

        /// <summary>
        /// 初始化波数/天数显示
        /// </summary>
        private void InitializeCityLevelText()
        {
            var monsterGenConfig = DataConfigManager.Tables.TbMonsterGenConfig
                .GetOrDefault(Global.StageId.Value);
            int totalDays = monsterGenConfig?.StageList?.Count ?? 0;

            // 初始值
            CityLevelText.Value = $"({Global.Date.Value}/{totalDays})";

            // 监听日期变化 - 使用弱引用避免内存泄漏
            Global.Date.RegisterWithInitValue(date =>
            {
                CityLevelText.Value = $"({date}/{totalDays})";
            });
        }

        /// <summary>
        /// 初始化总伤害显示
        /// </summary>
        private void InitializeTotalDamageText()
        {
            var stageConfig = DataConfigManager.Tables.TbStageBattleConfig
                .GetOrDefault(Global.StageId.Value);

            if (stageConfig == null)
            {
                ShowTotalDamage.Value = false;
                return;
            }

            switch (stageConfig.StageType)
            {
                case StageType.GreaterRift:
                    // Boss挑战关卡 - 显示特殊描述
                    /*ShowTotalDamage.Value = true;
                    var monsterEntries = GamePlayManager.Default?.MonsterEntries;
                    if (monsterEntries != null && monsterEntries.Count > 0)
                    {
                        var value = "";
                        foreach (var entry in monsterEntries)
                        {
                            value += entry.Desc;
                        }
                        TotalDamageText.Value = value;
                    }*/
                    break;

                case StageType.ChallengeBoss:
                    // 挑战Boss关卡 - 显示实时伤害
                    ShowTotalDamage.Value = true;
                    TotalDamageText.Value = $"伤害: {Global.TotalDamage.Value}";
                    Global.TotalDamage.RegisterWithInitValue(damage =>
                    {
                        TotalDamageText.Value = $"伤害: {damage}";
                    });
                    break;

                default:
                    // 普通关卡 - 隐藏
                    ShowTotalDamage.Value = false;
                    break;
            }
        }

        #endregion

        /// <summary>
        /// 初始化自动释放技能状态：每次开局默认开启自动战斗
        /// </summary>
        public void InitializeAutoReleaseSkill()
        {
            CanAutoReleaseSkill.Value = true;
            // 若上一局结束时仍为 true，强制制造一次 false→true，确保 GameLand 等监听一定收到开启事件
            if (Global.AutoReleaseSkillState.Value)
            {
                Global.AutoReleaseSkillState.SetValueWithoutEvent(false);
            }
            SetAutoReleaseSkillState(true);
            LogTool.Log("[BattleTimeViewModel] 开局默认开启自动战斗");
        }

       

        #region 公共方法

        /// <summary>
        /// 更新游戏时间
        /// </summary>
        public void UpdateTime(float deltaTime)
        {
            if (!IsPaused.Value && mTimeService != null)
            {
                mTimeService.UpdateTime(deltaTime);
                StartTime.Value = mTimeService.CurrentTime;
            }
        }

        /// <summary>
        /// 获取当前倍速倍数
        /// </summary>
        public float GetSpeedMultiplier()
        {
            return IsX2Speed.Value ? SPEED_X2 : SPEED_X1;
        }

        /// <summary>
        /// 重置时间
        /// </summary>
        public void ResetTime()
        {
            StartTime.Value = 0f;
            IsX2Speed.Value = false;
            SpeedText.Value = "x1";
            IsPaused.Value = false;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 将秒数转换为 MM:SS 格式
        /// </summary>
        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            int secs = Mathf.FloorToInt(seconds % 60);
            return string.Format("{0:D2}:{1:D2}", minutes, secs);
        }

        #endregion

        #region 事件

        public System.Action OnPauseRequested;
        public System.Action OnResumeRequested;
        public System.Action<float> OnSpeedChanged;
        public System.Action<bool> OnAutoReleaseSkillStateChanged;
        public System.Action<string> OnAutoReleaseSkillFailed;

        #endregion
    }
}
