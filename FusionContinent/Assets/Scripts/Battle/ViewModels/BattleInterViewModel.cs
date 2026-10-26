using UIFramework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Config;
using Cysharp.Threading.Tasks;
using FusionContinent;
using FusionContinent.Model;
using UIFramework.Services;

namespace Game.Battle
{
    /// <summary>
    /// 战斗交互 ViewModel
    /// 管理金币显示、购买技能/经验/流派按钮
    /// </summary>
    public class BattleInterViewModel : ViewModel
    {
        #region 属性
        /// <summary>
        /// 当前金币数量
        /// </summary>
        public BindableProperty<int> CurrentGold { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 技能购买按钮显示文本
        /// </summary>
        public BindableProperty<string> BuySkillBtnText { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 流派购买按钮显示文本
        /// </summary>
        public BindableProperty<string> BuySchoolBtnText { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 经验购买按钮显示文本
        /// </summary>
        public BindableProperty<string> BuyExpBtnText { get; } = new BindableProperty<string>("");

        /// <summary>
        /// 是否可以购买技能
        /// </summary>
        public BindableProperty<bool> CanBuySkill { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 是否可以购买经验
        /// </summary>
        public BindableProperty<bool> CanBuyExp { get; } = new BindableProperty<bool>(false);

        /// <summary>
        /// 城市当前等级
        /// </summary>
        public BindableProperty<int> CityLevel { get; } = new BindableProperty<int>(1);

        /// <summary>
        /// 城市当前经验
        /// </summary>
        public BindableProperty<int> CityExp { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 经验进度（0-1）
        /// </summary>
        public BindableProperty<float> ExpProgress { get; } = new BindableProperty<float>(0f);

        /// <summary>
        /// 连击数
        /// </summary>
        public BindableProperty<int> ComboCount { get; } = new BindableProperty<int>(0);

        /// <summary>
        /// 战斗计时文本（00:00 格式）
        /// </summary>
        public BindableProperty<string> BattleTimeText { get; } = new BindableProperty<string>("00:00");
        #endregion

        #region 命令
        public Command OnBuySkillCommand { get; private set; }
        public Command OnBuyExpCommand { get; private set; }
        #endregion

        #region 配置数据
        private int _skillBuyChooseGold;  // 技能购买基础金币
        private int _buySkillNum = 1;     // 当前技能购买次数
        private TbGameBattleConst _gameBattleConst;
        #endregion

        #region 事件
        public System.Action<int> OnGoldChanged; // 金币变化事件（用于动效）
        public System.Action<int> OnCoinGainEffect; // 金币增加动效
        #endregion

        private float _elapsedTime;
        private float _lastRealtime;
        private CancellationTokenSource _timerCts;

        public override void OnCreate()
        {
            base.OnCreate();

            // 加载配置
            _gameBattleConst = DataConfigManager.Tables.TbGameBattleConst;
            _skillBuyChooseGold = _gameBattleConst.SkillInitBuyChooseCardGoldNum;

            // 初始化命令
            OnBuySkillCommand = new Command(OnBuySkill);
            OnBuyExpCommand = new Command(OnBuyExp);

            // 初始化属性
            InitializeProperties();

            // 监听全局数据变化
            RegisterGlobalListeners();

            LogTool.Log("[BattleInterViewModel] 创建完成");
        }

        /// <summary>
        /// 初始化属性
        /// </summary>
        private void InitializeProperties()
        {
            // 初始化金币
            CurrentGold.Value = Global.GameGold.Value;
            
            // 初始化城市等级和经验
            CityLevel.Value = Global.CityLevel.Value;
            CityExp.Value = Global.CityExp.Value;
            
            // 更新按钮文本
            UpdateBuyBtnTexts();
            
            // 更新按钮状态
            UpdateCanBuyStates();
            
            // 更新经验进度
            UpdateExpProgress();
        }

        /// <summary>
        /// 注册全局数据监听
        /// </summary>
        private void RegisterGlobalListeners()
        {
            // 监听金币变化
            Global.GameGold.RegisterWithInitValue(gold =>
            {
                int oldGold = CurrentGold.Value;
                CurrentGold.Value = gold;
                
                // 触发金币变化事件（用于动效）
                OnGoldChanged?.Invoke(gold);
                
                // 触发金币增加动效
                int incNum = gold - oldGold;
                if (incNum > 0)
                {
                    OnCoinGainEffect?.Invoke(incNum);
                }
                
                // 更新按钮状态
                UpdateCanBuyStates();
            });

            // 监听城市等级变化
            Global.CityLevel.RegisterWithInitValue(level =>
            {
                CityLevel.Value = level;
                UpdateExpProgress();
                UpdateBuyBtnTexts();
                UpdateCanBuyStates();
            });

            // 监听城市经验变化
            Global.CityExp.RegisterWithInitValue(exp =>
            {
                CityExp.Value = exp;
                HandleCityExpChange(exp);
            });

            // 监听连击数变化
            Global.ConsecutiveSkillReleaseCount.RegisterWithInitValue(count =>
            {
                ComboCount.Value = count;
            });

            // 启动战斗计时
            _elapsedTime = 0f;
            _lastRealtime = Time.realtimeSinceStartup;
            _timerCts = new CancellationTokenSource();
            GamePlayManager.Default.OnPauseChanged += OnBattlePauseChanged;
            StartBattleTimerAsync(_timerCts.Token).Forget();
        }

        private bool _isBattlePaused;

        private void OnBattlePauseChanged(bool paused)
        {
            _isBattlePaused = paused;
        }

        private async UniTaskVoid StartBattleTimerAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                float now = Time.realtimeSinceStartup;
                float realDelta = now - _lastRealtime;
                _lastRealtime = now;

                if (!_isBattlePaused)
                {
                    _elapsedTime += realDelta * Global.SpeedMultiplier.Value;
                    BattleTimeText.Value = FormatBattleTime(_elapsedTime);
                }

                await UniTask.NextFrame(PlayerLoopTiming.Update);
                if (token.IsCancellationRequested) break;
            }
        }

        private static string FormatBattleTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60);
            int secs = Mathf.FloorToInt(seconds % 60);
            return $"{minutes:D2}:{secs:D2}";
        }

        public override void OnDestroy()
        {
            if (GamePlayManager.Default != null)
                GamePlayManager.Default.OnPauseChanged -= OnBattlePauseChanged;
            _timerCts?.Cancel();
            _timerCts?.Dispose();
            _timerCts = null;
            base.OnDestroy();
        }

        /// <summary>
        /// 处理城市经验变化（自动升级逻辑）
        /// </summary>
        private void HandleCityExpChange(int exp)
        {
            var cityLevelUpperLimit = GetCityLevelUpperLimit();
            if (cityLevelUpperLimit > 0 && Global.CityLevel.Value >= cityLevelUpperLimit)
            {
                UpdateExpProgress();
                return;
            }

            var currentLevel = Mathf.Max(Global.CityLevel.Value, 1);
            var requiredExp = GetRequiredCityExp(currentLevel);
            
            // 自动升级逻辑
            while (requiredExp > 0 && Global.CityExp.Value >= requiredExp && currentLevel <= cityLevelUpperLimit)
            {
                Global.CityExp.Value -= requiredExp;
                Global.CityLevel.Value++;
                currentLevel = Mathf.Max(Global.CityLevel.Value, 1);

                HandleCityLevelUpRewards(currentLevel);

                if (cityLevelUpperLimit > 0 && Global.CityLevel.Value >= cityLevelUpperLimit)
                {
                    break;
                }

                requiredExp = GetRequiredCityExp(currentLevel);
            }

            UpdateExpProgress();
        }

        /// <summary>
        /// 城市升级奖励处理（按配置决定是否新增英雄）
        /// </summary>
        private void HandleCityLevelUpRewards(int level)
        {
            var levelCfg = GetCityLevelConfig(level);
            if (levelCfg == null) return;

            if (levelCfg.IsAddHero > 0)
            {
                var addCount = Mathf.Max(levelCfg.IsAddHero, 0);
                var totalAdded = 0;
                for (int i = 0; i < addCount; i++)
                {
                    var added = GamePlayManager.Default?.TryAddHeroFromTeamByLevelUp() ?? false;
                    if (!added)
                    {
                        break;
                    }

                    totalAdded++;
                }

                if (totalAdded <= 0)
                {
                    LogTool.Log($"[BattleInterViewModel] 等级 {level} 配置了加英雄({addCount})，但当前没有可新增英雄");
                }
            }
        }

        #region 命令实现

        /// <summary>
        /// 购买技能
        /// </summary>
        private void OnBuySkill()
        {
            var consumeGold = _skillBuyChooseGold * _buySkillNum;
            if (consumeGold > Global.GameGold.Value)
            {
                FusionContinent.Toast.Show("金币不足");
                return;
            }

            _buySkillNum++;
            Global.GameGold.Value -= consumeGold;
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS, GuideType.CanBeUpgraded);

            // 更新按钮文本
            UpdateBuyBtnTexts();

            // 打开技能选择面板
            OpenSkillChoosePanel();

            LogTool.Log($"[BattleInterViewModel] 购买技能，消耗金币: {consumeGold}");
        }

        /// <summary>
        /// 打开技能选择面板
        /// </summary>
        private void OpenSkillChoosePanel()
        {
            LogTool.Log("[BattleInterViewModel] 打开技能选择面板");
            var vm = new Game.SchoolChooseCard.SchoolChooseCardViewModel();
            vm.OnCreate();
            vm.Initialize(2);
            UIFramework.UIManager.OpenPanelAsync<SchoolChooseCardPanelNew, Game.SchoolChooseCard.SchoolChooseCardViewModel>(vm, UIFramework.UILevel.PopUI).Forget();
        }

        /// <summary>
        /// 购买经验（流派）
        /// </summary>
        private void OnBuyExp()
        {
            Global.Publisher.Publish(MessagePublisher.CHANGE_GUIDE_PROGRESS,GuideType.CanBeUpSchool);
            var cityLevelUpperLimit = GetCityLevelUpperLimit();
            if (cityLevelUpperLimit > 0 && Global.CityLevel.Value >= cityLevelUpperLimit)
            {
                FusionContinent.Toast.Show("已达到最高等级");
                return;
            }

            if (!TryGetCurrentLevelNeedGold(out var consumeGold))
            {
                FusionContinent.Toast.Show("等级配置异常");
                return;
            }

            if (consumeGold > Global.GameGold.Value)
            {
                FusionContinent.Toast.Show("金币不足");
                return;
            }

            Global.GameGold.Value -= consumeGold;
            Global.CityExp.Value += 10;
            
            // 更新按钮文本
            UpdateBuyBtnTexts();
            UpdateCanBuyStates();
            LogTool.Log($"[BattleInterViewModel] 购买经验，消耗金币: {consumeGold}");
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 更新购买按钮文本
        /// </summary>
        private void UpdateBuyBtnTexts()
        {
            BuySkillBtnText.Value = $"{_skillBuyChooseGold * _buySkillNum}";
            if (TryGetCurrentLevelNeedGold(out var buyExpNeedGold))
            {
                BuyExpBtnText.Value = $"{buyExpNeedGold}";
            }
            else
            {
                BuyExpBtnText.Value = "0";
            }
        }

        /// <summary>
        /// 当前是否可以购买经验
        /// </summary>
        public bool CanBuyExpNow()
        {
            var cityLevelUpperLimit = GetCityLevelUpperLimit();
            if (!TryGetCurrentLevelNeedGold(out var buyExpNeedGold))
            {
                return false;
            }

            bool canBuyExp = CurrentGold.Value >= buyExpNeedGold;
            bool reachedMaxLevel = cityLevelUpperLimit > 0 && Global.CityLevel.Value >= cityLevelUpperLimit;
            return canBuyExp && !reachedMaxLevel;
        }

        /// <summary>
        /// 更新是否可以购买状态
        /// </summary>
        public void UpdateCanBuyStates(bool guideToLoading = false)
        {
            CanBuySkill.Value = CurrentGold.Value >= (_skillBuyChooseGold * _buySkillNum);
            if (CanBuySkill.Value)
            {
                GamePlayManager.Default?.TriggerGuide(GuideType.CanBeUpgraded); // 引导 - 买  
            }
            
            CanBuyExp.Value = CanBuyExpNow();
            if (CanBuyExp.Value)
            {
                GamePlayManager.Default?.TriggerGuide(GuideType.CanBeUpSchool); // 引导 - 买经验
            }
            
            
        }

        /// <summary>
        /// 更新经验进度
        /// </summary>
        private void UpdateExpProgress()
        {
            var currentLevel = Mathf.Max(Global.CityLevel.Value, 1);
            var requiredExp = GetRequiredCityExp(currentLevel);
            if (requiredExp > 0)
            {
                ExpProgress.Value = (float)Global.CityExp.Value / requiredExp;
            }
            else
            {
                ExpProgress.Value = 1f;
            }
        }

        /// <summary>
        /// 获取城市等级上限
        /// </summary>
        private int GetCityLevelUpperLimit()
        {
            var difficultyCfg = DataConfigManager.Tables.TbCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            return difficultyCfg?.LevelList?.Max(cfg => cfg.Level) ?? 0;
        }

        /// <summary>
        /// 获取升级所需经验
        /// </summary>
        private int GetRequiredCityExp(int level)
        {
            var difficultyCfg = DataConfigManager.Tables.TbCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            if (difficultyCfg == null || level <= 0 || level > difficultyCfg.LevelList.Count)
            {
                return 0;
            }

            return difficultyCfg.LevelList[level - 1].CityExp;
        }

        /// <summary>
        /// 获取指定等级城市配置
        /// </summary>
        private CityLevelList GetCityLevelConfig(int level)
        {
            var difficultyCfg = DataConfigManager.Tables.TbCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            if (difficultyCfg == null || level <= 0 || level > difficultyCfg.LevelList.Count)
            {
                return null;
            }

            return difficultyCfg.LevelList[level - 1];
        }

        /// <summary>
        /// 获取当前城市等级对应的购买经验金币
        /// </summary>
        private bool TryGetCurrentLevelNeedGold(out int needGold)
        {
            needGold = 0;
            var currentLevel = Mathf.Max(Global.CityLevel.Value, 1);
            var levelCfg = GetCityLevelConfig(currentLevel);
            if (levelCfg == null)
            {
                LogTool.LogError($"[BattleInterViewModel] 未找到城市等级配置，Level={currentLevel}, Difficulty={Global.GameDifficultyLevel.Value}");
                return false;
            }

            needGold = Mathf.Max(levelCfg.NeedGold, 0);
            return true;
        }

        #endregion
    }
}
