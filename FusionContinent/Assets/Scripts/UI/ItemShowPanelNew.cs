using System;
using Config;
using FusionContinent.Animation;
using FusionContinent.Utils;
using UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace FusionContinent
{
    [PrefabPath("ItemShowPanel")]
    public class ItemShowPanelNew : UIPanel<ItemShowPanelViewModel>
    {
        private ItemShowPanel mOldPanel;
        private UnityEngine.Events.UnityAction mCloseHandler;

        void Awake()
        {
            mOldPanel = GetComponent<ItemShowPanel>();
            if (mOldPanel == null)
            {
                LogTool.LogError("[ItemShowPanelNew] 找不到 ItemShowPanel 组件");
                return;
            }

            // 禁用旧 Panel 组件，避免潜在的生命周期冲突
            mOldPanel.enabled = false;
        }

        protected override void OnBindViewModel(ItemShowPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            mCloseHandler = () => vm.OnCloseCommand.Execute();

            mOldPanel.Mask.onClick.RemoveAllListeners();
            mOldPanel.Mask.onClick.AddListener(mCloseHandler);

            vm.OnCloseRequested += OnCloseRequested;
        }

        protected override void OnUnbindViewModel(ItemShowPanelViewModel vm)
        {
            if (mOldPanel == null) return;

            if (mCloseHandler != null)
            {
                mOldPanel.Mask.onClick.RemoveListener(mCloseHandler);
                mCloseHandler = null;
            }

            vm.OnCloseRequested -= OnCloseRequested;
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            if (VM == null || mOldPanel == null) return;

            // 提升到 Tips 层级，保证始终在其他 PopUI 上方
            var tipsRoot = UIRoot.Instance?.Config?.GetLevelTransform(UILevel.Tips);
            if (tipsRoot != null)
            {
                Level = UILevel.Tips;
                transform.SetParent(tipsRoot, false);
                transform.SetAsLastSibling();
            }

            SetData(VM.Data);
        }

        private void SetData(RewardEntityData data)
        {
            data.Check = false;
            data.IsEffect = false;
            data.Status = RewardStatusType.Default;
            data.StyleType = RewardStyleType.Default;
            mOldPanel.RewardEntity.GetComponent<RewardEntity>().SetData(data);
            mOldPanel.RewardEntity.transform.Find("TweenTarget/Num").gameObject.SetActive(false);
            SetItemPanelBg(data.Quality);
            mOldPanel.NameText.text = $"{data.Name}";
            // mOldPanel.NumText.text = $"x{NumberTool.FormatNumber(Global.GetItemNumById(data.ItemId))}";
            mOldPanel.NumText.text = "";
            mOldPanel.DescText.text = $"{data.Desc}";
            //WayText.text = $"{data.Access}";
            UIAnimationUtils.PlayAnimations(transform.Find("OpenAnimation"));
        }

        private void SetItemPanelBg(QualityType quality)
        {
            switch (quality)
            {
                case QualityType.Default:
                case QualityType.Ordinary:
                    mOldPanel.panelBG.GetComponent<Image>().sprite = Global.EquipBgStyleAtlas.GetSprite("Ordinary");
                    break;
                case QualityType.Rarity:
                    mOldPanel.panelBG.GetComponent<Image>().sprite = Global.EquipBgStyleAtlas.GetSprite("Rarity");
                    break;
                case QualityType.Epic:
                    mOldPanel.panelBG.GetComponent<Image>().sprite = Global.EquipBgStyleAtlas.GetSprite("Epic");
                    break;
                case QualityType.Legend:
                    mOldPanel.panelBG.GetComponent<Image>().sprite = Global.EquipBgStyleAtlas.GetSprite("Legend");
                    break;
                case QualityType.Ultimate:
                    mOldPanel.panelBG.GetComponent<Image>().sprite = Global.EquipBgStyleAtlas.GetSprite("Ultimate");
                    break;
                case QualityType.Mythology:
                    mOldPanel.panelBG.GetComponent<Image>().sprite = Global.EquipBgStyleAtlas.GetSprite("Mythology");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quality), quality, null);
            }
        }

        private void OnCloseRequested()
        {
            var closeAnimation = transform.Find("CloseAnimation");
            if (closeAnimation != null)
            {
                UIAnimationUtils.PlayAnimations(closeAnimation.gameObject, () =>
                {
                    PlayClickSound();
                    CloseSelf();
                });
            }
            else
            {
                PlayClickSound();
                CloseSelf();
            }
        }

        private void PlayClickSound()
        {
            var audioSource = UIRoot.Instance?.transform.Find("Audio Source")?.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
}
