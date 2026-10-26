using System.Collections;
using Game.Battle;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Config;
using FusionContinent.Guide;
using FusionContinent.Manager;
using FusionContinent.Model;

namespace FusionContinent
{
    [PrefabPath("BattleInterPanel")]
    /// <summary>
    /// 战斗交互子界面 - 新框架版本
    /// 管理金币显示、流派按钮、连击与战斗计时
    /// 作为 BattlePanelNew 的子界面，不参与 UIManager 堆栈管理
    /// </summary>
    public class BattleInterPanelNew : UIPanel<BattleInterViewModel>
    {
        /// <summary>
        /// 作为子界面，不参与UIManager堆栈管理
        /// </summary>
        public override PanelStackMode StackMode => PanelStackMode.Unmanaged;

        // 引用 BattleInterPanel 的 UI 组件
        private BattleInterPanel mOldPanel;

        // 金币数字滚动组件
        private RollingNumberManager _goldRollingNumber;

        // 事件处理器（用于解绑）
        private System.Action<int, int> _currentGoldHandler;
        private System.Action<string, string> _buySchoolBtnTextHandler;
        private System.Action<int> _coinGainEffectHandler;
        private System.Action<int, int> _comboCountHandler;
        private System.Action<string, string> _battleTimeTextHandler;

        // Combo 相关字段
        private Image _comboImg;
        private GameObject _comboGameObject;
        private GameObject comboEf1;
        private GameObject comboEf2;
        private bool _isComboExiting;

        // 士兵觉醒技能 Spine 相关
        private GameObject _currentSpine;
        private System.Action<SoldierAwakeningSkillEvent> _soldierAwakeningHandler;

        void Awake()
        {
            // 获取 BattleInterPanel 引用
            mOldPanel = GetComponent<BattleInterPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[BattleInterPanelNew] BattleInterPanel 组件未找到，尝试在子物体中查找...");
                // 尝试在子物体中查找
                mOldPanel = GetComponentInChildren<BattleInterPanel>();
                if (mOldPanel == null)
                {
                    LogTool.LogError("[BattleInterPanelNew] 在子物体中也未找到 BattleInterPanel 组件");
                }
            }

            // 给 GoldText 挂载数字滚动组件
            if (mOldPanel != null && mOldPanel.GoldText != null)
            {
                _goldRollingNumber = mOldPanel.GoldText.GetComponent<RollingNumberManager>()
                    ?? mOldPanel.GoldText.gameObject.AddComponent<RollingNumberManager>();
            }
        }

        protected override void OnBindViewModel(BattleInterViewModel vm)
        {
            LogTool.Log($"[BattleInterPanelNew] OnBindViewModel 被调用，mOldPanel = {mOldPanel != null}");
            
            if (mOldPanel == null)
            {
                LogTool.LogError("[BattleInterPanelNew] mOldPanel 为 null，无法绑定");
                return;
            }

            if (GameLandUI.Default == null)
            {
                LogTool.LogError("[BattleInterPanelNew] GameLandUI.Default 为 null，无法绑定购买按钮");
            }
            else
            {
                GameLandUI.Default.BindBuyButtons(vm);
            }

            // 绑定属性
            BindProperties(vm);

            // 绑定事件
            BindEvents(vm);

            LogTool.Log("[BattleInterPanelNew] 绑定完成");
        }

        protected override void OnUnbindViewModel(BattleInterViewModel vm)
        {
            if (GameLandUI.Default != null)
            {
                GameLandUI.Default.UnbindBuyButtons();
            }

            // 解绑属性
            UnbindProperties(vm);

            // 解绑事件
            UnbindEvents(vm);
        }

        #region 数据绑定

        private void BindProperties(BattleInterViewModel vm)
        {
            // 金币显示绑定（使用 RollingNumberManager 实现数字滚动效果）
            _currentGoldHandler = (oldVal, newVal) =>
            {
                _goldRollingNumber?.Play(newVal);
            };
            vm.CurrentGold.OnValueChanged += _currentGoldHandler;

            // 流派购买按钮文本绑定
            _buySchoolBtnTextHandler = (oldVal, newVal) =>
            {
                UpdateBuySchoolBtnText(newVal);
            };
            vm.BuySchoolBtnText.OnValueChanged += _buySchoolBtnTextHandler;

            // 连击数绑定
            _comboCountHandler = (oldVal, newVal) =>
            {
                UpdateComboUI(newVal);
            };
            vm.ComboCount.OnValueChanged += _comboCountHandler;

            // 战斗计时绑定
            _battleTimeTextHandler = (oldVal, newVal) =>
            {
                if (mOldPanel?.TimerNum != null)
                {
                    mOldPanel.TimerNum.text = newVal;
                }
            };
            vm.BattleTimeText.OnValueChanged += _battleTimeTextHandler;

            // 初始化显示
            InitializeUI(vm);
        }

        private void UnbindProperties(BattleInterViewModel vm)
        {
            if (_currentGoldHandler != null)
            {
                vm.CurrentGold.OnValueChanged -= _currentGoldHandler;
                _currentGoldHandler = null;
            }

            if (_buySchoolBtnTextHandler != null)
            {
                vm.BuySchoolBtnText.OnValueChanged -= _buySchoolBtnTextHandler;
                _buySchoolBtnTextHandler = null;
            }

            if (_comboCountHandler != null)
            {
                vm.ComboCount.OnValueChanged -= _comboCountHandler;
                _comboCountHandler = null;
            }

            if (_battleTimeTextHandler != null)
            {
                vm.BattleTimeText.OnValueChanged -= _battleTimeTextHandler;
                _battleTimeTextHandler = null;
            }
        }

        private void InitializeUI(BattleInterViewModel vm)
        {
            LogTool.Log($"[BattleInterPanelNew] InitializeUI 开始，GoldText = {mOldPanel?.GoldText != null}, BuySchoolBtn = {mOldPanel?.BuySchoolBtn != null}");
            
            // 初始化金币显示（首次调用不滚动，仅设置文本）
            if (_goldRollingNumber != null)
            {
                _goldRollingNumber.Play(vm.CurrentGold.Value);
                LogTool.Log($"[BattleInterPanelNew] 金币显示初始化: {vm.CurrentGold.Value}");
            }
            else
            {
                LogTool.LogWarning("[BattleInterPanelNew] GoldText 或 RollingNumberManager 为 null");
            }

            GoldCoinManager.Instance.target = this.mOldPanel.GoldBar.transform;
            UpdateBuySchoolBtnText(vm.BuySchoolBtnText.Value);

            // 初始化 BuySchoolBtn（如果存在）
            if (mOldPanel?.BuySchoolBtn != null)
            {
                var numText = mOldPanel.BuySchoolBtn.transform.Find("Num")?.GetComponent<TextMeshProUGUI>();
                if (numText != null)
                {
                    numText.color = vm.CanBuyExp.Value ? Color.white : new Color32(255, 61, 61, 255);
                }
                LogTool.Log($"[BattleInterPanelNew] BuySchoolBtn 状态初始化: interactable = {vm.CanBuyExp.Value}");
            }

            // 初始化计时
            if (mOldPanel?.TimerNum != null)
            {
                mOldPanel.TimerNum.text = vm.BattleTimeText.Value;
            }

            // 初始化 Combo
            InitializeCombo();

            LogTool.Log("[BattleInterPanelNew] InitializeUI 完成");
        }

        private void InitializeCombo()
        {
            if (mOldPanel?.Combo != null)
            {
                _comboGameObject = mOldPanel.Combo.gameObject;
                _comboImg = mOldPanel.Combo.transform.Find("Image")?.GetComponent<Image>();

                // 初始时隐藏 Combo
                if (_comboGameObject != null)
                {
                    _comboGameObject.SetActive(false);
                }
            }
        }

        private void UpdateBuySchoolBtnText(string text)
        {
            if (mOldPanel?.BuySchoolBtn != null)
            {
                var numText = mOldPanel.BuySchoolBtn.transform.Find("Num")?.GetComponent<TextMeshProUGUI>();
                if (numText != null)
                {
                    numText.text = text;
                }
            }
        }

        private void UpdateComboUI(int count)
        {
            if (_comboGameObject == null || _comboImg == null) return;

            if (count == 0)
            {
                // 连击结束：先播放退出动画
                if (_isComboExiting) return;

                _isComboExiting = true;
                var startPos = mOldPanel.Combo.anchoredPosition;
                mOldPanel.Combo.DOAnchorPosX(startPos.x + 800f, 0.3f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() =>
                    {
                        _comboGameObject.SetActive(false);
                        mOldPanel.Combo.anchoredPosition = startPos;
                        _isComboExiting = false;
                    });

                // 回收特效
                RecycleComboEffects();
                return;
            }

            if (count > 1)
            {
                // 显示连击 UI（仅在退出动画未进行时才需要主动显示）
                if (!_comboGameObject.activeSelf && !_isComboExiting)
                {
                    _comboGameObject.SetActive(true);
                }

                int iconIndex = Mathf.Min(count, 6);

                // 始终更新 sprite（保持与当前连击数同步）
                _comboImg.sprite = Global.ComboIconAtlas.GetSprite($"Combo{iconIndex}");

                bool shouldPlayCombo1 = iconIndex >= 5 && comboEf1 == null;
                bool shouldPlayCombo2 = iconIndex >= 6 && comboEf2 == null;

                if (shouldPlayCombo1)
                {
                    comboEf1 = EffectManager.Instance.PlayEffect("ComboEffectSix01", _comboImg.transform.position, 0f);
                }
                if (shouldPlayCombo2)
                {
                    comboEf2 = EffectManager.Instance.PlayEffect("ComboEffectSix02", _comboImg.transform.position, 0f);
                }

                // 图标切换时播放缩放动画
                _comboImg.transform.localScale = Vector3.one * 2f;
                _comboImg.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    GuideManager.Instance.SetGuideNode(GuideType.FirstComboGuide,_comboImg.gameObject);
                    GamePlayManager.Default.TriggerGuide(GuideType.FirstComboGuide);
                });
            }
        }

        private void RecycleComboEffects()
        {
            if (comboEf1 != null)
            {
                EffectManager.Instance.RecycleEffect(comboEf1);
                comboEf1 = null;
            }
            if (comboEf2 != null)
            {
                EffectManager.Instance.RecycleEffect(comboEf2);
                comboEf2 = null;
            }
        }

        #endregion

        #region 事件绑定

        private void BindEvents(BattleInterViewModel vm)
        {
            // 金币增加动效
            _coinGainEffectHandler = (incNum) =>
            {
                PlayCoinGainEffect(incNum);
            };
            vm.OnCoinGainEffect += _coinGainEffectHandler;

            // 士兵觉醒技能事件监听
            _soldierAwakeningHandler = (evt) =>
            {
                OnSoldierAwakening(evt.SoldierId);
            };
            EventBus.Subscribe(_soldierAwakeningHandler);
            
            
            Global.Publisher.Subscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS_DONE,OnGuideDone);
        }

        private void OnGuideDone(object obj)
        {
            int value = (int)obj;

            ///流派卡点击详情引导完成后执行
            if (value == (int) GuideType.PressSchoolShowDetails)
            {
                VM.UpdateCanBuyStates();
            }
        }

        private void UnbindEvents(BattleInterViewModel vm)
        {
            if (_coinGainEffectHandler != null)
            {
                vm.OnCoinGainEffect -= _coinGainEffectHandler;
                _coinGainEffectHandler = null;
            }

            // 取消士兵觉醒技能事件监听
            if (_soldierAwakeningHandler != null)
            {
                EventBus.Unsubscribe(_soldierAwakeningHandler);
                _soldierAwakeningHandler = null;
            }
            Global.Publisher.Unsubscribe(MessagePublisher.CHANGE_GUIDE_PROGRESS_DONE,OnGuideDone);
        }

        /// <summary>
        /// 播放金币增加动效
        /// </summary>
        private void PlayCoinGainEffect(int incNum)
        {
            if (incNum <= 0) return;

            if (incNum > 10)
            {
                if (mOldPanel?.CoinGainEffect01 != null)
                {
                    mOldPanel.CoinGainEffect01.gameObject.SetActive(true);
                    DOVirtual.DelayedCall(1.5f, () =>
                    {
                        if (mOldPanel?.CoinGainEffect01 != null)
                            mOldPanel.CoinGainEffect01.gameObject.SetActive(false);
                    });
                }
            }
            else
            {
                if (mOldPanel?.CoinGainEffect02 != null)
                {
                    mOldPanel.CoinGainEffect02.gameObject.SetActive(true);
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        if (mOldPanel?.CoinGainEffect02 != null)
                            mOldPanel.CoinGainEffect02.gameObject.SetActive(false);
                    });
                }
            }
        }

        #endregion

        #region 士兵觉醒技能 Spine

        /// <summary>
        /// 士兵觉醒技能回调 - 实例化 Spine
        /// 参考 AwakeningPanelNew.SetSkinDisplay 实现
        /// </summary>
        private void OnSoldierAwakening(int soldierId)
        {
            if (mOldPanel.SkillShowSpine == null)
            {
                LogTool.LogWarning("[BattleInterPanelNew] SkillShowSpine 为空，无法显示觉醒技能 Spine");
                return;
            }
            // DataConfigManager.Tables.TbSkinConfig
            // 获取士兵配置
            var soldierCfg = DataConfigManager.Tables.TbSoldierBattleConfig.GetOrDefault(soldierId);
            if (soldierCfg == null)
            {
                LogTool.LogWarning($"[BattleInterPanelNew] 找不到士兵配置: {soldierId}");
                return;
            }

            // 获取皮肤配置（优先使用当前装备的皮肤）
            SkinConfig skinConfig = null;
            var playerCard = Global.GetCardById(soldierId);
            if (playerCard != null  && playerCard.ObjData != null && playerCard.ObjData.Skin > 0)
            {
                skinConfig = DataConfigManager.Tables.TbSkinConfig.GetOrDefault(playerCard.ObjData.Skin);
            }
            else
            {
                foreach (var skinCfg in DataConfigManager.Tables.TbSkinConfig.DataList)
                {
                    if (skinCfg.ActsOnCard== soldierId && skinCfg.UnlockItemId == null)
                    {
                        skinConfig = skinCfg;
                        break;
                    }
                }
            }
            

            // 设置皮肤显示
            SetSkinDisplay(skinConfig);
        }
        Coroutine _idleCoroutine;
        /// <summary>
        /// 设置皮肤显示 - 参考 AwakeningPanelNew.SetSkinDisplay
        /// </summary>
        private void SetSkinDisplay(SkinConfig skinConfig)
        {
            if (mOldPanel?.SkillShowSpine == null || skinConfig == null) return;

            // 清除旧的 Spine（保留 Icon 子物体）
            ClearOldSpines();
            if (_idleCoroutine != null)
            {
                StopCoroutine(_idleCoroutine);
                _idleCoroutine = null;
            }
            // this.mOldPanel.AnnaSpineEffect02.gameObject.SetActive(true);
            // 设置 Spine 动画
            if (!string.IsNullOrEmpty(skinConfig.IllustrationSpine))
            {
                // mOldPanel.SkillShowContent.DOKill(false);
                // 隐藏图标
                // mOldPanel.SkillShowContent.anchoredPosition = new Vector2(-1000f, 0f);
                // 加载新的 Spine
                GameObject skin = GameObject.Instantiate(Global.ResLoader.LoadSync<GameObject>(skinConfig.IllustrationSpine),mOldPanel.SkillShowContent);
                if (skin != null)
                {
                    // skin.transform.SetParent(mOldPanel.SkillShowContent);
                    skin.transform.localScale = Vector3.one * skinConfig.IllustrationSpineScale * 0.6f;
                    var skinRect = skin.GetComponent<RectTransform>();
                    skinRect.anchoredPosition = new Vector2(0, 0);
                    skin.transform.localRotation = Quaternion.identity;
                    skin.gameObject.SetActive(true);

                    _currentSpine = skin;

                    LogTool.Log($"[BattleInterPanelNew] 显示士兵觉醒技能 Spine: {skinConfig.IllustrationSpine}");

                    // 3秒后自动清理
                    _idleCoroutine = StartCoroutine(DelayedClearSpine(3f));
                }
                mOldPanel.SkillShowSpine.Play("LingPainting",0,0);
                // mOldPanel.SkillShowContent.DOAnchorPosX(214.5f,0.3f).SetEase(Ease.InOutBack);
            }
        }

        /// <summary>
        /// 清除旧的 Spine（保留 Icon 子物体）
        /// </summary>
        private void ClearOldSpines()
        {
            if (mOldPanel?.SkillShowContent == null) return;
            if (_currentSpine != null)
            {
                Destroy(_currentSpine);
            }
            this.mOldPanel.AnnaSpineEffect02.gameObject.SetActive(false);
            _currentSpine = null;
        }

        /// <summary>
        /// 延迟清理 Spine
        /// </summary>
        private System.Collections.IEnumerator DelayedClearSpine(float delay)
        {
            
            yield return new WaitForSeconds(delay);
            // mOldPanel.SkillShowContent.DOAnchorPosX(-1000f,0.5f).SetEase(Ease.InOutBack);
            yield return new WaitForSeconds(0.5f);
            ClearOldSpines();
            
        }

        /// <summary>
        /// 清除当前显示的 Spine
        /// </summary>
        private void ClearCurrentSpine()
        {
            ClearOldSpines();
        }

        #endregion

        protected override void OnOpen(IUIData uiData = null)
        {
            gameObject.SetActive(true);
            StartCoroutine(WaitLoadingDeActive());
        }

        public IEnumerator WaitLoadingDeActive()
        {
            do
            {
                var loadingPanel = UIManager.GetPanel<LoadingPanelNew>();
                if (loadingPanel!= null && !loadingPanel.gameObject.activeInHierarchy)
                {
                    VM?.UpdateCanBuyStates(true);
                    yield break;
                }
                yield return null;
            } while (true);
        }
        
        protected override void OnShow()
        {
            gameObject.SetActive(true);
        }

        protected override void OnHide()
        {
            gameObject.SetActive(false);
        }

        protected override void OnClose()
        {
            base.OnClose();

            // 清理 Combo 特效
            RecycleComboEffects();

            // 清理 Spine
            ClearCurrentSpine();
        }
    }
}
