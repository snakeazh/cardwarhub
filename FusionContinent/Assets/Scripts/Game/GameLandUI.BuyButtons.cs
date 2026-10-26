using System.Linq;
using Config;
using DG.Tweening;
using FusionContinent.UI.Components;
using Game.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    /// <summary>
    /// GameLandUI/pve 下 BuySkillBtn / BuyExpBtn 的绑定与刷新。
    /// 业务逻辑仍由 BattleInterViewModel 持有。
    /// </summary>
    public partial class GameLandUI
    {
        private const float BuyBtnEffectDelay = 0.5f;

        private BattleInterViewModel mBuyButtonsVm;
        private GameObject mBuySkillEffectGo;
        private GameObject mBuyExpEffectGo;
        private Tween mBuySkillStateTween;
        private Tween mBuyExpStateTween;

        /// <summary>
        /// 绑定战斗购买按钮到 BattleInterViewModel（由 BattleInterPanel 生命周期驱动）。
        /// </summary>
        public void BindBuyButtons(BattleInterViewModel vm)
        {
            UnbindBuyButtons();
            EnsureBuyButtonRefs();
            mBuyButtonsVm = vm;
            CacheBuyBtnEffects();
            BindBuyButtonListeners(vm);
            InitializeBuyButtonsUI(vm);
        }

        /// <summary>
        /// Designer 未绑定时从 pve 节点解析，避免场景序列化丢失导致空引用。
        /// </summary>
        private void EnsureBuyButtonRefs()
        {
            var pve = transform.Find("pve");
            if (pve == null)
            {
                LogTool.LogError("[GameLandUI.BuyButtons] 未找到 pve 节点");
                return;
            }

            if (BuySkillBtn == null)
            {
                var skillTf = pve.Find("BuySkillBtn");
                if (skillTf == null)
                {
                    LogTool.LogError("[GameLandUI.BuyButtons] 未找到 pve/BuySkillBtn");
                }
                else
                {
                    BuySkillBtn = skillTf.GetComponent<UIPointerClickAndLongPress>();
                    BuySkillUseGameObject = skillTf.Find("use").gameObject;
                    BuySkillNoUseGameObject = skillTf.Find("nouse").gameObject;
                }
            }

            if (BuyExpBtn == null)
            {
                var expTf = pve.Find("BuyExpBtn");
                if (expTf == null)
                {
                    LogTool.LogError("[GameLandUI.BuyButtons] 未找到 pve/BuyExpBtn");
                }
                else
                {
                    BuyExpBtn = expTf.GetComponent<UIPointerClickAndLongPress>();
                    BuyExpUseGameObject = expTf.Find("use").gameObject;
                    BuyExpNoUseGameObject = expTf.Find("nouse").gameObject;
                }
            }
        }

        public void UnbindBuyButtons()
        {
            if (mBuyButtonsVm != null)
            {
                mBuyButtonsVm.BuySkillBtnText.OnValueChanged -= OnBuySkillBtnTextChanged;
                mBuyButtonsVm.BuyExpBtnText.OnValueChanged -= OnBuyExpBtnTextChanged;
                mBuyButtonsVm.CanBuySkill.OnValueChanged -= OnCanBuySkillChanged;
                mBuyButtonsVm.CanBuyExp.OnValueChanged -= OnCanBuyExpChanged;
                mBuyButtonsVm.CityLevel.OnValueChanged -= OnBuyBtnCityLevelChanged;
                mBuyButtonsVm.ExpProgress.OnValueChanged -= OnBuyBtnExpProgressChanged;
                mBuyButtonsVm = null;
            }

            // OnDestroy 时序下节点可能已销毁
            if (BuySkillBtn != null)
            {
                BuySkillBtn.onClick.RemoveListener(OnBuySkillBtnClicked);
            }

            if (BuyExpBtn != null)
            {
                BuyExpBtn.onClick.RemoveListener(OnBuyExpBtnClicked);
            }

            CancelBuySkillBtnStateTween();
            CancelBuyExpBtnStateTween();
        }

        private void CacheBuyBtnEffects()
        {
            mBuySkillEffectGo = null;
            mBuyExpEffectGo = null;

            var skillEffect = BuySkillBtn.transform.Find("effect");
            if (skillEffect != null)
            {
                mBuySkillEffectGo = skillEffect.gameObject;
            }

            var expEffect = BuyExpBtn.transform.Find("effect");
            if (expEffect != null)
            {
                mBuyExpEffectGo = expEffect.gameObject;
            }
        }

        private void BindBuyButtonListeners(BattleInterViewModel vm)
        {
            vm.BuySkillBtnText.OnValueChanged += OnBuySkillBtnTextChanged;
            vm.BuyExpBtnText.OnValueChanged += OnBuyExpBtnTextChanged;
            vm.CanBuySkill.OnValueChanged += OnCanBuySkillChanged;
            vm.CanBuyExp.OnValueChanged += OnCanBuyExpChanged;
            vm.CityLevel.OnValueChanged += OnBuyBtnCityLevelChanged;
            vm.ExpProgress.OnValueChanged += OnBuyBtnExpProgressChanged;

            BuySkillBtn.onClick.AddListener(OnBuySkillBtnClicked);
            BuyExpBtn.onClick.AddListener(OnBuyExpBtnClicked);
        }

        private void InitializeBuyButtonsUI(BattleInterViewModel vm)
        {
            UpdateBuySkillBtnText(vm.BuySkillBtnText.Value);
            UpdateBuyExpBtnText(vm.BuyExpBtnText.Value);
            SetBuySkillBtnPurchasableState(vm.CanBuySkill.Value, false);
            SetBuyExpBtnPurchasableState(vm.CanBuyExp.Value, false);
            RefreshBuyExpBtnUI(vm);
        }

        private void OnBuySkillBtnClicked()
        {
            PlayBuyBtnClickSound();
            mBuyButtonsVm.OnBuySkillCommand.Execute();
        }

        private void OnBuyExpBtnClicked()
        {
            PlayBuyBtnClickSound();
            mBuyButtonsVm.OnBuyExpCommand.Execute();
        }

        private void PlayBuyBtnClickSound()
        {
            var audioSource = UIFramework.UIRoot.Instance.transform.Find("Audio Source").GetComponent<AudioSource>();
            audioSource.Play();
        }

        private void OnBuySkillBtnTextChanged(string oldVal, string newVal)
        {
            UpdateBuySkillBtnText(newVal);
        }

        private void OnBuyExpBtnTextChanged(string oldVal, string newVal)
        {
            UpdateBuyExpBtnText(newVal);
        }

        private void OnCanBuySkillChanged(bool oldVal, bool newVal)
        {
            SetBuySkillBtnPurchasableState(newVal, !oldVal && newVal);
        }

        private void OnCanBuyExpChanged(bool oldVal, bool newVal)
        {
            SetBuyExpBtnPurchasableState(newVal, !oldVal && newVal);
        }

        private void OnBuyBtnCityLevelChanged(int oldVal, int newVal)
        {
            RefreshBuyExpBtnUI(mBuyButtonsVm);
        }

        private void OnBuyBtnExpProgressChanged(float oldVal, float newVal)
        {
            RefreshBuyExpBtnUI(mBuyButtonsVm);
        }

        private void RefreshBuyExpBtnUI(BattleInterViewModel vm)
        {
            var progressImage = BuyExpBtn.transform.Find("ExpProgress").GetComponent<Image>();
            var curLevelText = BuyExpBtn.transform.Find("CurLevelNumBg/CurLevelNum").GetComponent<TextMeshProUGUI>();
            var levelText = BuyExpBtn.transform.Find("LevelNum").GetComponent<TextMeshProUGUI>();

            var currentLevel = Mathf.Max(vm.CityLevel.Value, 1);
            var requiredExp = GetBuyBtnRequiredCityExp(currentLevel);
            var progress = requiredExp > 0 ? Mathf.Clamp01((float)vm.CityExp.Value / requiredExp) : 0f;

            var cityLevelUpperLimit = GetBuyBtnCityLevelUpperLimit();
            if (cityLevelUpperLimit > 0 && vm.CityLevel.Value >= cityLevelUpperLimit)
            {
                progress = 1f;
            }

            progressImage.DOFillAmount(progress * 0.482f, 0.3f).SetEase(Ease.OutQuad);
            curLevelText.text = $"{vm.CityLevel.Value}";
            levelText.text = $"{vm.CityExp.Value}/{requiredExp:0}";
        }

        private int GetBuyBtnCityLevelUpperLimit()
        {
            var difficultyCfg = DataConfigManager.Tables.TbCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            if (difficultyCfg == null || difficultyCfg.LevelList == null)
            {
                return 0;
            }

            return difficultyCfg.LevelList.Max(cfg => cfg.Level);
        }

        private int GetBuyBtnRequiredCityExp(int level)
        {
            var difficultyCfg = DataConfigManager.Tables.TbCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            if (difficultyCfg == null || level <= 0 || level > difficultyCfg.LevelList.Count)
            {
                return 0;
            }

            return difficultyCfg.LevelList[level - 1].CityExp;
        }

        private void SetBuySkillBtnPurchasableState(bool canBuy, bool animate)
        {
            CancelBuySkillBtnStateTween();
            UpdateBuySkillBtnNumColor(canBuy);

            if (!canBuy)
            {
                BuySkillUseGameObject.SetActive(false);
                if (mBuySkillEffectGo != null)
                {
                    mBuySkillEffectGo.SetActive(false);
                }

                BuySkillNoUseGameObject.SetActive(true);
                return;
            }

            if (!animate)
            {
                BuySkillUseGameObject.SetActive(true);
                if (mBuySkillEffectGo != null)
                {
                    mBuySkillEffectGo.SetActive(true);
                }

                BuySkillNoUseGameObject.SetActive(false);
                return;
            }

            BuySkillUseGameObject.SetActive(false);
            BuySkillNoUseGameObject.SetActive(true);
            if (mBuySkillEffectGo != null)
            {
                mBuySkillEffectGo.SetActive(true);
            }

            mBuySkillStateTween = DOVirtual.DelayedCall(BuyBtnEffectDelay, OnBuySkillBtnEffectDelayComplete);
        }

        private void OnBuySkillBtnEffectDelayComplete()
        {
            mBuySkillStateTween = null;
            BuySkillUseGameObject.SetActive(true);
            BuySkillNoUseGameObject.SetActive(false);
            if (mBuySkillEffectGo != null)
            {
                mBuySkillEffectGo.SetActive(true);
            }
        }

        private void SetBuyExpBtnPurchasableState(bool canBuy, bool animate)
        {
            CancelBuyExpBtnStateTween();
            UpdateBuyExpBtnNumColor(canBuy);

            if (!canBuy)
            {
                BuyExpUseGameObject.SetActive(false);
                if (mBuyExpEffectGo != null)
                {
                    mBuyExpEffectGo.SetActive(false);
                }

                BuyExpNoUseGameObject.SetActive(true);
                return;
            }

            if (!animate)
            {
                BuyExpUseGameObject.SetActive(true);
                if (mBuyExpEffectGo != null)
                {
                    mBuyExpEffectGo.SetActive(true);
                }

                BuyExpNoUseGameObject.SetActive(false);
                return;
            }

            BuyExpUseGameObject.SetActive(false);
            BuyExpNoUseGameObject.SetActive(true);
            if (mBuyExpEffectGo != null)
            {
                mBuyExpEffectGo.SetActive(true);
            }

            mBuyExpStateTween = DOVirtual.DelayedCall(BuyBtnEffectDelay, OnBuyExpBtnEffectDelayComplete);
        }

        private void OnBuyExpBtnEffectDelayComplete()
        {
            mBuyExpStateTween = null;
            BuyExpUseGameObject.SetActive(true);
            BuyExpNoUseGameObject.SetActive(false);
            if (mBuyExpEffectGo != null)
            {
                mBuyExpEffectGo.SetActive(true);
            }
        }

        private void CancelBuySkillBtnStateTween()
        {
            if (mBuySkillStateTween != null)
            {
                mBuySkillStateTween.Kill();
                mBuySkillStateTween = null;
            }
        }

        private void CancelBuyExpBtnStateTween()
        {
            if (mBuyExpStateTween != null)
            {
                mBuyExpStateTween.Kill();
                mBuyExpStateTween = null;
            }
        }

        private void UpdateBuySkillBtnNumColor(bool canBuy)
        {
            var numText = BuySkillBtn.transform.Find("Num").GetComponent<TextMeshProUGUI>();
            numText.color = canBuy ? Color.white : new Color32(255, 61, 61, 255);
        }

        private void UpdateBuyExpBtnNumColor(bool canBuy)
        {
            var numText = BuyExpBtn.transform.Find("Num").GetComponent<TextMeshProUGUI>();
            numText.color = canBuy ? Color.white : new Color32(255, 61, 61, 255);
        }

        private void UpdateBuySkillBtnText(string text)
        {
            var numText = BuySkillBtn.transform.Find("Num").GetComponent<TextMeshProUGUI>();
            numText.text = text;
        }

        private void UpdateBuyExpBtnText(string text)
        {
            var numText = BuyExpBtn.transform.Find("Num").GetComponent<TextMeshProUGUI>();
            numText.text = text;
        }
    }
}
