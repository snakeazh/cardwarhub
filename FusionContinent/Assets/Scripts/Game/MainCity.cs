using System.Collections;
using System.Collections.Generic;
using Config;
using QFramework;
using UnityEngine;
using DG.Tweening;

namespace FusionContinent
{
    public class MainCity : MonoBehaviour
    {
        public BindableProperty<int> _mainCityHp = new BindableProperty<int>(0);
        // public BindableProperty<int> _maxHp = new BindableProperty<int>(0);
        private int lastHp;
        private int lastShield;
        private BindableProperty<int> _currentShield = new BindableProperty<int>(0);
        private BindableProperty<float> _counterattack = new BindableProperty<float>(0);// 反伤

        private MonsterCard _monsterCard;
        private int damageBackValue = 0;

        // 用于存储当前活动效果的字典
        private Dictionary<string, float> _activeEffects = new Dictionary<string, float>();

        void Start()
        {
            // 初始化
            _mainCityHp.Register(hp =>
            {
                RefreshShow();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
            _currentShield.Register(hp =>
            {
                RefreshShow();
            }).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void RefreshShow()
        {
            if (_mainCityHp.Value >= Global.MaxCityHp.Value)
            {
                _mainCityHp.Value = Global.MaxCityHp.Value;
            }

            DamageEffect(lastHp - _mainCityHp.Value + lastShield - _currentShield.Value);
            lastHp = _mainCityHp.Value;
            lastShield = _currentShield.Value;

            if (_mainCityHp.Value <= 0)
            {
                GamePlayManager.Default.GameFailure();
                _mainCityHp.Value = 0;
            }
        }

        public void InitData()
        {
            _mainCityHp.Value = Global.MaxCityHp.Value;
            _currentShield.Value += Global.strShield.Value;
            lastHp = Global.MaxCityHp.Value;
            lastShield = _currentShield.Value;
        }

        public void TakeDamage(int atk, MonsterCard monsterCard)
        {
            monsterCard.NotifyMechanicsDealDamage(ref atk);

            // 对场上战士进行受击
            if (monsterCard.targetSolidierCard != null && !monsterCard.targetSolidierCard.isDie)
            {
                var soldier = monsterCard.targetSolidierCard;
                if (soldier.CanReceiveIncomingDamage(atk))
                {
                    var result = DamageContextBuilder.CalculateIncoming(
                        soldier, monsterCard, TakeDamageType.PhysicalDamage, atk);
                    atk = soldier.TakeDamage(result);
                }
            }

            //GamePlayManager.Default.VillageSoldierTaskDamage(atk, monsterCard);
            return;
            _monsterCard = monsterCard;
            damageBackValue = 0;

            // 外部的宝石反伤
            if (Global.GemImmuneDamageCount > 0)
            {
                damageBackValue += Mathf.CeilToInt(atk * Global.GemCounterattack);
                Global.GemImmuneDamageCount--;
                atk = 0;
                // _monsterCard.TakeDamage(damageBackValue); // 对攻击者造成反伤
            }
            // 内部的主城反伤技能
            if (_counterattack.Value > 0)
            {
                damageBackValue += Mathf.CeilToInt(atk * _counterattack.Value);
            }
            if (_monsterCard.enabled != false)
                try
                {
                    // _monsterCard.TakeDamage(damageBackValue, atk); // 对攻击者造成反伤
                }
                catch
                {

                }

            if (atk <= 0)
            {
                return;
            }
            // 检查是否有减伤效果
            if (_activeEffects.ContainsKey("DamageReduction") && _activeEffects["DamageReduction"] > 0)
            {
                atk = Mathf.CeilToInt(atk * (1 - _activeEffects["DamageReduction"])); // 应用减伤效果并向上取整
            }

            atk -= Global.CityDefense.Value + Global.GemMainCityDefense;

            if (_currentShield.Value > 0)
            {
                // 先扣除护盾
                int damageToShield = Mathf.Min(atk, _currentShield.Value);
                _currentShield.Value -= damageToShield;
                atk -= Mathf.CeilToInt(damageToShield); // 向上取整护盾吸收的伤害
            }
            if (atk > 0)
            {
                _mainCityHp.Value -= atk;

                // 检查是否有反伤效果并且在持续时间内
                if (_activeEffects.ContainsKey("DamageBack") && _activeEffects["DamageBack"] > 0 && _monsterCard != null)
                {
                    damageBackValue += Mathf.CeilToInt(atk * _activeEffects["DamageBack"]);
                    // _monsterCard.TakeDamage(damageBackValue); // 对攻击者造成反伤
                }
            }
        }
        
        private IEnumerator DisableAfterDelay(ItemGenericObjectPool pool, GameObject damageObject, float delay)
        {
            // 等待指定的时间
            yield return new WaitForSeconds(delay);

            // 检查对象是否仍然有效
            if (damageObject != null)
            {
                pool.Return(damageObject);
            }
        }

        private void DamageEffect(int damageValue)
        {
            var damageObj = GamePlayManager.Default._resLoader.Get<GameObject>("CityDamage").Instantiate();
            float randomX = Random.Range(-50f, 50f);
            float randomY = Random.Range(-50f, 50f);
            var anchor = GameLand.Default.wall.transform.position;
            damageObj.transform.position = anchor + new Vector3(randomX / 100, randomY / 100, 0);
            damageObj.transform.SetParent(GameLand.Default.DamageParent);
            damageObj.LocalScale(new Vector3(0.5f, 0.5f, 0.5f));
            damageObj.GetComponent<CityDamageFun>().SetData(damageValue);
        }

        // 根据配置应用主城技能效果
        public void ApplySkillEffects(int skillId)
        {
            var config = DataConfigManager.Tables.TbMainCityBattleConfig.GetOrDefault(skillId);

            if (config.LossHpRecovery.Count > 0)
                ApplyRecoveryEffect("LossHpRecovery", config.LossHpRecovery);

            if (config.MaxHpRecovery.Count > 0)
                ApplyRecoveryEffect("MaxHpRecovery", config.MaxHpRecovery);

            if (config.MaxHpShield.Count > 0)
                ApplyShieldEffect(config.MaxHpShield);

            if (config.DamageReduction.Count > 0)
                ApplyDamageReductionEffect(config.DamageReduction);

            if (config.DamageBack.Count > 0)
                ApplyDamageBackEffect(config.DamageBack);
        }

        private void ApplyRecoveryEffect(string type, List<PercentTime> recoveryData)
        {
            float percentage = recoveryData[0].Percent;
            float duration = recoveryData[0].Time;

            if (type == "LossHpRecovery")
            {
                float hpToRecover = (Global.MaxCityHp.Value - _mainCityHp.Value) * percentage;

                if (duration > 0)
                {
                    ApplyTimedEffect(duration, () => _mainCityHp.Value += Mathf.CeilToInt(hpToRecover / duration));
                }
                else
                {
                    // 如果 duration 为 0，直接一次性恢复
                    _mainCityHp.Value += Mathf.CeilToInt(hpToRecover);
                }
            }
            else if (type == "MaxHpRecovery")
            {
                float hpToRecover = Global.MaxCityHp.Value * percentage;

                if (duration > 0)
                {
                    ApplyTimedEffect(duration, () =>
                    {
                        // 检查是否已经达到或超过上限
                        if (_mainCityHp.Value < Global.MaxCityHp.Value)
                        {
                            // 计算每次恢复的量
                            int recoveryPerTick = Mathf.CeilToInt(hpToRecover / duration);

                            // 确保恢复后不会超过上限
                            _mainCityHp.Value = Mathf.Min(_mainCityHp.Value + recoveryPerTick, Global.MaxCityHp.Value);
                        }
                    });
                }
                else
                {
                    // 如果 duration 为 0，直接一次性恢复
                    _mainCityHp.Value = Mathf.Min(_mainCityHp.Value + Mathf.CeilToInt(hpToRecover), Global.MaxCityHp.Value);
                }
            }
        }

        private void ApplyShieldEffect(List<PercentTime> shieldData)
        {
            float percentage = shieldData[0].Percent;
            float duration = shieldData[0].Time;
            float shieldAmount = Global.MaxCityHp.Value * percentage;

            if (duration == 0)
            {
                _currentShield.Value += Mathf.CeilToInt(shieldAmount); // 向上取整护盾值
            }
            else
            {
                DOTween.To(() => _currentShield.Value, x => _currentShield.Value = x, _currentShield.Value + Mathf.CeilToInt(shieldAmount), duration);
            }

        }

        private void ApplyDamageReductionEffect(List<PercentTime> reductionData)
        {
            float percentage = reductionData[0].Percent;
            float duration = reductionData[0].Time;

            // 激活减伤效果
            _activeEffects["DamageReduction"] = percentage;

            // 设置减伤效果的持续时间
            DOTween.To(() => _activeEffects["DamageReduction"], x => _activeEffects["DamageReduction"] = x, percentage, duration)
                .OnComplete(() =>
                {
                    // 持续时间结束后将减伤效果清零
                    _activeEffects["DamageReduction"] = 0;
                });
        }

        private void ApplyDamageBackEffect(List<PercentTime> backData)
        {
            float percentage = backData[0].Percent;
            float duration = backData[0].Time;

            // 激活反伤效果
            _activeEffects["DamageBack"] = percentage;

            // 设置反伤效果的持续时间
            DOTween.To(() => _activeEffects["DamageBack"], x => _activeEffects["DamageBack"] = x, percentage, duration)
                .OnComplete(() =>
                {
                    // 持续时间结束后将反伤效果清零
                    _activeEffects["DamageBack"] = 0;
                });
        }

        private void ApplyTimedEffect(float duration, System.Action action)
        {
            if (duration == 0)
            {
                action.Invoke();
            }
            else
            {
                int totalCalls = Mathf.CeilToInt(duration); // 计算总调用次数

                DOTween.Sequence()
                    .AppendInterval(1f) // 每秒调用一次
                    .AppendCallback(action.Invoke)
                    .SetLoops(totalCalls, LoopType.Restart);
            }
        }

        public void EveryDayHpRestoration()
        {
            float hpToRecover = Global.MaxCityHp.Value * Global.GemEveryDayRestoration;
            _mainCityHp.Value += Mathf.CeilToInt(hpToRecover);

        }

        public void KillEliteRegenFun()
        {
            float hpToRecover = Global.MaxCityHp.Value * Global.GemKillEliteRegen;
            _mainCityHp.Value += Mathf.CeilToInt(hpToRecover);

        }

        /**
         * 增加主城最大血量
         */
        public void AddMainCityMaxHp(int hp)
        {
            Global.MaxCityHp.Value += hp;
            _mainCityHp.Value += hp;
            // LogTool.Log(">>>> 主城最大生命值增加：hp:" + _mainCityHp.Value + ", maxHp: " + Global.MaxCityHp.Value);
            RefreshShow();
        }
        
        /**
         * 增加主城血量
         */
        public void AddMainCityHp(int hp)
        {
            if (hp == 0 || _mainCityHp.Value >= Global.MaxCityHp.Value) return;
            if (_mainCityHp.Value <= 0)
            {
                return;
            }

            _mainCityHp.Value += hp;
            if (_mainCityHp.Value > Global.MaxCityHp.Value)
            {
                _mainCityHp.Value = Global.MaxCityHp.Value;
            }

            // LogTool.Log(">>>> 主城生命值增加：hp:" + _mainCityHp.Value + ", maxHp: " + Global.MaxCityHp.Value);
            RefreshShow();
        }

        /**
         * 增加主城护盾
         */
        public void AddMainCityShield(int shield)
        {
            _currentShield.Value += shield;
            LogTool.Log(">>>> 主城护盾增加: " + _currentShield.Value);
            RefreshShow();
        }

        /**
         * 增加主城反击伤害
         */
        public void AddMainCityCounterattack(float counterattack)
        {
            _counterattack.Value += counterattack;
            LogTool.Log(">>>> 主城反击伤害增加: " + _counterattack.Value);
        }

    }
}
