using System.Collections.Generic;
using Config;
using DG.Tweening;
using FusionContinent.Helper;
using QFramework;
using TMPro;
using UnityEngine;

namespace FusionContinent
{
    public partial class GameMap
    {
        private static readonly string[] HeroSlotLockNames = { "lock2", "lock3", "lock4" };
        private const float LockShowFloatOffset = 12f;
        private const float LockShowFloatDuration = 1.2f;

        private readonly List<HeroSlotLockView> _heroSlotLocks = new List<HeroSlotLockView>();
        private List<int> _heroSlotUnlockLevels = new List<int>();

        private class HeroSlotLockView
        {
            public GameObject LockRoot;
            public RectTransform LockShowRect;
            public TMP_Text UnlockLevelText;
            public float LockShowBaseY;
            public Tween FloatTween;
        }

        private void InitHeroSlotLocks()
        {
            var bottom2 = BattleArenaVisibility.FindUnderPve(transform, "Bg2/bottom2");
            if (bottom2 == null)
            {
                return;
            }

            _heroSlotUnlockLevels = BuildHeroSlotUnlockLevels();
            _heroSlotLocks.Clear();

            for (int i = 0; i < HeroSlotLockNames.Length; i++)
            {
                var lockTransform = bottom2.Find(HeroSlotLockNames[i]);
                if (lockTransform == null)
                {
                    LogTool.LogError($"[GameMap] 找不到 {HeroSlotLockNames[i]}");
                    continue;
                }

                var lockShow = lockTransform.Find("lockshow");
                if (lockShow == null)
                {
                    LogTool.LogError($"[GameMap] 找不到 {HeroSlotLockNames[i]}/lockshow");
                    continue;
                }

                var levelTextTransform = lockShow.Find("Text (TMP)");
                if (levelTextTransform == null)
                {
                    LogTool.LogError($"[GameMap] 找不到 {HeroSlotLockNames[i]}/lockshow/Text (TMP)");
                    continue;
                }

                var levelText = levelTextTransform.GetComponent<TMP_Text>();
                if (levelText == null)
                {
                    LogTool.LogError($"[GameMap] {HeroSlotLockNames[i]}/lockshow/Text (TMP) 缺少 TMP_Text 组件");
                    continue;
                }

                var lockShowRect = lockShow as RectTransform;
                _heroSlotLocks.Add(new HeroSlotLockView
                {
                    LockRoot = lockTransform.gameObject,
                    LockShowRect = lockShowRect,
                    UnlockLevelText = levelText,
                    LockShowBaseY = lockShowRect.anchoredPosition.y,
                });
            }

            Global.GameDifficultyLevel.RegisterWithInitValue(OnHeroSlotDifficultyChanged)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
            Global.CityLevel.RegisterWithInitValue(OnCityLevelChangedForHeroSlots)
                .UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        private void OnHeroSlotDifficultyChanged(int difficulty)
        {
            _heroSlotUnlockLevels = BuildHeroSlotUnlockLevels();
            RefreshHeroSlotLocks();
        }

        private void OnCityLevelChangedForHeroSlots(int cityLevel)
        {
            RefreshHeroSlotLocks();
        }

        private List<int> BuildHeroSlotUnlockLevels()
        {
            var unlockLevels = new List<int>();
            var cityLevelConfig = DataConfigManager.Tables.TbCityLevelConfig.GetOrDefault(Global.GameDifficultyLevel.Value);
            foreach (var levelCfg in cityLevelConfig.LevelList)
            {
                if (levelCfg.IsAddHero > 0)
                {
                    unlockLevels.Add(levelCfg.Level);
                }
            }

            return unlockLevels;
        }

        private void RefreshHeroSlotLocks()
        {
            var cityLevel = Global.CityLevel.Value;
            for (int i = 0; i < _heroSlotLocks.Count; i++)
            {
                var view = _heroSlotLocks[i];
                if (i >= _heroSlotUnlockLevels.Count)
                {
                    StopLockShowFloat(view);
                    view.LockRoot.SetActive(false);
                    continue;
                }

                var unlockLevel = _heroSlotUnlockLevels[i];
                view.UnlockLevelText.text = $"lv {unlockLevel}解锁";

                var unlocked = cityLevel >= unlockLevel;
                view.LockRoot.SetActive(!unlocked);
                StopLockShowFloat(view);
                view.LockShowRect.anchoredPosition = new Vector2(
                    view.LockShowRect.anchoredPosition.x,
                    view.LockShowBaseY);

                if (!unlocked)
                {
                    view.FloatTween = view.LockShowRect
                        .DOAnchorPosY(view.LockShowBaseY + LockShowFloatOffset, LockShowFloatDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo);
                }
            }
        }

        private void StopLockShowFloat(HeroSlotLockView view)
        {
            if (view.FloatTween != null)
            {
                view.FloatTween.Kill();
                view.FloatTween = null;
            }
        }

        private void ClearHeroSlotLockTweens()
        {
            for (int i = 0; i < _heroSlotLocks.Count; i++)
            {
                StopLockShowFloat(_heroSlotLocks[i]);
            }
        }
    }
}
