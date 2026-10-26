using System;
using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.Manager;
using FusionContinent.Model;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent.Battle
{
    /// <summary>
    /// 统一管理单个技能卡牌的 UI 状态和交互逻辑
    /// </summary>
    public class SkillCardInfoHandler
    {
        private BattleCardGroup _card;
        private GameObject _cardInfo;
        private int _cardIndex;
        private CardSlotLongClick _longClick;
        private TbSpecialEffectsConfig _specialEffectsConfig;

        private Canvas _canvas;

        #region 公开属性

        public int CardId => _card?.CardId ?? 0;
        public int CardIndex => _cardIndex;
        public GameObject CardInfoObj => _cardInfo;
        public BattleCardGroup Card => _card;

        private bool IsCardInfoAlive => _cardInfo != null;

        #endregion

        #region 生命周期

        public void Initialize(BattleCardGroup card, GameObject cardInfo, int index)
        {
            _card = card;
            _cardInfo = cardInfo;
            _cardIndex = index;
            _longClick = cardInfo.GetComponent<CardSlotLongClick>();
            _canvas = cardInfo.GetComponent<Canvas>();
            _specialEffectsConfig = DataConfigManager.Tables.TbSpecialEffectsConfig;

            BindUnlockState();
        }

        private void BindUnlockState()
        {
            _card.IsUnlock.RegisterWithInitValue(isUnlock =>
            {
                var maskNum = Convert.ToByte(isUnlock ? 255 : 196);
                var cardImage = _cardInfo.GetComponent<Image>();
                if (cardImage != null)
                {
                    var currentCardColor = cardImage.color;
                    cardImage.color = new Color32(maskNum, maskNum, maskNum, (byte)(currentCardColor.a * 255));
                }

                var painting = _cardInfo.transform.Find("Canvas/Mask/Painting");
                if (painting != null)
                {
                    var paintingImage = painting.GetComponent<Image>();
                    if (paintingImage != null)
                    {
                        var currentPaintingColor = paintingImage.color;
                        paintingImage.color = new Color32(maskNum, maskNum, maskNum, (byte)(currentPaintingColor.a * 255));
                    }
                }

                _cardInfo.transform.Find("Canvas/Lock").gameObject.SetActive(!isUnlock);

                var level = _cardInfo.transform.Find("Canvas/Level");
                level.GetComponent<Image>().enabled = isUnlock;

                var cardCfg = _card?.CardBaseCfg;
                level.transform.Find("LevelText").GetComponent<Text>().text =
                    isUnlock ? $"LV.{_card.CardLevel}" : $"{cardCfg?.UnLockLevel ?? 0}级解锁";

                // 技能拖拽状态
                var specialCfg = _specialEffectsConfig?.GetOrDefault(_card?.CardId ?? 0);
                if (Global.AutoReleaseSkillState.Value == false)
                    _longClick.dragState = isUnlock && (specialCfg?.PassiveSkill ?? 1) == 0
                        ? DragState.GamePlay
                        : DragState.DragFalse;
            }).UnRegisterWhenGameObjectDestroyed(_cardInfo);
        }

        /// <summary>
        /// 释放时调用，移除所有监听
        /// </summary>
        public void OnDestroy()
        {
            // 若有需要清理的内容可在此处处理
        }

        #endregion

        #region 冷却管理

        /// <summary>
        /// 进入冷却状态，隐藏 ready 特效，禁止拖拽
        /// </summary>
        public void StartCooldown(bool hasCharging = false)
        {
            if (!IsCardInfoAlive) return;

            var cooling = _cardInfo.transform.Find("Canvas/cooling");
            if (cooling == null) return;

            cooling.gameObject.SetActive(true);
            cooling.GetComponent<Image>().fillAmount = 1;

            // 显示时间文本
            var secondObj = _cardInfo.transform.Find("Canvas/Time/Second");
            var msObj = _cardInfo.transform.Find("Canvas/Time/Millisecond");
            if (secondObj != null) secondObj.localScale = Vector3.one;
            if (msObj != null) msObj.localScale = Vector3.one;

            _longClick.dragState = hasCharging ? DragState.GamePlay : DragState.DragFalse;
        }

        private void SetCooldownTimeVisible(bool visible)
        {
            var secondObj = _cardInfo.transform.Find("Canvas/Time/Second");
            var msObj = _cardInfo.transform.Find("Canvas/Time/Millisecond");
            if (secondObj != null)
            {
                secondObj.localScale = visible ? Vector3.one : Vector3.zero;
            }

            if (msObj != null)
            {
                msObj.localScale = visible ? Vector3.one : Vector3.zero;
            }
        }

        /// <summary>
        /// 实时更新冷却 UI（fillAmount + 时间文字）
        /// </summary>
        public void UpdateCooldownUI(float remainTime, float totalTime)
        {
            if (!IsCardInfoAlive) return;

            var isCooling = totalTime > 0f && remainTime > 0f;
            var cooling = _cardInfo.transform.Find("Canvas/cooling");
            if (cooling != null)
            {
                cooling.gameObject.SetActive(isCooling);
                if (isCooling)
                {
                    cooling.GetComponent<Image>().fillAmount = Mathf.Clamp01(remainTime / totalTime);
                }
            }

            if (!isCooling)
            {
                SetCooldownTimeVisible(false);
                return;
            }

            var second = Mathf.FloorToInt(remainTime);
            var ms = ((int)(remainTime * 10)) % 10;

            SetCooldownTimeVisible(true);
            var secondObj = _cardInfo.transform.Find("Canvas/Time/Second");
            var msObj = _cardInfo.transform.Find("Canvas/Time/Millisecond");
            if (secondObj != null)
            {
                secondObj.GetComponent<TMP_Text>().text = second.ToString();
            }

            if (msObj != null)
            {
                msObj.GetComponent<TMP_Text>().text = "." + ms;
            }
        }

        /// <summary>
        /// 冷却完成回调，恢复 ready 特效和拖拽状态
        /// </summary>
        public void OnCoolingComplete(float cooldownTime)
        {
            if (!IsCardInfoAlive) return;

            // 清除冷却 UI
            _cardInfo.transform.Find("Canvas/cooling").GetComponent<Image>().fillAmount = 0;
            _cardInfo.transform.Find("Canvas/Time/Second").GetComponent<TMP_Text>().text = "0";
            _cardInfo.transform.Find("Canvas/Time/Millisecond").GetComponent<TMP_Text>().text = ".0";
            _cardInfo.transform.Find("Canvas/Time/Second").localScale = Vector3.zero;
            _cardInfo.transform.Find("Canvas/Time/Millisecond").localScale = Vector3.zero;

            _longClick.GameWaitOver = true;

            var specialCfg = _specialEffectsConfig?.GetOrDefault(_card?.CardId ?? 0);
            if (specialCfg != null)
            {
                var specialCardLvCfg = specialCfg.LevelPropertyList[_card.CardLevel - 1];

                // 充能逻辑
                if (specialCardLvCfg.ChargingStatus.Count > 0)
                {
                    var chargingStatus = specialCardLvCfg.ChargingStatus.First();
                    switch (chargingStatus.Type)
                    {
                        case ChargingStatusType.CdTime:
                            // 充能由外部 GameLand 控制
                            break;
                        case ChargingStatusType.EffectiveInstantly:
                            break;
                    }
                }
            }

            if (!Global.AutoReleaseSkillState.Value)
            {
                SetReadyEffect();
                _longClick.dragState = DragState.GamePlay;
            }
            else
            {
                SetReadyEffect();
                DOTween.To(v => { }, 0, 0, 0.5f).onComplete += () =>
                {
                    if (!IsCardInfoAlive) return;
                    _cardInfo.transform.Find("Canvas/kapaizhunbei_02").localScale = Vector3.zero;
                    _cardInfo.transform.Find("Canvas/kapaizhunbei_01").localScale = Vector3.zero;
                };
            }
        }

        /// <summary>
        /// 冷却完成（外部控制版本）
        /// </summary>
        public void OnCoolingComplete_External(bool autoMode)
        {
            if (!IsCardInfoAlive) return;

            var cooling = _cardInfo.transform.Find("Canvas/cooling");
            if (cooling != null)
            {
                cooling.GetComponent<Image>().fillAmount = 0;
                cooling.gameObject.SetActive(false);
            }

            var secondObj = _cardInfo.transform.Find("Canvas/Time/Second");
            var msObj = _cardInfo.transform.Find("Canvas/Time/Millisecond");
            if (secondObj != null)
            {
                secondObj.GetComponent<TMP_Text>().text = "0";
                secondObj.localScale = Vector3.zero;
            }
            if (msObj != null)
            {
                msObj.GetComponent<TMP_Text>().text = ".0";
                msObj.localScale = Vector3.zero;
            }

            _longClick.GameWaitOver = true;

            if (!autoMode)
            {
                SetReadyEffect();
                _longClick.dragState = DragState.GamePlay;
            }
            else
            {
                SetReadyEffect();
                DOTween.To(v => { }, 0, 0, 0.5f).onComplete += () =>
                {
                    if (!IsCardInfoAlive) return;
                    _cardInfo.transform.Find("Canvas/kapaizhunbei_02").localScale = Vector3.zero;
                    _cardInfo.transform.Find("Canvas/kapaizhunbei_01").localScale = Vector3.zero;
                };
            }
        }

        #endregion

        #region 充能管理

        /// <summary>
        /// 更新充能次数显示
        /// </summary>
        public void UpdateChargingUI(int chargeNum)
        {
            if (!IsCardInfoAlive) return;

            var chargeObj = _cardInfo.transform.Find("Canvas/ChargingNum");
            chargeObj.gameObject.SetActive(true);
            chargeObj.GetComponent<Text>().text = chargeNum.ToString();
        }

        /// <summary>
        /// 更新充能 UI（带操作时长）
        /// </summary>
        public void UpdateChargingUI_WithDuration(int chargeNum, int duration)
        {
            if (!IsCardInfoAlive) return;

            var chargeObj = _cardInfo.transform.Find("Canvas/ChargingNum");
            chargeObj.gameObject.SetActive(true);
            chargeObj.GetComponent<Text>().text = chargeNum.ToString() + $" / {duration}";
        }

        #endregion

        #region 拖拽状态

        /// <summary>
        /// 设置拖拽状态
        /// </summary>
        public void SetDragState(DragState state)
        {
            _longClick.dragState = state;
        }

        /// <summary>
        /// 强制重置位置（正在拖拽时）
        /// </summary>
        public void ForceResetPosition()
        {
            _longClick.GameWaitOver = false;
            if (_longClick.IsDragging)
            {
                _longClick.ForceResetPosition();
            }
        }

        #endregion

        #region Ready 特效

        /// <summary>
        /// 显示卡牌就绪特效
        /// </summary>
        public void SetReadyEffect()
        {
            if (!IsCardInfoAlive) return;

            var effect = _cardInfo.transform.Find("Canvas/kapaizhunbei_01/eff_kpzb_01").gameObject;
            effect.SetActive(false);
            effect.SetActive(true);
        }

        #endregion

        #region 自动释放模式

        /// <summary>
        /// 自动释放模式切换
        /// </summary>
        public void SetAutoMode(bool isOn)
        {
            if (!IsCardInfoAlive) return;

            if (isOn)
            {
                _longClick.dragState = DragState.DragFalse;
                DOTween.To(v => { }, 0, 0, 0.5f).onComplete += () =>
                {
                    if (!IsCardInfoAlive) return;
                    _cardInfo.transform.Find("Canvas/kapaizhunbei_02").localScale = Vector3.zero;
                    _cardInfo.transform.Find("Canvas/kapaizhunbei_01").localScale = Vector3.zero;
                };
            }
            else
            {
                var specialCfg = _specialEffectsConfig?.GetOrDefault(_card?.CardId ?? 0);
                if (_card?.IsUnlock.Value == true && (specialCfg?.PassiveSkill ?? 1) == 0)
                    _longClick.dragState = DragState.GamePlay;
            }
        }

        #endregion

        #region 被动技能

        /// <summary>
        /// 是否为被动技能
        /// </summary>
        public bool IsPassiveSkill()
        {
            var specialCfg = _specialEffectsConfig?.GetOrDefault(_card?.CardId ?? 0);
            return (specialCfg?.PassiveSkill ?? 0) == 1;
        }

        #endregion
    }
}
