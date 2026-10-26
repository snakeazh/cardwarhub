using Config;
using DG.Tweening;
using FusionContinent;
using QFramework;
using UnityEngine;
using Unity.Profiling;

namespace Manager
{
    /// <summary>
    /// 连续释放技能计数器
    /// 功能：
    /// 1. 监听主动技能释放，计数器+1（最多6层）
    /// 2. 每层提供0.2倍伤害加成（20%）
    /// 3. 3秒内未释放技能，自动重置为0
    /// 4. 玩家与对侧（AI）分开计数：玩家写全局（UI/伤害用），AI 走独立计数器，互不影响
    /// </summary>
    public class ConsecutiveSkillReleaseCounter : MonoBehaviour
    {
        public static ConsecutiveSkillReleaseCounter Instance;
        
        
        private GameBattleConst gameBattleConst;
        private int _comboCount = 0;
        private int _opponentComboCount = 0;
        private Tween _resetTween;
        private Tween _opponentResetTween;
        
        /// <summary>
        /// 当前连击数（只读）
        /// </summary>
        public int ComboCount => _comboCount;
        
        /// <summary>
        /// 对侧（AI）连击数（只读）：与玩家分开计数，不影响玩家 UI 与伤害。
        /// </summary>
        public int OpponentComboCount => _opponentComboCount;
        
        /// <summary>
        /// 按战斗方取当前连击数：对侧走独立计数器，玩家读全局（UI 绑定 Global.ConsecutiveSkillReleaseCount）。
        /// </summary>
        public static int GetComboCount(BattleSide side)
        {
            if (side == BattleSide.Opponent)
            {
                return Instance != null ? Instance.OpponentComboCount : 0;
            }
            
            return Global.ConsecutiveSkillReleaseCount.Value;
        }
        
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            
            Instance = this;
            gameBattleConst = DataConfigManager.Tables.TbGameBattleConst.Data;
        }
        
        /// <summary>
        /// 技能释放时调用此方法
        /// 功能：
        /// 1. 连击数+1（最多6层）
        /// 2. 取消之前的3秒计时器
        /// 3. 启动新的3秒计时器
        /// 4. 更新全局属性
        /// </summary>
        public void OnSkillReleased()
        {
            using (BattleSkillProfilerMarkers.Begin(BattleSkillProfilerMarkers.CounterUpdate))
            {
                _comboCount++;
                // if (_comboCount > 6)
                //     _comboCount = 6;
            
                // TypeEventSystem.Global.Send<GameLandUI.ComboEvent>();

                UpdateGlobalProperties();

                ResetAfterDelay();
            }
        }
        
        /// <summary>
        /// 对侧（AI）释放技能时调用：单独计数并独立重置，不写入玩家全局连击（不影响玩家 UI / 伤害）。
        /// </summary>
        public void OnOpponentSkillReleased()
        {
            _opponentComboCount++;
            
            _opponentResetTween?.Kill();
            _opponentResetTween = DOTween.To(v => { }, 0, 1, gameBattleConst.ComboDuration)
                .OnComplete(() =>
                {
                    _opponentComboCount = 0;
                });
        }
        
        /// <summary>
        /// gameBattleConst.ComboDuration秒延迟后重置连击数
        /// 使用 DOTween 实现，支持取消
        /// </summary>
        private void ResetAfterDelay()
        {
            _resetTween?.Kill();
            _resetTween = DOTween.To(v => { }, 0, 1, gameBattleConst.ComboDuration)
                .OnComplete(() =>
                {
                    // TypeEventSystem.Global.Send<GameLandUI.ComboEvent>();
                    _comboCount = 0;
                    UpdateGlobalProperties();
                });
        }
        
        /// <summary>
        /// 更新全局属性
        /// 1. 更新连击数
        /// 2. 更新伤害加成（连击数 * 0.2）
        /// 3. 触发条件系统（可选）
        /// </summary>  
        private void UpdateGlobalProperties()
        {
            Global.ConsecutiveSkillReleaseCount.Value = _comboCount;
            Global.ConsecutiveSkillReleaseDamageBonus.Value = _comboCount * 0.2f;
        }
        
        /// <summary>
        /// 重置连击计数器
        /// 场景：战斗结束、场景切换等
        /// </summary>
        public void Reset()
        {
            _resetTween?.Kill();
            _opponentResetTween?.Kill();
            
            _comboCount = 0;
            _opponentComboCount = 0;
            UpdateGlobalProperties();
        }
        
        private void OnDestroy()
        {
            _resetTween?.Kill();
            _opponentResetTween?.Kill();
        }
    }
}